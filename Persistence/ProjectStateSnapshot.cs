using System;
using System.Collections.Generic;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectStateSnapshot
    {
        private readonly ProjectState _snapshot;
        private readonly ProjectState _capturedProject;
        private readonly IReadOnlyDictionary<string, ProjectElement> _capturedElements;

        private ProjectStateSnapshot(ProjectState snapshot, ProjectState capturedProject, IReadOnlyDictionary<string, ProjectElement> capturedElements)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _capturedProject = capturedProject ?? throw new ArgumentNullException(nameof(capturedProject));
            _capturedElements = capturedElements ?? throw new ArgumentNullException(nameof(capturedElements));
        }

        public static ProjectStateSnapshot Capture(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var capturedElements = CaptureElementReferences(project);
            return new ProjectStateSnapshot(CreateDetachedCopy(project), project, capturedElements);
        }

        public static ProjectState CreateDetachedCopy(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return Clone(project);
        }

        public void Restore(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!string.Equals(project.ProjectId, _snapshot.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Cannot restore a snapshot into a different project id.");
            var preservedElements = ReferenceEquals(project, _capturedProject) ? _capturedElements : null;
            CopyInto(_snapshot, project, preservedElements);
        }

        private static ProjectState Clone(ProjectState source)
        {
            var target = new ProjectState(source.ProjectId, source.Name);
            CopyInto(source, target, null);
            return target;
        }

        private static IReadOnlyDictionary<string, ProjectElement> CaptureElementReferences(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || string.IsNullOrWhiteSpace(element.Id))
                    throw new InvalidOperationException("Cannot capture a project containing an element without id.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Cannot capture a project containing duplicate element id: " + element.Id + ".");
                result.Add(element.Id, element);
            }
            return result;
        }

        private static void CopyInto(ProjectState source, ProjectState target, IReadOnlyDictionary<string, ProjectElement>? preservedElements)
        {
            target.SchemaVersion = source.SchemaVersion;
            target.Name = source.Name;
            target.DrawingPath = source.DrawingPath;
            target.DrawingFingerprint = source.DrawingFingerprint;
            target.ActiveZoneId = source.ActiveZoneId;
            target.ActiveFloorId = source.ActiveFloorId;

            target.Zones.Clear();
            foreach (var zone in source.Zones)
                target.Zones.Add(new ZoneDefinition(zone.Id, zone.Name));

            target.Floors.Clear();
            foreach (var floor in source.Floors)
                target.Floors.Add(new FloorDefinition(floor.Id, floor.Name, floor.ElevationM));

            target.Families.Clear();
            foreach (var family in source.Families)
            {
                var copy = new ProjectFamily(family.Id, family.Name, family.Category);
                foreach (var property in family.Properties) copy.Properties[property.Key] = property.Value;
                target.Families.Add(copy);
            }

            target.Elements.Clear();
            foreach (var element in source.Elements)
            {
                ProjectElement copy;
                if (preservedElements != null && preservedElements.TryGetValue(element.Id, out var preserved))
                {
                    copy = preserved;
                    CopyElementInto(element, copy);
                }
                else
                {
                    copy = CloneElement(element);
                }
                target.Elements.Add(copy);
            }

            target.QuantityRules.Clear();
            foreach (var rule in source.QuantityRules)
                target.QuantityRules.Add(new QuantityRule(rule.Id, rule.Category, rule.OutputName, rule.Expression, rule.Version));

            target.AuditEvents.Clear();
            foreach (var audit in source.AuditEvents)
            {
                target.AuditEvents.Add(new AuditEvent
                {
                    Utc = audit.Utc,
                    Action = audit.Action,
                    ElementId = audit.ElementId,
                    Detail = audit.Detail,
                    Actor = audit.Actor,
                    CorrelationId = audit.CorrelationId
                });
            }

            target.Metadata.Clear();
            foreach (var item in source.Metadata) target.Metadata[item.Key] = item.Value;
            target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);
        }

        private static ProjectElement CloneElement(ProjectElement source)
        {
            var target = new ProjectElement(source.Id, source.Category);
            CopyElementInto(source, target);
            return target;
        }

        private static void CopyElementInto(ProjectElement source, ProjectElement target)
        {
            if (!string.Equals(source.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot restore element state into a different element id.");

            target.Category = source.Category;
            target.FamilyId = source.FamilyId;
            target.FloorId = source.FloorId;
            target.ZoneId = source.ZoneId;
            target.DrawingFingerprint = source.DrawingFingerprint;

            target.SourceHandles.Clear();
            foreach (var handle in source.SourceHandles) target.SourceHandles.Add(handle);

            target.DependsOn.Clear();
            foreach (var dependency in source.DependsOn) target.DependsOn.Add(dependency);

            target.Properties.Clear();
            foreach (var property in source.Properties) target.Properties[property.Key] = property.Value;

            target.Quantities.Clear();
            foreach (var quantity in source.Quantities) target.Quantities[quantity.Key] = quantity.Value;

            target.RestorePersistenceState(source.Dirty, source.UpdatedUtc);
        }
    }
}
