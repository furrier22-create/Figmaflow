# FigmaFlow – Architecture Decision Records

> Minden fejlesztési döntés ide kerül bevezetésre: ID, kontextus, döntés, státusz.
> Státusz: `Accepted` = érvényben · `Superseded` = felváltva · `Proposed` = javaslat

---

## ADR-001 — StateCache / StateCacheBuilder státusza

- **Dátum:** 2026-08-01
- **Kontextus:** A `StateCache.cs` és `StateCacheBuilder.cs` a Startup Flash jobb architektúrájának kutatására készült, de a fejlesztés közben kiderült, hogy a végleges megoldás **editor-oldali** state cache lesz (v0.4). A fájlok jelenleg **nincsenek használatban**, nem részei a működő Runtime-nak.
- **Döntés:** Áthelyezés ide: `Assets/FigmaRuntime/Experimental/StateCache/`. Nem kerülnek a Stable Runtime-ba. Sorsuk a v0.4-ben dől el (továbbfejlesztés vagy törlés).
- **Státusz:** Accepted

## ADR-002 — Spike mappa sorsa

- **Dátum:** 2026-08-01
- **Kontextus:** `Assets/FigmaRuntime/Spike/` kutatási célból jött létre (`OverlaySpike.cs`, `VisualSnapshotSpike.cs`). Feladatukat elvégezték; nem részei a Runtime-nak. A handover szabálya: "Nem töröljük. Áthelyezzük."
- **Döntés:** Áthelyezés ide: `Assets/FigmaRuntime/Experimental/Spike/`. Törlés később lehetséges, de nem v0.3.7 feladat.
- **Státusz:** ✅ Superseded — **TÖRÖLVE (2026-08-01, projektvezetői döntés):** a spike-ok feladatukat elvégezték, a kutatási eredményeik a v0.4 Overlay munkában hasznosulnak; a mappa nem létezik többé.

## ADR-003 — Runtime Diagnostics helye

- **Dátum:** 2026-08-01
- **Kontextus:** A handover elvárja a diagnosztikai logot (Current State, Target State, Candidate Count, Track Count, Duration, Player).
- **Döntés:** Marad a `FigmaStateMachine.LogTransition(...)` metódusban. Külön `RuntimeDiagnostics` osztály később (v0.4+) jöhet, nem v0.3.7 feladat. A log formátum egységesítése a v0.3.7-ben megtörténik.
- **Státusz:** Accepted

## ADR-004 — Motion Studio azonosítása

- **Dátum:** 2026-08-01
- **Kontextus:** A handover "Motion Studio"-t említ az Editor oldalon, de a struktúrában csak `FigmaFlowWindow` szerepelt.
- **Döntés:** Motion Studio == `FigmaFlowWindow`. Feladata: TransitionAsset generálása, Runtime Analysis importálása, TransitionAsset szerkesztése. Bővítés (Preview, Timeline, Categories, Merge, Preview Slider) később, de a név ugyanaz marad.
- **Státusz:** Accepted

## ADR-005 — TransitionAssetBuilder átkerül az Editor-ba

- **Dátum:** 2026-08-01
- **Kontextus:** `TransitionAssetBuilder` a `Runtime/`-ban található, pedig ScriptableObjectet ír (`EditorUtility.SetDirty()`), Assetet módosít — ez a "Minden Builder Editor jellegű" szabályt sérti. Ottlétének oka történelmi: a Runtime Analysis jelenleg Play Mode-ban készül, a Motion Studio Play után importál.
- **Döntés:** Áthelyezés ide: `Editor/TransitionAssetBuilder.cs`. A Play Mode-os analysis nem indokolja a Builder runtime-beli létét; a Builder átkerül, a Runtime Analysis marad a Runtime-ban (az Analysis külön réteg, az építés Editor feladat).
- **Státusz:** Accepted — végrehajtás a kód kéznél lévő állapotában (v0.3.7 cleanup)

## ADR-006 — asmdef szeparáció ütemezése

- **Dátum:** 2026-08-01
- **Kontextus:** Jelenleg nincs külön asmdef (`Assembly-CSharp`). A handover szerint a három asmdef (`FigmaRuntime`, `FigmaRuntime.Editor`, `FigmaRuntime.Experimental`) a v0.4 elején ajánlott.
- **Döntés:** NEM v0.3.7 feladat. A v0.4 elején bevezetendő, az Experimental izolálásához.
- **Státusz:** Accepted (ütemezett: v0.4)

## ADR-007 — Overlay függőségek a Stable kódban (műszaki adósság)

- **Dátum:** 2026-08-01
- **Kontextus:** A Runtime audit (v0.3.7_CLEANUP_LEDGER) két helyen talált Overlay-ismeretet a Stable kódban: (1) `FigmaStateMachine` hivatkozza az `OverlayTransitionPlayer`-t (Experimental), (2) a freeze-elt `ElementTrack` `OverlayRenderPair`-t használ. Előbbi v0.3.7-ben javítható (nem freeze fájl), utóbbi nem (Freeze).
- **Döntés:** F1 (StateMachine) kivezetése a v0.3.7 cleanup része. F6 (ElementTrack) freeze alatt marad: nem módosítjuk, a függőség az **asmdef szeparáció előtt** (v0.4 eleje) szüntetendő meg — ez az asmdef bevezetésének előfeltétele.
- **Státusz:** ✅ F1 VÉGREHAJTVA (2026-08-01, diff: `docs/changes/F1_overlay_removal_from_state_machine.diff`)
- **F6 — ✅ VÉGREHAJTVA (2026-08-01, A irány, projektvezetői döntés):**
  - `ElementTrack`-ből kivezetve: `RenderPair`, `HasRenderPair`, `Target`, `HideSource()`, `ShowSource()` — a track kizárólag azon az elemen animál, amivel létrehozták; a freeze fejlécben a kivétel dokumentálva
  - `OverlayBinder` újraírva: a kötés a párokon (OverlayRenderData) keresztül történik, nem a trackeken
  - `OverlayTransitionBuilder`: a `track.RenderPair = pair` eltávolítva; az animációs reprezentációra váltás pontja dokumentálva (a Builder szintjén)
  - `OverlayTransitionPlayer`: a Bind/Unbind a `runtime.RenderData`-ból dolgozik
  - **Diff-ek:** `docs/changes/F6_*.diff` ×4
  - Eredmény: a Motion Engine újra tiszta — az asmdef szeparáció (P2) előfeltétele teljesült

## ADR-008 — TransitionAssetBuilder: a költöztetés kiegészítése (F12)

- **Dátum:** 2026-08-01
- **Kontextus:** Az Editor réteg auditja (F12) megerősítette: a `TransitionAssetBuilder.Fill`-t kizárólag Editor kódból hívják (`FigmaFlowWindow`, `TransitionAssetGenerator`), a Runtime-ból senki. Emellett a `TransitionAssetGenerator.Generate` duplikálja a `Fill` lookup/merge logikáját.
- **Döntés:** (1) `Runtime/TransitionAssetBuilder.cs` törlése, (2) `Editor/TransitionAssetBuilder.cs` létrehozása a `Fill` logikával, (3) korrekció: a `Generate`-ben NINCS duplikált blokk — dedup nem volt szükséges.
- **Státusz:** ✅ Accepted — VÉGREHAJTVA (2026-08-01, diff: `docs/changes/F12_transitionassetbuilder_runtime_to_editor.diff`)

## ADR-009 — StateCache formátum és előállítás (v0.4 / P3)

- **Dátum:** 2026-08-02
- **Kontextus:** A kickoff P3 az editor-oldali state cache-ről: a Runtime ne építsen cache-t (Startup Flash végleges megoldása). A meglévő Experimental `StateCache`/`StateCacheBuilder` runtime-oldali irányt (ADR-001) elvetettük. Nyitott kérdés volt a formátum (FigmaStateAsset vs tömörített) és az előállítás módja.
- **Döntés:**
  - **Formátum:** `FigmaStateCacheAsset` (ScriptableObject, inline `List<CachedState>`; `CachedState` = stateName + `List<ElementData>`) — nincs sub-asset kezelés, egyszerű és robusztus (nincs árván maradt sub-asset, nincs GUID élek).
  - **Előállítás:** `Editor/StateCacheExporter` — Play Mode-os export a `FigmaStateMachine.GetCapturedStates()`-ból. Miért Play Mode? A capture valódi layoutot igényel (worldRect / resolvedStyle); a Play Mode-os mérés megbízható. A tiszta editor-only offscreen mérés (rejtett EditorWindow/UIDocument) későbbi finomítás — nem blokkol.
  - **Runtime:** `FigmaStateMachine.stateCache` mező; ha érvényes (darabszám egyezik a `states`-szel), a `Start()` a cache-ből építi a `capturedStates`-t — nincs capture ciklus, nincs HideRoot workaround. Eltérésnél / hiánynál: fallback a runtime capture-re (a v0.3.7 viselkedés).
  - **UI:** Motion Studio (`FigmaFlowWindow`) új "State Cache" szekció — export gomb + mező. Play Mode-ban az asset kiválasztódik, a beállítás edit mode-ban történik (play mode-i jelenet-módosítás nem maradna meg).
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-012 — Overlay driver seam (IOverlayDriver)

- **Dátum:** 2026-08-02
- **Kontextus:** A P4 (Smart Overlay MVP — Dialog) megköveteli, hogy a `FigmaStateMachine` az Overlay pipeline-t használja, de a Stable `FigmaRuntime` az asmdef miatt **nem hivatkozhat** az Experimentalre (a P2 izoláció pont ezért készült).
- **Döntés:** Függőség-megfordítás — driver seam:
  - **Stable (`Runtime/IOverlayDriver.cs`):** `IOverlayDriver` interfész (`IsAvailable`, `Player`, `Prepare`, `BuildTransition`, `Commit`) — a StateMachine ezt a szerződést ismeri, semmi mást.
  - **Experimental:** `OverlayRuntime` implementálja az interfészt (a meglévő `Prepare`/`BuildTransition`/`Commit` szignatúrák szinte 1:1 illeszkedtek; `Player` = `OverlayTransitionPlayer`).
  - **StateMachine:** `Awake`-ban `GetComponent<IOverlayDriver>()`; ha `profile.animationMode == Overlay` **és** van driver → overlay út (`useOverlay`), különben Layout út (változatlan). `SetPlayer` helper a biztonságos player cseréhez. `overlayCommit` flag: csak a tényleges overlay átmenet végén commitol a driver — a Layout fallback nem.
  - Az induló állapot Overlay módban a scene-en keresztül jelenik meg (`Prepare(states[0], states[0], null)`), hogy ne legyen duplikáció a host és az overlayRoot között.
- **Kimenet:** A Stable nem tud az Experimentalről; a Layout út változatlan marad (regressziós bizonyíték); az Overlay út a driveren át érhető el.
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-013 — Ghost megerősítés az Overlay MVP alatt (a v0.3.7 döntés igazolása)

- **Dátum:** 2026-08-02
- **Kontextus:** A v0.3.7 handover szerint a "Ghost" kizárólag egy kézzel készített Unity Tabs demóban jelent meg (nem Figma export), átlátszó háttérrel; a következtetés az volt, hogy NEM a Motion Engine hibája. A P4 (Overlay MVP) teszten a Tabs demó **Overlay módban, átlátszó háttéren** futott.
- **Eredmény (projektvezetői teszt):** az Overlay pipeline alatt **nincs szellemkép** — a Tabs demo átlátszó háttéren is tisztán animál.
- **Következtetés:** a Ghost a Layout pipeline (single-tree capture + commit swap) sajátossága volt; a kétfa-megközelítés (current + target + render párok, OverlayScene) ezt megszünteti. **A Motion Engine továbbra sem érintett, a freeze áll.**
- **Státusz:** ✅ Accepted — bizonyítva (2026-08-02)

## ADR-014 — Appear/Disappear elemek (Smart Animate paritás, P4-2)

- **Dátum:** 2026-08-02
- **Kontextus:** A B döntés (valódi overlay eset — Dialog) rávilágított: a `TransitionBuilder.Analyze` csak a MINDKÉT állapotban létező elemeket párosította. A csak a célban létező elemek (backdrop, dialog panel) nem kaptak tracket → a commitnál hirtelen "felpattantak" — ez a Figma Smart Animate-tel ellentétes.
- **Döntés:**
  - **Builder (Core, nem freeze):** a `Analyze` kiegészítve appear/disappear kandidátusokkal:
    - megjelenő (csak a célban): `From` = szintetikus másolat, `opacity 0` → fade-in
    - eltűnő (csak a forrásban): `To` = szintetikus másolat, `opacity 0` → fade-out
    - a szintetikus oldal pozíció/méret megegyezik, csak az opacitás 0 — a trackek nem-null adatokat kapnak, **a Motion Engine-be nem kellett nyúlni (freeze tartva)**
  - **OverlayRenderer:** a párok egyik oldala lehet null (megjelenő = csak Target, eltűnő = csak Current)
  - **OverlayTransitionBuilder:** élő elem = `Current ?? Target` — a megjelenő elem a target fában animálódik
  - **OverlayScene:** a target konténer a valódi pozíción, teljes méretben, rejtve (nem offscreen) — a megjelenő elemek a helyükön vannak
  - **OverlayBinder:** a megjelenő elemeket egyedileg láthatóvá teszi (0 opacitásról); az Unbind visszaállítja a stílusokat
  - **Layout mód hatása:** az eltűnő elemek fade-out-ja Layout módban is működik (a live elem létezik); a megjelenő elemek Layout módban továbbra is kimaradnak (nincs élő elemük) — ez a dokumentált különbség, az Overlay az elsődleges út.
- **Regressziós figyelmeztetés:** a Shoes/Tabs demókban mostantól az egyik állapotban hiányzó elemek fade-elnek — ez a kívánt Smart Animate viselkedés, de új tesztelést igényel.
- **Státusz:** ✅ Accepted — implementálva + **Dialog demóval bizonyítva** (2026-08-02): a megjelenő elemek (backdrop, dialog panel) fade-in, az eltűnők fade-out — projektvezetői teszt sikeres. A Layout regresszió (Shoes/Tabs) még hátravan.

## ADR-015 — AppearMode: a megjelenő/eltűnő elemek mozgása (P5, Bottom Sheet)

- **Dátum:** 2026-08-02
- **Kontextus:** A P5 első esete a Bottom Sheet — az alulról felcsúszó panel. A puszta fade (ADR-014) nem elegendő: a panelnek pozícióban is animálódnia kell (alulról fel, záráskor lefelé eltűnve).
- **Döntés:** per-item `AppearMode` (Data, a `TransitionItem`-en; `Fade` / `SlideUp`), a Motion Studio-ban választható:
  - `SlideUp` + megjelenő elem (`IsAppear`): a szintetikus `From` az elem saját magasságával **lefelé tolva** + `Position` property → alulról felcsúszik közben fade-in
  - `SlideUp` + eltűnő elem (`IsDisappear`): a szintetikus `To` lefelé tolva + `Position` → lefelé csúszva tűnik el
  - `Fade` (alapértelmezés): a jelenlegi viselkedés (csak opacity)
  - A `TrackCandidate` `IsAppear`/`IsDisappear` flag-eket kap (a Builder jelöli őket); a slide-eltolás a **Build**-ben történik (az item konfigurációjának ismeretében) — az Analyze továbbra is csak "mi változott"-at állapítja meg
  - A Motion Engine-hez NEM kell nyúlni (freeze tartva) — a trackek nem-null adatokat kapnak, a `Position` property a meglévő `PositionTrack`-et használja
- **Kimenet:** a Bottom Sheet (és bármely csúszó megjelenés) a meglévő pipeline-nal megvalósítható; a Dialog viselkedése változatlan (Fade).
- **Kiegészítés (2026-08-02 hotfix #4):** a slide pozíciók az **élő elem geometriájából** számítódnak (`resolvedStyle.left/top/height`) — a `FigmaStateMachine` egy frame-et vár a `Prepare` után, így a layout lefut és az értékek érvényesek (a késleltetett layout volt a `top=0 h=NaN` és a hibás capture-ből jövő `to.top=-176` oka). A capture csak fallback, ha az élő érték érvénytelen. A `bottom` anchor törlésre kerül a slide-olt elemeken. A `[FigmaFlow] SlideUp ...` logok diagnosztikai célt szolgálnak.
- **Státusz:** ✅ Accepted — implementálva + hotfix #4 (2026-08-02)

## ADR-016 — OverlayLayer: animációs reprezentáció (v0.5/1)

- **Dátum:** 2026-08-02
- **Kontextus:** Eddig a trackek az **élő (current) elemeken** futottak, a commit pedig a stílusokat próbálta visszaállítani (a P4/P5-ös megoldás). Ez működik, de: (1) az élő fa tranziens stílusokkal piszkolódik, (2) interrupciónál (GoToState lejátszás közben) a fa maradvány-stílusokat őrizhet, (3) a Hero/Shared Element (v0.5) számára nincs szabad "mozgó felület".
- **Döntés:** a layer-alapú animációs reprezentáció:
  - **Proxy-k:** `OverlayElementFactory` (új) — vizuális másolat az élő elemből (pozíció, méret, háttér, sarok, opacitás; Label/Button esetén szöveg + alapstílus). Box-szerű elemekre pontos; a teljes USS-öröklés nem (dokumentált korlát).
  - **Életciklus:** `IOverlayDriver.PrepareAnimationLayer(analysis)` (új interfész-tag) — a `Prepare` UTÁNI layout-várakozás után hívja a StateMachine; a proxy-k a `OverlayScene.Layer`-re kerülnek, a pár render node-jára kötődnek (`SetAnimation`), az eredeti (current) elemek elrejtve. A trackek a proxykat animálják (`Element => animation ?? original`).
  - **Commit:** a réteg törlése (`OverlayLayer.Clear()` — a `Commit`-ben és az `Initialize`-ben is, interrupció-biztonság), a target fa előléptetése. Az élő fa tiszta marad.
  - **Graceful fallback:** ha egy párhoz nem készül proxy, az eredeti elem marad a track célja (a P4 viselkedés) — egy pár szintjén.
  - A `OverlayBinder` leegyszerűsödik (nyilvántartás; a vizuális feladatokat a runtime/scene végzi).
- **Kimenet:** a Hero/Shared Element (elemek szabad mozgatása a rétegen) alapja; a demók vizuálisan változatlanok maradnak (a proxy a helyén jelenik meg).
- **Kiegészítés (2026-08-02 hotfix):** a proxy pozíciója **világkoordinátából** számítódik (`worldBound`), a layer root-hoz viszonyítva — a `resolvedStyle.left/top` az eredeti szülőhöz relatív, a proxy másik szülőbe (layer) kerül, ezért azokkal az elem "eltűnt" (rossz pozíció). A `CreateProxy` megkapta a `parent` paramétert; NaN esetén a resolvedStyle a fallback.
- **Kiegészítés #2 (2026-08-02, OPT-IN döntés):** a layer **kísérleti és alapból kikapcsolt** — a `FigmaFlowProfile.useAnimationLayer` (default: false). Ok: (1) a `PositionTrack` style-módja a capture-beli, eredeti-szülő-relatív `left/top`-ot írja a proxyra, ami a layer koordináta-rendszerében elugrást okoz; (2) a USS-alapú megjelenés (háttérképek, komplex stílusok) nem másolódik a proxyra → az elem "eltűnik". A v0.4-es viselkedés (trackek az eredeti elemeken) a default. A layer a **Hero/Shared Element** feladatnál kapja a végleges koordináta-kezelést (akkor kerül bekapcsolásra).
- **Kiegészítés #3 (2026-08-02, binder kombinált):** az `OverlayBinder` visszakapta a megjelenő elemek láthatóvá tételét (a v0.5/1-es leegyszerűsítés kivette — a layer pótolta volna, de az opt-in miatt ki van kapcsolva). Most **kombinált**: ha a párnak van proxy-ja (`HasAnimation` — layer aktív), nem nyúl hozzá; ha nincs (layer inaktív), láthatóvá teszi a rejtett konténerben, 0 opacitásról. Így a Bottom Sheet nyitás (appear) ismét felcsúszik, és a layer-rel is kompatibilis.
- **Státusz:** ✅ Accepted — implementálva + hotfix + OPT-IN + binder kombinált (2026-08-02)

## ADR-017 — Interrupció fix: átmenet-epoch (v0.5)

- **Dátum:** 2026-08-02
- **Kontextus:** A cipőcserélő demo tesztje közben: lejátszás közbeni `GoToState`-nél a `StopAllCoroutines` csak a StateMachine coroutine-jait állítja le — a **player a saját MonoBehaviour-ján fut tovább**, és a végén a `Finished` eseménye a **már felülírt `pendingState`-tel commitol** → a `currentState` és a vizuális állapot eltér → a következő átmenet "az aktuális elejére ugrik" (from ≈ to, mert a vizuális már a célállapotban van).
- **Döntés:** átmenet-generáció (epoch) a StateMachine-ben:
  - `transitionEpoch` nő minden `GoToState`-nél; `startedEpoch` rögzíti az aktuális átmenet indulásakor
  - az `OnTransitionFinished` **csak akkor commitol**, ha `startedEpoch == transitionEpoch` — a régi player Finished-je ignorálva (a pendingState-et sem bántja, az az új átmeneté)
  - a régi player a már eltávolított fán animál — ártalmatlan
  - **a freeze-elt `TransitionPlayer`-höz nem kellett nyúlni** (a védelem a StateMachine-ben van)
- **Megjegyzés (viselkedés):** a `Next()/Previous()` index-alapú, nem ciklikus — az utolsó állapotban a Next, az elsőben a Previous nem csinál semmit (guard). A wrap-around opcionális kényelmi funkció (profil-kapcsoló), ha kell.
- **Kiegészítés (2026-08-02, navigáció fix):** a `Next()/Previous()` a **függő átmenet céljából** számol (`pendingState >= 0 ? pendingState : currentState`), és a `GoToState` guardja a függő célra is vonatkozik (`targetState == pendingState` → return; nyugalomban `targetState == currentState` → return). Ok: a `currentState` csak a commitkor frissül — az animáció alatt a gép-állapot és a vizuális állapot eltér, a parancsok "elnyelődtek" (a felhasználó "az állapot végén találta magát", vagy "a 0-ásban prevet nyomva 1-0 animációt kapott"). Az animáció közbeni, a currentState-tel egyező parancs mostantól interrupcióként fut (az epoch kezeli a régi Finished-t).
- **Státusz:** ✅ Accepted — implementálva + navigáció fix (2026-08-02)

## ADR-017c — Visszafordulás (reverse) fix: élő-fa capture (v0.5)

- **Dátum:** 2026-08-02
- **Kontextus:** A 0→1 animáció közbeni Previous (a "from = pending" navigációval) a cél 0-ra mutat — de a `currentState` még 0, így az elemzés `captured[0]→captured[0]`-t kapott → üres átmenet → a visszafordulás animáció nélkül "pattant" (a felhasználó "az 0 állapot végét" látta).
- **Döntés:** a visszafordulás (Layout módban, `pendingState >= 0 && targetState == currentState`) az **élő, félúton lévő fát** használja kiindulási állapotként:
  - a `GoToState` NEM reloadol ilyenkor (az ADR-017b kivételével) — a mid-flight fa megmarad
  - a `PlayTransition` **capture-eli az élő fát** (`StateCapture.Capture(host.Current, ...)`) és arról animál vissza az eredeti állapotba
  - a `TransitionBuilder.Analyze` új `worldRectPosition` kapcsolót kapott: a translate-módú mozgás közben a style left/top változatlan, a pozíció-változás csak a **worldRect-ből** detektálható — így a visszaforduló átmenet megkapja a Position tracket (a freeze-elt PositionTrack translate-módja a mid→origin deltát animálja)
  - ha a capture üres (hiba), a `capturedStates[currentState]` a fallback
  - az overlay út változatlan (a `Prepare` úgyis új fát épít; a visszafordulás ott instant — a demók cooldown-ja ezt fedezi)
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-019 — Preview Slider (v0.5/3)

- **Dátum:** 2026-08-02
- **Kontextus:** A Motion Studio-ban az átmenet-beállítások vakon, asset-szerkesztésből történtek. A bemutató-készítéshez (termékvízió) előnézet kell: a transition **Play Mode nélkül** lejátszható szerkesztés közben.
- **Döntés:**
  - `Editor/TransitionPreview.cs` (új): a meglévő, bizonyított pipeline-t használja (Analyze → Build → Evaluate) — a Motion Engine érintetlen (freeze)
  - a from fa az élő alap; a to állapot capture ideiglenes betöltéssel; az építés a `ScheduleOnce`-szal (editor-only layout késleltetett — a resolvedStyle a frame-várakozás után érvényes, a runtime-ban bevált minta)
  - a lejátszást az `EditorWindow.Update` hajtja (`EditorApplication.timeSinceStartup` delta); a `Seek` a `transition.Evaluate(time)`-t hívja
  - a preview egy rejtett `HideAndDontSave` host GameObject UIDocument-jén fut — a jelenet UIDocument-je nem bántva
  - a Motion Studio "Preview (v0.5)" szekciója: Load Preview / Play / Stop / Time slider
- **Kimenet:** az átmenetek szerkesztés közben láthatók; ez a Timeline (v0.5/4) alapja.
- **Kiegészítés (2026-08-02):** scale-to-fit (a fix desktop az ablakba), végállapot (a lejátszás végén a to fa), slider-húzás (dragging-flag + Seek callback), Play a végállapotból (from fa + auto-play). **Limitáció:** a színváltozás nem animálódik (nincs ColorTrack, az ElementTrack freeze) — a végállapot a to fát mutatja.
- **Státusz:** ✅ Accepted — implementálva + kiegészítések (2026-08-02)

## ADR-032 — PuzzleKit: SlotPuzzle + CombinationLock (v0.7/1)

- **Dátum:** 2026-08-02
- **Kontextus:** A szabadulószoba irány (a handover §1b) első komponensei: a tárgyak tap-tap pakolása a helyes sorrendbe (SlotPuzzle), és a tárcsák/gyűrűk beállítása a helyes kombinációra (CombinationLock / cryptex). A kulcs-meglátás: a LOGIKA külön réteg (a PlayMaker mintájára), a Figma state-ek csak a vizuális visszajelzés (megoldva / rossz).
- **Döntés:** `Puzzle/` mappa (a StateMachine MELLETT — nem benne):
  - `SlotPuzzle.cs`: a tárgyak kiválasztása (kiemeléssel), pakolás a slotokra, a megoldás ellenőrzése (a solutionOrder szerint); kész → `GoToState(solvedState)` + `onSolved`; hiba → `onWrong` + (opcionálisan) visszaállítás; a slotból visszavétel (a játékos elveheti)
  - `CombinationLock.cs`: a tárcsák (dialNames) érintésre a következő értékre fordulnak (values vagy 0-9); a helyes kombináció (solution) → `GoToState(solvedState)` + `onSolved`; `autoCheck` (minden lépésnél) vagy Check gomb; az érték egy labelben (`_value` suffix)
  - a ClickEvent-eket figyelik (a TUIO-input a mapping nélküli érintéseket ClickEventté alakítja — a puzzle így a TUIO-val és az egérrel is működik)
  - a Motion Engine-hez nem nyúltunk (freeze)
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-031 — TUIO bővítések: visszajelzés, időablak, nyomvatartás, események (v0.6/7)

- **Dátum:** 2026-08-02
- **Kontextus:** A felhasználó visszajelzései: (1) szelektív "érzékeltem" visszajelzés (csak a kiválasztott elemek, nyomvatartáskor is megmaradva), (2) a kombinált triggerhez állítható időablak + folyamatos nyomvatartás (elengedésre megszűnő aktiváció), (3) a mapping/trigger ne csak GoToState-et tudjon, hanem az Inspectorban bármilyen esemény (UnityEvent, mint a UI Button onClick) ráhúzható legyen.
- **Döntés:**
  - **Visszajelzés:** `pressFeedbackElements` (lista — csak ezek az elemek) + `pressScale`; az elem megnő az első érintésre, és AMÍG rajta a kéz, megnőve marad (a `pressCounts` számolja a cursort); az utolsó elengedésekor visszaáll (`Scale.None()`)
  - **Időablak:** `TuioCombinedTrigger.timeWindow` (mp) — a két érintés kezdete közötti megengedett különbség (0 = nincs korlát)
  - **Nyomvatartás:** `holdMode` + `releaseState` a kombinált triggeren (amíg mindkét pont érintett, aktív; bármelyik elengedése → deaktiválás) ÉS a mappingen (1 gomb: amíg nyomva, aktív; elengedés → releaseState)
  - **Események:** `onActivate` / `onDeactivate` UnityEvent a mappingen és a triggeren — a GoToState mellett/vagy helyett; az Inspectorban bármilyen metódus ráhúzható
  - **Egér:** a bal gomb nyomva tartása is "cursor" (a hold és a visszajelzés egérrel is tesztelhető)
  - a gomb-lock kizárólag a TUIO cursorokra (az egér nem lockol — neki nincs remove-eseménye)
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-030 — Multi-touch (v0.6/6)

- **Dátum:** 2026-08-02
- **Kontextus:** A termékvízió (érintőfal, többjátékos, szabadulószoba) megköveteli a több egyidejű érintés kezelését. A kihívások: (1) a simogatás/drag (a gyerek végighúzza a kezét), (2) a lidar jel-vesztése (új id-t ad egy elveszett érintésnek), (3) a tenyér (egyszerre több cursor egy érintésnek).
- **Döntés:** a `FigmaFlowTuioInput` multi-touch bővítése (a Tuio11Cursor referenciák tárolásával — a pozíció pollozható, az update-esemény hiánya ellenére):
  - **move-enter aktiválás** (`triggerOnMoveEnter`): a cursor áthalad egy gombon → aktiválás (a simogatás/drag)
  - **drop-helyreállítás** (`dropRecoveryWindow` 0.3s + `dropRecoveryDistance` 50px): az elveszett + újra megjelenő cursor UGYANAZ (nem új aktiválás)
  - **gomb-lock** (`enableButtonLock`): egy gomb csak akkor aktiválódhat újra, ha az ÖSSZES cursor lejött róla (a tenyeres több-cursor nem kapcsolgatja a toggle-t)
  - **kombinált trigger** (`TuioCombinedTrigger { element1, element2, targetState }`): a két elem EGYSZERRE érintett → GoToState (két-személyes feladat)
  - **hover-jelzés** (`ShowHover`): az érintett elem rövid kiemelése (scale 1.08, 250ms) — a "érzékeltem az érintést" vizuális visszajelzés
  - a single-touch viselkedés (mapping, click) változatlan; az egér-szimuláció megmarad
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-029 — Editor-only StateCache (v0.6/5)

- **Dátum:** 2026-08-02
- **Kontextus:** A StateCache export (P3) Play Mode-ot igényelt (a runtime capture kész állapotából). A bemutató-készítéshez a cache tisztán editorban készüljön — Play Mode és scene-futtatás nélkül.
- **Döntés:** a `StateCacheExporter` bővítése egy **offscreen útvonallal** (`ExportOffscreen`):
  - a `states` tömb minden VTA-ját egy **rejtett UIDocument-re** tölti (1920×1080, `HideAndDontSave` + `PanelSettings`), a `schedule` + frame-várakozás mintával (a TransitionPreview-ben bevált — az editor layout késleltetett)
  - sorban capture-öli, majd a **meglévő `FigmaStateCacheAsset` formátumot** hozza létre (a runtime betöltése VÁLTOZATLAN)
  - a Motion Studio **"Export State Cache (Editor)"** gombja (a Play Mode-os mellett marad, mint opció)
  - a `FinishOffscreenExport` a Play Mode-os export közös mentési részét használja
  - **ismert korlát:** ha egy VTA scene-kontextustól függ (bindings), az offscreen nem azt adja — a Figma-exportok (statikus) esetén nem gond
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-028 — Dropdown + popup scale-in (v0.6/3)

- **Dátum:** 2026-08-02
- **Kontextus:** A v0.4-es overlay referencia-lista (Dropdown) és a popup "megnőve" megjelenés (Figma-ban gyakori) — az AppearMode bővítése.
- **Döntés:**
  - `AppearMode.Scale` (új enum érték): a megjelenő elem **kicsinyről a teljes méretére nő** (a középpontból) — a `TransitionBuilder.ApplyAppearMode` Scale ága: a szintetikus oldal a `CreateScaledCopy`-val a középpont körül 0.1×-re kicsinyítve + opacity 0; `Size | Opacity` property-k (a meglévő SizeTrack + OpacityTrack animálja — nincs új track, a Motion Engine érintetlen)
  - a Motion Studio "Appear" választója már tartalmazza (az enum automatikusan)
  - **Dropdown demo** (`Samples/DropdownDemo/`): a Popup mintájára, de az opciók a gomb ALATT (lenyíló), a DropdownMenu AppearMode = Scale; toggle, click-away, barrier, a kiválasztás a Selection labelbe
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-027 — Motion Presets (v0.6/2)

- **Dátum:** 2026-08-02
- **Kontextus:** A gyakori beállítás-kombinációk (ease/duration/appearMode) ismételt beállítása itemenként lassú. A bemutató-tervezőhöz újrahasznosítható "stílusok" kellenek.
- **Döntés:**
  - `Data/MotionPreset.cs` (új): `presetName` + `ease` + `duration` + `appearMode`
  - a `TransitionAsset` + `presets` lista (az assetben tárolva — minden pár-assetnek saját preset-készlete)
  - a Motion Studio "Motion Presets (v0.6)" szekciója: név/ease/duration/appear szerkesztő + "Mentés presetként"; a mentett presets listája + "Apply to All Items" (minden itemre) + "Preset törlése"
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-026 — TUIO érintőfal input (v0.6/1)

- **Dátum:** 2026-08-02
- **Kontextus:** A termékvízió (múzeumi kioszk, érintőfal + lidar) megköveteli a TUIO érintés-adatok kezelését. A projekt már rendelkezik a TuioNet/TuioUnity klienssel (TuioSessionBehaviour, UDP 3333, cursor események).
- **Döntés:** `Runtime/FigmaFlowTuioInput.cs` — az `IFigmaFlowInput` implementációja:
  - a TUIO cursor (normalizált 0..1, Y felfelé) → panel-pozíció (`flipY` kapcsoló)
  - `root.panel.Pick(pos)` → a VisualElement a pont alatt
  - **mapping:** `TuioStateMapping { elementName, targetState }` lista — az elem (vagy ősének) neve egyezik → `GoToState(targetState)`; az Inspectorban állítható, ez a "melyik érintés melyik state-re" egyszerű beállítása
  - mapping nélküli elem → szintetikus `ClickEvent` (a meglévő input kezelők, gombok így élnek)
  - minden érintés = `Activate()` → a PlayMaker idle-reset
  - **egér-szimuláció:** `simulateWithMouse` — a bal klikk ugyanazon a pipeline-on (editor play mode tesztelés, valódi érintőfal nélkül)
  - a runtime input-agnosztikus marad (a komponens az interfészen csatlakozik)
  - **API (a TuioNet.Tuio11 szerint, a Tuio11Visualizer példa alapján):** a cursor osztály `Tuio11Cursor` (a `TuioNet.Tuio11`-ben); a dispatcher a `TuioSessionBehaviour.TuioDispatcher` cast-jával `(Tuio11Dispatcher)`; az események `OnCursorAdd/OnCursorUpdate/OnCursorRemove` `(object sender, Tuio11Cursor)`; a `SessionId` `uint`, a `Position` `Vector2` (normalizált)
  - **HELYEZÉS:** a fájl az `Assets/FigmaRuntime-Tuio/` mappában (az asmdef-fán KÍVÜL → Assembly-CSharp) — mert a `FigmaRuntime.asmdef` nem láthatja a TuioUnity/TuioNet típusait, ha azok az Assembly-CSharp-ban élnek; a bridge így mindent elér, a FigmaRuntime core továbbra sem függ a TUIO-tól
  - **MAPPING-ÁLLAPOT-FELTÉTEL (2026-08-02):** a `TuioStateMapping` új `fromState` mezőt kapott (>= 0 esetén csak abban az állapotban aktív). Így ugyanaz a `Button_A` név (ami az animáció path-alapjául szolgál, nem változtatható) a 0. állapotban `0->1`-re, az 1. állapotban pedig egy másik mappinggel `1->0`-ra vihet — a "visszafelé" navigáció ugyanarról a gombról.
  - **Y-TENGELY:** az `autoFlipY` ELTÁVOLÍTVA (a felhasználó döntése — csak a `flipY` marad, amit a küldő ismeretében állítanak; a te küldő (spec-konform, Y lefelé) esetén `flipY = false`).
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-025 — Input absztrakció: IFigmaFlowInput (v0.5)

- **Dátum:** 2026-08-02
- **Kontextus:** A termékvízió (érintőfal, lidar) külső input-réteget igényel. A demó input-kezelők (Dialog/BottomSheet/Popup/Hero) külön-külön implementálták a robusztus mintát (deferred GoToState, cooldown), a PlayMaker pedig direkt ClickEvent-re figyelt.
- **Döntés:**
  - `Runtime/IFigmaFlowInput.cs` (új interfész): `Machine`, `Bind()`, `Unbind()`, `Activate()`, `LastActivationTime` — a runtime input-agnosztikus; a külső input-réteg (lidar/érintőfal) ezen csatlakozik
  - `Runtime/FigmaFlowInputBase.cs` (új absztrakt): a közös minta (deferred GoToState, cooldown, Activate/LastActivationTime) — a demók ebből származnak, csak a gomb-logikát írják
  - `FigmaFlowPlaymaker`: a kattintás-észlelés a direkt ClickEvent helyett az `IFigmaFlowInput` pollozása (`LastActivationTime`); a `Ping()` az inputnak is jelez
  - a demó input-kezelők áttérése az alapra egy következő lépés (a mintát a jelenlegi demók már használják — az átállás nem változtat a viselkedésükön)
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-024 — Per-Pair TransitionAsset (v0.5/7)

- **Dátum:** 2026-08-02
- **Kontextus:** A felhasználó páronként (state-páronként) ELTÉRŐ átmenet-stílust akar (más delay/duration/ease/appearMode a 0→1, 1→2, 0→2 stb. párra). A jelenlegi egyetlen `transitionAsset` minden párra ugyanazt a stílust adná.
- **Döntés:** per-pár asset támogatás:
  - `FigmaStateMachine.pairAssets[]` (opcionális) + `ResolveAsset(fromIndex, toIndex)`: a tömbben a `fromState`/`toState` nevek alapján keres; ha nincs pár-egyezés, a default `transitionAsset` a fallback
  - a `PlayTransition` a `ResolveAsset`-tel választ (a `hasAsset` is a pár-assetre vonatkozik)
  - a `TransitionPreview.LoadSequence` megkapta a `FigmaStateMachine`-t — a szekvencia MINDEN párra a pár assetjét választja (a 0→1 és az auto-advance párjai is)
  - a `FigmaFlowPreviewWindow.OpenSequence` + a Motion Studio: a gép átadása
  - **"Add Current Pair to StateMachine"** gomb: a jelenlegi From/To pár assetjét hozzáadja a gép `pairAssets` tömbjéhez (kényelem — nem kell kézzel húzogatni)
  - a meglévő demók (egy asset a `transitionAsset`-en, `pairAssets` üres) → változatlanul működnek (fallback)
  - a Motion Engine-hez nem nyúltunk (a `Build` ugyanaz — csak más assetet kap)
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-023 — RotationTrack: az elforgatás animálása (v0.5, F8 zárás)

- **Dátum:** 2026-08-02
- **Kontextus:** A Figma-import hiányosságai miatt nem minden projekt importálható teljesen — a PNG+UI Builder út járható (a felhasználó felfedezése). A UI Builder rotate értéke azonban nem animálódott: a `StateCapture` a rotation-t 0-ra rögzítette (F8 limitáció a v0.3.7 óta), az `AnimatedProperties`-ben nem volt Rotation flag, és nem volt RotationTrack.
- **Döntés (freeze-eszkaláció, projektvezetői jóváhagyással):**
  - `StateCapture`: a `resolvedStyle.rotate.angle` rögzítése (a 0 helyett) — nem freeze
  - `AnimatedProperties`: + `Rotation = 8` flag — nem freeze
  - `TransitionBuilder.GetChangedProperties`: rotation-különbség detektálás (tolerancia 0.5°) — nem freeze
  - `Tracks/RotationTrack.cs` (ÚJ): a `style.rotate` animálása az `IPropertyTrack` mintára (Evaluate/Reset/Complete) — új fájl, nem a freeze lista része
  - `ElementTrack.RebuildTracks`: egy Rotation ág (az egyetlen freeze-elt módosítás, a fejlécben dokumentálva) — a meglévő három track érintetlen
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-022 — PlayMaker: bemutató-flow automatizálás (v0.5/6)

- **Dátum:** 2026-08-02
- **Kontextus:** A termékvízió (múzeumi kioszk, slideshow) megköveteli a bemutató automatikus vezénylését: időzített váltás, ciklikus szekvencia, inaktivitás utáni visszatérés a főmenüre.
- **Döntés:** `Runtime/FigmaFlowPlaymaker.cs` (a StateMachine mellé):
  - `autoAdvanceSeconds` (0 = ki) — állapotonkénti időzített váltás; `loop` — a szekvencia ciklikus; `idleReturnSeconds` + `idleReturnState` (0 = ki) — inaktivitás utáni visszatérés
  - az inaktivitás-észlelés **automatikus**: a root ClickEvent (bármilyen kattintás) reseteli az idle-időzítőt — a demó input-kezelőket NEM kell módosítani
  - `Ping()` — külső input (érintőfal, lidar) aktivitás-jelzése (a projekt filozófiája: az input absztrakció külső réteg)
  - a navigáció a `GoToState`-on megy — a v0.5-ös interrupció/navigáció fixekkel kompatibilis
  - a Motion Engine-hez nem nyúltunk (freeze)
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-021 — Timeline (v0.5/5)

- **Dátum:** 2026-08-02
- **Kontextus:** Az asset itemjeinek (delay/duration/ease) szerkesztése eddig csak a `TransitionAssetEditor` (IMGUI) listájában volt lehetséges. A bemutató-készítéshez vizuális idővonal kell, a Preview-val szinkronban.
- **Döntés:** `Editor/FigmaFlowTimelineWindow.cs` (UI Toolkit ablak — a Motion Studio IMGUI-s, ezért külön ablak, mint a Preview):
  - minden item egy sor: path + sáv (delay→delay+duration) + ease legördülő
  - a sáv húzása: eleje (delay), vége (duration), közepe (eltolás) — a kattintás helyétől függően; a módosítás az assetbe mentődik (SetDirty)
  - a playhead (piros vonal) a `preview.Time`-ból rajzolódik — a Preview-val szinkron
  - a sávra kattintás → `preview.Seek(item.delay)`
  - az item-sáv párosítás `Dictionary`-vel (a `TransitionItem`-et nem bővítettük)
  - a Motion Studio "Open Timeline (v0.5)" gombja nyitja (preview + asset)
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-020 — States Sequence Preview (v0.5/4)

- **Dátum:** 2026-08-02
- **Kontextus:** A from/to alapú preview nem tudja a 3+ állapotú demókat (pl. tab menü) végigkísérni. A felhasználó felvetése: a Motion Studio a `states` tömböt használja.
- **Döntés:** szekvencia-mód a `TransitionPreview`-ben:
  - `LoadSequence(states, asset, profile)` — a `states[0]→states[1]` párral indul; a `sequenceMode` flag
  - **auto-advance:** a pár lejátszása után (`ShowFinalState`) a következő pár betöltődik (`sequenceIndex++`); az utolsó pár után a végállapot
  - a `sequenceMode`-ot a `Load` törli — az auto-advance visszaállítja (a két mód független)
  - **Play a végállapotból** → a teljes szekvencia újraindul
  - a gép egyetlen `transitionAsset`-jét használja minden párra (path-alapú — a per-pár assetek a Timeline-lal jönnek)
  - a `FigmaFlowPreviewWindow.OpenSequence` + a Motion Studio "States Sequence (v0.5)" szekciója (`FigmaStateMachine` referencia + gomb)
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-017b — Interrupció fix #2: tiszta fa Layout módban

- **Dátum:** 2026-08-02
- **Kontextus:** A cipőcserélő scene "több oda-vissza Next/Prev után az 1 animáció nélkül jelenik meg" tünete. Ok: a **megszakított átmenet stílusmaradványai** (a `PositionTrack` translate/left/top, az `OpacityTrack` opacity) a live fán maradnak — a commit nem fut le, a player-routine leáll. Több megszakítás után a fa "szennyezett": az elemek már a végpozíción vannak → a következő átmenet láthatatlan (a commit "animáció nélkül" hozza a célállapotot).
- **Döntés:** a `GoToState`-ben, ha van függő átmenet (`pendingState >= 0`), Layout módban (`!useOverlay`), az új átmenet indítása ELŐTT a **commit-olt állapot újra-betöltése** (`host.Load(states[currentState])`) — az új átmenet tiszta fán indul. Az overlay úton nincs szükség erre (a `Prepare` úgyis új fát épít).
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-018 — Hero / Shared Element (v0.5/2)

- **Dátum:** 2026-08-02
- **Kontextus:** A Hero/Shared Element (egy mindkét állapotban létező elem átrepül és átméreteződik — pl. kártya → részletnézet) a layer legnagyobb hozadéka. Ehhez a layer **végleges koordináta-kezelése** kell (a korábbi "rossz helyen jelenik meg" gyökere: a trackek style-pozíciót írtak a proxyra, ami másik koordináta-rendszerben van).
- **Döntés:** a layer-aktív trackek a **translate-módot** használják:
  - **`ToLayerData`** (az `OverlayTransitionBuilder`-ben): a from/to `worldRect` a **layer root-hoz igazítva**, a `left/top` nullázva → a freeze-elt `PositionTrack` a `useStyle=false` (translate) ágban fut — a `worldRect` delta a layer-koordinátákban is helyes
  - a **proxy a from pozícióra** kerül (`live.style.left/top = from.worldRect`) — a translate delta arra épül
  - **`preferCapture`** paraméter az `ApplyAppearMode`-on: proxykon a slide a capture-adatokat használja (a proxy `resolvedStyle`-ja layer-koordináta, a capture képernyő-koordináta — keverve rossz)
  - a `OverlayTransitionBuilder.Build` megkapta a `layerRoot` paramétert (az `OverlayRuntime.BuildTransition` a `scene.Layer.Root`-ot adja); a proxy-detektálás a páron (`HasAnimation`)
  - az `OverlayElementFactory` **gyermek-másolást** kapott (rekurzív, maxDepth) + háttérkép/szegély/stílus másolást — a Hero kártya valósághű a rétegen
  - a layer **továbbra is opt-in** (`useAnimationLayer`); a Hero demo az első, ahol BE van kapcsolva; a többi demo (Dialog/BottomSheet/Popup) a layer nélkül marad
- **Kimenet:** a Hero a meglévő Motion Engine-nel fut (a freeze-elt trackek változatlanok — csak az adatok lettek layer-koordinátásra konvertálva a Builderben).
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

- **Dátum:** 2026-08-02
- **Kontextus:** A P3 export egyetlen fix útvonalra írt (`Assets/FigmaFlow/Cache/StateCache_{machine.gameObject.name}.asset`). Több scene-es projektekben az azonos nevű GameObject-ök (pl. "FigmaFlow") cache-ei **felülírták egymást**.
- **Döntés:**
  - Export útvonal scene-enként: `Assets/FigmaFlow/Cache/{scene}/StateCache_{machineName}.asset` (a scene név sanitizálva, invalid karakterek → `_`)
  - Az asset rögzíti a `sceneName`-t; a runtime betöltéskor **figyelmeztet**, ha az aktív scene nem egyezik (nem hiba — csak jelzés, mert a mező beállítása scene-enként történik)
  - A régi, mappa nélküli cache assetek nem migrálódnak — törlendők, az export újra lefut scene-enként
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)

## ADR-011 — ITransitionPlayer.Finished interfészre emelése

- **Dátum:** 2026-08-02
- **Kontextus:** A StateMachine a player-típus ellenőrzéssel (`is TransitionPlayer`) iratkozott fel a Finished eseményre (F1 maradvány). A kickoff Q4 döntés: az interfész bővítése elfogadva v0.4-ben.
- **Döntés:** `ITransitionPlayer` (Core) kapja: `event Action Finished;`. A `TransitionPlayer` és `OverlayTransitionPlayer` meglévő publikus eseménye **implicit implementálja** — a freeze-elt `TransitionPlayer`-höz nem kellett nyúlni. A StateMachine `SubscribePlayer/UnsubscribePlayer` egyszerűsödik (`player.Finished ±= OnTransitionFinished`), nincs típus-ellenőrzés.
- **Státusz:** ✅ Accepted — implementálva (2026-08-02)
