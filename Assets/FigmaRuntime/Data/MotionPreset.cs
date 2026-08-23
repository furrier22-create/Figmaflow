using System;
using UnityEngine;

namespace FigmaRuntime
{
    /// <summary>
    /// v0.6/2 (ADR-027): egy mentett mozgás-előbeállítás.
    ///
    /// A Motion Studio-ban névvel menthető (ease + duration + appearMode
    /// kombináció), és egy gombbal alkalmazható a TransitionAsset
    /// itemjeire — így a gyakori "stílusok" (pl. gyors fade, csúszó,
    /// bouncy) újrahasznosíthatók.
    /// </summary>
    [Serializable]
    public class MotionPreset
    {
        [Tooltip("A preset neve (pl. 'Gyors fade', 'Csúszó', 'Bouncy').")]
        public string presetName = "Új preset";

        [Tooltip("Az ease.")]
        public EaseType ease = EaseType.EaseInOut;

        [Tooltip("A duration másodpercben.")]
        public float duration = 0.6f;

        [Tooltip("Az appearMode.")]
        public AppearMode appearMode = AppearMode.Fade;

        public MotionPreset()
        {
        }

        public MotionPreset(
            string name,
            EaseType ease,
            float duration,
            AppearMode appearMode)
        {
            this.presetName = name;
            this.ease = ease;
            this.duration = duration;
            this.appearMode = appearMode;
        }
    }
}
