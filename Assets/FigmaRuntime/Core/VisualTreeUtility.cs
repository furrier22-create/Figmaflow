using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// A VisualElement hierarchia közös segédosztálya.
    ///
    /// Minden runtime komponens ugyanazt a
    /// logikai gyökeret és útvonal-kezelést használja.
    /// </summary>
    public static class VisualTreeUtility
    {
        /// <summary>
        /// A Figma logikai gyökere.
        ///
        /// A TemplateContainer alatt általában
        /// egyetlen Desktop/Root elem található.
        /// A runtime mindenhol innen dolgozik.
        /// </summary>
        public static VisualElement GetLogicalRoot(
            VisualElement root)
        {
            if (root == null)
                return null;

            // TemplateContainer -> Desktop
            if (root.childCount == 1)
                return root[0];

            return root;
        }

        /// <summary>
        /// Path alapú keresés.
        ///
        /// A StateCapture path-építése a névtelen elemeket átugorja
        /// (lásd StateCapture.Scan). Ez a keresés ugyanazt a szemantikát
        /// követi: a névtelen elemek átlátszóak a navigációban.
        /// </summary>
        public static VisualElement FindByPath(
            VisualElement root,
            string path)
        {
            root = GetLogicalRoot(root);

            if (root == null)
                return null;

            if (string.IsNullOrWhiteSpace(path))
                return null;

            string[] parts = path.Split('/');

            VisualElement current = root;

            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                current = FindNextNamed(
                    current,
                    part);

                if (current == null)
                    return null;
            }

            return current;
        }

        /// <summary>
        /// A következő elnevezett elem keresése, a névtelen
        /// köztes elemek átugrásával.
        ///
        /// Elsőbbség: közvetlen, elnevezett gyermek.
        /// Ha nincs, a névtelen gyermekekben folytatja a keresést.
        /// </summary>
        static VisualElement FindNextNamed(
            VisualElement parent,
            string part)
        {
            if (parent == null)
                return null;

            foreach (VisualElement child in parent.Children())
            {
                if (child.name == part)
                    return child;
            }

            foreach (VisualElement child in parent.Children())
            {
                if (string.IsNullOrEmpty(child.name))
                {
                    VisualElement found =
                        FindNextNamed(
                            child,
                            part);

                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Közvetlen gyermek keresése név alapján.
        /// </summary>
        public static VisualElement FindDirectChild(
            VisualElement parent,
            string childName)
        {
            if (parent == null)
                return null;

            foreach (VisualElement child in parent.Children())
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        /// <summary>
        /// Runtime útvonal elõállítása.
        /// </summary>
        public static string GetPath(
            VisualElement element)
        {
            if (element == null)
                return string.Empty;

            string path = element.name;

            VisualElement current = element.parent;

            while (current != null)
            {
                if (!string.IsNullOrEmpty(current.name))
                {
                    path = current.name + "/" + path;
                }

                current = current.parent;
            }

            return path;
        }
    }
}