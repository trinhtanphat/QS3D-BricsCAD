using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimViewportHostSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var document = new IfcStandaloneDocument("viewport.ifc", "R9", new[]
            {
                Element("G2", "IfcDoor", "Internal", "ARC.DOOR", "mesh://G2", 2d),
                Element("G1", "IfcWall", "External", "ARC.WALL", "mesh://G1", 5d)
            });
            var source = new StaticSource(document);
            var renderer = new FakeRenderer();
            var host = new QuantBimStandaloneViewportHost(
                new QuantBimStandaloneWorkbench(source),
                new QuantBimStandaloneSceneBuilder(new TriangleResolver()),
                renderer);

            var opened = host.Open("viewport.ifc");
            Equal(2, opened.VisibleGuids.Count, "open visible count");
            Equal("G1", opened.VisibleGuids[0], "open deterministic visibility");
            Equal(1, renderer.PresentCount, "open presents scene");

            var filtered = host.ApplyFilter(new IfcWorkbenchFilter("IfcWall", string.Empty, "External", string.Empty));
            Equal(1, filtered.VisibleGuids.Count, "filtered visible count");
            Equal("G1", filtered.VisibleGuids[0], "filtered wall");

            var selected = host.Select("Wall selection", new[] { "G1" });
            Equal(1, selected.SelectedGuids.Count, "selected count");
            True(selected.Scene.Nodes.Single(x => x.Guid == "G1").Selected, "scene highlight projection");
            Throws<InvalidOperationException>(() => host.Select("Hidden door", new[] { "G2" }), "hidden selection rejected");

            var inspection = host.Inspect("G1");
            Equal("viewport.ifc", inspection.DocumentPath, "inspection source path");
            Equal("R9", inspection.Revision, "inspection revision");
            Equal("mesh://G1", inspection.GeometryReference, "inspection geometry evidence");
            True(inspection.Properties.Any(x => x.Name == "Identity.Guid" && x.Value == "G1"), "inspection property tree");
            Equal(1, inspection.Quantities.Count, "inspection quantity evidence");

            var takeoff = host.TakeoffSelection();
            Equal(1, takeoff.Count, "selected takeoff count");
            var boq = host.BuildSelectionBoq();
            Equal(1, boq.Count, "selected boq count");
            True(host.ExportSelectionBoqCsv().Contains("ARC.WALL"), "selected boq csv");

            host.Orbit(1d, 2d, 3d);
            Equal(IfcViewCommandKind.Orbit, renderer.LastNavigation.Kind, "orbit forwarded");
            host.Pan(4d, 5d);
            Equal(IfcViewCommandKind.Pan, renderer.LastNavigation.Kind, "pan forwarded");
            host.Zoom(2d);
            Equal(IfcViewCommandKind.Zoom, renderer.LastNavigation.Kind, "zoom forwarded");
            host.FitAll();
            Equal(IfcViewCommandKind.FitAll, renderer.LastNavigation.Kind, "fit forwarded");
            host.FocusSelection();
            Equal(IfcViewCommandKind.FocusSelection, renderer.LastNavigation.Kind, "focus forwarded");
            host.SetStandardView(QuantBimViewportStandardView.Isometric);
            Equal(QuantBimViewportStandardView.Isometric, renderer.LastStandardView, "standard view forwarded");

            var doorOnly = host.ApplyFilter(new IfcWorkbenchFilter("IfcDoor", string.Empty, "Internal", string.Empty));
            Equal(0, doorOnly.SelectedGuids.Count, "filter clears hidden selection");
            True(doorOnly.Scene.Nodes.All(x => !x.Selected), "scene clears hidden highlight");
            Throws<InvalidOperationException>(() => host.Inspect("UNKNOWN"), "unknown inspection rejected");
        }

        private static IfcStandaloneElement Element(string guid, string entity, string type, string classification, string geometryReference, double quantity)
        {
            return new IfcStandaloneElement(
                guid,
                entity,
                guid,
                "L01",
                type,
                classification,
                new[] { new IfcPropertyNode("Pset_Test.Status", "Verified") },
                new[] { new IfcQtoItem(guid, entity, "L01", classification, "Length", quantity, "m") },
                geometryReference);
        }

        private sealed class StaticSource : IIfcStandaloneSource
        {
            private readonly IfcStandaloneDocument _document;
            public StaticSource(IfcStandaloneDocument document) { _document = document; }
            public IfcStandaloneDocument Open(string path)
            {
                if (!string.Equals(path, _document.Path, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected test path.");
                return _document;
            }
        }

        private sealed class TriangleResolver : IIfcGeometryResolver
        {
            public IfcSceneMesh Resolve(string geometryReference)
            {
                if (string.IsNullOrWhiteSpace(geometryReference)) throw new InvalidOperationException("Geometry reference is required.");
                return new IfcSceneMesh(
                    new[]
                    {
                        new IfcSceneVertex(0d, 0d, 0d),
                        new IfcSceneVertex(1d, 0d, 0d),
                        new IfcSceneVertex(0d, 1d, 0d)
                    },
                    new[] { 0, 1, 2 });
            }
        }

        private sealed class FakeRenderer : IQuantBimViewportRenderer
        {
            public int PresentCount { get; private set; }
            public QuantBimViewportPresentation LastPresentation { get; private set; }
            public IfcViewCommand LastNavigation { get; private set; }
            public QuantBimViewportStandardView LastStandardView { get; private set; }

            public void Present(QuantBimViewportPresentation presentation)
            {
                LastPresentation = presentation ?? throw new ArgumentNullException("presentation");
                PresentCount++;
            }

            public void Navigate(IfcViewCommand command)
            {
                LastNavigation = command ?? throw new ArgumentNullException("command");
            }

            public void SetStandardView(QuantBimViewportStandardView view)
            {
                LastStandardView = view;
            }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            catch (Exception ex) { throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ", actual " + ex.GetType().Name + "."); }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }
    }
}
