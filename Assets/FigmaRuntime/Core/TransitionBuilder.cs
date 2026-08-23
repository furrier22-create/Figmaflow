using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    public static class TransitionBuilder
    {
        public static TransitionAnalysis Analyze(
            FigmaStateAsset from,
            FigmaStateAsset to,
            float positionTolerance = 1f,
            float sizeTolerance = 1f,
            float opacityTolerance = 0.01f,
            bool worldRectPosition = false)
        {
            TransitionAnalysis analysis = new();

            Dictionary<string, ElementData> targetLookup = new();

            foreach (var e in to.elements)
                targetLookup[e.path] = e;

            foreach (var source in from.elements)
            {
                if (!targetLookup.TryGetValue(source.path, out var target))
                    continue;

                AnimatedProperties props =
                    GetChangedProperties(
                        source,
                        target,
                        positionTolerance,
                        sizeTolerance,
                        opacityTolerance,
                        worldRectPosition);

                if (props == AnimatedProperties.None)
                    continue;

                analysis.Candidates.Add(
                    new TrackCandidate()
                    {
                        Path = source.path,
                        From = source,
                        To = target,
                        Properties = props
                    });
            }

            AddAppearDisappearCandidates(
                analysis,
                from,
                to);

            Optimize(analysis.Candidates);

            return analysis;
        }

        public static Transition Build(
            TransitionAnalysis analysis,
            TransitionAsset asset,
            VisualElement liveRoot)
        {
            Transition transition = new();

            Dictionary<string, TransitionItem> lookup = new();

            foreach (var item in asset.items)
                lookup[item.path] = item;

            foreach (var candidate in analysis.Candidates)
            {
                if (!lookup.TryGetValue(candidate.Path, out var item))
                    continue;

                if (!item.enabled)
                    continue;

                VisualElement live =
                    VisualTreeUtility.FindByPath(
                        liveRoot,
                        candidate.Path);

                if (live == null)
                    continue;

                // v0.4 / P5 (ADR-015): a megjelenő/eltűnő elem mozgása
                // (közös helper — az OverlayTransitionBuilder is ezt hívja).
                ElementData from = candidate.From;
                ElementData to = candidate.To;

                AnimatedProperties props = candidate.Properties;

                ApplyAppearMode(
                    live,
                    candidate,
                    item,
                    ref from,
                    ref to,
                    ref props);

                transition.Add(
                    new ElementTrack(
                        live,
                        from,
                        to,
                        props,
                        item.delay,
                        item.duration,
                        item.ease,
                        item.customCurve));
            }

            return transition;
        }

        /// <summary>
        /// v0.4 / P5 (ADR-015): a per-item AppearMode alkalmazása a
        /// kandidátus from/to/props értékeire. Közös helper — a Layout
        /// (TransitionBuilder.Build) és az Overlay (OverlayTransitionBuilder)
        /// is ezt használja, hogy a SlideUp mindkét úton működjön.
        ///
        /// FONTOS (2026-08-02 hotfix #4): a pozíciók az ÉLŐ elem
        /// geometriájából számítódnak (resolvedStyle) — a Prepare utáni
        /// egy-frame várakozás (FigmaStateMachine) biztosítja, hogy a
        /// layout lefutott és az értékek érvényesek. Ha az élő érték
        /// mégis érvénytelen, a capture adatai a fallback.
        ///
        /// SlideUp: a szintetikus oldal az elem magasságával lefelé tolva
        /// + láthatatlan (opacity 0) → alulról felcsúszás (megjelenés) /
        /// lefelé csúszva eltűnés (eltűnés). A bottom anchor törölve
        /// (top+bottom konfliktus).
        /// </summary>
        public static void ApplyAppearMode(
            VisualElement live,
            TrackCandidate candidate,
            TransitionItem item,
            ref ElementData from,
            ref ElementData to,
            ref AnimatedProperties props,
            bool preferCapture = false)
        {
            if (live == null || candidate == null || item == null)
                return;

            if (item.appearMode != AppearMode.SlideUp &&
                item.appearMode != AppearMode.Scale)
                return;

            // Élő geometria (a layout-várakozás után érvényes).
            // preferCapture (v0.5 / Hero, ADR-018): proxykon a resolvedStyle
            // layer-koordináta, a capture viszont képernyő-koordináta —
            // keverve rossz lenne, ezért proxykon a capture adat a horgony.
            float liveTop = live.resolvedStyle.top;
            float liveLeft = live.resolvedStyle.left;
            float liveHeight = live.resolvedStyle.height;

            bool liveValid =
                !preferCapture &&
                !float.IsNaN(liveTop) &&
                !float.IsNaN(liveHeight) &&
                liveHeight > 0.01f;

            // Anchorként: az élő elem (ha érvényes), különben a capture.
            ElementData anchor =
                candidate.IsAppear ? to : from;

            float baseTop =
                liveValid ? liveTop : (anchor != null ? anchor.top : 0f);

            float x =
                liveValid ? liveLeft : (anchor != null ? anchor.left : 0f);

            float h =
                liveValid
                    ? liveHeight
                    : (anchor != null ? anchor.worldRect.height : 0f);

            if (h <= 0.01f || float.IsNaN(h))
            {
                Debug.LogWarning(
                    $"[FigmaFlow] SlideUp: {candidate.Path} magasság érvénytelen " +
                    $"(live h={liveHeight}, anchor h={anchor?.worldRect.height}) — " +
                    "a slide kihagyva (csak fade).");

                return;
            }

            // SCALE (ADR-028): a középpontból nő — a szintetikus oldal
            // kicsinyítve (a from/to worldRect a középpont körül), a
            // SizeTrack így a méretet animálja; az opacity is (a
            // láthatatlanból). A PositionTrack nem mozgat (a középpont
            // áll), de a SizeTrack a világméreten dolgozik — a kicsiny
            // rect a középpontnál kisebb, így a növekedés középre fut.
            if (item.appearMode == AppearMode.Scale)
            {
                if (candidate.IsAppear && from != null && to != null)
                {
                    from = CreateScaledCopy(from, 0.1f, 0f);

                    props |= AnimatedProperties.Size | AnimatedProperties.Opacity;

                    Debug.Log(
                        $"[FigmaFlow] Scale appear: {candidate.Path}");
                }
                else if (candidate.IsDisappear && from != null && to != null)
                {
                    to = CreateScaledCopy(to, 0.1f, 0f);

                    props |= AnimatedProperties.Size | AnimatedProperties.Opacity;

                    Debug.Log(
                        $"[FigmaFlow] Scale disappear: {candidate.Path}");
                }

                return;
            }

            if (candidate.IsAppear && from != null && to != null)
            {
                // Végpozíció: az élő elem helye.
                // Kezdőpozíció: lefelé tolva, láthatatlanul.
                from = CreatePositionedCopy(from, x, baseTop + h, 0f);
                to = CreatePositionedCopy(to, x, baseTop, to.opacity);

                props |= AnimatedProperties.Position;

                live.style.bottom = StyleKeyword.Auto;

                Debug.Log(
                    $"[FigmaFlow] SlideUp appear: {candidate.Path} " +
                    $"top={baseTop} h={h} (from {baseTop + h})");
            }
            else if (candidate.IsDisappear && from != null && to != null)
            {
                // Kezdőpozíció: az élő elem helye.
                // Végpozíció: lefelé tolva, láthatatlanul.
                from = CreatePositionedCopy(from, x, baseTop, from.opacity);
                to = CreatePositionedCopy(to, x, baseTop + h, 0f);

                props |= AnimatedProperties.Position;

                live.style.bottom = StyleKeyword.Auto;

                Debug.Log(
                    $"[FigmaFlow] SlideUp disappear: {candidate.Path} " +
                    $"top={baseTop} h={h} (to {baseTop + h})");
            }
        }

        /// <summary>
        /// A középpont-körüli kicsinyített másolat (Scale appear/disappear).
        /// A világméretet a középpont körül szorozza, az opacity-t beállítja.
        /// </summary>
        static ElementData CreateScaledCopy(
            ElementData src,
            float scaleFactor,
            float opacity)
        {
            float w = src.worldRect.width * scaleFactor;
            float h = src.worldRect.height * scaleFactor;

            float cx = src.worldRect.center.x;
            float cy = src.worldRect.center.y;

            return new ElementData()
            {
                name = src.name,
                path = src.path,
                worldRect = new Rect(
                    cx - w * 0.5f,
                    cy - h * 0.5f,
                    w,
                    h),
                left = src.left,
                top = src.top,
                width = src.width,
                height = src.height,
                opacity = opacity,
                rotation = src.rotation,
                scale = src.scale
            };
        }

        /// <summary>
        /// A pozíció-felülírt másolat (a slide élő-geometriás változata).
        /// </summary>
        static ElementData CreatePositionedCopy(
            ElementData src,
            float left,
            float top,
            float opacity)
        {
            return new ElementData()
            {
                name = src.name,
                path = src.path,
                worldRect = new Rect(
                    left,
                    top,
                    src.worldRect.width,
                    src.worldRect.height),
                left = left,
                top = top,
                width = src.width,
                height = src.height,
                opacity = opacity,
                rotation = src.rotation,
                scale = src.scale
            };
        }

        static AnimatedProperties GetChangedProperties(
            ElementData from,
            ElementData to,
            float positionTolerance,
            float sizeTolerance,
            float opacityTolerance,
            bool worldRectPosition = false)
        {
            AnimatedProperties props =
                AnimatedProperties.None;

            // -------------------------------------------------
            // STYLE POSITION
            // -------------------------------------------------

            if (Mathf.Abs(from.left - to.left) > positionTolerance ||
                Mathf.Abs(from.top - to.top) > positionTolerance)
            {
                props |= AnimatedProperties.Position;
            }

            // -------------------------------------------------
            // WORLD POSITION (v0.5, reverse fix — ADR-017c):
            // a translate-módú animáció közbeni visszafordulásnál a
            // mid-flight fa worldRect-je eltér az eredeti állapottól,
            // de a style left/top változatlan — a pozíció-változást
            // itt a worldRect-ből detektáljuk.
            // -------------------------------------------------

            if (worldRectPosition)
            {
                if (Mathf.Abs(from.worldRect.x - to.worldRect.x) > positionTolerance ||
                    Mathf.Abs(from.worldRect.y - to.worldRect.y) > positionTolerance)
                {
                    props |= AnimatedProperties.Position;
                }
            }

            // -------------------------------------------------
            // STYLE SIZE
            // -------------------------------------------------

            if (Mathf.Abs(from.width - to.width) > sizeTolerance ||
                Mathf.Abs(from.height - to.height) > sizeTolerance)
            {
                props |= AnimatedProperties.Size;
            }

            // -------------------------------------------------
            // VISUAL
            // -------------------------------------------------

            if (Mathf.Abs(from.opacity - to.opacity) > opacityTolerance)
            {
                props |= AnimatedProperties.Opacity;
            }

            // -------------------------------------------------
            // ROTATION (v0.5, ADR-023 — F8 zárás)
            // -------------------------------------------------

            const float RotationTolerance = 0.5f;

            if (Mathf.Abs(from.rotation - to.rotation) > RotationTolerance)
            {
                props |= AnimatedProperties.Rotation;
            }

            return props;
        }

        /// <summary>
        /// v0.4 / P4-2 (ADR-014): a csak az egyik állapotban létező elemek
        /// kezelése (Smart Animate paritás).
        ///
        /// - Csak a célban: megjelenő elem — a From oldal szintetikus
        ///   (opacity 0), így fade-in lesz.
        /// - Csak a forrásban: eltűnő elem — a To oldal szintetikus
        ///   (opacity 0), így fade-out lesz.
        ///
        /// A trackek mindkét oldalt nem-null ElementData-val kapják meg
        /// (a szintetikus oldal ugyanaz a pozíció/méret, csak opacity 0),
        /// ezért a Motion Engine-be nem kellett nyúlni (freeze tartva).
        /// </summary>
        static void AddAppearDisappearCandidates(
            TransitionAnalysis analysis,
            FigmaStateAsset from,
            FigmaStateAsset to)
        {
            if (analysis == null || from == null || to == null)
                return;

            Dictionary<string, ElementData> fromLookup = new();
            foreach (var e in from.elements)
            {
                if (e != null && !string.IsNullOrEmpty(e.path) && !fromLookup.ContainsKey(e.path))
                    fromLookup[e.path] = e;
            }

            Dictionary<string, ElementData> toLookup = new();
            foreach (var e in to.elements)
            {
                if (e != null && !string.IsNullOrEmpty(e.path) && !toLookup.ContainsKey(e.path))
                    toLookup[e.path] = e;
            }

            // Megjelenő elemek (csak a célban léteznek) → fade-in.
            foreach (var kv in toLookup)
            {
                if (fromLookup.ContainsKey(kv.Key))
                    continue;

                analysis.Candidates.Add(
                    new TrackCandidate()
                    {
                        Path = kv.Key,
                        From = CreateInvisibleCopy(kv.Value),
                        To = kv.Value,
                        Properties = AnimatedProperties.Opacity,
                        IsAppear = true
                    });
            }

            // Eltűnő elemek (csak a forrásban léteznek) → fade-out.
            foreach (var kv in fromLookup)
            {
                if (toLookup.ContainsKey(kv.Key))
                    continue;

                analysis.Candidates.Add(
                    new TrackCandidate()
                    {
                        Path = kv.Key,
                        From = kv.Value,
                        To = CreateInvisibleCopy(kv.Value),
                        Properties = AnimatedProperties.Opacity,
                        IsDisappear = true
                    });
            }
        }

        /// <summary>
        /// A megjelenés/eltűnés szintetikus oldala: ugyanaz a
        /// pozíció/méret, de opacity 0.
        /// </summary>
        static ElementData CreateInvisibleCopy(ElementData src)
        {
            return new ElementData()
            {
                name = src.name,
                path = src.path,
                worldRect = src.worldRect,
                left = src.left,
                top = src.top,
                width = src.width,
                height = src.height,
                opacity = 0f,
                rotation = src.rotation,
                scale = src.scale
            };
        }

        static void Optimize(List<TrackCandidate> candidates)
        {
            candidates.Sort((a, b) =>
                a.Path.Length.CompareTo(b.Path.Length));

            List<TrackCandidate> remove = new();

            for (int i = 0; i < candidates.Count; i++)
            {
                string parent = candidates[i].Path + "/";

                for (int j = i + 1; j < candidates.Count; j++)
                {
                    if (candidates[j].Path.StartsWith(parent))
                    {
                        remove.Add(candidates[j]);
                    }
                }
            }

            foreach (var r in remove)
                candidates.Remove(r);
        }
    }
}