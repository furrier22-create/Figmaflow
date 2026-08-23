using System.Collections.Generic;

namespace FigmaRuntime
{
    public class OverlayRenderData
    {
        /// <summary>
        /// A Current / Target párok.
        /// Ezeket az OverlayRenderer építi fel.
        /// </summary>
        public readonly List<OverlayRenderPair> Pairs =
            new();

        /// <summary>
        /// Az animáció életciklusához tartozó elemek.
        /// Ezeket a következõ lépésben fogjuk felépíteni.
        /// </summary>
        public readonly List<OverlayAnimationItem> AnimationItems =
            new();

        public int PairCount => Pairs.Count;

        public int AnimationCount => AnimationItems.Count;

        // Visszafelé kompatibilitás
        public int Count => PairCount;

        public void Clear()
        {
            Pairs.Clear();
            AnimationItems.Clear();
        }
    }
}