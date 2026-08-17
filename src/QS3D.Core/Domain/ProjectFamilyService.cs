using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace QS3D.Core.Domain
{
    public sealed class FamilyPropertyUpdateResult
    {
        public int InheritedInstancesUpdated { get; set; }
        public int OverridesPreserved { get; set; }
    }

    public static class ProjectFamilyService
    {
        private sealed class PendingFamilyAssignment
        {
            public ProjectElement Element { get; set; } = null!;
            public IReadOnlyList<KeyValuePair<string, string>> PreviousProperties { get; set; } = Array.Empty<KeyValuePair<string, string>>();
        }

        private const int MaxFamilies = 10000;
        private const int MaxNameLength = 160;
        private const int MaxPropertyKeyLength = 120;
        private const int MaxPropertyValueLength = 1000;
        private const int MaxAssignmentTargetEntries = 10000;

        public static ProjectFamily Create(ProjectState project, string id, string name, ElementCategory category)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedId = Required(id, nameof(id), 80);
            var normalizedName = Required(name, nameof(name), MaxNameLength);
            if (project.Families.Any(x => x == null))
                throw new InvalidOperationException("Project family collection contains a null family.");
            ValidateUniqueFamilyIds(project);
            if (project.Families.Count >= MaxFamilies) throw new InvalidOperationException("Project supports at most " + MaxFamilies + " families.");
            if (project.Families.Any(x => string.Equals(x.Id, normalizedId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Family id already exists: " + normalizedId);
            EnsureUniqueName(project, normalizedName, category, string.Empty);
            var family = new ProjectFamily(normalizedId, normalizedName, category);
            project.Touch();
            project.Families.Add(family);
            return family;
        }

        public static ProjectFamily Duplicate(ProjectState project, string sourceFamilyId, string newId, string newName)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var source = FindRequired(project, sourceFamilyId);
            var properties = SnapshotProperties(source, "Source", "duplication");

            var clone = Create(project, newId, newName, source.Category);
            foreach (var pair in properties) clone.Properties[pair.Key] = pair.Value;
            return clone;
        }

        public static ProjectFamily Rename(ProjectState project, string familyId, string newName)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var family = FindRequired(project, familyId);
            var normalized = Required(newName, nameof(newName), MaxNameLength);
            EnsureUniqueName(project, normalized, family.Category, family.Id);
            if (string.Equals(family.Name, normalized, StringComparison.Ordinal)) return family;
            project.Touch();
            family.Name = normalized;
            return family;
        }

        public static FamilyPropertyUpdateResult SetProperty(ProjectState project, string familyId, string key, string value)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var family = FindRequired(project, familyId);
            ValidatePropertyKeysForMutation(family, "setting a property");
            var normalizedKey = Required(key, nameof(key), MaxPropertyKeyLength);
            var normalizedValue = Value(value, nameof(value), MaxPropertyValueLength);
            var hadPrevious = family.Properties.TryGetValue(normalizedKey, out var previousRaw);
            var previous = previousRaw ?? string.Empty;
            if (hadPrevious && string.Equals(previous, normalizedValue, StringComparison.Ordinal)) return new FamilyPropertyUpdateResult();
            var members = ResolveFamilyMembers(project, family.Id);
            ValidateMemberPropertyKeysForMutation(members, "setting a property");

            project.Touch();
            family.Properties[normalizedKey] = normalizedValue;
            var result = new FamilyPropertyUpdateResult();
            foreach (var element in members)
            {
                var hasInstance = element.Properties.TryGetValue(normalizedKey, out var instanceRaw);
                var instance = instanceRaw ?? string.Empty;
                if (!hasInstance || (hadPrevious && string.Equals(instance, previous, StringComparison.Ordinal)))
                {
                    element.SetProperty(normalizedKey, normalizedValue);
                    result.InheritedInstancesUpdated++;
                }
                else result.OverridesPreserved++;
            }
            return result;
        }

        public static FamilyPropertyUpdateResult RemoveProperty(ProjectState project, string familyId, string key)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var family = FindRequired(project, familyId);
            ValidatePropertyKeysForMutation(family, "removing a property");
            var normalizedKey = Required(key, nameof(key), MaxPropertyKeyLength);
            if (!family.Properties.TryGetValue(normalizedKey, out var previousRaw)) return new FamilyPropertyUpdateResult();
            var previous = previousRaw ?? string.Empty;
            var members = ResolveFamilyMembers(project, family.Id);
            ValidateMemberPropertyKeysForMutation(members, "removing a property");

            project.Touch();
            family.Properties.Remove(normalizedKey);
            var result = new FamilyPropertyUpdateResult();
            foreach (var element in members)
            {
                if (!element.Properties.TryGetValue(normalizedKey, out var instanceRaw)) continue;
                var instance = instanceRaw ?? string.Empty;
                if (string.Equals(instance, previous, StringComparison.Ordinal))
                {
                    element.RemoveProperty(normalizedKey);
                    result.InheritedInstancesUpdated++;
                }
                else result.OverridesPreserved++;
            }
            return result;
        }

        public static int Assign(ProjectState project, string familyId, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var target = FindRequired(project, familyId);
            var targetProperties = SnapshotProperties(target, "Target", "assignment");

            var beforeTargetEnumeration = project.ChangeVersion;
            var owned = ResolveOwnedElements(project, elements, target);
            RequireTargetEnumerationFreshness(project, beforeTargetEnumeration);
            RequireCurrentAssignmentOwnership(project, target, owned);
            targetProperties = SnapshotProperties(target, "Target", "assignment");
            var pending = new List<PendingFamilyAssignment>();
            var previousSnapshots = new Dictionary<string, IReadOnlyList<KeyValuePair<string, string>>>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in owned)
            {
                var previousFamilyId = (element.FamilyId ?? string.Empty).Trim();
                if (string.Equals(previousFamilyId, target.Id, StringComparison.OrdinalIgnoreCase)) continue;
                IReadOnlyList<KeyValuePair<string, string>> previousProperties = Array.Empty<KeyValuePair<string, string>>();
                if (previousFamilyId.Length > 0)
                {
                    var previous = project.FindFamily(previousFamilyId) ??
                        throw new InvalidOperationException("Element " + element.Id + " references missing family id: " + previousFamilyId + ". Repair the relation before reassignment.");
                    if (previous.Category != element.Category)
                        throw new InvalidOperationException("Element " + element.Id + " references previous Family '" + previous.Id + "' category " + previous.Category + " while the element category is " + element.Category + ". Repair the relation before reassignment.");
                    if (!previousSnapshots.TryGetValue(previous.Id, out previousProperties))
                    {
                        previousProperties = SnapshotProperties(previous, "Previous", "assignment");
                        previousSnapshots.Add(previous.Id, previousProperties);
                    }
                }
                pending.Add(new PendingFamilyAssignment { Element = element, PreviousProperties = previousProperties });
            }

            if (pending.Count == 0) return 0;
            ValidateMemberPropertyKeysForMutation(pending.Select(x => x.Element).ToList(), "assigning a Family");
            project.Touch();
            foreach (var item in pending)
            {
                var element = item.Element;
                foreach (var pair in item.PreviousProperties)
                {
                    if (!element.Properties.TryGetValue(pair.Key, out var instance)) continue;
                    if (string.Equals(instance, pair.Value, StringComparison.Ordinal)) element.Properties.Remove(pair.Key);
                }
                element.FamilyId = target.Id;
                foreach (var pair in targetProperties)
                    if (!element.Properties.ContainsKey(pair.Key)) element.Properties[pair.Key] = pair.Value;
                element.MarkDirty(ElementDirtyFlags.All);
            }
            return pending.Count;
        }

        public static bool Delete(ProjectState project, string familyId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var family = FindRequired(project, familyId);
            var references = ResolveFamilyMembers(project, family.Id).Count;
            if (references > 0)
                throw new InvalidOperationException("Family '" + family.Name + "' is referenced by " + references + " semantic element(s). Reassign them before deletion.");
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var active) && string.Equals((active ?? string.Empty).Trim(), family.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot delete the active Family. Activate another Family first.");
            project.Touch();
            return project.Families.Remove(family);
        }

        public static int ReferenceCount(ProjectState project, string familyId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var family = FindRequired(project, familyId);
            return ResolveFamilyMembers(project, family.Id).Count;
        }

        internal static IReadOnlyList<KeyValuePair<string, string>> SnapshotProperties(ProjectFamily family, string role, string repairOperation)
        {
            if (family == null) throw new ArgumentNullException(nameof(family));
            var normalizedRole = Required(role, nameof(role), 40);
            var normalizedOperation = Required(repairOperation, nameof(repairOperation), 80);
            var parameterPrefix = normalizedRole.ToLowerInvariant();
            var properties = new List<KeyValuePair<string, string>>();
            var canonicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in family.Properties)
            {
                var normalizedKey = Required(pair.Key, parameterPrefix + " property key", MaxPropertyKeyLength);
                if (!string.Equals(normalizedKey, pair.Key, StringComparison.Ordinal))
                    throw new InvalidOperationException(normalizedRole + " Family contains a non-canonical property key: '" + pair.Key + "'. Repair the Family before " + normalizedOperation + ".");
                if (!canonicalKeys.Add(normalizedKey))
                    throw new InvalidOperationException(normalizedRole + " Family contains duplicate canonical property key: " + normalizedKey);
                properties.Add(new KeyValuePair<string, string>(normalizedKey, Value(pair.Value, parameterPrefix + " property value", MaxPropertyValueLength)));
            }

            return properties.AsReadOnly();
        }

        private static void ValidatePropertyKeysForMutation(ProjectFamily family, string repairOperation)
        {
            var canonicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in family.Properties)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new InvalidOperationException("Target Family contains an empty property key. Repair the Family before " + repairOperation + ".");
                var normalizedKey = pair.Key.Trim();
                if (!string.Equals(normalizedKey, pair.Key, StringComparison.Ordinal))
                    throw new InvalidOperationException("Target Family contains a non-canonical property key: '" + pair.Key + "'. Repair the Family before " + repairOperation + ".");
                if (!canonicalKeys.Add(normalizedKey))
                    throw new InvalidOperationException("Target Family contains duplicate canonical property key: " + normalizedKey);
            }
        }

        internal static void ValidateMemberPropertyKeysForMutation(IReadOnlyList<ProjectElement> members, string repairOperation)
        {
            foreach (var element in members)
            {
                var canonicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in element.Properties)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        throw new InvalidOperationException("Family member '" + element.Id + "' contains an empty property key. Repair the element before " + repairOperation + ".");
                    var normalizedKey = pair.Key.Trim();
                    if (!string.Equals(normalizedKey, pair.Key, StringComparison.Ordinal))
                        throw new InvalidOperationException("Family member '" + element.Id + "' contains a non-canonical property key: '" + pair.Key + "'. Repair the element before " + repairOperation + ".");
                    if (!canonicalKeys.Add(normalizedKey))
                        throw new InvalidOperationException("Family member '" + element.Id + "' contains duplicate canonical property key: " + normalizedKey);
                }
            }
        }

        private static IReadOnlyList<ProjectElement> ResolveFamilyMembers(ProjectState project, string familyId)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<ProjectElement>();
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (!ids.Add(element.Id)) throw new InvalidOperationException("Project contains duplicate semantic element id: " + element.Id);
                if (string.Equals((element.FamilyId ?? string.Empty).Trim(), familyId, StringComparison.OrdinalIgnoreCase)) result.Add(element);
            }
            result.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id));
            return result.AsReadOnly();
        }

        private static IReadOnlyList<ProjectElement> ResolveOwnedElements(ProjectState project, IEnumerable<ProjectElement> elements, ProjectFamily target)
        {
            var projectElements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var projectElement in project.Elements)
            {
                if (projectElement == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (projectElements.ContainsKey(projectElement.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + projectElement.Id);
                projectElements[projectElement.Id] = projectElement;
            }

            var targetEnumerationVersion = project.ChangeVersion;
            RequireAssignmentTargetCountWithinLimit(elements);
            if (project.ChangeVersion != targetEnumerationVersion)
                throw new InvalidOperationException("Project changed while Family assignment targets were being counted. Retry the operation against the current project state.");

            var unique = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var observedEntries = 0;
            foreach (var element in elements)
            {
                observedEntries++;
                if (observedEntries > MaxAssignmentTargetEntries)
                    throw AssignmentTargetLimitExceeded();
                if (element == null) throw new ArgumentException("Family assignment elements cannot contain null entries.", nameof(elements));
                if (!projectElements.TryGetValue(element.Id, out var owned) || !ReferenceEquals(owned, element))
                    throw new InvalidOperationException("Element does not belong to the project instance: " + element.Id);
                if (owned.Category != target.Category)
                    throw new InvalidOperationException("Family '" + target.Name + "' category " + target.Category + " cannot be assigned to element " + owned.Id + " category " + owned.Category + ".");
                unique[owned.Id] = owned;
            }
            if (project.ChangeVersion != targetEnumerationVersion)
                throw new InvalidOperationException("Project changed while Family assignment targets were being enumerated. Retry the operation against the current project state.");
            return unique.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static void RequireAssignmentTargetCountWithinLimit(IEnumerable<ProjectElement> elements)
        {
            if (elements is ICollection<ProjectElement> collection)
                RequireValidAssignmentTargetKnownCount(collection.Count);
            if (elements is IReadOnlyCollection<ProjectElement> readOnlyCollection)
                RequireValidAssignmentTargetKnownCount(readOnlyCollection.Count);
            if (elements is System.Collections.ICollection nonGenericCollection)
                RequireValidAssignmentTargetKnownCount(nonGenericCollection.Count);
        }

        private static void RequireValidAssignmentTargetKnownCount(int count)
        {
            if (count < 0)
                throw new InvalidOperationException("Family assignment target collection reported an invalid negative known count.");
            if (count > MaxAssignmentTargetEntries)
                throw AssignmentTargetLimitExceeded();
        }

        private static InvalidOperationException AssignmentTargetLimitExceeded()
        {
            return new InvalidOperationException(
                "Family assignment supports at most " + MaxAssignmentTargetEntries + " target entries per operation.");
        }

        private static void RequireTargetEnumerationFreshness(ProjectState project, long beforeEnumeration)
        {
            if (project.ChangeVersion != beforeEnumeration)
                throw new InvalidOperationException("Project changed while Family assignment targets were being enumerated.");
        }

        private static void RequireCurrentAssignmentOwnership(ProjectState project, ProjectFamily target, IReadOnlyList<ProjectElement> elements)
        {
            ValidateUniqueFamilyIds(project);
            var currentElements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var projectElement in project.Elements)
            {
                if (projectElement == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (currentElements.ContainsKey(projectElement.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + projectElement.Id);
                currentElements[projectElement.Id] = projectElement;
            }

            var currentTarget = project.FindFamily(target.Id);
            if (!ReferenceEquals(currentTarget, target))
                throw new InvalidOperationException("Target Family no longer belongs to the project after assignment target enumeration: " + target.Id + ".");

            foreach (var element in elements)
            {
                if (!currentElements.TryGetValue(element.Id, out var current) || !ReferenceEquals(current, element))
                    throw new InvalidOperationException("Element no longer belongs to the project after Family assignment target enumeration: " + element.Id + ".");
                if (element.Category != target.Category)
                    throw new InvalidOperationException("Family '" + target.Name + "' category " + target.Category + " cannot be assigned to element " + element.Id + " category " + element.Category + ".");
            }
        }

        private static ProjectFamily FindRequired(ProjectState project, string id)
        {
            var normalized = Required(id, nameof(id), 80);
            ValidateUniqueFamilyIds(project);
            return project.FindFamily(normalized) ?? throw new InvalidOperationException("Family not found: " + normalized);
        }

        private static void ValidateUniqueFamilyIds(ProjectState project)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null)
                    throw new InvalidOperationException("Project family collection contains a null family.");
                if (!seenIds.Add(family.Id))
                    throw new InvalidOperationException("Project contains duplicate family id: " + family.Id + ".");
            }
        }

        private static void EnsureUniqueName(ProjectState project, string name, ElementCategory category, string exceptId)
        {
            if (project.Families.Any(x => x.Category == category && !string.Equals(x.Id, exceptId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Another " + category + " Family already uses the name '" + name + "'.");
        }

        private static string Required(string value, string parameterName, int maxLength)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0 || text.Length > maxLength) throw new ArgumentException(parameterName + " must contain 1.." + maxLength + " characters.", parameterName);
            if (text.Any(char.IsControl)) throw new ArgumentException(parameterName + " cannot contain control characters.", parameterName);
            try
            {
                XmlConvert.VerifyXmlChars(text);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(parameterName + " contains characters that are invalid in XML.", parameterName, ex);
            }
            return text;
        }

        private static string Value(string value, string parameterName, int maxLength)
        {
            var text = value ?? string.Empty;
            if (text.Length > maxLength) throw new ArgumentException(parameterName + " must contain at most " + maxLength + " characters.", parameterName);
            try
            {
                XmlConvert.VerifyXmlChars(text);
            }
            catch (XmlException)
            {
                throw new ArgumentException(parameterName + " contains characters that are invalid in XML.", parameterName);
            }
            return text;
        }
    }
}
