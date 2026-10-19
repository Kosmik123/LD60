using SpaghettiWires.Networking;
using SpaghettiWires.Simulation;
using SpaghettiWires.Tools;
using SpaghettiWires.Views;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaghettiWires.DebugTools
{
    /// <summary>
    /// TYLKO DO TESTÓW: wymuszanie rozmów z klawiatury. Nie jest częścią gry, działa tylko w edytorze
    /// i w buildach deweloperskich.
    /// <list type="bullet">
    /// <item><b>C</b> nad domem: wybierz dzwoniącego, potem <b>C</b> nad drugim domem: dzwoń.</item>
    /// <item><b>Shift+C</b> nad domem: dzwoń z niego do losowego domu.</item>
    /// <item><b>G</b>: włącz lub wyłącz automatyczny generator rozmów.</item>
    /// </list>
    /// </summary>
    public sealed class CallDebugInput : MonoBehaviour
    {
        private static readonly Color CallerHighlight = new Color(0.3f, 0.8f, 1f);

        [SerializeField] private ScenePointer pointer;
        [SerializeField] private CallSimulationRunner simulation;
        [SerializeField] private ToolController tools;

        private NodeView _caller;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.gKey.wasPressedThisFrame)
                ToggleGenerator();

            if (!keyboard.cKey.wasPressedThisFrame)
                return;

            var house = pointer.HoveredNode;
            if (house == null || house.Kind != NodeVisualKind.House)
            {
                tools.PostMessage("[test] Wskaż dom kursorem.");
                return;
            }

            if (keyboard.shiftKey.isPressed)
                Report(simulation.Simulation.CallRandomHouseFrom(new NodeId(house.Id)), house, null);
            else if (_caller == null)
                SelectCaller(house);
            else if (_caller == house)
                ClearCaller();
            else
                CallSelected(house);
        }
#endif

        private void ToggleGenerator()
        {
            var sim = simulation.Simulation;
            sim.GeneratorEnabled = !sim.GeneratorEnabled;
            tools.PostMessage(sim.GeneratorEnabled ? "[test] Generator rozmów włączony." : "[test] Generator rozmów wyłączony.");
        }

        private void SelectCaller(NodeView house)
        {
            _caller = house;
            _caller.SetHighlight(CallerHighlight, 0.6f);
            tools.PostMessage($"[test] Dzwoni dom #{house.Id}. Wskaż odbiorcę i naciśnij C.");
        }

        private void CallSelected(NodeView callee)
        {
            var caller = _caller;
            ClearCaller();
            Report(simulation.Simulation.TryCall(new NodeId(caller.Id), new NodeId(callee.Id)), caller, callee);
        }

        private void ClearCaller()
        {
            if (_caller != null)
                _caller.ClearHighlight();
            _caller = null;
        }

        private void Report(CallOutcome outcome, NodeView caller, NodeView callee)
        {
            string target = callee != null ? $"#{callee.Id}" : "losowy dom";
            string result = outcome switch
            {
                CallOutcome.Connected => "połączono",
                CallOutcome.CallerBusy => "linia dzwoniącego zajęta",
                CallOutcome.CalleeBusy => "zajęte",
                _ => "brak połączenia",
            };
            tools.PostMessage($"[test] #{caller.Id} → {target}: {result}.");
        }
    }
}
