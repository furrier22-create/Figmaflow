using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// Fel�p�ti az Overlay render modellj�t.
    ///
    /// Feladata kiz�r�lag:
    /// - Current / Target p�rok l�trehoz�sa
    /// - OverlayAnimationItem-ek l�trehoz�sa
    ///
    /// Nem anim�l.
    /// Nem m�dos�tja a UI-t.
    /// </summary>
    public class OverlayRenderer
    {
        public OverlayRenderData Build(
            TransitionAnalysis analysis,
            VisualElement currentRoot,
            VisualElement targetRoot)
        {
            OverlayRenderData data = new();

            if (analysis == null ||
                currentRoot == null ||
                targetRoot == null)
            {
                return data;
            }

            currentRoot =
                VisualTreeUtility.GetLogicalRoot(currentRoot);

            targetRoot =
                VisualTreeUtility.GetLogicalRoot(targetRoot);

            foreach (TrackCandidate candidate in analysis.Candidates)
            {
                VisualElement current =
                    VisualTreeUtility.FindByPath(
                        currentRoot,
                        candidate.Path);

                VisualElement target =
                    VisualTreeUtility.FindByPath(
                        targetRoot,
                        candidate.Path);

                if (current == null && target == null)
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogWarning(
                        $"[OverlayRenderer] Missing pair: {candidate.Path}");
#endif
                    continue;
                }

                // v0.4 / P4-2 (ADR-014): a párok egyik oldala lehet null —
                // megjelenő elem (csak target), eltűnő elem (csak current).
                RenderNode currentNode =
                    current != null ? new RenderNode(current) : null;

                RenderNode targetNode =
                    target != null ? new RenderNode(target) : null;

                OverlayRenderPair pair =
                    new OverlayRenderPair
                    {
                        Path = candidate.Path,
                        Current = currentNode,
                        Target = targetNode
                    };

                data.Pairs.Add(pair);

                OverlayAnimationItem item =
                    new OverlayAnimationItem
                    {
                        Path = candidate.Path,
                        Current = currentNode,
                        Target = targetNode,

                        // Az animált elem: current, ha létezik
                        // (megosztott / eltűnő), különben target
                        // (megjelenő).
                        AnimationElement =
                            currentNode?.Element ?? targetNode?.Element
                    };

                data.AnimationItems.Add(item);
            }

            UnityEngine.Debug.Log(
                $"[FigmaFlow] Overlay pairs      : {data.PairCount}");

            UnityEngine.Debug.Log(
                $"[FigmaFlow] Animation elements : {data.AnimationCount}");

            return data;
        }
    }
}