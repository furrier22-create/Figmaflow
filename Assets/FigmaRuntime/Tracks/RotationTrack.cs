using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.5 (ADR-023): az elforgatás animálása (F8 zárás).
    ///
    /// A style.rotate szögét interpolálja a from/to között.
    /// A Position/Size/Opacity trackekkel azonos mintát követ
    /// (IPropertyTrack: Evaluate/Reset/Complete).
    ///
    /// Ez ÚJ fájl — nem része a Motion Engine Freeze listának.
    /// </summary>
    public class RotationTrack : IPropertyTrack
    {
        private readonly VisualElement visual;

        private readonly float startAngle;
        private readonly float endAngle;

        public RotationTrack(
            VisualElement visual,
            float startAngle,
            float endAngle)
        {
            this.visual = visual;
            this.startAngle = startAngle;
            this.endAngle = endAngle;
        }

        public void Evaluate(float t)
        {
            float angle =
                Mathf.Lerp(startAngle, endAngle, t);

            visual.style.rotate =
                new Rotate(
                    new Angle(
                        angle,
                        AngleUnit.Degree));
        }

        public void Reset()
        {
            visual.style.rotate =
                new Rotate(
                    new Angle(
                        startAngle,
                        AngleUnit.Degree));
        }

        public void Complete()
        {
            visual.style.rotate =
                new Rotate(
                    new Angle(
                        endAngle,
                        AngleUnit.Degree));
        }
    }
}
