using System;
using System.Collections.Generic;
using Bipolar;
using SpaghettiWires.Simulation;
using SpaghettiWires.Tools;
using UnityEngine;

namespace SpaghettiWires.Game
{
    /// <summary>
    /// Postęp rozgrywki w jednym miejscu: domy na start, pojawianie się nowych domów (coraz szybciej, GDD 3.7)
    /// i odblokowywanie narzędzi w czasie gry. Czas bierze z <see cref="GameSession"/>, więc po przegranej postęp staje.
    /// </summary>
    public sealed class GameProgression : MonoBehaviour
    {
        [Serializable]
        private struct ToolUnlock
        {
            public ToolType tool;
            [Tooltip("Sekunda gry, w której narzędzie się odblokowuje.")]
            public float time;
        }

        [SerializeField] private GameSession session;
        [SerializeField] private ToolController tools;
        [SerializeField] private Serialized<IHouseSpawner> houses;

        [Header("Domy")]
        [SerializeField] private int startHouses = 2;
        [Tooltip("Po ilu sekundach gry pojawia się pierwszy nowy dom.")]
        [SerializeField] private float firstHouseDelay = 30f;
        [Tooltip("Mnożnik odstępu po każdym nowym domu. Mniej niż 1: miasto rośnie coraz szybciej.")]
        [Range(0.1f, 1f)] [SerializeField] private float houseIntervalFactor = 0.9f;
        [Tooltip("Najkrótszy odstęp między nowymi domami (s).")]
        [SerializeField] private float minHouseInterval = 8f;

        [Header("Narzędzia")]
        [Tooltip("Kiedy odblokowują się narzędzia. Kabel jest dostępny od początku (ToolController).")]
        [SerializeField] private List<ToolUnlock> toolUnlocks = new List<ToolUnlock>
        {
            new ToolUnlock { tool = ToolType.Pole, time = 45f },
            new ToolUnlock { tool = ToolType.Scissors, time = 90f },
            new ToolUnlock { tool = ToolType.Meter, time = 150f },
            new ToolUnlock { tool = ToolType.Trunk, time = 240f },
        };

        private float _houseInterval;
        private float _nextHouseTime;

        public IHouseSpawner Houses
        {
            get => houses.Value;
            set => houses.Value = value;
        }

        /// <summary>Wywoływane po pojawieniu się nowego domu (także tych na start).</summary>
        public event Action HouseSpawned;

        private void Start()
        {
            for (int i = 0; i < startHouses; i++)
                SpawnHouse();

            _houseInterval = firstHouseDelay;
            _nextHouseTime = firstHouseDelay;
        }

        private void Update()
        {
            float time = session.ElapsedTime;

            if (time >= _nextHouseTime)
            {
                SpawnHouse();
                _houseInterval = Mathf.Max(minHouseInterval, _houseInterval * houseIntervalFactor);
                _nextHouseTime += _houseInterval;
            }

            foreach (var unlock in toolUnlocks)
            {
                if (time >= unlock.time && !tools.IsUnlocked(unlock.tool))
                    UnlockTool(unlock.tool);
            }
        }

        /// <summary>Stawia nowy dom poza harmonogramem. False, gdy nie ma na niego miejsca.</summary>
        public bool SpawnHouse()
        {
            if (!houses.Value.TrySpawnHouse())
                return false;

            HouseSpawned?.Invoke();
            return true;
        }

        /// <summary>Odblokowuje narzędzie poza harmonogramem.</summary>
        public void UnlockTool(ToolType tool) => tools.Unlock(tool);

        #region Debug

        [ContextMenu("Debug/Spawn house")]
        private void DebugSpawnHouse() => SpawnHouse();

        [ContextMenu("Debug/Unlock all tools")]
        private void DebugUnlockAllTools()
        {
            foreach (ToolType tool in Enum.GetValues(typeof(ToolType)))
                UnlockTool(tool);
        }

        #endregion
    }
}
