using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloodshedModToolkit.Coop.Bots
{
    /// <summary>
    /// 런타임 레이캐스트 지형 스캔 + A* 경로탐색 (Phase A: 3D 경사 지원)
    ///
    /// [스캔]  셀당 4개 레이캐스트 (중앙 + 동·북 삼각측량 + 상향 천장)
    ///   - 중앙 하향 레이  → RaycastHit.point.y → groundY 저장
    ///   - East/North 하향 → cross product → slopeAngle 계산
    ///   - 상향 레이       → 천장 높이 < HeadRoom → 불통행
    ///   - Pass 2          → 이웃 낙차 > CliffDrop → 절벽 = 불통행
    ///
    /// [탐색]  A* 간선 비용 = 수평거리 × SlopeCost(평균경사) + ΔY 페널티
    ///         (오르막 ×1.5, 내리막 ×0.3)
    ///
    /// [웨이포인트]  CellToWorld가 groundY를 반환 → 지형 표면에 스냅
    /// </summary>
    public static class NavGrid
    {
        // ── 그리드 파라미터 ───────────────────────────────────────────────────
        public  const int   Width    = 80;
        public  const int   Height   = 80;
        public  const float CellSize = 1.0f;   // 1m — 좁은 통로(2m)에 셀 2개 확보
        private const float ScanAlt  = 3.0f;   // 레이 시작 오프셋 (지면 위)
        private const float RayDown  = 8.0f;   // 하향 레이 최대 길이
        private const float HeadRoom = 1.6f;   // 봇 통과 최소 높이
        private const float CliffDrop     = 1.5f;  // 이웃과의 낙차 한계 (절벽 감지)
        private const float MaxSlopeAngle = 45f;   // CharacterController.slopeLimit
        private const float TriOffset     = 0.40f; // 삼각측량 샘플 오프셋 (m, 셀 내부)
        private const float CnyConst      = -(TriOffset * TriOffset); // = -0.16f (항상 고정)

        // ── 그리드 데이터 ─────────────────────────────────────────────────────
        private static readonly bool[]  _walkable   = new bool[Width * Height];
        private static readonly bool[]  _hasGround  = new bool[Width * Height]; // 지면 존재 여부
        private static readonly float[] _groundY    = new float[Width * Height]; // 지면 Y 좌표
        private static readonly float[] _slopeAngle = new float[Width * Height]; // 경사각 (도)

        private static Vector3 _origin;
        private static Vector3 _scanCenter;
        private static bool    _scanned = false;

        // ── A* 사전 할당 버퍼 (GC 압력 최소화) ───────────────────────────────
        private static readonly float[]   _g        = new float[Width * Height];
        private static readonly float[]   _f        = new float[Width * Height];
        private static readonly int[]     _parent   = new int[Width * Height];
        private static readonly bool[]    _open     = new bool[Width * Height];
        private static readonly bool[]    _closed   = new bool[Width * Height];
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
            _scanCenter = center;

            float rayStartY  = center.y + ScanAlt;
            float maxRayDist = RayDown + ScanAlt;
            var   downDir    = new Vector3(0f, -1f, 0f);
            var   upDir      = new Vector3(0f,  1f, 0f);

            // ── Pass 1: 셀별 지면 Y · 경사각 · 천장 ────────────────────────
            for (int cz = 0; cz < Height; cz++)
            for (int cx = 0; cx < Width;  cx++)
            {
                int   idx = cz * Width + cx;
                float wx  = _origin.x + (cx + 0.5f) * CellSize;
                float wz  = _origin.z + (cz + 0.5f) * CellSize;

                // ① 중앙 하향 레이 — 지면 Y 획득
                bool hasGround = Physics.Raycast(
                    new Vector3(wx, rayStartY, wz), downDir, out var hitC, maxRayDist);

                _hasGround[idx] = hasGround;
                if (!hasGround)
                {
                    _walkable[idx]   = false;
                    _groundY[idx]    = center.y - maxRayDist; // 심연 센티넬
                    _slopeAngle[idx] = 0f;
                    continue;
                }

                float groundY = hitC.point.y;
                _groundY[idx] = groundY;

                // ② 상향 레이 — 낮은 천장 필터 (실제 지면 Y 기준)
                bool lowCeiling = Physics.Raycast(
                    new Vector3(wx, groundY + 0.05f, wz), upDir, HeadRoom);
                if (lowCeiling) { _walkable[idx] = false; _slopeAngle[idx] = 0f; continue; }

                // ③ 삼각측량 — East·North 샘플 하향 레이로 지면 법선 계산
                //
                //   P_C (wx,       wz      )  중앙
                //   P_E (wx+T,     wz      )  동쪽
                //   P_N (wx,       wz+T    )  북쪽
                //
                //   v1 = P_E - P_C = (T,  dy1, 0)
                //   v2 = P_N - P_C = (0,  dy2, T)
                //   cross(v1,v2)   = (T*dy1, -T², T*dy2)
                //   → ny는 항상 음수이므로 abs 적용 후 acos
                //
                bool hE = Physics.Raycast(
                    new Vector3(wx + TriOffset, rayStartY, wz), downDir, out var hitE, maxRayDist);
                bool hN = Physics.Raycast(
                    new Vector3(wx, rayStartY, wz + TriOffset), downDir, out var hitN, maxRayDist);

                if (hE && hN)
                {
                    float dy1   = hitE.point.y - groundY;
                    float dy2   = hitN.point.y - groundY;
                    float cnx   = TriOffset * dy1;
                    float cnz   = TriOffset * dy2;
                    // CnyConst = -0.16f (상수), CnyConst² = 0.0256f
                    float cnLen = MathF.Sqrt(cnx * cnx + 0.0256f + cnz * cnz);
                    _slopeAngle[idx] = cnLen > 0.0001f
                        ? MathF.Acos(MathF.Min(1f, 0.16f / cnLen)) * (180f / MathF.PI)
                        : 0f;
                }
                else
                {
                    _slopeAngle[idx] = 0f;
                }

                // ④ 경사 > 45° → 불통행
                if (_slopeAngle[idx] > MaxSlopeAngle) { _walkable[idx] = false; continue; }

                _walkable[idx] = true;
            }

            // ── Pass 2: 절벽 감지 ────────────────────────────────────────────
            // 이웃 셀과 낙차가 CliffDrop 초과이거나 이웃이 지면 없음(심연) → 절벽 처리
            for (int cz = 0; cz < Height; cz++)
            for (int cx = 0; cx < Width;  cx++)
            {
                int idx = cz * Width + cx;
                if (!_walkable[idx]) continue;

                float myY = _groundY[idx];

                // 4방향 이웃 낙차 체크
                if (cx > 0          && IsCliffNeighbor(idx - 1,     myY)) { _walkable[idx] = false; continue; }
                if (cx < Width  - 1 && IsCliffNeighbor(idx + 1,     myY)) { _walkable[idx] = false; continue; }
                if (cz > 0          && IsCliffNeighbor(idx - Width,  myY)) { _walkable[idx] = false; continue; }
                if (cz < Height - 1 && IsCliffNeighbor(idx + Width,  myY)) { _walkable[idx] = false; continue; }
            }

            _scanned = true;
#if DEBUG
            int walkCount = 0;
            for (int i = 0, n = Width * Height; i < n; i++) if (_walkable[i]) walkCount++;
            Plugin.Log.LogInfo($"[NavGrid] 스캔 완료 — {Width}×{Height} ({walkCount} 통행 가능), center={center}");
#else
            Plugin.Log.LogInfo($"[NavGrid] 스캔 완료 — {Width}×{Height}, center={center}");
#endif
        }

        // 이웃이 지면 없음(심연)이거나 낙차 > CliffDrop → true
        private static bool IsCliffNeighbor(int nIdx, float myY)
            => !_hasGround[nIdx] || _groundY[nIdx] < myY - CliffDrop;

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

            // 8방향 이웃 (dx, dz, 수평 비용)
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

                if (cur == goalIdx) return BuildPath(cur);

                _closed[cur] = true;
                int curX = cur % Width, curZ = cur / Width;

                for (int d = 0; d < 8; d++)
                {
                    int nx = curX + ndx[d], nz = curZ + ndz[d];
                    if (nx < 0 || nx >= Width || nz < 0 || nz >= Height) continue;

                    int nIdx = nz * Width + nx;
                    if (_closed[nIdx] || !_walkable[nIdx]) continue;

                    // 대각 이동 코너 끼임 방지
                    if (ndx[d] != 0 && ndz[d] != 0)
                    {
                        if (!_walkable[curZ * Width + nx]) continue;
                        if (!_walkable[nz  * Width + curX]) continue;
                    }

                    // ── 3D 간선 비용: 수평 × 경사 가중치 + 고도차 페널티 ──
                    float dY       = _groundY[nIdx] - _groundY[cur];
                    float avgSlope = (_slopeAngle[cur] + _slopeAngle[nIdx]) * 0.5f;
                    float dyPenalty = dY > 0f ? dY * 1.5f : -dY * 0.3f; // 오르막 1.5×, 내리막 0.3×
                    float ng = _g[cur] + ndc[d] * SlopeCost(avgSlope) + dyPenalty;

                    if (ng < _g[nIdx])
                    {
                        _parent[nIdx] = cur;
                        _g[nIdx] = ng;
                        _f[nIdx] = ng + Heuristic(nx, nz, tx, tz);
                        if (!_open[nIdx]) { _openList.Add(nIdx); _open[nIdx] = true; }
                    }
                }
            }
            return null;
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
                int ddx = ox - cx, ddz = oz - cz;
                if (ddx * ddx + ddz * ddz < Math.Max(1, (int)(r * r * 0.06f))) continue;
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

        // 웨이포인트 Y = 실제 지면 높이 (groundY 반환)
        private static Vector3 CellToWorld(int cx, int cz)
        {
            int idx = cz * Width + cx;
            return new Vector3(
                _origin.x + (cx + 0.5f) * CellSize,
                _groundY[idx],
                _origin.z + (cz + 0.5f) * CellSize);
        }

        // 불통행 셀 → 외곽 링 방향으로 확장하며 가장 가까운 통행 셀 탐색
        private static void FindNearestWalkable(ref int cx, ref int cz)
        {
            for (int r = 1; r <= 10; r++)
            for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (Math.Abs(dx) < r && Math.Abs(dz) < r) continue; // 내부 스킵
                int nx = cx + dx, nz = cz + dz;
                if (nx < 0 || nx >= Width || nz < 0 || nz >= Height) continue;
                if (_walkable[nz * Width + nx]) { cx = nx; cz = nz; return; }
            }
        }

        // 경사각 → A* 비용 가중치
        // θ ≤ 20° : 1.0  / 20~35° : 선형 1.0→2.5  / 35~45° : 급증 2.5→12.5
        // Pass 1에서 >45° 셀은 이미 불통행 처리되므로 호출 시 deg ≤ MaxSlopeAngle 보장
        private static float SlopeCost(float deg)
        {
            if (deg <= 20f) return 1.0f;
            if (deg <= 35f) return 1.0f + (deg - 20f) / 15f * 1.5f;
            return 2.5f + (deg - 35f) / 10f * 10.0f;
        }

        // 옥틸 거리 휴리스틱 (8방향 A*에 최적, 수평 전용으로 허용 가능성 유지)
        private static float Heuristic(int x1, int z1, int x2, int z2)
        {
            int dx = x1 > x2 ? x1 - x2 : x2 - x1;
            int dz = z1 > z2 ? z1 - z2 : z2 - z1;
            int maxD = dx > dz ? dx : dz, minD = dx < dz ? dx : dz;
            return (float)maxD + 0.414f * (float)minD;
        }

        private static List<Vector3> BuildPath(int goalIdx)
        {
            // goal→start 순서로 셀 인덱스 수집
            var cells = new List<int>(32);
            for (int cur = goalIdx; cur >= 0; cur = _parent[cur])
                cells.Add(cur);

            // start→goal 순서로 채우되, 시작 셀(봇 현재 위치 = cells[Count-1])은 제외
            var path = new List<Vector3>(cells.Count - 1);
            for (int i = cells.Count - 2; i >= 0; i--)
                path.Add(CellToWorld(cells[i] % Width, cells[i] / Width));
            return path;
        }
    }
}
