using System;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectStateSnapshot
    {
        private readonly ProjectState _snapshot;

        private ProjectStateSnapshot(ProjectState snapshot)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public static ProjectStateSnapshot Capture(ProjectState project)
        {
            return new ProjectStateSnapshot(CreateDetachedCopy(project));
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
            CopyInto(_snapshot, project);
        }

        private static ProjectState Clone(ProjectState source)
        {
            var target = new ProjectState(source.ProjectId, source.Name);
            CopyInto(source, target);
            return target;
        }

        private static void CopyInto(ProjectState source, ProjectState target)
        {
            target.SchemaVersion = source.SchemaVersion;
            target.Name = source.Name;
            target.DrawingPath = source.DrawingPath ?? string.Empty;
            target.DrawingFingerprint = source.DrawingFingerprint ?? string.Empty;
            target.ActiveZoneId = source.ActiveZoneId ?? string.Empty;
            target.ActiveFloorId = source.ActiveFloorId ?? string.Empty;

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
                foreach (var property in family.Properties) copy.Properties[property.Key] = property.Value ?? string.Empty;
                target.Families.Add(copy);
            }

            target.Elements.Clear();
            foreach (var element in source.Elements)
            {
                var copy = new ProjectElement(element.Id, element.Category)
                {
                    FamilyId = element.FamilyId ?? string.Empty,
                    FloorId = element.FloorId ?? string.Empty,
                    ZoneId = element.ZoneId ?? string.Empty,
                    DrawingFingerprint = element.DrawingFingerprint ?? string.Empty
                };
                foreach (var handle in element.SourceHandles) copy.SourceHandles.Add(handle ?? string.Empty);
                foreach (var dependency in element.DependsOn) copy.DependsOn.Add(dependency ?? string.Empty);
                foreach (var property in element.Properties) copy.Properties[property.Key] = property.Value ?? string.Empty;
                foreach (var quantity in element.Quantities) copy.Quantities[quantity.Key] = quantity.Value;
                copy.RestorePersistenceState(element.Dirty, element.UpdatedUtc);
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
                    Action = audit.Action ?? string.Empty,
                    ElementId = audit.ElementId ?? string.Empty,
                    Detail = audit.Detail ?? string.Empty,
                    Actor = audit.Actor ?? string.Empty,
                    CorrelationId = audit.CorrelationId ?? string.Empty
                });
            }

            target.Metadata.Clear();
            foreach (var item in source.Metadata) target.Metadata[item.Key] = item.Value ?? string.Empty;
            target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);
        }
    }
}
