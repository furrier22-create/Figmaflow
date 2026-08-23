#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace FigmaRuntime.Editor
{
    public static class TransitionAssetGenerator
    {
        public static TransitionAsset Generate(
            VisualTreeAsset fromState,
            VisualTreeAsset toState,
            FigmaFlowProfile profile)
        {
            if (!FigmaFlowRuntime.HasAnalysis)
            {
                EditorUtility.DisplayDialog(
                    "FigmaFlow",
                    "Nincs Runtime Analysis.\n\nLépj Play módba és futtass le egy átmenetet (Next), majd próbáld újra.",
                    "OK");

                return null;
            }

            EnsureFolders();

            string assetPath =
                $"Assets/FigmaFlow/Transitions/{fromState.name}_{toState.name}.asset";

            TransitionAsset asset =
                AssetDatabase.LoadAssetAtPath<TransitionAsset>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TransitionAsset>();

                asset.fromState = fromState.name;
                asset.toState = toState.name;

                AssetDatabase.CreateAsset(asset, assetPath);
            }

            TransitionAssetBuilder.Fill(
                asset,
                FigmaFlowRuntime.CurrentAnalysis.Candidates);

            EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;

            Debug.Log(
                $"FigmaFlow: Generated {asset.items.Count} items.");

            return asset;
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FigmaFlow"))
            {
                AssetDatabase.CreateFolder(
                    "Assets",
                    "FigmaFlow");
            }

            if (!AssetDatabase.IsValidFolder("Assets/FigmaFlow/Transitions"))
            {
                AssetDatabase.CreateFolder(
                    "Assets/FigmaFlow",
                    "Transitions");
            }
        }
    }
}

#endif