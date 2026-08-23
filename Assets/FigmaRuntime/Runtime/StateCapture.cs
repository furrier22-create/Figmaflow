using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    public static class StateCapture
    {
        public static FigmaStateAsset Capture(
            VisualElement root,
            string stateName)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(
                $"[FigmaFlow] Capture: {stateName} world={root.worldBound}");
#endif

            var state =
                ScriptableObject.CreateInstance<FigmaStateAsset>();

            state.stateName = stateName;

            VisualElement scanRoot =
                VisualTreeUtility.GetLogicalRoot(root);

            foreach (var child in scanRoot.Children())
            {
                Scan(
                    child,
                    "",
                    state);
            }

            return state;
        }

        static void Scan(
            VisualElement element,
            string parentPath,
            FigmaStateAsset state)
        {
            if (string.IsNullOrEmpty(element.name))
            {
                foreach (var child in element.Children())
                {
                    Scan(
                        child,
                        parentPath,
                        state);
                }

                return;
            }

            string path =
                string.IsNullOrEmpty(parentPath)
                ? element.name
                : parentPath + "/" + element.name;

            ElementData e = new();

            e.name = element.name;
            e.path = path;

            e.worldRect = element.worldBound;

            e.left = element.resolvedStyle.left;
            e.top = element.resolvedStyle.top;

            e.width = element.resolvedStyle.width;
            e.height = element.resolvedStyle.height;

            e.opacity = element.resolvedStyle.opacity;

            // F8 zárás (v0.5, ADR-023): a rotation rögzítése a resolvedStyle
            // valódi értékéből (a UI Builder rotate / Figma export).
            // A scale továbbra is 1 (a ScaleTrack külön feladat).
            e.rotation =
                element.resolvedStyle.rotate.angle.value;

            e.scale = Vector2.one;

            state.elements.Add(e);

            foreach (var child in element.Children())
            {
                Scan(
                    child,
                    path,
                    state);
            }
        }
    }
}