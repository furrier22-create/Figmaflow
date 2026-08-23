using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Samples
{
    /// <summary>
    /// Popup demo interakció (v0.4 / P5/2).
    ///
    /// A popup a Dialog/Bottom Sheet mintától eltérő eset:
    /// - NINCS sötétítő háttér (nem kell Backdrop)
    /// - kattintás a háttérre (Screen / TopBar) → bezárás (click-away)
    /// - menüpont kiválasztása → bezárás
    /// - a "Menü" gomb TOGGLE: nyitva állapotban rákattintva bezár
    ///
    /// A megjelenés a meglévő Fade AppearMode-dal megy (a MenuPanel
    /// fade-in) — új engine-funkció nélkül. (A popup "scale-in" hatás
    /// egy jövőbeli AppearMode lehet — most nem kell.)
    ///
    /// Ugyanaz a robusztus minta: késleltetett GoToState (dispatch
    /// közbeni fa-csere ellen) + nyitás utáni cooldown + logok.
    ///
    /// Nem módosítja a Runtime-ot — csak demó eseménykezelés.
    /// </summary>
    public class PopupMenuDemoInput : MonoBehaviour
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
                // Menüpontok: kiválasztás → bezárás.
                switch (current.name)
                {
                    case "OptionProfile":
                    case "OptionSettings":
                    case "OptionLogout":
                        if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log($"[FigmaFlowDemo] {current.name} kiválasztva -> bezárás");

                            DeferGoTo(0);
                        }

                        return;

                    case "OpenMenu":
                        // Toggle: zárva → nyit, nyitva → zár.
                        if (state == 0)
                        {
                            Debug.Log("[FigmaFlowDemo] OpenMenu -> GoToState(1)");

                            openTime = Time.time;

                            DeferGoTo(1);
                        }
                        else if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log("[FigmaFlowDemo] OpenMenu (toggle) -> GoToState(0)");

                            DeferGoTo(0);
                        }

                        return;
                }

                // A menüpanel barrier: a panelen belüli kattintás
                // (a gombokon kívül) nem zár.
                if (current.name == "MenuPanel")
                    return;

                // Click-away: kattintás a háttérre (Screen / TopBar)
                // a nyitott állapotban → bezárás.
                if (state == 1 &&
                    CloseCooldownPassed() &&
                    (current.name == "Screen" || current.name == "TopBar"))
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
