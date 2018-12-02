#if ENABLE_DEBUG

namespace MainContents.DebugUtility
{
    using UnityEngine;

    /// <summary>
    /// FPS計測
    /// </summary>
    public sealed class FPSCounter
    {
        const float FPSMeasurePeriod = 0.5f;

        public static int CurrentFps { get; private set; } = 0;

        int _FpsAccumulator = 0;
        float _FpsNextPeriod = 0;

        public FPSCounter()
        {
            this._FpsNextPeriod = Time.realtimeSinceStartup + FPSMeasurePeriod;
        }

        public void UpdateInternal()
        {
            ++this._FpsAccumulator;
            if (Time.realtimeSinceStartup > this._FpsNextPeriod)
            {
                CurrentFps = (int)(this._FpsAccumulator / FPSMeasurePeriod);
                this._FpsAccumulator = 0;
                this._FpsNextPeriod += FPSMeasurePeriod;
            }
        }
    }
}

#endif
