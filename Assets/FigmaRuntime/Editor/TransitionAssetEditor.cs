#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FigmaRuntime.Editor
{
    [CustomEditor(typeof(TransitionAsset))]
    public class TransitionAssetEditor : UnityEditor.Editor
    {
        private TransitionAsset asset;

        private string search = "";

        private bool onlyEnabled = false;

        void OnEnable()
        {
            asset = (TransitionAsset)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            EditorGUILayout.LabelField(
                "FigmaFlow Motion Studio",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("From", asset.fromState);
            EditorGUILayout.LabelField("To", asset.toState);

            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            search =
                EditorGUILayout.TextField(
                    "Search",
                    search);

            onlyEnabled =
                EditorGUILayout.Toggle(
                    "Only Enabled",
                    onlyEnabled);

            GUILayout.Space(8);

            DrawSection(
                "★ Favorites",
                asset.items.Where(i => i.favorite));

            DrawSection(
                "All",
                asset.items.Where(i => !i.favorite));

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
                EditorUtility.SetDirty(asset);
        }

        void DrawSection(
            string title,
            System.Collections.Generic.IEnumerable<TransitionItem> items)
        {
            var list = items.Where(PassFilter).ToList();

            if (list.Count == 0)
                return;

            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);

            GUILayout.Space(3);

            foreach (var item in list)
            {
                DrawItem(item);
            }

            GUILayout.Space(8);
        }

        bool PassFilter(TransitionItem item)
        {
            if (onlyEnabled && !item.enabled)
                return false;

            if (!string.IsNullOrEmpty(search))
            {
                if (!item.path.ToLower().Contains(search.ToLower()))
                    return false;
            }

            return true;
        }

        void DrawItem(TransitionItem item)
        {
            EditorGUILayout.BeginVertical("HelpBox");

            EditorGUILayout.BeginHorizontal();

            item.enabled =
                EditorGUILayout.Toggle(
                    item.enabled,
                    GUILayout.Width(18));

            item.favorite =
                GUILayout.Toggle(
                    item.favorite,
                    "★",
                    GUILayout.Width(24));

            EditorGUILayout.LabelField(
                item.path,
                EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(!item.enabled);

            item.delay =
    EditorGUILayout.Slider(
        "Delay",
        item.delay,
        0f,
        5f);

            item.duration =
                EditorGUILayout.Slider(
                    "Duration",
                    item.duration,
                    0.05f,
                    5f);

            item.ease =
                (EaseType)EditorGUILayout.EnumPopup(
                    "Motion",
                    item.ease);

            if (item.ease == EaseType.CustomSpring)
            {
                // F7/F25 (v0.3.7): a CustomSpring a MotionUtility-ban még
                // nincs implementálva (stub, lineárisan viselkedik) — v0.4.
                EditorGUILayout.HelpBox(
                    "CustomSpring még nincs implementálva (v0.4). " +
                    "Amíg nem készül el, lineárisan viselkedik.",
                    MessageType.Warning);
            }

            if (item.ease == EaseType.CustomBezier)
            {
                item.customCurve =
                    EditorGUILayout.CurveField(
                        "Curve",
                        item.customCurve);
            }
            item.animationMode =
                (AnimationMode)EditorGUILayout.EnumPopup(
                    "Mode",
                    item.animationMode);

            item.appearMode =
                (AppearMode)EditorGUILayout.EnumPopup(
                    "Appear",
                    item.appearMode);

            EditorGUILayout.LabelField(
                "Properties",
                item.properties.ToString());

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();

            GUILayout.Space(2);
        }
    }
}

#endif