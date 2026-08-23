using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime
{
    [RequireComponent(typeof(FigmaDocumentHost))]
    public class FigmaStateMachine : MonoBehaviour
    {
        [Header("States")]
        public VisualTreeAsset[] states;

        [Header("Profile")]
        public FigmaFlowProfile profile;

        [Header("Motion")]
        public TransitionAsset transitionAsset;

        [Header("Per-Pair Assets (v0.5, ADR-024)")]
        [Tooltip("Páronkénti transition asset-ek — a pár (from/to) neve szerint. " +
                 "Üres tömb esetén a 'transitionAsset' a fallback.")]
        public TransitionAsset[] pairAssets;

        [Header("State Cache (v0.4)")]
        public FigmaStateCacheAsset stateCache;

        private FigmaDocumentHost host;
        private ITransitionPlayer player;
        private IOverlayDriver overlayDriver;
        private FigmaStateAsset[] capturedStates;

        private int currentState;
        private int pendingState = -1;

        /// <summary>
        /// v0.5 (interrupció fix): átmenet-generáció. Minden GoToState
        /// növeli — a lejátszás közbeni újabb GoToState-nél a régi player
        /// Finished eseménye nem commitolhat (a pendingState-et az új
        /// átmenet már felülírta). A StopAllCoroutines nem állítja le a
        /// playert (az a saját MonoBehaviour-ján fut) — ez a védelem.
        /// </summary>
        private int transitionEpoch;

        /// <summary>
        /// Az aktuálisan indított átmenet epoch-ja.
        /// </summary>
        private int startedEpoch;

        /// <summary>
        /// Overlay mód aktív-e (ADR-012): profil Overlay + van driver.
        /// </summary>
        private bool useOverlay;

        /// <summary>
        /// Az aktuális átmenet overlay commitot igényel-e a lejátszás
        /// végén (Layout fallback esetén hamis marad).
        /// </summary>
        private bool overlayCommit;

        void Awake()
        {
            host = GetComponent<FigmaDocumentHost>();

            overlayDriver = GetComponent<IOverlayDriver>();

            if (profile == null)
            {
                Debug.LogWarning(
                    "[FigmaFlow] profile nincs beállítva — Layout mód alapértelmezéssel.");

                SetPlayer(GetComponent<TransitionPlayer>());
                useOverlay = false;
            }
            else if (profile.animationMode == AnimationMode.Overlay)
            {
                if (overlayDriver != null && overlayDriver.IsAvailable)
                {
                    // v0.4 / P4 (ADR-012): az overlay pipeline a driveren
                    // keresztül — a Stable nem hivatkozik az Experimentalre.
                    SetPlayer(overlayDriver.Player);
                    useOverlay = true;

                    Debug.Log(
                        "[FigmaFlow] Overlay mód aktív (IOverlayDriver).");
                }
                else
                {
                    Debug.LogWarning(
                        "[FigmaFlow] Overlay mód, de nincs IOverlayDriver a jeleneten — " +
                        "Layout fallback.");

                    SetPlayer(GetComponent<TransitionPlayer>());
                    useOverlay = false;
                }
            }
            else
            {
                SetPlayer(GetComponent<TransitionPlayer>());
                useOverlay = false;
            }
        }

        void OnDestroy()
        {
            if (player != null)
                player.Finished -= OnTransitionFinished;
        }

        /// <summary>
        /// Player csere biztonságosan: leiratkozás a régiről,
        /// feliratkozás az újra.
        /// </summary>
        void SetPlayer(ITransitionPlayer newPlayer)
        {
            if (player != null && player != newPlayer)
                player.Finished -= OnTransitionFinished;

            player = newPlayer;

            if (player != null)
                player.Finished += OnTransitionFinished;
        }

        IEnumerator Start()
        {
            if (states == null || states.Length == 0)
            {
                Debug.LogWarning(
                    "[FigmaFlow] Nincs state definiálva (states üres) — a runtime nem indul el.");

                yield break;
            }

            // v0.4 / P3 (ADR-009): editor-oldali state cache —
            // nincs runtime capture ciklus, nincs felvillanás,
            // a HideRoot workaround nem kell.
            if (TryLoadStateCache())
            {
                ShowInitialState();

                yield break;
            }

            // Fallback: runtime capture (a v0.3.7 workaround).
            host.HideRoot();

            capturedStates = new FigmaStateAsset[states.Length];

            for (int i = 0; i < states.Length; i++)
            {
                TemplateContainer live = host.Load(states[i]);

#if UNITY_EDITOR
                Debug.Log($"[FigmaFlow] Load {i}");
#endif

                yield return null;

#if UNITY_EDITOR
                Debug.Log($"[FigmaFlow] Capture {i}");
#endif

                capturedStates[i] =
                    StateCapture.Capture(
                        live,
                        states[i].name);
            }

            ShowInitialState();
        }

        /// <summary>
        /// Az első állapot megjelenítése.
        /// Overlay módban a scene-en keresztül (nincs host.Load
        /// duplikáció az overlayRoot mellett); egyébként a host útvonal.
        /// </summary>
        void ShowInitialState()
        {
            if (useOverlay && overlayDriver != null)
            {
                if (overlayDriver.Prepare(states[0], states[0], null))
                {
                    currentState = 0;

                    host.ShowRoot();

                    return;
                }

                Debug.LogWarning(
                    "[FigmaFlow] Overlay inicializálás sikertelen — Layout fallback.");

                SetPlayer(GetComponent<TransitionPlayer>());
                useOverlay = false;
            }

            host.Load(states[0]);

            currentState = 0;

            host.ShowRoot();
        }

        /// <summary>
        /// v0.4 / P3 (ADR-009): az editor-oldali StateCache betöltése.
        /// Ha a cache érvényes (darabszám egyezik), a capturedStates-t
        /// abból építjük — runtime capture és HideRoot nélkül.
        /// </summary>
        bool TryLoadStateCache()
        {
            if (stateCache == null ||
                stateCache.states == null ||
                stateCache.states.Count == 0)
            {
                return false;
            }

            if (stateCache.states.Count != states.Length)
            {
                Debug.LogWarning(
                    $"[FigmaFlow] A StateCache ({stateCache.states.Count} state) " +
                    $"nem egyezik a states tömb méretével ({states.Length}) — " +
                    "runtime capture fallback.");

                return false;
            }

            capturedStates = new FigmaStateAsset[states.Length];

            for (int i = 0; i < states.Length; i++)
            {
                CachedState cached = stateCache.states[i];

                FigmaStateAsset asset =
                    ScriptableObject.CreateInstance<FigmaStateAsset>();

                asset.stateName = cached.stateName;
                asset.elements = cached.elements;

                capturedStates[i] = asset;
            }

            // ADR-010: több scene esetén ellenőrizzük, hogy a cache
            // ahhoz a scene-hez készült-e, ahol éppen futunk.
            if (!string.IsNullOrEmpty(stateCache.sceneName))
            {
                string currentScene =
                    UnityEngine.SceneManagement.SceneManager
                        .GetActiveScene().name;

                if (stateCache.sceneName != currentScene)
                {
                    Debug.LogWarning(
                        $"[FigmaFlow] A StateCache a(z) '{stateCache.sceneName}' " +
                        $"scene-hez készült, de az aktív scene '{currentScene}' — " +
                        "ellenőrizd, hogy a megfelelő asset van beállítva.");
                }
            }

            Debug.Log(
                $"[FigmaFlow] StateCache betöltve: {capturedStates.Length} state " +
                "(nincs runtime capture — nincs felvillanás).");

            return true;
        }

        [ContextMenu("Next")]
        public void Next()
        {
            // v0.5 (navigáció fix): ha van függő átmenet, abból számolunk —
            // a currentState csak a commitkor frissül, a felhasználó
            // viszont a vizuális haladás szerint navigál.
            int from = pendingState >= 0 ? pendingState : currentState;

            GoToState(from + 1);
        }

        [ContextMenu("Previous")]
        public void Previous()
        {
            int from = pendingState >= 0 ? pendingState : currentState;

            GoToState(from - 1);
        }

        [ContextMenu("Go To State 0")]
        public void GoTo0()
        {
            GoToState(0);
        }

        [ContextMenu("Go To State 1")]
        public void GoTo1()
        {
            GoToState(1);
        }

        [ContextMenu("Go To State 2")]
        public void GoTo2()
        {
            GoToState(2);
        }

        /// <summary>
        /// v0.4 / P3: az Editor StateCacheExporter-nek adja át a
        /// legyártott state-eket (Play Mode, export idején).
        /// </summary>
        public FigmaStateAsset[] GetCapturedStates()
        {
            return capturedStates;
        }

        /// <summary>
        /// v0.5/7 (ADR-024): a párhoz tartozó TransitionAsset kiválasztása.
        /// A pairAssets tömbben a fromState/toState nevek alapján keres;
        /// ha nincs pár-egyezés, a default transitionAsset a fallback.
        /// </summary>
        public TransitionAsset ResolveAsset(
            int fromIndex,
            int toIndex)
        {
            if (pairAssets != null)
            {
                string fromName =
                    fromIndex >= 0 && fromIndex < states.Length
                        ? states[fromIndex].name
                        : null;

                string toName =
                    toIndex >= 0 && toIndex < states.Length
                        ? states[toIndex].name
                        : null;

                foreach (TransitionAsset pair in pairAssets)
                {
                    if (pair == null)
                        continue;

                    if (pair.fromState == fromName &&
                        pair.toState == toName)
                    {
                        return pair;
                    }
                }
            }

            return transitionAsset;
        }

        /// <summary>
        /// v0.4 / P4-2: az aktuális állapot indexe — a demó
        /// input-kezelő állapot-guardjaihoz (csak a valóban
        /// megfelelő állapotban reagáljon a kattintásokra).
        /// </summary>
        public int GetCurrentStateIndex()
        {
            return currentState;
        }

        public void GoTo(string stateName)
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].name == stateName)
                {
                    GoToState(i);
                    return;
                }
            }

            UnityEngine.Debug.LogWarning($"[FigmaFlow] State not found: {stateName}");
        }

        public void GoToState(int targetState)
        {
            if (capturedStates == null)
            {
                Debug.LogWarning(
                    "[FigmaFlow] A state capture még nem készült el — GoToState figyelmen kívül hagyva.");

                return;
            }

            if (targetState < 0)
                return;

            if (targetState >= states.Length)
                return;

            // v0.5 (navigáció fix): ha már oda tartunk (függő átmenet
            // célja) → nincs teendő. Nyugalmi állapotban: ugyanaz az
            // állapot → nincs teendő. Az animáció közbeni, a currentState-
            // tel egyező parancs NEM ignored (a vizuális már halad) —
            // interrupcióként fut (az epoch kezeli a régi Finished-t).
            if (pendingState >= 0 && targetState == pendingState)
                return;

            if (pendingState < 0 && targetState == currentState)
                return;

            StopAllCoroutines();

            // Interrupció fix #2 (v0.5, ADR-017): a megszakított átmenet
            // stílusmaradványai (translate/left/top/opacity) a live fán
            // maradnak. Layout módban a commit-olt állapot újra-betöltésével
            // tiszta fát kap az új átmenet — különben a fa "szennyezett"
            // lenne, és egy későbbi átmenet animáció nélkül (azonnal a
            // célállapotban) jelenne meg. (Az overlay úton a Prepare úgyis
            // új fát épít — ott nincs szükség erre.)
            //
            // KIVÉTEL (v0.5, ADR-017c): a visszafordulás (target == currentState,
            // pl. 0→1 alatt Previous) — itt NEM reloadolunk, hanem az élő,
            // félúton lévő fáról animálunk vissza (a PlayTransition capture-eli).
            if (pendingState >= 0 &&
                targetState != currentState &&
                !useOverlay &&
                host != null)
            {
                host.Load(states[currentState]);
            }

            // Interrupció fix (v0.5): az új átmenet generációt kap.
            transitionEpoch++;

            StartCoroutine(
                PlayTransition(targetState));
        }

        IEnumerator PlayTransition(int targetState)
        {
            // Az átmenet epoch-jának rögzítése (a Finished csak ezzel
            // az epoch-gal commitolhat — interrupció fix, v0.5).
            startedEpoch = transitionEpoch;

            // v0.4 / P5 (workflow fix): az üres transitionAsset NEM blokkol.
            // A Next-nek mindig futnia kell — az elemzés (CurrentAnalysis)
            // a Motion Studio Generate-jéhez kell, és elemzés nélkül nem
            // lehet assetet generálni (tojás-csirke). Üres asset esetén
            // az átmenet elemek nélkül fut (közvetlen commit) + warning.
            //
            // v0.5/7 (ADR-024): a pár asset kiválasztása (per-pair).
            TransitionAsset pairAsset =
                ResolveAsset(
                    currentState,
                    targetState);

            bool hasAsset = pairAsset != null;

            // v0.5 (ADR-017c): visszafordulás — az eredeti állapotba
            // fordulunk egy függő átmenet közben (pl. 0→1 alatt Previous).
            // Ilyenkor az ÉLŐ, félúton lévő fa a kiindulási állapot
            // (a trackek épp ráírták a köztes stílusokat) — arról kell
            // visszaanimálni, nem az üres 0→0-t játszani.
            bool reverseToOrigin =
                !useOverlay &&
                pendingState >= 0 &&
                targetState == currentState;

            pendingState = targetState;

            overlayCommit = false;

            float posTol = profile != null ? profile.positionTolerance : 1f;
            float sizeTol = profile != null ? profile.sizeTolerance : 1f;
            float opTol = profile != null ? profile.opacityTolerance : 0.01f;

            // Visszafordulásnál az élő (félúton lévő) fa a from — a
            // worldRect-eltérések detektálásával (a translate-módú mozgás
            // a style-ből nem látszik, csak a worldRect-ből).
            FigmaStateAsset fromState = capturedStates[currentState];

            if (reverseToOrigin && host != null && host.Current != null)
            {
                FigmaStateAsset live =
                    StateCapture.Capture(
                        host.Current,
                        states[currentState].name + "_live");

                if (live != null && live.elements.Count > 0)
                    fromState = live;
            }

            TransitionAnalysis analysis =
                TransitionBuilder.Analyze(
                    fromState,
                    capturedStates[targetState],
                    posTol,
                    sizeTol,
                    opTol,
                    worldRectPosition: reverseToOrigin);

            FigmaFlowRuntime.SetAnalysis(
                analysis);

            // v0.4 / P4 (ADR-012): Overlay út — a driveren keresztül.
            if (useOverlay && overlayDriver != null)
            {
                bool prepared =
                    overlayDriver.Prepare(
                        states[currentState],
                        states[targetState],
                        analysis);

                if (prepared)
                {
                    // P5 hotfix (ADR-015): egy frame várakozás a layout
                    // lefutásához — a frissen létrehozott current/target
                    // fák resolvedStyle értékei csak ezután érvényesek
                    // (a slide élő-geometriája ezt használja).
                    yield return null;

                    // v0.5 / OverlayLayer (ADR-016): a proxy-k a layer-en
                    // (a layout-várakozás után — a resolvedStyle érvényes).
                    // OPT-IN: a profil useAnimationLayer kapcsolója (alapból
                    // ki) — a v0.4-es viselkedés (trackek az eredeti elemeken)
                    // a default. A layer a Hero/Shared Element feladatnál
                    // kapja a végleges koordináta-kezelést.
                    if (profile != null && profile.useAnimationLayer)
                    {
                        overlayDriver.PrepareAnimationLayer(analysis);
                    }

                    if (hasAsset)
                    {
                        Transition transition =
                            overlayDriver.BuildTransition(
                                analysis,
                                pairAsset);

                        if (transition != null && transition.Count > 0)
                        {
                            LogTransition(
                                analysis,
                                transition);

                            overlayCommit = true;

                            player.PlayForward(
                                transition,
                                transition.Duration);

                            yield break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[FigmaFlow] transitionAsset nincs beállítva — " +
                            "az átmenet elemek nélkül fut (csak elemzéshez).");
                    }

                    // Üres / nincs asset → közvetlen commit az overlay úton.
                    overlayDriver.Commit(
                        states[targetState]);

                    currentState = targetState;
                    pendingState = -1;

                    yield break;
                }

                Debug.LogWarning(
                    "[FigmaFlow] Overlay prepare sikertelen — Layout fallback.");

                // A host veszi át a megjelenítést a layout úthoz.
                host.Load(states[currentState]);
            }

            // Layout út (változatlan).
            if (!hasAsset)
            {
                Debug.LogWarning(
                    "[FigmaFlow] transitionAsset nincs beállítva — " +
                    "az átmenet elemek nélkül fut (csak elemzéshez).");

                host.CommitState(
                    states[pendingState]);

                currentState = pendingState;
                pendingState = -1;

                yield break;
            }

            Transition layoutTransition =
                TransitionBuilder.Build(
                    analysis,
                    pairAsset,
                    host.Current);

            if (layoutTransition == null || layoutTransition.Count == 0)
            {
                Debug.LogWarning(
                    "[FigmaFlow] Az átmenet üres (0 track) — a cél állapot közvetlenül commitolva.");

                host.CommitState(
                    states[pendingState]);

                currentState = pendingState;
                pendingState = -1;

                yield break;
            }

            LogTransition(
                analysis,
                layoutTransition);

            player.PlayForward(
                layoutTransition,
                layoutTransition.Duration);
        }

        void OnTransitionFinished()
        {
            if (pendingState < 0)
                return;

            // Interrupció fix (v0.5): ha közben új átmenet indult
            // (az epoch nőtt), ez a Finished egy RÉGI playeré — nem
            // commitolunk, a pendingState-et sem bántjuk (az új
            // átmeneté). A régi player a már eltávolított fán animál —
            // ártalmatlan.
            if (startedEpoch != transitionEpoch)
                return;

            // v0.4 / P4: overlay commit — a rétegek összeolvasztása.
            if (overlayCommit && overlayDriver != null)
            {
                overlayDriver.Commit(
                    states[pendingState]);
            }
            else
            {
                host.CommitState(
                    states[pendingState]);
            }

            currentState = pendingState;
            pendingState = -1;
            overlayCommit = false;
        }

        private void LogTransition(
            TransitionAnalysis analysis,
            Transition transition)
        {
            int candidates =
                analysis?.Candidates?.Count ?? 0;

            int tracks =
                transition?.Count ?? 0;

            UnityEngine.Debug.Log(
$@"==========================
[FigmaFlow] Transition
==========================
From        : {states[currentState].name}
To          : {states[pendingState].name}

Candidates  : {candidates}
Tracks      : {tracks}

Duration    : {transition?.Duration:F2}
Player      : {player?.GetType().Name}
==========================");
        }
    }
}
