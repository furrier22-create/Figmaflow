using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// Az OverlayBinder a render párok életciklusát kezeli (v0.4 / P4).
    ///
    /// v0.5 / OverlayLayer (ADR-016) — KOMBINÁLT MŰKÖDÉS:
    /// - Ha a párhoz készült proxy (layer AKTÍV, RenderNode.HasAnimation):
    ///   a binder nem nyúl az eredeti elemhez — a layer intézi a
    ///   megjelenést (az eredeti elrejtve, a proxy animálódik).
    /// - Ha NINCS proxy (layer INAKTÍV — az alapértelmezett v0.4-es
    ///   viselkedés): a megjelenő elemeket (csak a target fában élnek)
    ///   a binder láthatóvá teszi a rejtett konténerben, 0 opacitásról
    ///   indulva (a track fade-in-el). Az Unbind visszaállítja a
    ///   stílusokat — a commit után a fa a saját stílusaival jelenik meg.
    ///
    /// Nem animál. Nem módosítja a Motion Engine-t.
    /// </summary>
    public class OverlayBinder
    {
        private readonly VisualElement root;

        private readonly List<OverlayRenderPair> boundPairs =
            new();

        private readonly List<VisualElement> shownTargetElements =
            new();

        public OverlayBinder(VisualElement root)
        {
            this.root = root;
        }

        public void Bind(OverlayRenderData renderData)
        {
            UnbindInternal();

            if (renderData == null)
                return;

            foreach (OverlayRenderPair pair in renderData.Pairs)
            {
                if (pair == null)
                    continue;

                boundPairs.Add(pair);

                // Megjelenő elem (csak a target fában él) + nincs proxy
                // (layer inaktív) → láthatóvá tétel a rejtett konténerben
                // + 0 opacitás (a track fade-in-el).
                if (pair.Current == null &&
                    pair.Target?.Element != null &&
                    !pair.Target.HasAnimation)
                {
                    VisualElement element = pair.Target.Element;

                    element.style.visibility = Visibility.Visible;
                    element.style.opacity = 0f;

                    shownTargetElements.Add(element);
                }
            }
        }

        public void Unbind()
        {
            UnbindInternal();
        }

        private void UnbindInternal()
        {
            foreach (VisualElement element in shownTargetElements)
            {
                if (element == null)
                    continue;

                // Visszaállítás a fa saját stílusaira (a commit
                // után a konténer látható, az elem a stílusa szerint).
                element.style.visibility = StyleKeyword.Null;
                element.style.opacity = StyleKeyword.Null;
            }

            shownTargetElements.Clear();

            boundPairs.Clear();
        }
    }
}
