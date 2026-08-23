using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    [RequireComponent(typeof(UIDocument))]
    public class OverlayTransitionPlayer :
        MonoBehaviour,
        ITransitionPlayer
    {
        UIDocument document;

        OverlayBinder binder;

        OverlayRuntime runtime;

        Coroutine currentRoutine;

        public bool IsPlaying { get; private set; }

        public event Action Finished;

        void Awake()
        {
            document = GetComponent<UIDocument>();

            runtime = GetComponent<OverlayRuntime>();

            binder = new OverlayBinder(
                document.rootVisualElement);
        }

        public void PlayForward(
            Transition transition,
            float duration)
        {
            Play(
                transition,
                false);
        }

        public void PlayBackward(
            Transition transition,
            float duration)
        {
            Play(
                transition,
                true);
        }

        void Play(
            Transition transition,
            bool reverse)
        {
            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine =
                StartCoroutine(
                    Routine(
                        transition,
                        reverse));
        }

        IEnumerator Routine(
            Transition transition,
            bool reverse)
        {
            IsPlaying = true;

            // F6 (ADR-007, v0.4): a párok a renderData-ból jönnek,
            // nem a trackekből. A binder a párok láthatóságát kezeli.
            binder.Bind(
                runtime != null
                    ? runtime.RenderData
                    : null);

            float timer = 0f;

            float total = transition.Duration;

            while (timer < total)
            {
                timer += Time.deltaTime;

                float time =
                    reverse
                    ? total - timer
                    : timer;

                transition.Evaluate(time);

                yield return null;
            }

            transition.Evaluate(
                reverse ? 0 : total);

            binder.Unbind();

            IsPlaying = false;

            currentRoutine = null;

            Finished?.Invoke();
        }
    }
}
