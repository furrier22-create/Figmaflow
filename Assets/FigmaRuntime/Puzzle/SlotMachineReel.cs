using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.7 (ADR-032 kiegészítés): a klasszikus slot-machine dob (reel).
    ///
    /// A tárcsa-értékeket SPRITE-okkal jeleníti meg (a saját képeiddel),
    /// egy SpriteMask "ablakában" — a képek FEL/LE CSÚSZNAK, mint egy
    /// igazi gyümölcsös gép. A logika (a CombinationLock) változatlan,
    /// ez csak a megjelenítő réteg.
    ///
    /// ELRENDEZÉS (a GameObject-en):
    /// - ez a komponens (a "dob")
    /// - egy SpriteMask (a "ablak" — a látható rész)
    /// - a képek a dob alatt (SpriteRenderer-ek), amik a mask alatt
    ///   csúsznak
    ///
    /// A CombinationLock a SetValue(index)-szel lépteti.
    /// </summary>
    public class SlotMachineReel : MonoBehaviour
    {
        [Header("SlotMachineReel (v0.7)")]

        [Tooltip("A képek (sprite-ok) sorrendben — ezek között léptetünk.")]
        public List<Sprite> sprites = new();

        [Tooltip("A SpriteMask (a 'ablak' — a látható rész).")]
        public SpriteMask mask;

        [Tooltip("A kép-méret a dobban (a sprite-ok egységes méretre).")]
        public float cellHeight = 1f;

        [Tooltip("A csúszás hossza (mp).")]
        public float slideDuration = 0.2f;

        [Tooltip("A csúszás iránya: fel (true) vagy le (false).")]
        public bool slideUp = true;

        // ---- Állapot ----

        private readonly List<SpriteRenderer> cells = new();

        private int currentIndex;

        private float reelHeight;

        private bool ready;

        // ------------------------------------------------------------

        void Start()
        {
            BuildReel();
        }

        /// <summary>
        /// A dob felépítése: a sprites listából a cellák (SpriteRenderer-ek),
        /// a mask alatt, függőlegesen egymás alatt.
        /// </summary>
        void BuildReel()
        {
            if (sprites == null || sprites.Count == 0)
            {
                Debug.LogWarning(
                    $"[FigmaFlow] SlotMachineReel '{gameObject.name}': NINCS sprite a listában.");

                return;
            }

            // A cellák: a mask magasságában 3 cella (felső, középső, alsó) —
            // a csúszáshoz a szomszédos képek is látszanak.
            reelHeight = cellHeight;

            for (int i = 0; i < 3; i++)
            {
                GameObject go = new GameObject($"ReelCell_{i}");

                go.transform.SetParent(transform, false);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

                sr.sprite = sprites[(currentIndex + i - 1 + sprites.Count) % sprites.Count];

                sr.sortingOrder = 1; // a mask előtt? — a mask alatt kell lennie

                // A mask alá: a sortingLayer/maskInteraction beállítása.
                sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

                cells.Add(sr);
            }

            LayoutCells();

            ready = true;

            Debug.Log(
                $"[FigmaFlow] SlotMachineReel '{gameObject.name}': dob felépítve — " +
                $"{cells.Count} cella, {sprites.Count} sprite, " +
                $"mask={(mask != null ? mask.name : "NULL")}.");
        }

        void LayoutCells()
        {
            // A 3 cella: [felső, középső, alsó] — a középső a látható.
            for (int i = 0; i < cells.Count; i++)
            {
                float y = (i - 1) * cellHeight; // -1, 0, +1

                cells[i].transform.localPosition = new Vector3(0f, y, 0f);

                cells[i].transform.localScale =
                    new Vector3(1f, 1f, 1f);
            }
        }

        /// <summary>
        /// A látható érték beállítása (a CombinationLock hívja).
        /// A dob átszúszik a következő képig.
        /// </summary>
        public void SetValue(int index)
        {
            if (!ready)
            {
                // Ha még nem épült fel, csak a cél rögzítése.
                currentIndex = index;

                return;
            }

            if (sprites == null || sprites.Count == 0)
                return;

            int targetIndex = ((index % sprites.Count) + sprites.Count) % sprites.Count;

            Debug.Log(
                $"[FigmaFlow] SlotMachineReel '{gameObject.name}': SetValue " +
                $"{currentIndex} -> {targetIndex} (sprite: {sprites[targetIndex]?.name}).");

            if (targetIndex == currentIndex)
                return;

            // A következő kép betöltése a mozgó cellába.
            int delta = slideUp ? 1 : -1;

            int nextIndex = ((currentIndex + delta) % sprites.Count + sprites.Count) % sprites.Count;

            // A csúszás: a középső cella a delta irányába megy, és a
            // következő kép a túloldalról jön.
            StopCoroutine(SlideRoutine(targetIndex));

            StartCoroutine(SlideRoutine(targetIndex));
        }

        IEnumerator SlideRoutine(int targetIndex)
        {
            // A jelenlegi középső cella elmozdul (a delta irányába),
            // és a célkép a túloldalról jön.
            float dir = slideUp ? 1f : -1f;

            // A cellák előkészítése: a középső (0) a jelenlegi, a
            // mozgó (a delta irányában) a cél.
            float t = 0f;

            float startY = 0f;

            float endY = -dir * cellHeight; // a középső kimegy

            // A célkép a túloldali cellába.
            int center = 1; // a középső cella indexe a cells listában

            // A középső cella sprite-ja a cél.
            cells[center].sprite =
                sprites[targetIndex];

            // A csúszás.
            while (t < 1f)
            {
                t += Time.deltaTime / slideDuration;

                float e = Mathf.Clamp01(t);

                // A középső cella megy kifelé (a delta irányába).
                cells[center].transform.localPosition =
                    new Vector3(0f, Mathf.Lerp(startY, endY, e), 0f);

                // A másik oldali cella jön be.
                int other = center - (int)dir;

                if (other < 0 || other >= cells.Count)
                    other = (other + cells.Count) % cells.Count;

                cells[other].transform.localPosition =
                    new Vector3(0f, Mathf.Lerp(-endY, 0f, e), 0f);

                yield return null;
            }

            // A végállapot: a célkép a középsőn.
            cells[center].transform.localPosition = Vector3.zero;

            cells[center].sprite = sprites[targetIndex];

            // A többi cella vissza a helyére.
            LayoutCells();

            currentIndex = targetIndex;
        }

        /// <summary>
        /// A látható sprite (a középső cella) — a debug-hoz.
        /// </summary>
        public Sprite CurrentSprite =>
            cells.Count > 1 ? cells[1].sprite : null;
    }
}
