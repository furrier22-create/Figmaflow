using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Samples
{
    /// <summary>
    /// Dropdown demo interakció (v0.6/3, ADR-028).
    ///
    /// A Popup mintájára, de az opciók a gomb ALATT jelennek meg
    /// (lenyíló menü), és a DropdownMenu AppearMode-ja SCALE — a
    /// menü kicsinyről a teljes méretére nő.
    ///
    /// - DropdownButton → nyitás (GoToState(1))
    /// - opció kiválasztása → bezárás + a kiválasztás a Selection labelbe
    /// - kattintás a háttérre (Screen) → bezárás (click-away)
    /// - a gomb toggle: nyitva állapotban rákattintva bezár
    ///
    /// Ugyanaz a robusztus minta: deferred GoToState + cooldown + logok.
    /// </summary>
    public class DropdownDemoInput : MonoBehaviour
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
                // Opciók: kiválasztás → bezárás + a szöveg a labelbe.
                switch (current.name)
                {
                    case "OptionRed":
                    case "OptionGreen":
                    case "OptionBlue":
                        if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log($"[FigmaFlowDemo] {current.name} kiválasztva -> bezárás");

                            SetSelection(current.name.Replace("Option", ""));

                            DeferGoTo(0);
                        }

                        return;

                    case "DropdownButton":
                        // Toggle: zárva → nyit, nyitva → zár.
                        if (state == 0)
                        {
                            Debug.Log("[FigmaFlowDemo] DropdownButton -> GoToState(1)");

                            openTime = Time.time;

                            DeferGoTo(1);
                        }
                        else if (state == 1 && CloseCooldownPassed())
                        {
                            Debug.Log("[FigmaFlowDemo] DropdownButton (toggle) -> GoToState(0)");

                            DeferGoTo(0);
                        }

                        return;

                    case "DropdownMenu":
                        // Barrier: a menün belüli kattintás (a gombokon kívül) nem zár.
                        return;
                }

                // Click-away: kattintás a háttérre nyitott állapotban.
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

        void SetSelection(string text)
        {
            UIDocument document = GetComponent<UIDocument>();

            if (document == null)
                return;

            Label label =
                document.rootVisualElement.Q<Label>("Selection");

            if (label != null)
                label.text = "Kiválasztva: " + text;
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
