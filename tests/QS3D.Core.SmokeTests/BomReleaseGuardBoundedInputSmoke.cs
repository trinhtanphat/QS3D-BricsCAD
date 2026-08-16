using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BomReleaseGuardBoundedInputSmoke
    {
        private const int MaxLiveGeneratedHandleInputs = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactBoundIsAcceptedWithoutProjectMutation();
            OversizedCountFailsBeforeEnumerationOrProjectMutation();
            DishonestCountCannotEvadeStreamingBound();
            CanonicalDuplicateAndBlankHandlesPreserveDiagnostics();
        }

        private static void ExactBoundIsAcceptedWithoutProjectMutation()
        {
            var project = new ProjectState("bom-live-bound", "BOM live bound");
            var handles = CreateHandles(MaxLiveGeneratedHandleInputs);
            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var issues = BomReleaseGuardService.Inspect(project, handles);
            Require(project.ChangeVersion == version, "Boundary live-handle inspection mutated project revision.");
            Require(project.UpdatedUtc == updatedUtc, "Boundary live-handle inspection mutated project timestamp.");
            Require(issues.Any(x => x.Code == "BOM_EMPTY"), "Boundary live-handle inspection changed empty-project diagnostics.");
        }

        private static void OversizedCountFailsBeforeEnumerationOrProjectMutation()
        {
            var project = new ProjectState("bom-live-overflow", "BOM live overflow");
            project.Elements.Add(null!);
            var version = project.ChangeVersion;
            var handles = new EnumerationFailSet(CreateHandles(MaxLiveGeneratedHandleInputs + 1));
            ThrowsBound(() => BomReleaseGuardService.Inspect(project, handles));
            Require(!handles.EnumerationAttempted, "Oversized Count input was enumerated before fast rejection.");
            Require(project.ChangeVersion == version && project.Elements.Count == 1 && project.Elements[0] == null,
                "Rejected oversized Count input mutated project state.");
        }

        private static void DishonestCountCannotEvadeStreamingBound()
        {
            var project = new ProjectState("bom-live-dishonest", "BOM live dishonest count");
            project.Elements.Add(null!);
            var version = project.ChangeVersion;
            var handles = new DishonestCountSet(MaxLiveGeneratedHandleInputs + 50, 1);
            ThrowsBound(() => BomReleaseGuardService.Inspect(project, handles));
            Require(handles.YieldedCount == MaxLiveGeneratedHandleInputs + 1,
                "Streaming bound did not stop on the first disallowed live handle.");
            Require(project.ChangeVersion == version && project.Elements.Count == 1 && project.Elements[0] == null,
                "Streaming rejection mutated project state.");
        }

        private static void CanonicalDuplicateAndBlankHandlesPreserveDiagnostics()
        {
            var project = new ProjectState("bom-live-canonical", "BOM live canonical");
            var noisy = new HashSet<string>(StringComparer.Ordinal) { " 1a ", "1A", string.Empty, "   " };
            var canonical = new HashSet<string>(StringComparer.Ordinal) { "1A" };
            var noisyCodes = BomReleaseGuardService.Inspect(project, noisy).Select(x => x.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var canonicalCodes = BomReleaseGuardService.Inspect(project, canonical).Select(x => x.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Require(noisyCodes.SequenceEqual(canonicalCodes, StringComparer.Ordinal),
                "Equivalent canonical live-handle sets changed BOM diagnostics.");
        }

        private static HashSet<string> CreateHandles(int count)
        {
            var handles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < count; index++) handles.Add((index + 1).ToString("X"));
            return handles;
        }

        private sealed class EnumerationFailSet : ISet<string>
        {
            private readonly HashSet<string> _inner;
            internal EnumerationFailSet(HashSet<string> inner) => _inner = inner;
            internal bool EnumerationAttempted { get; private set; }
            public int Count => _inner.Count;
            public bool IsReadOnly => true;
            public bool Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<string> other) => throw new NotSupportedException();
            public bool IsProperSubsetOf(IEnumerable<string> other) => _inner.IsProperSubsetOf(other);
            public bool IsProperSupersetOf(IEnumerable<string> other) => _inner.IsProperSupersetOf(other);
            public bool IsSubsetOf(IEnumerable<string> other) => _inner.IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<string> other) => _inner.IsSupersetOf(other);
            public bool Overlaps(IEnumerable<string> other) => _inner.Overlaps(other);
            public bool SetEquals(IEnumerable<string> other) => _inner.SetEquals(other);
            public void SymmetricExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => _inner.Contains(item);
            public void CopyTo(string[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
            public bool Remove(string item) => throw new NotSupportedException();
            public IEnumerator<string> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new InvalidOperationException("Oversized set must be rejected before enumeration.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DishonestCountSet : ISet<string>
        {
            private readonly int _actualCount;
            private readonly int _reportedCount;
            internal DishonestCountSet(int actualCount, int reportedCount)
            {
                _actualCount = actualCount;
                _reportedCount = reportedCount;
            }
            internal int YieldedCount { get; private set; }
            public int Count => _reportedCount;
            public bool IsReadOnly => true;
            public bool Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<string> other) => throw new NotSupportedException();
            public bool IsProperSubsetOf(IEnumerable<string> other) => throw new NotSupportedException();
            public bool IsProperSupersetOf(IEnumerable<string> other) => throw new NotSupportedException();
            public bool IsSubsetOf(IEnumerable<string> other) => throw new NotSupportedException();
            public bool IsSupersetOf(IEnumerable<string> other) => throw new NotSupportedException();
            public bool Overlaps(IEnumerable<string> other) => throw new NotSupportedException();
            public bool SetEquals(IEnumerable<string> other) => throw new NotSupportedException();
            public void SymmetricExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => throw new NotSupportedException();
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public IEnumerator<string> GetEnumerator()
            {
                for (var index = 0; index < _actualCount; index++)
                {
                    YieldedCount++;
                    yield return "H" + index.ToString("X");
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void ThrowsBound(Action action)
        {
            try
            {
                action();
                throw new Exception("Oversized live generated Handle input must fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                var expected = "BOM live generated Handle input exceeds the supported bound of " + MaxLiveGeneratedHandleInputs + ".";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new Exception("Unexpected BOM live-handle bound diagnostic: " + ex.Message);
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
