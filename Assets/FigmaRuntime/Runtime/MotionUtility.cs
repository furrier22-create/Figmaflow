// ============================================================
// MOTION ENGINE FREEZE (v0.3.7)
// Ez a fájl a Motion Engine része. Stabil, bizonyítottan működik.
// Csak KRITIKUS BUG esetén módosítható, dokumentált döntéssel.
// Minden más fejlesztés ezen a fájlon kívül épüljön.
// ============================================================

using UnityEngine;

namespace FigmaRuntime
{
    public static class MotionUtility
    {
        const float Back = 1.70158f;

        public static float Evaluate(
            EaseType ease,
            float t,
            AnimationCurve curve = null)
        {
            t = Mathf.Clamp01(t);

            switch (ease)
            {
                case EaseType.Linear:
                    return t;

                case EaseType.EaseIn:
                    return EaseIn(t);

                case EaseType.EaseOut:
                    return EaseOut(t);

                case EaseType.EaseInOut:
                    return EaseInOut(t);

                case EaseType.EaseInBack:
                    return EaseInBack(t);

                case EaseType.EaseOutBack:
                    return EaseOutBack(t);

                case EaseType.EaseInOutBack:
                    return EaseInOutBack(t);

                case EaseType.Gentle:
                    return Gentle(t);

                case EaseType.Quick:
                    return Quick(t);

                case EaseType.Bouncy:
                    return Bounce(t);

                case EaseType.Slow:
                    return Slow(t);

                case EaseType.CustomBezier:
                    if (curve != null)
                        return curve.Evaluate(t);

                    return t;

                case EaseType.CustomSpring:
                    return t;

                default:
                    return t;
            }
        }

        static float EaseIn(float t)
        {
            return t * t * t;
        }

        static float EaseOut(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        static float EaseInOut(float t)
        {
            return Mathf.SmoothStep(0f, 1f, t);
        }

        static float Gentle(float t)
        {
            return t * t * (3f - 2f * t);
        }

        static float Quick(float t)
        {
            return 1f - Mathf.Pow(1f - t, 5f);
        }

        static float Slow(float t)
        {
            return Mathf.Pow(t, 1.5f);
        }

        static float EaseInBack(float t)
        {
            return (Back + 1f) * t * t * t - Back * t * t;
        }

        static float EaseOutBack(float t)
        {
            t -= 1f;

            return 1f +
                   (Back + 1f) * t * t * t +
                   Back * t * t;
        }

        static float EaseInOutBack(float t)
        {
            float s = Back * 1.525f;

            t *= 2f;

            if (t < 1f)
            {
                return 0.5f *
                    (t * t * ((s + 1f) * t - s));
            }

            t -= 2f;

            return 0.5f *
                (t * t * ((s + 1f) * t + s) + 2f);
        }

        static float Bounce(float t)
        {
            if (t < 1f / 2.75f)
                return 7.5625f * t * t;

            if (t < 2f / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + .75f;
            }

            if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + .9375f;
            }

            t -= 2.625f / 2.75f;

            return 7.5625f * t * t + .984375f;
        }
    }
}