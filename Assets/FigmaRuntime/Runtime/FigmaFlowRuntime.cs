namespace FigmaRuntime
{
    public static class FigmaFlowRuntime
    {
        public static bool HasAnalysis =>
            CurrentAnalysis != null;

        public static TransitionAnalysis CurrentAnalysis;

        public static void SetAnalysis(
            TransitionAnalysis analysis)
        {
            CurrentAnalysis = analysis;
        }

        public static void Clear()
        {
            CurrentAnalysis = null;
        }
    }
}