using System;
using System.Collections.Generic;
using UnityEngine;

namespace FigmaRuntime
{
    [CreateAssetMenu(menuName = "FigmaFlow/State")]
    public class FigmaStateAsset : ScriptableObject
    {
        public string stateName;

        public List<ElementData> elements = new();
    }

    [Serializable]
    public class ElementData
    {
        public string name;

        public string path;

        // Render
        public Rect worldRect;

        // Layout (Style)
        public float left;
        public float top;

        public float width;
        public float height;

        // Visual
        public float opacity;

        public float rotation;

        public Vector2 scale = Vector2.one;
    }
}
