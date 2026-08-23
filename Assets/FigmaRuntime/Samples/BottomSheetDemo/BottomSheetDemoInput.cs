using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Samples
{
    /// <summary>
    /// Bottom Sheet demo interakció (v0.4 / P5, ADR-015).
    ///
    /// Ugyanaz a minta, mint a DialogDemoInput:
    /// - OpenSheet gomb      → GoToState(1) — sheet megnyitás
    /// - CloseSheet gomb     → GoToState(0) — sheet bezárás
    /// - Backdrop kattintás  → GoToState(0) — kattintás a háttérre
    /// - BottomSheetPanel barrier: a panelen belüli kattintás (a
    ///   bezáró gombon kívül) nem zár
    ///
    /// Késleltetett GoToState (dispatch közbeni fa-csere ellen) +
    /// nyitás utáni cooldown + [FigmaFlowDemo] logok — lásd
    /// DialogDemoInput osztály-dokumentációját.
    ///
    /// Nem módosítja a Runtime-ot — csak demó eseménykezelés.
    /// </summary>
    public class BottomSheetDemoInput : MonoBehaviour
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
                if (current.name == "BottomSheetPanel")
                    return; // barrier — a panelen belüli kattintás nem zár

                switch (current.name)
                {
                    case "OpenSheet":
                        if (state == 0)
                        {
                            Debug.Log("[FigmaFlowDemo] OpenSheet -> GoToState(1)");

                            openTime = Time.time;

                            DeferGoTo(1);
                        }

                        return;

                    case "CloseSheet":
                        if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log("[FigmaFlowDemo] CloseSheet -> GoToState(0)");

                            DeferGoTo(0);
                        }

                        return;

                    case "Backdrop":
                        if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log("[FigmaFlowDemo] Backdrop -> GoToState(0)");

                            DeferGoTo(0);
                        }

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
