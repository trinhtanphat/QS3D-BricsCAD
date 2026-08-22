using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Export
{
    public enum InterchangeSnapshotObjectKind
    {
        Manifest = 0,
        Project = 1,
        Zone = 2,
        Floor = 3,
        Family = 4,
        Element = 5
    }

    public enum InterchangeSnapshotChangeKind
    {
        Added = 0,
        Removed = 1,
        Changed = 2
    }

    public sealed class InterchangeSnapshotChange
    {
        internal InterchangeSnapshotChange(
            InterchangeSnapshotObjectKind objectKind,
            InterchangeSnapshotChangeKind changeKind,
            string id,
            IEnumerable<string> fields)
        {
            ObjectKind = objectKind;
            ChangeKind = changeKind;
            Id = id ?? string.Empty;
            Fields = (fields ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
        }

        public InterchangeSnapshotObjectKind ObjectKind { get; }
        public InterchangeSnapshotChangeKind ChangeKind { get; }
        public string Id { get; }
        public IReadOnlyList<string> Fields { get; }
    }

    public sealed class ProjectInterchangeSnapshotDiffResult
    {
        internal ProjectInterchangeSnapshotDiffResult(
            string leftProjectId,
            string rightProjectId,
            IEnumerable<InterchangeSnapshotChange> changes)
        {
            LeftProjectId = leftProjectId ?? string.Empty;
            RightProjectId = rightProjectId ?? string.Empty;
            Changes = (changes ?? Enumerable.Empty<InterchangeSnapshotChange>()).ToList().AsReadOnly();
        }

        public string LeftProjectId { get; }
        public string RightProjectId { get; }
        public IReadOnlyList<InterchangeSnapshotChange> Changes { get; }
        public bool HasChanges => Changes.Count > 0;
        public int AddedCount => Changes.Count(x => x.ChangeKind == InterchangeSnapshotChangeKind.Added);
        public int RemovedCount => Changes.Count(x => x.ChangeKind == InterchangeSnapshotChangeKind.Removed);
        public int ChangedCount => Changes.Count(x => x.ChangeKind == InterchangeSnapshotChangeKind.Changed);
    }

    public static class ProjectInterchangeSnapshotDiff
    {
        private const int MaxChanges = 120000;

        public static ProjectInterchangeSnapshotDiffResult CompareJson(string leftJson, string rightJson)
        {
            var left = ProjectInterchangeValidatedSnapshotReader.Read(leftJson);
            var right = ProjectInterchangeValidatedSnapshotReader.Read(rightJson);
            return Compare(left, right);
        }

        public static ProjectInterchangeSnapshotDiffResult Compare(
            ProjectInterchangeValidatedSnapshot left,
            ProjectInterchangeValidatedSnapshot right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            var changes = new List<InterchangeSnapshotChange>();
            CompareManifest(left, right, changes);
            CompareProject(left.Project, right.Project, changes);
            CompareById(
                left.Zones,
                right.Zones,
                x => x.Id,
                InterchangeSnapshotObjectKind.Zone,
                (a, b) => Fields(
                    Pair("name", !string.Equals(a.Name, b.Name, StringComparison.Ordinal))),
                changes);
            CompareById(
                left.Floors,
                right.Floors,
                x => x.Id,
                InterchangeSnapshotObjectKind.Floor,
                (a, b) => Fields(
                    Pair("name", !string.Equals(a.Name, b.Name, StringComparison.Ordinal)),
                    Pair("elevationM", !a.ElevationM.Equals(b.ElevationM))),
                changes);
            CompareById(
                left.Families,
                right.Families,
                x => x.Id,
                InterchangeSnapshotObjectKind.Family,
                CompareFamily,
                changes);
            CompareById(
                left.Elements,
                right.Elements,
                x => x.Id,
                InterchangeSnapshotObjectKind.Element,
                CompareElement,
                changes);

            changes.Sort(ChangeComparer.Instance);
            return new ProjectInterchangeSnapshotDiffResult(left.Project.Id, right.Project.Id, changes);
        }

        private static void CompareManifest(
            ProjectInterchangeValidatedSnapshot left,
            ProjectInterchangeValidatedSnapshot right,
            ICollection<InterchangeSnapshotChange> changes)
        {
            var fields = Fields(
                Pair("format", !string.Equals(left.Format, right.Format, StringComparison.Ordinal)),
                Pair("formatVersion", left.FormatVersion != right.FormatVersion),
                Pair("units.length", !string.Equals(left.Units.Length, right.Units.Length, StringComparison.Ordinal)),
                Pair("units.area", !string.Equals(left.Units.Area, right.Units.Area, StringComparison.Ordinal)),
                Pair("units.volume", !string.Equals(left.Units.Volume, right.Units.Volume, StringComparison.Ordinal)),
                Pair("units.mass", !string.Equals(left.Units.Mass, right.Units.Mass, StringComparison.Ordinal)));
            AddChanged(InterchangeSnapshotObjectKind.Manifest, "manifest", fields, changes);
        }

        private static void CompareProject(
            InterchangeProjectSnapshot left,
            InterchangeProjectSnapshot right,
            ICollection<InterchangeSnapshotChange> changes)
        {
            var fields = Fields(
                Pair("id", !IdEquals(left.Id, right.Id)),
                Pair("name", !string.Equals(left.Name, right.Name, StringComparison.Ordinal)),
                Pair("schemaVersion", left.SchemaVersion != right.SchemaVersion),
                Pair("drawingFingerprint", !string.Equals(left.DrawingFingerprint, right.DrawingFingerprint, StringComparison.Ordinal)),
                Pair("updatedUtc", !string.Equals(left.UpdatedUtcRaw, right.UpdatedUtcRaw, StringComparison.Ordinal)));
            AddChanged(InterchangeSnapshotObjectKind.Project, left.Id, fields, changes);
        }

        private static IReadOnlyList<string> CompareFamily(InterchangeFamilySnapshot left, InterchangeFamilySnapshot right)
        {
            var fields = new List<string>();
            if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal)) fields.Add("name");
            if (left.Category != right.Category) fields.Add("category");
            if (!StringMapEquals(left.Properties, right.Properties)) fields.Add("properties");
            return fields.AsReadOnly();
        }

        private static IReadOnlyList<string> CompareElement(InterchangeElementSnapshot left, InterchangeElementSnapshot right)
        {
            var fields = new List<string>();
            if (left.Category != right.Category) fields.Add("category");
            if (!IdEquals(left.FamilyId, right.FamilyId)) fields.Add("familyId");
            if (!IdEquals(left.FloorId, right.FloorId)) fields.Add("floorId");
            if (!IdEquals(left.ZoneId, right.ZoneId)) fields.Add("zoneId");
            if (!string.Equals(left.DrawingFingerprint, right.DrawingFingerprint, StringComparison.Ordinal)) fields.Add("drawingFingerprint");
            if (!string.Equals(left.UpdatedUtcRaw, right.UpdatedUtcRaw, StringComparison.Ordinal)) fields.Add("updatedUtc");
            if (!string.Equals(left.SourceRefScope, right.SourceRefScope, StringComparison.Ordinal)) fields.Add("sourceRefScope");
            if (!SetEquals(left.SourceHandles, right.SourceHandles, StringComparer.OrdinalIgnoreCase)) fields.Add("sourceHandles");
            if (!SetEquals(left.Dependencies, right.Dependencies, StringComparer.OrdinalIgnoreCase)) fields.Add("dependencies");
            if (!StringMapEquals(left.Properties, right.Properties)) fields.Add("properties");
            if (!NumberMapEquals(left.Quantities, right.Quantities)) fields.Add("quantities");
            return fields.AsReadOnly();
        }

        private static void CompareById<T>(
            IEnumerable<T> leftSource,
            IEnumerable<T> rightSource,
            Func<T, string> idSelector,
            InterchangeSnapshotObjectKind kind,
            Func<T, T, IReadOnlyList<string>> compare,
            ICollection<InterchangeSnapshotChange> changes)
        {
            var left = UniqueIndex(leftSource, idSelector, kind, "left");
            var right = UniqueIndex(rightSource, idSelector, kind, "right");
            var ids = new SortedSet<string>(left.Keys, StringComparer.OrdinalIgnoreCase);
            ids.UnionWith(right.Keys);

            foreach (var id in ids)
            {
                var hasLeft = left.TryGetValue(id, out var leftValue);
                var hasRight = right.TryGetValue(id, out var rightValue);
                if (!hasLeft)
                {
                    Add(new InterchangeSnapshotChange(kind, InterchangeSnapshotChangeKind.Added, id, Array.Empty<string>()), changes);
                    continue;
                }
                if (!hasRight)
                {
                    Add(new InterchangeSnapshotChange(kind, InterchangeSnapshotChangeKind.Removed, id, Array.Empty<string>()), changes);
                    continue;
                }

                var fields = compare(leftValue!, rightValue!);
                AddChanged(kind, id, fields, changes);
            }
        }

        private static Dictionary<string, T> UniqueIndex<T>(
            IEnumerable<T> source,
            Func<T, string> idSelector,
            InterchangeSnapshotObjectKind kind,
            string side)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                if (item == null) throw new InvalidOperationException(side + " snapshot contains a null " + kind + " entry.");
                var id = (idSelector(item) ?? string.Empty).Trim();
                if (id.Length == 0) throw new InvalidOperationException(side + " snapshot contains an empty " + kind + " id.");
                if (result.ContainsKey(id))
                    throw new InvalidOperationException(side + " snapshot contains duplicate " + kind + " id: " + id + ".");
                result[id] = item;
            }
            return result;
        }

        private static void AddChanged(
            InterchangeSnapshotObjectKind kind,
            string id,
            IReadOnlyList<string> fields,
            ICollection<InterchangeSnapshotChange> changes)
        {
            if (fields.Count == 0) return;
            Add(new InterchangeSnapshotChange(kind, InterchangeSnapshotChangeKind.Changed, id, fields), changes);
        }

        private static void Add(InterchangeSnapshotChange change, ICollection<InterchangeSnapshotChange> changes)
        {
            if (changes.Count >= MaxChanges)
                throw new InvalidOperationException("Semantic snapshot diff exceeds the supported " + MaxChanges + " change limit.");
            changes.Add(change);
        }

        private static KeyValuePair<string, bool> Pair(string field, bool changed) =>
            new KeyValuePair<string, bool>(field, changed);

        private static IReadOnlyList<string> Fields(params KeyValuePair<string, bool>[] values) =>
            values.Where(x => x.Value).Select(x => x.Key).ToList().AsReadOnly();

        private static bool IdEquals(string left, string right) =>
            string.Equals((left ?? string.Empty).Trim(), (right ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

        private static bool SetEquals(IEnumerable<string> left, IEnumerable<string> right, IEqualityComparer<string> comparer)
        {
            var leftSet = new HashSet<string>((left ?? Enumerable.Empty<string>()).Select(x => (x ?? string.Empty).Trim()), comparer);
            var rightSet = new HashSet<string>((right ?? Enumerable.Empty<string>()).Select(x => (x ?? string.Empty).Trim()), comparer);
            return leftSet.SetEquals(rightSet);
        }

        private static bool StringMapEquals(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
        {
            if (left.Count != right.Count) return false;
            foreach (var pair in left)
            {
                if (!TryGet(right, pair.Key, out var value)) return false;
                if (!string.Equals(pair.Value ?? string.Empty, value ?? string.Empty, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static bool NumberMapEquals(IReadOnlyDictionary<string, double> left, IReadOnlyDictionary<string, double> right)
        {
            if (left.Count != right.Count) return false;
            foreach (var pair in left)
            {
                if (!TryGet(right, pair.Key, out var value)) return false;
                if (!pair.Value.Equals(value)) return false;
            }
            return true;
        }

        private static bool TryGet<T>(IReadOnlyDictionary<string, T> source, string key, out T value)
        {
            foreach (var pair in source)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                value = pair.Value;
                return true;
            }
            value = default(T)!;
            return false;
        }

        private sealed class ChangeComparer : IComparer<InterchangeSnapshotChange>
        {
            public static readonly ChangeComparer Instance = new ChangeComparer();
            public int Compare(InterchangeSnapshotChange x, InterchangeSnapshotChange y)
            {
                var kind = x.ObjectKind.CompareTo(y.ObjectKind);
                if (kind != 0) return kind;
                var id = StringComparer.OrdinalIgnoreCase.Compare(x.Id, y.Id);
                if (id != 0) return id;
                return x.ChangeKind.CompareTo(y.ChangeKind);
            }
        }
    }
}
