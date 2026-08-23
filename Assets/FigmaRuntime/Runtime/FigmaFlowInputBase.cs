using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.5 (ADR-025): az input-kezelők közös alapja.
    ///
    /// A demók eddig külön-külön implementálták a robusztus mintát:
    /// - a GoToState hívás EGY KÉPKOCKÁVAL KÉSLELTETVE (a ClickEvent
    ///   dispatch közbeni fa-csere újra-dispatch-et okozna — "szellem-
    ///   kattintás", ami a dialogt magától bezárná)
    /// - a nyitás utáni cooldown (a bezáró akciók védőhálója)
    /// - a kattintás = aktivitás (az idle-időzítő resetje)
    ///
    /// Ez az alap ezt a közös szerkezetet adja — a származtatott
    /// osztályok csak a saját gomb-logikájukat (OnClick) írják meg.
    ///
    /// A kattintás a PlayMaker-nek is jelez (Activate — az interfészen
    /// keresztül, nem ClickEvent-függő).
    /// </summary>
    public abstract class FigmaFlowInputBase :
        MonoBehaviour,
        IFigmaFlowInput
    {
        [Tooltip("A vezérelt StateMachine.")]
        public FigmaStateMachine machine;

        public FigmaStateMachine Machine => machine;

        private float openTime = -999f;

        private bool bound;

        /// <summary>
        /// Az utolsó aktivitás időpontja (Time.time) — a PlayMaker
        /// pollozza (ADR-025).
        /// </summary>
        public float LastActivationTime { get; private set; } = -999f;

        // ---- IFigmaFlowInput ----

        public void Bind()
        {
            if (bound)
                return;

            UIDocument doc = GetComponent<UIDocument>();

            if (doc == null || doc.rootVisualElement == null)
                return;

            doc.rootVisualElement.RegisterCallback<ClickEvent>(OnClick);

            bound = true;
        }

        public void Unbind()
        {
            if (!bound)
                return;

            UIDocument doc = GetComponent<UIDocument>();

            if (doc != null && doc.rootVisualElement != null)
                doc.rootVisualElement.UnregisterCallback<ClickEvent>(OnClick);

            bound = false;
        }

        public void Activate()
        {
            openTime = Time.time;

            LastActivationTime = Time.time;
        }

        // ---- MonoBehaviour ----

        void OnEnable()
        {
            Bind();
        }

        void OnDisable()
        {
            Unbind();
        }

        // ---- A demók által felülírandó ----

        protected abstract void OnClick(ClickEvent evt);

        // ---- Közös segédek (a demók ezeket használják) ----

        /// <summary>
        /// A nyitás utáni cooldown — a szellem-kattintás ellen.
        /// </summary>
        protected bool CloseCooldownPassed()
        {
            return Time.time - openTime > 1f;
        }

        /// <summary>
        /// A GoToState EGY KÉPKOCKÁVAL KÉSLELTETVE — a ClickEvent
        /// dispatch közbeni fa-csere okozná a szellem-kattintást.
        /// </summary>
        protected void DeferGoTo(int state)
        {
            StartCoroutine(DeferredGoToRoutine(state));
        }

        IEnumerator DeferredGoToRoutine(int state)
        {
            yield return null;

            if (machine != null)
                machine.GoToState(state);
        }
    }
}
