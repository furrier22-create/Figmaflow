using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.7/1 (ADR-032): CombinationLock — a tárcsák/gyűrűk beállítása a
    /// helyes kombinációra (szabadulószoba — számzáras lakat, cryptex).
    ///
    /// A LOGIKA külön réteg — a kombináció itt él, a Figma state-ek csak a
    /// vizuális visszajelzés (zárva / nyitva / rossz).
    ///
    /// MŰKÖDÉS:
    /// - a tárcsa (VisualElement) érintése → a KÖVETKEZŐ értékre fordul a
    ///   values listából (STRING — szám, betű, szimbólum is lehet)
    /// - a label (dialName + dialValueLabelSuffix) az értéket mutatja, és
    ///   egy rövid FLIP animációval fordul át (a slot-machine illúziója)
    /// - ha minden tárcsa a megfelelő értéken van → megoldva
    ///   (GoToState + onSolved); hiba → onWrong
    ///
    /// A TUIO-inputtal együtt használandó (az IFigmaFlowTouchTarget.OnTap
    /// közvetlen hívással — a ClickEvent-ek is működnek).
    /// </summary>
    public class CombinationLock : MonoBehaviour, IFigmaFlowTouchTarget
    {
        /// <summary>
        /// A TUIO-input által lekérdezhető regisztrált lock-ok.
        /// </summary>
        private static readonly System.Collections.Generic.List<CombinationLock>
            registered = new();

        public static CombinationLock FindForElement(string elementName)
        {
            foreach (CombinationLock l in registered)
            {
                if (l == null)
                    continue;

                if (l.dialElements.ContainsKey(elementName))
                {
                    return l;
                }
            }

            return null;
        }

        [Header("CombinationLock (v0.7)")]

        [Tooltip("A UIDocument (a rootVisualElement a tárcsákhoz).")]
        public UIDocument document;

        [Tooltip("A tárcsák nevei (a UXML-ből), SORRENDBEN (a kombinációhoz).")]
        public List<string> dialNames = new();

        [Tooltip("A helyes kombináció (a dialNames sorrendjében) — a values lista INDEXE (0-alapú). " +
                 "Pl. ha a values = [képA, képB, képC], a solution = [0, 2] a képA + képC-hez.")]
        public List<int> solution = new();

        [Tooltip("A tárcsák értékei — SPRITE-ok (a saját képeid), sorrendben. " +
                 "A dial_1_value Image elem ezeket mutatja. (Vagy használd a spriteSheet-et lent.)")]
        public List<Sprite> values = new();

        [Header("Sprite sheet (opcionális — a values helyett)")]

        [Tooltip("Ha be van állítva, ezt a sprite sheetet szeleteljük fel cellákra " +
                 "(pl. az egész ABC egy képben) — a values lista helyett. A solution a karakterek INDEXE.")]
        public Sprite spriteSheet;

        [Tooltip("A cellák száma a sheetben (pl. 26 az ABC-hez, 10 a számokhoz).")]
        public int sheetCellCount = 26;

        [Tooltip("A cellák elrendezése: true = vízszintesen egy sorban, false = függőlegesen egy oszlopban.")]
        public bool sheetHorizontal = true;

        [Tooltip("A tárcsa érintésére hány értéket lép (alap 1).")]
        public int step = 1;

        [Tooltip("A tárcsa szöveg-labelje (a UXML-ben) — az érték megjelenítéséhez. A név: dialName + suffix.")]
        public string dialValueLabelSuffix = "_value";

        [Tooltip("A flip animáció hossza (mp) — a slot-machine átfordulás (a labelnél).")]
        public float flipDuration = 0.15f;

        [Tooltip("A tárcsákhoz tartozó SlotMachineReel-ek (a sprite-os megjelenítéshez) — " +
                 "a dialNames sorrendjében. Ha be van állítva, a label helyett a reel csúszik.")]
        public List<SlotMachineReel> reels = new();

        [Tooltip("A megoldás ellenőrzése automatikusan (minden lépésnél) vagy csak egy 'Check' gombbal.")]
        public bool autoCheck = true;

        [Tooltip("A 'Check' gomb neve (ha autoCheck = false).")]
        public string checkButtonName = "CheckButton";

        [Header("Kimenetek")]

        [Tooltip("A megoldás célállapota. -1 = nincs.")]
        public int solvedState = -1;

        [Tooltip("A vezérelt StateMachine.")]
        public FigmaStateMachine machine;

        [Header("Események")]

        public UnityEvent onSolved;

        public UnityEvent onWrong;

        // ---- Állapot ----

        private readonly Dictionary<string, int> dialValues = new();

        private readonly Dictionary<string, VisualElement> dialElements = new();

        private readonly Dictionary<string, Image> dialImages = new();

        // A sprite sheet futásidejű szeletelésének eredménye.
        private readonly List<Sprite> slicedSprites = new();

        private bool solved;

        private bool checkBound;

        private bool bound;

        private int bindAttempts;

        // ------------------------------------------------------------

        void OnEnable()
        {
            // Regisztráció a TUIO-input számára.
            if (!registered.Contains(this))
                registered.Add(this);
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

            if (bindAttempts > 60)
                return;

            bindAttempts++;

            if (document == null || document.rootVisualElement == null)
                return;

            // A sprite sheet szeletelése (egyszer).
            EnsureSlicedSprites();

            bool allFound = true;

            for (int i = 0; i < dialNames.Count; i++)
            {
                string name = dialNames[i];

                if (dialElements.ContainsKey(name))
                    continue;

                VisualElement dial =
                    document.rootVisualElement.Q<VisualElement>(name);

                if (dial == null)
                {
                    allFound = false;

                    continue;
                }

                dialElements[name] = dial;

                dialValues[name] = 0;

                // A kép-elem (a dial neve + suffix) — a sprite-ok megjelenítéséhez.
                string imageName = name + dialValueLabelSuffix;

                Image image =
                    document.rootVisualElement.Q<Image>(imageName);

                if (image != null)
                {
                    dialImages[name] = image;

                    // A sprite méretezése: a kép kitöltse az elemet
                    // (a UI Toolkit Image alapból nem méretezi a sprite-ot).
                    image.scaleMode = ScaleMode.ScaleToFit;

                    // Ha az Image-nek nincs explicit mérete, adjunk egy
                    // alapméretet (különben 0 méretű → nem látszik).
                    if (image.resolvedStyle.width <= 1f)
                        image.style.width = 100;

                    if (image.resolvedStyle.height <= 1f)
                        image.style.height = 100;

                    // A kezdeti sprite (0) beállítása.
                    Sprite s0 = GetSprite(0);

                    image.sprite = s0;

                    Debug.Log(
                        $"[FigmaFlow] CombinationLock: image '{imageName}' megtalálva " +
                        $"a '{name}' tárcsához (sprite: {(s0 != null ? s0.name : "NULL")}, " +
                        $"méret: {image.resolvedStyle.width:F0}x{image.resolvedStyle.height:F0}).");
                }
                else
                {
                    Debug.LogWarning(
                        $"[FigmaFlow] CombinationLock: image '{imageName}' NEM található " +
                        $"— a '{name}' tárcsa értéke nem látszik. " +
                        "(A UXML-ben a dial_1_value egy IMAGE elem legyen, nem Label!)");
                }

                dial.RegisterCallback<ClickEvent>(OnDialClick);
            }

            // A Check gomb (ha nincs autoCheck).
            if (!autoCheck && !checkBound)
            {
                VisualElement check =
                    document.rootVisualElement.Q<VisualElement>(checkButtonName);

                if (check != null)
                {
                    check.RegisterCallback<ClickEvent>(OnCheckClick);

                    checkBound = true;
                }
                else
                {
                    allFound = false;
                }
            }

            if (!allFound)
            {
                Debug.Log(
                    $"[FigmaFlow] CombinationLock: bind próba {bindAttempts} — " +
                    $"tárcsák {dialElements.Count}/{dialNames.Count} " +
                    "(a fa még töltődik, vagy rossz név).");

                return;
            }

            bound = true;

            Debug.Log(
                $"[FigmaFlow] CombinationLock: bind kész — " +
                $"tárcsák {dialElements.Count}/{dialNames.Count}.");
        }

        void Unbind()
        {
            foreach (var kv in dialElements)
            {
                if (kv.Value != null)
                    kv.Value.UnregisterCallback<ClickEvent>(OnDialClick);
            }
        }

        // ---- Az érték kezelése ----

        int ValueCount
        {
            get
            {
                if (slicedSprites.Count > 0)
                    return slicedSprites.Count;

                if (values != null && values.Count > 0)
                    return values.Count;

                return 10;
            }
        }

        /// <summary>
        /// Az indexhez tartozó sprite: előbb a szeletelt sheet, majd a values,
        /// végül null (a logika akkor is működik).
        /// </summary>
        Sprite GetSprite(int index)
        {
            if (slicedSprites.Count > 0)
            {
                int i = ((index % slicedSprites.Count) + slicedSprites.Count) % slicedSprites.Count;

                return slicedSprites[i];
            }

            if (values != null && values.Count > 0)
            {
                int i = ((index % values.Count) + values.Count) % values.Count;

                return values[i];
            }

            return null;
        }

        /// <summary>
        /// A sprite sheet futásidejű szeletelése egyenkénti sprite-okra
        /// (Sprite.Create — a sheet minden cellája egy külön sprite).
        /// </summary>
        void EnsureSlicedSprites()
        {
            if (slicedSprites.Count > 0 || spriteSheet == null)
                return;

            Texture2D tex = spriteSheet.texture;

            if (tex == null || sheetCellCount <= 0)
                return;

            slicedSprites.Clear();

            Vector2 pivot = new Vector2(0.5f, 0.5f);

            if (sheetHorizontal)
            {
                float cellW = tex.width / (float)sheetCellCount;

                for (int i = 0; i < sheetCellCount; i++)
                {
                    slicedSprites.Add(
                        Sprite.Create(
                            tex,
                            new Rect(i * cellW, 0f, cellW, tex.height),
                            pivot,
                            spriteSheet.pixelsPerUnit));
                }
            }
            else
            {
                float cellH = tex.height / (float)sheetCellCount;

                for (int i = 0; i < sheetCellCount; i++)
                {
                    slicedSprites.Add(
                        Sprite.Create(
                            tex,
                            new Rect(0f, tex.height - (i + 1) * cellH, tex.width, cellH),
                            pivot,
                            spriteSheet.pixelsPerUnit));
                }
            }

            Debug.Log(
                $"[FigmaFlow] CombinationLock: sprite sheet szeletelve — " +
                $"{slicedSprites.Count} cella " +
                $"(sheet: {spriteSheet.name}, {tex.width}x{tex.height}, " +
                $"irány: {(sheetHorizontal ? "vízszintes" : "függőleges")}).");
        }

        // ---- IFigmaFlowTouchTarget ----

        /// <summary>
        /// A TUIO-input közvetlen hívása: a tárcsa a következő értékre fordul
        /// (a label flip animációval).
        /// </summary>
        public void OnTap(string elementName)
        {
            if (solved)
                return;

            if (!dialValues.ContainsKey(elementName))
                return;

            int current = dialValues[elementName];

            dialValues[elementName] = (current + step) % ValueCount;

            // A megjelenítés: a reel (sprite-os) vagy a label (flip).
            SlotMachineReel reel = GetReel(elementName);

            if (reel != null)
                reel.SetValue(dialValues[elementName]);
            else
                FlipLabel(elementName);

            if (autoCheck)
                CheckSolution();
        }

        void OnDialClick(ClickEvent evt)
        {
            if (solved)
                return;

            VisualElement target = evt.target as VisualElement;

            if (target == null)
                return;

            string dialName = target.name;

            OnTap(dialName);
        }

        void OnCheckClick(ClickEvent evt)
        {
            if (solved)
                return;

            CheckSolution();
        }

        /// <summary>
        /// A tárcsához tartozó reel (a dialNames sorrendjéből).
        /// </summary>
        SlotMachineReel GetReel(string dialName)
        {
            if (reels == null)
                return null;

            int idx = dialNames.IndexOf(dialName);

            if (idx < 0 || idx >= reels.Count)
                return null;

            return reels[idx];
        }

        // ---- A flip animáció ----

        /// <summary>
        /// A kép átfordulása (a slot-machine illúziója): a scale.x
        /// 1 → 0 → 1, a közepén vált a sprite.
        /// </summary>
        void FlipLabel(string dialName)
        {
            if (!dialImages.TryGetValue(dialName, out Image image) ||
                image == null)
            {
                // Nincs kép — az érték logikai, a CheckSolution úgyis fut.
                return;
            }

            StopCoroutine(FlipRoutine(image, dialName));

            StartCoroutine(FlipRoutine(image, dialName));
        }

        System.Collections.IEnumerator FlipRoutine(
            Image image,
            string dialName)
        {
            float t = 0f;

            float half = flipDuration * 0.5f;

            // 1 → 0 (összecsukódik).
            while (t < half)
            {
                t += Time.deltaTime;

                float s = 1f - Mathf.Clamp01(t / half);

                image.style.scale = new Scale(new Vector2(s, 1f));

                yield return null;
            }

            // A sprite váltása (a közepén).
            int current = dialValues[dialName];

            image.sprite = GetSprite(current);

            // 0 → 1 (kinyílik).
            t = 0f;

            while (t < half)
            {
                t += Time.deltaTime;

                float s = Mathf.Clamp01(t / half);

                image.style.scale = new Scale(new Vector2(s, 1f));

                yield return null;
            }

            image.style.scale = Scale.None();
        }

        // ---- A megoldás ellenőrzése ----

        void CheckSolution()
        {
            bool correct = true;

            for (int i = 0; i < dialNames.Count; i++)
            {
                if (i >= solution.Count)
                    break;

                if (!dialValues.TryGetValue(dialNames[i], out int v))
                {
                    correct = false;

                    break;
                }

                // A beállított index == a megoldás indexe.
                if (solution[i] < 0 || v != solution[i])
                {
                    correct = false;

                    break;
                }
            }

            if (correct)
            {
                solved = true;

                if (solvedState >= 0 && machine != null)
                    machine.GoToState(solvedState);

                onSolved?.Invoke();

                Debug.Log("[FigmaFlow] CombinationLock: MEGOLDVA.");
            }
            else
            {
                onWrong?.Invoke();

                Debug.Log("[FigmaFlow] CombinationLock: rossz kombináció.");
            }
        }
    }
}
