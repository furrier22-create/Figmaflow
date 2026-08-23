using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// Smart Overlay futási koordinátor — v0.4 / P4 (ADR-012).
    ///
    /// Feladata:
    /// - a két élő UI kezelése
    /// - RenderPair-ek felépítése
    /// - Overlay Transition létrehozása
    /// - Commit kezelése
    ///
    /// Ez az osztály az IOverlayDriver implementációja: a Stable
    /// FigmaStateMachine ezen az interfészen keresztül éri el az
    /// Overlay pipeline-t (a Stable nem hivatkozhat az Experimentalre).
    ///
    /// FONTOS (2026-08-02): a Player / IsAvailable / scene init LUSTA —
    /// nem függ az Awake-ek sorrendjétől. A StateMachine.Awake kérheti
    /// a Player-t akár az OverlayRuntime.Awake előtt is: a GetComponent
    /// mindig működik, a mezők felhasználáskor töltődnek.
    ///
    /// Nem tartalmaz animációs logikát.
    /// </summary>
    [RequireComponent(typeof(FigmaDocumentHost))]
    [RequireComponent(typeof(OverlayScene))]
    public class OverlayRuntime : MonoBehaviour, IOverlayDriver
    {
        private FigmaDocumentHost host;
        private OverlayScene scene;
        private OverlayRenderer renderer;
        private OverlayTransitionPlayer overlayPlayer;
        private bool sceneInitialized;

        private OverlayRenderData renderData;

        public OverlayScene Scene => scene;

        public OverlayRenderData RenderData => renderData;

        // ---- IOverlayDriver (ADR-012) ----

        public bool IsAvailable
        {
            get
            {
                EnsurePlayer();

                return overlayPlayer != null;
            }
        }

        public ITransitionPlayer Player
        {
            get
            {
                EnsurePlayer();

                return overlayPlayer;
            }
        }

        void Awake()
        {
            host = GetComponent<FigmaDocumentHost>();

            scene = GetComponent<OverlayScene>();

            renderer = new OverlayRenderer();

            // Lusta inicializálás — az Awake sorrend nem számít.
            EnsurePlayer();

            EnsureSceneInitialized();
        }

        /// <summary>
        /// Az OverlayTransitionPlayer felkutatása (ha még nincs meg).
        /// </summary>
        void EnsurePlayer()
        {
            if (overlayPlayer == null)
            {
                overlayPlayer =
                    GetComponent<OverlayTransitionPlayer>();
            }
        }

        /// <summary>
        /// A scene root biztosítása (egyszer).
        /// Ha a FigmaDocumentHost.Awake még nem futott le, a root
        /// még null — a Prepare újrapróbálja, mire odaér, már kész.
        /// </summary>
        void EnsureSceneInitialized()
        {
            if (sceneInitialized)
                return;

            if (host == null || host.Root == null)
                return;

            scene.InitializeRoot(host.Root);

            sceneInitialized = true;
        }

        /// <summary>
        /// Előkészíti a két élő UI-t.
        /// Hamis visszatérés: a StateMachine Layout útra esik vissza.
        /// </summary>
        public bool Prepare(
            VisualTreeAsset currentTree,
            VisualTreeAsset targetTree,
            TransitionAnalysis analysis)
        {
            EnsureSceneInitialized();

            if (!sceneInitialized)
            {
                Debug.LogWarning(
                    "[FigmaFlow] OverlayRuntime: a scene még nem inicializált " +
                    "(nincs root) — Layout fallback.");

                return false;
            }

            if (currentTree == null || targetTree == null)
                return false;

            RemoveForeignChildren();

            scene.Initialize(
                currentTree,
                targetTree);

            renderData =
                renderer.Build(
                    analysis,
                    scene.CurrentRoot,
                    scene.TargetRoot);

            Debug.Log(
                $"[FigmaFlow] Overlay prepared ({renderData.Count} pairs)");

            return true;
        }

        /// <summary>
        /// v0.5 / OverlayLayer (ADR-016): a proxy-k létrehozása a layer-en.
        ///
        /// A Prepare (layout-várakozás) UTÁN hívandó. Minden párhoz:
        /// - proxy készül az élő elemből (OverlayElementFactory)
        /// - a proxy a layer-re kerül
        /// - a pár render node-jára kötődik (a trackek a proxyt animálják)
        /// - az eredeti (current) elem elrejtve — nincs dupla megjelenés
        ///
        /// Ha egy párhoz nem készül proxy (sikertelen), az eredeti elem
        /// marad a track célja (graceful fallback — a P4 viselkedés).
        /// </summary>
        public bool PrepareAnimationLayer(TransitionAnalysis analysis)
        {
            if (renderData == null || scene == null)
                return false;

            scene.Layer.Clear();

            int proxyCount = 0;

            foreach (OverlayRenderPair pair in renderData.Pairs)
            {
                if (pair == null)
                    continue;

                VisualElement source =
                    pair.Current?.Original ?? pair.Target?.Original;

                if (source == null)
                    continue;

                VisualElement proxy =
                    OverlayElementFactory.CreateProxy(
                        source,
                        scene.Layer.Root);

                if (proxy == null)
                    continue;

                scene.Layer.Add(proxy);

                if (pair.Current != null)
                {
                    pair.Current.SetAnimation(proxy);

                    // Az eredeti elem elrejtése — csak a proxy látszódjon.
                    if (pair.Current.Original != null)
                        pair.Current.Original.style.visibility = Visibility.Hidden;
                }
                else if (pair.Target != null)
                {
                    pair.Target.SetAnimation(proxy);
                }

                proxyCount++;
            }

            Debug.Log(
                $"[FigmaFlow] Overlay layer: {proxyCount} proxy");

            return true;
        }

        /// <summary>
        /// Eltávolítja a host root azon gyermekeit, amelyek nem az
        /// overlay réteghez tartoznak (pl. az UIDocument statikus
        /// tartalma, vagy a korábbi capture-állapot). Csak az overlay
        /// réteg maradjon látható — különben a régi UI áttetszik/duplikál
        /// az animáció alatt.
        /// </summary>
        void RemoveForeignChildren()
        {
            if (host == null || host.Root == null)
                return;

            VisualElement overlayRoot =
                scene != null ? scene.OverlayRoot : null;

            if (overlayRoot == null)
                return;

            foreach (VisualElement child in host.Root.Children())
            {
                if (child == overlayRoot)
                    continue;

                child.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// Overlay Transition létrehozása.
        /// </summary>
        public Transition BuildTransition(
            TransitionAnalysis analysis,
            TransitionAsset asset)
        {
            if (renderData == null)
            {
                Debug.LogError(
                    "[FigmaFlow] OverlayRuntime: Prepare() must be called before BuildTransition().");

                return new Transition();
            }

            return OverlayTransitionBuilder.Build(
                analysis,
                asset,
                renderData,
                scene.Layer.Root);
        }

        /// <summary>
        /// Animáció vége — a rétegek összeolvasztása.
        /// </summary>
        public void Commit(
            VisualTreeAsset nextTarget)
        {
            scene.Commit(nextTarget);

            host.SetCurrent(scene.Current);

            Debug.Log(
                "[FigmaFlow] Overlay commit completed.");
        }

        public void Reset()
        {
            renderData = null;
        }
    }
}
