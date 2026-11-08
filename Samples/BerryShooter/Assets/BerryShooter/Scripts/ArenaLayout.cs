using UnityEngine;

namespace BerryShooter
{
    public struct Block
    {
        public float X, Z, Hx, Hz, Height;
        public Block(float x, float z, float hx, float hz, float h) { X = x; Z = z; Hx = hx; Hz = hz; Height = h; }
    }

    public static class ArenaLayout
    {
        public const float Half = 22f;
        public const float PlayerRadius = 0.45f;
        public static readonly Block[] Blocks =
        {
            new Block(-9, -9, 2.5f, 0.6f, 2.4f), new Block(9, -9, 0.6f, 2.5f, 2.4f), new Block(-9, 9, 0.6f, 2.5f, 2.4f), new Block(9, 9, 2.5f, 0.6f, 2.4f),
            new Block(0, 0, 1.6f, 1.6f, 3.2f), new Block(0, 14, 3f, 0.6f, 2.4f), new Block(0, -14, 3f, 0.6f, 2.4f),
            new Block(14, 0, 0.6f, 3f, 2.4f), new Block(-14, 0, 0.6f, 3f, 2.4f),
        };

        public static Vector3 Resolve(Vector3 p, float radius)
        {
            float lim = Half - radius;
            p.x = Mathf.Clamp(p.x, -lim, lim);
            p.z = Mathf.Clamp(p.z, -lim, lim);
            for (int pass = 0; pass < 2; pass++)
                foreach (Block b in Blocks)
                {
                    float cx = Mathf.Clamp(p.x, b.X - b.Hx, b.X + b.Hx), cz = Mathf.Clamp(p.z, b.Z - b.Hz, b.Z + b.Hz);
                    float dx = p.x - cx, dz = p.z - cz, d2 = dx * dx + dz * dz;
                    if (d2 >= radius * radius) continue;
                    if (d2 > 0.00001f)
                    {
                        float d = Mathf.Sqrt(d2), k = (radius - d) / d;
                        p.x += dx * k; p.z += dz * k;
                    }
                    else
                    {
                        float l = p.x - (b.X - b.Hx), r = (b.X + b.Hx) - p.x, f = p.z - (b.Z - b.Hz), bk = (b.Z + b.Hz) - p.z;
                        float m = Mathf.Min(Mathf.Min(l, r), Mathf.Min(f, bk));
                        if (m == l) p.x = b.X - b.Hx - radius; else if (m == r) p.x = b.X + b.Hx + radius; else if (m == f) p.z = b.Z - b.Hz - radius; else p.z = b.Z + b.Hz + radius;
                    }
                }
            return p;
        }

        public static float RayToCover(Vector3 o, Vector3 d, float maxDist)
        {
            float best = maxDist;
            foreach (Block b in Blocks)
            {
                float t0 = 0f, t1 = best;
                if (!Slab(o.x, d.x, b.X - b.Hx, b.X + b.Hx, ref t0, ref t1)) continue;
                if (!Slab(o.y, d.y, 0f, b.Height, ref t0, ref t1)) continue;
                if (!Slab(o.z, d.z, b.Z - b.Hz, b.Z + b.Hz, ref t0, ref t1)) continue;
                if (t0 < best) best = t0;
            }
            return best;
        }

        private static bool Slab(float o, float d, float min, float max, ref float t0, ref float t1)
        {
            if (Mathf.Abs(d) < 1e-6f) return o >= min && o <= max;
            float a = (min - o) / d, b = (max - o) / d;
            if (a > b) { float s = a; a = b; b = s; }
            if (a > t0) t0 = a;
            if (b < t1) t1 = b;
            return t0 <= t1;
        }

        public static Vector3 SpawnPoint(int i)
        {
            float a = (i % 8) * Mathf.PI / 4f + 0.4f;
            return Resolve(new Vector3(Mathf.Cos(a) * 5.5f, 0, Mathf.Sin(a) * 5.5f), PlayerRadius);
        }
    }
}
