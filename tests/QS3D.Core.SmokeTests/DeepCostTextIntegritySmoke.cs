using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class DeepCostTextIntegritySmoke
    {
        internal static void Run()
        {
            TradeCodeFailsClosed();
            TradeFallbacksStayCompatible();
            BqLibraryTextFailsClosed();
            ValidUnicodeAndGroupingArePreserved();
        }

        private static void TradeCodeFailsClosed()
        {
            AssertArgument("tradeCode", () => new TradeCostItem("T1", " MEP", 1m), "surrounding whitespace");
            AssertArgument("tradeCode", () => new TradeCostItem("T2", "MEP ", 1m), "surrounding whitespace");
            AssertArgument("tradeCode", () => new TradeCostItem("T3", "MEP\tCoord", 1m), "control characters");
            AssertArgument("tradeCode", () => new TradeCostItem("T4", "MEP" + LoneHighSurrogate(), 1m), "malformed UTF-16");
            AssertArgument("tradeCode", () => new TradeCostItem("T5", LoneLowSurrogate() + "MEP", 1m), "malformed UTF-16");
        }

        private static void TradeFallbacksStayCompatible()
        {
            Equal("Unclassified", new TradeCostItem("F1", null, 1m).TradeCode,
                "Null trade code must preserve the Unclassified compatibility fallback.");
            Equal("Unclassified", new TradeCostItem("F2", string.Empty, 1m).TradeCode,
                "Empty trade code must preserve the Unclassified compatibility fallback.");
            Equal("Unclassified", new TradeCostItem("F3", " \t ", 1m).TradeCode,
                "Whitespace-only trade code must preserve the Unclassified compatibility fallback.");
        }

        private static void BqLibraryTextFailsClosed()
        {
            AssertArgument("description", () => Entry(" Leading", "Trade/Concrete"), "surrounding whitespace");
            AssertArgument("description", () => Entry("Trailing ", "Trade/Concrete"), "surrounding whitespace");
            AssertArgument("description", () => Entry("Bad\nDescription", "Trade/Concrete"), "control characters");
            AssertArgument("description", () => Entry("Bad" + LoneHighSurrogate(), "Trade/Concrete"), "malformed UTF-16");
            AssertArgument("description", () => Entry(LoneLowSurrogate() + "Bad", "Trade/Concrete"), "malformed UTF-16");

            AssertArgument("categoryPath", () => Entry("Concrete", " Trade/Concrete"), "surrounding whitespace");
            AssertArgument("categoryPath", () => Entry("Concrete", "Trade/Concrete "), "surrounding whitespace");
            AssertArgument("categoryPath", () => Entry("Concrete", "Trade\tConcrete"), "control characters");
            AssertArgument("categoryPath", () => Entry("Concrete", "Trade/" + LoneHighSurrogate()), "malformed UTF-16");
            AssertArgument("categoryPath", () => Entry("Concrete", LoneLowSurrogate() + "Trade"), "malformed UTF-16");
        }

        private static void ValidUnicodeAndGroupingArePreserved()
        {
            const string trade = "Cơ điện 😀 区 A";
            const string description = "Bê tông 😀 梁 grade 40";
            const string categoryPath = "Kết cấu / 😀 Zone A";

            var item = new TradeCostItem("U1", trade, 1m);
            var entry = Entry(description, categoryPath);

            Equal(trade, item.TradeCode,
                "Valid supplementary-plane Unicode and interior spaces must be preserved in trade code.");
            Equal(description, entry.Description,
                "Valid supplementary-plane Unicode and interior spaces must be preserved in BQ description.");
            Equal(categoryPath, entry.CategoryPath,
                "Valid supplementary-plane Unicode and interior spaces must be preserved in BQ category path.");

            var rows = new TradeCostAnalysisService().Analyze(
                new[]
                {
                    new TradeCostItem("G1", "MEP", 1m),
                    new TradeCostItem("G2", "mep", 2m)
                },
                1m);

            Equal(1, rows.Count, "Case-insensitive trade grouping must remain unchanged.");
            Equal("MEP", rows[0].TradeCode, "Deterministic trade casing selection must remain unchanged.");
            Equal(2, rows[0].ItemCount, "Trade grouping item count must remain unchanged.");
            Equal(3m, rows[0].TotalCost, "Trade grouping total must remain unchanged.");
        }

        private static BqLibraryEntry Entry(string description, string categoryPath) =>
            new BqLibraryEntry("BQ-TEXT", description, "m2", categoryPath, 1m);

        private static string LoneHighSurrogate() => new string(new[] { '\uD800' });
        private static string LoneLowSurrogate() => new string(new[] { '\uDC00' });

        private static void AssertArgument(string parameterName, Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                Equal(parameterName, ex.ParamName,
                    "Deep cost text rejection must identify the exact parameter.");
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) < 0)
                {
                    throw new InvalidOperationException(
                        "Deep cost text rejection did not report the expected invariant. Expected fragment='" +
                        expectedMessage + "', actual='" + ex.Message + "'.");
                }
                return;
            }

            throw new InvalidOperationException(
                "Deep cost text boundary accepted non-canonical or malformed input.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
            }
        }
    }

    internal static class DeepCostTextIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DeepCostTextIntegritySmoke.Run();
        }
    }
}
