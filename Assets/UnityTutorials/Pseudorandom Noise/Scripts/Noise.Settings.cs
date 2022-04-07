using System;
using UnityEngine;

namespace PseudorandomNoise
{
    public static partial class Noise
    {
        [Serializable]
        public struct Settings
        {
            // pseudorandom seed to generate numbers
            public int seed;

            // sample frequency, 
            [Min(1)]
            public int frequency;

            // more samples means more detail(via different size)
            // number samples
            [Range(1,6)]
            public int octaves;

            // scaling term to control how frequency changes
            // larger means more gap between octaves
            // because the distance between samples is larger
            [Range(2,4)]
            public int lacunarity;

            // amplitude reduction factor
            [Range(0.0f,1.0f)]
            public float persistence;

            public static Settings Default => new Settings
            {
                seed = 0,
                frequency = 4,
                octaves = 1,
                lacunarity = 2,
                persistence = 0.5f,
            };
        }
    }
}