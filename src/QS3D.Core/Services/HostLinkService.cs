using System;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class HostLinkService
    {
        public void LinkOpening(ProjectState project, string openingId, string wallId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var opening = project.FindElement(openingId) ?? throw new InvalidOperationException("Opening element not found: " + openingId);
            var wall = project.FindElement(wallId) ?? throw new InvalidOperationException("Wall element not found: " + wallId);
            EnsureOpening(opening, openingId);
            if (!IsWall(wall.Category)) throw new InvalidOperationException("Host is not a wall: " + wallId);

            var previousHost = opening.Properties.TryGetValue("HostWallId", out var previous) ? (previous ?? string.Empty).Trim() : string.Empty;
            var relationshipChanged = !string.Equals(previousHost, wall.Id, StringComparison.OrdinalIgnoreCase);
            if (previousHost.Length > 0 && relationshipChanged)
            {
                RemoveDependencies(opening, previousHost);
                MarkHostOpeningRelationChanged(project.FindElement(previousHost), opening.Id, "unlinked/re-hosted");
            }

            opening.Properties["HostWallId"] = wall.Id;
            var dependencyAdded = !opening.DependsOn.Any(x => DependencyMatches(x, wall.Id));
            RemoveDependencies(opening, wall.Id);
            opening.DependsOn.Add(wall.Id);
            opening.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            if (relationshipChanged || dependencyAdded)
                MarkHostOpeningRelationChanged(wall, opening.Id, "linked/re-hosted");
            else
                wall.MarkDirty(ElementDirtyFlags.Quantity);
            project.Touch();
            AuditTrail.ForProject(project).Record("host.link", opening.Id, (previousHost.Length == 0 ? "" : previousHost + " → ") + wall.Id);
        }

        public void UnlinkOpening(ProjectState project, string openingId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var opening = project.FindElement(openingId) ?? throw new InvalidOperationException("Opening element not found: " + openingId);
            EnsureOpening(opening, openingId);
            var hostId = opening.Properties.TryGetValue("HostWallId", out var value) ? (value ?? string.Empty).Trim() : string.Empty;
            opening.Properties.Remove("HostWallId");
            var dependencyRemoved = RemoveDependencies(opening, hostId) > 0;
            opening.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            if (hostId.Length > 0)
            {
                var host = project.FindElement(hostId);
                if (host != null)
                {
                    if (dependencyRemoved || !opening.Properties.ContainsKey("HostWallId"))
                        MarkHostOpeningRelationChanged(host, opening.Id, "unlinked");
                    else
                        host.MarkDirty(ElementDirtyFlags.Quantity);
                }
            }
            project.Touch();
            AuditTrail.ForProject(project).Record("host.unlink", opening.Id, hostId);
        }

        private static int RemoveDependencies(ProjectElement opening, string hostId)
        {
            if (string.IsNullOrWhiteSpace(hostId)) return 0;
            var removed = 0;
            for (var i = opening.DependsOn.Count - 1; i >= 0; i--)
            {
                if (!DependencyMatches(opening.DependsOn[i], hostId)) continue;
                opening.DependsOn.RemoveAt(i);
                removed++;
            }
            return removed;
        }

        private static bool DependencyMatches(string candidate, string expected)
        {
            return string.Equals((candidate ?? string.Empty).Trim(), (expected ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void MarkHostOpeningRelationChanged(ProjectElement? host, string openingId, string action)
        {
            if (host == null) return;
            host.MarkDirty(ElementDirtyFlags.Quantity);
            if (host.Category == ElementCategory.GlassWall)
                host.MarkGeneratedCurtainFrameStale("Linked opening " + openingId + " was " + action + ".");
        }

        private static void EnsureOpening(ProjectElement element, string id)
        {
            if (element.Category != ElementCategory.WallOpening && element.Category != ElementCategory.Door)
                throw new InvalidOperationException("Element is not an opening/door: " + id);
        }

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;
    }
}
