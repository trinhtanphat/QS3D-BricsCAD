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

            var hasPreviousHost = opening.Properties.TryGetValue("HostWallId", out var previous);
            var previousHostRaw = hasPreviousHost ? previous ?? string.Empty : string.Empty;
            var previousHost = previousHostRaw.Trim();
            var relationshipChanged = !string.Equals(previousHost, wall.Id, StringComparison.OrdinalIgnoreCase);
            var matchingDependencies = opening.DependsOn.Where(x => DependencyMatches(x, wall.Id)).ToList();
            var propertyCanonical = hasPreviousHost && string.Equals(previousHostRaw, wall.Id, StringComparison.Ordinal);
            var dependencyCanonical = matchingDependencies.Count == 1 && string.Equals(matchingDependencies[0], wall.Id, StringComparison.Ordinal);
            if (!relationshipChanged && propertyCanonical && dependencyCanonical) return;

            ProjectElement? previousHostElement = null;
            if (previousHost.Length > 0 && relationshipChanged)
            {
                previousHostElement = project.FindElement(previousHost);
                EnsureCanLeavePhysicalCutHost(project, opening, previousHostElement, previousHost, "re-host");
            }

            ProjectSemanticMutationExecutor.Execute(project, "host.link", () =>
            {
                if (relationshipChanged)
                    ClearAutoHostMetadata(opening);

                if (previousHost.Length > 0 && relationshipChanged)
                {
                    RemoveDependencies(opening, previousHost);
                    MarkHostOpeningRelationChanged(previousHostElement, opening.Id, "unlinked/re-hosted");
                }

                opening.Properties["HostWallId"] = wall.Id;
                var dependencyAdded = matchingDependencies.Count == 0;
                RemoveDependencies(opening, wall.Id);
                opening.DependsOn.Add(wall.Id);
                opening.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                if (relationshipChanged || dependencyAdded)
                    MarkHostOpeningRelationChanged(wall, opening.Id, "linked/re-hosted");
                else
                    wall.MarkDirty(ElementDirtyFlags.Quantity);
                AuditTrail.ForProject(project).Record("host.link", opening.Id, (previousHost.Length == 0 ? "" : previousHost + " → ") + wall.Id);
                return true;
            });
        }

        public void UnlinkOpening(ProjectState project, string openingId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var opening = project.FindElement(openingId) ?? throw new InvalidOperationException("Opening element not found: " + openingId);
            EnsureOpening(opening, openingId);
            var hasHostProperty = opening.Properties.TryGetValue("HostWallId", out var value);
            if (!hasHostProperty)
            {
                ProjectSemanticMutationExecutor.Execute(project, "host.auto-provenance.clear", () =>
                {
                    if (!ClearAutoHostMetadata(opening)) return false;
                    AuditTrail.ForProject(project).Record("host.auto-provenance.clear", opening.Id, "stale metadata without HostWallId");
                    return true;
                });
                return;
            }

            var hostId = (value ?? string.Empty).Trim();
            var host = hostId.Length > 0 ? project.FindElement(hostId) : null;
            if (hostId.Length > 0)
                EnsureCanLeavePhysicalCutHost(project, opening, host, hostId, "unlink");

            ProjectSemanticMutationExecutor.Execute(project, "host.unlink", () =>
            {
                opening.Properties.Remove("HostWallId");
                ClearAutoHostMetadata(opening);
                var dependencyRemoved = RemoveDependencies(opening, hostId) > 0;
                opening.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                if (host != null)
                {
                    if (dependencyRemoved || !opening.Properties.ContainsKey("HostWallId"))
                        MarkHostOpeningRelationChanged(host, opening.Id, "unlinked");
                    else
                        host.MarkDirty(ElementDirtyFlags.Quantity);
                }
                AuditTrail.ForProject(project).Record("host.unlink", opening.Id, hostId);
                return true;
            });
        }

        private static bool ClearAutoHostMetadata(ProjectElement opening)
        {
            var changed = false;
            changed |= opening.Properties.Remove("AutoHostMatched");
            changed |= opening.Properties.Remove("AutoHostGapM");
            changed |= opening.Properties.Remove("AutoHostElevDeltaM");
            changed |= opening.Properties.Remove("AutoHostCandidateCount");
            return changed;
        }

        private static void EnsureCanLeavePhysicalCutHost(ProjectState project, ProjectElement opening, ProjectElement? host, string hostId, string operation)
        {
            if (host == null) return;

            var hasSolid = host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle", out var solidHandle) && !string.IsNullOrWhiteSpace(solidHandle);
            var hasFingerprint = host.Properties.TryGetValue("PhysicalOpeningCutFingerprint", out var fingerprint) && !string.IsNullOrWhiteSpace(fingerprint);
            var hasTargets = host.Properties.ContainsKey(PhysicalOpeningCutTargetStateCodec.OpeningIdsKey);
            if (!hasSolid && !hasFingerprint && !hasTargets) return;

            if (hasSolid != hasFingerprint)
                throw new InvalidOperationException(
                    "Host " + hostId + " has incomplete physical opening cut state. Rebuild its 3D geometry before " + operation + " of opening " + opening.Id + ".");
            if (!hasSolid && hasTargets)
                throw new InvalidOperationException(
                    "Host " + hostId + " has orphan physical opening target-state. Rebuild its 3D geometry before " + operation + " of opening " + opening.Id + ".");

            // Legacy physical cuts may have been created before the exact opening-id target state was
            // persisted. We cannot prove that this opening is not baked into the host solid, so changing
            // HostWallId must fail closed rather than leaving an irreversible hole behind.
            if (!hasTargets)
                throw new InvalidOperationException(
                    "Host " + hostId + " has a physical opening cut without exact target-state. Rebuild its 3D geometry before " + operation + " of opening " + opening.Id + ".");

            if (!PhysicalOpeningCutTargetStateCodec.TryRead(host, out var targetIds))
                throw new InvalidOperationException(
                    "Host " + hostId + " physical opening target-state is unavailable. Rebuild its 3D geometry before " + operation + ".");

            // Validate the whole target set before trusting absence of this particular opening. A stale
            // or partially corrupt target-set must not be used as evidence that leaving the host is safe.
            PhysicalOpeningCutTargetStateCodec.Resolve(project, host, targetIds);
            if (!targetIds.Any(x => string.Equals(x, opening.Id, StringComparison.OrdinalIgnoreCase))) return;

            throw new InvalidOperationException(
                "Opening " + opening.Id + " is physically boolean-cut into host " + hostId + ". Rebuild the old host 3D geometry first, then " + operation + " and cut the opening on its new host.");
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
            {
                host.MarkGeneratedCurtainFrameStale("Linked opening " + openingId + " was " + action + ".");
                host.MarkGeneratedCurtainPanelStale("Linked opening " + openingId + " was " + action + ".");
            }
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
