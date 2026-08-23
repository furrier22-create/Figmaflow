using UnityEngine;

namespace FigmaRuntime
{
    public enum AnimationMode
    {
        Layout,
        Overlay
    }

    public enum EaseType
    {
        Linear,

        EaseIn,
        EaseOut,
        EaseInOut,

        EaseInBack,
        EaseOutBack,
        EaseInOutBack,

        Gentle,
        Quick,
        Bouncy,
        Slow,

        CustomBezier,
        CustomSpring
    }

    /// <summary>
    /// A megjelenő / eltűnő elemek animációs módja (v0.4 / P5, ADR-015).
    /// - Fade:    csak áttűnés (opacity 0 → 1 / 1 → 0)
    /// - SlideUp: áttűnés + az elem saját magasságával lefelé eltolva
    ///            indul (megjelenés: alulról felcsúszik; eltűnés:
    ///            lefelé csúszva tűnik el) — Bottom Sheet panelekhez.
    /// </summary>
    public enum AppearMode
    {
        Fade,
        SlideUp,

        /// <summary>
        /// v0.6/3 (ADR-028): a megjelenő elem kicsinyről a teljes
        /// méretére nő (popup/dropdown scale-in).
        /// </summary>
        Scale
    }

    [CreateAssetMenu(menuName = "FigmaFlow/Profile")]
    public class FigmaFlowProfile : ScriptableObject
    {
        [Header("Animation")]

        public AnimationMode animationMode =
            AnimationMode.Layout;

        public EaseType easing =
            EaseType.EaseInOut;

        public float duration = 0.6f;

        [Header("Tolerance")]

        public float positionTolerance = 1f;
        public float sizeTolerance = 1f;
        public float opacityTolerance = 0.01f;

        [Header("Overlay Layer (v0.5)")]

        /// <summary>
        /// v0.5 (ADR-016): az animációs reprezentáció (proxy-k a layer-en).
        /// Alapból KI van kapcsolva — a trackek az eredeti elemeken futnak
        /// (a v0.4 bizonyított viselkedés). Bekapcsolása KÍSÉRLETI: a
        /// style-módú (absolute) elemek koordináta-rendszere és a USS-alapú
        /// megjelenés még nem tökéletes a proxykon. A Hero/Shared Element
        /// feladatnál kapja meg a végleges kezelést.
        /// </summary>
        public bool useAnimationLayer = false;
    }
}