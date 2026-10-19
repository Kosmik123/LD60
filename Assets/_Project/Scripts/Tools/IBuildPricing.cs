using UnityEngine;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Ceny budowy do pokazania graczowi przed zakupem. Implementuje ją obiekt akcji gry,
    /// jeśli budowa jest płatna (np. <c>PaidBuildActions</c>). Gdy go nie ma, UI cen nie pokazuje.
    /// </summary>
    public interface IBuildPricing
    {
        int PolePrice { get; }
        float GetCablePricePerMeter(int pairs);

        /// <summary>Cena kabla o danej długości w metrach (odległość między węzłami, bez zwisu).</summary>
        int GetCablePrice(float lengthMeters, int pairs);

        bool CanAfford(int price);
    }

    /// <summary>Cena pokazywana nad podglądem kabla. Domyślna wartość = nic nie pokazujemy.</summary>
    public readonly struct PricePreview
    {
        public bool IsVisible { get; }
        public Vector3 Position { get; }
        public int Price { get; }
        public bool IsAffordable { get; }

        public PricePreview(Vector3 position, int price, bool isAffordable)
        {
            IsVisible = true;
            Position = position;
            Price = price;
            IsAffordable = isAffordable;
        }
    }
}
