using System;

namespace SpaghettiWires.Simulation
{
    /// <summary>Dlaczego gra się skończyła (GDD 3.8).</summary>
    public enum LossReason
    {
        None,
        Reputation,
        Money,
    }

    /// <summary>
    /// Pieniądze i reputacja gracza (GDD 3.8). Czysta logika: kto i za co płaci, decydują reguły i symulacja.
    /// Przegrana: reputacja spada do zera albo pieniądze spadają poniżej zera przez wydatek przymusowy.
    /// </summary>
    public sealed class Economy
    {
        public int Money { get; private set; }

        /// <summary>Reputacja od 0 (przegrana) do 1.</summary>
        public float Reputation { get; private set; }

        public LossReason LossReason { get; private set; } = LossReason.None;
        public bool IsLost => LossReason != LossReason.None;

        public event Action<int> MoneyChanged;
        public event Action<float> ReputationChanged;

        /// <summary>Wysyłane raz, przy pierwszej przegranej.</summary>
        public event Action<LossReason> Lost;

        public Economy(int startMoney, float startReputation = 1f)
        {
            if (startMoney < 0)
                throw new ArgumentOutOfRangeException(nameof(startMoney), startMoney, "Start money must be non-negative.");
            if (float.IsNaN(startReputation) || startReputation <= 0f || startReputation > 1f)
                throw new ArgumentOutOfRangeException(nameof(startReputation), startReputation, "Start reputation must be in (0, 1].");

            Money = startMoney;
            Reputation = startReputation;
        }

        public bool CanAfford(int cost) => RequireNonNegative(cost, nameof(cost)) <= Money;

        /// <summary>Zakup gracza (np. kabel, słup). Gdy nie stać gracza, nic nie zmienia i zwraca false.</summary>
        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost))
                return false;

            SetMoney(Money - cost);
            return true;
        }

        /// <summary>Wpływy, np. za udaną rozmowę.</summary>
        public void Earn(int amount) => SetMoney(Money + RequireNonNegative(amount, nameof(amount)));

        /// <summary>
        /// Wydatek przymusowy (np. utrzymanie sieci, kara). Może zejść poniżej zera, co oznacza przegraną.
        /// </summary>
        public void Charge(int amount)
        {
            SetMoney(Money - RequireNonNegative(amount, nameof(amount)));
            if (Money < 0)
                Lose(LossReason.Money);
        }

        /// <summary>Zmienia reputację o <paramref name="delta"/> (ujemna obniża), w granicach 0–1.</summary>
        public void ChangeReputation(float delta)
        {
            if (float.IsNaN(delta))
                throw new ArgumentException("Reputation change cannot be NaN.", nameof(delta));

            float value = Math.Clamp(Reputation + delta, 0f, 1f);
            if (value != Reputation)
            {
                Reputation = value;
                ReputationChanged?.Invoke(Reputation);
            }

            if (Reputation <= 0f)
                Lose(LossReason.Reputation);
        }

        private void SetMoney(int value)
        {
            if (value == Money)
                return;

            Money = value;
            MoneyChanged?.Invoke(Money);
        }

        private void Lose(LossReason reason)
        {
            if (IsLost)
                return;

            LossReason = reason;
            Lost?.Invoke(reason);
        }

        private static int RequireNonNegative(int amount, string name)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(name, amount, "Amount must be non-negative.");
            return amount;
        }
    }
}
