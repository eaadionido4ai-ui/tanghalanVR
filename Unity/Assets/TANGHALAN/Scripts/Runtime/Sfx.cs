// Small sounds made when the game starts, as the web app makes them (src/rooms/sfx.js): a pop, a zip, a
// click, a cheer of notes, an alarm, and for TANGHALAN VR the rumble of an earthquake and the rain.
// No sound files, so the app stays light.
using System.Collections.Generic;
using UnityEngine;

namespace Tanghalan.Vr
{
    public static class Sfx
    {
        const int Rate = 44100;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioSource oneShots, rumble, rain;
        static System.Random rnd = new System.Random(7);

        public static void Init(Transform listener)
        {
            oneShots = listener.gameObject.AddComponent<AudioSource>();
            oneShots.playOnAwake = false; oneShots.spatialBlend = 0;
            Make("pop", Tones(new[] { 520f, 880f }, new[] { 0f, 0.07f }, 0.14f, 1, 0.5f));
            Make("drop", Tones(new[] { 330f }, new[] { 0f }, 0.14f, 1, 0.45f));
            Make("click", Tones(new[] { 1400f }, new[] { 0f }, 0.05f, 2, 0.18f));
            Make("open", Tones(new[] { 240f, 180f }, new[] { 0f, 0.08f }, 0.2f, 2, 0.16f));
            Make("cheer", Tones(new[] { 523f, 659f, 784f, 1047f }, new[] { 0f, 0.11f, 0.22f, 0.33f }, 0.3f, 1, 0.45f));
            Make("sad", Tones(new[] { 330f, 262f }, new[] { 0f, 0.25f }, 0.45f, 0, 0.4f));
            Make("zip", Noise(0.45f, 0.25f, true));
            var alarm = new List<float>(); var at = new List<float>();
            for (int i = 0; i < 6; i++) { alarm.Add(960); at.Add(i * 0.32f); alarm.Add(760); at.Add(i * 0.32f + 0.16f); }
            Make("alarm", Tones(alarm.ToArray(), at.ToArray(), 0.16f, 2, 0.22f));
            rumble = Loop(listener, Rumble(), "rumble");
            rain = Loop(listener, Noise(2f, 0.35f, false), "rain");
        }

        static void Make(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            clips[name] = c;
        }

        static AudioSource Loop(Transform listener, float[] data, string name)
        {
            Make(name, data);
            var s = listener.gameObject.AddComponent<AudioSource>();
            s.clip = clips[name]; s.loop = true; s.playOnAwake = false; s.volume = 0; s.spatialBlend = 0;
            return s;
        }

        // notes: 0 sine, 1 triangle, 2 square
        static float[] Tones(float[] freq, float[] start, float len, int wave, float vol)
        {
            float end = 0; foreach (var s in start) end = Mathf.Max(end, s + len);
            var d = new float[(int)(Rate * (end + 0.05f))];
            for (int k = 0; k < freq.Length; k++)
            {
                int s0 = (int)(start[k] * Rate), n = (int)(len * Rate);
                for (int i = 0; i < n && s0 + i < d.Length; i++)
                {
                    float t = (float)i / Rate, ph = freq[k] * t;
                    float w = wave == 0 ? Mathf.Sin(ph * 2 * Mathf.PI) : wave == 1 ? 1 - 4 * Mathf.Abs(ph - Mathf.Floor(ph + 0.5f)) : (ph - Mathf.Floor(ph) < 0.5f ? 1 : -1);
                    float env = Mathf.Min(1, t / 0.02f) * Mathf.Exp(-4f * t / len);
                    d[s0 + i] += w * env * vol;
                }
            }
            return d;
        }

        static float[] Noise(float len, float vol, bool rising)
        {
            var d = new float[(int)(Rate * len)];
            float lp = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / d.Length;
                float k = rising ? 0.05f + 0.6f * t : 0.35f;
                lp += k * ((float)rnd.NextDouble() * 2 - 1 - lp);
                float env = rising ? Mathf.Sin(t * Mathf.PI) : 1;
                d[i] = lp * vol * env;
            }
            return d;
        }

        // a deep, uneven roar: low-passed noise with slow swells
        static float[] Rumble()
        {
            var d = new float[Rate * 3];
            float lp = 0, lp2 = 0;
            for (int i = 0; i < d.Length; i++)
            {
                lp += 0.02f * ((float)rnd.NextDouble() * 2 - 1 - lp);
                lp2 += 0.05f * (lp - lp2);
                float swell = 0.7f + 0.3f * Mathf.Sin(i * 2 * Mathf.PI / (Rate * 1.5f));
                d[i] = lp2 * 6f * swell;
            }
            return d;
        }

        public static void Play(string name, float volume = 1f)
        {
            AudioClip c;
            if (oneShots != null && clips.TryGetValue(name, out c)) oneShots.PlayOneShot(c, volume);
        }

        /// <summary>The earthquake's rumble, as loud as the shaking is strong (0 to 1).</summary>
        public static void Rumble(float strength) { SetLoop(rumble, Mathf.Clamp01(strength)); }

        /// <summary>The rain, as loud as it falls (0 to 1).</summary>
        public static void Rain(float strength) { SetLoop(rain, Mathf.Clamp01(strength) * 0.6f); }

        static void SetLoop(AudioSource s, float v)
        {
            if (s == null) return;
            s.volume = Mathf.MoveTowards(s.volume, v, Time.deltaTime * 2f);
            if (s.volume > 0.001f && !s.isPlaying) s.Play();
            else if (s.volume <= 0.001f && s.isPlaying) s.Stop();
        }
    }
}
