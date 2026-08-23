using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// Overlay Transition építő.
    ///
    /// A Layout TransitionBuilder-t nem módosítja.
    /// A Transition ugyanaz marad.
    ///
    /// v0.5 / Hero (ADR-018): ha a layer aktív (a párnak van proxy-ja),
    /// a track-ek a LAYER koordináta-rendszerében futnak:
    /// - a from/to worldRect a layer root-hoz igazítva (ToLayerData)
    /// - a style-pozíció nullázva → a PositionTrack TRANSLATE-módban
    ///   dolgozik (a freeze-elt track változtatás nélkül helyes a layeren)
    /// - a proxy a from pozícióra kerül (a translate delta arra épül)
    /// - a slide (ApplyAppearMode) proxykon a capture-adatokat használja
    ///   (preferCapture — a proxy resolvedStyle-ja layer-koordináta, a
    ///   capture-koordinátákkal keverve rossz lenne)
    ///
    /// Ha a layer inaktív (nincs proxy), a régi viselkedés fut.
    /// </summary>
    public static class OverlayTransitionBuilder
    {
        public static Transition Build(
            TransitionAnalysis analysis,
            TransitionAsset asset,
            OverlayRenderData renderData,
            VisualElement layerRoot = null)
        {
            Transition transition = new();

            if (analysis == null ||
                asset == null ||
                renderData == null)
            {
                return transition;
            }

            Dictionary<string, TransitionItem> itemLookup = new();

            foreach (var item in asset.items)
                itemLookup[item.path] = item;

            Dictionary<string, OverlayRenderPair> pairLookup = new();

            foreach (var pair in renderData.Pairs)
                pairLookup[pair.Path] = pair;

            Rect layerRect = default;

            bool haveLayerRect =
                layerRoot != null &&
                !float.IsNaN(layerRoot.worldBound.x) &&
                !float.IsNaN(layerRoot.worldBound.y);

            if (haveLayerRect)
                layerRect = layerRoot.worldBound;

            foreach (var candidate in analysis.Candidates)
            {
                if (!itemLookup.TryGetValue(candidate.Path, out var item))
                    continue;

                if (!item.enabled)
                    continue;

                if (!pairLookup.TryGetValue(candidate.Path, out var pair))
                    continue;

                VisualElement live =
                    pair.Current?.Element ?? pair.Target?.Element;

                if (live == null)
                    continue;

                ElementData from = candidate.From;
                ElementData to = candidate.To;

                AnimatedProperties props = candidate.Properties;

                // v0.5 / Hero (ADR-018): van-e proxy a párhoz (layer aktív).
                bool isProxy =
                    (pair.Current?.HasAnimation ?? false) ||
                    (pair.Target?.HasAnimation ?? false);

                TransitionBuilder.ApplyAppearMode(
                    live,
                    candidate,
                    item,
                    ref from,
                    ref to,
                    ref props,
                    preferCapture: isProxy);

                if (isProxy && haveLayerRect)
                {
                    // Layer-koordináták + style nullázás (translate-mód).
                    from = ToLayerData(from, layerRect);
                    to = ToLayerData(to, layerRect);

                    // A proxy a from pozícióra — a translate delta arra épül.
                    if (from != null)
                    {
                        live.style.left = from.worldRect.x;
                        live.style.top = from.worldRect.y;
                    }
                }

                ElementTrack track = new ElementTrack(
                    live,
                    from,
                    to,
                    props,
                    item.delay,
                    item.duration,
                    item.ease,
                    item.customCurve);

                transition.Add(track);
            }

            return transition;
        }

        /// <summary>
        /// Layer-koordinátákra konvertálás + style-pozíció nullázás
        /// (a PositionTrack translate-módját kényszeríti — a layeren
        /// a style-mód rossz lenne, mert a proxy abszolút a layeren van).
        /// </summary>
        static ElementData ToLayerData(ElementData src, Rect layer)
        {
            if (src == null)
                return null;

            return new ElementData()
            {
                name = src.name,
                path = src.path,
                worldRect = new Rect(
                    src.worldRect.x - layer.x,
                    src.worldRect.y - layer.y,
                    src.worldRect.width,
                    src.worldRect.height),
                left = 0f,
                top = 0f,
                width = src.width,
                height = src.height,
                opacity = src.opacity,
                rotation = src.rotation,
                scale = src.scale
            };
        }
    }
}
