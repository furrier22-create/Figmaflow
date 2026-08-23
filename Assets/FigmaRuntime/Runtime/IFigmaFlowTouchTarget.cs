namespace FigmaRuntime
{
    /// <summary>
    /// v0.7 (ADR-032 kiegészítés): az érintés-célpont interfésze.
    ///
    /// A FigmaRuntime assembly-ben van (nem a TUIO-ban) — így a puzzle-ok
    /// (a FigmaRuntime alatt) és a TUIO-input (az Assembly-CSharp-ban)
    /// egyaránt elérik. A szintetikus ClickEvent dispatch-e nem garantáltan
    /// éri el a RegisterCallback<ClickEvent>-et (a ClickEvent a pointer-
    /// fázisokból származik) — a közvetlen OnTap hívás megbízható.
    /// </summary>
    public interface IFigmaFlowTouchTarget
    {
        /// <summary>
        /// Érintés (tap) — a kijelölés / slotra helyezés logikája.
        /// </summary>
        void OnTap(string elementName);
    }
}
