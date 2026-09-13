using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimViewportSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var viewport = new QuantBimStandaloneViewport();
            var meshA = Box(0.0, 0.0, 0.0, 2.0, 4.0, 6.0);
            var meshB = Box(10.0, 0.0, 0.0, 12.0, 2.0, 2.0);
            var scene = new IfcStandaloneScene(
                "sample.ifc",
                "rev-1",
                new[]
                {
                    new IfcSceneNode("A", "ifc-step://#1", meshA, true),
                    new IfcSceneNode("B", "ifc-step://#2", meshB, false)
                },
                new IfcViewCommand[0]);

            var all = viewport.FitAll(scene);
            Near(0.0, all.Bounds.MinX, "all min x");
            Near(12.0, all.Bounds.MaxX, "all max x");
            Near(6.0, all.Bounds.MaxZ, "all max z");
            True(all.Distance > all.Radius, "all camera outside radius");
            True(all.NearPlane > 0.0, "all positive near plane");
            True(all.FarPlane > all.NearPlane, "all ordered clipping");

            var selected = viewport.FitSelection(scene);
            Near(0.0, selected.Bounds.MinX, "selection min x");
            Near(2.0, selected.Bounds.MaxX, "selection max x");
            Near(1.0, selected.Bounds.CenterX, "selection center x");
            Near(2.0, selected.Bounds.CenterY, "selection center y");
            Near(3.0, selected.Bounds.CenterZ, "selection center z");

            var pointMesh = new IfcSceneMesh(
                new[]
                {
                    new IfcSceneVertex(1, 1, 1),
                    new IfcSceneVertex(1, 1, 1),
                    new IfcSceneVertex(1, 1, 1)
                },
                new[] { 0, 1, 2 });
            var degenerate = viewport.Fit(new[] { new IfcSceneNode("P", "ifc-step://#3", pointMesh, true) }, 45.0);
            True(degenerate.Radius > 0.0, "degenerate radius floor");

            // Same-sign finite endpoints used to overflow center via (min + max) / 2.
            var hugeBase = 1.2e308;
            var hugeDelta = 2.0e292;
            var hugeSameSign = viewport.Fit(
                new[] { new IfcSceneNode("H", "ifc-step://#4", Box(hugeBase, 0.0, 0.0, hugeBase + hugeDelta, 2.0, 2.0), true) },
                45.0);
            Finite(hugeSameSign.Bounds.CenterX, "same-sign extreme midpoint");
            Finite(hugeSameSign.Radius, "same-sign extreme radius");
            Finite(hugeSameSign.Distance, "same-sign extreme distance");
            Finite(hugeSameSign.FarPlane, "same-sign extreme far plane");

            // Opposite-sign endpoints can overflow max-min even when the half extent and
            // complete viewport frame remain finite.
            var hugeOpposite = viewport.Fit(
                new[] { new IfcSceneNode("O", "ifc-step://#5", Box(-1.0e307, 0.0, 0.0, 1.0e307, 1.0, 1.0), true) },
                45.0);
            NearRelative(0.0, hugeOpposite.Bounds.CenterX, 1.0, "opposite-sign extreme midpoint");
            Finite(hugeOpposite.Radius, "opposite-sign extreme radius");
            Finite(hugeOpposite.Distance, "opposite-sign extreme distance");
            Finite(hugeOpposite.NearPlane, "opposite-sign extreme near plane");
            Finite(hugeOpposite.FarPlane, "opposite-sign extreme far plane");
            True(hugeOpposite.FarPlane > hugeOpposite.NearPlane, "opposite-sign clipping order");

            // Large finite half-extents must not overflow merely because x*x does.
            var largeNorm = viewport.Fit(
                new[] { new IfcSceneNode("N", "ifc-step://#6", Box(-1.0e153, -1.0e153, -1.0e153, 1.0e153, 1.0e153, 1.0e153), true) },
                45.0);
            Finite(largeNorm.Radius, "scaled hypot radius");
            True(largeNorm.Radius > 1.0e153, "scaled hypot preserves diagonal radius");

            Reject("empty scene", delegate { viewport.Fit(new IfcSceneNode[0], 45.0); });
            Reject("empty selection", delegate
            {
                viewport.FitSelection(new IfcStandaloneScene(
                    "sample.ifc",
                    "rev-1",
                    new[] { new IfcSceneNode("A", "ifc-step://#1", meshA, false) },
                    new IfcViewCommand[0]));
            });
            RejectArgument("invalid fov", delegate { viewport.Fit(scene.Nodes, 180.0); });
        }

        private static IfcSceneMesh Box(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            return new IfcSceneMesh(
                new[]
                {
                    new IfcSceneVertex(minX, minY, minZ),
                    new IfcSceneVertex(maxX, minY, minZ),
                    new IfcSceneVertex(maxX, maxY, minZ),
                    new IfcSceneVertex(minX, maxY, minZ),
                    new IfcSceneVertex(minX, minY, maxZ),
                    new IfcSceneVertex(maxX, minY, maxZ),
                    new IfcSceneVertex(maxX, maxY, maxZ),
                    new IfcSceneVertex(minX, maxY, maxZ)
                },
                new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 });
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

        private static void NearRelative(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Finite(double value, string label)
        {
            True(!double.IsNaN(value) && !double.IsInfinity(value), label + " is finite");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
