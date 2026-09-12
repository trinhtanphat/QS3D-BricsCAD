using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public enum QuantBimViewportStandardView
    {
        Isometric,
        Top,
        Front,
        Right
    }

    public sealed class QuantBimViewportPresentation
    {
        public QuantBimViewportPresentation(IfcStandaloneScene scene, IEnumerable<string> visibleGuids, IEnumerable<string> selectedGuids)
        {
            Scene = scene ?? throw new ArgumentNullException("scene");
            var known = new HashSet<string>(scene.Nodes.Select(x => x.Guid), StringComparer.OrdinalIgnoreCase);
            var visible = Normalize(visibleGuids, "visibleGuids");
            var selected = Normalize(selectedGuids, "selectedGuids");
            var unknownVisible = visible.FirstOrDefault(x => !known.Contains(x));
            if (unknownVisible != null) throw new InvalidOperationException("Viewport visibility references unknown IFC element: " + unknownVisible + ".");
            var visibleSet = new HashSet<string>(visible, StringComparer.OrdinalIgnoreCase);
            var hiddenSelection = selected.FirstOrDefault(x => !visibleSet.Contains(x));
            if (hiddenSelection != null) throw new InvalidOperationException("Viewport selection references a hidden IFC element: " + hiddenSelection + ".");
            VisibleGuids = new ReadOnlyCollection<string>(visible);
            SelectedGuids = new ReadOnlyCollection<string>(selected);
        }

        public IfcStandaloneScene Scene { get; private set; }
        public IReadOnlyList<string> VisibleGuids { get; private set; }
        public IReadOnlyList<string> SelectedGuids { get; private set; }

        private static List<string> Normalize(IEnumerable<string> values, string name)
        {
            if (values == null) throw new ArgumentNullException(name);
            return values.Select(x => QsModelElementSnapshot.Require(x, "guid"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public interface IQuantBimViewportRenderer
    {
        void Present(QuantBimViewportPresentation presentation);
        void Navigate(IfcViewCommand command);
        void SetStandardView(QuantBimViewportStandardView view);
    }

    public sealed class QuantBimViewportInspection
    {
        public QuantBimViewportInspection(string documentPath, string revision, IfcStandaloneElement element, IEnumerable<IfcPropertyNode> properties)
        {
            DocumentPath = QsModelElementSnapshot.Require(documentPath, "documentPath");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Element = element ?? throw new ArgumentNullException("element");
            Properties = new ReadOnlyCollection<IfcPropertyNode>((properties ?? throw new ArgumentNullException("properties")).ToList());
            Quantities = new ReadOnlyCollection<IfcQtoItem>(element.Quantities.ToList());
        }

        public string DocumentPath { get; private set; }
        public string Revision { get; private set; }
        public IfcStandaloneElement Element { get; private set; }
        public IReadOnlyList<IfcPropertyNode> Properties { get; private set; }
        public IReadOnlyList<IfcQtoItem> Quantities { get; private set; }
        public string GeometryReference { get { return Element.GeometryReference; } }
    }

    public sealed class QuantBimStandaloneViewportHost
    {
        private readonly QuantBimStandaloneWorkbench _workbench;
        private readonly QuantBimStandaloneSceneBuilder _sceneBuilder;
        private readonly IQuantBimViewportRenderer _renderer;
        private IfcStandaloneDocument _document;
        private IfcWorkbenchFilter _filter;
        private IfcSelectionSet _selection;
        private QuantBimViewportPresentation _presentation;

        public QuantBimStandaloneViewportHost(QuantBimStandaloneWorkbench workbench, QuantBimStandaloneSceneBuilder sceneBuilder, IQuantBimViewportRenderer renderer)
        {
            _workbench = workbench ?? throw new ArgumentNullException("workbench");
            _sceneBuilder = sceneBuilder ?? throw new ArgumentNullException("sceneBuilder");
            _renderer = renderer ?? throw new ArgumentNullException("renderer");
            _filter = AllFilter();
            _selection = new IfcSelectionSet("Viewport", Array.Empty<string>());
        }

        public IfcStandaloneDocument CurrentDocument { get { return _document; } }
        public QuantBimViewportPresentation CurrentPresentation { get { return _presentation; } }
        public IfcSelectionSet CurrentSelection { get { return _selection; } }

        public QuantBimViewportPresentation Open(string path)
        {
            _document = _workbench.Open(QsModelElementSnapshot.Require(path, "path"));
            _filter = AllFilter();
            _selection = new IfcSelectionSet("Viewport", Array.Empty<string>());
            return Refresh();
        }

        public QuantBimViewportPresentation ApplyFilter(IfcWorkbenchFilter filter)
        {
            RequireDocument();
            _filter = filter ?? throw new ArgumentNullException("filter");
            var visible = new HashSet<string>(_workbench.Filter(_document, _filter).Select(x => x.Guid), StringComparer.OrdinalIgnoreCase);
            _selection = new IfcSelectionSet(_selection.Name, _selection.Guids.Where(visible.Contains));
            return Refresh();
        }

        public QuantBimViewportPresentation Select(string name, IEnumerable<string> guids)
        {
            RequireDocument();
            var next = new IfcSelectionSet(name, guids ?? throw new ArgumentNullException("guids"));
            var visible = new HashSet<string>(_workbench.Filter(_document, _filter).Select(x => x.Guid), StringComparer.OrdinalIgnoreCase);
            var hidden = next.Guids.FirstOrDefault(x => !visible.Contains(x));
            if (hidden != null) throw new InvalidOperationException("Cannot select an IFC element hidden by the current viewport filter: " + hidden + ".");
            _selection = next;
            return Refresh();
        }

        public QuantBimViewportInspection Inspect(string guid)
        {
            RequireDocument();
            guid = QsModelElementSnapshot.Require(guid, "guid");
            var element = _document.Elements.FirstOrDefault(x => string.Equals(x.Guid, guid, StringComparison.OrdinalIgnoreCase));
            if (element == null) throw new InvalidOperationException("Unknown IFC element: " + guid + ".");
            return new QuantBimViewportInspection(_document.Path, _document.Revision, element, _workbench.PropertyTree(element));
        }

        public IReadOnlyList<IfcQtoItem> TakeoffSelection()
        {
            RequireDocument();
            return _workbench.Takeoff(_document, _selection);
        }

        public IReadOnlyList<TakeoffInventoryLine> BuildSelectionBoq()
        {
            return _workbench.BuildBoq(TakeoffSelection());
        }

        public string ExportSelectionBoqCsv()
        {
            return _workbench.ExportCsv(BuildSelectionBoq());
        }

        public void FitAll() { Navigate(new IfcViewCommand(IfcViewCommandKind.FitAll, 0d, 0d, 0d)); }
        public void FocusSelection() { Navigate(new IfcViewCommand(IfcViewCommandKind.FocusSelection, 0d, 0d, 0d)); }
        public void Orbit(double x, double y, double z) { Navigate(new IfcViewCommand(IfcViewCommandKind.Orbit, x, y, z)); }
        public void Pan(double x, double y) { Navigate(new IfcViewCommand(IfcViewCommandKind.Pan, x, y, 0d)); }
        public void Zoom(double amount) { Navigate(new IfcViewCommand(IfcViewCommandKind.Zoom, amount, 0d, 0d)); }

        public void SetStandardView(QuantBimViewportStandardView view)
        {
            RequireDocument();
            _renderer.SetStandardView(view);
        }

        private void Navigate(IfcViewCommand command)
        {
            RequireDocument();
            _renderer.Navigate(command ?? throw new ArgumentNullException("command"));
        }

        private QuantBimViewportPresentation Refresh()
        {
            RequireDocument();
            var visible = _workbench.Filter(_document, _filter).Select(x => x.Guid).ToList();
            var scene = _sceneBuilder.Build(_document, _selection);
            _presentation = new QuantBimViewportPresentation(scene, visible, _selection.Guids);
            _renderer.Present(_presentation);
            return _presentation;
        }

        private void RequireDocument()
        {
            if (_document == null) throw new InvalidOperationException("Open an IFC document before using the viewport host.");
        }

        private static IfcWorkbenchFilter AllFilter()
        {
            return new IfcWorkbenchFilter(string.Empty, string.Empty, string.Empty, string.Empty);
        }
    }
}
