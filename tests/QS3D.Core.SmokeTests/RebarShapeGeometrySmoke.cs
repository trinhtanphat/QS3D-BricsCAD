using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarShapeGeometrySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();
        public static void Run() { Straight(); LShape(); UShape(); CustomTurns(); RejectsMissingDimensions(); RejectsLengthMismatch(); RejectsCollapsedPositiveLeg(); }
        private static void Straight() { var path = RebarShapePathBuilder.Build("00", 2.5d); Equal(2, path.Points.Count); Near(2.5d, path.Points[1].X); Near(0d, path.Points[1].Y); }
        private static void LShape() { var path = RebarShapePathBuilder.Build("11", 3d, "2;1"); Equal(3, path.Points.Count); Near(2d, path.Points[1].X); Near(0d, path.Points[1].Y); Near(2d, path.Points[2].X); Near(1d, path.Points[2].Y); }
        private static void UShape() { var path = RebarShapePathBuilder.Build("U", 4d, "1;2;1"); Equal(4, path.Points.Count); Near(0d, path.Points.Last().X); Near(2d, path.Points.Last().Y); }
        private static void CustomTurns() { var path = RebarShapePathBuilder.Build("CUSTOM", 3d, "1;1;1", "45;-45"); Equal(4, path.Points.Count); True(path.Points.All(x => !double.IsNaN(x.X) && !double.IsInfinity(x.X) && !double.IsNaN(x.Y) && !double.IsInfinity(x.Y))); }
        private static void RejectsMissingDimensions() => Throws<InvalidOperationException>(() => RebarShapePathBuilder.Build("21", 3d));
        private static void RejectsLengthMismatch() => Throws<InvalidOperationException>(() => RebarShapePathBuilder.Build("11", 3d, "1;1"));
        private static void RejectsCollapsedPositiveLeg() => Throws<OverflowException>(() => RebarShapePathBuilder.Build("CUSTOM", 1e16d, "10000000000000000;1", "0"));
        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name + "."); }
    }
}
