using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineOpeningProjectionOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var input = new PolylineOpeningCutInput
            {
                Centerline = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(8e307d, 8e307d)
                },
                OpeningCenter = new Point2(1.3e308d, 1.3e308d),
                HostThicknessM = 1d,
                HostHeightM = 3d,
                OpeningWidthM = 1d,
                OpeningHeightM = 1d,
                SillHeightM = 0d,
                ClearanceM = 0d,
                MaximumCenterlineOffsetM = 1e308d
            };

            try
            {
                PolylineOpeningCutPlanner.Plan(input);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("corner/junction", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception("Expected the beyond-end opening to reach the existing polyline corner/junction policy after finite endpoint clamping, got: " + ex.Message);
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf("cannot be represented", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception("Expected the beyond-end opening to fail closed through the opening-span precision guard, got: " + ex.Message);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new Exception("Finite beyond-end opening projection must not fail through raw dot-product overflow.", ex);
            }

            throw new Exception("Expected an opening projected to the host endpoint to be rejected by the existing polyline corner/junction policy or the stronger opening-span precision guard.");
        }
    }
}
