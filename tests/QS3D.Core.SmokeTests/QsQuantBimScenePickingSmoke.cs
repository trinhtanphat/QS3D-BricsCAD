using System;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimScenePickingSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var picker = new QuantBimStandaloneScenePicker();
            var near = TriangleAtZ(2.0);
            var far = TriangleAtZ(5.0);
            var scene = new IfcStandaloneScene(
                "sample.ifc",
                "rev-1",
                new[]
                {
                    new IfcSceneNode("FAR", "ifc-step://#20", far, false),
                    new IfcSceneNode("NEAR", "ifc-step://#10", near, false)
                },
                new IfcViewCommand[0]);

            var ray = new IfcSceneRay(0.25, 0.25, 0.0, 0.0, 0.0, 10.0);
            var hit = picker.PickNearest(scene, ray);
            if (hit == null) throw new InvalidOperationException("nearest hit exists: expected true.");
            Equal("NEAR", hit.Guid, "nearest guid");
            Equal("ifc-step://#10", hit.GeometryReference, "geometry evidence reference");
            Near(2.0, hit.Distance, "normalized-ray distance");
            Near(0.25, hit.X, "hit x");
            Near(0.25, hit.Y, "hit y");
            Near(2.0, hit.Z, "hit z");

            var miss = picker.PickNearest(scene, new IfcSceneRay(5.0, 5.0, 0.0, 0.0, 0.0, 1.0));
            True(miss == null, "miss returns null");

            RejectArgument("zero direction", delegate { new IfcSceneRay(0, 0, 0, 0, 0, 0); });
            Reject("duplicate guid", delegate
            {
                picker.PickNearest(
                    new IfcStandaloneScene("sample.ifc", "rev-1", new[]
                    {
                        new IfcSceneNode("A", "ifc-step://#1", near, false),
                        new IfcSceneNode("a", "ifc-step://#2", far, false)
                    }, new IfcViewCommand[0]),
                    ray);
            });
            Reject("degenerate triangle", delegate
            {
                var degenerate = new IfcSceneMesh(
                    new[] { new IfcSceneVertex(0, 0, 2), new IfcSceneVertex(1, 0, 2), new IfcSceneVertex(2, 0, 2) },
                    new[] { 0, 1, 2 });
                picker.PickNearest(
                    new IfcStandaloneScene("sample.ifc", "rev-1", new[] { new IfcSceneNode("D", "ifc-step://#3", degenerate, false) }, new IfcViewCommand[0]),
                    ray);
            });
            Reject("ambiguous equal-distance identities", delegate
            {
                picker.PickNearest(
                    new IfcStandaloneScene("sample.ifc", "rev-1", new[]
                    {
                        new IfcSceneNode("A", "ifc-step://#4", near, false),
                        new IfcSceneNode("B", "ifc-step://#5", near, false)
                    }, new IfcViewCommand[0]),
                    ray);
            });
        }

        private static IfcSceneMesh TriangleAtZ(double z)
        {
            return new IfcSceneMesh(
                new[]
                {
                    new IfcSceneVertex(0, 0, z),
                    new IfcSceneVertex(1, 0, z),
                    new IfcSceneVertex(0, 1, z)
                },
                new[] { 0, 1, 2 });
        }

        private static void Reject(string label, Action action)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            True(failed, label + " fails closed");
        }

        private static void RejectArgument(string label, Action action)
        {
            var failed = false;
            try { action(); }
            catch (ArgumentException) { failed = true; }
            True(failed, label + " fails closed");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
