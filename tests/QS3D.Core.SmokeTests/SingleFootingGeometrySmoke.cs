using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class SingleFootingGeometrySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var box = new SingleFootingDimensions(1.6d, 1.6d, 1d, 1d, 1d, 0d);
            AssertClose(box.VolumeM3, 2.56d, "H2=0 box volume");
            if (box.HasTaper) throw new InvalidOperationException("H2=0 must not report a taper.");

            var tapered = new SingleFootingDimensions(1.6d, 1.6d, 1d, 1d, 1d, .3d);
            // Integral of (1.6 - .6t)^2 from t=0..1, multiplied by H2, plus lower prism.
            AssertClose(tapered.VolumeM3, 2.56d + .3d * (2.56d - .96d + .12d), "square tapered volume");
            if (!tapered.HasTaper) throw new InvalidOperationException("Reduced top with H2>0 must report a taper.");

            var oneAxis = new SingleFootingDimensions(2d, 1.5d, 2d, 1d, .5d, .4d);
            AssertClose(oneAxis.VolumeM3, 1.5d + .4d * 2d * 1.25d, "one-axis tapered volume");

            ExpectInvalid(() => new SingleFootingDimensions(0d, 1d, 1d, 1d, 1d, 0d));
            ExpectInvalid(() => new SingleFootingDimensions(1d, 1d, 1.1d, 1d, 1d, 0d));
            ExpectInvalid(() => new SingleFootingDimensions(1d, 1d, 1d, 1.1d, 1d, 0d));
            ExpectInvalid(() => new SingleFootingDimensions(1d, 1d, 1d, 1d, 1d, -.01d));
            ExpectInvalid(() => new SingleFootingDimensions(double.NaN, 1d, 1d, 1d, 1d, 0d));
        }

        private static void AssertClose(double actual, double expected, string label)
        {
            if (Math.Abs(actual - expected) > 1e-12d)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + ", actual " + actual + ".");
        }

        private static void ExpectInvalid(Action action)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new InvalidOperationException("Invalid single footing dimensions must fail closed.");
        }
    }
}