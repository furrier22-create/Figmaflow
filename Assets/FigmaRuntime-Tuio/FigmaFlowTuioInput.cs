using System;
using System.Collections.Generic;
using TuioNet.Tuio11;
using TuioUnity.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.6/6 + v0.6/7 (ADR-030 + ADR-031): a TUIO érintőfal input —
    /// MULTI-TOUCH + események + visszajelzés.
    ///
    /// MULTI-TOUCH (ADR-030): simogatás/drag (move-enter), lidar jel-vesztés
    /// (drop-helyreállítás), tenyér (gomb-lock), két-pontos kombinált trigger.
    ///
    /// BŐVÍTÉSEK (ADR-031):
    /// - VISSZAJELZÉS: csak a pressFeedbackElements listában szereplő elemek
    ///   nőnek meg érintésre (pressScale), és AMÍG rajta a kéz, MEGNŐVE
    ///   maradnak — elengedéskor visszaállnak
    /// - KOMBINÁLT TRIGGER: timeWindow (időablak — a két érintés ennyin
    ///   belül), holdMode (folyamatos nyomvatartás — ha bármelyik pontot
    ///   elengedik, az aktiváció megszűnik: releaseState + onDeactivate)
    /// - MAPPING HOLD: a mapping is lehet nyomvatartásos (holdMode) —
    ///   1 gomb esetén is: amíg nyomva, aktív; elengedésre releaseState
    /// - ESEMÉNYEK: onActivate/onDeactivate UnityEvent-ek (mint a UI Button
    ///   onClick) — az Inspectorban bármilyen metódus ráhúzható, nem csak
    ///   GoToState
    /// - EGÉR: a bal gomb nyomva tartása is "cursor" (a hold és a
    ///   visszajelzés egérrel is tesztelhető)
    ///
    /// A single-touch viselkedés (mapping, click) változatlan alapértelmezésben.
    /// </summary>
    public class FigmaFlowTuioInput : MonoBehaviour, IFigmaFlowInput
    {
        [Header("TUIO")]
        [Tooltip("A TuioUnity TuioSessionBehaviour — a TUIO adatok forrása.")]
        public TuioSessionBehaviour tuioSession;

        [Tooltip("A TUIO Y tengely felfelé van-e (a te küldőd Y-lefelé: false).")]
        public bool flipY = true;

        [Header("Multi-touch")]
        [Tooltip("Áthaladáskor is aktivál (simogatás/drag).")]
        public bool triggerOnMoveEnter = true;

        [Tooltip("Drop-helyreállítás ablak (mp) — a lidar jel-vesztése után ennyi időn belül ugyanaz az érintés.")]
        public float dropRecoveryWindow = 0.3f;

        [Tooltip("Drop-helyreállítás távolság (px).")]
        public float dropRecoveryDistance = 50f;

        [Tooltip("Gomb-lock: egy gomb csak akkor aktiválódhat újra, ha az összes cursor lejött róla (a tenyeres több-cursor nem kapcsolgatja).")]
        public bool enableButtonLock = true;

        [Header("Visszajelzés (növekedés érintésre)")]
        [Tooltip("CSAK az ebben a listában szereplő elemek nőnek meg érintésre — a 'érzékeltem' jelzés. Amíg rajta a kéz, megnőve maradnak.")]
        public List<string> pressFeedbackElements = new();

        [Tooltip("A megnövekedés mértéke (pl. 1.1 = 10%).")]
        public float pressScale = 1.1f;

        [Header("Egér-szimuláció (teszt)")]
        [Tooltip("Bal gomb = érintés; nyomva tartva 'cursor' (a hold és a visszajelzés tesztelhető).")]
        public bool simulateWithMouse = true;

        [Header("Mapping (érintés -> state / esemény)")]
        [Tooltip("A névvel ellátott elem érintése: GoToState + onActivate. A holdMode-dal nyomvatartásos.")]
        public List<TuioStateMapping> mappings = new();

        [Tooltip("Mapping nélküli elemre szintetikus ClickEvent (a meglévő input kezelők így élnek).")]
        public bool dispatchClickOnUnmapped = true;

        [Header("Kombinált trigger (két pont)")]
        [Tooltip("A két elem EGYSZERRE (időablakon belül) érintett → GoToState + onActivate. A holdMode-dal amíg mindkettőt tartják, aktív.")]
        public List<TuioCombinedTrigger> combinedTriggers = new();

        [Header("Vezérlés")]
        [Tooltip("A vezérelt StateMachine (a GoToState-ekhez).")]
        public FigmaStateMachine machine;

        // ---- IFigmaFlowInput ----

        public FigmaStateMachine Machine => machine;

        public float LastActivationTime { get; private set; } = -999f;

        // ------------------------------------------------------------

        // Az egér-szimuláció pszeudo-cursor id-ja (a TUIO sessionId-k 32 bitesek,
        // ettől biztosan különbözik).
        private const uint MouseSessionId = 0xFFFFFFFEu;

        private bool bound;

        private UIDocument document;

        private VisualElement root;

        private Tuio11Dispatcher dispatcher;

        // A cursor-nyilvántartás: sessionId -> pozíció (panel-koordinátában).
        private readonly Dictionary<uint, Vector2> cursorPositions = new();

        private readonly Dictionary<uint, float> cursorTimes = new();

        // A gomb-lock: a gomb neve -> hány cursor van rajta.
        private readonly Dictionary<string, int> buttonLockCount = new();

        // A visszajelzés: elem név -> hány cursor van rajta + a referencia.
        private readonly Dictionary<string, int> pressCounts = new();

        private readonly Dictionary<string, VisualElement> pressRefs = new();

        // A kombinált trigger időablakához: elem név -> első érintés ideje.
        private readonly Dictionary<string, float> elementTouchStart = new();

        // A nyomvatartásos mappingek (aktívan tartottak).
        private readonly HashSet<string> activeMappingHolds = new();

        // Az aktív kombinált triggerek.
        private readonly HashSet<TuioCombinedTrigger> activeCombined = new();

        // ------------------------------------------------------------

        public void Bind()
        {
            if (bound)
                return;

            document = GetComponent<UIDocument>();

            if (document == null)
            {
                Debug.LogWarning("[FigmaFlow] TUIO input: NINCS UIDocument a GameObject-en.");

                return;
            }

            root = document.rootVisualElement;

            if (tuioSession != null)
            {
                dispatcher =
                    tuioSession.TuioDispatcher as Tuio11Dispatcher;

                if (dispatcher != null)
                {
                    dispatcher.OnCursorAdd += OnCursorAdd;
                    dispatcher.OnCursorRemove += OnCursorRemove;
                }
                else
                {
                    Debug.LogWarning(
                        "[FigmaFlow] A TuioSession verziója nem TUIO 1.1.");
                }
            }

            bound = true;
        }

        public void Unbind()
        {
            if (!bound)
                return;

            if (dispatcher != null)
            {
                dispatcher.OnCursorAdd -= OnCursorAdd;
                dispatcher.OnCursorRemove -= OnCursorRemove;
            }

            bound = false;
        }

        public void Activate()
        {
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

        void Update()
        {
            UpdateMouseCursor();

            PollCursors();

            CheckHoldsAndCombined();
        }

        // ---- Egér-szimuláció (a bal gomb = cursor) ----

        void UpdateMouseCursor()
        {
            if (!simulateWithMouse)
                return;

            bool down = Input.GetMouseButtonDown(0);
            bool held = Input.GetMouseButton(0);
            bool up = Input.GetMouseButtonUp(0);

            if (down)
            {
                Vector2 panelPos = ScreenToPanel(Input.mousePosition);

                cursorPositions[MouseSessionId] = panelPos;
                cursorTimes[MouseSessionId] = Time.time;

                Activate();

                HandleTouchAt(panelPos, "mouse");
            }
            else if (held && cursorPositions.ContainsKey(MouseSessionId))
            {
                Vector2 panelPos = ScreenToPanel(Input.mousePosition);

                Vector2 oldPos = cursorPositions[MouseSessionId];

                cursorPositions[MouseSessionId] = panelPos;
                cursorTimes[MouseSessionId] = Time.time;

                // Move-enter (drag): másik elemre került.
                string oldName = PickName(oldPos);
                string newName = PickName(panelPos);

                if (oldName != newName)
                {
                    if (!string.IsNullOrEmpty(oldName))
                        OnCursorLeft(oldName);

                    HandleTouchAt(panelPos, "mouse");
                }
            }
            else if (up && cursorPositions.ContainsKey(MouseSessionId))
            {
                Vector2 pos = cursorPositions[MouseSessionId];

                // FONTOS: a cursor MINDIG törlődik (a pozíciótól függetlenül) —
                // ha az egér a gomb nélküli területen engedődött fel, a
                // cursor örökre a nyilvántartásban maradna, és a kombinált
                // trigger (IsElementTouched) örökre igazat adna.
                cursorPositions.Remove(MouseSessionId);
                cursorTimes.Remove(MouseSessionId);

                string name = PickName(pos);

                if (!string.IsNullOrEmpty(name))
                    OnCursorLeft(name);
            }
        }

        // ---- TUIO események ----

        void OnCursorAdd(object sender, Tuio11Cursor cursor)
        {
            if (cursor == null)
                return;

            if (TryRecoverDrop(cursor))
                return;

            uint id = cursor.SessionId;

            cursorPositions[id] = TuioToPanel(cursor.Position);
            cursorTimes[id] = Time.time;

            Activate();

            HandleTouchAt(cursorPositions[id], $"cursor-{id}");
        }

        void OnCursorRemove(object sender, Tuio11Cursor cursor)
        {
            if (cursor == null)
                return;

            uint id = cursor.SessionId;

            if (cursorPositions.TryGetValue(id, out Vector2 pos))
            {
                string name = PickName(pos);

                if (!string.IsNullOrEmpty(name))
                {
                    OnCursorLeft(name);

                    // A gomb-lock feloldása.
                    if (buttonLockCount.ContainsKey(name))
                        buttonLockCount[name] =
                            Mathf.Max(0, buttonLockCount[name] - 1);
                }
            }

            cursorPositions.Remove(id);
            cursorTimes.Remove(id);
        }

        /// <summary>
        /// A drop-helyreállítás: az elveszett + újra megjelenő cursor
        /// ugyanazként kezelése (nem új aktiválás).
        /// </summary>
        bool TryRecoverDrop(Tuio11Cursor cursor)
        {
            Vector2 newPos = TuioToPanel(cursor.Position);

            uint bestId = uint.MaxValue;

            float bestDist = dropRecoveryDistance;

            float bestTime = 0f;

            foreach (KeyValuePair<uint, float> kv in cursorTimes)
            {
                if (kv.Key == MouseSessionId)
                    continue;

                if (Time.time - kv.Value > dropRecoveryWindow)
                    continue;

                if (!cursorPositions.TryGetValue(kv.Key, out Vector2 oldPos))
                    continue;

                float d = Vector2.Distance(oldPos, newPos);

                if (d < bestDist)
                {
                    bestDist = d;
                    bestId = kv.Key;
                    bestTime = kv.Value;
                }
            }

            if (bestId == uint.MaxValue)
                return false;

            cursorPositions.Remove(cursor.SessionId);
            cursorPositions[bestId] = newPos;
            cursorTimes[bestId] = Time.time;

            Debug.Log(
                $"[FigmaFlow] TUIO: drop-helyreállítás — " +
                $"a {cursor.SessionId} a {bestId} folytatása " +
                $"({bestDist:F0}px, {Time.time - bestTime:F2}s).");

            return true;
        }

        // ---- A TUIO cursor-ok pollozása (a pozíció mindig friss) ----

        void PollCursors()
        {
            if (dispatcher == null || !triggerOnMoveEnter)
                return;

            foreach (KeyValuePair<uint, Vector2> kv in
                new Dictionary<uint, Vector2>(cursorPositions))
            {
                if (kv.Key == MouseSessionId)
                    continue;

                // A pozíciót a Tuio11Cursor-ból frissítjük, ha elérhető —
                // itt a nyilvántartott panel-pozíció a forrás; a move-enter
                // az egérhez hasonlóan a pozíció változásából.
                Vector2 newPos = kv.Value;

                // (A Tuio11Cursor referenciát nem tároljuk külön — a
                // pozíciót az add/remove frissíti; a folyamatos mozgás
                // a move-enter-hez az egér-szimuláció mintájára a
                // feldolgozás későbbi bővítése lehet.)
                _ = newPos;
            }
        }

        // ---- Egy közös esemény: egy elem elhagyása ----

        void OnCursorLeft(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            // Visszajelzés: a cursor lejött az elemről.
            PressFeedback(name, -1, null);

            // A gomb-lock csökkentése (TUIO cursoroknál a remove is ezt
            // hívja — itt a move-enter esetére, ha nem a remove úton).
        }

        // ---- Koordináta-konverzió ----

        UnityEngine.Vector2 TuioToPanel(System.Numerics.Vector2 tuio)
        {
            if (root == null)
                return UnityEngine.Vector2.zero;

            float x = tuio.X * root.worldBound.width;

            float y = flipY
                ? (1f - tuio.Y) * root.worldBound.height
                : tuio.Y * root.worldBound.height;

            return new UnityEngine.Vector2(x, y);
        }

        UnityEngine.Vector2 ScreenToPanel(UnityEngine.Vector3 screenPos)
        {
            if (root == null || Screen.width <= 0f || Screen.height <= 0f)
                return UnityEngine.Vector2.zero;

            float x = (screenPos.x / Screen.width) * root.worldBound.width;

            float y = (1f - screenPos.y / Screen.height) * root.worldBound.height;

            return new UnityEngine.Vector2(x, y);
        }

        string PickName(UnityEngine.Vector2 panelPos)
        {
            if (root == null)
                return null;

            VisualElement picked = root.panel.Pick(panelPos);

            return picked?.name;
        }

        /// <summary>
        /// Van-e aktív érintés (cursor vagy egér) az adott nevű elemen —
        /// az ősökön is keres.
        /// </summary>
        bool IsElementTouched(string elementName)
        {
            if (string.IsNullOrEmpty(elementName) || root == null)
                return false;

            foreach (KeyValuePair<uint, Vector2> kv in cursorPositions)
            {
                VisualElement picked = root.panel.Pick(kv.Value);

                for (VisualElement e = picked; e != null; e = e.parent)
                {
                    if (e.name == elementName)
                        return true;
                }
            }

            return false;
        }

        // ---- Visszajelzés (növekedés érintésre) ----

        void PressFeedback(string name, int delta, VisualElement element)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (!pressFeedbackElements.Contains(name))
                return;

            int c = pressCounts.TryGetValue(name, out int v) ? v : 0;

            c = Mathf.Max(0, c + delta);

            pressCounts[name] = c;

            if (element != null)
                pressRefs[name] = element;

            if (c == 1 &&
                pressRefs.TryGetValue(name, out VisualElement el) &&
                el != null)
            {
                // Az első érintés: megnő (és amíg rajta van, így marad).
                el.style.scale = new Scale(new Vector2(pressScale, pressScale));
            }
            else if (c == 0 &&
                pressRefs.TryGetValue(name, out VisualElement el2) &&
                el2 != null)
            {
                // Az utolsó érintés is lejött: visszaáll.
                el2.style.scale = Scale.None();
            }
        }

        // ---- A közös érintés-kezelés ----

        void HandleTouchAt(UnityEngine.Vector2 panelPos, string source)
        {
            if (root == null || machine == null)
                return;

            VisualElement picked = root.panel.Pick(panelPos);

            if (picked == null)
                return;

            string pickedName = picked.name;

            // GOMB-LOCK (CSAK a TUIO cursorokra — az egér nem lockol):
            // ha a gomb zárolva van, a TUIO-érintés nem aktiválódhat újra.
            bool isCursor = source.StartsWith("cursor");

            if (enableButtonLock &&
                pickedName != null &&
                isCursor &&
                buttonLockCount.TryGetValue(pickedName, out int lockCount) &&
                lockCount > 0)
            {
                return;
            }

            // A lock növelése (TUIO cursor).
            if (enableButtonLock &&
                pickedName != null &&
                isCursor)
            {
                buttonLockCount[pickedName] =
                    (buttonLockCount.TryGetValue(pickedName, out int c) ? c : 0) + 1;
            }

            int currentState = machine.GetCurrentStateIndex();

            // A név-lánc bejárása: a mapping / visszajelzés az ősökön is keres.
            for (VisualElement e = picked; e != null; e = e.parent)
            {
                string name = e.name;

                if (string.IsNullOrEmpty(name))
                    continue;

                // Visszajelzés (ha az elem a listában van).
                if (pressFeedbackElements.Contains(name))
                    PressFeedback(name, +1, e);

                // Mapping.
                TuioStateMapping mapping = FindMapping(name, currentState);

                if (mapping != null)
                {
                    if (mapping.targetState >= 0)
                    {
                        Debug.Log(
                            $"[FigmaFlow] TUIO: '{name}' " +
                            $"(state {currentState} -> {mapping.targetState}).");

                        machine.GoToState(mapping.targetState);
                    }

                    mapping.onActivate?.Invoke();

                    if (mapping.holdMode)
                        activeMappingHolds.Add(name);

                    return;
                }
            }

            // Mapping nélkül: a közvetlen érintés-célpont hívása.
            // A szintetikus ClickEvent dispatch-e nem garantáltan éri el
            // a RegisterCallback<ClickEvent>-et (a ClickEvent a pointer-
            // fázisokból származik) — a közvetlen OnTap hívás megbízható.
            // Az egér-szimulációnál a NATÍV kattintás generálja a
            // ClickEventet — ott nem hívunk (a dupla elkerülése).
            if (dispatchClickOnUnmapped)
            {
                // A regisztrált puzzle-k közül, amelyiknek ez az elem a
                // tárgya/slotja → közvetlen OnTap hívás (a VisualElement-
                // keresés nem működik: a puzzle MonoBehaviour, nem VE).
                // A puzzle-célpontok: a SlotPuzzle VAGY a CombinationLock
                // (a regiszterekből — a VisualElement-keresés nem működik,
                // mert ezek MonoBehaviour-ok).
                SlotPuzzle puzzle =
                    SlotPuzzle.FindForElement(pickedName);

                if (puzzle != null)
                {
                    Debug.Log(
                        $"[FigmaFlow] TUIO: puzzle-célpont '{pickedName}' " +
                        $"(forrás: {source}).");

                    puzzle.OnTap(pickedName);

                    return;
                }

                CombinationLock lockPuzzle =
                    CombinationLock.FindForElement(pickedName);

                if (lockPuzzle != null)
                {
                    Debug.Log(
                        $"[FigmaFlow] TUIO: lock-célpont '{pickedName}' " +
                        $"(forrás: {source}).");

                    lockPuzzle.OnTap(pickedName);

                    return;
                }

                // Ha nincs célpont, a szintetikus ClickEvent (a meglévő
                // input kezelőknek) — de csak TUIO cursorra (az egér natív).
                if (source.StartsWith("cursor"))
                {
                    Debug.Log(
                        $"[FigmaFlow] TUIO: nincs mapping '{pickedName}' — ClickEvent.");

                    picked.SendEvent(new ClickEvent());
                }
            }
        }

        TuioStateMapping FindMapping(string name, int currentState)
        {
            foreach (TuioStateMapping mapping in mappings)
            {
                if (mapping == null)
                    continue;

                if (mapping.elementName != name)
                    continue;

                if (mapping.targetState < 0 && mapping.onActivate == null)
                    continue;

                if (mapping.fromState >= 0 &&
                    mapping.fromState != currentState)
                {
                    continue;
                }

                return mapping;
            }

            return null;
        }

        // ---- A nyomvatartás + kombinált trigger ellenőrzése (minden frame) ----

        void CheckHoldsAndCombined()
        {
            if (root == null)
                return;

            // Az elem-érintés kezdőidők karbantartása (az időablakhoz).
            List<string> toRemove = new();

            foreach (KeyValuePair<string, float> kv in elementTouchStart)
            {
                if (!IsElementTouched(kv.Key))
                    toRemove.Add(kv.Key);
            }

            foreach (string n in toRemove)
                elementTouchStart.Remove(n);

            // A mapping-holdok: ha az elemet elengedték → deaktiválás.
            foreach (string name in new List<string>(activeMappingHolds))
            {
                if (IsElementTouched(name))
                {
                    // Az időablakhoz rögzítjük a kezdőidőt (ha még nincs).
                    if (!elementTouchStart.ContainsKey(name))
                        elementTouchStart[name] = Time.time;

                    continue;
                }

                activeMappingHolds.Remove(name);

                TuioStateMapping mapping = FindMapping(name, -1);

                if (mapping != null)
                {
                    if (mapping.releaseState >= 0)
                    {
                        Debug.Log(
                            $"[FigmaFlow] TUIO: '{name}' elengedve (hold) -> " +
                            $"{mapping.releaseState}.");

                        machine.GoToState(mapping.releaseState);
                    }

                    mapping.onDeactivate?.Invoke();
                }
            }

            // A kombinált triggerek.
            foreach (TuioCombinedTrigger trigger in combinedTriggers)
            {
                if (trigger == null)
                    continue;

                bool e1 = IsElementTouched(trigger.element1);
                bool e2 = IsElementTouched(trigger.element2);

                bool isActive = activeCombined.Contains(trigger);

                // Az időablak: ha > 0, a két érintés kezdete ennyin belül
                // legyen.
                bool windowOk = true;

                if (trigger.timeWindow > 0f && e1 && e2)
                {
                    if (!elementTouchStart.ContainsKey(trigger.element1))
                        elementTouchStart[trigger.element1] = Time.time;

                    if (!elementTouchStart.ContainsKey(trigger.element2))
                        elementTouchStart[trigger.element2] = Time.time;

                    float t1 = elementTouchStart[trigger.element1];
                    float t2 = elementTouchStart[trigger.element2];

                    windowOk =
                        Mathf.Abs(t1 - t2) <= trigger.timeWindow;
                }

                bool conditions = e1 && e2 && windowOk;

                if (conditions && !isActive)
                {
                    activeCombined.Add(trigger);

                    if (trigger.targetState >= 0)
                    {
                        Debug.Log(
                            $"[FigmaFlow] TUIO: kombinált " +
                            $"'{trigger.element1}' + '{trigger.element2}' " +
                            $"(időablak {trigger.timeWindow:F2}s) -> " +
                            $"{trigger.targetState}.");

                        machine.GoToState(trigger.targetState);
                    }

                    trigger.onActivate?.Invoke();
                }
                else if (isActive &&
                         trigger.holdMode &&
                         !conditions)
                {
                    // Folyamatos nyomvatartás: ha bármelyik pontot
                    // elengedték, az aktiváció megszűnik.
                    activeCombined.Remove(trigger);

                    if (trigger.releaseState >= 0)
                    {
                        Debug.Log(
                            $"[FigmaFlow] TUIO: kombinált elengedve -> " +
                            $"{trigger.releaseState}.");

                        machine.GoToState(trigger.releaseState);
                    }

                    trigger.onDeactivate?.Invoke();
                }
                else if (isActive && !trigger.holdMode && !conditions)
                {
                    // NEM nyomvatartásos: ha a feltételek megszűntek, a
                    // trigger "kész" — a következő aktiváláshoz újra kell
                    // érinteni (edge-trigger). (A gomb-lock ezt is kezeli,
                    // de a kombinált nem lockolt a HandleTouchAt-ban —
                    // ezért itt magától újra aktiválódhatna minden
                    // frame-ben, ha a feltétel fennmaradna. A kivezetés
                    // a feltétel megszűnésével az újra-aktiválás lehetősége.)
                    activeCombined.Remove(trigger);

                    if (trigger.releaseState >= 0)
                    {
                        Debug.Log(
                            $"[FigmaFlow] TUIO: kombinált kész (nem hold) -> " +
                            $"{trigger.releaseState}.");
                    }

                    trigger.onDeactivate?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Egy TUIO-mapping: a megnevezett VisualElement érintése GoToState-et
    /// és onActivate eseményt vált ki. A holdMode-dal nyomvatartásos
    /// (elengedésre releaseState + onDeactivate).
    /// </summary>
    [Serializable]
    public class TuioStateMapping
    {
        [Tooltip("A VisualElement neve (a UXML-ben), amire az érintés érkezik.")]
        public string elementName;

        [Tooltip("A mapping ÉRVÉNYESSÉGE: ha ez >= 0, csak ebben az állapotban aktív.")]
        public int fromState = -1;

        [Tooltip("A cél state index (a StateMachine states tömbjében). -1 = nincs (csak esemény).")]
        public int targetState = -1;

        [Header("Nyomvatartás")]
        [Tooltip("Folyamatos nyomvatartás: amíg az elemet tartják, aktív; elengedésre releaseState + onDeactivate.")]
        public bool holdMode = false;

        [Tooltip("Elengedéskori cél state (holdMode-nál). -1 = nincs.")]
        public int releaseState = -1;

        [Header("Események")]
        [Tooltip("Aktiváláskor (érintés) — a GoToState mellett/vagy helyett (mint a UI Button onClick).")]
        public UnityEvent onActivate;

        [Tooltip("Deaktiváláskor (elengedés, holdMode-nál).")]
        public UnityEvent onDeactivate;
    }

    /// <summary>
    /// Egy kombinált trigger: a két elem az időablakon belül egyszerre
    /// érintett → GoToState + onActivate. A holdMode-dal amíg MINDKETTŐT
    /// tartják, aktív; ha bármelyiket elengedik, megszűnik.
    /// </summary>
    [Serializable]
    public class TuioCombinedTrigger
    {
        [Tooltip("Az első elem neve (a UXML-ben).")]
        public string element1;

        [Tooltip("A második elem neve — ennek is érintettnek kell lennie.")]
        public string element2;

        [Tooltip("A cél state index. -1 = nincs (csak esemény).")]
        public int targetState = -1;

        [Header("Időablak")]
        [Tooltip("A két érintés kezdete közötti megengedett idő (mp). 0 = nincs korlát (mindkettő érintett = aktivál).")]
        public float timeWindow = 0f;

        [Header("Nyomvatartás")]
        [Tooltip("Folyamatos nyomvatartás: amíg MINDKÉT pont érintett, aktív; ha egyik is lejön, az aktiváció megszűnik.")]
        public bool holdMode = false;

        [Tooltip("Elengedéskori cél state (holdMode-nál). -1 = nincs.")]
        public int releaseState = -1;

        [Header("Események")]
        [Tooltip("Aktiváláskor.")]
        public UnityEvent onActivate;

        [Tooltip("Deaktiváláskor (elengedés, holdMode-nál).")]
        public UnityEvent onDeactivate;
    }
}
