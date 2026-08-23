using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.7/1 (ADR-032): SlotPuzzle — a tárgyak tap-tap pakolása a helyes
    /// sorrendbe (szabadulószoba).
    ///
    /// A LOGIKA külön réteg (a PlayMaker mintájára) — a Figma state-ek csak
    /// a vizuális visszajelzés (a puzzle állapota: megoldva / nem). A
    /// kombináció (a helyes sorrend) itt él, nem a state-ekben.
    ///
    /// MŰKÖDÉS:
    /// - a játékos egy tárgyat (VisualElement) megérintve kiválaszt
    /// - a következő érintés egy slotra (VisualElement) → a tárgy odakerül
    /// - ha a slot sorrendje megfelel a solutionOrdernek → a puzzle megoldva
    ///   (GoToState + onSolved)
    /// - hiba esetén: onWrong + (opcionálisan) a tárgyak visszaállítása
    ///
    /// A TUIO-inputtal együtt használandó (a mapping nélküli érintések
    /// ClickEventet kapnak — a puzzle a ClickEvent-eket figyeli).
    /// </summary>
    public class SlotPuzzle : MonoBehaviour, IFigmaFlowTouchTarget
    {
        /// <summary>
        /// A TUIO-input által lekérdezhető regisztrált puzzle-ok.
        /// (A VisualElement-eken nem lehet interfészt keresni — a
        /// puzzle a MonoBehaviour, ezért itt regisztrálja magát.)
        /// </summary>
        private static readonly System.Collections.Generic.List<SlotPuzzle>
            registered = new();

        public static SlotPuzzle FindForElement(string elementName)
        {
            foreach (SlotPuzzle p in registered)
            {
                if (p == null)
                    continue;

                if (p.itemElements.ContainsKey(elementName) ||
                    p.slotContents.ContainsKey(elementName))
                {
                    return p;
                }
            }

            return null;
        }

        [Header("SlotPuzzle (v0.7)")]

        [Tooltip("A UIDocument (a rootVisualElement a gombokhoz).")]
        public UIDocument document;

        [Tooltip("A kiválasztás állapota: ha egy tárgyat kiválasztunk, ez az osztály kerül rá (kiemelés).")]
        public string selectedClass = "puzzle-selected";

        [Tooltip("A tárgyak nevei (a UXML-ből), a helyes sorrendben.")]
        public List<string> solutionOrder = new();

        [Tooltip("A slotok nevei (a UXML-ből) — a tárgyak ide pakolhatók.")]
        public List<string> slotNames = new();

        [Tooltip("Hiba esetén a tárgyak visszaállítása (üres slotok).")]
        public bool resetOnWrong = true;

        [Header("Kimenetek")]

        [Tooltip("A megoldás célállapota (a StateMachine states tömbjében). -1 = nincs.")]
        public int solvedState = -1;

        [Tooltip("A vezérelt StateMachine.")]
        public FigmaStateMachine machine;

        [Header("Események")]

        public UnityEvent onSolved;

        public UnityEvent onWrong;

        // ---- Állapot ----

        private readonly Dictionary<string, VisualElement> slotElements = new();

        private readonly Dictionary<string, VisualElement> itemElements = new();

        // slot neve -> az oda rakott tárgy neve (null = üres).
        private readonly Dictionary<string, string> slotContents = new();

        // tárgy neve -> az eredeti pozíció (a visszavételhez).
        private readonly Dictionary<string, Vector3> originalPositions = new();

        private string selectedItem;

        private bool solved;

        // ------------------------------------------------------------

        private bool bound;

        private int bindAttempts;

        void OnEnable()
        {
            // Regisztráció a TUIO-input számára (a statikus regiszterbe).
            if (!registered.Contains(this))
                registered.Add(this);

            // A bind NEM az OnEnable-ben — a fa (a StateMachine Start-je)
            // aszinkron töltődik, a rootVisualElement még üres lehet.
            // A TryBind az Update-ben próbálkozik, amíg minden megvan.
        }

        void OnDisable()
        {
            registered.Remove(this);

            Unbind();
        }

        void Update()
        {
            TryBind();
        }

        void TryBind()
        {
            if (bound)
                return;

            // Védelem: ne próbálkozzon örökké (a rossz nevek miatt).
            if (bindAttempts > 60)
                return;

            bindAttempts++;

            if (document == null || document.rootVisualElement == null)
                return;

            bool allFound = true;

            // A slotok felkutatása.
            foreach (string slot in slotNames)
            {
                if (slotElements.ContainsKey(slot))
                    continue;

                VisualElement el =
                    document.rootVisualElement.Q<VisualElement>(slot);

                if (el != null)
                {
                    slotElements[slot] = el;

                    slotContents[slot] = null;

                    el.RegisterCallback<ClickEvent>(OnSlotClick);
                }
                else
                {
                    allFound = false;
                }
            }

            // A tárgyak felkutatása.
            foreach (string item in solutionOrder)
            {
                if (itemElements.ContainsKey(item))
                    continue;

                VisualElement el =
                    document.rootVisualElement.Q<VisualElement>(item);

                if (el != null)
                {
                    itemElements[item] = el;

                    el.RegisterCallback<ClickEvent>(OnItemClick);
                }
                else
                {
                    allFound = false;
                }
            }

            if (!allFound)
            {
                Debug.Log(
                    $"[FigmaFlow] SlotPuzzle: bind próba {bindAttempts} — " +
                    $"slotok {slotElements.Count}/{slotNames.Count}, " +
                    $"tárgyak {itemElements.Count}/{solutionOrder.Count} " +
                    "(a fa még töltődik, vagy rossz név).");

                return;
            }

            bound = true;

            Debug.Log(
                $"[FigmaFlow] SlotPuzzle: bind kész — " +
                $"slotok {slotElements.Count}/{slotNames.Count}, " +
                $"tárgyak {itemElements.Count}/{solutionOrder.Count}.");

            // Az eredeti pozíciók rögzítése — egy frame múlva (a layout
            // lefutása után a worldBound érvényes).
            document.rootVisualElement.schedule.Execute(() =>
            {
                CaptureOriginalPositions();
            }).StartingIn(50);
        }

        void CaptureOriginalPositions()
        {
            foreach (var kv in itemElements)
            {
                VisualElement item = kv.Value;

                VisualElement parent = item.parent;

                if (parent == null)
                    continue;

                originalPositions[kv.Key] =
                    new Vector3(
                        item.worldBound.x - parent.worldBound.x,
                        item.worldBound.y - parent.worldBound.y,
                        item.resolvedStyle.position == Position.Absolute ? 1f : 0f);
            }
        }

        void Unbind()
        {
            foreach (var kv in slotElements)
            {
                if (kv.Value != null)
                    kv.Value.UnregisterCallback<ClickEvent>(OnSlotClick);
            }

            foreach (var kv in itemElements)
            {
                if (kv.Value != null)
                    kv.Value.UnregisterCallback<ClickEvent>(OnItemClick);
            }
        }

        // ---- Kattintások ----

        void OnItemClick(ClickEvent evt)
        {
            if (solved)
                return;

            VisualElement target = evt.target as VisualElement;

            if (target == null)
                return;

            string itemName = target.name;

            // Ha a tárgy egy slotban van, az érintés a SLOT érintése
            // (a slot a célpont a behelyezéshez/cseréhez). A slotban
            // lévő tárgyat csak akkor jelöljük ki, ha NINCS kijelölt
            // tárgy — így a foglalt slot mindig elérhető cél.
            string slotOfItem = FindSlotOf(itemName);

            if (slotOfItem != null)
            {
                if (!string.IsNullOrEmpty(selectedItem))
                {
                    // Van kijelölt tárgy → a slotra helyezés (a slot
                    // kattintásának logikája).
                    Debug.Log(
                        $"[FigmaFlow] SlotPuzzle: '{itemName}' a '{slotOfItem}' slotban — " +
                        "a slotra helyezés (a kijelölt tárggyal).");

                    HandleSlotPlacement(slotOfItem);

                    return;
                }

                // Nincs kijelölt tárgy → a slotban lévő tárgy kijelölése
                // (hogy át lehessen vinni).
                DeselectAll();

                selectedItem = itemName;

                target.style.scale = new Scale(new Vector2(1.15f, 1.15f));

                Debug.Log(
                    $"[FigmaFlow] SlotPuzzle: tárgy kiválasztva '{selectedItem}' (a slotból, megnőve).");

                return;
            }

            // A nem-slotban lévő tárgy kiválasztása.
            DeselectAll();

            selectedItem = itemName;

            target.style.scale = new Scale(new Vector2(1.15f, 1.15f));

            Debug.Log(
                $"[FigmaFlow] SlotPuzzle: tárgy kiválasztva '{selectedItem}' (megnőve).");
        }

        void OnSlotClick(ClickEvent evt)
        {
            if (solved)
                return;

            VisualElement target = evt.target as VisualElement;

            if (target == null)
                return;

            string slotName = target.name;

            Debug.Log(
                $"[FigmaFlow] SlotPuzzle: slot kattintás '{slotName}' " +
                $"(kiválasztott: {selectedItem ?? "nincs"}).");

            HandleSlotPlacement(slotName);
        }

        /// <summary>
        /// A slot-kezelés közös logikája (a slot kattintásából ÉS a
        /// slotban lévő tárgy kattintásából — a kettő ugyanaz).
        /// </summary>
        void HandleSlotPlacement(string slotName)
        {
            if (solved)
                return;

            if (!slotContents.ContainsKey(slotName))
                return;

            // Ha nincs kiválasztott tárgy, és a slot FOGLALT, a slotban
            // lévő tárgyat KIJELÖLJÜK (hogy át lehessen vinni) — az üres
            // kézzel a foglalt slotra kattintás a tárgy kijelölése.
            if (string.IsNullOrEmpty(selectedItem))
            {
                if (slotContents.TryGetValue(slotName, out string content) &&
                    content != null &&
                    itemElements.TryGetValue(content, out VisualElement item))
                {
                    selectedItem = content;

                    item.style.scale = new Scale(new Vector2(1.15f, 1.15f));

                    Debug.Log(
                        $"[FigmaFlow] SlotPuzzle: '{content}' kijelölve (a '{slotName}' slotból, üres kézzel).");
                }

                return;
            }

            // A kiválasztott tárgy a slotba.
            bool slotWasFilled =
                slotContents.TryGetValue(slotName, out string existing) &&
                existing != null;

            string incoming = selectedItem;

            // A kijelölés megszűnése (a tárgy normál méretre).
            DeselectAll();

            if (slotWasFilled)
            {
                if (existing == incoming)
                {
                    // A kiválasztott tárgy a SAJÁT slotjára: VISSZAVÉTEL
                    // — a tárgy visszatér az eredeti helyére.
                    slotContents[slotName] = null;

                    RestoreItemPosition(incoming);

                    Debug.Log(
                        $"[FigmaFlow] SlotPuzzle: '{incoming}' visszavéve " +
                        "(saját slot) — az eredeti helyére.");

                    DeselectAll();

                    CheckSolution();

                    return;
                }

                // A célslot foglalt: CSERE.
                // Azt nézzük, honnan jön az INCOMING tárgy:
                // - ha az incoming egy MÁSIK slotban van (be van pakolva),
                //   akkor HELYCSERE: az existing a incoming slotjába, az
                //   incoming a célslotba
                // - ha az incoming az eredeti helyéről jön (nincs bepakolva),
                //   akkor az existing az eredeti helyére tér vissza
                string incomingSlot = FindSlotOf(incoming);

                if (incomingSlot != null && incomingSlot != slotName)
                {
                    // HELYCSERE: az existing a incomingSlot-ba kerül.
                    slotContents[incomingSlot] = existing;

                    if (itemElements.TryGetValue(existing, out VisualElement exEl) &&
                        slotElements.TryGetValue(incomingSlot, out VisualElement exSlot))
                    {
                        PositionItemOnSlot(exEl, exSlot);
                    }

                    Debug.Log(
                        $"[FigmaFlow] SlotPuzzle: csere — '{incoming}' " +
                        $"a '{slotName}'-ba, '{existing}' a '{incomingSlot}'-ba.");
                }
                else
                {
                    // Az incoming az eredeti helyéről jön: az existing
                    // vissza az eredeti helyére.
                    RestoreItemPosition(existing);

                    Debug.Log(
                        $"[FigmaFlow] SlotPuzzle: '{existing}' vissza az eredeti helyére " +
                        $"('{incoming}' a '{slotName}'-ba).");
                }
            }
            else
            {
                // Üres célslot: ha az incoming egy MÁSIK slotból jön,
                // a régi slotját ki kell üríteni.
                string fromSlot = FindSlotOf(incoming);

                if (fromSlot != null && fromSlot != slotName)
                {
                    slotContents[fromSlot] = null;
                }
            }

            slotContents[slotName] = incoming;

            // A tárgy vizuálisan a slotra kerül (odaúszva).
            if (itemElements.TryGetValue(incoming, out VisualElement itemEl) &&
                slotElements.TryGetValue(slotName, out VisualElement slotEl))
            {
                PositionItemOnSlot(itemEl, slotEl);
            }

            CheckSolution();
        }

        /// <summary>
        /// A tárgyat a slot fölé pozicionálja (a tárgy a szülőjén marad).
        /// A left/top a slot KÖZEPÉRE igazítva (a tárgy méretének fele
        /// levonva); a pozícióváltás a style.translate-tel történik —
        /// azaz az "odaúszás" (a Motion Engine translate-módjának mintája).
        /// </summary>
        void PositionItemOnSlot(
            VisualElement item,
            VisualElement slot)
        {
            VisualElement parent = item.parent;

            if (parent == null || slot == null)
                return;

            Rect slotWorld = slot.worldBound;

            Rect parentWorld = parent.worldBound;

            if (float.IsNaN(slotWorld.x) || float.IsNaN(parentWorld.x))
                return;

            // A slot középpontja a szülő koordináta-rendszerében,
            // a tárgy méretének felével (középre igazítás).
            float cx =
                (slotWorld.x + slotWorld.width * 0.5f) -
                (parentWorld.x + item.worldBound.width * 0.5f);

            float cy =
                (slotWorld.y + slotWorld.height * 0.5f) -
                (parentWorld.y + item.worldBound.height * 0.5f);

            // A tárgy a szülőjén marad — az odaúszás a jelenlegi
            // pozícióból a célpozícióba (animált).
            item.style.position = Position.Absolute;

            StopCoroutine(GlideRoutine(item, cx, cy));

            StartCoroutine(GlideRoutine(item, cx, cy));
        }

        /// <summary>
        /// Az "odaúszás": a tárgy left/top-jának animálása a célpozícióig.
        /// (A Motion Engine mintájára — a freeze-elt osztályokat nem
        /// módosítjuk, csak a pozíció-stílust animáljuk egy coroutine-nal.)
        /// </summary>
        System.Collections.IEnumerator GlideRoutine(
            VisualElement item,
            float targetX,
            float targetY)
        {
            float duration = 0.25f;

            float startX = item.resolvedStyle.left;
            float startY = item.resolvedStyle.top;

            Debug.Log(
                $"[FigmaFlow] SlotPuzzle: odaúszás " +
                $"{item.name}: ({startX:F0},{startY:F0}) -> ({targetX:F0},{targetY:F0}).");

            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;

                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

                item.style.left = Mathf.Lerp(startX, targetX, e);
                item.style.top = Mathf.Lerp(startY, targetY, e);

                yield return null;
            }

            item.style.left = targetX;
            item.style.top = targetY;
        }

        /// <summary>
        /// A tárgy visszaállítása az EREDETI helyére.
        ///
        /// Az "eredeti hely" megbízható forrása: a UXML szerinti elrendezés.
        /// Ezért a tárgyat visszahelyezzük a saját szülőjére (a fában nem
        /// mozogtunk — a szülője ugyanaz), és a style left/top/translate
        /// TÖRLÉSÉVEL (Auto) a layout a UXML szerinti pozícióba teszi.
        /// (A rögzített resolvedStyle a bind idején lehetett 0/NaN — az
        /// Auto a helyes.)
        /// </summary>
        void RestoreItemPosition(string itemName)
        {
            if (!itemElements.TryGetValue(itemName, out VisualElement item))
                return;

            // A rögzített EREDETI pozícióra állunk (a worldBound-ből,
            // a szülőhöz képest — a layout lefutása után rögzítve).
            if (originalPositions.TryGetValue(itemName, out Vector3 orig))
            {
                item.style.position =
                    orig.z > 0.5f ? Position.Absolute : Position.Relative;

                item.style.left = orig.x;
                item.style.top = orig.y;

                Debug.Log(
                    $"[FigmaFlow] SlotPuzzle: visszaállítás '{itemName}' -> " +
                    $"({orig.x:F0},{orig.y:F0}) (abs={orig.z > 0.5f}).");
            }
            else
            {
                // Ha nincs rögzítve, a UXML szerinti (Auto).
                item.style.left = StyleKeyword.Auto;
                item.style.top = StyleKeyword.Auto;
            }

            item.style.translate = new Translate(0, 0);
        }

        void DeselectAll()
        {
            if (!string.IsNullOrEmpty(selectedItem) &&
                itemElements.TryGetValue(selectedItem, out VisualElement item))
            {
                item.style.scale = Scale.None();
            }

            selectedItem = null;
        }

        // ---- A megoldás ellenőrzése ----

        /// <summary>
        /// Melyik slotban van a megadott tárgy (null = egyikben sem —
        /// az eredeti helyén van).
        /// </summary>
        string FindSlotOf(string itemName)
        {
            foreach (KeyValuePair<string, string> kv in slotContents)
            {
                if (kv.Value == itemName)
                    return kv.Key;
            }

            return null;
        }

        /// <summary>
        /// Az IFigmaFlowTouchTarget: a TUIO-input közvetlen hívása.
        /// A megérintett elem neve alapján a tárgy-kijelölés vagy a
        /// slot-kezelés logikáját hívja (a ClickEvent-ekkel azonos).
        /// </summary>
        public void OnTap(string elementName)
        {
            if (solved)
                return;

            if (string.IsNullOrEmpty(elementName))
                return;

            // Ha az elem egy slot, a slot-kezelés.
            if (slotContents.ContainsKey(elementName))
            {
                HandleSlotPlacement(elementName);

                return;
            }

            // Ha a tárgy egy slotban van, a slot-kezelés (a kijelöléssel).
            string slotOf = FindSlotOf(elementName);

            if (slotOf != null && !string.IsNullOrEmpty(selectedItem))
            {
                HandleSlotPlacement(slotOf);

                return;
            }

            // Különben a tárgy-kijelölés (ha van ilyen tárgy).
            if (itemElements.ContainsKey(elementName))
            {
                DeselectAll();

                selectedItem = elementName;

                if (itemElements.TryGetValue(elementName, out VisualElement item))
                    item.style.scale = new Scale(new Vector2(1.15f, 1.15f));

                Debug.Log(
                    $"[FigmaFlow] SlotPuzzle: tárgy kiválasztva '{selectedItem}' (TUIO, megnőve).");
            }
        }

        void CheckSolution()
        {
            // A slotok sorrendje: a slotNames sorrendjében a tartalmuk.
            List<string> current = new();

            foreach (string slot in slotNames)
            {
                if (slotContents.TryGetValue(slot, out string content))
                    current.Add(content);
                else
                    current.Add(null);
            }

            // Minden slot tele van-e?
            bool allFilled = true;

            foreach (string c in current)
            {
                if (string.IsNullOrEmpty(c))
                {
                    allFilled = false;

                    break;
                }
            }

            // A sorrend helyes-e (a slotNames sorrendjében a solutionOrder)?
            bool correct = current.Count == solutionOrder.Count;

            if (correct)
            {
                for (int i = 0; i < solutionOrder.Count; i++)
                {
                    if (current[i] != solutionOrder[i])
                    {
                        correct = false;

                        break;
                    }
                }
            }

            if (correct)
            {
                solved = true;

                if (solvedState >= 0 && machine != null)
                    machine.GoToState(solvedState);

                onSolved?.Invoke();

                Debug.Log("[FigmaFlow] SlotPuzzle: MEGOLDVA.");
            }
            else if (allFilled)
            {
                // CSAK akkor hiba, ha MINDEN slot tele van ÉS a sorrend
                // rossz — a részleges pakolás nem hiba (a játékos
                // szabadon pakolhat).
                onWrong?.Invoke();

                Debug.Log("[FigmaFlow] SlotPuzzle: rossz sorrend (minden slot tele).");

                if (resetOnWrong)
                {
                    // A slotok ürítése (a tárgyak vissza a helyükre).
                    foreach (string slot in slotNames)
                        slotContents[slot] = null;

                    // A tárgyak vissza az eredeti pozíciójukra.
                    foreach (var kv in itemElements)
                    {
                        RestoreItemPosition(kv.Key);
                    }
                }
            }
            else
            {
                // Részleges — nincs értékelés, a pakolás folytatódhat.
                Debug.Log(
                    $"[FigmaFlow] SlotPuzzle: pakolás folyamatban " +
                    $"({current.Count(x => !string.IsNullOrEmpty(x))}/" +
                    $"{slotNames.Count} slot tele).");
            }
        }
    }
}
