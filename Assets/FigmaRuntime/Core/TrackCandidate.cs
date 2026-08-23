namespace FigmaRuntime
{
    public class TrackCandidate
    {
        public string Path;

        public ElementData From;

        public ElementData To;

        public AnimatedProperties Properties;

        /// <summary>
        /// v0.4 / P5 (ADR-015): az elem csak a cél állapotban létezik
        /// (megjelenő elem) — a From szintetikus (opacity 0).
        /// </summary>
        public bool IsAppear;

        /// <summary>
        /// v0.4 / P5 (ADR-015): az elem csak a forrás állapotban létezik
        /// (eltűnő elem) — a To szintetikus (opacity 0).
        /// </summary>
        public bool IsDisappear;
    }
}
