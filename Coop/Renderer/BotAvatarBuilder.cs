using System;
using UnityEngine;
using com8com1.SCFPS;
using Enemies.EnemyAi;

namespace BloodshedModToolkit.Coop.Renderer
{
    /// <summary>
    /// 봇 아바타 비주얼을 세 단계로 구성한다.
    /// 1차: 플레이어 SkinnedMesh 복제 (카메라면 즉시 포기)
    /// 2차: 씬의 적 엔티티 모델 복제
    /// 3차: 프리미티브 기반 프로시저럴 인체
    /// </summary>
    public static class BotAvatarBuilder
    {
        // ── 1차: 플레이어 모델 ────────────────────────────────────────────────
        public static bool TryClonePlayerModel(GameObject root, BotAvatarAnimator anim)
        {
            try
            {
                var player = UnityEngine.Object.FindObjectOfType<Q3PlayerController>();
                if (player == null) { Log("Q3PlayerController 없음"); return false; }

                var smrs = player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (smrs == null || smrs.Length == 0) { Log("플레이어 SMR 없음"); return false; }

                var vr = FindVisualRootFromSMR(smrs[0].transform, player.transform);
                if (vr == null) { Log("플레이어 visualRoot 탐색 실패"); return false; }
                if (vr.GetComponent<Camera>() != null)
                {
                    Log($"visualRoot={vr.gameObject.name}이 Camera — FPS 전용 게임, 바디 없음");
                    return false;
                }

                return AttachClone(vr.gameObject, root, anim, "플레이어");
            }
            catch (Exception ex) { Log($"플레이어 복제 예외: {ex.Message}"); return false; }
        }

        // ── 2차: 적 엔티티 모델 (인간형만) ─────────────────────────────────
        public static bool TryCloneEnemyModel(GameObject root, BotAvatarAnimator anim)
        {
            try
            {
                var healths = UnityEngine.Object.FindObjectsOfType<Health>();
                if (healths == null || healths.Length == 0) { Log("씬에 적 없음"); return false; }

                // 모든 살아있는 적을 스캔, 인간형(isHuman) 우선 선택
                GameObject? bestGo = null;
                Transform?  bestVR = null;
                bool        bestIsHuman = false;

                foreach (var h in healths)
                {
                    if (h == null || h.isPlayer || h.currentHealth <= 0) continue;
                    var vr = FindVisualRoot(h.gameObject);
                    if (vr == null) continue;

                    // Animator.isHuman 으로 인간형 판별
                    bool isHuman = false;
                    var animator = h.gameObject.GetComponentInChildren<Animator>();
                    if (animator != null) isHuman = animator.isHuman;

                    Log($"[진단] 적 후보: name={h.gameObject.name} " +
                        $"visualRoot={vr.gameObject.name} isHuman={isHuman}");

                    if (bestGo == null || (!bestIsHuman && isHuman))
                    {
                        bestGo      = h.gameObject;
                        bestVR      = vr;
                        bestIsHuman = isHuman;
                    }
                }

                if (bestVR == null) { Log("살아있는 적 없음"); return false; }

                // 인간형 없으면 거미/비인간형 사용 거부 → 프로시저럴 폴백
                if (!bestIsHuman)
                {
                    Log($"[경고] 인간형 적 없음 (최선: {bestGo!.name}) — 프로시저럴 폴백");
                    return false;
                }

                Log($"인간형 적 선택: {bestGo!.name} visualRoot={bestVR.gameObject.name}");

                if (!AttachClone(bestVR.gameObject, root, anim, $"적({bestVR.gameObject.name})"))
                    return false;

                foreach (var h in root.GetComponentsInChildren<Health>(true))
                    h.enabled = false;
                foreach (var e in root.GetComponentsInChildren<EnemyAbilityController>(true))
                    e.enabled = false;

                return true;
            }
            catch (Exception ex) { Log($"적 복제 예외: {ex.Message}"); return false; }
        }

        // ── 3차: 프로시저럴 인체 ─────────────────────────────────────────────
        public static void BuildProcedural(GameObject root, Color color, BotAvatarAnimator anim)
        {
            var darkColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);

            Prim(root, "Head", PrimitiveType.Sphere,
                new Vector3(0f, 0.78f, 0f), new Vector3(0.24f, 0.24f, 0.24f), color);

            anim.Torso = Prim(root, "Torso", PrimitiveType.Capsule,
                new Vector3(0f, 0.35f, 0f), new Vector3(0.38f, 0.27f, 0.30f), color).transform;

            Prim(root, "Hips", PrimitiveType.Sphere,
                new Vector3(0f, 0.03f, 0f), new Vector3(0.30f, 0.20f, 0.24f), color);

            var hipL    = Pivot(root, "HipPivotL",    new Vector3(-0.10f, 0.02f, 0f));
            var thighL  = Prim(hipL, "ThighL", PrimitiveType.Capsule,
                new Vector3(0f, -0.225f, 0f), new Vector3(0.15f, 0.225f, 0.15f), darkColor);
            anim.ThighL = hipL.transform;
            var kneeL   = Pivot(thighL, "KneePivotL", new Vector3(0f, -0.45f, 0f));
            Prim(kneeL, "ShinL", PrimitiveType.Capsule,
                new Vector3(0f, -0.225f, 0f), new Vector3(0.13f, 0.225f, 0.13f), darkColor);
            anim.ShinL  = kneeL.transform;

            var hipR    = Pivot(root, "HipPivotR",    new Vector3(+0.10f, 0.02f, 0f));
            var thighR  = Prim(hipR, "ThighR", PrimitiveType.Capsule,
                new Vector3(0f, -0.225f, 0f), new Vector3(0.15f, 0.225f, 0.15f), darkColor);
            anim.ThighR = hipR.transform;
            var kneeR   = Pivot(thighR, "KneePivotR", new Vector3(0f, -0.45f, 0f));
            Prim(kneeR, "ShinR", PrimitiveType.Capsule,
                new Vector3(0f, -0.225f, 0f), new Vector3(0.13f, 0.225f, 0.13f), darkColor);
            anim.ShinR  = kneeR.transform;

            var shL  = Pivot(root, "ShoulderPivotL", new Vector3(-0.24f, 0.56f, 0f));
            Prim(shL, "UpperArmL", PrimitiveType.Capsule,
                new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.15f, 0.13f), darkColor);
            anim.ShoulderL = shL.transform;
            var elL  = Pivot(shL, "ElbowPivotL", new Vector3(0f, -0.30f, 0f));
            Prim(elL, "ForearmL", PrimitiveType.Capsule,
                new Vector3(0f, -0.13f, 0f), new Vector3(0.11f, 0.13f, 0.11f), darkColor);

            var shR  = Pivot(root, "ShoulderPivotR", new Vector3(+0.24f, 0.56f, 0f));
            Prim(shR, "UpperArmR", PrimitiveType.Capsule,
                new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.15f, 0.13f), darkColor);
            anim.ShoulderR = shR.transform;
            var elR  = Pivot(shR, "ElbowPivotR", new Vector3(0f, -0.30f, 0f));
            Prim(elR, "ForearmR", PrimitiveType.Capsule,
                new Vector3(0f, -0.13f, 0f), new Vector3(0.11f, 0.13f, 0.11f), darkColor);
            anim.ForearmR = elR.transform;

            var wGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wGo.name = "WeaponVisual";
            wGo.transform.SetParent(elR.transform);
            wGo.transform.localPosition = new Vector3(0.04f, -0.28f, 0.12f);
            wGo.transform.localScale    = new Vector3(0.06f, 0.06f, 0.45f);
            ApplyMat(wGo, new Color(0.3f, 0.3f, 0.35f));
            DisableCollider(wGo);

            anim.Mode = BotAvatarAnimator.AvatarMode.Procedural;
            Log("프로시저럴 인체 구성 완료");
        }

        // ── 공통: 시각 서브루트 탐색 ─────────────────────────────────────────
        /// <summary>source의 자식 중 SMR→MeshRenderer 순으로 시각 서브루트를 반환. Camera면 null.</summary>
        private static Transform? FindVisualRoot(GameObject source)
        {
            var smrs = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (smrs != null && smrs.Length > 0)
            {
                var vr = FindVisualRootFromSMR(smrs[0].transform, source.transform);
                if (vr != null && vr.GetComponent<Camera>() == null) return vr;
            }
            var mrs = source.GetComponentsInChildren<MeshRenderer>(true);
            if (mrs != null && mrs.Length > 0)
            {
                var vr = mrs[0].transform;
                while (vr.parent != null && vr.parent != source.transform)
                    vr = vr.parent;
                if (vr.GetComponent<Camera>() == null) return vr;
            }
            return null;
        }

        private static Transform? FindVisualRootFromSMR(Transform smrTr, Transform root)
        {
            var vr = smrTr;
            while (vr.parent != null && vr.parent != root)
                vr = vr.parent;
            return vr;
        }

        // ── 공통: 복제 + 활성화 + Renderer 설정 + Animator 연결 ─────────────
        private static bool AttachClone(GameObject source, GameObject root,
            BotAvatarAnimator anim, string label)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            if (clone == null) { Log($"{label} Instantiate 실패"); return false; }

            clone.transform.SetParent(root.transform);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale    = new Vector3(1f, 1f, 1f);

            // 전체 활성화 (SetActive(false) 상태 해제)
            clone.SetActive(true);
            foreach (var tr in clone.GetComponentsInChildren<Transform>(true))
                tr.gameObject.SetActive(true);

            // Collider 비활성화
            foreach (var col in clone.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            // Renderer 강제 활성화 + Default 레이어(0)
            int cnt = 0;
            foreach (var r in clone.GetComponentsInChildren<UnityEngine.Renderer>(true))
            {
                r.enabled = true;
                r.gameObject.layer = 0;
                cnt++;
            }

            var animator = clone.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                anim.InitGameModel(animator);
                anim.Mode = BotAvatarAnimator.AvatarMode.GameModel;
                Log($"{label} 복제 성공 — 렌더러 {cnt}개, Animator 연결");
            }
            else
            {
                anim.Mode = BotAvatarAnimator.AvatarMode.GameModel;
                Log($"{label} 복제 성공 — 렌더러 {cnt}개 (Animator 없음)");
            }
            return true;
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────────────
        private static GameObject Pivot(GameObject parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = new Vector3(1f, 1f, 1f);
            return go;
        }

        private static GameObject Prim(GameObject parent, string name, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            go.transform.localRotation = Quaternion.identity;
            ApplyMat(go, color);
            DisableCollider(go);
            return go;
        }

        private static void ApplyMat(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader != null) { var m = new Material(shader); m.SetColor("_BaseColor", color); mr.material = m; }
        }

        private static void DisableCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private static void Log(string msg)
            => Plugin.Log.LogInfo($"[BotAvatarBuilder] {msg}");
    }
}
