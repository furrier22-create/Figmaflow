# v0.4 — STABLE RELEASE

> **Dátum:** 2026-08-02
> **Jelleg:** Figma Overlay Support — a v0.4-es ág lezárása
> **Előző:** v0.3.7 STABLE (2026-08-01) · v0.4 P1–P4 köztes (2026-08-02)
> **Kapcsolódó:** `DEVELOPMENT_HANDOVER.md` · `v0.4_KICKOFF.md` · `ADRs.md` (009–015)

---

## 1. Mi ez a kiadás

A v0.4-es ág (Figma Overlay Support) teljes lezárása. A Smart Overlay — a handover
szerint **új render pipeline, nem bugfix** — elkészült, és három valódi esettel
bizonyítva: Dialog, Bottom Sheet, Popup. Emellett a Startup Flash végleges
megoldása (editor-oldali StateCache) és az asmdef szeparáció is éles.

**A Motion Engine Freeze továbbra is érvényben** — a v0.3.7 óta egyetlen freeze-elt
fájl sem változott funkcionálisan.

---

## 2. Változások (P1–P5)

| Fázis | Tartalom | Státusz |
|---|---|---|
| **P1** | F6: Overlay-függőség kivezetése az `ElementTrack`-ből (ADR-007) — a Motion Engine újra tiszta | ✅ |
| **P2** | asmdef szeparáció: `FigmaRuntime` / `.Editor` / `.Experimental` (az Experimental autoReferenced: false) | ✅ |
| **P3** | Editor-oldali StateCache (ADR-009/010) — Startup Flash megszűnése, scene-alapú cache | ✅ |
| **P4** | Smart Overlay MVP — Dialog (ADR-011–014): driver seam, appear/disappear, Dialog demo | ✅ |
| **P5** | Bottom Sheet (ADR-015: SlideUp) + Popup (click-away, toggle) — a pipeline sablonként működik | ✅ |

### A v0.4-ben bizonyított esetek

| Eset | AppearMode | Különlegesség |
|---|---|---|
| **Dialog** | Fade | sötétítő, barrier, gomb+háttér bezárás |
| **Bottom Sheet** | SlideUp | alulról felcsúszás (élő-geometriás slide) |
| **Popup** | Fade | nincs sötétítő, click-away, toggle, menüpont-zárás |

**Mindhárom új engine-funkció nélkül fut** — a pipeline sablonként működik.

---

## 3. Építészeti eredmények

- **Driver seam (ADR-012):** `IOverlayDriver` a Stable-ben, `OverlayRuntime` implementálja az Experimentalben — a Stable nem tud az Experimentalről (asmdef-konform)
- **Appear/disappear (ADR-014):** a csak az egyik állapotban létező elemek fade-elnek (szintetikus opacity-0 oldal) — a Smart Animate paritás; a Motion Engine érintetlen
- **AppearMode (ADR-015):** per-item `Fade` / `SlideUp` — a SlideUp az élő elem geometriájából számol (a `Prepare` utáni layout-várakozással)
- **StateCache (ADR-009/010):** `FigmaStateCacheAsset` + `StateCacheExporter` — scene-enkénti útvonal, a workaround (HideRoot) nem fut többé
- **Ghost:** Overlay módban átlátszó háttéren is eltűnt (ADR-013) — a v0.3.7 döntés igazolva

---

## 4. Ismert határok (dokumentált, nem hiba)

| Limitáció | Sorsa |
|---|---|
| Az `OverlayLayer` jelenleg üres — a trackek az élő (current) elemeken animálnak | v0.5 (animációs reprezentáció, a Hero/Shared Element alapja) |
| A demó input-kezelők (Dialog/BottomSheet/Popup input) külön scriptek — nincs közös input absztrakció | v0.5 (az érintőfal/lidar input-réteg kapcsán) |
| A StateCache export Play Mode-os (valódi layout) — editor-only offscreen finomítás | v0.5 |
| A popup scale-in / dropdown lista-variáció | v0.5 |
| `CustomSpring` ease stub (F7) | v0.5 |
| Layout mód: a csak-célban létező elemek kimaradnak (nincs élő elemük) — az Overlay az elsődleges út | dokumentált különbség |

---

## 5. Regressziós GATE (a v0.4 lezárásához)

- [x] Shoes demo — Layout mód ✅
- [x] Tabs demo — Layout mód ✅
- [x] Tabs demo — Overlay mód (nincs ghost) ✅
- [x] 0→2, 2→0, 2→1 ✅
- [x] Motion Studio: Duration / Delay / Ease / Custom Curve ✅
- [x] StateCache: scene-enkénti export, betöltés, rossz-scene figyelmeztetés ✅
- [x] Dialog demo (Fade, sötétítő, barrier, gomb+háttér) ✅
- [x] Bottom Sheet demo (SlideUp, alulról felcsúszás) ✅
- [x] Popup demo (click-away, toggle, menüpont-zárás) ✅
- [x] Console: nincs NRE / hiba ✅

---

## 6. Dokumentáció és ADR-k

- **ADR-009** StateCache formátum · **ADR-010** StateCache több scene · **ADR-011** ITransitionPlayer.Finished
- **ADR-012** Overlay driver seam · **ADR-013** Ghost megerősítés · **ADR-014** Appear/Disappear · **ADR-015** AppearMode (SlideUp)
- **Telepítések:** `v0.4_ASMDEF_INSTALL.md` · `v0.4_P3_STATECACHE_INSTALL.md` · `v0.4_P4_OVERLAY_INSTALL.md` · `v0.4_P5_BOTTOMSHEET_INSTALL.md` · `v0.4_P5_POPUP_INSTALL.md`

---

## 7. Lezárás / következő lépés

- [x] Regresszió a §5 szerint ✅ (2026-08-02, projektvezetői tesztek)
- [x] **v0.4 LEZÁRVA (2026-08-02)**
- [ ] → **v0.5 indul** (Preview Slider, Timeline, Shared Element, Hero, PlayMaker, Motion Presets + OverlayLayer + finomítások)
