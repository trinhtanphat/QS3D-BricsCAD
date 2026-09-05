using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Domain
{
    public static class AutoRoomLifecycle
    {
        public const string BoundaryModeKey = "BoundaryMode";
        public const string BoundaryModeAutoNetwork = "AutoNetwork";
        public const string BoundaryStateKey = "BoundaryState";
        public const string BoundaryStateActive = "Active";
        public const string BoundaryStateStale = "Stale";
        public const string BoundarySourceHandlesKey = "BoundarySourceHandles";
        public const string BoundarySourceSignatureKey = "BoundarySourceSignature";
        public const string RoomSourceIdKey = "RoomSourceId";
        private const string FamilyDefaultSnapshotPrefix = "AutoRoomFamilyDefault:";
        private const int MaxSourceHandleInputCount = 5000;

        private static readonly string[] RoomReferencePropertyKeys =
        {
            RoomSourceIdKey,
            "ParentRoomId",
            "SourceRoomId",
            "GeneratedFromRoomId",
            "RoomId"
        };

        public static bool IsAutoRoom(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return element.Category == ElementCategory.Room &&
                   element.Properties.TryGetValue(BoundaryModeKey, out var mode) &&
                   string.Equals(mode, BoundaryModeAutoNetwork, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsStaleAutoRoom(ProjectElement element)
        {
            if (!IsAutoRoom(element)) return false;
            return element.Properties.TryGetValue(BoundaryStateKey, out var state) &&
                   string.Equals(state, BoundaryStateStale, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRoomFinishCategory(ElementCategory category)
        {
            return category == ElementCategory.FloorFinish ||
                   category == ElementCategory.Waterproofing ||
                   category == ElementCategory.Skirting ||
                   category == ElementCategory.WallFinish ||
                   category == ElementCategory.CeilingFinish;
        }

        public static string NormalizeSourceHandles(IEnumerable<string> handles)
        {
            if (handles == null) throw new ArgumentNullException(nameof(handles));

            var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inputCount = 0;
            foreach (var raw in handles)
            {
                if (inputCount >= MaxSourceHandleInputCount)
                    throw new InvalidOperationException(
                        "Auto Room source handles cannot exceed " + MaxSourceHandleInputCount + " input entries.");
                inputCount++;
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var canonical = CanonicalizeSourceHandle(raw);
                if (canonical.Length > 0) normalized.Add(canonical);
            }

            return string.Join(";", normalized.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        private static string CanonicalizeSourceHandle(string raw)
        {
            var canonical = GeneratedHandleIdentity.Normalize(raw);
            if (canonical.Length == 0) return canonical;

            var trimmed = raw.Trim();
            if (!string.Equals(canonical, trimmed, StringComparison.Ordinal)) return canonical;

            return canonical.Any(ch => !char.IsLetterOrDigit(ch))
                ? canonical.ToUpperInvariant()
                : canonical;
        }

        private static IReadOnlyList<string> ParseSourceHandleText(string? raw)
        {
            var source = raw ?? string.Empty;
            var handles = new List<string>();
            var tokenStart = 0;

            for (var index = 0; index <= source.Length; index++)
            {
                if (index < source.Length && source[index] != ';') continue;

                var tokenLength = index - tokenStart;
                if (tokenLength == 0)
                {
                    tokenStart = index + 1;
                    continue;
                }
                if (handles.Count >= MaxSourceHandleInputCount)
                    throw new InvalidOperationException(
                        "Auto Room source handles cannot exceed " + MaxSourceHandleInputCount + " input entries.");

                handles.Add(source.Substring(tokenStart, tokenLength));
                tokenStart = index + 1;
            }

            return handles.AsReadOnly();
        }

        private static string NormalizeSourceHandleText(string? raw) =>
            NormalizeSourceHandles(ParseSourceHandleText(raw));

        public static string SourceSignature(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Properties.TryGetValue(BoundarySourceSignatureKey, out var signature) && !string.IsNullOrWhiteSpace(signature))
                return NormalizeSourceHandleText(signature);
            if (!element.Properties.TryGetValue(BoundarySourceHandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return NormalizeSourceHandleText(raw);
        }

        public static string ResolveRoomReferenceId(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in RoomReferencePropertyKeys)
            {
                if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                candidates.Add(CanonicalRoomReferenceId(raw, element, key));
            }

            foreach (var dependencyRaw in element.DependsOn)
            {
                if (string.IsNullOrWhiteSpace(dependencyRaw)) continue;
                var dependencyId = CanonicalRoomReferenceId(dependencyRaw, element, "DependsOn");
                var dependency = project.FindElement(dependencyId);
                if (dependency != null && dependency.Category == ElementCategory.Room)
                    candidates.Add(dependency.Id);
                else if (dependency == null && IsRoomFinishCategory(element.Category))
                    candidates.Add(dependencyId);
            }

            if (candidates.Count > 1)
                throw new InvalidOperationException("Conflicting room provenance on " + element.Id + ": " + string.Join(";", candidates.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));
            return candidates.Count == 1 ? candidates.First() : string.Empty;
        }

        public static ProjectElement? FindBySourceSignature(ProjectState project, string signature, string floorId, string zoneId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = NormalizeSourceHandleText(signature);
            if (normalized.Length == 0) return null;
            var matches = ResolveProjectElements(project)
                .Where(IsAutoRoom)
                .Where(x => SameScopeId(x.FloorId, floorId))
                .Where(x => SameScopeId(x.ZoneId, zoneId))
                .Where(x => string.Equals(SourceSignature(x), normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (matches.Count > 1) throw new InvalidOperationException("Multiple auto rooms share the same boundary source signature: " + normalized);
            return matches.Count == 1 ? matches[0] : null;
        }

        public static IReadOnlyList<ProjectElement> MarkStaleForSelection(ProjectState project, ISet<string> activeRoomIds, ISet<string> selectedSourceHandles, string floorId, string zoneId, DateTime utcNow)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (activeRoomIds == null) throw new ArgumentNullException(nameof(activeRoomIds));
            if (selectedSourceHandles == null) throw new ArgumentNullException(nameof(selectedSourceHandles));
            if (utcNow.Kind != DateTimeKind.Utc) throw new ArgumentException("utcNow must have DateTimeKind.Utc.", nameof(utcNow));
            var knownSelectedSourceHandleCount = selectedSourceHandles.Count;
            if (knownSelectedSourceHandleCount > MaxSourceHandleInputCount)
                throw new InvalidOperationException(
                    "Auto Room source handles cannot exceed " + MaxSourceHandleInputCount + " input entries.");

            var inputVersion = project.ChangeVersion;
            var knownActiveRoomCount = activeRoomIds.Count;
            var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var activeInputCount = 0;
            using (var activeEnumerator = activeRoomIds.GetEnumerator())
            {
                while (activeEnumerator.MoveNext())
                {
                    RequireCanProcessNextKnownCount("Auto Room active room id set", knownActiveRoomCount, activeInputCount);
                    var rawRoomId = activeEnumerator.Current;
                    activeInputCount++;
                    if (string.IsNullOrWhiteSpace(rawRoomId)) continue;
                    active.Add(rawRoomId.Trim());
                }
            }
            RequireKnownCountMatchesTraversal("Auto Room active room id set", knownActiveRoomCount, activeInputCount);

            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var selectedInputCount = 0;
            using (var selectedEnumerator = selectedSourceHandles.GetEnumerator())
            {
                while (selectedEnumerator.MoveNext())
                {
                    if (selectedInputCount >= MaxSourceHandleInputCount)
                        throw new InvalidOperationException(
                            "Auto Room source handles cannot exceed " + MaxSourceHandleInputCount + " input entries.");
                    if (selectedInputCount >= knownSelectedSourceHandleCount)
                    {
                        selectedInputCount++;
                        continue;
                    }

                    var raw = selectedEnumerator.Current;
                    selectedInputCount++;
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var canonical = GeneratedHandleIdentity.Normalize(raw);
                    if (canonical.Length > 0) selected.Add(canonical);
                }
            }
            RequireKnownCountMatchesTraversal("Auto Room selected source handle set", knownSelectedSourceHandleCount, selectedInputCount);
            if (project.ChangeVersion != inputVersion)
                throw new InvalidOperationException("Project changed while Auto Room stale-selection inputs were being enumerated. Retry against the current project state.");
            var stale = ResolveProjectElements(project)
                .Where(IsAutoRoom)
                .Where(room => !active.Contains(room.Id))
                .Where(room => SameScopeId(room.FloorId, floorId))
                .Where(room => SameScopeId(room.ZoneId, zoneId))
                .Where(room =>
                {
                    var handles = ParseSourceHandleText(SourceSignature(room));
                    return handles.Count > 0 && handles.All(selected.Contains);
                })
                .Where(room => !HasCanonicalTopologyStaleMetadata(room))
                .OrderBy(room => room.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (stale.Count == 0) return stale;

            project.Touch();
            foreach (var room in stale)
            {
                room.Properties[BoundaryStateKey] = BoundaryStateStale;
                room.Properties["BoundaryStaleUtc"] = utcNow.ToString("O");
                room.Properties["BoundaryStaleReason"] = "TopologyChanged";
                room.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
            }
            return stale;
        }

        public static void MarkActive(ProjectElement room, string sourceSignature)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            var normalizedSourceSignature = NormalizeSourceHandleText(sourceSignature);
            room.SetProperty(BoundarySourceSignatureKey, normalizedSourceSignature);
            room.SetProperty(BoundaryStateKey, BoundaryStateActive);
            room.RemoveProperty("BoundaryStaleUtc");
            room.RemoveProperty("BoundaryStaleReason");
        }

        public static int SyncFamilyDefaults(ProjectState project, ProjectElement room, ProjectFamily family)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (family == null) throw new ArgumentNullException(nameof(family));
            var metadata = project.Metadata as ProjectMetadataDictionary
                ?? throw new InvalidOperationException("Auto-room family synchronization requires the canonical project metadata store.");

            ResolveProjectElements(project);
            ValidateUniqueFamilyIds(project);
            var ownedRoom = project.FindElement(room.Id) ?? throw new InvalidOperationException("Room does not belong to the project: " + room.Id);
            if (!ReferenceEquals(ownedRoom, room))
                throw new InvalidOperationException("Room instance does not belong to the project: " + room.Id);
            var ownedFamily = project.FindFamily(family.Id) ?? throw new InvalidOperationException("Family does not belong to the project: " + family.Id);
            if (!ReferenceEquals(ownedFamily, family))
                throw new InvalidOperationException("Family instance does not belong to the project: " + family.Id);
            if (room.Category != ElementCategory.Room || family.Category != ElementCategory.Room)
                throw new InvalidOperationException("Auto-room family synchronization requires Room category values.");

            var familyProperties = ProjectFamilyService.SnapshotProperties(family, "Target", "auto-room synchronization");
            var previousFamilyId = (room.FamilyId ?? string.Empty).Trim();
            var familyChanged = !string.Equals(previousFamilyId, family.Id, StringComparison.OrdinalIgnoreCase);
            ProjectFamily? previousFamily = null;
            if (familyChanged && previousFamilyId.Length > 0)
            {
                previousFamily = project.FindFamily(previousFamilyId) ??
                    throw new InvalidOperationException(
                        "Room " + room.Id + " references missing family id: " + previousFamilyId +
                        ". Repair the relation before Auto Room family synchronization.");
                if (previousFamily.Category != room.Category)
                    throw new InvalidOperationException(
                        "Room " + room.Id + " references previous Family '" + previousFamily.Id + "' category " + previousFamily.Category +
                        " while the room category is " + room.Category + ". Repair the relation before Auto Room family synchronization.");
            }
            var previousFamilyProperties = previousFamily != null
                ? ProjectFamilyService.SnapshotProperties(previousFamily, "Previous", "auto-room synchronization")
                : Array.Empty<KeyValuePair<string, string>>();
            var previousFamilyMap = previousFamilyProperties.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            var prefix = FamilyDefaultSnapshotPrefix + room.Id + ":";
            var currentFamilyKeys = new HashSet<string>(familyProperties.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var roomSets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var roomRemoves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var metadataSets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var metadataRemoves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changed = 0;

            foreach (var property in familyProperties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var key = property.Key;
                var nextDefault = property.Value;
                var snapshotKey = prefix + key;
                var hasCurrent = room.Properties.TryGetValue(key, out var currentValue);
                var inherited = !hasCurrent;

                if (hasCurrent && project.Metadata.TryGetValue(snapshotKey, out var previousSnapshot) &&
                    string.Equals(currentValue, previousSnapshot, StringComparison.Ordinal))
                {
                    inherited = true;
                }
                else if (hasCurrent && previousFamilyMap.TryGetValue(key, out var previousDefault) &&
                         string.Equals(currentValue, previousDefault, StringComparison.Ordinal))
                {
                    inherited = true;
                }

                if (inherited && (!hasCurrent || !string.Equals(currentValue, nextDefault, StringComparison.Ordinal)))
                {
                    roomSets[key] = nextDefault;
                    changed++;
                }

                if (!project.Metadata.TryGetValue(snapshotKey, out var storedDefault) || !string.Equals(storedDefault, nextDefault, StringComparison.Ordinal))
                    metadataSets[snapshotKey] = nextDefault;
            }

            foreach (var previousProperty in previousFamilyProperties)
            {
                if (currentFamilyKeys.Contains(previousProperty.Key)) continue;
                if (room.Properties.TryGetValue(previousProperty.Key, out var currentValue) &&
                    string.Equals(currentValue, previousProperty.Value, StringComparison.Ordinal) &&
                    roomRemoves.Add(previousProperty.Key))
                {
                    changed++;
                }
            }

            var staleSnapshots = project.Metadata
                .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Where(x => !currentFamilyKeys.Contains(x.Key.Substring(prefix.Length)))
                .ToList();
            foreach (var snapshot in staleSnapshots)
            {
                var propertyName = snapshot.Key.Substring(prefix.Length);
                if (!roomRemoves.Contains(propertyName) &&
                    room.Properties.TryGetValue(propertyName, out var currentValue) &&
                    string.Equals(currentValue, snapshot.Value, StringComparison.Ordinal))
                {
                    roomRemoves.Add(propertyName);
                    changed++;
                }
                metadataRemoves.Add(snapshot.Key);
            }

            if (familyChanged) changed++;
            if (changed == 0 && metadataSets.Count == 0 && metadataRemoves.Count == 0) return 0;

            metadata.EnsureCanApplyOwned(metadataRemoves, metadataSets.Keys);
            project.Touch();
            foreach (var key in roomRemoves) room.Properties.Remove(key);
            foreach (var property in roomSets) room.Properties[property.Key] = property.Value;
            foreach (var key in metadataRemoves) metadata.RemoveOwned(key);
            foreach (var property in metadataSets) metadata.SetOwned(property.Key, property.Value);
            if (familyChanged) room.FamilyId = family.Id;
            if (changed > 0) room.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
            return changed;
        }

        public static bool IsExcludedFromQuantity(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (IsStaleAutoRoom(element)) return true;
            if (HasStaleAutoRoomAncestor(project, element, new HashSet<string>(StringComparer.OrdinalIgnoreCase))) return true;

            if (IsRoomFinishCategory(element.Category))
            {
                var roomId = ResolveRoomReferenceId(project, element);
                if (roomId.Length > 0)
                {
                    var room = project.FindElement(roomId);
                    if (room == null || room.Category != ElementCategory.Room) return true;
                    if (IsStaleAutoRoom(room)) return true;
                    if (!SameScopeId(room.FloorId, element.FloorId) ||
                        !SameScopeId(room.ZoneId, element.ZoneId)) return true;
                }
            }
            return false;
        }

        private static bool SameScopeId(string? left, string? right)
        {
            return string.Equals((left ?? string.Empty).Trim(), (right ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasCanonicalTopologyStaleMetadata(ProjectElement room)
        {
            if (!room.Properties.TryGetValue(BoundaryStateKey, out var state) ||
                !string.Equals(state, BoundaryStateStale, StringComparison.Ordinal))
                return false;
            if (!room.Properties.TryGetValue("BoundaryStaleReason", out var reason) ||
                !string.Equals(reason, "TopologyChanged", StringComparison.Ordinal))
                return false;
            if (!room.Properties.TryGetValue("BoundaryStaleUtc", out var staleUtc) ||
                !DateTime.TryParseExact(
                    staleUtc,
                    "O",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var parsed) ||
                parsed.Kind != DateTimeKind.Utc)
                return false;
            return string.Equals(staleUtc, parsed.ToString("O"), StringComparison.Ordinal);
        }

        private static void RequireCanProcessNextKnownCount(string collectionLabel, int knownCount, int observedCount)
        {
            if (observedCount < knownCount) return;
            throw new InvalidOperationException(
                collectionLabel + " traversal produced more entries than its known count reported " + knownCount + ".");
        }

        private static void RequireKnownCountMatchesTraversal(string collectionLabel, int knownCount, int observedCount)
        {
            if (knownCount == observedCount) return;
            throw new InvalidOperationException(
                collectionLabel + " traversal produced " + observedCount +
                " entries but its known count reported " + knownCount + ".");
        }

        private static void ValidateUniqueFamilyIds(ProjectState project)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null)
                    throw new InvalidOperationException("Project contains a null Family entry.");
                if (!seenIds.Add(family.Id))
                    throw new InvalidOperationException("Project contains duplicate Family id: " + family.Id + ".");
            }
        }

        private static IReadOnlyList<ProjectElement> ResolveProjectElements(ProjectState project)
        {
            var resolved = new List<ProjectElement>(project.Elements.Count);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("Project contains an element with a blank semantic id.");
                if (!ids.Add(elementId))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + elementId);
                resolved.Add(element);
            }
            return resolved.AsReadOnly();
        }

        private static string CanonicalRoomReferenceId(string raw, ProjectElement element, string source)
        {
            var canonical = raw.Trim();
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Room provenance id on " + element.Id + "/" + source +
                    " must be canonical without surrounding whitespace.");
            return canonical;
        }

        private static bool HasStaleAutoRoomAncestor(ProjectState project, ProjectElement element, ISet<string> visited)
        {
            if (!visited.Add(element.Id)) return false;
            foreach (var dependencyRaw in element.DependsOn)
            {
                if (string.IsNullOrWhiteSpace(dependencyRaw)) continue;
                var dependencyId = CanonicalRoomReferenceId(dependencyRaw, element, "DependsOn");
                var dependency = project.FindElement(dependencyId);
                if (dependency == null) continue;
                if (IsStaleAutoRoom(dependency)) return true;
                if (HasStaleAutoRoomAncestor(project, dependency, visited)) return true;
            }
            return false;
        }
    }
}
