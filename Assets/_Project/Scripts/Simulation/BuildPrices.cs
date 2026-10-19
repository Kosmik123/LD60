using System;

namespace SpaghettiWires.Simulation
{
    /// <summary>
    /// Ceny budowy. Kabel i magistrala kosztują od długości odcinka między węzłami (bez zwisu).
    /// Magistrala płaci od żyły, więc jej cena za metr rośnie z liczbą żył.
    /// </summary>
    public sealed class BuildPrices
    {
        public int Pole { get; }
        public float CablePerMeter { get; }
        public float TrunkPerPairMeter { get; }

        public BuildPrices(int pole, float cablePerMeter, float trunkPerPairMeter)
        {
            if (pole < 0)
                throw new ArgumentOutOfRangeException(nameof(pole), pole, "Price must be non-negative.");
            if (!(cablePerMeter >= 0f))
                throw new ArgumentOutOfRangeException(nameof(cablePerMeter), cablePerMeter, "Price must be non-negative.");
            if (!(trunkPerPairMeter >= 0f))
                throw new ArgumentOutOfRangeException(nameof(trunkPerPairMeter), trunkPerPairMeter, "Price must be non-negative.");

            Pole = pole;
            CablePerMeter = cablePerMeter;
            TrunkPerPairMeter = trunkPerPairMeter;
        }

        /// <summary>Cena za metr kabla o danej liczbie żył.</summary>
        public float PerMeter(int pairs)
        {
            if (pairs < 1)
                throw new ArgumentOutOfRangeException(nameof(pairs), pairs, "Cable needs at least one pair.");
            return pairs == 1 ? CablePerMeter : pairs * TrunkPerPairMeter;
        }

        /// <summary>Cena kabla o długości <paramref name="lengthMeters"/>, zaokrąglona w górę do pełnych $.</summary>
        public int Cable(float lengthMeters, int pairs)
        {
            if (!(lengthMeters >= 0f))
                throw new ArgumentOutOfRangeException(nameof(lengthMeters), lengthMeters, "Length must be non-negative.");
            return (int)Math.Ceiling(lengthMeters * PerMeter(pairs));
        }
    }
}
