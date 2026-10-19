using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Networking;
using SpaghettiWires.Simulation;

namespace SpaghettiWires.Tests.Simulation
{
    public class CallSimulationTests
    {
        private PhoneNetwork _network;
        private CallSimulation _simulation;
        private readonly List<Call> _connected = new List<Call>();
        private readonly List<Call> _completed = new List<Call>();
        private readonly List<Call> _dropped = new List<Call>();
        private readonly List<(NodeId from, NodeId to, CallOutcome outcome)> _failed = new List<(NodeId, NodeId, CallOutcome)>();

        // Rozmowa trwa dokładnie 5 s, generator wyłączony, chyba że test go włączy.
        private void Create(CallSettings settings = null, bool generator = false)
        {
            _network = new PhoneNetwork();
            _simulation = new CallSimulation(_network, settings ?? new CallSettings(0f, 0f, 5f, 5f), new Random(1))
            {
                GeneratorEnabled = generator,
            };
            _simulation.CallConnected += _connected.Add;
            _simulation.CallCompleted += _completed.Add;
            _simulation.CallDropped += _dropped.Add;
            _simulation.CallFailed += (from, to, outcome) => _failed.Add((from, to, outcome));
        }

        [SetUp]
        public void SetUp()
        {
            _connected.Clear();
            _completed.Clear();
            _dropped.Clear();
            _failed.Clear();
            Create();
        }

        private NodeId House() => _network.Structure.AddNode(NodeKind.House).Id;
        private NodeId Pole() => _network.Structure.AddNode(NodeKind.Pole).Id;
        private CableId Cable(NodeId a, NodeId b) => _network.Structure.AddCable(a, b, 1, 4f).Id;

        // Domy podłączone do wspólnego słupa, więc każdy może dzwonić do każdego.
        private NodeId[] HousesOnPole(int count)
        {
            var pole = Pole();
            var houses = new NodeId[count];
            for (int i = 0; i < count; i++)
            {
                houses[i] = House();
                Cable(houses[i], pole);
            }
            return houses;
        }

        [Test]
        public void TryCall_WithRoute_ConnectsAndEndsAfterDuration()
        {
            var houses = HousesOnPole(2);

            Assert.AreEqual(CallOutcome.Connected, _simulation.TryCall(houses[0], houses[1]));
            Assert.AreEqual(1, _connected.Count);
            Assert.AreEqual(1, _network.Traffic.Calls.Count);

            _simulation.Tick(4.9f);
            Assert.IsEmpty(_completed, "The call is still in progress");

            _simulation.Tick(0.2f);
            Assert.AreEqual(1, _completed.Count);
            Assert.AreSame(_connected[0], _completed[0]);
            Assert.IsEmpty(_network.Traffic.Calls, "Ending the call released the route");
        }

        [Test]
        public void TryCall_WithoutRoute_FailsWithNoRoute()
        {
            NodeId a = House(), b = House();

            Assert.AreEqual(CallOutcome.NoRoute, _simulation.TryCall(a, b));
            CollectionAssert.AreEqual(new[] { (a, b, CallOutcome.NoRoute) }, _failed);
            Assert.IsEmpty(_connected);
        }

        [Test]
        public void TryCall_ToHouseThatIsTalking_FailsWithCalleeBusy()
        {
            var houses = HousesOnPole(3);
            _simulation.TryCall(houses[0], houses[1]);

            Assert.AreEqual(CallOutcome.CalleeBusy, _simulation.TryCall(houses[2], houses[1]));
        }

        [Test]
        public void TryCall_FromHouseThatIsTalking_FailsWithCallerBusy()
        {
            var houses = HousesOnPole(3);
            _simulation.TryCall(houses[0], houses[1]);

            Assert.AreEqual(CallOutcome.CallerBusy, _simulation.TryCall(houses[0], houses[2]));
        }

        [Test]
        public void RemovingCableDuringCall_DropsItAndItNeverCompletes()
        {
            NodeId a = House(), b = House();
            var cable = Cable(a, b);
            _simulation.TryCall(a, b);

            _network.Structure.RemoveCable(cable);
            _simulation.Tick(10f);

            Assert.AreEqual(1, _dropped.Count);
            Assert.IsEmpty(_completed);
        }

        [Test]
        public void CallRandomHouseFrom_NeverCallsItself()
        {
            var houses = HousesOnPole(2);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(CallOutcome.Connected, _simulation.CallRandomHouseFrom(houses[0]));
                Assert.AreEqual(houses[1], _connected[_connected.Count - 1].To);
                _simulation.Tick(5f);
            }
        }

        [Test]
        public void Generator_Disabled_PlacesNoCalls()
        {
            Create(new CallSettings(600f, 0f, 5f, 5f), generator: false);
            HousesOnPole(4);

            _simulation.Tick(10f);

            Assert.IsEmpty(_connected);
            Assert.IsEmpty(_failed);
        }

        [Test]
        public void Generator_PlacesCallsAtConfiguredRate()
        {
            // 4 niepodłączone domy, każdy 6 rozmów na minutę: przez minutę 24 próby (każda kończy się „brak połączenia”).
            Create(new CallSettings(6f, 0f, 5f, 5f), generator: true);
            for (int i = 0; i < 4; i++)
                House();

            for (int i = 0; i < 600; i++)
                _simulation.Tick(0.1f);

            Assert.That(_failed.Count, Is.InRange(23, 24));
            Assert.IsTrue(_failed.TrueForAll(attempt => attempt.outcome == CallOutcome.NoRoute));
        }

        [Test]
        public void Generator_OnlyHousesWithFreeLinesStartCalls()
        {
            Create(new CallSettings(600f, 0f, 100f, 100f), generator: true);
            HousesOnPole(2);

            _simulation.Tick(1f);

            Assert.AreEqual(1, _connected.Count, "The second house is already talking, so nobody else can call");
            Assert.IsEmpty(_failed);
        }

        [Test]
        public void Settings_CallRateGrowsWithTime()
        {
            var settings = new CallSettings(1f, 0.5f, 5f, 5f);

            Assert.AreEqual(1f, settings.CallsPerHousePerMinute(0f), 1e-5f);
            Assert.AreEqual(2f, settings.CallsPerHousePerMinute(120f), 1e-5f);
        }

        [Test]
        public void CallRandomHouseFrom_WithoutOtherHouses_Throws()
        {
            var house = House();

            Assert.Throws<InvalidOperationException>(() => _simulation.CallRandomHouseFrom(house));
        }
    }
}
