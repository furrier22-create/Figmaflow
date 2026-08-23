using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    [RequireComponent(typeof(UIDocument))]
    public class OverlayDocumentHost : MonoBehaviour
    {
        private UIDocument sourceDocument;

        private VisualElement hiddenRoot;

        public VisualElement HiddenRoot => hiddenRoot;

        void Awake()
        {
            sourceDocument =
                GetComponent<UIDocument>();

            hiddenRoot = new VisualElement();

            hiddenRoot.name = "FigmaFlow Hidden Root";

            hiddenRoot.style.position = Position.Absolute;

            hiddenRoot.style.left = -100000;
            hiddenRoot.style.top = -100000;

            hiddenRoot.style.visibility =
                Visibility.Hidden;

            sourceDocument.rootVisualElement.Add(
                hiddenRoot);
        }

        public TemplateContainer LoadHidden(
            VisualTreeAsset tree)
        {
            hiddenRoot.Clear();

            TemplateContainer container =
                tree.Instantiate();

            hiddenRoot.Add(container);

            return container;
        }

        public void Clear()
        {
            hiddenRoot.Clear();
        }
    }
}