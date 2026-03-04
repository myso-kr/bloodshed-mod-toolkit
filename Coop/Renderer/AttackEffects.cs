using System;
using UnityEngine;

namespace BloodshedModToolkit.Coop.Renderer
{
    /// <summary>지정 Delay 후 GameObject를 파괴.</summary>
    public class AutoDestroy : MonoBehaviour
    {
        public AutoDestroy(IntPtr ptr) : base(ptr) { }

        public float Delay = 2f;
        private float _elapsed;

        void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= Delay) UnityEngine.Object.Destroy(gameObject);
        }
    }

    /// <summary>총알 트레이서 이펙트 — 전진 후 0.3초 또는 50m에서 소멸.</summary>
    public class BulletTracerEffect : MonoBehaviour
    {
        public BulletTracerEffect(IntPtr ptr) : base(ptr) { }

        private Vector3 _dir;
        private float   _speed;
        private float   _elapsed;
        private Vector3 _startPos;
        private const float MaxTime = 0.3f;
        private const float MaxDist = 50f;

        public void Init(Vector3 dir, float speed)
        {
            _dir      = dir;
            _speed    = speed;
            _startPos = gameObject.transform.position;

            // URP 라인 렌더러 대신 가느다란 캡슐로 트레이서 표현 (폴백 포함)
            var trail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            if (trail != null)
            {
                trail.transform.SetParent(gameObject.transform, false);
                trail.transform.localScale    = new Vector3(0.03f, 0.25f, 0.03f);
                trail.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                RemoveCollider(trail);
                ApplyTrailColor(trail, new Color(1f, 0.95f, 0.7f));
            }

            // 총구 파티클 스폰
            EffectCache.TrySpawnPs(EffectCache.MuzzlePs, _startPos);
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            gameObject.transform.position = gameObject.transform.position + _dir * (_speed * Time.deltaTime);

            var dp = gameObject.transform.position - _startPos;
            float distSq = dp.x * dp.x + dp.y * dp.y + dp.z * dp.z;
            if (_elapsed >= MaxTime || distSq >= MaxDist * MaxDist)
                UnityEngine.Object.Destroy(gameObject);
        }

        private static void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private static void ApplyTrailColor(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.color = color;
                mr.material = mat;
            }
        }
    }

    /// <summary>절차적 폭발 이펙트 — 오렌지 구체 0→3m 팽창 후 소멸.</summary>
    public class ProceduralExplosion : MonoBehaviour
    {
        public ProceduralExplosion(IntPtr ptr) : base(ptr) { }

        private float   _elapsed;
        private const float Duration = 0.5f;
        private const float MaxScale = 3f;

        public void Init(Vector3 pos)
        {
            gameObject.transform.position = pos;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (sphere != null)
            {
                sphere.transform.SetParent(gameObject.transform, false);
                RemoveCollider(sphere);
                ApplyExplosionColor(sphere, new Color(1f, 0.55f, 0.05f, 0.85f));
            }
            gameObject.transform.localScale = Vector3.zero;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / Duration;
            if (t >= 1f) { UnityEngine.Object.Destroy(gameObject); return; }
            float s = MaxScale * t;
            gameObject.transform.localScale = new Vector3(s, s, s);
        }

        private static void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private static void ApplyExplosionColor(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.color = color;
                mr.material = mat;
            }
        }
    }

    /// <summary>발사체 이펙트 — 포물선 비행 후 폭발.</summary>
    public class LauncherProjectileEffect : MonoBehaviour
    {
        public LauncherProjectileEffect(IntPtr ptr) : base(ptr) { }

        private Vector3 _vel;
        private float   _elapsed;
        private const float Speed    = 18f;
        private const float Gravity  = -9.8f;
        private const float Lifetime = 2.5f;

        public void Init(Vector3 dir)
        {
            _vel = dir * Speed;

            // 발사체 시각화
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            if (capsule != null)
            {
                capsule.transform.SetParent(gameObject.transform, false);
                capsule.transform.localScale = new Vector3(0.06f, 0.12f, 0.06f);
                capsule.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var col = capsule.GetComponent<Collider>();
                if (col != null) col.enabled = false;
                ApplyProjectileColor(capsule, new Color(0.9f, 0.7f, 0.1f));
            }
        }

        void Update()
        {
            _elapsed += Time.deltaTime;

            // 포물선 운동
            _vel = _vel + new Vector3(0f, Gravity * Time.deltaTime, 0f);
            gameObject.transform.position = gameObject.transform.position + _vel * Time.deltaTime;

            // 발사체 방향 회전
            if (_vel.x != 0f || _vel.y != 0f || _vel.z != 0f)
                gameObject.transform.rotation = Quaternion.LookRotation(_vel);

            if (_elapsed >= Lifetime) Explode();
        }

        private void Explode()
        {
            var pos = gameObject.transform.position;

            // 절차적 폭발 구체
            var go = new GameObject("Explosion");
            var expl = go.AddComponent<ProceduralExplosion>();
            expl?.Init(pos);

            // 씬 파티클
            EffectCache.TrySpawnPs(EffectCache.ExplosionPs, pos);

            // 폭발음
            EffectCache.PlaySfx(EffectCache.SfxExplosion, pos);

            UnityEngine.Object.Destroy(gameObject);
        }

        private static void ApplyProjectileColor(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.color = color;
                mr.material = mat;
            }
        }
    }
}
