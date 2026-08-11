using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarNotationWhitespaceRegressionSmoke
    {
        public static void Run()
        {
            EmbeddedQuantityWhitespaceIsRejected();
            EmbeddedDiameterWhitespaceIsRejected();
            LegitimateTokenWhitespaceStillParses();
            CompoundAndMultipliedWhitespaceStillParses();
        }

        private static void EmbeddedQuantityWhitespaceIsRejected()
        {
            Throws<FormatException>(() => RebarNotationParser.Parse("2 0D16"));
        }

        private static void EmbeddedDiameterWhitespaceIsRejected()
        {
            Throws<FormatException>(() => RebarNotationParser.Parse("D1 6@150"));
        }

        private static void LegitimateTokenWhitespaceStillParses()
        {
            var groups = RebarNotationParser.Parse(" 4 Ø20 ");
            if (groups.Count != 1 || groups[0].Quantity != 4 || Math.Abs(groups[0].DiameterMm - 20d) > 1e-12d)
                throw new InvalidOperationException("Legitimate count/diameter whitespace no longer parses correctly.");

            groups = RebarNotationParser.Parse(" D8 @ 150 ");
            if (groups.Count != 1 || Math.Abs(groups[0].DiameterMm - 8d) > 1e-12d || !groups[0].SpacingMm.HasValue || Math.Abs(groups[0].SpacingMm.Value - 150d) > 1e-12d)
                throw new InvalidOperationException("Legitimate diameter/spacing whitespace no longer parses correctly.");
        }

        private static void CompoundAndMultipliedWhitespaceStillParses()
        {
            var groups = RebarNotationParser.Parse(" 3 x 4 Ø16 + 2 D20 ");
            if (groups.Count != 2)
                throw new InvalidOperationException("Whitespace around compound separators changed the parsed group count.");
            if (groups[0].Quantity != 12 || groups[0].Sets != 3 || groups[0].BarsPerSet != 4 || Math.Abs(groups[0].DiameterMm - 16d) > 1e-12d)
                throw new InvalidOperationException("Whitespace around multiplied notation changed its parsed values.");
            if (groups[1].Quantity != 2 || Math.Abs(groups[1].DiameterMm - 20d) > 1e-12d)
                throw new InvalidOperationException("Whitespace around compound notation changed its parsed values.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class RebarNotationWhitespaceSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RebarNotationWhitespaceRegressionSmoke.Run();
        }
    }
}
