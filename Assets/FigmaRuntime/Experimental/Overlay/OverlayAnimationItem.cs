using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// Egy animálható Overlay elem leírása.
    ///
    /// Nem animál.
    /// Nem renderel.
    /// Nem módosít UI-t.
    ///
    /// Kizárólag összeköti:
    /// - a Current elemet
    /// - a Target elemet
    /// - a késõbbi animációs reprezentációt.
    /// </summary>
    public class OverlayAnimationItem
    {
        /// <summary>
        /// Egyedi útvonal a dokumentumban.
        /// </summary>
        public string Path;

        /// <summary>
        /// Current UI eleme.
        /// </summary>
        public IRenderNode Current;

        /// <summary>
        /// Target UI eleme.
        /// </summary>
        public IRenderNode Target;

        /// <summary>
        /// A késõbbi animációs reprezentáció.
        ///
        /// v0.4 elsõ lépésében még megegyezik a Current elemmel.
        /// Késõbb külön VisualElement lesz.
        /// </summary>
        public VisualElement AnimationElement;

        /// <summary>
        /// Animáció alatt aktív-e.
        /// </summary>
        public bool Active;

        public bool IsReady =>
            Current != null &&
            Target != null;

        public void Begin()
        {
            Active = true;
        }

        public void End()
        {
            Active = false;
        }
    }
}