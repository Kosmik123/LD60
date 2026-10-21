using System;
using NUnit.Framework;
using SpaghettiWires.Networking;
using SpaghettiWires.Simulation;

namespace SpaghettiWires.Tests.Simulation
{
    public class CallPayoutsTests
    {
        private PhoneNetwork _network;
        private CallSimulation _simulation;
        private Economy _economy;
        private CallPayouts _payouts;
        private NodeId _a, _b, _c, _pole;
        private CableId _toA;

        [SetUp]
        public void SetUp()
        {
            _network = new PhoneNetwork();
            _a = _network.Structure.AddNode(NodeKind.House).Id;
            _b = _network.Structure.AddNode(NodeKind.House).Id;
            _c = _network.Structure.AddNode(NodeKind.House).Id;
            _pole = _network.Structure.AddNode(NodeKind.Pole).Id;
            _toA = _network.Structure.AddCable(_a, _pole, 1, 4f).Id;
            _network.Structure.AddCable(_b, _pole, 4, 4f);

            // Rozmowy trwają dokładnie 1 s, a generator jest wyłączony: dzwonimy ręcznie.
            _simulation = new CallSimulation(_network, new CallSettings(0f, 0f, 1f, 1f), new Random(1)) { GeneratorEnabled = false };
            _economy = new Economy(100, 0.5f);
            _payouts = new CallPayouts(_simulation, _economy,
                new CallPayoutSettings(moneyPerCompletedCallPerSecond: 25, reputationPerCompletedCall: 0.01f, reputationPerNoRoute: -0.02f,
                    reputationPerCalleeBusy: -0.03f, reputationPerCallerBusy: -0.05f, reputationPerDroppedCall: -0.1f));
        }

        [Test]
        public void ConnectedCall_PaysOnlyWhenCompleted()
        {
            Assert.AreEqual(CallOutcome.Connected, _simulation.TryCall(_a, _b));
            Assert.AreEqual(100, _economy.Money);

            _simulation.Tick(1f);

            Assert.AreEqual(125, _economy.Money);
            Assert.AreEqual(0.51f, _economy.Reputation, 1e-5f);
        }

        [Test]
        public void NoRoute_LowersReputation()
        {
            Assert.AreEqual(CallOutcome.NoRoute, _simulation.TryCall(_a, _c));

            Assert.AreEqual(0.48f, _economy.Reputation, 1e-5f);
            Assert.AreEqual(100, _economy.Money);
        }

        [Test]
        public void BusyLines_LowerReputation()
        {
            _simulation.TryCall(_a, _b);

            Assert.AreEqual(CallOutcome.CalleeBusy, _simulation.TryCall(_c, _b));
            Assert.AreEqual(0.47f, _economy.Reputation, 1e-5f);

            Assert.AreEqual(CallOutcome.CallerBusy, _simulation.TryCall(_a, _c));
            Assert.AreEqual(0.42f, _economy.Reputation, 1e-5f);
        }

        [Test]
        public void DroppedCall_LowersReputationAndPaysNothing()
        {
            _simulation.TryCall(_a, _b);

            _network.Structure.RemoveCable(_toA);
            _simulation.Tick(1f);

            Assert.AreEqual(0.4f, _economy.Reputation, 1e-5f);
            Assert.AreEqual(100, _economy.Money);
        }

        [Test]
        public void ManyFailures_LoseTheGame()
        {
            for (int i = 0; i < 30; i++)
                _simulation.TryCall(_a, _c);

            Assert.AreEqual(LossReason.Reputation, _economy.LossReason);
        }

        [Test]
        public void Dispose_StopsPayouts()
        {
            _payouts.Dispose();

            _simulation.TryCall(_a, _c);

            Assert.AreEqual(0.5f, _economy.Reputation);
        }

        [Test]
        public void FailureReputation_RejectsConnectedOutcome()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CallPayoutSettings().ReputationPerFailure(CallOutcome.Connected));
        }
    }
}
