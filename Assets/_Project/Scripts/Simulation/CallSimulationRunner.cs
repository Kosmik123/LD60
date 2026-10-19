using Bipolar;
using UnityEngine;

namespace SpaghettiWires.Simulation
{
    /// <summary>Uruchamia <see cref="CallSimulation"/> na scenie: tworzy ją przy starcie i podaje jej czas gry.</summary>
    public sealed class CallSimulationRunner : MonoBehaviour
    {
        [SerializeField] private Serialized<IPhoneNetworkSource> network;
        [SerializeField] private CallSettings settings = new CallSettings();
        [Tooltip("Czy rozmowy pojawiają się same od startu gry.")]
        [SerializeField] private bool generatorEnabled = true;
        [Tooltip("Ziarno losowania rozmów. 0 = inne przy każdym uruchomieniu.")]
        [SerializeField] private int seed;

        private CallSimulation _simulation;

        /// <summary>Symulacja. Tworzona przy pierwszym użyciu, więc jest dostępna już w Start innych komponentów.</summary>
        public CallSimulation Simulation => _simulation ??= CreateSimulation();

        public IPhoneNetworkSource NetworkSource
        {
            get => network.Value;
            set => network.Value = value;
        }

        private CallSimulation CreateSimulation()
        {
            var random = seed == 0 ? new System.Random() : new System.Random(seed);
            return new CallSimulation(network.Value.Network, settings, random) { GeneratorEnabled = generatorEnabled };
        }

        private void Update() => Simulation.Tick(Time.deltaTime);
    }
}
