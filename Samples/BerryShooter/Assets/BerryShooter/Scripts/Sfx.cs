using UnityEngine;

namespace BerryShooter
{
    public static class Sfx
    {
        public enum Kind { Shot, Hit, Hurt, Kill, Wave, Click, Join, Over, Error }

        private static AudioSource source;
        private static readonly AudioClip[] clips = new AudioClip[9];

        public static void Init(GameObject host)
        {
            if (source != null) return;
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            AudioListener.volume = Settings.Volume;
        }

        public static void Play(Kind k, float volume = 1f)
        {
            if (source == null) return;
            int i = (int)k;
            if (clips[i] == null) clips[i] = Make(k);
            source.PlayOneShot(clips[i], volume);
        }

        private static AudioClip Make(Kind k)
        {
            switch (k)
            {
                case Kind.Shot: return Build("shot", 0.14f, (t, n) => (Noise(t) * 0.6f + Mathf.Sin(t * 2f * Mathf.PI * (180f - 900f * t)) * 0.5f) * Mathf.Exp(-t * 28f));
                case Kind.Hit: return Build("hit", 0.07f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * 1500f) * Mathf.Exp(-t * 40f));
                case Kind.Hurt: return Build("hurt", 0.22f, (t, n) => (Saw(t * 120f) * 0.6f + Noise(t) * 0.3f) * Mathf.Exp(-t * 12f));
                case Kind.Kill: return Build("kill", 0.22f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.1f ? 700f : 1050f)) * Mathf.Exp(-t * 11f));
                case Kind.Wave: return Build("wave", 0.5f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (400f + 700f * t)) * Mathf.Exp(-t * 3f) * 0.8f);
                case Kind.Click: return Build("click", 0.04f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * 900f) * Mathf.Exp(-t * 90f));
                case Kind.Join: return Build("join", 0.25f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.12f ? 600f : 900f)) * Mathf.Exp(-t * 8f));
                case Kind.Over: return Build("over", 0.7f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (500f - 350f * t)) * Mathf.Exp(-t * 3.5f));
                default: return Build("err", 0.18f, (t, n) => Saw(t * 200f) * 0.5f * Mathf.Exp(-t * 14f));
            }
        }

        private static float Noise(float t) { return Mathf.Sin(t * 91237.13f) * Mathf.Sin(t * 7311.7f + 3f) * 1.6f; }
        private static float Saw(float x) { return 2f * (x - Mathf.Floor(x + 0.5f)); }

        private static AudioClip Build(string name, float seconds, System.Func<float, int, float> f)
        {
            int rate = 44100, n = Mathf.CeilToInt(seconds * rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f((float)i / rate, i) * 0.6f, -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
