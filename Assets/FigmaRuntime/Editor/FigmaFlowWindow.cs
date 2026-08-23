#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Editor
{
    public class FigmaFlowWindow : EditorWindow
    {
        private VisualTreeAsset fromState;
        private VisualTreeAsset toState;

        private FigmaFlowProfile profile;

        private TransitionAsset transitionAsset;

        private TransitionPreview preview;

        // v0.5/4: a states-alapú szekvencia forrása.
        private FigmaStateMachine stateMachine;

        // v0.6/2: a Motion Presets szerkesztő állapota.
        private string presetName = "Új preset";

        private EaseType presetEase = EaseType.EaseInOut;

        private float presetDuration = 0.6f;

        private AppearMode presetAppear = AppearMode.Fade;

        private int selectedPresetIndex;

        private Vector2 scrollPos;

        [MenuItem("Tools/FigmaFlow/Motion Studio")]
        public static void Open()
        {
            GetWindow<FigmaFlowWindow>("FigmaFlow");
        }

        void OnEnable()
        {
            preview = new TransitionPreview();
        }

        void OnGUI()
        {
            // v0.6 (javítás): az ablak hosszú lett — a tartalom
            // görgethető.
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Space(8);

            GUILayout.Label(
                "FigmaFlow Motion Studio",
                EditorStyles.boldLabel);

            GUILayout.Space(8);

            fromState =
                (VisualTreeAsset)EditorGUILayout.ObjectField(
                    "From",
                    fromState,
                    typeof(VisualTreeAsset),
                    false);

            toState =
                (VisualTreeAsset)EditorGUILayout.ObjectField(
                    "To",
                    toState,
                    typeof(VisualTreeAsset),
                    false);

            profile =
                (FigmaFlowProfile)EditorGUILayout.ObjectField(
                    "Profile",
                    profile,
                    typeof(FigmaFlowProfile),
                    false);

            transitionAsset =
                (TransitionAsset)EditorGUILayout.ObjectField(
                    "Transition Asset",
                    transitionAsset,
                    typeof(TransitionAsset),
                    false);

            GUILayout.Space(12);

            GUI.enabled =
                fromState != null &&
                toState != null &&
                profile != null;

            if (GUILayout.Button(
                "Generate Transition Asset",
                GUILayout.Height(32)))
            {
                transitionAsset =
                    TransitionAssetGenerator.Generate(
                        fromState,
                        toState,
                        profile);
            }

            GUI.enabled = true;

            GUILayout.Space(20);

            EditorGUILayout.LabelField(
                "Runtime",
                EditorStyles.boldLabel);

            GUI.enabled =
                transitionAsset != null &&
                FigmaFlowRuntime.HasAnalysis;

            if (GUILayout.Button(
                "Import Runtime Analysis",
                GUILayout.Height(32)))
            {
                TransitionAssetBuilder.Fill(
                    transitionAsset,
                    FigmaFlowRuntime.CurrentAnalysis.Candidates);

                EditorUtility.SetDirty(
                    transitionAsset);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject =
                    transitionAsset;
            }

            GUI.enabled = true;

            GUILayout.Space(20);

            EditorGUILayout.LabelField(
                "State Cache",
                EditorStyles.boldLabel);

            FigmaStateMachine machine =
                Object.FindFirstObjectByType<FigmaStateMachine>();

            if (machine == null)
            {
                EditorGUILayout.HelpBox(
                    "Nincs FigmaStateMachine a jelenetben.",
                    MessageType.Warning);
            }
            else
            {
                machine.stateCache =
                    (FigmaStateCacheAsset)EditorGUILayout.ObjectField(
                        "State Cache",
                        machine.stateCache,
                        typeof(FigmaStateCacheAsset),
                        false);

                if (GUILayout.Button(
                    "Export State Cache",
                    GUILayout.Height(28)))
                {
                    if (!Application.isPlaying)
                    {
                        EditorUtility.DisplayDialog(
                            "FigmaFlow",
                            "Az export Play Mode-ban működik (a capture valódi " +
                            "layoutot igényel).\n\n" +
                            "1. Lépj Play Mode-ba, várd meg az indítást\n" +
                            "2. Kattints az Export gombra\n" +
                            "3. Lépj ki Play Mode-ból\n" +
                            "4. Húzd a létrejött assetet a 'State Cache' mezőre",
                            "OK");
                    }
                    else
                    {
                        FigmaStateCacheAsset cache =
                            StateCacheExporter.Export(machine);

                        if (cache != null)
                        {
                            // Play Mode-ban a jelenet-módosítás nem marad
                            // meg — az asset kiválasztva, edit mode-ban
                            // a mezőre húzással állítható be.
                            Selection.activeObject = cache;

                            Debug.Log(
                                "[FigmaFlow] Lépj ki Play Mode-ból, majd húzd " +
                                "az assetet a StateMachine 'State Cache' mezőjére.");
                        }
                    }
                }

                // v0.6/5 (ADR-029): EDITOR-ONLY export — Play Mode nélkül,
                // az offscreen méréssel (a StateMachine states tömbjéből).
                if (GUILayout.Button(
                    "Export State Cache (Editor)",
                    GUILayout.Height(26)))
                {
                    StateCacheExporter.ExportOffscreen(
                        machine,
                        (cache) =>
                        {
                            if (cache != null)
                            {
                                Selection.activeObject = cache;

                                Debug.Log(
                                    "[FigmaFlow] Húzd az assetet a StateMachine " +
                                    "'State Cache' mezőjére.");
                            }
                        });
                }
            }

            GUILayout.Space(20);

            EditorGUILayout.LabelField(
                "Preview (v0.5)",
                EditorStyles.boldLabel);

            GUI.enabled =
                fromState != null &&
                toState != null &&
                transitionAsset != null &&
                profile != null;

            if (GUILayout.Button(
                "Load Preview",
                GUILayout.Height(28)))
            {
                // A preview a saját, UI Toolkit-alapú ablakban fut
                // (a Motion Studio IMGUI-s — ott a UI Toolkit nem
                // renderelődne).
                FigmaFlowPreviewWindow.Open(
                    preview,
                    fromState,
                    toState,
                    transitionAsset,
                    profile);
            }

            GUI.enabled = true;

            EditorGUILayout.HelpBox(
                "A lejátszás a 'FigmaFlow Preview' ablakban történik " +
                "(Play/Stop + idő-slider).",
                MessageType.Info);

            GUILayout.Space(12);

            GUI.enabled = transitionAsset != null;

            if (GUILayout.Button(
                "Open Timeline (v0.5)",
                GUILayout.Height(28)))
            {
                // v0.5/5 (ADR-021): a vizuális idővonal — az asset
                // itemjei, a delay/duration húzogatva, a Preview-val
                // szinkronban.
                FigmaFlowTimelineWindow.Open(
                    preview,
                    transitionAsset);
            }

            GUI.enabled = true;

            GUILayout.Space(20);

            EditorGUILayout.LabelField(
                "Motion Presets (v0.6)",
                EditorStyles.boldLabel);

            // A preset-szerkesztő mezők.
            presetName =
                EditorGUILayout.TextField(
                    "Preset név",
                    presetName);

            presetEase =
                (EaseType)EditorGUILayout.EnumPopup(
                    "Ease",
                    presetEase);

            presetDuration =
                EditorGUILayout.FloatField(
                    "Duration",
                    presetDuration);

            presetAppear =
                (AppearMode)EditorGUILayout.EnumPopup(
                    "Appear",
                    presetAppear);

            GUILayout.BeginHorizontal();

            GUI.enabled = transitionAsset != null;

            if (GUILayout.Button(
                "Mentés presetként",
                GUILayout.Height(26)))
            {
                transitionAsset.presets.Add(
                    new MotionPreset(
                        presetName,
                        presetEase,
                        presetDuration,
                        presetAppear));

                EditorUtility.SetDirty(transitionAsset);

                Debug.Log(
                    $"[FigmaFlow] Preset mentve: '{presetName}'.");
            }

            GUI.enabled = true;

            GUILayout.EndHorizontal();

            // A mentett presets listája + alkalmazás.
            if (transitionAsset != null &&
                transitionAsset.presets != null &&
                transitionAsset.presets.Count > 0)
            {
                GUILayout.Space(6);

                selectedPresetIndex =
                    Mathf.Clamp(
                        selectedPresetIndex,
                        0,
                        transitionAsset.presets.Count - 1);

                string[] presetNames = new string[
                    transitionAsset.presets.Count];

                for (int i = 0; i < transitionAsset.presets.Count; i++)
                {
                    MotionPreset p = transitionAsset.presets[i];

                    presetNames[i] =
                        $"{p.presetName} " +
                        $"({p.ease}, {p.duration:F2}s, {p.appearMode})";
                }

                selectedPresetIndex =
                    EditorGUILayout.Popup(
                        "Preset",
                        selectedPresetIndex,
                        presetNames);

                if (GUILayout.Button(
                    "Apply to All Items",
                    GUILayout.Height(26)))
                {
                    MotionPreset p =
                        transitionAsset.presets[selectedPresetIndex];

                    foreach (TransitionItem item in transitionAsset.items)
                    {
                        item.ease = p.ease;
                        item.duration = p.duration;
                        item.appearMode = p.appearMode;
                    }

                    EditorUtility.SetDirty(transitionAsset);

                    Debug.Log(
                        $"[FigmaFlow] Preset '{p.presetName}' alkalmazva " +
                        $"{transitionAsset.items.Count} itemre.");
                }

                if (GUILayout.Button(
                    "Preset törlése",
                    GUILayout.Height(22)))
                {
                    transitionAsset.presets.RemoveAt(
                        selectedPresetIndex);

                    selectedPresetIndex = 0;

                    EditorUtility.SetDirty(transitionAsset);
                }
            }

            GUILayout.Space(20);

            EditorGUILayout.LabelField(
                "States Sequence (v0.5)",
                EditorStyles.boldLabel);

            stateMachine =
                (FigmaStateMachine)EditorGUILayout.ObjectField(
                    "State Machine",
                    stateMachine,
                    typeof(FigmaStateMachine),
                    true);

            GUI.enabled =
                stateMachine != null &&
                stateMachine.states != null &&
                stateMachine.states.Length >= 2 &&
                stateMachine.transitionAsset != null;

            if (GUILayout.Button(
                "Load Sequence Preview",
                GUILayout.Height(28)))
            {
                // v0.5/4: a teljes 0->1->2->... szekvencia a states
                // tömbből (a gép transitionAsset-jével, páronként).
                FigmaFlowPreviewWindow.OpenSequence(
                    preview,
                    stateMachine.states,
                    stateMachine.transitionAsset,
                    stateMachine.profile,
                    stateMachine);
            }

            GUI.enabled = true;

            GUILayout.Space(6);

            // v0.5/7 (ADR-024): a kényelmi gomb — a jelenlegi From/To pár
            // assetjét hozzáadja a gép pairAssets tömbjéhez (hogy ne kelljen
            // kézzel húzogatni).
            GUI.enabled =
                stateMachine != null &&
                transitionAsset != null &&
                transitionAsset.fromState != null &&
                transitionAsset.toState != null;

            if (GUILayout.Button(
                "Add Current Pair to StateMachine",
                GUILayout.Height(28)))
            {
                System.Array.Resize(
                    ref stateMachine.pairAssets,
                    (stateMachine.pairAssets != null
                        ? stateMachine.pairAssets.Length
                        : 0) + 1);

                stateMachine.pairAssets[
                    stateMachine.pairAssets.Length - 1] =
                    transitionAsset;

                EditorUtility.SetDirty(stateMachine);

                Debug.Log(
                    $"[FigmaFlow] Pair asset hozzáadva: " +
                    $"{transitionAsset.fromState} -> {transitionAsset.toState} " +
                    $"({stateMachine.gameObject.name}).");
            }

            GUI.enabled = true;

            if (stateMachine == null)
            {
                EditorGUILayout.HelpBox(
                    "Válassz ki egy FigmaStateMachine-t a jelenetből.",
                    MessageType.Info);
            }
            else if (stateMachine.states == null ||
                     stateMachine.states.Length < 2)
            {
                EditorGUILayout.HelpBox(
                    "A StateMachine-nek legalább 2 állapotra van szüksége.",
                    MessageType.Warning);
            }

            GUILayout.Space(8);

            if (FigmaFlowRuntime.HasAnalysis)
            {
                EditorGUILayout.HelpBox(
                    $"Runtime Analysis Ready\nCandidates: {FigmaFlowRuntime.CurrentAnalysis.Candidates.Count}",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Play Mode-ban futtass egy átmenetet (Next), majd importáld az elemzést.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}

#endif