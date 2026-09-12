using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class IfcSceneVertex
    {
        public IfcSceneVertex(double x, double y, double z)
        {
            X = QsModelElementSnapshot.Finite(x, "x");
            Y = QsModelElementSnapshot.Finite(y, "y");
            Z = QsModelElementSnapshot.Finite(z, "z");
        }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
    }

    public sealed class IfcSceneMesh
    {
        private readonly ReadOnlyCollection<byte> _artifactBytes;

        public IfcSceneMesh(IEnumerable<IfcSceneVertex> vertices, IEnumerable<int> triangleIndices)
            : this(vertices, triangleIndices, Array.Empty<byte>())
        {
        }

        public IfcSceneMesh(IEnumerable<IfcSceneVertex> vertices, IEnumerable<int> triangleIndices, IEnumerable<byte> artifactBytes)
        {
            var vertexList = (vertices ?? throw new ArgumentNullException("vertices")).ToList();
            var indexList = (triangleIndices ?? throw new ArgumentNullException("triangleIndices")).ToList();
            var artifactSnapshot = (artifactBytes ?? throw new ArgumentNullException("artifactBytes")).ToArray();
            if (vertexList.Count < 3) throw new ArgumentException("Scene mesh requires at least three vertices.", "vertices");
            if (vertexList.Any(x => x == null)) throw new ArgumentException("Scene mesh contains a null vertex.", "vertices");
            if (indexList.Count == 0 || indexList.Count % 3 != 0) throw new ArgumentException("Triangle indices must contain complete triangles.", "triangleIndices");
            if (indexList.Any(x => x < 0 || x >= vertexList.Count)) throw new ArgumentOutOfRangeException("triangleIndices", "Triangle index is outside the vertex range.");
            Vertices = new ReadOnlyCollection<IfcSceneVertex>(vertexList);
            TriangleIndices = new ReadOnlyCollection<int>(indexList);
            _artifactBytes = new ReadOnlyCollection<byte>(artifactSnapshot);
        }

        public IReadOnlyList<IfcSceneVertex> Vertices { get; private set; }
        public IReadOnlyList<int> TriangleIndices { get; private set; }
        public IReadOnlyList<byte> ArtifactBytes { get { return _artifactBytes; } }
    }

    public interface IIfcGeometryResolver
    {
        IfcSceneMesh Resolve(string geometryReference);
    }

    public sealed class IfcSceneNode
    {
        public IfcSceneNode(string guid, string geometryReference, IfcSceneMesh mesh, bool selected)
        {
            Guid = QsModelElementSnapshot.Require(guid, "guid");
            GeometryReference = QsModelElementSnapshot.Require(geometryReference, "geometryReference");
            Mesh = mesh ?? throw new ArgumentNullException("mesh");
            Selected = selected;
        }

        public string Guid { get; private set; }
        public string GeometryReference { get; private set; }
        public IfcSceneMesh Mesh { get; private set; }
        public bool Selected { get; private set; }
    }

    public sealed class IfcStandaloneScene
    {
        public IfcStandaloneScene(string documentPath, string revision, IEnumerable<IfcSceneNode> nodes, IEnumerable<IfcViewCommand> navigation)
        {
            DocumentPath = QsModelElementSnapshot.Require(documentPath, "documentPath");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Nodes = new ReadOnlyCollection<IfcSceneNode>((nodes ?? throw new ArgumentNullException("nodes")).ToList());
            Navigation = new ReadOnlyCollection<IfcViewCommand>((navigation ?? throw new ArgumentNullException("navigation")).ToList());
        }

        public string DocumentPath { get; private set; }
        public string Revision { get; private set; }
        public IReadOnlyList<IfcSceneNode> Nodes { get; private set; }
        public IReadOnlyList<IfcViewCommand> Navigation { get; private set; }
    }

    public sealed class QuantBimStandaloneSceneBuilder
    {
        private readonly IIfcGeometryResolver _geometry;

        public QuantBimStandaloneSceneBuilder(IIfcGeometryResolver geometry)
        {
            _geometry = geometry ?? throw new ArgumentNullException("geometry");
        }

        public IfcStandaloneScene Build(IfcStandaloneDocument document, IfcSelectionSet selection)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (selection == null) throw new ArgumentNullException("selection");

            var selected = new HashSet<string>(selection.Guids, StringComparer.OrdinalIgnoreCase);
            var known = new HashSet<string>(document.Elements.Select(x => x.Guid), StringComparer.OrdinalIgnoreCase);
            var unknown = selected.Where(x => !known.Contains(x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            if (unknown.Count != 0) throw new InvalidOperationException("Scene selection references unknown IFC element: " + unknown[0] + ".");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nodes = new List<IfcSceneNode>();
            foreach (var element in document.Elements.OrderBy(x => x.Guid, StringComparer.OrdinalIgnoreCase))
            {
                if (!seen.Add(element.Guid)) throw new InvalidOperationException("Duplicate IFC scene guid: " + element.Guid + ".");
                if (string.IsNullOrWhiteSpace(element.GeometryReference)) throw new InvalidOperationException("IFC element has no geometry reference: " + element.Guid + ".");
                var mesh = _geometry.Resolve(element.GeometryReference);
                if (mesh == null) throw new InvalidOperationException("Geometry resolver returned no mesh for " + element.Guid + ".");
                nodes.Add(new IfcSceneNode(element.Guid, element.GeometryReference, mesh, selected.Contains(element.Guid)));
            }

            var navigation = new QuantBimStandaloneWorkbench(new SceneOnlySource(document)).DefaultNavigation(selection);
            return new IfcStandaloneScene(document.Path, document.Revision, nodes, navigation);
        }

        private sealed class SceneOnlySource : IIfcStandaloneSource
        {
            private readonly IfcStandaloneDocument _document;
            public SceneOnlySource(IfcStandaloneDocument document) { _document = document; }
            public IfcStandaloneDocument Open(string path) { return _document; }
        }
    }
}
