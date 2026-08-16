using System;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepTbqDecimalUnderflowSmoke
    {
        internal static void Run()
        {
            var groups = new MepQuantityService().Aggregate(new[]
            {
                new MepElement(
                    "tiny-pipe",
                    MepElementKind.Pipe,
                    "Cold Water",
                    "DN20",
                    "Level 1",
                    count: 0,
                    lengthM: double.Epsilon)
            });

            try
            {
                new MepTbqProjectionService().BuildReport(groups);
            }
            catch (OverflowException ex)
            {
                if (!string.Equals(
                    "MEP report length cannot be represented by TBQ decimal arithmetic.",
                    ex.Message,
                    StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("MEP/TBQ decimal underflow must preserve the projection overflow contract.", ex);
                }
                return;
            }

            throw new InvalidOperationException("MEP/TBQ must reject a nonzero quantity that underflows to decimal zero.");
        }
    }
}
