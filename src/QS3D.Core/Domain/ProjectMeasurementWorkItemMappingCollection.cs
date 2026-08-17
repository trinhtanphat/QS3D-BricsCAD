using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Mapping;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectMeasurementWorkItemMappingCollection : ICollection<MeasurementWorkItemMapping>
    {
        private readonly ProjectState _project;
        private readonly ProjectMetadataDictionary _metadata;

        internal ProjectMeasurementWorkItemMappingCollection(ProjectState project, IDictionary<string, string> metadata)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _metadata = metadata as ProjectMetadataDictionary
                ?? throw new ArgumentException("Project measurement/work-item mappings require the project metadata store.", nameof(metadata));
            _metadata.BindProject(project);
        }

        public int Count { get { return Snapshot().Count; } }
        public bool IsReadOnly { get { return false; } }

        public void Add(MeasurementWorkItemMapping item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var all = Snapshot().ToList();
            all.Add(item);
            new MeasurementWorkItemMappingCatalog(all);
            var key = ProjectMeasurementWorkItemMappingCodec.Key(item);
            var value = ProjectMeasurementWorkItemMappingCodec.Value(item);
            if (_metadata.ContainsKey(key)) throw new ArgumentException("Duplicate measurement/work-item mapping id: " + item.MappingId + ".", nameof(item));
            _metadata.EnsureCanAddOwned(key);
            _project.Touch();
            _metadata.AddOwned(key, value);
        }

        public void Clear()
        {
            if (!_metadata.Keys.Any(ProjectMeasurementWorkItemMappingCodec.IsReservedKey)) return;
            Snapshot();
            _project.Touch();
            _metadata.ClearReservedOwned();
        }

        public bool Contains(MeasurementWorkItemMapping item) { return item != null && Snapshot().Any(x => Same(x, item)); }
        public void CopyTo(MeasurementWorkItemMapping[] array, int arrayIndex) { Snapshot().CopyTo(array, arrayIndex); }

        public bool Remove(MeasurementWorkItemMapping item)
        {
            if (item == null) return false;
            var match = Snapshot().FirstOrDefault(x => Same(x, item));
            if (match == null) return false;
            var key = ProjectMeasurementWorkItemMappingCodec.Key(match);
            _project.Touch();
            if (!_metadata.RemoveOwned(key))
                throw new InvalidOperationException("Project measurement/work-item mapping disappeared during removal: " + match.MappingId + ".");
            return true;
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
