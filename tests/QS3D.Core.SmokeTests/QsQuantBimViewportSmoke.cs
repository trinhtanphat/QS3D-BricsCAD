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

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
