using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampNestedStabilitySmoke
    {
        internal static void Run()
        {
            RejectsFamilyMutationDuringMaterialization();
            DetectsNestedMutationAfterStableCapture();
        }

        private static void RejectsFamilyMutationDuringMaterialization()
        {
            var project = new ProjectState("P-STAMP-NESTED-RACE", "Nested stability");
            var family = new ProjectFamily("F1", "Before", ElementCategory.Beam);
            family.Properties["Width"] = "200";
            project.Families.Add(family);

            var beforeProjectRevision = project.ChangeVersion;
            ReplaceFamilyProperties(
                family,
                new MutatingDictionary(
                    family.Properties,
                    () => family.Name = "After"));

            Throws<InvalidOperationException>(
                () => _ = new ProjectPersistenceStamp(project),
                "Persistence stamp accepted a mixed-time Family snapshot.");

            Equal(beforeProjectRevision + 1L, project.ChangeVersion,
                "Family mutation during stamp materialization must advance the parent project revision exactly once.");
            Equal("After", family.Name, "Deterministic Family mutation did not execute during stamp materialization.");
        }

        private static void DetectsNestedMutationAfterStableCapture()
        {
            var project = new ProjectState("P-STAMP-NESTED-CONTROL", "Nested control");
            var family = new ProjectFamily("F1", "Before", ElementCategory.Beam);
            project.Families.Add(family);

            var stamp = new ProjectPersistenceStamp(project);
            var beforeProjectRevision = project.ChangeVersion;
            family.Name = "After";

            Equal(beforeProjectRevision + 1L, project.ChangeVersion,
                "Family mutation after stable capture must advance the parent project revision exactly once.");
            Equal(true, stamp.RequiresSave(project),
                "Stable persistence stamp did not detect later nested persisted-state mutation.");
        }

        private static void ReplaceFamilyProperties(ProjectFamily family, IDictionary<string, string> properties)
        {
            var field = typeof(ProjectFamily).GetField("<Properties>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectFamily Properties backing field was not found.");
            field.SetValue(family, properties);
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception(message + " Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class MutatingDictionary : IDictionary<string, string>
        {
            private readonly IDictionary<string, string> _inner;
            private readonly Action _mutation;
            private bool _mutated;

            public MutatingDictionary(IDictionary<string, string> inner, Action mutation)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            }

            public string this[string key] { get => _inner[key]; set => _inner[key] = value; }
            public ICollection<string> Keys => _inner.Keys;
            public ICollection<string> Values => _inner.Values;
            public int Count => _inner.Count;
            public bool IsReadOnly => _inner.IsReadOnly;
            public void Add(string key, string value) => _inner.Add(key, value);
            public void Add(KeyValuePair<string, string> item) => _inner.Add(item);
            public void Clear() => _inner.Clear();
            public bool Contains(KeyValuePair<string, string> item) => _inner.Contains(item);
            public bool ContainsKey(string key) => _inner.ContainsKey(key);
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
            public bool Remove(string key) => _inner.Remove(key);
            public bool Remove(KeyValuePair<string, string> item) => _inner.Remove(item);
            public bool TryGetValue(string key, out string value) => _inner.TryGetValue(key, out value!);

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                if (!_mutated)
                {
                    _mutated = true;
                    _mutation();
                }
                return _inner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
