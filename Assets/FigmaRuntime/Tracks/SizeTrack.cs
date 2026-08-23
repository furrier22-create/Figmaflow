// ============================================================
// MOTION ENGINE FREEZE (v0.3.7)
// Ez a fájl a Motion Engine része. Stabil, bizonyítottan működik.
// Csak KRITIKUS BUG esetén módosítható, dokumentált döntéssel.
// Minden más fejlesztés ezen a fájlon kívül épüljön.
// ============================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    public class SizeTrack : IPropertyTrack
    {
        private readonly VisualElement visual;

        private readonly Rect startRect;
        private readonly Rect endRect;

        public SizeTrack(
            VisualElement visual,
            Rect startRect,
            Rect endRect)
        {
            this.visual = visual;
            this.startRect = startRect;
            this.endRect = endRect;
        }

        public void Evaluate(float t)
        {
            visual.style.width =
                Mathf.Lerp(startRect.width, endRect.width, t);

            visual.style.height =
                Mathf.Lerp(startRect.height, endRect.height, t);
        }

        public void Reset()
        {
            visual.style.width = startRect.width;
            visual.style.height = startRect.height;
        }

        public void Complete()
        {
            visual.style.width = endRect.width;
            visual.style.height = endRect.height;
        }
    }
}