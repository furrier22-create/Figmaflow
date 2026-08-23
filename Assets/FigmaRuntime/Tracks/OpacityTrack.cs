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
    public class OpacityTrack : IPropertyTrack
    {
        private readonly VisualElement visual;

        private readonly float startOpacity;
        private readonly float endOpacity;

        public OpacityTrack(
            VisualElement visual,
            float startOpacity,
            float endOpacity)
        {
            this.visual = visual;
            this.startOpacity = startOpacity;
            this.endOpacity = endOpacity;
        }

        public void Evaluate(float t)
        {
            visual.style.opacity =
                Mathf.Lerp(startOpacity, endOpacity, t);
        }

        public void Reset()
        {
            visual.style.opacity = startOpacity;
        }

        public void Complete()
        {
            visual.style.opacity = endOpacity;
        }
    }
}