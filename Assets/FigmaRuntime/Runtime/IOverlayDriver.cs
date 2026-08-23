using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.4 / P4 (ADR-012) — Overlay driver seam.
    ///
    /// A Stable FigmaRuntime nem hivatkozhat az Experimentalre (asmdef),
    /// ezért a FigmaStateMachine ezen az interfészen keresztül éri el
    /// az Overlay pipeline-t. Az implementáció az Experimentalben él
    /// (OverlayRuntime).
    ///
    /// Ha a jeleneten nincs IOverlayDriver (vagy a profil nem Overlay
    /// módot kér), a StateMachine a Layout úton dolgozik — változatlanul.
    /// </summary>
    public interface IOverlayDriver
    {
        /// <summary>
        /// A driver használható-e (komponensei jelen vannak-e).
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Az overlay lejátszó — a StateMachine a Finished eseményére
        /// iratkozik fel rajta keresztül (ITransitionPlayer, ADR-011).
        /// </summary>
        ITransitionPlayer Player { get; }

        /// <summary>
        /// A két élő UI (current / target) előkészítése + render párok.
        /// Hamis visszatérés: a StateMachine Layout útra esik vissza.
        /// </summary>
        bool Prepare(
            VisualTreeAsset currentTree,
            VisualTreeAsset targetTree,
            TransitionAnalysis analysis);

        /// <summary>
        /// v0.5 / OverlayLayer (ADR-016): az animációs reprezentáció
        /// (proxy-k) létrehozása a layer-en. A Prepare UTÁNI layout-
        /// várakozás után hívandó (a resolvedStyle csak ekkor érvényes).
        /// Hamis visszatérés: a trackek az eredeti elemeken futnak
        /// (graceful fallback).
        /// </summary>
        bool PrepareAnimationLayer(TransitionAnalysis analysis);

        /// <summary>
        /// Overlay transition létrehozása (a Layout Build helyett).
        /// </summary>
        Transition BuildTransition(
            TransitionAnalysis analysis,
            TransitionAsset asset);

        /// <summary>
        /// Overlay commit — a rétegek összeolvasztása (a Layout
        /// CommitState helyett).
        /// </summary>
        void Commit(VisualTreeAsset nextTarget);
    }
}
