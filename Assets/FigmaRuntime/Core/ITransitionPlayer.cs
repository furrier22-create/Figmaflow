using System;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.4 (ADR-011): a Finished esemény interfészre emelve —
    /// a StateMachine player-agnostic (Layout és Overlay player is
    /// ugyanúgy használható, konkrét típus-ellenőrzés nélkül).
    ///
    /// A TransitionPlayer és az OverlayTransitionPlayer meglévő
    /// publikus eseménye implicit implementálja ezt a tagot —
    /// a freeze-elt fájlokat nem kellett módosítani.
    /// </summary>
    public interface ITransitionPlayer
    {
        void PlayForward(
            Transition transition,
            float duration);

        void PlayBackward(
            Transition transition,
            float duration);

        bool IsPlaying { get; }

        event Action Finished;
    }
}
