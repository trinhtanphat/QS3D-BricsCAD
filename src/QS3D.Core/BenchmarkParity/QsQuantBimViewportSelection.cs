using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class QuantBimViewportSelectionResult
    {
        public QuantBimViewportSelectionResult(
            IfcScenePickHit? hit,
            IfcSelectionSet selection,
            IEnumerable<IfcPropertyNode> properties,
            IEnumerable<IfcQtoItem> takeoff,
            IEnumerable<TakeoffInventoryLine> boq)
        {
            Hit = hit;
            Selection = selection ?? throw new ArgumentNullException("selection");
            Properties = new ReadOnlyCollection<IfcPropertyNode>((properties ?? throw new ArgumentNullException("properties")).ToList());
            Takeoff = new ReadOnlyCollection<IfcQtoItem>((takeoff ?? throw new ArgumentNullException("takeoff")).ToList());
            Boq = new ReadOnlyCollection<TakeoffInventoryLine>((boq ?? throw new ArgumentNullException("boq")).ToList());
        }

        public IfcScenePickHit? Hit { get; private set; }
        public IfcSelectionSet Selection { get; private set; }
        public IReadOnlyList<IfcPropertyNode> Properties { get; private set; }
        public IReadOnlyList<IfcQtoItem> Takeoff { get; private set; }
        public IReadOnlyList<TakeoffInventoryLine> Boq { get; private set; }
        public bool HasHit { get { return Hit != null; } }
    }

    /// <summary>
    /// Composes the generation-bound IFC session, renderer-neutral screen-ray projection and scene picking
    /// into one deterministic standalone selection workflow. Visual mesh/ray evidence remains navigation-only
    /// and never becomes authoritative quantity evidence.
    /// </summary>
    public sealed class QuantBimStandaloneViewportSelection
    {
        private readonly QuantBimStandaloneIfcSession _session;
        private readonly QuantBimStandaloneScreenRayProjector _projector;
        private readonly QuantBimStandaloneScenePicker _picker;

        public QuantBimStandaloneViewportSelection(QuantBimStandaloneIfcSession session)
        {
            _session = session ?? throw new ArgumentNullException("session");
            _projector = new QuantBimStandaloneScreenRayProjector();
            _picker = new QuantBimStandaloneScenePicker();
        }

        public QuantBimViewportSelectionResult Pick(
            IfcCameraNavigationState camera,
            double viewportWidth,
            double viewportHeight,
            double pointerX,
            double pointerY,
            double verticalFieldOfViewDegrees)
        {
            if (camera == null) throw new ArgumentNullException("camera");

            var allGuids = _session.Document.Elements.Select(x => x.Guid).ToList();
            var wholeModel = new IfcSelectionSet("__viewport_pick_all__", allGuids);
            var scene = _session.BuildScene(wholeModel);
            if (!string.Equals(scene.Revision, _session.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException("Viewport selection scene revision does not match the bound IFC session generation.");

            var ray = _projector.ProjectPerspective(camera, viewportWidth, viewportHeight, pointerX, pointerY, verticalFieldOfViewDegrees);
            var hit = _picker.PickNearest(scene, ray);
            if (hit == null) return Empty();

            var matching = _session.Document.Elements.Where(x => string.Equals(x.Guid, hit.Guid, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matching.Count != 1)
                throw new InvalidOperationException("Viewport selection requires exactly one bound IFC element for guid: " + hit.Guid + ".");

            var element = matching[0];
            if (!string.Equals(element.GeometryReference, hit.GeometryReference, StringComparison.Ordinal))
                throw new InvalidOperationException("Viewport selection geometry reference does not match the bound IFC element generation.");

            var selection = new IfcSelectionSet("Viewport selection", new[] { element.Guid });
            var properties = _session.PropertyTree(element.Guid);
            var takeoff = _session.Takeoff(selection);
            var boq = _session.BuildBoq(selection);
            return new QuantBimViewportSelectionResult(hit, selection, properties, takeoff, boq);
        }

        private static QuantBimViewportSelectionResult Empty()
        {
            return new QuantBimViewportSelectionResult(
                null,
                new IfcSelectionSet("Viewport selection", Array.Empty<string>()),
                Array.Empty<IfcPropertyNode>(),
                Array.Empty<IfcQtoItem>(),
                Array.Empty<TakeoffInventoryLine>());
        }
    }
}
