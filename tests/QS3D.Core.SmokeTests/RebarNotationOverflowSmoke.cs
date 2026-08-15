using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarNotationOverflowSmoke
    {
        internal static void Run()
        {
            var valid = RebarNotationParser.Parse("D16@200");
            if (valid.Count != 1 || Math.Abs(valid[0].DiameterMm - 16d) > 1e-12 ||
                !valid[0].SpacingMm.HasValue || Math.Abs(valid[0].SpacingMm.GetValueOrDefault() - 200d) > 1e-12)
            {
                throw new InvalidOperationException("Ordinary rebar notation parsing changed unexpectedly.");
            }

            ExpectFormatException("D" + new string('9', 512) + "@200");
            ExpectFormatException("D16@" + new string('9', 512));
        }

        private static void ExpectFormatException(string notation)
        {
            try
            {
                RebarNotationParser.Parse(notation);
            }
            catch (FormatException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Out-of-range rebar notation must fail with FormatException, but received " +
                    ex.GetType().FullName + ".",
                    ex);
            }

            throw new InvalidOperationException(
                "Out-of-range rebar notation must fail with FormatException.");
        }
    }
}
