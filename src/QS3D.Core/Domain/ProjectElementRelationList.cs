using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace QS3D.Core.Domain
{
    internal sealed class ProjectElementRelationList : IList<string>
    {
        private readonly ProjectElement _owner;
        private readonly List<string> _values;

        internal ProjectElementRelationList(ProjectElement owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _values = new List<string>();
        }

        public string this[int index]
        {
            get => _values[index];
            set
            {
                var canonical = RequireRelationValue(value);
                if (string.Equals(_values[index], canonical, StringComparison.Ordinal)) return;
                RequireUnique(canonical, index);
                _values[index] = canonical;
                _owner.MarkRelationChanged();
            }
        }

        public int Count => _values.Count;
        public bool IsReadOnly => false;

        public void Add(string item)
        {
            var canonical = RequireRelationValue(item);
            RequireUnique(canonical, null);
            _values.Add(canonical);
            _owner.MarkRelationChanged();
        }

        public void Clear()
        {
            if (_values.Count == 0) return;
            _values.Clear();
            _owner.MarkRelationChanged();
        }

        public bool Contains(string item) => _values.Contains(item);
        public void CopyTo(string[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);
        public IEnumerator<string> GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int IndexOf(string item) => _values.IndexOf(item);

        public void Insert(int index, string item)
        {
            var canonical = RequireRelationValue(item);
            RequireUnique(canonical, null);
            _values.Insert(index, canonical);
            _owner.MarkRelationChanged();
        }

        public bool Remove(string item)
        {
            var index = _values.IndexOf(item);
            if (index < 0) return false;
            _values.RemoveAt(index);
            _owner.MarkRelationChanged();
            return true;
        }

        public void RemoveAt(int index)
        {
            _values.RemoveAt(index);
            _owner.MarkRelationChanged();
        }

        internal void AddPersistenceValue(string item)
        {
            var canonical = RequireRelationValue(item);
            RequireUnique(canonical, null);
            _values.Add(canonical);
        }

        internal void ClearPersistenceValues()
        {
            _values.Clear();
        }

        internal static string RequireRelationValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Relation value is required.", nameof(value));
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Relation value must be canonical and unpadded.", nameof(value));
            foreach (var ch in value)
                if (char.IsControl(ch))
                    throw new ArgumentException("Relation value cannot contain control characters.", nameof(value));
            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Relation value must be valid XML text.", nameof(value), ex);
            }
            return value;
        }

        private void RequireUnique(string value, int? replacingIndex)
        {
            for (var i = 0; i < _values.Count; i++)
            {
                if (replacingIndex.HasValue && replacingIndex.Value == i) continue;
                if (string.Equals(_values[i], value, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Duplicate relation value: " + value + ".", nameof(value));
            }
        }
    }
}
