using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Binds standalone IFC parsing, QTO and scene geometry to one exact STEP generation.
    /// </summary>
    public sealed class QuantBimStandaloneIfcSession
    {
        private readonly QuantBimStandaloneWorkbench _workbench;
        private readonly QuantBimStandaloneSceneBuilder _sceneBuilder;

        private QuantBimStandaloneIfcSession(IfcStandaloneDocument document, IIfcGeometryResolver geometry)
        {
            Document = document ?? throw new ArgumentNullException("document");
            _workbench = new QuantBimStandaloneWorkbench(new BoundSource(document));
            _sceneBuilder = new QuantBimStandaloneSceneBuilder(geometry ?? throw new ArgumentNullException("geometry"));
        }

        public IfcStandaloneDocument Document { get; private set; }
        public string Path { get { return Document.Path; } }
        public string Revision { get { return Document.Revision; } }

        public static QuantBimStandaloneIfcSession Parse(string path, string stepText)
        {
            path = QsModelElementSnapshot.Require(path, "path");
            if (string.IsNullOrWhiteSpace(stepText)) throw new InvalidDataException("IFC STEP session content is empty.");
            var document = new IfcStepStandaloneSource().Parse(path, stepText);
            var geometry = new IfcStepGeometryResolver(stepText);
            return new QuantBimStandaloneIfcSession(document, geometry);
        }

        public static QuantBimStandaloneIfcSession Open(string path)
        {
            path = QsModelElementSnapshot.Require(path, "path");
            var stepText = File.ReadAllText(path);
            return Parse(path, stepText);
        }

        public IReadOnlyList<IfcStandaloneElement> Filter(IfcWorkbenchFilter filter)
        {
            return _workbench.Filter(Document, filter ?? throw new ArgumentNullException("filter"));
        }

        public IReadOnlyList<IfcPropertyNode> PropertyTree(string guid)
        {
            var element = RequireElement(guid);
            return _workbench.PropertyTree(element);
        }

        public IReadOnlyList<IfcQtoItem> Takeoff(IfcSelectionSet selection)
        {
            return _workbench.Takeoff(Document, selection ?? throw new ArgumentNullException("selection"));
        }

        public IReadOnlyList<TakeoffInventoryLine> BuildBoq(IfcSelectionSet selection)
        {
            return _workbench.BuildBoq(Takeoff(selection));
        }

        public string ExportCsv(IfcSelectionSet selection)
        {
            return _workbench.ExportCsv(BuildBoq(selection));
        }

        public IfcStandaloneScene BuildScene(IfcSelectionSet selection)
        {
            return _sceneBuilder.Build(Document, selection ?? throw new ArgumentNullException("selection"));
        }

        public IfcStandaloneScene BuildScene(IfcStandaloneDocument candidate, IfcSelectionSet selection)
        {
            ValidateGeneration(candidate);
            return BuildScene(selection);
        }

        public void ValidateGeneration(IfcStandaloneDocument candidate)
        {
            if (candidate == null) throw new ArgumentNullException("candidate");
            if (!string.Equals(Document.Path, candidate.Path, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("IFC session path does not match the bound document generation.");
            if (!string.Equals(Document.Revision, candidate.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException("IFC session revision does not match the bound document generation.");
        }

        private IfcStandaloneElement RequireElement(string guid)
        {
            guid = QsModelElementSnapshot.Require(guid, "guid");
            var matches = Document.Elements.Where(x => string.Equals(x.Guid, guid, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count != 1) throw new InvalidOperationException("IFC session requires exactly one element for guid: " + guid + ".");
            return matches[0];
        }

        private sealed class BoundSource : IIfcStandaloneSource
        {
            private readonly IfcStandaloneDocument _document;
            public BoundSource(IfcStandaloneDocument document) { _document = document; }
            public IfcStandaloneDocument Open(string path)
            {
                path = QsModelElementSnapshot.Require(path, "path");
                if (!string.Equals(path, _document.Path, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Bound IFC session cannot open a different path.");
                return _document;
            }
        }
    }
}
