// ============================================================
// MOTION ENGINE FREEZE (v0.3.7)
// Ez a fájl a Motion Engine része. Stabil, bizonyítottan működik.
// Csak KRITIKUS BUG esetén módosítható, dokumentált döntéssel.
// Minden más fejlesztés ezen a fájlon kívül épüljön.
//
// KIVÉTEL (v0.4, ADR-007 / F6 — 2026-08-01, projektvezetői döntés):
// Az Overlay-függőség kivezetve (RenderPair / Target / HasRenderPair
// / HideSource / ShowSource) — az asmdef szeparáció előfeltétele.
// A Motion Engine ezzel újra tiszta: nem tud Overlay-ről.
// A kötés a Builder / Binder szintre került.
//
// KIVÉTEL (v0.5, ADR-023 / F8 zárás — 2026-08-02, projektvezetői
// döntés): a RebuildTracks-ben egy Rotation ág — a rotation animálása
// (a Figma-import hiányosságai miatt a PNG+rotate út járható).
// A meglévő három track érintetlen; a RotationTrack külön fájl.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    public class ElementTrack
    {
        private readonly VisualElement source;

        private readonly ElementData from;
        private readonly ElementData to;

        private readonly List<IPropertyTrack> tracks = new();

        private readonly AnimatedProperties properties;

        private readonly EaseType ease;

        private readonly AnimationCurve customCurve;

        public float Delay { get; }

        public float Duration { get; }

        /// <summary>
        /// Az animált elem.
        ///
        /// Layout módban a live elem (a TransitionBuilder adja át).
        /// Overlay módban az animációs reprezentáció eleme
        /// (az OverlayTransitionBuilder adja át) — a track nem
        /// tud Overlay-ről, kizárólag azon az elemen animál,
        /// amivel létrehozták (ADR-007 / F6).
        /// </summary>
        public VisualElement Source { get; }

        public ElementTrack(
            VisualElement visual,
            ElementData from,
            ElementData to,
            AnimatedProperties properties,
            float delay,
            float duration,
            EaseType ease,
            AnimationCurve customCurve)
        {
            Source = visual;
            source = visual;

            this.from = from;
            this.to = to;

            this.properties = properties;

            Delay = Mathf.Max(0f, delay);
            Duration = Mathf.Max(0.01f, duration);

            this.ease = ease;
            this.customCurve = customCurve;

            RebuildTracks();
        }

        private void RebuildTracks()
        {
            tracks.Clear();

            if (source == null)
                return;

            if ((properties & AnimatedProperties.Position) != 0)
            {
                tracks.Add(
                    new PositionTrack(
                        source,
                        from,
                        to));
            }

            if ((properties & AnimatedProperties.Size) != 0)
            {
                tracks.Add(
                    new SizeTrack(
                        source,
                        from.worldRect,
                        to.worldRect));
            }

            if ((properties & AnimatedProperties.Opacity) != 0)
            {
                tracks.Add(
                    new OpacityTrack(
                        source,
                        from.opacity,
                        to.opacity));
            }

            // ADR-023 (v0.5): a rotation animálása (F8 zárás).
            if ((properties & AnimatedProperties.Rotation) != 0)
            {
                tracks.Add(
                    new RotationTrack(
                        source,
                        from.rotation,
                        to.rotation));
            }
        }

        public void Evaluate(float globalTime)
        {
            if (globalTime < Delay)
            {
                foreach (var track in tracks)
                    track.Evaluate(0f);

                return;
            }

            float localTime = globalTime - Delay;

            float t = Mathf.Clamp01(localTime / Duration);

            t = MotionUtility.Evaluate(
                ease,
                t,
                customCurve);

            foreach (var track in tracks)
                track.Evaluate(t);
        }

        public void Reset()
        {
            foreach (var track in tracks)
                track.Reset();
        }
    }
}
