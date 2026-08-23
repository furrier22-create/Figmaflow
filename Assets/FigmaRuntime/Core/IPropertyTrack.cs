// ============================================================
// MOTION ENGINE FREEZE (v0.3.7)
// Ez a fájl a Motion Engine része. Stabil, bizonyítottan működik.
// Csak KRITIKUS BUG esetén módosítható, dokumentált döntéssel.
// Minden más fejlesztés ezen a fájlon kívül épüljön.
// ============================================================

namespace FigmaRuntime
{
    public interface IPropertyTrack
    {
        void Evaluate(float t);

        void Reset();

        void Complete();
    }
}