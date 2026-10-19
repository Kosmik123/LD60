namespace SpaghettiWires.Tools
{
    /// <summary>Jedno narzędzie gracza. Aktywne jest najwyżej jedno naraz.</summary>
    public interface ITool
    {
        void Enter();

        /// <summary>Wołane co klatkę, gdy narzędzie jest aktywne.</summary>
        void Tick();

        /// <summary>Przerywa trwającą akcję (np. przeciąganie kabla), ale zostawia narzędzie aktywne.</summary>
        void Cancel();

        void Exit();
    }
}
