using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqWorkspaceTextIntegritySmoke
    {
        internal static void Run()
        {
            DescriptionFailsClosedOnMalformedUtf16();
            TradeCodeFailsClosedOnMalformedUtf16();
            ExistingCanonicalRulesRemainClosed();
            ValidSupplementaryUnicodeIsPreserved();
        }

        private static void DescriptionFailsClosedOnMalformedUtf16()
        {
            AssertArgument(
                "description",
                () => Item("Description" + LoneHighSurrogate(), "Trade"),
                "malformed UTF-16");
            AssertArgument(
                "description",
                () => Item(LoneLowSurrogate() + "Description", "Trade"),
                "malformed UTF-16");
        }

        private static void TradeCodeFailsClosedOnMalformedUtf16()
        {
            AssertArgument(
                "tradeCode",
                () => Item("Description", "Trade" + LoneHighSurrogate()),
                "malformed UTF-16");
            AssertArgument(
                "tradeCode",
                () => Item("Description", LoneLowSurrogate() + "Trade"),
                "malformed UTF-16");
        }

        private static void ExistingCanonicalRulesRemainClosed()
        {
            AssertArgument("description", () => Item(" Leading", "Trade"), "surrounding whitespace");
            AssertArgument("description", () => Item("Bad\tControl", "Trade"), "control characters");
            AssertArgument("tradeCode", () => Item("Description", "Trailing "), "surrounding whitespace");
            AssertArgument("tradeCode", () => Item("Description", "Bad\rControl"), "control characters");
        }

        private static void ValidSupplementaryUnicodeIsPreserved()
        {
            const string description = "Cốt thép 😀 梁 grade 500";
            const string tradeCode = "Kết cấu 😀";

            var item = Item(description, tradeCode);

            Equal(description, item.Description, "TBQ description must preserve valid supplementary Unicode exactly.");
            Equal(tradeCode, item.TradeCode, "TBQ trade code must preserve valid supplementary Unicode exactly.");
        }

        private static TbqBillItem Item(string description, string tradeCode) =>
            new TbqBillItem("TBQ-TEXT", description, "m2", tradeCode, 1m, 1m);

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
                Equal(parameterName, ex.ParamName, "TBQ text rejection must identify the exact parameter.");
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "TBQ text rejection did not report the expected invariant. Expected fragment='" +
                        expectedMessage + "', actual='" + ex.Message + "'.");
                return;
            }

            throw new InvalidOperationException("TBQ canonical-text boundary accepted malformed or non-canonical input.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class TbqWorkspaceTextIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TbqWorkspaceTextIntegritySmoke.Run();
        }
    }
}
