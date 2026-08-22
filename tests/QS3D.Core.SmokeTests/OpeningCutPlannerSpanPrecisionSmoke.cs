using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningCutPlannerSpanPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExpectOverflow(
                () => OpeningCutPlanner.Plan(CreateInput(
                    hostLengthM: 2e16d,
                    centerAlongHostM: 1e16d,
                    openingWidthM: 1d)),
                "Positive opening width that collapses at a large center coordinate");

            ExpectOverflow(
                () => OpeningCutPlanner.Plan(CreateInput(
                    hostLengthM: 2d,
                    centerAlongHostM: 1d,
                    openingWidthM: double.Epsilon)),
                "Positive opening width whose half-width underflows to zero");

            var ordinary = OpeningCutPlanner.Plan(CreateInput(
                hostLengthM: 10d,
                centerAlongHostM: 5d,
                openingWidthM: 2d));

            AssertEqual(ordinary.StartAlongHostM, 4d, "Ordinary opening start");
            AssertEqual(ordinary.CenterAlongHostM, 5d, "Ordinary opening center");
            AssertEqual(ordinary.EndAlongHostM, 6d, "Ordinary opening end");
            if (!(ordinary.StartAlongHostM < ordinary.CenterAlongHostM &&
                  ordinary.CenterAlongHostM < ordinary.EndAlongHostM))
            {
                throw new InvalidOperationException("Ordinary positive opening width must retain a strict start < center < end span.");
            }
        }

        private static OpeningCutInput CreateInput(double hostLengthM, double centerAlongHostM, double openingWidthM)
        {
            return new OpeningCutInput
            {
                HostLengthM = hostLengthM,
                HostThicknessM = 0.2d,
                HostHeightM = 3d,
                OpeningWidthM = openingWidthM,
                OpeningHeightM = 2d,
                SillHeightM = 0.5d,
                CenterAlongHostM = centerAlongHostM,
                ClearanceM = 0.01d
            };
        }

        private static void AssertEqual(double actual, double expected, string label)
        {
            if (actual != expected)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + ", actual " + actual + ".");
        }

        private static void ExpectOverflow(Action action, string label)
        {
            try
            {
                action();
            }
            catch (OverflowException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(label + " should fail closed with OverflowException, but got " + ex.GetType().Name + ".", ex);
            }

            throw new InvalidOperationException(label + " should fail closed instead of returning a collapsed opening span.");
        }
    }
}
