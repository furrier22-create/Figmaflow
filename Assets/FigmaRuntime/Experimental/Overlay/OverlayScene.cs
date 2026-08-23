using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    public class OverlayScene : MonoBehaviour
    {
        private const string OverlayRootName = "FigmaFlow-OverlayRoot";

        private VisualElement documentRoot;
        private VisualElement overlayRoot;

        private TemplateContainer current;
        private TemplateContainer target;

        private OverlayLayer overlayLayer;

        private readonly OverlayRenderer renderer =
            new OverlayRenderer();

        public TemplateContainer Current => current;
        public TemplateContainer Target => target;

        public VisualElement CurrentRoot => current;
        public VisualElement TargetRoot => target;

        public OverlayLayer Layer => overlayLayer;

        /// <summary>
        /// A dokumentum gyÃ¶kerÃ©be fÃ¼ggesztett overlay rÃ©teg.
        /// (Az OverlayRuntime ezt hasznÃ¡lja az idegen gyermekek
        /// eltÃ¡volÃ­tÃ¡sÃ¡hoz â a statikus UIDocument tartalom nem
        /// maradhat az overlay alatt.)
        /// </summary>
        public VisualElement OverlayRoot => overlayRoot;

        public void InitializeRoot(VisualElement rootElement)
        {
            documentRoot = rootElement;

            overlayRoot = documentRoot.Q<VisualElement>(OverlayRootName);

            if (overlayRoot == null)
            {
                overlayRoot = new VisualElement
                {
                    name = OverlayRootName
                };

                overlayRoot.style.position = Position.Absolute;
                overlayRoot.style.left = 0;
                overlayRoot.style.top = 0;
                overlayRoot.style.right = 0;
                overlayRoot.style.bottom = 0;

                documentRoot.Add(overlayRoot);
            }

            overlayLayer ??= new OverlayLayer();
            overlayLayer.Attach(overlayRoot);
        }

        public void Initialize(
            VisualTreeAsset currentTree,
            VisualTreeAsset targetTree)
        {
            if (overlayRoot == null)
            {
                UnityEngine.Debug.LogError(
                    "[FigmaFlow] OverlayScene has no OverlayRoot.");

                return;
            }

            overlayRoot.Clear();

            current = Create(currentTree, true);
            target = Create(targetTree, false);

            overlayRoot.Add(current);
            overlayRoot.Add(target);

            overlayLayer.Attach(overlayRoot);

            // v0.5 / OverlayLayer (ADR-016): az előző animáció proxyi
            // nem maradhatnak (interrupció esetén a réteg tartalma
            // átvihető lenne az új átmenetre).
            overlayLayer.Clear();
        }

        public OverlayRenderData Build(
            TransitionAnalysis analysis)
        {
            if (analysis == null)
                return new OverlayRenderData();

            return renderer.Build(
                analysis,
                current,
                target);
        }

        public void Commit(
            VisualTreeAsset nextTarget)
        {
            if (overlayRoot == null)
                return;

            current?.RemoveFromHierarchy();

            current = target;

            current.style.visibility = Visibility.Visible;
            current.style.position = Position.Relative;

            // 2026-08-02 (P4 fix): a target az animáció alatt offscreen-re
            // volt tolva a rejtett layout miatt. Előléptetéskor a TELJES
            // stílus-visszaállítás kell — különben a látható állapot is
            // a képernyőn kívül marad, vagy nem tölti ki a réteget
            // („eltűnik a desktop”).
            current.style.left = StyleKeyword.Auto;
            current.style.top = StyleKeyword.Auto;
            current.style.right = StyleKeyword.Auto;
            current.style.bottom = StyleKeyword.Auto;

            current.style.width = new Length(100, LengthUnit.Percent);
            current.style.height = new Length(100, LengthUnit.Percent);

            target = Create(nextTarget, false);

            overlayRoot.Add(target);

            overlayLayer.Attach(overlayRoot);
            overlayLayer.Clear();
        }

        private TemplateContainer Create(
            VisualTreeAsset tree,
            bool visible)
        {
            TemplateContainer container = tree.Instantiate();

            // A konténer mindig a teljes overlay réteget töltse ki —
            // a benne lévő absolute elemek (pl. Backdrop) így a teljes
            // képernyőt fedik. (2026-08-02, P4-2 hotfix)
            container.style.width = new Length(100, LengthUnit.Percent);
            container.style.height = new Length(100, LengthUnit.Percent);

            if (visible)
            {
                container.style.visibility = Visibility.Visible;
                container.style.position = Position.Relative;
            }
            else
            {
                // v0.4 / P4-2 (ADR-014): a target konténer a valódi
                // pozíción, teljes méretben, rejtve. Így a megjelenő
                // elemek a valódi helyükön vannak; a binder egyedileg
                // láthatóvá teszi őket az animáció idejére.
                container.style.visibility = Visibility.Hidden;
                container.style.position = Position.Absolute;

                container.style.left = 0;
                container.style.top = 0;
                container.style.right = 0;
                container.style.bottom = 0;
            }

            return container;
        }
    }
}