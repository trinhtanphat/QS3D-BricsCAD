using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarNotationRoundTripSmoke
    {
        internal static void Run()
        {
            OrdinaryNotationRemainsStable();
            LargeDiameterRoundTripsWithoutExponent();
            LargeSpacingRoundTripsWithoutExponent();
            TinySpacingRoundTripsWithoutExponent();
            ScheduleGeneratedNotationRemainsParseable();
        }

        private static void OrdinaryNotationRemainsStable()
        {
            Assert(RebarNotationParser.Parse("D16@150")[0].ToString() == "D16@150", "Ordinary spacing notation changed unexpectedly.");
            Assert(RebarNotationParser.Parse("2x3D20")[0].ToString() == "2x3D20", "Ordinary set/count notation changed unexpectedly.");
        }

        private static void LargeDiameterRoundTripsWithoutExponent()
        {
            AssertRoundTrip("D100000000000000000000", "large diameter");
        }

        private static void LargeSpacingRoundTripsWithoutExponent()
        {
            AssertRoundTrip("D16@100000000000000000000", "large spacing");
        }

        private static void TinySpacingRoundTripsWithoutExponent()
        {
            var notation = "D16@0." + new string('0', 323) + "5";
            AssertRoundTrip(notation, "tiny spacing");
        }

        private static void ScheduleGeneratedNotationRemainsParseable()
        {
            var rows = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput
                {
                    ElementId = "E1",
                    BarMark = "B1",
                    Notation = "1D100000000000000000000",
                    CuttingLengthM = 1d
                }
            });

            Assert(rows.Count == 1, "Expected one generated BBS row.");
            AssertNoExponent(rows[0].Notation, "schedule notation");
            var reparsed = RebarNotationParser.Parse(rows[0].Notation);
            Assert(reparsed.Count == 1 && reparsed[0].DiameterMm == rows[0].DiameterMm, "Generated BBS notation must parse back to the same diameter.");
        }

        private static void AssertRoundTrip(string notation, string label)
        {
            var original = RebarNotationParser.Parse(notation);
            Assert(original.Count == 1, "Expected one group for " + label + ".");
            var formatted = original[0].ToString();
            AssertNoExponent(formatted, label);

            var reparsed = RebarNotationParser.Parse(formatted);
            Assert(reparsed.Count == 1, "Round-trip group count changed for " + label + ".");
            Assert(reparsed[0].DiameterMm == original[0].DiameterMm, "Round-trip diameter changed for " + label + ".");
            Assert(reparsed[0].SpacingMm == original[0].SpacingMm, "Round-trip spacing changed for " + label + ".");
        }

        private static void AssertNoExponent(string notation, string label)
        {
            Assert(notation.IndexOf('E') < 0 && notation.IndexOf('e') < 0, label + " must remain inside the decimal-only parser grammar.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
