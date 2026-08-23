#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;

namespace FigmaRuntime.Editor
{
    /// <summary>
    /// ADR-005 / ADR-008 (v0.3.7):
    /// A TransitionAssetBuilder az Editor réteg része.
    ///
    /// A Runtime Analysis kimenetét (TrackCandidate lista) tölti bele
    /// egy TransitionAsset-be. A Runtime NEM hívhatja — csak az
    /// Editor (FigmaFlowWindow, TransitionAssetGenerator).
    /// </summary>
    public static class TransitionAssetBuilder
    {
        public static void Fill(
            TransitionAsset asset,
            List<TrackCandidate> candidates)
        {
            Dictionary<string, TransitionItem> existing =
                new();

            foreach (var item in asset.items)
            {
                existing[item.path] = item;
            }

            List<TransitionItem> result =
                new();

            foreach (var candidate in candidates)
            {
                if (existing.TryGetValue(
                    candidate.Path,
                    out TransitionItem oldItem))
                {
                    oldItem.properties =
                        candidate.Properties;

                    result.Add(oldItem);
                }
                else
                {
                    result.Add(
                        new TransitionItem()
                        {
                            path = candidate.Path,
                            enabled = true,
                            favorite = false,
                            delay = 0f,
                            duration = 0.6f,
                            ease = EaseType.EaseInOut,
                            animationMode = AnimationMode.Layout,
                            properties = candidate.Properties
                        });
                }
            }

            asset.items = result;

            EditorUtility.SetDirty(asset);
        }
    }
}

#endif
