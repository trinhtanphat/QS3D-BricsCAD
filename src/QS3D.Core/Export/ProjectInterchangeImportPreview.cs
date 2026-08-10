using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public enum InterchangeIdentityKind { Zone = 0, Floor = 1, Family = 2, Element = 3 }
    public enum InterchangeIdentityDisposition { New = 0, ExistingNeedsPolicy = 1, ExistingIncompatible = 2 }
    public enum InterchangeDrawingFingerprintRelation { Unknown = 0, Match = 1, Different = 2 }

    public sealed class InterchangeImportPreviewItem
    {
        public InterchangeImportPreviewItem(InterchangeIdentityKind kind, string id, InterchangeIdentityDisposition disposition, string reason, string sourceCategory = "", string targetCategory = "")
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Disposition = disposition;
            Reason = reason ?? string.Empty;
            SourceCategory = sourceCategory ?? string.Empty;
            TargetCategory = targetCategory ?? string.Empty;
        }

        public InterchangeIdentityKind Kind { get; }
        public string Id { get; }
        public InterchangeIdentityDisposition Disposition { get; }
        public string Reason { get; }
        public string SourceCategory { get; }
        public string TargetCategory { get; }
    }

    public sealed class ProjectInterchangeImportPreviewResult
    {
        internal ProjectInterchangeImportPreviewResult(ProjectInterchangeValidationResult validation, string sourceProjectId, string targetProjectId, bool sameProjectId, InterchangeDrawingFingerprintRelation drawingFingerprintRelation, int totalIdentityCount, int newIdentityCount, int policyCollisionCount, int incompatibleCollisionCount, bool detailsTruncated, IEnumerable<InterchangeImportPreviewItem> items)
        {
            Validation = validation ?? throw new ArgumentNullException(nameof(validation));
            SourceProjectId = sourceProjectId ?? string.Empty;
            TargetProjectId = targetProjectId ?? string.Empty;
            SameProjectId = sameProjectId;
            DrawingFingerprintRelation = drawingFingerprintRelation;
            TotalIdentityCount = totalIdentityCount;
            NewIdentityCount = newIdentityCount;
            PolicyCollisionCount = policyCollisionCount;
            IncompatibleCollisionCount = incompatibleCollisionCount;
            DetailsTruncated = detailsTruncated;
            Items = (items ?? Enumerable.Empty<InterchangeImportPreviewItem>()).ToList().AsReadOnly();
        }

        public ProjectInterchangeValidationResult Validation { get; }
        public string SourceProjectId { get; }
        public string TargetProjectId { get; }
        public bool SameProjectId { get; }
        public InterchangeDrawingFingerprintRelation DrawingFingerprintRelation { get; }
        public int TotalIdentityCount { get; }
        public int NewIdentityCount { get; }
        public int PolicyCollisionCount { get; }
        public int IncompatibleCollisionCount { get; }
        public int CollisionCount => PolicyCollisionCount + IncompatibleCollisionCount;
        public bool DetailsTruncated { get; }
        public IReadOnlyList<InterchangeImportPreviewItem> Items { get; }
        public bool RequiresIdentityPolicy => CollisionCount > 0;
    }

    public static class ProjectInterchangeImportPreview
    {
        public const int MaxDetailedItems = 10000;

        public static ProjectInterchangeImportPreviewResult Plan(ProjectState targetProject, string json)
        {
            if (targetProject == null) throw new ArgumentNullException(nameof(targetProject));
            var validation = ProjectInterchangeJsonValidator.Validate(json);
            if (!validation.IsValid)
                return new ProjectInterchangeImportPreviewResult(validation, string.Empty, targetProject.ProjectId, false, InterchangeDrawingFingerprintRelation.Unknown, 0, 0, 0, 0, false, Array.Empty<InterchangeImportPreviewItem>());

            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var sourceProjectId = source.Project.Id;
            var sameProjectId = string.Equals(sourceProjectId, targetProject.ProjectId, StringComparison.OrdinalIgnoreCase);
            var fingerprintRelation = CompareFingerprint(source.Project.DrawingFingerprint, targetProject.DrawingFingerprint);
            var targetZones = UniqueIndex(targetProject.Zones, x => x.Id, "target Zone");
            var targetFloors = UniqueIndex(targetProject.Floors, x => x.Id, "target Floor");
            var targetFamilies = UniqueIndex(targetProject.Families, x => x.Id, "target Family");
            var targetElements = UniqueIndex(targetProject.Elements, x => x.Id, "target element");
            var items = new List<InterchangeImportPreviewItem>(Math.Min(MaxDetailedItems, source.Zones.Count + source.Floors.Count + source.Families.Count + source.Elements.Count));
            var total = 0;
            var newCount = 0;
            var policyCount = 0;
            var incompatibleCount = 0;

            foreach (var zone in source.Zones)
                AddSimple(InterchangeIdentityKind.Zone, zone.Id, targetZones.ContainsKey(zone.Id), items, ref total, ref newCount, ref policyCount);

            foreach (var floor in source.Floors)
                AddSimple(InterchangeIdentityKind.Floor, floor.Id, targetFloors.ContainsKey(floor.Id), items, ref total, ref newCount, ref policyCount);

            foreach (var family in source.Families)
            {
                if (!targetFamilies.TryGetValue(family.Id, out var existing))
                {
                    AddDetail(new InterchangeImportPreviewItem(InterchangeIdentityKind.Family, family.Id, InterchangeIdentityDisposition.New, "No target Family uses this semantic id.", family.Category.ToString()), items);
                    total++; newCount++;
                }
                else if (existing.Category != family.Category)
                {
                    AddDetail(new InterchangeImportPreviewItem(InterchangeIdentityKind.Family, family.Id, InterchangeIdentityDisposition.ExistingIncompatible, "The target Family uses the same id with a different category; automatic merge must fail closed.", family.Category.ToString(), existing.Category.ToString()), items);
                    total++; incompatibleCount++;
                }
                else
                {
                    AddDetail(new InterchangeImportPreviewItem(InterchangeIdentityKind.Family, family.Id, InterchangeIdentityDisposition.ExistingNeedsPolicy, "The target already contains this Family id. Property/name merge semantics require an explicit import policy.", family.Category.ToString(), existing.Category.ToString()), items);
                    total++; policyCount++;
                }
            }

            foreach (var element in source.Elements)
            {
                if (!targetElements.TryGetValue(element.Id, out var existing))
                {
                    AddDetail(new InterchangeImportPreviewItem(InterchangeIdentityKind.Element, element.Id, InterchangeIdentityDisposition.New, "No target semantic element uses this id.", element.Category.ToString()), items);
                    total++; newCount++;
                }
                else if (existing.Category != element.Category)
                {
                    AddDetail(new InterchangeImportPreviewItem(InterchangeIdentityKind.Element, element.Id, InterchangeIdentityDisposition.ExistingIncompatible, "The target element uses the same id with a different category; automatic merge must fail closed.", element.Category.ToString(), existing.Category.ToString()), items);
                    total++; incompatibleCount++;
                }
                else
                {
                    AddDetail(new InterchangeImportPreviewItem(InterchangeIdentityKind.Element, element.Id, InterchangeIdentityDisposition.ExistingNeedsPolicy, "The target already contains this semantic element id. Geometry/provenance/property merge semantics require an explicit import policy.", element.Category.ToString(), existing.Category.ToString()), items);
                    total++; policyCount++;
                }
            }

            return new ProjectInterchangeImportPreviewResult(validation, sourceProjectId, targetProject.ProjectId, sameProjectId, fingerprintRelation, total, newCount, policyCount, incompatibleCount, total > items.Count, items);
        }

        private static void AddSimple(InterchangeIdentityKind kind, string id, bool exists, ICollection<InterchangeImportPreviewItem> items, ref int total, ref int newCount, ref int policyCount)
        {
            AddDetail(new InterchangeImportPreviewItem(kind, id, exists ? InterchangeIdentityDisposition.ExistingNeedsPolicy : InterchangeIdentityDisposition.New, exists ? "The target already contains this semantic id; rename/merge/replace behavior requires an explicit import policy." : "No target semantic definition uses this id."), items);
            total++;
            if (exists) policyCount++; else newCount++;
        }

        private static void AddDetail(InterchangeImportPreviewItem item, ICollection<InterchangeImportPreviewItem> items)
        {
            if (items.Count < MaxDetailedItems) items.Add(item);
        }

        private static InterchangeDrawingFingerprintRelation CompareFingerprint(string? source, string? target)
        {
            var left = source ?? string.Empty;
            var right = target ?? string.Empty;
            if (left.Length == 0 || right.Length == 0) return InterchangeDrawingFingerprintRelation.Unknown;
            return string.Equals(left, right, StringComparison.Ordinal) ? InterchangeDrawingFingerprintRelation.Match : InterchangeDrawingFingerprintRelation.Different;
        }

        private static Dictionary<string, T> UniqueIndex<T>(IEnumerable<T> source, Func<T, string> idSelector, string label) where T : class
        {
            var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                if (item == null) throw new InvalidOperationException("Project contains a null " + label + " entry.");
                var id = Required(idSelector(item), label + " id");
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project contains duplicate " + label + " id: " + id + ". Import preview refuses ambiguous target identity.");
                result[id] = item;
            }
            return result;
        }

        private static string Required(string? value, string label)
        {
            var raw = value ?? string.Empty;
            if (raw.Length == 0 || string.IsNullOrWhiteSpace(raw)) throw new InvalidDataException("Required identity value is empty: " + label + ".");
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("Required identity value is not canonical: " + label + ".");
            return raw;
        }
    }
}
