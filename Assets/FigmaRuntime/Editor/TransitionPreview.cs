#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Editor
{
    /// <summary>
    /// v0.5/3 (ADR-019): a TransitionAsset előnézete.
    ///
    /// A Preview a meglévő, bizonyított pipeline-t használja Play Mode
    /// nélkül: Analyze → Build → Evaluate.
    ///
    /// JAVÍTÁSOK (2026-08-02):
    /// - Scale-to-fit: a fix méretű desktop arányosan az ablakba
    ///   kicsinyítve (a trackek a layout-koordinátákban futnak, a
    ///   skálázás vizuális — a translate is arányosan skálázódik)
    /// - Végállapot: a lejátszás végén a TO fa betöltése (a háttér/
    ///   szín is a végleges; a szín-ugrás az animáció alatt limitáció
    ///   — nincs ColorTrack, az ElementTrack freeze)
    /// - Play a végállapotból: újra a from fa + újraépítés + auto-play
    ///
    /// NEM módosítja a Motion Engine-t (freeze tartva).
    /// </summary>
    public class TransitionPreview
    {
        private VisualElement root;

        private TemplateContainer currentRoot;

        private VisualElement liveRoot;

        private Transition transition;

        private bool isPlaying;

        private float playTime;

        private float lastFrameTime;

        private int loadToken;

        // A végállapot-kezeléshez (utolsó Load bemenetei).
        private VisualTreeAsset lastFromTree;
        private VisualTreeAsset lastToTree;
        private TransitionAsset lastAsset;
        private FigmaFlowProfile lastProfile;

        private bool atFinalState;

        private bool autoPlayAfterLoad;

        /// <summary>
        /// Végállapotból érkező seek: a from fa + transition újraépítése
        /// után alkalmazandó idő (a Load aszinkron — ezért tároljuk).
        /// </summary>
        private float pendingSeek = -1f;

        // v0.5/4 (szekvencia-preview): a states-alapú 0->1->2->... mód.
        private bool sequenceMode;

        private VisualTreeAsset[] sequenceStates;

        private TransitionAsset sequenceAsset;

        private FigmaFlowProfile sequenceProfile;

        private FigmaStateMachine sequenceMachine;

        private int sequenceIndex;

        /// <summary>
        /// A desktop TERMÉSZETES (skálázatlan) mérete — az első érvényes
        /// méréskor rögzítve. A worldBound a scale-tel együtt változik,
        /// ezért minden frame-ben a skálázott értékből számolni
        /// OSZCILLÁCIÓT okozna (a skála visszaugrana 1-re).
        /// </summary>
        private Vector2 naturalSize = Vector2.zero;

        /// <summary>
        /// A tartalom (Desktop) eltolása a currentRoot-ban (pl. felső sáv)
        /// — a kézi középre igazításhoz.
        /// </summary>
        private Vector2 contentOffset = Vector2.zero;

        // ------------------------------------------------------------

        public bool IsReady => transition != null;

        public bool IsPlaying => isPlaying;

        public bool AtFinalState => atFinalState;

        public int TrackCount =>
            transition != null ? transition.Count : 0;

        public float Duration =>
            transition != null ? transition.Duration : 0f;

        public float Time => playTime;

        // ------------------------------------------------------------

        public void Initialize(VisualElement rootElement)
        {
            root = rootElement;
        }

        public void Load(
            VisualTreeAsset fromTree,
            VisualTreeAsset toTree,
            TransitionAsset asset,
            FigmaFlowProfile profile)
        {
            if (root == null)
                return;

            loadToken++;

            int token = loadToken;

            atFinalState = false;

            // A Load az EGYES (from/to) mód — a LoadSequence a hívás
            // után visszaállítja a sequenceMode-ot.
            sequenceMode = false;

            naturalSize = Vector2.zero;

            // FONTOS (2026-08-02): a transition nullázása a Load-nál —
            // a fit a "transition != null" guard miatt NEM futhat a
            // Build előtt (a második Load-nál a régi transition még
            // élne, és a fit skálázná az új fát a capture előtt →
            // a capture skálázott értékeket mérne → elcsúszott animáció).
            transition = null;

            isPlaying = false;

            lastFromTree = fromTree;
            lastToTree = toTree;
            lastAsset = asset;
            lastProfile = profile;

            root.Clear();

            if (fromTree == null || toTree == null || asset == null)
            {
                transition = null;

                return;
            }

            // A from fa az élő alap — EXPLICIT 100% méret (a Figma export
            // gyökere flex-grow-os; explicit szülő-méret nélkül a Desktop
            // 0 magasságú lenne a preview-ban).
            currentRoot = fromTree.Instantiate();

            currentRoot.style.width = new Length(100, LengthUnit.Percent);
            currentRoot.style.height = new Length(100, LengthUnit.Percent);

            root.Add(currentRoot);

            liveRoot = currentRoot;

            // Layout lefutása (a resolvedStyle csak utána érvényes).
            root.schedule.Execute(() =>
            {
                if (token != loadToken)
                    return;

                Build(fromTree, toTree, asset, profile, token);
            }).StartingIn(50);
        }

        /// <summary>
        /// v0.5/4: a states-alapú szekvencia betöltése (0->1->2->...).
        /// A párokat a gép transitionAsset-jével játssza (path-alapú —
        /// minden párra működik). A lejátszás végén automatikusan a
        /// következő párra lép (ShowFinalState / auto-advance).
        /// </summary>
        public void LoadSequence(
            VisualTreeAsset[] states,
            TransitionAsset asset,
            FigmaFlowProfile profile,
            FigmaStateMachine machine = null)
        {
            sequenceStates = states;
            sequenceAsset = asset;
            sequenceProfile = profile;
            sequenceMachine = machine;
            sequenceIndex = 0;

            if (states == null || states.Length < 2)
            {
                Debug.LogWarning(
                    $"[FigmaFlow] Szekvencia: legalább 2 állapot kell " +
                    $"(kapott: {(states != null ? states.Length : 0)}).");

                return;
            }

            // v0.5/7 (ADR-024): a pár asset kiválasztása a gépből —
            // ha a gépnek van pairAssets-e, a 0->1 párhoz a saját asset.
            TransitionAsset firstAsset =
                machine != null
                    ? machine.ResolveAsset(0, 1)
                    : asset;

            Load(
                states[0],
                states[1],
                firstAsset != null ? firstAsset : asset,
                profile);

            // A Load törölte — visszaállítjuk a szekvencia-módot.
            sequenceMode = true;

            Debug.Log(
                $"[FigmaFlow] Szekvencia betöltve: {states.Length} állapot " +
                $"({states.Length - 1} átmenet).");
        }

        /// <summary>
        /// v0.5/4: szekvencia-módban van-e (a window info-jához).
        /// </summary>
        public bool SequenceMode => sequenceMode;

        /// <summary>
        /// v0.5/4: az aktuális pár info (pl. "Pár 1/2").
        /// </summary>
        public string SequenceInfo
        {
            get
            {
                if (!sequenceMode || sequenceStates == null)
                    return "";

                int pairs = Mathf.Max(1, sequenceStates.Length - 1);

                return $" · Pár {sequenceIndex + 1}/{pairs}";
            }
        }

        void Build(
            VisualTreeAsset fromTree,
            VisualTreeAsset toTree,
            TransitionAsset asset,
            FigmaFlowProfile profile,
            int token)
        {
            // A from állapot capture (a live fa a from — a layout lefutott).
            FigmaStateAsset fromState =
                StateCapture.Capture(
                    liveRoot,
                    fromTree.name);

            // A to állapot capture: ideiglenesen betöltjük, capture,
            // majd vissza a from-ra.
            TemplateContainer toRoot = toTree.Instantiate();

            toRoot.style.width = new Length(100, LengthUnit.Percent);
            toRoot.style.height = new Length(100, LengthUnit.Percent);

            root.Add(toRoot);

            root.schedule.Execute(() =>
            {
                if (token != loadToken)
                    return;

                FigmaStateAsset toState =
                    StateCapture.Capture(
                        toRoot,
                        toTree.name);

                toRoot.RemoveFromHierarchy();

                float posTol = profile != null ? profile.positionTolerance : 1f;
                float sizeTol = profile != null ? profile.sizeTolerance : 1f;
                float opTol = profile != null ? profile.opacityTolerance : 0.01f;

                TransitionAnalysis analysis =
                    TransitionBuilder.Analyze(
                        fromState,
                        toState,
                        posTol,
                        sizeTol,
                        opTol);

                transition =
                    TransitionBuilder.Build(
                        analysis,
                        asset,
                        liveRoot);

                playTime = 0f;

                isPlaying = false;

                FitToWindow();

                // Függőben lévő seek (végállapotból): alkalmazás.
                if (pendingSeek >= 0f)
                {
                    float s = pendingSeek;

                    pendingSeek = -1f;

                    Seek(s);
                }
                else if (autoPlayAfterLoad)
                {
                    autoPlayAfterLoad = false;

                    Play();
                }

                Debug.Log(
                    $"[FigmaFlow] Preview kész: {transition.Count} track, " +
                    $"{transition.Duration:F2}s");
            }).StartingIn(50);
        }

        // ------------------------------------------------------------

        public void Play()
        {
            // Végállapotból: szekvenciában a TELJES szekvencia újraindul;
            // egyes módban a from fa + auto-play.
            if (atFinalState)
            {
                atFinalState = false;

                if (sequenceMode && sequenceStates != null)
                {
                    LoadSequence(
                        sequenceStates,
                        sequenceAsset,
                        sequenceProfile,
                        sequenceMachine);

                    autoPlayAfterLoad = true;

                    return;
                }

                autoPlayAfterLoad = true;

                Load(
                    lastFromTree,
                    lastToTree,
                    lastAsset,
                    lastProfile);

                return;
            }

            if (transition == null)
                return;

            transition.Reset();

            playTime = 0f;

            isPlaying = true;

            lastFrameTime = (float)EditorApplication.timeSinceStartup;
        }

        public void Stop()
        {
            isPlaying = false;
        }

        public void Seek(float time)
        {
            // Végállapotból: a from fa + transition újraépítése, majd
            // a seek alkalmazása (a régi transition a leválasztott
            // elemekre mutatna — a slider "nem használható" tünet).
            if (atFinalState)
            {
                atFinalState = false;

                pendingSeek = time;

                Load(
                    lastFromTree,
                    lastToTree,
                    lastAsset,
                    lastProfile);

                return;
            }

            if (transition == null)
                return;

            playTime = Mathf.Clamp(time, 0f, transition.Duration);

            transition.Evaluate(playTime);

            // A slider végén a végállapot (to fa) — így a háttérszín
            // változása is látszik a seek végén.
            if (playTime >= transition.Duration)
                ShowFinalState();
        }

        // ------------------------------------------------------------

        /// <summary>
        /// MINDEN frame-ben hívandó (a window Update-jéből).
        ///
        /// A fit CSAK a jelenlegi Build után fut — a capture-nek és a
        /// trackeknek a SKÁLÁZATLAN koordinátarendszerben kell maradniuk.
        /// A fit tiltva van Load után (amíg a build be nem fejeződött) —
        /// különben a második Load-nál a fit már az új fát skálázná a
        /// capture előtt, és a capture skálázott értékeket mérne.
        /// </summary>
        public void Update()
        {
            if (currentRoot != null && transition != null)
                FitToWindow();
        }

        /// <summary>
        /// A lejátszás frame-léptetése (a window Update hívja, ha IsReady).
        /// </summary>
        public void OnEditorUpdate()
        {
            if (isPlaying && transition != null)
            {
                float now = (float)EditorApplication.timeSinceStartup;

                float dt = now - lastFrameTime;

                lastFrameTime = now;

                playTime += dt;

                if (playTime >= transition.Duration)
                {
                    playTime = transition.Duration;

                    transition.Evaluate(playTime);

                    isPlaying = false;

                    ShowFinalState();

                    return;
                }

                transition.Evaluate(playTime);
            }

            // Scale-to-fit: MINDEN frame-ben próbáljuk — az editor-ablak
            // layout késleltetett, a worldBound eleinte 0 lehet (a régi
            // "ha változott" guard sosem triggerelt → a desktop lemaradt).
            if (currentRoot != null)
                FitToWindow();
        }

        /// <summary>
        /// A lejátszás végén a TO fa betöltése — a végállapot (háttér,
        /// szín) a valóságos. A következő Play a from-ról indul újra.
        /// </summary>
        void ShowFinalState()
        {
            if (atFinalState || lastToTree == null || root == null)
                return;

            // v0.5/4: szekvenciában, ha van következő pár → auto-advance
            // (a következő pár betöltése; a végállapot csak az utolsó
            // pár után jelenik meg).
            if (sequenceMode &&
                sequenceStates != null &&
                sequenceIndex < sequenceStates.Length - 2)
            {
                sequenceIndex++;

                // v0.5/7 (ADR-024): a következő pár assetje.
                TransitionAsset nextAsset =
                    sequenceMachine != null
                        ? sequenceMachine.ResolveAsset(
                            sequenceIndex,
                            sequenceIndex + 1)
                        : sequenceAsset;

                Load(
                    sequenceStates[sequenceIndex],
                    sequenceStates[sequenceIndex + 1],
                    nextAsset != null ? nextAsset : sequenceAsset,
                    sequenceProfile);

                // FONTOS: a Load törli a sequenceMode-ot — visszaállítjuk,
                // különben a következő pár már nem lépne tovább.
                sequenceMode = true;

                return;
            }

            atFinalState = true;

            // FONTOS (2026-08-02): a naturalSize RESET — a to desktop
            // MÁS méretű lehet, mint a from (pl. tab menü: 1019x487 ->
            // 1019x244). A fit a naturalSize-t csak egyszer méri (a
            // fromból); a to fához újra kell mérni, különben a végállapot
            // a from-méretre van skálázva/pozicionálva (elcsúszik).
            naturalSize = Vector2.zero;

            contentOffset = Vector2.zero;

            root.Clear();

            currentRoot = lastToTree.Instantiate();

            currentRoot.style.width = new Length(100, LengthUnit.Percent);
            currentRoot.style.height = new Length(100, LengthUnit.Percent);

            root.Add(currentRoot);

            liveRoot = currentRoot;

            FitToWindow();

            Debug.Log("[FigmaFlow] Preview: végállapot (to fa).");
        }

        /// <summary>
        /// A desktop illesztése — DETERMINISZTIKUSAN (2026-08-02).
        ///
        /// Kézi középre igazítás + bal-felső transform origó — semmi nem
        /// függ a flex-aligntól vagy a default origótól (a UI Toolkit-ben
        /// ezek megbízhatatlanok editor ablakban).
        ///
        /// Lépések:
        /// 1. a tartalom (Desktop) természetes méretét és a currentRoot-ban
        ///    lévő eltolását egyszer rögzíti
        /// 2. a currentRoot a tartalom méretére áll, ABSZOLÚT pozícióval
        /// 3. a skála bal-felső origóból (transformOrigin 0,0)
        /// 4. a pozíció: (window - skálázott tartalom) / 2 - eltolás*s
        ///    → a tartalom PONTOSAN középen, arányosan
        /// </summary>
        void FitToWindow()
        {
            if (currentRoot == null || root == null)
                return;

            float windowW = root.worldBound.width;
            float windowH = root.worldBound.height;

            if (windowW <= 0f || windowH <= 0f ||
                float.IsNaN(windowW) || float.IsNaN(windowH))
            {
                return; // a következő frame újra próbálja
            }

            // A tartalom természetes mérete + eltolása (egyszer).
            if (naturalSize.x <= 0f)
            {
                VisualElement content =
                    currentRoot.childCount > 0
                        ? currentRoot[0]
                        : currentRoot;

                float cw = content.worldBound.width;
                float ch = content.worldBound.height;

                if (cw <= 0f || ch <= 0f ||
                    float.IsNaN(cw) || float.IsNaN(ch))
                {
                    return;
                }

                naturalSize = new Vector2(cw, ch);

                contentOffset = new Vector2(
                    content.worldBound.x - currentRoot.worldBound.x,
                    content.worldBound.y - currentRoot.worldBound.y);

                // A konténer a tartalom méretére, abszolút pozícióval
                // (a left/top így érvényesül).
                currentRoot.style.position = Position.Absolute;
                currentRoot.style.width = cw;
                currentRoot.style.height = ch;

                Debug.Log(
                    $"[FigmaFlow] Fit: tartalom {cw:F0}x{ch:F0} " +
                    $"off ({contentOffset.x:F0},{contentOffset.y:F0}) " +
                    $"window {windowW:F0}x{windowH:F0}");
            }

            float s = Mathf.Min(
                1f,
                windowW / naturalSize.x,
                windowH / naturalSize.y);

            s = Mathf.Max(0.01f, s);

            float currentScale =
                currentRoot.style.scale.value.value.x;

            if (Mathf.Abs(currentScale - s) > 0.001f)
            {
                // Bal-felső origó + kézi középre igazítás.
                currentRoot.style.transformOrigin = new TransformOrigin(0f, 0f);
                currentRoot.style.scale = new Scale(new Vector2(s, s));

                float scaledW = naturalSize.x * s;
                float scaledH = naturalSize.y * s;

                currentRoot.style.left =
                    (windowW - scaledW) * 0.5f;

                currentRoot.style.top =
                    (windowH - scaledH) * 0.5f - contentOffset.y * s;

                Debug.Log(
                    $"[FigmaFlow] Fit: skála {currentScale:F3} -> {s:F3} " +
                    $"left={currentRoot.style.left.value.value:F0} " +
                    $"top={currentRoot.style.top.value.value:F0}");
            }
        }
    }
}

#endif
