using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleSelectionKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GenericCountDriftRejects();
            ReadOnlyCountDriftRejects();
            NonGenericCountDriftRejects();
            NegativePostTraversalCountRejects();
            ConflictingPostTraversalCountsReject();
            CountOverrunRejectsBeforeSecondHandleCanResolve();
            CountUnderYieldRejects();
            StableCountedSelectionResolves();
            PureStreamingSelectionResolves();
        }

        private static void GenericCountDriftRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new GenericDriftCollection("A", 1, 2)), "changed during traversal");
        }

        private static void ReadOnlyCountDriftRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new ReadOnlyDriftCollection("A", 1, 2)), "changed during traversal");
        }

        private static void NonGenericCountDriftRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new NonGenericDriftCollection("A", 1, 2)), "changed during traversal");
        }

        private static void NegativePostTraversalCountRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new GenericDriftCollection("A", 1, -1)), "negative known Count value after traversal");
        }

        private static void ConflictingPostTraversalCountsReject()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new ConflictingAfterTraversalCollection("A")), "conflicting known Count values after traversal");
        }

        private static void CountOverrunRejectsBeforeSecondHandleCanResolve()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new FixedCountEnumerable(1, "A", "UNOWNED")), "does not match completed traversal cardinality");
        }

        private static void CountUnderYieldRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new FixedCountEnumerable(2, "A")), "does not match completed traversal cardinality");
        }

        private static void StableCountedSelectionResolves()
        {
            var project = CreateProject(out var element);
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new List<string> { "A" });
            Require(resolved.Count == 1 && ReferenceEquals(resolved[0], element),
                "Stable counted semantic handle selection did not resolve the expected owner.");
        }

        private static void PureStreamingSelectionResolves()
        {
            var project = CreateProject(out var element);
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, Streaming("A"));
            Require(resolved.Count == 1 && ReferenceEquals(resolved[0], element),
                "Pure streaming semantic handle selection did not preserve supported behavior.");
        }

        private static ProjectState CreateProject(out ProjectElement element)
        {
            var project = new ProjectState("HANDLE-COUNT", "Semantic handle Count stability");
            element = new ProjectElement("E1", ElementCategory.CustomQuantity);
            element.SourceHandles.Add("A");
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> Streaming(string value)
        {
            yield return value;
        }

        private static void ThrowsCountIntegrity(Action action, string expectedFragment)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "Unexpected semantic handle Count-integrity error: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(
                "Expected semantic handle Count-integrity rejection containing: " + expectedFragment);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class GenericDriftCollection : ICollection<string>
        {
            private readonly string _value;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            internal GenericDriftCollection(string value, int before, int after)
            {
                _value = value;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public bool IsReadOnly => true;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
                _completed = true;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => string.Equals(item, _value, StringComparison.Ordinal);
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyDriftCollection : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            internal ReadOnlyDriftCollection(string value, int before, int after)
            {
                _value = value;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
                _completed = true;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericDriftCollection : IEnumerable<string>, ICollection
        {
            private readonly string _value;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            internal NonGenericDriftCollection(string value, int before, int after)
            {
                _value = value;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
                _completed = true;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => array.SetValue(_value, index);
        }

        private sealed class ConflictingAfterTraversalCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string _value;
            private bool _completed;

            internal ConflictingAfterTraversalCollection(string value) => _value = value;

            public int Count => 1;
            int IReadOnlyCollection<string>.Count => _completed ? 2 : 1;
            public bool IsReadOnly => true;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
                _completed = true;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => string.Equals(item, _value, StringComparison.Ordinal);
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class FixedCountEnumerable : IReadOnlyCollection<string>
        {
            private readonly int _count;
            private readonly string[] _values;

            internal FixedCountEnumerable(int count, params string[] values)
            {
                _count = count;
                _values = values;
            }

            public int Count => _count;
            public IEnumerator<string> GetEnumerator()
            {
                for (var i = 0; i < _values.Length; i++)
                    yield return _values[i];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
