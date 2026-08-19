using System;
using QS3D.Core.Domain;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionRuleIdentitySmoke
    {
        internal static void Run()
        {
            var canonical = new RecognitionRule("Beam.Rule", ElementCategory.Beam);
            if (!string.Equals(canonical.Id, "Beam.Rule", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical recognition rule id changed unexpectedly.");

            foreach (var padded in new[]
            {
                " beam",
                "beam ",
                "\tbeam",
                "beam\t",
                "\rbeam",
                "beam\r",
                "\nbeam",
                "beam\n",
                " beam "
            })
            {
                Throws<ArgumentException>(
                    () => new RecognitionRule(padded, ElementCategory.Beam),
                    "padded recognition rule id '" + Escape(padded) + "'");
            }

            Throws<ArgumentException>(
                () => new RecognitionRule("be\tam", ElementCategory.Beam),
                "embedded control character remains rejected");

            Throws<ArgumentException>(
                () => new RecognitionEngine(new[]
                {
                    new RecognitionRule("beam", ElementCategory.Beam),
                    new RecognitionRule("BEAM", ElementCategory.Beam)
                }),
                "case-insensitive duplicate rule id remains rejected");
        }

        private static string Escape(string value) =>
            value.Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
