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
    public class PositionTrack : IPropertyTrack
    {
        readonly VisualElement visual;

        readonly float startLeft;
        readonly float startTop;

        readonly float endLeft;
        readonly float endTop;

        readonly Rect startRect;
        readonly Rect endRect;

        readonly bool useStyle;

        public PositionTrack(
            VisualElement visual,
            ElementData from,
            ElementData to)
        {
            this.visual = visual;

            startLeft = from.left;
            startTop = from.top;

            endLeft = to.left;
            endTop = to.top;

            startRect = from.worldRect;
            endRect = to.worldRect;

            bool styleChanged =
                Mathf.Abs(startLeft - endLeft) > 0.01f ||
                Mathf.Abs(startTop - endTop) > 0.01f;

            useStyle = styleChanged;
        }

        public void Evaluate(float t)
        {
            if (useStyle)
            {
                visual.style.left =
                    Mathf.Lerp(startLeft, endLeft, t);

                visual.style.top =
                    Mathf.Lerp(startTop, endTop, t);

                return;
            }

            float x =
                Mathf.Lerp(
                    startRect.x,
                    endRect.x,
                    t);

            float y =
                Mathf.Lerp(
                    startRect.y,
                    endRect.y,
                    t);

            visual.style.translate =
                new Translate(
                    new Length(
                        x - startRect.x,
                        LengthUnit.Pixel),
                    new Length(
                        y - startRect.y,
                        LengthUnit.Pixel));
        }

        public void Reset()
        {
            if (useStyle)
            {
                visual.style.left = startLeft;
                visual.style.top = startTop;
            }
            else
            {
                visual.style.translate =
                    new Translate(0, 0);
            }
        }

        public void Complete()
        {
            if (useStyle)
            {
                visual.style.left = endLeft;
                visual.style.top = endTop;
            }
            else
            {
                visual.style.translate =
                    new Translate(
                        new Length(
                            endRect.x - startRect.x,
                            LengthUnit.Pixel),
                        new Length(
                            endRect.y - startRect.y,
                            LengthUnit.Pixel));
            }
        }
    }
}