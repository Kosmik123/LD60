using System;
using UnityEngine;

namespace SpaghettiWires.Simulation
{
    /// <summary>
    /// Liczby do balansu rozmów. Wartości są robocze, do ustalenia w testach (GDD 9).
    /// Docelowo trafią do wspólnego ScriptableObject z ustawieniami gry (PLAN etap 2).
    /// </summary>
    [Serializable]
    public sealed class CallSettings
    {
        [Tooltip("Ile rozmów na minutę zaczyna jeden dom na początku gry.")]
        [Min(0f)] [SerializeField] private float callsPerHousePerMinute = 1f;

        [Tooltip("O ile rośnie ta liczba z każdą minutą gry.")]
        [Min(0f)] [SerializeField] private float growthPerMinute = 0.25f;

        [Tooltip("Najkrótsza rozmowa (s).")]
        [Min(0.1f)] [SerializeField] private float minDuration = 6f;

        [Tooltip("Najdłuższa rozmowa (s).")]
        [Min(0.1f)] [SerializeField] private float maxDuration = 12f;

        public float MinDuration => minDuration;
        public float MaxDuration => Mathf.Max(minDuration, maxDuration);

        public CallSettings() { }

        public CallSettings(float callsPerHousePerMinute, float growthPerMinute, float minDuration, float maxDuration)
        {
            this.callsPerHousePerMinute = callsPerHousePerMinute;
            this.growthPerMinute = growthPerMinute;
            this.minDuration = minDuration;
            this.maxDuration = maxDuration;
        }

        /// <summary>Ile rozmów na minutę zaczyna jeden dom po <paramref name="time"/> sekundach gry.</summary>
        public float CallsPerHousePerMinute(float time) => callsPerHousePerMinute + growthPerMinute * time / 60f;
    }
}
