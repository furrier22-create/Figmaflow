# FigmaFlow – Development Handover

> **Verzió:** v0.6 ✅ STABLE (lezárva: 2026-08-02)
> **Aktív ág:** v0.7 — PuzzleKit (tervezés alatt)
> **Platform:** Unity 6 · UI Toolkit Runtime
> **Egyetlen igazságforrás:** minden döntés, ami a fejlesztés során születik, ide kerül bevezetésre.

---

## 1. Projekt

- **Név:** FigmaFlow
- **Platform:** Unity 6, UI Toolkit Runtime
- **Cél:** A Figma Smart Animate működésének reprodukálása Unity UI Toolkit alatt.
- **Jelleg:** Runtime Engine — **NEM animációs asset.**
- **Elsődleges feladat:** Figma state-ek közötti Smart Animate lejátszása.

## 1b. Termékvízió (kontextus, 2026-08-02)

A végső cél: **érintőfalon futó profi bemutatók** (múzeumi kiállítások, slideshow-k,
interaktív installációk) — projektorral vetített kép + lidar, ami már kommunikál a
Unity-vel (a lidar/érintőfal a projekten kívüli input-réteg).

A Figmából átemelt "desktopok" = állapotok. Példa-forgatókönyv:
- főmenü, amiből tab menüvel navigálunk,
- oldalról beúszó ablak további funkciókkal (másik gombbal),
- lapozható galéria,
- overlay elemek (dialog, bottom sheet, popup) a fenti elemek tetején.

**Hogyan felel meg a jelenlegi architektúra:**
- Figma desktopok → `FigmaStateMachine.states` — a `GoTo()/Next()/Previous()` a navigáció
- Oldalról beúszó ablak → a Bottom Sheet minta (`AppearMode.SlideUp`, P5)
- Overlay elemek → az Overlay pipeline (P4–P5: Dialog, Bottom Sheet)
- Indítási felvillanás nélküli váltás → `StateCache` (P3)
- Lapozható galéria / slideshow / időzített váltás → a v0.5 útiterv (Timeline, Preview
  Slider, PlayMaker, Motion Presets) — ezek a bemutató-flow-t szolgálják
- Érintőfal/lidar input → **a projekten kívüli réteg**; a demó input-kezelők
  (`DialogDemoInput` minta) a helyettesítők, amíg az input absztrakció nem kell

**A vízió NEM változtat a tervezett haladási útvonalon** — kontextusként szolgál a
prioritásokhoz (v0.3.7 ✅ → v0.4 P1–P5 → v0.5).

**Kiterjesztés — szabadulószoba (escape room) irány (2026-08-02, feljegyzés):**
- Cél: az érintőfal interaktív puzzle-ként is működhessen — slotokba pakolás
  (tap-tap, helyes sorrend), kombinációs lakat / cryptex, "egyszerre két pont
  érintése" feladatok stb. A design Figmában készül.
- Kulcs-meglátás: a puzzle-LOGIKA nem a Figma state-ekben élhet (a kombinatórikus
  állapottérből nem lehet state-et csinálni) — a Figma state-ek csak a vizuális
  visszajelzés (zárva/nyitva/rossz), a logika külön réteg (a PlayMaker mintájára).
- Ez a "PuzzleKit" irány (v0.7 jelölt) — a v0.6 után tervezendő.
- **MULTI-TOUCH IGÉNY (kötelező a PuzzleKit-hez):** a rendszernek kezelnie kell,
  hogy TÖBBEN érnek egyszerre az érintőfalhoz —
  (a) két különböző pont egyidejű érintése aktivál valamit (két-személyes feladat),
  (b) egy gyerek az egyik kezével TÁMASZKODIK a falon, a másikkal érint — a
  támaszkodást figyelmen kívül kell hagyni (a valódi érintés aktiváljon).
  A TUIO már több cursort ad (a jelenlegi FigmaFlowTuioInput az add-ra aktivál);
  a multi-touch logika (kombinált trigger, támaszkodás-szűrés) a TUIO-input
  bővítéseként / a PuzzleKit részeként valósítandó meg.

---

## 2. A projekt filozófiája — 3 réteg

```
Analysis
    ↓
Transition Build
    ↓
Motion Engine
```

- A Motion Engine nem tudhat: Figmáról, Overlay-ről, Popup-ról, Hero-ról, StateMachine-ről. **Csak animál.** Ez a projekt egyik legfontosabb szabálya.
- Minden Builder Editor jellegű; minden Runtime csak használ.
  - Példa: `TransitionBuilder` → `TransitionPlayer`. **Soha** fordítva.
- A StateMachine kizárólag Runtime vezérlő: `GoTo()`, `GoToState()`, `Next()`, `Previous()`, transition indítás, Commit. Nem lehet: Builder, Analyzer, Overlay Engine.
- A Runtime nem függhet a Figma Bridge implementációjától. Kizárólag ezt ismeri:
  - `VisualTreeAsset`
  - `FigmaStateAsset`
  - `TransitionAsset`
- Minden VisualElement keresés a `VisualTreeUtility`-n keresztül történik. **Nem:** `Q(name)`, `root[0]`, saját path parser. Minden egy helyen.

---

## 3. Könyvtárstruktúra — CÉLÁLLAPOT (v0.3.7 végére)

Jelmagyarázat: 🔒 = Motion Engine Freeze · ⚙️ = Builder/Editor jellegű · 🧪 = Experimental · 📦 = Data · ➜ = átkerül (ADR hivatkozással)

```
Assets/FigmaRuntime
├── Core
│   ├── AnimatedProperties.cs
│   ├── IPropertyTrack.cs          🔒
│   ├── ITransitionPlayer.cs
│   ├── TrackCandidate.cs          (Analysis kimeneti típusa)
│   ├── Transition.cs              🔒
│   ├── TransitionAnalysis.cs      (Analysis réteg)
│   ├── TransitionBuilder.cs       ⚙️ (TrackCandidate → ElementTrack)
│   └── VisualTreeUtility.cs       (egyetlen keresési belépési pont)
│
├── Runtime
│   ├── ElementTrack.cs            🔒
│   ├── FigmaDocumentHost.cs
│   ├── FigmaFlowRuntime.cs        (fő runtime, életciklus)
│   ├── FigmaStateMachine.cs       (diagnostics: LogTransition — ADR-003)
│   ├── MotionUtility.cs           🔒
│   ├── StateCapture.cs            (Startup Flash workaround része)
│   └── TransitionPlayer.cs        🔒
│
├── Tracks
│   ├── PositionTrack.cs           🔒
│   ├── SizeTrack.cs               🔒
│   └── OpacityTrack.cs            🔒
│
├── Data
│   ├── FigmaFlowProfile.cs        📦
│   ├── FigmaStateAsset.cs         📦
│   └── TransitionAsset.cs         📦
│
├── Editor
│   ├── FigmaFlowWindow.cs         (Motion Studio — ADR-004)
│   ├── TransitionAssetBuilder.cs  ⚙️ ➜ Runtime-ból ide (ADR-005)
│   ├── TransitionAssetEditor.cs
│   └── TransitionAssetGenerator.cs ⚙️
│
├── Experimental 🧪
│   ├── README.md                  (karanténszabályok)
│   ├── Overlay/                   (15 kutatási fájl — lásd lent)
│   └── StateCache/                StateCache.cs, StateCacheBuilder.cs (ADR-001, nem használt)
│   └── (Spike — TÖRÖLVE, ADR-002)
│
└── Samples
```

**Experimental/Overlay tartalma (15 fájl, 2026-08-01 — a 12-es lista kiegészült: `OverlayDocumentHost`, `OverlayInstance`, `OverlayRenderItem`):**
`OverlayAnimationItem.cs` · `OverlayBinder.cs` · `OverlayDocumentHost.cs` · `OverlayInstance.cs` · `OverlayLayer.cs` · `OverlayRenderData.cs` · `OverlayRenderItem.cs` · `OverlayRenderPair.cs` · `OverlayRenderer.cs` · `OverlayRuntime.cs` · `OverlayScene.cs` · `OverlayTransitionBuilder.cs` · `OverlayTransitionPlayer.cs` · `IRenderNode.cs` · `RenderNode.cs`

**Áthelyezések a v0.3.7 cleanup során:**

| Fájl | Honnan | Hova | Indok |
|---|---|---|---|
| `TransitionAssetBuilder.cs` | `Runtime/` | `Editor/` | ADR-005/008 — Builder Editor jellegű ✅ végrehajtva |
| `StateCache.cs`, `StateCacheBuilder.cs` | (nem használt) | `Experimental/StateCache/` | ADR-001 — a v0.4 editor-oldali cache alapja ✅ |
| `OverlaySpike.cs`, `VisualSnapshotSpike.cs` | `Spike/` | — | ADR-002 — **törölve** (2026-08-01) |

---

## 4. API specifikáció

### 4.1 FigmaStateMachine

Szerep: kizárólag Runtime vezérlő. Nem végez elemzést, renderelést, Motion logikát, Overlay logikát.

**Publikus API:**
```
GoTo(string stateName)
GoToState(int index)
Next()
Previous()
GoTo0()
GoTo1()
GoTo2()
```

- `GoTo()` és `GoToState()` a hivatalos API.
- A többi kényelmi függvény.

**Állapotváltás életciklusa:**
```
GoTo()
  ↓
Analyze()          → TransitionAnalysis
  ↓
Build()            → TransitionBuilder
  ↓
Play()             → TransitionPlayer
  ↓
Finished
  ↓
CommitState()
  ↓
currentState frissítés
```

### 4.2 TransitionPlayer

Szerep: NEM épít Transition-t. Nem ismeri a StateMachine-t. Nem tud Overlay-ről.

```
Transition
  ↓
Evaluate()
  ↓
Finished Event
```

- **Esemény:** `Finished` — ezt figyeli a StateMachine.
- A **Commit nem a Player feladata.**

---

## 5. Startup Flash

### 5.1 Jelenség

Induláskor a StateMachine minden State-et egymás után betölt, hogy cache-t építsen:

```
Load(State0) → Capture
Load(State1) → Capture
Load(State2) → Capture
Load(State0)
```

Következmény: a felhasználó röviden láthatja a különböző Desktopokat.

### 5.2 Jelenlegi workaround (v0.3.7)

```
HideRoot()
  ↓
Capture
  ↓
ShowRoot()
```

- Jelenleg megfelelően működik.
- **Nem végleges architektúra** — v0.3.7 workaround.

### 5.3 Hosszú távú terv (várhatóan v0.4)

A State cache editor oldalon készüljön el; a Runtime többé ne építsen cache-t:

```
Import
  ↓
StateCache
  ↓
Runtime
  ↓
Play
```

---

## 6. Assembly Definitions

**Jelenlegi állapot:** nincs külön asmdef → `Assembly-CSharp`.

**Ajánlott jövőbeli struktúra (v0.4 eleje):**

| asmdef | Tartalom |
|---|---|
| `FigmaRuntime.asmdef` | Runtime, Core, Tracks, Data |
| `FigmaRuntime.Editor.asmdef` | Motion Studio, Editors, Generators |
| `FigmaRuntime.Experimental.asmdef` | Overlay, Spike, Prototype |

**Előnyök:** gyorsabb fordítás, tisztább függőségek, Editor/Runtime szétválasztás, Experimental izoláció.

**Ütemezés:** NEM prioritás v0.3.7-ben. Ajánlott a v0.4 elején.

---

## 7. Motion Engine Freeze

A következő fájlok stabilnak tekintendők. **Csak kritikus hiba esetén módosíthatók:**

```
Transition.cs
TransitionPlayer.cs
ElementTrack.cs
PositionTrack.cs
SizeTrack.cs
OpacityTrack.cs
MotionUtility.cs
IPropertyTrack.cs
```

- Minden új funkció ezen kívül épüljön.
- Módosítási igény esetén először: Builder, Runtime, Render Pipeline, életciklus vizsgálandó — a hiba valószínűleg ott van, nem a Motion Engine-ben.

---

## 8. Bizonyított / nem bizonyított

**Bizonyított:**
- ✅ Motion Engine megfelelő
- ✅ Track rendszer megfelelő
- ✅ TransitionBuilder megfelelő
- ✅ StateMachine megfelelő
- ✅ Runtime Analysis megfelelő
- ✅ Shoes Demo működik
- ✅ Tabs Demo működik
- ✅ `GoTo()`, `GoToState()`, 0→2, 2→0, 2→1 működik

**Nem bizonyított (Ghost):**
- A Ghost kizárólag egy kézzel készített Unity Tabs demóban jelent meg (nem Figma export), átlátszó háttérrel; háttér hozzáadásával eltűnt.
- **Következtetés:** A Ghost NEM bizonyítja a Motion Engine hibáját, nem indokol átírást.

---

## 9. Feature státuszok

### Smart Overlay (v0.4)

- **Nem bugfix** — új render pipeline.
- Csak valódi Figma Overlay esetén kell: Dialog, Bottom Sheet, Popup, Dropdown, Context Menu, FAB Morph, Hero Transition, Shared Element.
- A Shoes Demo nem igényli; a Tabs Demo nem bizonyítja.

### Overlay kutatási kód (jelenlegi státusz)

Készült (félkész kutatási eredmények):
`OverlayScene`, `OverlayRenderer`, `OverlayBinder`, `OverlayTransitionBuilder`, `RenderNode`, `IRenderNode`, `OverlayLayer`, `OverlayAnimationItem`, továbbá: `OverlayRuntime`, `OverlayTransitionPlayer`, `OverlayRenderData`, `OverlayRenderPair`.

- **Nem részei a Stable Runtime-nak.**
- **Experimental státuszban maradnak.**

### Runtime Diagnostics

- Maradjon, később fejlődjön.
- Elvárt log: **Current State · Target State · Candidate Count · Track Count · Duration · Player**

---

## 10. Útiterv

| Verzió | Tartalom | Státusz |
|---|---|---|
| **v0.3.7** | Production Ready Runtime · Motion Engine Freeze · Runtime Cleanup · Diagnostics · Stabil kiadás | ✅ LEZÁRVA (2026-08-01) |
| **v0.4** | Figma Overlay Support (Dialog, Bottom Sheet, Popup) · State cache editor-oldali generálás · asmdef szeparáció | ✅ LEZÁRVA (2026-08-02) |
| **v0.5** | Preview Slider · Timeline · Shared Element · Hero · PlayMaker · Motion Presets · OverlayLayer · input absztrakció | ✅ LEZÁRVA (2026-08-02) |
| **v0.6** | TUIO érintőfal input (ADR-026) · Motion Presets · Dropdown/scale-in · editor-only StateCache | ✅ LEZÁRVA (2026-08-02) |
| **v0.7** | PuzzleKit (szabadulószoba) — slot-puzzle, kombinációs zár, multi-touch logika | 📋 FELJEGYEZVE (a v0.6 után) |

**A v0.4 csak a v0.3.7 lezárása után indult — ez megtörtént.**

---

## 11. Regressziós teszt lista (v0.3.7 gate)

- [ ] Shoes demo
- [ ] Tabs demo
- [ ] Runtime Analysis
- [ ] `GoTo()`
- [ ] `GoToState()`
- [ ] 0→2
- [ ] 2→0
- [ ] 2→1
- [ ] Motion Studio: Duration
- [ ] Motion Studio: Delay
- [ ] Motion Studio: Ease
- [ ] Motion Studio: Custom Curve

---

## 12. A legfontosabb mérnöki szabály

> Ha egy új funkció miatt hozzá kell nyúlni a Motion Engine-hez, akkor valószínűleg rossz helyen történik a fejlesztés. Elsőként mindig a Builder, a Runtime, a Render Pipeline és az életciklus vizsgálandó. **A Motion Engine legyen a projekt legstabilabb része.**

---

## 13. Határozatok (döntésnapló)

A fejlesztés során született döntések az `ADRs.md`-ben vezetve: ADR-001 (StateCache → Experimental), ADR-002 (Spike → Experimental), ADR-003 (Diagnostics a FigmaStateMachine.LogTransition-ben), ADR-004 (Motion Studio == FigmaFlowWindow), ADR-005 (TransitionAssetBuilder → Editor), ADR-006 (asmdef a v0.4 elején).

**Zárt kérdések:**
1. ~~StateCache / StateCacheBuilder / Spike helye~~ → `Experimental/StateCache/`, `Experimental/Spike/` (ADR-001, ADR-002)
2. ~~Runtime Diagnostics helye~~ → `FigmaStateMachine.LogTransition(...)` (ADR-003)
3. ~~Motion Studio azonosítása~~ → `FigmaFlowWindow` (ADR-004)
4. ~~TransitionAssetBuilder a Runtime-ban~~ → Editor-ba költözik (ADR-005)
