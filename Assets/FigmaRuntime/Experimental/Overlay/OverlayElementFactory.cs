using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.5 / OverlayLayer (ADR-016) + Hero (ADR-018): vizuális proxy készítés.
    ///
    /// A proxy a layer-en él az animáció idejére — az eredeti fát
    /// nem piszkáljuk. A trackek a proxyra írnak, a commit a réteg
    /// törlésével eldobja őket.
    ///
    /// Pozíció: világkoordináta (worldBound) a layer root-hoz képest.
    /// Megjelenés: háttér, kép, szegély, sarok, opacitás, szöveg +
    /// GYERMEKEK rekurzív másolása (maxDepth) — a Hero kártya így
    /// valósághűen néz ki a rétegen.
    ///
    /// A teljes USS-öröklés nem másolódik (dokumentált korlát, ADR-016).
    /// </summary>
    public static class OverlayElementFactory
    {
        public static VisualElement CreateProxy(
            VisualElement source,
            VisualElement parent,
            int maxDepth = 3)
        {
            if (source == null)
                return null;

            VisualElement proxy = CreateElement(source);

            if (proxy == null)
                return null;

            CopyStyle(source, proxy);

            // Pozíció: világkoordináta a layer root-hoz képest.
            float px = float.NaN;
            float py = float.NaN;

            if (parent != null)
            {
                Rect sr = source.worldBound;
                Rect pr = parent.worldBound;

                if (!float.IsNaN(sr.x) && !float.IsNaN(pr.x))
                {
                    px = sr.x - pr.x;
                    py = sr.y - pr.y;
                }
            }

            IStyle ps = proxy.style;

            ps.position = Position.Absolute;

            if (!float.IsNaN(px))
            {
                ps.left = px;
                ps.top = py;
            }

            ps.width = source.resolvedStyle.width;
            ps.height = source.resolvedStyle.height;

            CopyChildren(source, proxy, maxDepth);

            proxy.pickingMode = PickingMode.Ignore;

            return proxy;
        }

        static VisualElement CreateElement(VisualElement source)
        {
            if (source is Label label)
                return new Label { text = label.text };

            if (source is Button button)
                return new Button { text = button.text };

            return new VisualElement();
        }

        static void CopyStyle(VisualElement source, VisualElement proxy)
        {
            IResolvedStyle s = source.resolvedStyle;
            IStyle p = proxy.style;

            p.opacity = s.opacity;

            p.backgroundColor = s.backgroundColor;
            p.backgroundImage = s.backgroundImage;

            p.borderTopLeftRadius = s.borderTopLeftRadius;
            p.borderTopRightRadius = s.borderTopRightRadius;
            p.borderBottomLeftRadius = s.borderBottomLeftRadius;
            p.borderBottomRightRadius = s.borderBottomRightRadius;

            p.borderLeftWidth = s.borderLeftWidth;
            p.borderRightWidth = s.borderRightWidth;
            p.borderTopWidth = s.borderTopWidth;
            p.borderBottomWidth = s.borderBottomWidth;

            p.borderLeftColor = s.borderLeftColor;
            p.borderRightColor = s.borderRightColor;
            p.borderTopColor = s.borderTopColor;
            p.borderBottomColor = s.borderBottomColor;

            p.fontSize = s.fontSize;
            p.color = s.color;
            p.unityTextAlign = s.unityTextAlign;
            p.unityFontStyleAndWeight = s.unityFontStyleAndWeight;

            p.alignItems = s.alignItems;
            p.justifyContent = s.justifyContent;
            p.flexDirection = s.flexDirection;
        }

        static void CopyChildren(
            VisualElement source,
            VisualElement proxy,
            int depth)
        {
            if (depth <= 0)
                return;

            foreach (VisualElement child in source.Children())
            {
                VisualElement cp = CreateElement(child);

                if (cp == null)
                    continue;

                CopyStyle(child, cp);

                CopyChildren(child, cp, depth - 1);

                proxy.Add(cp);
            }
        }
    }
}
