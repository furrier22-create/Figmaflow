using UnityEngine.UIElements;

namespace FigmaRuntime
{
    public interface IRenderNode
    {
        VisualElement Element { get; }

        VisualElement Original { get; }

        VisualElement Animation { get; }

        bool HasAnimation { get; }

        void SetAnimation(
            VisualElement element);

        void ClearAnimation();

        void Show();

        void Hide();
    }
}