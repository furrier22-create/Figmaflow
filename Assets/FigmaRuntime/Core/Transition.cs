// ============================================================
// MOTION ENGINE FREEZE (v0.3.7)
// Ez a fájl a Motion Engine része. Stabil, bizonyítottan működik.
// Csak KRITIKUS BUG esetén módosítható, dokumentált döntéssel.
// Minden más fejlesztés ezen a fájlon kívül épüljön.
// ============================================================

using System.Collections.Generic;

namespace FigmaRuntime
{
    public class Transition
    {
        private readonly List<ElementTrack> tracks = new();

        public IReadOnlyList<ElementTrack> Tracks => tracks;

        public int Count => tracks.Count;

        public float Duration { get; private set; }

        public void Add(ElementTrack track)
        {
            tracks.Add(track);

            float endTime = track.Delay + track.Duration;

            if (endTime > Duration)
                Duration = endTime;
        }

        public void Evaluate(float time)
        {
            foreach (var track in tracks)
            {
                track.Evaluate(time);
            }
        }

        public void Reset()
        {
            foreach (var track in tracks)
            {
                track.Reset();
            }
        }
    }
}