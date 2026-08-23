using System;
using System.Collections.Generic;
using UnityEngine;

namespace FigmaRuntime
{
    [CreateAssetMenu(menuName = "FigmaFlow/Transition Asset")]
    public class TransitionAsset : ScriptableObject
    {
        public string fromState;
        public string toState;

        public List<TransitionItem> items = new();

        /// <summary>
        /// v0.6/2 (ADR-027): a Motion Studio mentett mozgás-előbeállításai
        /// (az assetben tárolva — így minden pár-assetnek lehet saját
        /// preset-készlete).
        /// </summary>
        public List<MotionPreset> presets = new();
    }

    [Serializable]
    public class TransitionItem
    {
        public string path;

        public bool enabled = true;

        public bool favorite = false;

        public float delay = 0f;

        public float duration = 0.6f;

        public EaseType ease = EaseType.EaseInOut;

        public AppearMode appearMode =
            AppearMode.Fade;

        public AnimationMode animationMode =
            AnimationMode.Layout;

        public AnimatedProperties properties;

        public AnimationCurve customCurve =
            AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
}
