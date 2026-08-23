using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Samples
{
    /// <summary>
    /// Dialog demo interakció (v0.4 / P4-2).
    ///
    /// Egyetlen root szintű ClickEvent figyelő vezérli a két állapotot:
    /// - OpenDialog gomb      → GoToState(1) — dialog megnyitás
    /// - CloseDialog gomb     → GoToState(0) — dialog bezárás
    /// - Backdrop kattintás   → GoToState(0) — kattintás a háttérre
    ///
    /// A DialogPanel barrier: a panelen belüli kattintás (a bezáró
    /// gombon kívül) nem zár.
    ///
    /// GYÖKÉR-OK / MEGOLDÁS (2026-08-02, második javítás):
    /// A "megnyitás után magától bezáródik" oka: a GoToState a ClickEvent
    /// DISPATCH KÖZBEN cseréli a vizuális fát. A Unity ilyenkor a függőben
    /// lévő pointer-eseményt ÚJRA dispatch-eli az új fa azon elemére, ami
    /// a kurzor alatt van (a dialog panel/backdrop) → a handler "Bezárás"
    /// kattintást lát → azonnal bezár.
    ///
    /// Megoldás: a GoToState hívás EGY KÉPKOCKÁVAL KÉSLELTETVE történik
    /// (DeferGoTo) — az esemény így tisztán lefut a régi fán, a csere
    /// utána történik, nincs újra-dispatch. Emellett a nyitás utáni 1
    /// másodpercben a bezáró akciók (CloseDialog/Backdrop) figyelmen
    /// kívül maradnak (védőháló), és a kattintások logolva vannak (a
    /// hibakereséshez).
    ///
    /// Nem módosítja a Runtime-ot — csak demó eseménykezelés.
    /// </summary>
    public class DialogDemoInput : MonoBehaviour
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

            // Kilépéskor (teardown) a root már null lehet — ez volt a
            // NullReferenceException forrása.
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
                if (current.name == "DialogPanel")
                    return; // barrier — a panelen belüli kattintás nem zár

                switch (current.name)
                {
                    case "OpenDialog":
                        if (state == 0)
                        {
                            Debug.Log("[FigmaFlowDemo] OpenDialog -> GoToState(1)");

                            openTime = Time.time;

                            DeferGoTo(1);
                        }

                        return;

                    case "CloseDialog":
                        if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log("[FigmaFlowDemo] CloseDialog -> GoToState(0)");

                            DeferGoTo(0);
                        }
                        else if (state == 1)
                        {
                            Debug.Log("[FigmaFlowDemo] CloseDialog: cooldown, ignorálva");
                        }

                        return;

                    case "Backdrop":
                        if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log("[FigmaFlowDemo] Backdrop -> GoToState(0)");

                            DeferGoTo(0);
                        }
                        else if (state == 1)
                        {
                            Debug.Log("[FigmaFlowDemo] Backdrop: cooldown, ignorálva");
                        }

                        return;
                }

                current = current.parent;
            }
        }

        /// <summary>
        /// A nyitás utáni rövid ablakban nem fogadunk el bezárást —
        /// védőháló a fa-csere miatti újra-dispatch ellen.
        /// </summary>
        bool CloseCooldownPassed()
        {
            return Time.time - openTime > 1f;
        }

        /// <summary>
        /// A GoToState EGY KÉPKOCKÁVAL KÉSLELTETVE — a ClickEvent dispatch
        /// közbeni fa-csere okozná az újra-dispatch-et ("szellem-kattintás").
        /// </summary>
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
