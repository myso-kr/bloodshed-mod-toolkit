using System;
using UnityEngine;
using BloodshedModToolkit.Coop.Bots;

namespace BloodshedModToolkit.Coop.Renderer
{
    /// <summary>
    /// DOOM-스타일 저폴리곤 봇 아바타.
    /// 큐브 프리미티브 + 8×8 픽셀 텍스처 (FilterMode.Point).
    /// </summary>
    public static class BotAvatarBuilder
    {
        // ── 8×8 패턴 정의 (시각적 위→아래, 왼→오른) ─────────────────────────
        // 팔레트: 0=기본, 1=중간(×0.60), 2=어두움(×0.30), 3=밝음(+0.35)

        // 헬멧: 바이저 줄 + 얼굴판
        static readonly byte[] PatHead = {
            2,2,2,2,2,2,2,2,
            2,1,1,1,1,1,1,2,
            2,1,3,3,3,3,1,2,  // 바이저 하이라이트
            2,1,2,2,2,2,1,2,  // 눈 슬롯 (어둠)
            2,1,2,2,2,2,1,2,
            2,1,0,0,0,0,1,2,  // 얼굴판
            2,1,1,1,1,1,1,2,
            2,2,2,2,2,2,2,2,
        };

        // 갑옷 흉판: 세로 분할선
        static readonly byte[] PatTorso = {
            2,2,2,2,2,2,2,2,
            2,3,3,3,3,3,3,2,  // 상단 하이라이트
            2,0,2,0,0,2,0,2,  // 갑옷 선
            2,0,0,0,0,0,0,2,
            2,0,0,0,0,0,0,2,
            2,0,2,0,0,2,0,2,  // 갑옷 선
            2,1,1,1,1,1,1,2,  // 하단 그림자
            2,2,2,2,2,2,2,2,
        };

        // 관절/사지 세그먼트: 상하 조인트 강조
        static readonly byte[] PatLimb = {
            2,2,2,2,2,2,2,2,  // 조인트 상단
            2,0,0,0,0,0,0,2,
            2,0,3,3,3,3,0,2,  // 하이라이트 줄
            2,0,0,0,0,0,0,2,
            2,0,0,0,0,0,0,2,
            2,0,1,1,1,1,0,2,  // 그림자 줄
            2,0,0,0,0,0,0,2,
            2,2,2,2,2,2,2,2,  // 조인트 하단
        };

        // 허리: 벨트 라인
        static readonly byte[] PatHips = {
            2,2,2,2,2,2,2,2,
            2,1,1,1,1,1,1,2,
            2,1,0,0,0,0,1,2,
            2,0,0,2,2,0,0,2,  // 벨트
            2,0,0,2,2,0,0,2,
            2,1,0,0,0,0,1,2,
            2,1,1,1,1,1,1,2,
            2,2,2,2,2,2,2,2,
        };

        // ── 프로시저럴 인체 빌드 ─────────────────────────────────────────────
        public static void BuildProcedural(GameObject root, Color color,
            BotAvatarAnimator anim, WeaponClass wc = WeaponClass.Melee)
        {
            var texHead  = MakeTex8(color, PatHead);
            var texTorso = MakeTex8(color, PatTorso);
            var texLimb  = MakeTex8(color, PatLimb);
            var texHips  = MakeTex8(color, PatHips);

            // 머리 (Cube)
            Prim(root, "Head", PrimitiveType.Cube,
                new Vector3(0f, 0.78f, 0f),
                new Vector3(0.34f, 0.30f, 0.30f), texHead);

            // 몸통 (Cube)
            anim.Torso = Prim(root, "Torso", PrimitiveType.Cube,
                new Vector3(0f, 0.35f, 0f),
                new Vector3(0.50f, 0.54f, 0.36f), texTorso).transform;

            // 허리 (Cube, 납작)
            Prim(root, "Hips", PrimitiveType.Cube,
                new Vector3(0f, 0.03f, 0f),
                new Vector3(0.44f, 0.16f, 0.36f), texHips);

            // ── 다리 ────────────────────────────────────────────────────────
            var hipL   = Pivot(root,   "HipPivotL",  new Vector3(-0.13f, 0.02f, 0f));
            var thighL = Prim(hipL,    "ThighL", PrimitiveType.Cube,
                new Vector3(0f, -0.225f, 0f), new Vector3(0.26f, 0.45f, 0.26f), texLimb);
            anim.ThighL = hipL.transform;

            var kneeL  = Pivot(thighL, "KneePivotL", new Vector3(0f, -0.45f, 0f));
            Prim(kneeL, "ShinL", PrimitiveType.Cube,
                new Vector3(0f, -0.225f, 0f), new Vector3(0.22f, 0.45f, 0.22f), texLimb);
            anim.ShinL = kneeL.transform;

            var hipR   = Pivot(root,   "HipPivotR",  new Vector3(+0.13f, 0.02f, 0f));
            var thighR = Prim(hipR,    "ThighR", PrimitiveType.Cube,
                new Vector3(0f, -0.225f, 0f), new Vector3(0.26f, 0.45f, 0.26f), texLimb);
            anim.ThighR = hipR.transform;

            var kneeR  = Pivot(thighR, "KneePivotR", new Vector3(0f, -0.45f, 0f));
            Prim(kneeR, "ShinR", PrimitiveType.Cube,
                new Vector3(0f, -0.225f, 0f), new Vector3(0.22f, 0.45f, 0.22f), texLimb);
            anim.ShinR = kneeR.transform;

            // ── 팔 ──────────────────────────────────────────────────────────
            var shL  = Pivot(root, "ShoulderPivotL", new Vector3(-0.30f, 0.54f, 0f));
            Prim(shL, "UpperArmL", PrimitiveType.Cube,
                new Vector3(0f, -0.15f, 0f), new Vector3(0.22f, 0.30f, 0.22f), texLimb);
            anim.ShoulderL = shL.transform;

            var elL  = Pivot(shL, "ElbowPivotL", new Vector3(0f, -0.30f, 0f));
            Prim(elL, "ForearmL", PrimitiveType.Cube,
                new Vector3(0f, -0.13f, 0f), new Vector3(0.18f, 0.26f, 0.18f), texLimb);
            anim.ElbowL = elL.transform;

            var shR  = Pivot(root, "ShoulderPivotR", new Vector3(+0.30f, 0.54f, 0f));
            Prim(shR, "UpperArmR", PrimitiveType.Cube,
                new Vector3(0f, -0.15f, 0f), new Vector3(0.22f, 0.30f, 0.22f), texLimb);
            anim.ShoulderR = shR.transform;

            var elR  = Pivot(shR, "ElbowPivotR", new Vector3(0f, -0.30f, 0f));
            Prim(elR, "ForearmR", PrimitiveType.Cube,
                new Vector3(0f, -0.13f, 0f), new Vector3(0.18f, 0.26f, 0.18f), texLimb);
            anim.ForearmR = elR.transform;

            // 무기 (WeaponClass별)
            BuildWeapon(elR, wc, anim);

            anim.Mode = BotAvatarAnimator.AvatarMode.Procedural;
            Log("DOOM-스타일 프로시저럴 인체 구성 완료");
        }

        // ── 무기 빌드 ────────────────────────────────────────────────────────
        static void BuildWeapon(GameObject elbowR, WeaponClass wc, BotAvatarAnimator anim)
        {
            var t = MakeWeaponTex32(wc);
            switch (wc)
            {
                case WeaponClass.Melee:
                    Prim(elbowR, "Blade", PrimitiveType.Cube,
                        new Vector3(0.04f, -0.28f, 0.06f), new Vector3(0.04f, 0.38f, 0.05f), t);
                    Prim(elbowR, "Guard", PrimitiveType.Cube,
                        new Vector3(0.04f, -0.25f, 0.06f), new Vector3(0.14f, 0.04f, 0.05f), t);
                    break;
                case WeaponClass.Pistol:
                    Prim(elbowR, "PistolBody", PrimitiveType.Cube,
                        new Vector3(0.04f, -0.30f, 0.10f), new Vector3(0.06f, 0.10f, 0.22f), t);
                    Prim(elbowR, "PistolGrip", PrimitiveType.Cube,
                        new Vector3(0.04f, -0.37f, 0.05f), new Vector3(0.05f, 0.12f, 0.05f), t);
                    break;
                case WeaponClass.Rifle:
                    Prim(elbowR, "RifleBarrel", PrimitiveType.Cube,
                        new Vector3(0.04f, -0.28f, 0.16f), new Vector3(0.06f, 0.06f, 0.52f), t);
                    Prim(elbowR, "RifleStock", PrimitiveType.Cube,
                        new Vector3(0.04f, -0.30f, -0.04f), new Vector3(0.05f, 0.08f, 0.12f), t);
                    break;
                case WeaponClass.Launcher:
                    Prim(elbowR, "LauncherTube", PrimitiveType.Cube,
                        new Vector3(0.04f, -0.26f, 0.16f), new Vector3(0.14f, 0.14f, 0.58f), t);
                    Prim(elbowR, "LauncherGrip", PrimitiveType.Cube,
                        new Vector3(0.04f, -0.38f, 0.10f), new Vector3(0.05f, 0.14f, 0.07f), t);
                    break;
            }
        }

        // ── 무기 32×32 절차적 텍스처 (게임 분위기 기반) ──────────────────────

        // 결정론적 해시 노이즈 (0~1 float)
        static float H(int x, int y, int s = 0)
        {
            uint n = (uint)x * 374761393u ^ (uint)y * 1103515245u ^ (uint)s * 2246822519u;
            n = (n ^ (n >> 13)) * 1274126177u;
            return (n & 0xFFFFu) / 65535f;
        }

        static Texture2D MakeWeaponTex32(WeaponClass wc)
        {
            var tex = new Texture2D(32, 32);
            tex.filterMode = FilterMode.Point;
            var px = new Color[1024];
            // row=0 = 텍스처 하단(Unity), y=0 = 이미지 상단(디자인)
            for (int row = 0; row < 32; row++)
            for (int col = 0; col < 32; col++)
            {
                int x = col, y = 31 - row;
                px[row * 32 + col] = wc switch {
                    WeaponClass.Melee    => MeleePx(x, y),
                    WeaponClass.Pistol   => PistolPx(x, y),
                    WeaponClass.Rifle    => RiflePx(x, y),
                    WeaponClass.Launcher => LauncherPx(x, y),
                    _                    => new Color(0.2f, 0.2f, 0.2f),
                };
            }
            tex.SetPixels(px);
            tex.Apply(false);
            return tex;
        }

        // Melee: 피 묻은 은빛 검날 — 날 엣지 하이라이트, 풀러 홈, 혈흔
        static Color MeleePx(int x, int y)
        {
            Color c = new Color(0.72f, 0.72f, 0.78f);
            if (x < 2 || x > 29 || y < 2 || y > 29)
                return new Color(c.r * 0.12f, c.g * 0.12f, c.b * 0.16f);

            // 날 엣지 하이라이트 (왼쪽 = 날카로운 쪽)
            float edge = x < 7 ? (7 - x) / 7f * 0.55f : 0f;
            // 풀러 홈 (세로 중심 미세 어둠)
            float fuller = (x >= 13 && x <= 17) ? -0.18f : 0f;
            // 세로 광량: 위 밝고 아래 약간 어두움
            float vLight = 1f - (y / 31f) * 0.18f;
            float noise  = H(x, y) * 0.10f - 0.05f;
            // 대각 스크래치 (전투 흔적)
            bool scratch = ((x + y) % 19 == 0 || (x * 2 + y) % 31 == 0) && H(x, y, 3) > 0.35f;
            // 혈흔 (날 하단부, 불규칙 얼룩)
            float bx = x - 14f, by = y - 26f;
            bool blood = y > 20
                && (float)Math.Sqrt(bx * bx + by * by) < 5f + H(x, y, 2) * 4f
                && H(x, y, 1) > 0.42f;

            float light = Math.Min(Math.Max(vLight + edge + fuller + noise, 0.15f), 1.55f);
            Color col = new Color(
                Math.Min(c.r * light, 1f),
                Math.Min(c.g * light, 1f),
                Math.Min(c.b * light * 1.03f, 1f));  // 강철 청빛 미세 강조
            if (scratch) col = new Color(col.r * 0.52f, col.g * 0.52f, col.b * 0.58f);
            if (blood)   col = Color.Lerp(col, new Color(0.40f, 0.04f, 0.04f),
                                          0.62f + H(x, y, 4) * 0.28f);
            return col;
        }

        // Pistol: 블루-스틸 권총 — 슬라이드 세레이션, 그립 스티플링, 이젝션포트, 보어
        static Color PistolPx(int x, int y)
        {
            Color c = new Color(0.25f, 0.25f, 0.28f);
            if (x < 1 || x > 30 || y < 1 || y > 30)
                return new Color(0.03f, 0.03f, 0.04f);

            float topEdge  = y < 5  ? (5  - y) / 5f  * 0.35f : 0f;
            float leftEdge = x < 4  ? (4  - x) / 4f  * 0.20f : 0f;
            // 슬라이드 세레이션 (전술적 수직 홈)
            bool serration = y < 13 && x >= 7 && x <= 26 && (x % 3 == 0);
            // 그립 스티플링 (마름모 격자)
            bool stipple   = y > 19 && ((x + y) % 2 == 0) && x >= 5 && x <= 27;
            // 이젝션 포트 (슬라이드 우측 어두운 절개)
            bool ejPort    = x >= 19 && x <= 28 && y >= 6 && y <= 11;
            // 배럴 보어
            bool bore      = (float)Math.Sqrt((x - 4f) * (x - 4f) + (y - 15f) * (y - 15f)) < 3f;
            // 슬라이드-프레임 분할선
            bool divLine   = y == 15 && x >= 4 && x <= 28;

            float noise = H(x, y) * 0.07f - 0.035f;
            float light = 0.82f + topEdge + leftEdge + noise;
            Color col = new Color(
                Math.Min(c.r * light, 1f),
                Math.Min(c.g * light, 1f),
                Math.Min(c.b * light * 1.08f, 1f));  // 블루-스틸 색조
            if (divLine)   col = new Color(col.r * 0.40f, col.g * 0.40f, col.b * 0.45f);
            if (serration) col = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.50f);
            if (stipple)   col = new Color(col.r * 0.65f, col.g * 0.65f, col.b * 0.70f);
            if (ejPort)    col = new Color(col.r * 0.28f, col.g * 0.28f, col.b * 0.32f);
            if (bore)      col = new Color(0.02f, 0.02f, 0.03f);
            return col;
        }

        // Rifle: 매트 군용 소총 — 피카티니 레일, 탄창 웰, 긁힘, 도료 벗겨짐
        static Color RiflePx(int x, int y)
        {
            Color c = new Color(0.20f, 0.20f, 0.22f);
            if (x < 1 || x > 30 || y < 1 || y > 30)
                return new Color(0.02f, 0.02f, 0.03f);

            bool barrelZone   = y < 9;
            bool receiverZone = y >= 9 && y <= 21;
            bool stockZone    = y > 21;
            // 피카티니 레일 슬롯 (배럴 최상단)
            bool rail     = barrelZone && y < 4 && x >= 3 && x <= 29;
            bool railSlot = rail && ((x - 3) % 5 == 0);
            // 탄창 웰
            bool magWell = receiverZone && x >= 11 && x <= 21 && y >= 16 && y <= 21;
            // 사용 스크래치
            bool scratch = barrelZone && ((x + y * 2) % 13 == 0) && H(x, y, 5) > 0.45f;
            // 개머리판 질감 (어두운 폴리머)
            bool grain = stockZone && ((y + x / 4) % 3 == 0) && H(x, y, 6) > 0.55f;
            // 도료 벗겨짐 (금속 엣지)
            bool chip = (x < 3 || x > 28) && H(x, y, 7) > 0.55f;

            float vLight = 1f - (y / 31f) * 0.22f;
            float matte  = barrelZone ? 0.78f : receiverZone ? 0.72f : 0.68f;
            float noise  = H(x, y) * 0.08f - 0.04f;
            float light  = (vLight + noise) * matte;
            Color col = new Color(
                Math.Min(c.r * light, 1f),
                Math.Min(c.g * light, 1f),
                Math.Min(c.b * light, 1f));
            if (railSlot) col = new Color(0.04f, 0.04f, 0.05f);
            if (magWell)  col = new Color(0.02f, 0.02f, 0.03f);
            if (scratch)  col = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.48f);
            if (grain)    col = Color.Lerp(col, new Color(0.18f, 0.14f, 0.10f), 0.40f);
            if (chip)     col = Color.Lerp(col, new Color(0.38f, 0.35f, 0.30f), 0.55f);
            return col;
        }

        // Launcher: 군용 로켓 런처 — 원통형 명암, 위험 경고 밴드, 그립 랩, 후연 흔적
        static Color LauncherPx(int x, int y)
        {
            Color c = new Color(0.25f, 0.30f, 0.15f);
            if (x < 1 || x > 30 || y < 1 || y > 30)
                return new Color(0.04f, 0.05f, 0.02f);

            // 원통형 좌우 명암
            float cylLight = 1f - Math.Abs(x - 15.5f) / 16f * 0.45f;
            // 전면 보어 (상단 원형)
            bool bore = (float)Math.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 4f) * (y - 4f)) < 4.5f;
            // 위험 경고 밴드 (흑황 사선 스트라이프)
            bool warnBand   = y >= 6 && y <= 10;
            bool warnBlack  = warnBand && ((x + y) / 3 % 2 == 0);
            bool warnYellow = warnBand && !warnBlack;
            // 그립 랩 (하단 중앙 가로 띠)
            bool gripWrap = y >= 20 && y <= 29 && x >= 7 && x <= 25 && (y % 3 == 0);
            // 도료 마모 (엣지 반짝임)
            bool wear = (x < 4 || x > 27) && H(x, y, 9) > 0.60f;
            // 후연·그을음 (발사 후미)
            bool burn = y > 25
                && (float)Math.Sqrt((x - 15f) * (x - 15f) + (y - 28f) * (y - 28f)) < 5f
                && H(x, y, 10) > 0.62f;

            float noise = H(x, y) * 0.09f - 0.045f;
            float light = Math.Max(Math.Min(cylLight + noise, 1.30f), 0.20f);
            Color col = new Color(
                Math.Min(c.r * light, 1f),
                Math.Min(c.g * light, 1f),
                Math.Min(c.b * light, 1f));
            if (bore)       col = new Color(0.03f, 0.04f, 0.02f);
            if (warnYellow) col = new Color(0.90f, 0.78f, 0.05f);
            if (warnBlack)  col = new Color(0.05f, 0.05f, 0.04f);
            if (gripWrap)   col = new Color(col.r * 0.42f, col.g * 0.42f, col.b * 0.42f);
            if (wear)       col = Color.Lerp(col, new Color(0.40f, 0.38f, 0.28f), 0.55f);
            if (burn)       col = Color.Lerp(col, new Color(0.10f, 0.08f, 0.05f), 0.72f);
            return col;
        }

        // ── 8×8 픽셀 텍스처 생성 ─────────────────────────────────────────────
        static Texture2D MakeTex8(Color c, byte[] pat)
        {
            // 4색 팔레트
            Color[] pal = {
                c,
                new Color(c.r * 0.60f, c.g * 0.60f, c.b * 0.60f),
                new Color(c.r * 0.30f, c.g * 0.30f, c.b * 0.30f),
                new Color(Math.Min(c.r + 0.35f, 1f),
                          Math.Min(c.g + 0.35f, 1f),
                          Math.Min(c.b + 0.35f, 1f)),
            };
            var tex = new Texture2D(8, 8);
            tex.filterMode = FilterMode.Point;  // 도트 질감 (픽셀 보간 없음)
            var pixels = new Color[64];
            // Unity SetPixels: row 0 = 텍스처 하단 → 패턴 row 7이 pixels row 0에 대응
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                    pixels[row * 8 + col] = pal[pat[(7 - row) * 8 + col]];
            tex.SetPixels(pixels);
            tex.Apply(false);
            return tex;
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────────────
        static GameObject Pivot(GameObject parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = new Vector3(1f, 1f, 1f);
            return go;
        }

        static GameObject Prim(GameObject parent, string name, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Texture2D tex)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            go.transform.localRotation = Quaternion.identity;
            ApplyTex(go, tex);
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            return go;
        }

        static void ApplyTex(GameObject go, Texture2D tex)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) return;
            var m = new Material(shader);
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            mr.material = m;
        }

        static void Log(string msg)
            => Plugin.Log.LogInfo($"[BotAvatarBuilder] {msg}");
    }
}
