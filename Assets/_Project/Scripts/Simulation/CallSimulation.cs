using System;
using System.Collections.Generic;
using SpaghettiWires.Networking;

namespace SpaghettiWires.Simulation
{
    /// <summary>
    /// Rozmowy w czasie (TECH 6): kto, kiedy i do kogo dzwoni i jak długo rozmawia.
    /// Rozpoczyna i kończy rozmowy przez warstwę ruchu sieci. Czysty C#, czas podaje <see cref="Tick"/>.
    /// Wyniki rozmów (połączono, zajęte, brak drogi, zerwana) wychodzą zdarzeniami, z których
    /// korzystają widoki, a docelowo ekonomia i zadowolenie (PLAN etap 6).
    /// </summary>
    public sealed class CallSimulation
    {
        private readonly PhoneNetwork _network;
        private readonly CallSettings _settings;
        private readonly Random _random;

        // Trwające rozmowy symulacji i czas, w którym się skończą.
        private readonly Dictionary<CallId, float> _endTimes = new Dictionary<CallId, float>();
        private readonly List<CallId> _finished = new List<CallId>();
        private readonly List<NodeId> _houses = new List<NodeId>();
        private float _pendingCalls;

        public PhoneNetwork Network => _network;

        /// <summary>Czas gry w sekundach, od którego zależy częstość rozmów.</summary>
        public float Time { get; private set; }

        /// <summary>Czy rozmowy pojawiają się same. Wyłączony generator nadal kończy trwające rozmowy.</summary>
        public bool GeneratorEnabled { get; set; } = true;

        public event Action<Call> CallConnected;
        public event Action<NodeId, NodeId, CallOutcome> CallFailed;

        /// <summary>Rozmowa skończyła się normalnie.</summary>
        public event Action<Call> CallCompleted;

        /// <summary>Rozmowa zerwana przez uszkodzenie lub usunięcie kabla.</summary>
        public event Action<Call> CallDropped;

        public CallSimulation(PhoneNetwork network, CallSettings settings, Random random)
        {
            _network = network;
            _settings = settings;
            _random = random;
            _network.Traffic.CallDropped += OnCallDropped;
        }

        public void Tick(float deltaTime)
        {
            Time += deltaTime;
            EndFinishedCalls();
            if (GeneratorEnabled)
                GenerateCalls(deltaTime);
        }

        /// <summary>Próba połączenia <paramref name="from"/> z <paramref name="to"/>, np. wymuszona do testów.</summary>
        public CallOutcome TryCall(NodeId from, NodeId to)
        {
            var traffic = _network.Traffic;

            // Czas trwania znany przed połączeniem, bo rozmowa przechowuje go od początku (np. do zapłaty).
            float duration = Lerp(_settings.MinDuration, _settings.MaxDuration, (float)_random.NextDouble());

            CallOutcome outcome;
            if (!HasFreeLine(from))
                outcome = CallOutcome.CallerBusy;
            else if (!HasFreeLine(to))
                outcome = CallOutcome.CalleeBusy;
            else if (!traffic.TryStartCall(from, to, duration, out var call))
                outcome = CallOutcome.NoRoute;
            else
            {
                _endTimes.Add(call.Id, Time + duration);
                CallConnected?.Invoke(call);
                return CallOutcome.Connected;
            }

            CallFailed?.Invoke(from, to, outcome);
            return outcome;
        }

        /// <summary>Rozmowa z domu <paramref name="from"/> do losowego innego domu.</summary>
        public CallOutcome CallRandomHouseFrom(NodeId from)
        {
            CollectHouses();
            _houses.Remove(from);
            if (_houses.Count == 0)
                throw new InvalidOperationException("There is no other house to call.");

            return TryCall(from, _houses[_random.Next(_houses.Count)]);
        }

        private void GenerateCalls(float deltaTime)
        {
            CollectHouses();
            if (_houses.Count < 2)
                return;

            // Ułamki rozmów się sumują, więc średnia częstość zgadza się przy każdej długości klatki.
            _pendingCalls += deltaTime / 60f * _settings.CallsPerHousePerMinute(Time) * _houses.Count;
            while (_pendingCalls >= 1f)
            {
                _pendingCalls -= 1f;
                PlaceRandomCall();
            }
        }

        private void PlaceRandomCall()
        {
            // Dzwoni tylko ktoś, kto ma wolną linię: zajęty dom po prostu nie podnosi słuchawki.
            CollectHouses();
            _houses.RemoveAll(house => !HasFreeLine(house));
            if (_houses.Count == 0)
                return;

            CallRandomHouseFrom(_houses[_random.Next(_houses.Count)]);
        }

        private void EndFinishedCalls()
        {
            _finished.Clear();
            foreach (var (callId, endTime) in _endTimes)
            {
                if (endTime <= Time)
                    _finished.Add(callId);
            }

            foreach (var callId in _finished)
            {
                _endTimes.Remove(callId);
                _network.Traffic.TryGetCall(callId, out var call);
                _network.Traffic.EndCall(callId);
                CallCompleted?.Invoke(call);
            }
        }

        private void OnCallDropped(Call call)
        {
            _endTimes.Remove(call.Id);
            CallDropped?.Invoke(call);
        }

        private bool HasFreeLine(NodeId nodeId) =>
            _network.Traffic.GetNodeLoad(nodeId) < Traffic.GetCallCapacity(_network.Structure.GetNode(nodeId).Kind);

        private void CollectHouses()
        {
            _houses.Clear();
            foreach (var node in _network.Structure.Nodes)
            {
                if (node.Kind == NodeKind.House)
                    _houses.Add(node.Id);
            }
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
