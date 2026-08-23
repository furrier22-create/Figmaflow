using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// Felépíti a Runtime StateCache-t.
    ///
    /// Nem animál.
    /// Nem vált state-et.
    /// Kizárólag FigmaStateAsset-eket készít.
    /// </summary>
    public class StateCacheBuilder
    {
        private readonly FigmaDocumentHost host;

        public StateCacheBuilder(FigmaDocumentHost host)
        {
            this.host = host;
        }

        public IEnumerator Build(
            VisualTreeAsset[] visualStates,
            System.Action<StateCache> completed)
        {
            if (visualStates == null)
                yield break;

            StateCache cache =
                new StateCache(visualStates.Length);

            for (int i = 0; i < visualStates.Length; i++)
            {
                TemplateContainer live =
                    host.Load(visualStates[i]);

                // Megvárjuk a layoutot.
                yield return null;

                FigmaStateAsset state =
                    StateCapture.Capture(
                        live,
                        visualStates[i].name);

                cache.Set(i, state);
            }

            completed?.Invoke(cache);
        }
    }
}