using SpaghettiWires.Networking;

namespace SpaghettiWires.Simulation
{
    /// <summary>
    /// Kto prowadzi sieć gry. Teraz tymczasowy sandbox, docelowo reguły budowy (PLAN etap 2).
    /// Symulacja dostaje sieć przez ten interfejs, więc nie zależy od tego, kto ją buduje.
    /// </summary>
    public interface IPhoneNetworkSource
    {
        PhoneNetwork Network { get; }
    }
}
