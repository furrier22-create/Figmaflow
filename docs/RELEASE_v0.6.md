# v0.6 — STABLE RELEASE

> **Dátum:** 2026-08-02
> **Jelleg:** az érintőfal (TUIO) hivatalos input-rétege + a Motion Studio finomításai
> **Előző:** v0.5 STABLE (2026-08-02)
> **Kapcsolódó:** `DEVELOPMENT_HANDOVER.md` · `v0.6_TUIO_KICKOFF.md` · `ADRs.md` (026–029)

---

## 1. Mi ez a kiadás

A termékvízió (múzeumi kioszk, érintőfal + lidar) élesítése: a **TUIO érintés-adatok**
a FigmaFlow hivatalos input-rétegeként csatlakoznak, a Motion Studio pedig
kényelmesebbé vált (presets, görgetés, editor-only cache).

**A Motion Engine Freeze továbbra is érvényben** — a v0.3.7 óta egyetlen freeze-elt
fájl sem változott funkcionálisan.

---

## 2. Változások (v0.6)

| # | Tartalom | Státusz |
|---|---|---|
| 1 | **TUIO input** (ADR-026) — `FigmaFlowTuioInput` (IFigmaFlowInput): cursor → panel-pozíció, `Pick` → elem, **mapping** (elemnév + `fromState` állapot-feltétel → `GoToState`), mapping nélkül ClickEvent, `Activate()` az idle-hez, **egér-szimuláció** teszteléshez | ✅ |
| 2 | **Motion Presets** (ADR-027) — mentett ease/duration/appearMode kombinációk, "Apply to All Items" | ✅ |
| 3 | **Dropdown + popup scale-in** (ADR-028) — `AppearMode.Scale` (középpontból nő, Size+Opacity), Dropdown demo | ✅ |
| 4 | **Motion Studio görgethető** (a hosszú ablak) | ✅ |
| 5 | **Editor-only StateCache** (ADR-029) — `ExportOffscreen`: a cache Play Mode nélkül, offscreen méréssel (a runtime változatlan) | ✅ |
| 6 | **Multi-touch** (ADR-030/031) — simogatás/drag (move-enter), drop-helyreállítás (a lidar jel-vesztése), gomb-lock (a tenyeres több-cursor), kombinált trigger (időablak + folyamatos nyomvatartás), onActivate/onDeactivate UnityEvent-ek (mint a UI Button onClick), szelektív növekedés-visszajelzés (pressFeedbackElements) | ✅ |

---

## 3. TUIO-integráció (a lényeg)

```
TUIO küldő (érintőfal) → TuioSessionBehaviour → FigmaFlowTuioInput
  → cursor (normalizált, Y lefelé: flipY=false) → panel-pozíció
  → Pick → elem → MAPPING (elemnév + fromState → GoToState)
  → mapping nélkül → ClickEvent (a meglévő kezelők élnek)
  → Activate() → PlayMaker idle-reset
  → egér-szimuláció (simulateWithMouse) a teszteléshez
```

- **Mapping állapot-feltétel:** ugyanaz a `Button_A` név a 0. állapotban 0→1-re,
  az 1. állapotban 1→0-ra vihet (a név az animáció path-alapja, nem változtatható)
- A komponens a **`Assets/FigmaRuntime-Tuio/`** mappában (az asmdef-fán kívül,
  Assembly-CSharp) — a FigmaRuntime core nem függ a TUIO-tól
- A **TuioNet/TuioUnity** API-hoz igazítva (Tuio11, csak OnCursorAdd, System.Numerics.Vector2)

---

## 4. Regressziós GATE (a v0.6 lezárásához)

- [x] A v0.5 teljes stack (runtime, Motion Studio, PlayMaker, per-pair, rotation) ✅
- [x] TUIO: cursor → elem → mapping/click (éles) ✅
- [x] Egér-szimuláció: a mapping egérrel is működik (editor play mode) ✅
- [x] PlayMaker idle: a TUIO érintésre is resetel (Activate) ✅
- [x] Motion Presets: mentés + Apply to All ✅
- [x] Dropdown: scale-in, toggle, click-away, barrier ✅
- [x] Editor-only StateCache: edit módban exportál, a runtime betölti ✅
- [x] Multi-touch: simogatás, toggle (drop-helyreállítás), tenyér (gomb-lock), kombinált (időablak + hold), események, visszajelzés ✅
- [x] Motion Studio: görgethető ✅
- [x] A meglévő demók (egér) változatlanok ✅

---

## 5. Dokumentáció és ADR-k

- **ADR-026** TUIO input · **ADR-027** Motion Presets · **ADR-028** Dropdown + scale-in · **ADR-029** Editor-only StateCache
- **Telepítések:** `v0.6_TUIO_KICKOFF.md` · `v0.6_2_PRESETS_INSTALL.md` · `v0.6_3_DROPDOWN_INSTALL.md` · `v0.6_5_CACHE_INSTALL.md`
- **Handover:** §1b kiterjesztés — szabadulószoba irány + MULTI-TOUCH igény (v0.7 feljegyezve)

---

## 6. Lezárás / következő lépés

- [x] Regresszió a §4 szerint ✅ (2026-08-02, projektvezetői tesztek)
- [x] **v0.6 LEZÁRVA (2026-08-02)**
- [ ] → **v0.7 tervezés:** PuzzleKit (szabadulószoba) — slot-puzzle, kombinációs zár,
      **multi-touch logika** (két pont egyidejű érintése, támaszkodás-szűrés)
      — a handover §1b-ben feljegyezve
