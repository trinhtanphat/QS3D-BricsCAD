using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimSceneSmoke
    {
        internal static void Run()
        {
            var document = new IfcStandaloneDocument("scene.ifc", "R7", new[]
            {
                Element("G2", "mesh://G2"),
                Element("G1", "mesh://G1")
            });
            var selection = new IfcSelectionSet("Selected wall", new[] { "G2" });
            var scene = new QuantBimStandaloneSceneBuilder(new TriangleResolver()).Build(document, selection);

            Equal("R7", scene.Revision, "scene revision");
            Equal("G1", scene.Nodes[0].Guid, "deterministic node order");
            True(scene.Nodes.Single(x => x.Guid == "G2").Selected, "selected highlight state");
            True(!scene.Nodes.Single(x => x.Guid == "G1").Selected, "unselected highlight state");
            Equal(IfcViewCommandKind.FocusSelection, scene.Navigation.Last().Kind, "selection navigation");
            Equal(3, scene.Nodes[0].Mesh.Vertices.Count, "resolved mesh vertices");
            Equal(3, scene.Nodes[0].Mesh.TriangleIndices.Count, "resolved mesh indices");
            Equal(4, scene.Nodes[0].Mesh.ArtifactBytes.Count, "resolved artifact bytes");

            VerifyArtifactBytesAreImmutable();
            RejectUnknownSelection(document);
            RejectMissingGeometry();
            RejectInvalidMesh();
        }

        private static IfcStandaloneElement Element(string guid, string geometryReference)
        {
            return new IfcStandaloneElement(guid, "IfcWall", guid, "L01", "External", "ARC.WALL", Array.Empty<IfcPropertyNode>(), Array.Empty<IfcQtoItem>(), geometryReference);
        }

        private static void VerifyArtifactBytesAreImmutable()
        {
            var input = new byte[] { 10, 20, 30 };
            var mesh = new IfcSceneMesh(
                new[]
                {
                    new IfcSceneVertex(0d, 0d, 0d),
                    new IfcSceneVertex(1d, 0d, 0d),
                    new IfcSceneVertex(0d, 1d, 0d)
                },
                new[] { 0, 1, 2 },
                input);

            input[0] = 99;
            Equal((byte)10, mesh.ArtifactBytes[0], "artifact bytes defensive copy");

            var exposed = mesh.ArtifactBytes as IList<byte>;
            True(exposed != null, "artifact bytes read-only list surface");
            var rejected = false;
            try
            {
                exposed[0] = 77;
            }
            catch (NotSupportedException)
            {
                rejected = true;
            }

            True(rejected, "artifact bytes reject consumer mutation");
            Equal((byte)10, mesh.ArtifactBytes[0], "artifact bytes remain immutable");
        }

        private static void RejectUnknownSelection(IfcStandaloneDocument document)
        {
            var failed = false;
            try
            {
                new QuantBimStandaloneSceneBuilder(new TriangleResolver()).Build(document, new IfcSelectionSet("bad", new[] { "UNKNOWN" }));
            }
            catch (InvalidOperationException)
            {
                failed = true;
            }
            True(failed, "unknown scene selection fails closed");
        }

        private static void RejectMissingGeometry()
        {
            var failed = false;
            try
            {
                var document = new IfcStandaloneDocument("missing.ifc", "R1", new[] { Element("G1", string.Empty) });
                new QuantBimStandaloneSceneBuilder(new TriangleResolver()).Build(document, new IfcSelectionSet("none", Array.Empty<string>()));
            }
            catch (InvalidOperationException)
            {
                failed = true;
            }
            True(failed, "missing geometry fails closed");
        }

        private static void RejectInvalidMesh()
        {
            var failed = false;
            try
            {
                new IfcSceneMesh(new[] { new IfcSceneVertex(0d, 0d, 0d), new IfcSceneVertex(1d, 0d, 0d), new IfcSceneVertex(0d, 1d, 0d) }, new[] { 0, 1, 3 });
            }
            catch (ArgumentOutOfRangeException)
            {
                failed = true;
            }
            True(failed, "invalid triangle index fails closed");
        }

        private sealed class TriangleResolver : IIfcGeometryResolver
        {
            public IfcSceneMesh Resolve(string geometryReference)
            {
                if (string.IsNullOrWhiteSpace(geometryReference)) return null;
                return new IfcSceneMesh(
                    new[]
                    {
                        new IfcSceneVertex(0d, 0d, 0d),
                        new IfcSceneVertex(1d, 0d, 0d),
                        new IfcSceneVertex(0d, 1d, 0d)
                    },
                    new[] { 0, 1, 2 },
                    new byte[] { 1, 2, 3, 4 });
            }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
