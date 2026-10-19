using NUnit.Framework;
using SpaghettiWires.UI;

namespace SpaghettiWires.Tests
{
    public class HudFormatTests
    {
        [TestCase(0f, "00:00")]
        [TestCase(59.9f, "00:59")]
        [TestCase(61f, "01:01")]
        [TestCase(3599f, "59:59")]
        [TestCase(3661f, "1:01:01")]
        [TestCase(-5f, "00:00")]
        public void Time_FormatsMinutesAndSeconds(float seconds, string expected)
        {
            Assert.AreEqual(expected, HudFormat.Time(seconds));
        }

        [TestCase(0, "$0")]
        [TestCase(950, "$950")]
        [TestCase(1250, "$1,250")]
        [TestCase(1234567, "$1,234,567")]
        [TestCase(-1500, "-$1,500")]
        public void Money_GroupsThousandsWithCommas(int amount, string expected)
        {
            Assert.AreEqual(expected, HudFormat.Money(amount));
        }

        [TestCase(5f, "$5/m")]
        [TestCase(3.5f, "$3.5/m")]
        [TestCase(14f, "$14/m")]
        public void MoneyPerMeter_DropsTrailingZeros(float amount, string expected)
        {
            Assert.AreEqual(expected, HudFormat.MoneyPerMeter(amount));
        }

        [TestCase(1, "1 pair")]
        [TestCase(4, "4 pairs")]
        [TestCase(16, "16 pairs")]
        public void Pairs_UsesEnglishPlural(int count, string expected)
        {
            Assert.AreEqual(expected, HudFormat.Pairs(count));
        }

        [TestCase(1f, "100%")]
        [TestCase(0.806f, "81%")]
        [TestCase(0f, "0%")]
        [TestCase(1.5f, "100%")]
        [TestCase(-0.2f, "0%")]
        public void Percent_IsClampedAndRounded(float normalized, string expected)
        {
            Assert.AreEqual(expected, HudFormat.Percent(normalized));
        }
    }
}
