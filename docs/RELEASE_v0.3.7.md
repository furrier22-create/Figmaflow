# v0.3.7 — STABLE RELEASE

> **Kiadás dátuma:** 2026-08-01
> **Jelleg:** Production Ready Runtime · Stabil baseline
> **Előző verzió:** v0.3.7 (stabilizációs ág — ez a verzió a cleanup eredménye)
> **Kapcsolódó:** `DEVELOPMENT_HANDOVER.md` · `v0.3.7_STABILIZATION_PLAN.md` · `v0.3.7_CLEANUP_LEDGER.md` · `ADRs.md`

---

## 1. Mi ez a kiadás

A v0.3.7 a FigmaFlow **Stable baseline** kiadása. A cél a handover szerint:
Production Ready Runtime · Motion Engine Freeze · Runtime Cleanup · Diagnostics · Stabil kiadás.

**Ez a kiadás NEM tartalmazza a Smart Overlay-t** — az külön fejlesztési ág (v0.4), új render pipeline.

---

## 2. Változások ebben a kiadásban

### Építészeti javítások

| # | Változás | Részletek |
|---|---|---|
| **F1** | Overlay ág kivezetve a `FigmaStateMachine`-ből | A Stable StateMachine nem hivatkozik többé az Experimental Overlay kódra; `AnimationMode.Overlay` → warning + Layout fallback (ADR-007) |
| **F12** | `TransitionAssetBuilder` átkerült az **Editor**-ba | A "Minden Builder Editor jellegű" szabály teljesül (ADR-005/008); a Runtime-ból senki nem hívta |
| **F17** | `Q(name)` kivezetve a `TransitionBuilder`-ből | `VisualTreeUtility.FindByPath` — a handover szabálya betartva; a `FindByPath` névtelen-átlátszó navigációt kapott (konzisztencia a StateCapture path-szemantikájával) |
| **F21** | Profile toleranciák bekötve | `TransitionBuilder.Analyze` tolerancia paraméterekkel (default: 1f / 1f / 0.01f); a `FigmaFlowProfile` mezői élnek |
| **F4** | Robusztusság | Guardok: üres `states[]`, null profile, korai `GoToState`, null asset, üres transition → közvetlen commit (a gép nem akad fenn) |
| **F2/F3** | Log cleanup | A diagnostics a handover szerinti mezők: From/To/Candidates/Tracks/Duration/Player; a debug logok `#if UNITY_EDITOR` mögött |
| **F25** | CustomSpring figyelmeztetés | A Motion Studio jelzi, ha stub-ot választasz (v0.4-ig lineáris) |
| **F20/F9** | Apró takarítás | `FigmaStateAsset` indentálás; felesleges using |

### Motion Engine Freeze

A 8 freeze-elt fájl **fejléc kommentet** kapott — a freeze mostantól a kódban is látható:
`Transition` · `TransitionPlayer` · `ElementTrack` · `MotionUtility` · `IPropertyTrack` · `PositionTrack` · `SizeTrack` · `OpacityTrack`

Csak kritikus bug esetén módosíthatók, dokumentált döntéssel (ADR-folyamat).

### Experimental karantén

- **Overlay** (15 fájl) — félkész kutatási eredmények, a v0.4 alapja
- **StateCache / StateCacheBuilder** — nem használt; a Startup Flash editor-oldali megoldását készíti elő (ADR-001)
- **Spike** — feladatukat elvégezték (ADR-002)

---

## 3. Ismert limitációk (dokumentált, nem hiba)

| Limitáció | Sorsa |
|---|---|
| `CustomSpring` ease stub — lineárisan viselkedik (F7) | v0.4 implementáció; a Motion Studio figyelmeztet |
| `rotation`/`scale` nem capture-ölve (F8) | v0.5 (Timeline) környéke |
| `PositionTrack` translate delta pontosság, ha a szülő elrendezése változik és az elem nem absolute (F13) | v0.5 (Shared Element) |
| `TransitionItem.animationMode` tétlen a Layout pipeline-ban (F22) | v0.4 (per-item Overlay) előkészület |
| `ElementTrack.RenderPair` Overlay függőség a freeze-elt kódban (F6) | v0.4 elején, az asmdef split ELŐTT kivezetendő (ADR-007) |
| Startup Flash: `HideRoot → Capture → ShowRoot` workaround | a végleges (editor-oldali StateCache) megoldás v0.4 |

---

## 4. Regressziós teszt — GATE

> A kiadás csak akkor érvényes, ha ez a lista teljesül.

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

**Külön figyelem:** az F17 (path-alapú keresés) szigorúbb, mint a korábbi `Q(name)` — ha egy elem kimarad az animációból, a neve a live fán és a capture path-ben eltér.

---

## 5. A kiadás tartalma (fájlok)

- **Javított fájlok:** `Core/TransitionBuilder.cs` · `Core/VisualTreeUtility.cs` · `Runtime/FigmaStateMachine.cs` · `Runtime/FigmaDocumentHost.cs` · `Runtime/StateCapture.cs` · `Runtime/FigmaFlowRuntime.cs` · `Editor/TransitionAssetBuilder.cs` (új) · `Editor/TransitionAssetEditor.cs` · `Data/FigmaStateAsset.cs` + 8 freeze-fájl fejléccel
- **Diff fájlok:** `docs/changes/` — F1, F17×2, F12, F20, batch2×5
- **Alkalmazási útmutató:** `docs/HOGYAN_ALKALMAZD.md`

---

## 6. Lezárás

- [x] Regresszió a §4 szerint — ✅ (2026-08-01, projektvezetői megerősítés: fut minden, nincs hiba)
- [x] Verziószám rögzítve: **v0.3.7**
- [x] Ág lezárva → **v0.4 elindítva** (Overlay + StateCache + asmdef) — kickoff: `docs/v0.4_KICKOFF.md`
