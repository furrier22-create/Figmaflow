// ============================================================
// MOTION ENGINE FREEZE (v0.3.7)
// Ez a fájl a Motion Engine része. Stabil, bizonyítottan működik.
// Csak KRITIKUS BUG esetén módosítható, dokumentált döntéssel.
// Minden más fejlesztés ezen a fájlon kívül épüljön.
// ============================================================

using System;
using System.Collections;
using UnityEngine;

namespace FigmaRuntime
{
    public class TransitionPlayer : MonoBehaviour, ITransitionPlayer
    {
        private Coroutine currentRoutine;

        public bool IsPlaying { get; private set; }

        public event Action Finished;

        public void PlayForward(
            Transition transition,
            float duration)
        {
            Play(transition, false);
        }

        public void PlayBackward(
            Transition transition,
            float duration)
        {
            Play(transition, true);
        }

        private void Play(
            Transition transition,
            bool reverse)
        {
            if (transition == null)
                return;

            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine =
                StartCoroutine(
                    PlayRoutine(
                        transition,
                        reverse));
        }

        private IEnumerator PlayRoutine(
            Transition transition,
            bool reverse)
        {
            IsPlaying = true;

            transition.Reset();

            float timer = 0f;
            float totalDuration = transition.Duration;

            while (timer < totalDuration)
            {
                timer += Time.deltaTime;

                float time = reverse
                    ? totalDuration - timer
                    : timer;

                transition.Evaluate(time);

                yield return null;
            }

            transition.Evaluate(
                reverse ? 0f : totalDuration);

            IsPlaying = false;
            currentRoutine = null;

            Finished?.Invoke();
        }
    }
}