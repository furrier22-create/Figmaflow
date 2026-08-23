using System;
using System.Collections.Generic;
using UnityEngine;

namespace FigmaRuntime
{
    /// <summary>
    /// Editor-oldalon előállított state cache (v0.4 / P3, ADR-009).
    ///
    /// A Runtime ebből tölti a state-eket — nem épít cache-t futásidőben,
    /// így a Startup Flash gyökeresen megszűnik (a HideRoot workaround
    /// nem kell, ha a cache érvényes).
    ///
    /// Előállítás: Editor/StateCacheExporter (Motion Studio → State Cache).
    /// </summary>
    [CreateAssetMenu(menuName = "FigmaFlow/State Cache")]
    public class FigmaStateCacheAsset : ScriptableObject
    {
        /// <summary>
        /// Az a scene, amelyikhez ez a cache készült (ADR-010).
        /// Több scene esetén a runtime figyelmeztet, ha nem egyezik.
        /// </summary>
        public string sceneName;

        public List<CachedState> states = new();
    }

    /// <summary>
    /// Egyetlen state pillanatképe.
    /// Inline tárolás (nincs sub-asset) — egyszerű és robusztus.
    /// </summary>
    [Serializable]
    public class CachedState
    {
        public string stateName;

        public List<ElementData> elements = new();
    }
}
