using System;
using System.Linq;
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
            if (opening.Category != ElementCategory.WallOpening && opening.Category != ElementCategory.Door) throw new InvalidOperationException("Element is not an opening/door: " + openingId);
            if (!IsWall(wall.Category)) throw new InvalidOperationException("Host is not a wall: " + wallId);

            var previousHost = opening.Properties.TryGetValue("HostWallId", out var previous) ? previous : string.Empty;
            if (!string.IsNullOrWhiteSpace(previousHost) && !string.Equals(previousHost, wall.Id, StringComparison.OrdinalIgnoreCase))
            {
                for (var i = opening.DependsOn.Count - 1; i >= 0; i--)
                    if (string.Equals(opening.DependsOn[i], previousHost, StringComparison.OrdinalIgnoreCase)) opening.DependsOn.RemoveAt(i);
                project.FindElement(previousHost)?.MarkDirty(ElementDirtyFlags.Quantity);
            }

            opening.Properties["HostWallId"] = wall.Id;
            if (!opening.DependsOn.Any(x => string.Equals(x, wall.Id, StringComparison.OrdinalIgnoreCase))) opening.DependsOn.Add(wall.Id);
            opening.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            wall.MarkDirty(ElementDirtyFlags.Quantity);
            project.Touch();
        }

        public void UnlinkOpening(ProjectState project, string openingId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var opening = project.FindElement(openingId) ?? throw new InvalidOperationException("Opening element not found: " + openingId);
            var hostId = opening.Properties.TryGetValue("HostWallId", out var value) ? value : string.Empty;
            opening.Properties.Remove("HostWallId");
            for (var i = opening.DependsOn.Count - 1; i >= 0; i--) if (string.Equals(opening.DependsOn[i], hostId, StringComparison.OrdinalIgnoreCase)) opening.DependsOn.RemoveAt(i);
            opening.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            if (!string.IsNullOrWhiteSpace(hostId)) project.FindElement(hostId)?.MarkDirty(ElementDirtyFlags.Quantity);
            project.Touch();
        }

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;
    }
}
