using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.5/6 (ADR-022): a bemutató-flow automatizálása — "PlayMaker".
    ///
    /// A FigmaStateMachine mellé helyezve:
    /// - autoAdvanceSeconds: állapotonkénti időzített váltás (0 = ki)
    /// - loop: a szekvencia ciklikus (az utolsó után az első)
    /// - idleReturnSeconds: inaktivitás utáni visszatérés (0 = ki)
    /// - idleReturnState: a visszatérés célállapota (általában a főmenü, 0)
    ///
    /// JAVÍTÁS (2026-08-02): az idle-return NEM blokkolja az autoAdvance-t —
    /// az idle NEM nullázza a stateTimer-t, hacsak ténylegesen vissza nem
    /// váltott (az auto és az idle független időzítők). FIGYELEM: ha az
    /// idleReturnSeconds RÖVIDEBB, mint az autoAdvanceSeconds, az idle
    /// mindig visszahúzza a bemutatót (az auto sosem vált) — az idle-t
    /// HOSSZABBRA állítsd, mint az auto-t.
    ///
    /// Az inaktivitás-észlelés AUTOMATIKUS: a root ClickEvent bármilyen
    /// kattintást (gomb, háttér) az idle-időzítő resetjeként értelmez.
    /// Külső input (érintőfal, lidar) a Ping()-pel jelezhet.
    ///
    /// NEM módosítja a Motion Engine-t (freeze tartva).
    /// </summary>
    public class FigmaFlowPlaymaker : MonoBehaviour
    {
        [Header("Playmaker (v0.5)")]

        [Tooltip("A StateMachine, amit vezérel.")]
        public FigmaStateMachine machine;

        [Tooltip("Főkapcsoló.")]
        public bool running = true;

        [Tooltip("Másodperc állapotonként (0 = kikapcsolva).")]
        public float autoAdvanceSeconds = 0f;

        [Tooltip("A szekvencia ciklikus (az utolsó után az első).")]
        public bool loop = false;

        [Tooltip("A loop tartomány eleje (0-alapú). Csak ettől az állapottól pörög.")]
        public int loopStartIndex = 0;

        [Tooltip("A loop tartomány vége (0-alapú, -1 = az utolsó). Csak eddig az állapotig pörög.")]
        public int loopEndIndex = -1;

        [Tooltip("Inaktivitás utáni visszatérés másodpercben (0 = kikapcsolva).")]
        public float idleReturnSeconds = 0f;

        [Tooltip("A visszatérés célállapota (általában a főmenü, 0).")]
        public int idleReturnState = 0;

        private float stateTimer;

        private float idleTimer;

        private bool warnedAutoOff;

        private bool warnedIdleShorter;

        // v0.5 (ADR-025): a kattintás-észlelés az IFigmaFlowInput-ra épül —
        // a komponens a saját UIDocument-én keresi az input-kezelőt, és
        // annak Activate() jeleit figyeli (nem direkt ClickEvent).
        private IFigmaFlowInput input;

        private bool inputBound;

        // ------------------------------------------------------------

        void OnEnable()
        {
            FindInput();

            // Diagnosztika (egyszer): a gép állapota.
            if (machine != null)
            {
                int sc = machine.states != null ? machine.states.Length : 0;

                int endIdx =
                    loopEndIndex >= 0
                        ? Mathf.Min(loopEndIndex, sc - 1)
                        : sc - 1;

                Debug.Log(
                    $"[FigmaFlow] Playmaker: gép '{machine.gameObject.name}' — " +
                    $"states.Length={sc}, currentState={machine.GetCurrentStateIndex()}, " +
                    $"loop-tartomány {Mathf.Clamp(loopStartIndex, 0, endIdx)}-{endIdx}.");
            }
            else
            {
                Debug.LogWarning(
                    "[FigmaFlow] Playmaker: a 'machine' mező NINCS beállítva — " +
                    "a PlayMaker nem működik.");
            }
        }

        void OnDisable()
        {
            UnbindInput();
        }

        void FindInput()
        {
            if (inputBound)
                return;

            input = GetComponent<IFigmaFlowInput>();

            if (input != null)
            {
                input.Bind();

                inputBound = true;

                return;
            }

            // FALLBACK (2026-08-02): ha nincs IFigmaFlowInput a gépen
            // (a demó input-kezelők még a régi formában vannak), a
            // PlayMaker a SAJÁT UIDocument-jén figyeli a kattintást —
            // így az idle-reset mindenképp működik.
            UIDocument doc = GetComponent<UIDocument>();

            if (doc != null && doc.rootVisualElement != null)
            {
                doc.rootVisualElement.RegisterCallback<ClickEvent>(
                    OnFallbackClick);

                inputBound = true;
            }
        }

        void UnbindInput()
        {
            if (!inputBound)
                return;

            input?.Unbind();

            input = null;

            UIDocument doc = GetComponent<UIDocument>();

            if (doc != null && doc.rootVisualElement != null)
                doc.rootVisualElement.UnregisterCallback<ClickEvent>(
                    OnFallbackClick);

            inputBound = false;
        }

        void OnFallbackClick(ClickEvent evt)
        {
            // Bármilyen kattintás = aktivitás → az idle-időzítő reset.
            idleTimer = 0f;
        }

        private float lastSeenActivation = -999f;

        void PollInput()
        {
            if (input == null)
                return;

            float t = input.LastActivationTime;

            if (t > lastSeenActivation)
            {
                lastSeenActivation = t;

                // Bármilyen aktivitás (kattintás vagy külső input) →
                // az idle-időzítő reset.
                idleTimer = 0f;
            }
        }

        /// <summary>
        /// A jelenlegi input-állapot (a demó input-kezelőkhöz / diagnosztikához).
        /// </summary>
        public bool HasInputBinding => inputBound;

        // ------------------------------------------------------------

        void Update()
        {
            if (!running || machine == null)
                return;

            // Az input keresése (ha még nincs — a komponens sorrend
            // miatt késhet).
            FindInput();

            // Az aktivitás pollozása (a kattintás / külső input
            // idle-resetje).
            PollInput();

            // Diagnosztika (egyszer).
            if (!warnedAutoOff &&
                autoAdvanceSeconds <= 0f &&
                idleReturnSeconds > 0f)
            {
                warnedAutoOff = true;

                Debug.LogWarning(
                    "[FigmaFlow] Playmaker: autoAdvanceSeconds nincs beállítva (0) — " +
                    "az időzített váltás nem fut, csak az idle-return.");
            }

            if (!warnedIdleShorter &&
                idleReturnSeconds > 0f &&
                autoAdvanceSeconds > 0f &&
                idleReturnSeconds <= autoAdvanceSeconds)
            {
                warnedIdleShorter = true;

                Debug.LogWarning(
                    $"[FigmaFlow] Playmaker: az idleReturnSeconds ({idleReturnSeconds:F0}) " +
                    $"NEM hosszabb, mint az autoAdvanceSeconds ({autoAdvanceSeconds:F0}) — " +
                    "az idle (a kód sorrendje miatt) mindig visszahúzza a bemutatót, " +
                    "az időzített váltás sosem lép tovább a következő állapotra. " +
                    "Állítsd az idle-t HOSSZABBRA, mint az auto-t (pl. auto=5s, idle=60s).");
            }

            float dt = Time.deltaTime;

            stateTimer += dt;
            idleTimer += dt;

            // Idle-visszatérés (2026-08-02, kioszk-logika): CSAK akkor
            // szakít, ha a bemutató "áll" —
            // (1) az auto ki van kapcsolva, VAGY
            // (2) a gép a loop-tartományon KÍVÜL van (pl. főmenü).
            // A tartományon belül az auto-advance él — az idle NEM
            // ránthatja vissza (különben a rövid idle (<= auto) örök
            // 0-1 loopot okozna, hiába áll a loop 0-2-re).
            if (idleReturnSeconds > 0f &&
                idleTimer >= idleReturnSeconds)
            {
                idleTimer = 0f;

                int cur = machine.GetCurrentStateIndex();

                GetLoopRange(out int startIdx, out int endIdx);

                bool outsideRange = cur < startIdx || cur > endIdx;

                bool presentationAlive =
                    autoAdvanceSeconds > 0f && !outsideRange;

                if (presentationAlive)
                {
                    Debug.Log(
                        $"[FigmaFlow] Playmaker: idle lejárt, de a bemutató " +
                        $"a tartományban ({cur}) — az idle nem szakítja meg.");
                }
                else if (cur != idleReturnState)
                {
                    stateTimer = 0f; // csak tényleges visszaváltáskor

                    Debug.Log(
                        $"[FigmaFlow] Playmaker: idle ({idleReturnSeconds:F0}s) -> " +
                        $"állapot {idleReturnState}.");

                    machine.GoToState(idleReturnState);
                }
            }

            // Időzített váltás.
            if (autoAdvanceSeconds > 0f &&
                stateTimer >= autoAdvanceSeconds)
            {
                stateTimer = 0f;

                Advance();
            }
        }

        /// <summary>
        /// A loop-tartomány kiszámítása (loopStartIndex..loopEndIndex).
        /// </summary>
        void GetLoopRange(out int startIndex, out int endIndex)
        {
            int stateCount =
                machine != null && machine.states != null
                    ? machine.states.Length
                    : 0;

            endIndex =
                loopEndIndex >= 0
                    ? Mathf.Min(loopEndIndex, stateCount - 1)
                    : stateCount - 1;

            startIndex = Mathf.Clamp(loopStartIndex, 0, Mathf.Max(0, endIndex));
        }

        void Advance()
        {
            int cur = machine.GetCurrentStateIndex();

            // A loop tartomány (v0.5, ADR-022 kiegészítés): csak a
            // loopStartIndex..loopEndIndex közötti állapotokat pörgeti
            // (pl. 30-ből az 5-öt) — a tartományon kívüli állapotok
            // (főmenü, bevezető) nem ismétlődnek.
            GetLoopRange(out int startIndex, out int endIndex);

            // Ha a cur a tartományon kívül van (pl. az idle visszahozta
            // egy bevezető állapotba), a tartomány elejére lépünk.
            int next;

            if (cur < startIndex || cur > endIndex)
            {
                next = startIndex;
            }
            else
            {
                next = cur + 1;
            }

            if (next > endIndex)
            {
                if (loop)
                {
                    next = startIndex;
                }
                else
                {
                    Debug.Log(
                        "[FigmaFlow] Playmaker: a szekvencia vége " +
                        "(loop ki) — megállás.");

                    return;
                }
            }

            Debug.Log(
                $"[FigmaFlow] Playmaker: időzített váltás " +
                $"{cur} -> {next} " +
                $"(tartomány {startIndex}-{endIndex}, loop={loop}).");

            machine.GoToState(next);
        }

        // ------------------------------------------------------------

        /// <summary>
        /// Külső input (érintőfal, lidar) jelezheti az aktivitást —
        /// az idle-időzítő resetje (a demó input-kezelő is jelez,
        /// ha van — az IFigmaFlowInput.Activate hívásával).
        /// </summary>
        public void Ping()
        {
            idleTimer = 0f;

            input?.Activate();
        }

        /// <summary>
        /// Az időzítők nullázása (pl. a bemutató újraindításakor).
        /// </summary>
        public void ResetTimers()
        {
            stateTimer = 0f;
            idleTimer = 0f;
        }
    }
}
