using UnityEngine.UIElements;

namespace FigmaRuntime
{
    public class RenderNode : IRenderNode
    {
        private readonly VisualElement original;

        private VisualElement animation;

        public VisualElement Element =>
            animation ?? original;

        public VisualElement Original =>
            original;

        public VisualElement Animation =>
            animation;

        public bool HasAnimation =>
            animation != null;

        public RenderNode(
            VisualElement element)
        {
            original = element;
        }

        public void SetAnimation(
            VisualElement element)
        {
            animation = element;
        }

        public void ClearAnimation()
        {
            animation = null;
        }

        public void Show()
        {
            Element.style.visibility =
                Visibility.Visible;
        }

        public void Hide()
        {
            Element.style.visibility =
                Visibility.Hidden;
        }
    }
}