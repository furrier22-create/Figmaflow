using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Samples
{
    /// <summary>
    /// Hero demo interakció (v0.5 / ADR-018).
    ///
    /// A "shared element" (Hero) esete: a HeroCard mindkét állapotban
    /// létezik — a layer-en a proxy átrepül és átméreteződik.
    ///
    /// - HeroCard kattintás (zárva) → megnyitás (GoToState(1))
    /// - HeroCard / CloseHero kattintás (nyitva) → bezárás (GoToState(0))
    /// - kattintás a háttérre (nyitva) → bezárás (click-away)
    ///
    /// Ugyanaz a robusztus minta: késleltetett GoToState + cooldown + logok.
    ///
    /// Nem módosítja a Runtime-ot — csak demó eseménykezelés.
    /// </summary>
    public class HeroDemoInput : MonoBehaviour
    {
        public FigmaStateMachine machine;

        private float openTime = -999f;

        void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();

            if (document == null)
                return;

            document.rootVisualElement.RegisterCallback<ClickEvent>(OnClick);
        }

        void OnDisable()
        {
            UIDocument document = GetComponent<UIDocument>();

            if (document == null)
                return;

            if (document.rootVisualElement == null)
                return;

            document.rootVisualElement.UnregisterCallback<ClickEvent>(OnClick);
        }

        void OnClick(ClickEvent evt)
        {
            if (machine == null)
                return;

            int state = machine.GetCurrentStateIndex();

            VisualElement current = evt.target as VisualElement;

            while (current != null)
            {
                switch (current.name)
                {
                    case "HeroCard":
                        if (state == 0)
                        {
                            Debug.Log("[FigmaFlowDemo] HeroCard -> GoToState(1)");

                            openTime = Time.time;

                            DeferGoTo(1);
                        }
                        else if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log("[FigmaFlowDemo] HeroCard -> GoToState(0)");

                            DeferGoTo(0);
                        }

                        return;

                    case "CloseHero":
                        if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log("[FigmaFlowDemo] CloseHero -> GoToState(0)");

                            DeferGoTo(0);
                        }

                        return;
                }

                if (state == 1 &&
                    CloseCooldownPassed() &&
                    current.name == "Screen")
                {
                    Debug.Log("[FigmaFlowDemo] Kattintás a háttérre -> bezárás");

                    DeferGoTo(0);

                    return;
                }

                current = current.parent;
            }
        }

        bool CloseCooldownPassed()
        {
            return Time.time - openTime > 1f;
        }

        void DeferGoTo(int state)
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
