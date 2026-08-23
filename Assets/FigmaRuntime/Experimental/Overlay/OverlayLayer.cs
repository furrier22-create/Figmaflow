using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// Az animációs elemek külön rétege.
    ///
    /// Nem része sem a Current,
    /// sem a Target UI-nak.
    /// Csak az animáció idejére tartalmaz elemeket.
    /// </summary>
    public class OverlayLayer
    {
        private readonly VisualElement root;

        public VisualElement Root => root;

        public OverlayLayer()
        {
            root = new VisualElement
            {
                name = "FigmaFlow-OverlayLayer"
            };

            root.style.position = Position.Absolute;

            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;

            root.style.visibility = Visibility.Visible;

            // Az animációs réteg ne fogjon inputot.
            root.pickingMode = PickingMode.Ignore;
        }

        public void Attach(VisualElement parent)
        {
            if (parent == null)
                return;

            if (root.parent == parent)
                return;

            root.RemoveFromHierarchy();

            parent.Add(root);
        }

        public void Clear()
        {
            root.Clear();
        }

        public void Add(VisualElement element)
        {
            if (element == null)
                return;

            root.Add(element);
        }

        public void Remove(VisualElement element)
        {
            if (element == null)
                return;

            if (element.parent == root)
                root.Remove(element);
        }
    }
}