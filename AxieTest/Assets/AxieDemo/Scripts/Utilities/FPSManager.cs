using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Axie.Utilities
{
    public class FPSManager : MonoBehaviour
    {
        private const float t_fpsMeasurePeriod = 0.5f;

        private int fpsAccumulator = 0;
        private float fpsNextPeriod = 0;
        private int currentFps;

        public int FPS => currentFps;

        private void Start()
        {
            fpsNextPeriod = Time.realtimeSinceStartup + t_fpsMeasurePeriod;
        }


        private void Update()
        {
            // measure average frames per second
            fpsAccumulator++;
            if (Time.realtimeSinceStartup > fpsNextPeriod)
            {
                currentFps = (int)(fpsAccumulator / t_fpsMeasurePeriod);
                fpsAccumulator = 0;
                fpsNextPeriod += t_fpsMeasurePeriod;
            }
        }
    }
}