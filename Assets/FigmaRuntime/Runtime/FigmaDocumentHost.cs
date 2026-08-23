using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    [RequireComponent(typeof(UIDocument))]
    public class FigmaDocumentHost : MonoBehaviour
    {
        private UIDocument document;

        private VisualElement root;

        private TemplateContainer current;

        public UIDocument Document => document;

        public VisualElement Root => root;

        public TemplateContainer Current => current;

        void Awake()
        {
            document = GetComponent<UIDocument>();
            root = document.rootVisualElement;
        }

        /// <summary>
        /// Layout m�d bet�lt�se.
        /// Overlay m�dban ezt nem haszn�ljuk majd az anim�ci�hoz,
        /// de a jelenlegi m�k�d�st v�ltozatlanul hagyjuk.
        /// </summary>
        public TemplateContainer Load(VisualTreeAsset tree)
        {
            if (tree == null)
                return null;

            if (current != null && current.parent == root)
            {
                root.Remove(current);
            }

            current = tree.Instantiate();

            root.Add(current);

#if UNITY_EDITOR
            Debug.Log(
                $"[FigmaFlow] Load: {tree.name}, root children: {root.childCount}");

            foreach (var child in root.Children())
            {
                Debug.Log(
                    $"[FigmaFlow]   Child: {child.GetType().Name} name={child.name}");
            }
#endif

            return current;
        }

        /// <summary>
        /// OverlayScene haszn�lhatja ezt, hogy a jelenlegi
        /// TemplateContainert szinkronban tartsa a Hosttal.
        /// </summary>
        public void SetCurrent(TemplateContainer container)
        {
            current = container;
        }

        /// <summary>
        /// Anim�ci� v�g�n megh�vjuk.
        /// Layout m�dban egyszer�en lecser�li a teljes UI-t.
        /// </summary>
        public TemplateContainer CommitState(VisualTreeAsset tree)
        {
            return Load(tree);
        }

        /// <summary>
        /// A root teljes �r�t�se.
        /// Overlay m�d inicializ�l�s�n�l lesz haszn�lva.
        /// </summary>
        public void Clear()
        {
            root.Clear();
            current = null;
        }

        public void HideRoot()
        {
            root.style.visibility = Visibility.Hidden;
        }

        public void ShowRoot()
        {
            root.style.visibility = Visibility.Visible;
        }
    }
}