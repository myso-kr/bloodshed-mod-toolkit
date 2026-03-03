using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloodshedModToolkit.Coop.Bots
{
    /// <summary>
    /// 런타임 레이캐스트 지형 스캔 + A* 경로탐색.
    ///
    /// [스캔]  Physics.Raycast — Unity 메인 스레드(BotManager.Update)에서 Scan() 호출
    /// [탐색]  FindPath / GetRandomWalkableNear — 순수 배열 연산, 어느 컨텍스트에서도 안전
    ///
    /// 항아리식 지형 대응:
    ///   - 1m 셀로 좁은 출구(2m)에 2셀 확보
    ///   - 상향 레이로 천장 낮은 내부 필터
    ///   - A*가 출구까지의 최단 경로를 탐색 → 국소 회피만으론 불가한 경우 해결
    /// </summary>
    public static class NavGrid
    {
        // ── 그리드 파라미터 ───────────────────────────────────────────────────
        public  const int   Width    = 80;
        public  const int   Height   = 80;
        public  const float CellSize = 1.0f;  // 1m — 좁은 통로(2m)에 셀 2개 확보
        private const float ScanAlt  = 3.0f;  // 레이 시작 오프셋 (지면 위)
        private const float RayDown  = 8.0f;  // 하향 레이 최대 길이
        private const float HeadRoom = 1.6f;  // 봇 통과 최소 높이

        // ── 그리드 데이터 ─────────────────────────────────────────────────────
        private static readonly bool[] _walkable = new bool[Width * Height];
        private static Vector3 _origin;
        private static float   _centerY;
        private static Vector3 _scanCenter;
        private static bool    _scanned = false;

        // ── A* 사전 할당 버퍼 (GC 압력 최소화) ───────────────────────────────
        private static readonly float[] _g      = new float[Width * Height];
        private static readonly float[] _f      = new float[Width * Height];
        private static readonly int[]   _parent = new int[Width * Height];
        private static readonly bool[]  _open   = new bool[Width * Height];
        private static readonly bool[]  _closed = new bool[Width * Height];
        private static readonly List<int> _openList = new(256);

        private static readonly System.Random _rng = new();

        public static bool IsScanned => _scanned;

        // ── 스캔 ─────────────────────────────────────────────────────────────
        // BotManager.Update에서만 호출 (Physics.Raycast = 메인 스레드 필수)
        public static void Scan(Vector3 center)
        {
            _origin = new Vector3(
                center.x - Width  * CellSize * 0.5f,
                0f,
                center.z - Height * CellSize * 0.5f);
            _centerY    = center.y;
            _scanCenter = center;

            float rayStartY = center.y + ScanAlt;
            var   downDir   = new Vector3(0f, -1f, 0f);
            var   upDir     = new Vector3(0f,  1f, 0f);
            int   walkCount = 0;

            for (int cz = 0; cz < Height; cz++)
            for (int cx = 0; cx < Width;  cx++)
            {
                int   idx = cz * Width + cx;
                float wx  = _origin.x + (cx + 0.5f) * CellSize;
                float wz  = _origin.z + (cz + 0.5f) * CellSize;

                // ① 하향 레이: 지면 존재 확인
                bool hasGround = Physics.Raycast(
                    new Vector3(wx, rayStartY, wz), downDir, RayDown + ScanAlt);

                if (!hasGround) { _walkable[idx] = false; continue; }

                // ② 상향 레이: 낮은 천장 필터 (HeadRoom 확보 못 하면 불통행)
                bool lowCeiling = Physics.Raycast(
                    new Vector3(wx, center.y + 0.05f, wz), upDir, HeadRoom);

                _walkable[idx] = !lowCeiling;
                if (_walkable[idx]) walkCount++;
            }

            _scanned = true;
            Plugin.Log.LogInfo(
                $"[NavGrid] 스캔 완료 — {Width}×{Height} ({walkCount} 통행 가능), center={center}");
        }

        // 플레이어가 threshold 이상 이동했으면 재스캔 필요
        public static bool NeedsRescan(Vector3 playerPos, float threshold = 25f)
        {
            if (!_scanned) return true;
            float dx = playerPos.x - _scanCenter.x;
            float dz = playerPos.z - _scanCenter.z;
            return dx * dx + dz * dz > threshold * threshold;
        }

        // ── A* 경로 탐색 ──────────────────────────────────────────────────────
        public static List<Vector3>? FindPath(Vector3 from, Vector3 to)
        {
            if (!_scanned) return null;

            WorldToCell(from, out int fx, out int fz);
            WorldToCell(to,   out int tx, out int tz);

            // 불통행 시작/목표 → 가장 가까운 통행 셀로 보정
            if (!_walkable[fz * Width + fx]) FindNearestWalkable(ref fx, ref fz);
            if (!_walkable[tz * Width + tx]) FindNearestWalkable(ref tx, ref tz);

            int startIdx = fz * Width + fx;
            int goalIdx  = tz * Width + tx;
            if (startIdx == goalIdx) return null;

            // ── 버퍼 초기화 ──────────────────────────────────────────────────
            int total = Width * Height;
            Array.Fill(_g,      float.MaxValue, 0, total);
            Array.Fill(_parent, -1,             0, total);
            Array.Clear(_open,   0, total);
            Array.Clear(_closed, 0, total);
            _openList.Clear();

            _g[startIdx] = 0f;
            _f[startIdx] = Heuristic(fx, fz, tx, tz);
            _openList.Add(startIdx);
            _open[startIdx] = true;

            // 8방향 이웃 (dx, dz, 비용)
            ReadOnlySpan<int>   ndx = stackalloc int[]   { -1, 0, 1, -1, 1, -1, 0, 1 };
            ReadOnlySpan<int>   ndz = stackalloc int[]   { -1,-1,-1,  0, 0,  1, 1, 1 };
            ReadOnlySpan<float> ndc = stackalloc float[] { 1.414f, 1f, 1.414f, 1f, 1f, 1.414f, 1f, 1.414f };

            while (_openList.Count > 0)
            {
                // 최소 fScore 노드 선택
                int bestPos = 0;
                for (int i = 1; i < _openList.Count; i++)
                    if (_f[_openList[i]] < _f[_openList[bestPos]]) bestPos = i;

                int cur = _openList[bestPos];
                _openList[bestPos] = _openList[_openList.Count - 1];
                _openList.RemoveAt(_openList.Count - 1);
                _open[cur] = false;

                if (cur == goalIdx) return BuildPath(cur, from);

                _closed[cur] = true;
                int curX = cur % Width, curZ = cur / Width;

                for (int d = 0; d < 8; d++)
                {
                    int nx = curX + ndx[d], nz = curZ + ndz[d];
                    if (nx < 0 || nx >= Width || nz < 0 || nz >= Height) continue;

                    int nIdx = nz * Width + nx;
                    if (_closed[nIdx] || !_walkable[nIdx]) continue;

                    // 대각 이동 코너 끼임 방지: 인접 직선 셀 모두 통행 가능이어야 함
                    if (ndx[d] != 0 && ndz[d] != 0)
                    {
                        if (!_walkable[curZ * Width + nx]) continue;
                        if (!_walkable[nz  * Width + curX]) continue;
                    }

                    float ng = _g[cur] + ndc[d];
                    if (ng < _g[nIdx])
                    {
                        _parent[nIdx] = cur;
                        _g[nIdx] = ng;
                        _f[nIdx] = ng + Heuristic(nx, nz, tx, tz);
                        if (!_open[nIdx]) { _openList.Add(nIdx); _open[nIdx] = true; }
                    }
                }
            }
            return null; // 경로 없음 (항아리 완전 폐쇄 등)
        }

        // Wander용 — center 근방 랜덤 통행 가능 지점 반환
        public static Vector3? GetRandomWalkableNear(Vector3 center, float radius)
        {
            if (!_scanned) return null;
            WorldToCell(center, out int cx, out int cz);
            int r = Math.Max(1, (int)(radius / CellSize));

            for (int attempt = 0; attempt < 24; attempt++)
            {
                int ox = cx + (int)((_rng.NextDouble() * 2.0 - 1.0) * r);
                int oz = cz + (int)((_rng.NextDouble() * 2.0 - 1.0) * r);
                if (ox < 0 || ox >= Width || oz < 0 || oz >= Height) continue;
                if (!_walkable[oz * Width + ox]) continue;
                // 너무 가까운 곳은 제외 (최소 r×0.25 거리)
                int ddx = ox - cx, ddz = oz - cz;
                if (ddx * ddx + ddz * ddz < (int)(r * r * 0.06f)) continue;
                return CellToWorld(ox, oz);
            }
            return null;
        }

        // ── 내부 헬퍼 ────────────────────────────────────────────────────────

        private static void WorldToCell(Vector3 pos, out int cx, out int cz)
        {
            cx = (int)((pos.x - _origin.x) / CellSize);
            cz = (int)((pos.z - _origin.z) / CellSize);
            if (cx < 0) cx = 0; else if (cx >= Width)  cx = Width  - 1;
            if (cz < 0) cz = 0; else if (cz >= Height) cz = Height - 1;
        }

        private static Vector3 CellToWorld(int cx, int cz)
            => new Vector3(
                _origin.x + (cx + 0.5f) * CellSize,
                _centerY,
                _origin.z + (cz + 0.5f) * CellSize);

        // 불통행 셀 → 외곽 링 방향으로 확장하며 가장 가까운 통행 셀 탐색
        private static void FindNearestWalkable(ref int cx, ref int cz)
        {
            for (int r = 1; r <= 10; r++)
            for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx < -r || dx > r) continue;
                if (Math.Abs(dx) < r && Math.Abs(dz) < r) continue; // 내부 스킵
                int nx = cx + dx, nz = cz + dz;
                if (nx < 0 || nx >= Width || nz < 0 || nz >= Height) continue;
                if (_walkable[nz * Width + nx]) { cx = nx; cz = nz; return; }
            }
        }

        // 옥틸 거리 휴리스틱 (8방향 A*에 최적)
        private static float Heuristic(int x1, int z1, int x2, int z2)
        {
            int dx = x1 > x2 ? x1 - x2 : x2 - x1;
            int dz = z1 > z2 ? z1 - z2 : z2 - z1;
            int maxD = dx > dz ? dx : dz, minD = dx < dz ? dx : dz;
            return (float)maxD + 0.414f * (float)minD;
        }

        private static List<Vector3> BuildPath(int goalIdx, Vector3 fromWorld)
        {
            var cells = new List<int>(32);
            int cur = goalIdx;
            while (cur >= 0) { cells.Add(cur); cur = _parent[cur]; }
            cells.Reverse();

            var path = new List<Vector3>(cells.Count);
            foreach (var idx in cells)
                path.Add(CellToWorld(idx % Width, idx / Width));

            // 시작 셀(봇 현재 위치)은 제거
            if (path.Count > 0) path.RemoveAt(0);
            return path;
        }
    }
}
