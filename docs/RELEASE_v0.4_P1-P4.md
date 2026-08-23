# v0.4 — KÖZTES RELEASE (P1–P4)

> **Dátum:** 2026-08-02
> **Jelleg:** v0.4-es ág első mérföldköve — Overlay infrastruktúra + StateCache + Overlay MVP
> **Előző:** v0.3.7 STABLE (lezárva 2026-08-01)
> **Kapcsolódó:** `DEVELOPMENT_HANDOVER.md` · `v0.4_KICKOFF.md` · `ADRs.md` (009–014)

---

## 1. Mi ez a kiadás

A v0.4-es ág első lezárt mérföldköve. Cél: a Smart Overlay (új render pipeline) alapjainak
lerakása, a Startup Flash végleges megoldása, és az első valódi Overlay eset (Dialog)
bizonyítása — mindezt a Motion Engine Freeze megtartásával.

**A v0.4 ág hátralévő része (P5):** Bottom Sheet / Popup esetek, OverlayLayer animációs
reprezentáció, editor-only StateCache mérés finomítás, v0.4 végleges kiadás.

---

## 2. Változások ebben a kiadásban (P1–P4)

| Fázis | Tartalom | Státusz |
|---|---|---|
| **P1** | F6: Overlay-függőség kivezetése az `ElementTrack`-ből (ADR-007, A irány) — a Motion Engine újra tiszta | ✅ |
| **P2** | asmdef szeparáció: `FigmaRuntime` / `.Editor` / `.Experimental` (izolált build, autoReferenced: false az Experimentalnél) | ✅ |
| **P3** | Editor-oldali StateCache (ADR-009/010) — `FigmaStateCacheAsset` + `StateCacheExporter` + scene-alapú útvonal + StateMachine cache-betöltés | ✅ |
| **P4** | Smart Overlay MVP — Dialog (ADR-011/012/013/014) — driver seam, appear/disappear, Dialog demo | ✅ |

### Részletek

**P1 — Motion Engine megtisztítása (freeze-eszkaláció, ADR-007)**
- `ElementTrack`-ből kivezetve: `RenderPair`, `HasRenderPair`, `Target`, `HideSource()`, `ShowSource()`
- A kötés a Builder/Binder szintre került — a Motion Engine nem tud Overlay-ről
- A freeze fejlécben a kivétel dokumentálva

**P2 — asmdef szeparáció**
- 3 assembly, a handover szerinti struktúra
- Az Experimental `autoReferenced: false` — a karantén a legerősebb formában
- A Stable `FigmaRuntime` nem hivatkozhat az Experimentalre (fordítási hiba jelzi)

**P3 — Startup Flash végleges megoldása (ADR-009/010)**
- `FigmaStateCacheAsset`: inline `CachedState` lista (nincs sub-asset)
- `StateCacheExporter`: Play Mode-os export a kész capture-ből; scene-enkénti útvonal
  `Assets/FigmaFlow/Cache/{scene}/...` (több scene támogatás)
- `FigmaStateMachine.TryLoadStateCache()`: érvényes cache → nincs runtime capture,
  nincs HideRoot workaround; hiány/eltérés → automatikus fallback a régi útra
- `sceneName` ellenőrzés betöltéskor (rossz scene cache → figyelmeztetés)

**P4 — Smart Overlay MVP: Dialog (ADR-011–014)**
- `IOverlayDriver` seam (Runtime) + `OverlayRuntime` implementáció (Experimental)
- `ITransitionPlayer.Finished` interfészre emelve — a StateMachine player-agnostic
- Appear/disappear támogatás a Builder-ben: megjelenő elem fade-in, eltűnő fade-out
  (szintetikus opacity-0 oldal) — a Motion Engine érintetlen
- Dialog demo (`Samples/DialogDemo/`): nyitás gombbal, bezárás gombbal + háttérkattintással,
  barrier a panelen, késleltetett GoToState (a ClickEvent dispatch újra-dispatch elkerülésére)
- **Bizonyítva:** Tabs demo Overlay módban, átlátszó háttéren — nincs szellemkép (ADR-013)

---

## 3. Bizonyított eredmények

- ✅ A **Ghost** eltűnt Overlay módban, átlátszó háttéren is — a v0.3.7 döntés igazolva
  (a Motion Engine nem volt hibás; a kétfa-megközelítés megoldja) — ADR-013
- ✅ A **megjelenő/eltűnő elemek** (dialog backdrop, panel) szépen fade-elnek — ADR-014
- ✅ A **Startup Flash** megszűnt StateCache-csel (a workaround nem fut)
- ✅ A **Layout út** változatlan a driver nélküli jeleneteken (a regressziós bizonyíték)

---

## 4. Ismert határok (dokumentált, nem hiba)

| Limitáció | Sorsa |
|---|---|
| Az `OverlayLayer` jelenleg üres — a trackek az élő (current) elemeken animálnak, nincs külön animációs másolat | P5 (animációs reprezentáció) |
| A dialog input kezelés demó-szintű (`DialogDemoInput`) — a root figyelő mintája | általánosítás P5 |
| A StateCache export Play Mode-os (valódi layout) — a tiszta editor-only offscreen mérés későbbi finomítás | P5 |
| `CustomSpring` ease stub (F7) — lineárisan viselkedik, a Motion Studio figyelmeztet | v0.4 vége |
| A Layout módban a csak-célban létező elemek továbbra is kimaradnak (nincs élő elemük) — az Overlay az elsődleges út | dokumentált különbség |

---

## 5. Regressziós GATE

> A kiadás csak akkor érvényes, ha ez a lista teljesül.

- [x] Shoes demo — Layout mód (az ADR-014 fade-ekkel) ✅
- [x] Tabs demo — Layout mód ✅
- [x] Tabs demo — Overlay mód (a ghost-mentesség újraellenőrzése) ✅
- [x] 0→2, 2→0, 2→1 menetek ✅
- [x] Motion Studio: Duration / Delay / Ease / Custom Curve ✅
- [x] StateCache: scene-enkénti export, betöltés, rossz-scene figyelmeztetés ✅
- [x] Dialog demo: nyitás / bezárás gombbal / bezárás háttérrel / barrier / nincs automatikus bezárás ✅
- [x] Console: nincs NRE / hiba a fenti menetekben ✅

**Regresszió megerősítve: 2026-08-02 (projektvezetői teszt).**

---

## 6. Dokumentáció és ADR-k

- **ADR-009** — StateCache formátum és előállítás (Play Mode-os export)
- **ADR-010** — StateCache több scene támogatása (scene-alapú útvonal)
- **ADR-011** — ITransitionPlayer.Finished interfészre emelése
- **ADR-012** — Overlay driver seam (IOverlayDriver)
- **ADR-013** — Ghost megerősítés az Overlay MVP alatt
- **ADR-014** — Appear/Disappear elemek (Smart Animate paritás)
- **Telepítések:** `docs/v0.4_ASMDEF_INSTALL.md` · `docs/v0.4_P3_STATECACHE_INSTALL.md` · `docs/v0.4_P4_OVERLAY_INSTALL.md`

---

## 7. Lezárás / következő lépés

- [x] Regresszió a §5 szerint ✅ (2026-08-02, projektvezetői megerősítés)
- [x] **P1–P4 LEZÁRVA (2026-08-02)**
- [ ] → **P5 indul** (Bottom Sheet / Popup / OverlayLayer / finomítások)
- [ ] A v0.4 végleges kiadása a P5 után
