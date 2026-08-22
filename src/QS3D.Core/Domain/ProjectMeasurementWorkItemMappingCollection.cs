using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Mapping;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectMeasurementWorkItemMappingCollection : ICollection<MeasurementWorkItemMapping>
    {
        private readonly IDictionary<string, string> _metadata;
        internal ProjectMeasurementWorkItemMappingCollection(IDictionary<string, string> metadata) { _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata)); }
        public int Count { get { return Snapshot().Count; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(MeasurementWorkItemMapping item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var all = Snapshot().ToList();
            all.Add(item);
            new MeasurementWorkItemMappingCatalog(all);
            var key = ProjectMeasurementWorkItemMappingCodec.Key(item);
            if (_metadata.ContainsKey(key)) throw new ArgumentException("Duplicate measurement/work-item mapping id: " + item.MappingId + ".", nameof(item));
            _metadata.Add(key, ProjectMeasurementWorkItemMappingCodec.Value(item));
        }
        public void Clear() { foreach (var key in _metadata.Keys.Where(ProjectMeasurementWorkItemMappingCodec.IsReservedKey).ToArray()) _metadata.Remove(key); }
        public bool Contains(MeasurementWorkItemMapping item) { return item != null && Snapshot().Any(x => Same(x, item)); }
        public void CopyTo(MeasurementWorkItemMapping[] array, int arrayIndex) { Snapshot().CopyTo(array, arrayIndex); }
        public bool Remove(MeasurementWorkItemMapping item)
        {
            if (item == null) return false;
            var match = Snapshot().FirstOrDefault(x => Same(x, item));
            return match != null && _metadata.Remove(ProjectMeasurementWorkItemMappingCodec.Key(match));
        }
        public IEnumerator<MeasurementWorkItemMapping> GetEnumerator() { return Snapshot().GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        private List<MeasurementWorkItemMapping> Snapshot() { return ProjectMeasurementWorkItemMappingCodec.Read(_metadata).ToList(); }
        private static bool Same(MeasurementWorkItemMapping a, MeasurementWorkItemMapping b)
        {
            return a.Category == b.Category && string.Equals(a.MappingId, b.MappingId, StringComparison.Ordinal) && string.Equals(a.MeasurementItemId, b.MeasurementItemId, StringComparison.Ordinal) && string.Equals(a.ClassificationId, b.ClassificationId, StringComparison.Ordinal) && string.Equals(a.WorkItemId, b.WorkItemId, StringComparison.Ordinal);
        }
    }
}
