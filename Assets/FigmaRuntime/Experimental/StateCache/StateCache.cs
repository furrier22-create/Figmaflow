using UnityEngine;

namespace FigmaRuntime
{
    /// <summary>
    /// A Runtime állapotcache-e.
    ///
    /// A StateMachine nem építi fel a state-eket,
    /// csak használja õket.
    ///
    /// A cache felépítése a StateCacheBuilder feladata.
    /// </summary>
    public class StateCache
    {
        private readonly FigmaStateAsset[] states;

        public int Count => states.Length;

        public StateCache(int count)
        {
            states = new FigmaStateAsset[count];
        }

        public void Set(
            int index,
            FigmaStateAsset state)
        {
            states[index] = state;
        }

        public FigmaStateAsset Get(
            int index)
        {
            if (index < 0 || index >= states.Length)
                return null;

            return states[index];
        }

        public bool IsValid(
            int index)
        {
            return Get(index) != null;
        }

        public bool IsComplete()
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] == null)
                    return false;
            }

            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = null;
            }
        }
    }
}