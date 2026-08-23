#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace FigmaRuntime.Editor
{
    /// <summary>
    /// v0.4 / P3 (ADR-009) — a FigmaStateCacheAsset előállítása.
    ///
    /// Működés: Play Mode-ban a FigmaStateMachine már felépítette a
    /// state-eket (runtime capture). Ez az eszköz ezt a kész állapotot
    /// menti el editor asset-ként. A mentés után a StateMachine-nek elég
    /// a cache-re hivatkoznia — többé nem épít cache-t, nincs felvillanás.
    ///
    /// Több scene támogatás (ADR-010): az export SCENE-enként külön
    /// almappába megy (Assets/FigmaFlow/Cache/{scene}/...), így a
    /// különböző scene-ek cache-ei nem írják felül egymást. Az asset-ben
    /// a sceneName is rögzítésre kerül — a runtime eltérésre figyelmeztet.
    ///
    /// Miért Play Mode-os export? A capture valódi layoutot igényel
    /// (worldRect / resolvedStyle). A Play Mode-os mérés megbízható;
    /// a tiszta editor-only offscreen mérés egy későbbi finomítás.
    /// </summary>
    public static class StateCacheExporter
    {
        public static FigmaStateCacheAsset Export(
            FigmaStateMachine machine)
        {
            if (machine == null)
                return null;

            FigmaStateAsset[] captured =
                machine.GetCapturedStates();

            if (captured == null || captured.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "FigmaFlow",
                    "A StateMachine még nem készítette el a state capture-t.\n\n" +
                    "Lépj Play Mode-ba, várd meg az indítást, majd próbáld újra.",
                    "OK");

                return null;
            }

            string sceneName = GetActiveSceneName();

            string safeScene = SanitizeFolderName(sceneName);

            EnsureFolders(safeScene);

            string folder =
                $"Assets/FigmaFlow/Cache/{safeScene}";

            string path =
                $"{folder}/StateCache_{machine.gameObject.name}.asset";

            FigmaStateCacheAsset cache =
                AssetDatabase.LoadAssetAtPath<FigmaStateCacheAsset>(path);

            if (cache == null)
            {
                cache =
                    ScriptableObject.CreateInstance<FigmaStateCacheAsset>();

                AssetDatabase.CreateAsset(cache, path);
            }

            cache.sceneName = sceneName;

            cache.states.Clear();

            foreach (FigmaStateAsset state in captured)
            {
                if (state == null)
                    continue;

                cache.states.Add(
                    new CachedState()
                    {
                        stateName = state.stateName,
                        elements = state.elements
                    });
            }

            EditorUtility.SetDirty(cache);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[FigmaFlow] StateCache exportálva: " +
                $"{cache.states.Count} state -> {path} (scene: {sceneName})");

            return cache;
        }

        static string GetActiveSceneName()
        {
            Scene scene = SceneManager.GetActiveScene();

            return string.IsNullOrEmpty(scene.name)
                ? "Untitled"
                : scene.name;
        }

        static string SanitizeFolderName(string name)
        {
            string s = name;

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');

            s = s.Replace(':', '_');

            return string.IsNullOrWhiteSpace(s)
                ? "Scene"
                : s.Trim();
        }

        /// <summary>
        /// v0.6/5 (ADR-029): EDITOR-ONLY export — Play Mode NÉLKÜL.
        ///
        /// A states tömb minden VTA-ját egy rejtett UIDocument-re tölti
        /// (a TransitionPreview-ben bevált schedule + frame-várakozás
        /// mintával — az editor layout késleltetett), capture-eli, és
        /// ugyanazt a FigmaStateCacheAsset-et hozza létre (a meglévő
        /// formátum!). A runtime betöltése változatlan.
        ///
        /// Az aszinkron (schedule) miatt az export kérése és a fájl
        /// létrehozása között idő telik — a kész log jelzi.
        /// </summary>
        public static void ExportOffscreen(
            FigmaStateMachine machine,
            System.Action<FigmaStateCacheAsset> onDone = null)
        {
            if (machine == null)
                return;

            VisualTreeAsset[] states = machine.states;

            if (states == null || states.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "FigmaFlow",
                    "A StateMachine states tömbje üres.",
                    "OK");

                return;
            }

            // Rejtett offscreen host.
            GameObject go =
                new GameObject("__FigmaFlowOffscreenCache");

            go.hideFlags = HideFlags.HideAndDontSave;

            UIDocument doc = go.AddComponent<UIDocument>();

            PanelSettings ps =
                ScriptableObject.CreateInstance<PanelSettings>();

            doc.panelSettings = ps;

            VisualElement root = doc.rootVisualElement;

            root.style.width = 1920;
            root.style.height = 1080;

            // Az állapotok capture-ölése sorban (a layout-várakozás
            // minden VTA után).
            System.Collections.Generic.List<FigmaStateAsset> captured =
                new();

            int index = 0;

            System.Action<VisualElement> processNext = null;

            processNext = (current) =>
            {
                if (current != null)
                    current.RemoveFromHierarchy();

                if (index >= states.Length)
                {
                    // Kész — a cache létrehozása.
                    Object.DestroyImmediate(go);

                    FinishOffscreenExport(
                        machine,
                        captured,
                        onDone);

                    return;
                }

                TemplateContainer container =
                    states[index].Instantiate();

                container.style.width = new Length(100, LengthUnit.Percent);
                container.style.height = new Length(100, LengthUnit.Percent);

                root.Add(container);

                // A layout lefutása (a resolvedStyle csak utána érvényes).
                root.schedule.Execute(() =>
                {
                    FigmaStateAsset state =
                        StateCapture.Capture(
                            container,
                            states[index].name);

                    captured.Add(state);

                    index++;

                    processNext(container);
                }).StartingIn(50);
            };

            processNext(null);
        }

        /// <summary>
        /// A kész capture-ekből a cache asset létrehozása (a meglévő
        /// formátummal — a Play Mode-os Export közös része).
        /// </summary>
        static void FinishOffscreenExport(
            FigmaStateMachine machine,
            System.Collections.Generic.List<FigmaStateAsset> captured,
            System.Action<FigmaStateCacheAsset> onDone)
        {
            if (captured.Count == 0)
            {
                Debug.LogError(
                    "[FigmaFlow] Offscreen export: 0 állapotot sikerült capture-ölni.");

                return;
            }

            string sceneName = GetActiveSceneName();

            string safeScene = SanitizeFolderName(sceneName);

            EnsureFolders(safeScene);

            string folder =
                $"Assets/FigmaFlow/Cache/{safeScene}";

            string path =
                $"{folder}/StateCache_{machine.gameObject.name}.asset";

            FigmaStateCacheAsset cache =
                AssetDatabase.LoadAssetAtPath<FigmaStateCacheAsset>(path);

            if (cache == null)
            {
                cache =
                    ScriptableObject.CreateInstance<FigmaStateCacheAsset>();

                AssetDatabase.CreateAsset(cache, path);
            }

            cache.sceneName = sceneName;

            cache.states.Clear();

            foreach (FigmaStateAsset state in captured)
            {
                if (state == null)
                    continue;

                cache.states.Add(
                    new CachedState()
                    {
                        stateName = state.stateName,
                        elements = state.elements
                    });
            }

            EditorUtility.SetDirty(cache);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[FigmaFlow] Offscreen StateCache exportálva: " +
                $"{cache.states.Count} state -> {path} " +
                $"(scene: {sceneName}, Play Mode NÉLKÜL).");

            onDone?.Invoke(cache);
        }

        static void EnsureFolders(string sceneFolder)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FigmaFlow"))
            {
                AssetDatabase.CreateFolder(
                    "Assets",
                    "FigmaFlow");
            }

            if (!AssetDatabase.IsValidFolder("Assets/FigmaFlow/Cache"))
            {
                AssetDatabase.CreateFolder(
                    "Assets/FigmaFlow",
                    "Cache");
            }

            string cacheFolder = "Assets/FigmaFlow/Cache";

            if (!AssetDatabase.IsValidFolder($"{cacheFolder}/{sceneFolder}"))
            {
                AssetDatabase.CreateFolder(
                    cacheFolder,
                    sceneFolder);
            }
        }
    }
}

#endif
