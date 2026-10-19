using System;
using System.Globalization;

namespace SpaghettiWires.UI
{
    /// <summary>Formatowanie liczb w HUD i na ekranach.</summary>
    public static class HudFormat
    {
        /// <summary>Czas gry jako „mm:ss”, a od godziny „h:mm:ss”.</summary>
        public static string Time(float seconds)
        {
            var time = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(seconds)));
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
                : $"{time.Minutes:00}:{time.Seconds:00}";
        }

        /// <summary>Kwota z odstępami co trzy cyfry, np. „1 250 $”.</summary>
        public static string Money(int amount) =>
            amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ') + " $";

        /// <summary>Cena za metr, np. „3.5 $/m”.</summary>
        public static string MoneyPerMeter(float amount) =>
            amount.ToString("0.##", CultureInfo.InvariantCulture) + " $/m";

        /// <summary>Liczba żył z odmianą, np. „1 żyła”, „4 żyły”, „9 żył”.</summary>
        public static string Pairs(int count)
        {
            int lastDigit = count % 10, lastTwo = count % 100;
            string noun = count == 1 ? "żyła"
                : lastDigit >= 2 && lastDigit <= 4 && (lastTwo < 12 || lastTwo > 14) ? "żyły"
                : "żył";
            return $"{count} {noun}";
        }

        /// <summary>Reputacja 0–1 jako procent, np. „80%”.</summary>
        public static string Percent(float normalized) =>
            $"{Math.Round(Math.Clamp(normalized, 0f, 1f) * 100f, MidpointRounding.AwayFromZero)}%";
    }
}
