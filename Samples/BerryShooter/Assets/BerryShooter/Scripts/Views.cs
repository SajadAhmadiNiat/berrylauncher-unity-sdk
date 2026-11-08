using System;
using System.Collections.Generic;
using UnityEngine;

namespace BerryShooter
{
    public static class ArenaFactory
    {
        public const string RootName = "Arena";

        public static Transform Build(Transform parent, Func<Color, bool, Material> material, bool withLight)
        {
            var root = new GameObject(RootName).transform;
            root.SetParent(parent, false);
            Box(root, "Floor", new Vector3(0, -0.2f, 0), new Vector3(ArenaLayout.Half * 2f, 0.4f, ArenaLayout.Half * 2f), new Color(0.16f, 0.18f, 0.22f), material);
            Color wall = new Color(0.1f, 0.11f, 0.15f);
            float s = ArenaLayout.Half;
            Box(root, "Wall North", new Vector3(0, 1.5f, s + 0.5f), new Vector3(s * 2 + 2, 3, 1), wall, material);
            Box(root, "Wall South", new Vector3(0, 1.5f, -s - 0.5f), new Vector3(s * 2 + 2, 3, 1), wall, material);
            Box(root, "Wall East", new Vector3(s + 0.5f, 1.5f, 0), new Vector3(1, 3, s * 2), wall, material);
            Box(root, "Wall West", new Vector3(-s - 0.5f, 1.5f, 0), new Vector3(1, 3, s * 2), wall, material);
            for (int i = 0; i < ArenaLayout.Blocks.Length; i++)
            {
                Block b = ArenaLayout.Blocks[i];
                Color c = i == 4 ? new Color(0.9f, 0.2f, 0.4f) : new Color(0.28f, 0.34f, 0.5f);
                Box(root, "Cover " + i, new Vector3(b.X, b.Height * 0.5f, b.Z), new Vector3(b.Hx * 2, b.Height, b.Hz * 2), c, material);
            }
            Color grid = new Color(0.22f, 0.25f, 0.32f);
            for (int i = -(int)s; i <= (int)s; i += 4)
            {
                Box(root, "Grid X " + i, new Vector3(i, 0.01f, 0), new Vector3(0.05f, 0.02f, s * 2), grid, material);
                Box(root, "Grid Z " + i, new Vector3(0, 0.012f, i), new Vector3(s * 2, 0.02f, 0.05f), grid, material);
            }
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = ArenaLayout.SpawnPoint(i);
                var sp = new GameObject("Spawn " + i).transform;
                sp.SetParent(root, false);
                sp.localPosition = p;
            }
            if (withLight)
            {
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.1f;
                sun.transform.rotation = Quaternion.Euler(55, 30, 0);
                sun.transform.SetParent(root, false);
            }
            return root;
        }

        private static void Box(Transform root, string name, Vector3 pos, Vector3 scale, Color c, Func<Color, bool, Material> material)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(root, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            var col = g.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            g.GetComponent<Renderer>().sharedMaterial = material(c, false);
        }
    }

    public sealed class PlayerView : MonoBehaviour
    {
        public int Id;
        private Transform body, gun;
        private Renderer bodyRenderer;
        private Vector3 target;
        private float yawTarget;
        private bool alive = true, local;
        private Color shown = Color.clear;

        public Vector3 HeadPos { get { return transform.position + Vector3.up * 2.15f; } }
        public bool Visible { get { return !local; } }
        public bool Alive { get { return alive; } }

        public static PlayerView Create(PlayerState p, bool isLocal)
        {
            var go = new GameObject("Player " + p.Id);
            var v = go.AddComponent<PlayerView>();
            v.Id = p.Id;
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cap.transform.SetParent(go.transform, false);
            cap.transform.localPosition = new Vector3(0, 0.95f, 0);
            cap.transform.localScale = new Vector3(0.9f, 0.95f, 0.9f);
            Destroy(cap.GetComponent<Collider>());
            v.body = cap.transform;
            v.bodyRenderer = cap.GetComponent<Renderer>();
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.transform.SetParent(go.transform, false);
            g.transform.localScale = new Vector3(0.14f, 0.14f, 0.7f);
            g.transform.localPosition = new Vector3(0.36f, 1.25f, 0.45f);
            Destroy(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial = Mats.Solid(new Color(0.12f, 0.12f, 0.14f));
            v.gun = g.transform;
            var eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eye.transform.SetParent(go.transform, false);
            eye.transform.localScale = new Vector3(0.5f, 0.14f, 0.12f);
            eye.transform.localPosition = new Vector3(0, 1.55f, 0.4f);
            Destroy(eye.GetComponent<Collider>());
            eye.GetComponent<Renderer>().sharedMaterial = Mats.Glow(new Color(0.7f, 0.95f, 1f));
            v.local = isLocal;
            v.target = p.Pos;
            go.transform.position = p.Pos;
            v.Apply(p, isLocal);
            return v;
        }

        public void Apply(PlayerState p, bool isLocal)
        {
            local = isLocal;
            target = p.Pos;
            yawTarget = p.Yaw;
            alive = p.Alive;
            Color c;
            ColorUtility.TryParseHtmlString(BerrySdk.BerryUtil.TierColorHex(p.Tier), out c);
            if (c.a <= 0f) c = new Color(0.6f, 0.65f, 0.75f);
            if (c != shown) { shown = c; bodyRenderer.sharedMaterial = Mats.Solid(c, 0.4f); }
            bool show = !isLocal && p.Alive;
            body.gameObject.SetActive(show);
            gun.gameObject.SetActive(show);
            foreach (Transform t in transform) if (t != body && t != gun) t.gameObject.SetActive(show);
        }

        private void Update()
        {
            float k = 1f - Mathf.Exp(-18f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yawTarget, 0), k);
        }
    }

    public sealed class EnemyView : MonoBehaviour
    {
        public int Id;
        private Vector3 target;
        private float yawTarget;
        private Transform bar;
        private float height;

        public static EnemyView Create(EnemyState e)
        {
            var go = new GameObject("Enemy " + e.Id);
            var v = go.AddComponent<EnemyView>();
            v.Id = e.Id;
            float h = EnemyState.HeightFor(e.Type), r = EnemyState.RadiusFor(e.Type);
            v.height = h;
            Color col = e.Type == 1 ? new Color(0.55f, 0.1f, 0.12f) : e.Type == 2 ? new Color(0.85f, 0.45f, 0.1f) : new Color(0.85f, 0.15f, 0.25f);
            PrimitiveType shape = e.Type == 2 ? PrimitiveType.Capsule : PrimitiveType.Cube;
            var body = GameObject.CreatePrimitive(shape);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, h * 0.5f, 0);
            body.transform.localScale = e.Type == 2 ? new Vector3(r * 1.5f, h * 0.5f, r * 1.5f) : new Vector3(r * 1.7f, h, r * 1.7f);
            body.GetComponent<Renderer>().sharedMaterial = Mats.Solid(col, 0.3f);
            var eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(go.transform, false);
            eye.transform.localPosition = new Vector3(0, h * 0.72f, r * 0.85f);
            eye.transform.localScale = new Vector3(r * 1.1f, 0.12f, 0.1f);
            eye.GetComponent<Renderer>().sharedMaterial = Mats.Glow(new Color(1f, 0.9f, 0.3f));
            var barGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(barGo.GetComponent<Collider>());
            barGo.transform.SetParent(go.transform, false);
            barGo.GetComponent<Renderer>().sharedMaterial = Mats.Glow(new Color(1f, 0.25f, 0.25f));
            v.bar = barGo.transform;
            v.target = e.Pos;
            go.transform.position = e.Pos;
            v.Apply(e);
            return v;
        }

        public void Apply(EnemyState e)
        {
            target = e.Pos;
            yawTarget = e.Yaw;
            float f = Mathf.Clamp01(e.Hp / (float)Mathf.Max(1, e.MaxHp));
            bar.localPosition = new Vector3(0, height + 0.45f, 0);
            bar.localScale = new Vector3(Mathf.Max(0.05f, f) * 1.2f, 0.1f, 0.1f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-16f * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yawTarget, 0), 1f - Mathf.Exp(-12f * dt));
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 look = cam.transform.position - bar.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.001f) bar.rotation = Quaternion.LookRotation(-look.normalized, Vector3.up);
            }
        }
    }

    public sealed class Bullet : MonoBehaviour
    {
        public Vector3 From, To;
        private float t;
        private float duration;

        public static void Fire(Vector3 a, Vector3 b, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.name = "Bullet";
            go.transform.localScale = new Vector3(0.09f, 0.09f, 0.55f);
            go.GetComponent<Renderer>().sharedMaterial = Mats.Glow(c);
            var bl = go.AddComponent<Bullet>();
            bl.From = a;
            bl.To = b;
            bl.duration = Mathf.Clamp((b - a).magnitude / 160f, 0.03f, 0.35f);
            go.transform.position = a;
            if ((b - a).sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(b - a);
        }

        private void Update()
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(From, To, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }

    public sealed class SceneBuilder
    {
        private readonly Transform root;
        private readonly Dictionary<int, PlayerView> players = new Dictionary<int, PlayerView>();
        private readonly Dictionary<int, EnemyView> enemies = new Dictionary<int, EnemyView>();
        public bool FromScene { get; private set; }
        public IEnumerable<PlayerView> Views { get { return players.Values; } }

        public SceneBuilder(Transform parent)
        {
            GameObject existing = GameObject.Find(ArenaFactory.RootName);
            if (existing != null && existing.transform.childCount > 8)
            {
                root = existing.transform;
                FromScene = true;
            }
            else
            {
                bool hasLight = false;
                #if UNITY_2022_2_OR_NEWER
                Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
                Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>();
#endif
                foreach (Light l in lights) if (l.type == LightType.Directional) hasLight = true;
                root = ArenaFactory.Build(parent, (c, glow) => glow ? Mats.Glow(c) : Mats.Solid(c, 0.15f), !hasLight);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.43f, 0.5f);
            RenderSettings.fog = false;
        }

        public void Sync(World w, int localId)
        {
            foreach (PlayerState p in w.Players.Values)
            {
                PlayerView v;
                if (!players.TryGetValue(p.Id, out v) || v == null)
                {
                    v = PlayerView.Create(p, p.Id == localId);
                    players[p.Id] = v;
                }
                v.Apply(p, p.Id == localId);
            }
            RemoveMissing(players, k => w.Players.ContainsKey(k));
            var ids = new HashSet<int>();
            foreach (EnemyState e in w.Enemies)
            {
                ids.Add(e.Id);
                EnemyView v;
                if (!enemies.TryGetValue(e.Id, out v) || v == null)
                {
                    v = EnemyView.Create(e);
                    enemies[e.Id] = v;
                }
                v.Apply(e);
            }
            RemoveMissing(enemies, k => ids.Contains(k));
        }

        private static void RemoveMissing<T>(Dictionary<int, T> map, Func<int, bool> keep) where T : MonoBehaviour
        {
            List<int> dead = null;
            foreach (var kv in map) if (!keep(kv.Key)) { if (dead == null) dead = new List<int>(); dead.Add(kv.Key); }
            if (dead == null) return;
            foreach (int k in dead)
            {
                if (map[k] != null) UnityEngine.Object.Destroy(map[k].gameObject);
                map.Remove(k);
            }
        }

        public void ClearAll()
        {
            foreach (var v in players.Values) if (v != null) UnityEngine.Object.Destroy(v.gameObject);
            foreach (var v in enemies.Values) if (v != null) UnityEngine.Object.Destroy(v.gameObject);
            players.Clear();
            enemies.Clear();
        }

        public void Destroy()
        {
            ClearAll();
            if (root != null && !FromScene) UnityEngine.Object.Destroy(root.gameObject);
        }

        public void Tracer(Vector3 a, Vector3 b, bool enemyShot, bool hit)
        {
            Color c = enemyShot ? new Color(1f, 0.5f, 0.2f) : new Color(1f, 0.95f, 0.4f);
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = 0.07f;
            lr.endWidth = 0.03f;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sharedMaterial = Mats.Glow(c);
            UnityEngine.Object.Destroy(go, 0.14f);
            Bullet.Fire(a, b, Color.white);
            if (hit) Sparks(b, enemyShot ? new Color(1, 0.3f, 0.3f) : new Color(1, 0.8f, 0.3f));
        }

        public void Sparks(Vector3 at, Color c)
        {
            for (int i = 0; i < 8; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(s.GetComponent<Collider>());
                s.transform.position = at;
                s.transform.localScale = Vector3.one * 0.1f;
                s.GetComponent<Renderer>().sharedMaterial = Mats.Glow(c);
                var sp = s.AddComponent<Spark>();
                sp.Velocity = UnityEngine.Random.onUnitSphere * 3.5f;
                UnityEngine.Object.Destroy(s, 0.45f);
            }
        }
    }

    public sealed class Spark : MonoBehaviour
    {
        public Vector3 Velocity;

        private void Update()
        {
            Velocity += Vector3.down * 9f * Time.deltaTime;
            transform.position += Velocity * Time.deltaTime;
        }
    }
}
