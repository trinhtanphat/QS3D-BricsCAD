using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BomReleaseGuardRobustnessSmoke
    {
        private const int MaximumLiveHandles = 10000;

        internal static void Run()
        {
            AcceptsBoundaryWithoutProjectMutation();
            RejectsBoundaryPlusOneBeforeEnumerationOrProjectMutation();
            CanonicalDuplicateAndBlankHandlesPreserveDiagnostics();
        }

        private static void AcceptsBoundaryWithoutProjectMutation()
        {
            var project = new ProjectState("bom-live-boundary", "BOM live boundary");
            var handles = CreateHandles(MaximumLiveHandles);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            var issues = BomReleaseGuardService.Inspect(project, handles);

            Require(project.ChangeVersion == beforeVersion, "Boundary live-handle inspection mutated project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Boundary live-handle inspection mutated project timestamp.");
            Require(issues.Any(x => x.Code == "BOM_EMPTY"), "Boundary live-handle inspection changed empty-project BOM semantics.");
        }

        private static void RejectsBoundaryPlusOneBeforeEnumerationOrProjectMutation()
        {
            var project = new ProjectState("bom-live-overflow", "BOM live overflow");
            var handles = new EnumerationFailSet(CreateHandles(MaximumLiveHandles + 1));
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => BomReleaseGuardService.Inspect(project, handles));

            Require(!handles.EnumerationAttempted, "Oversized live-handle input was enumerated before cardinality rejection.");
            Require(project.ChangeVersion == beforeVersion, "Rejected live-handle overflow mutated project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Rejected live-handle overflow mutated project timestamp.");
        }

        private static void CanonicalDuplicateAndBlankHandlesPreserveDiagnostics()
        {
            var project = new ProjectState("bom-live-canonical", "BOM live canonical");
            var noisy = new HashSet<string>(StringComparer.Ordinal)
            {
                " 1a ",
                "1A",
                string.Empty,
                "   "
            };
            var canonical = new HashSet<string>(StringComparer.Ordinal) { "1A" };

            var noisyCodes = BomReleaseGuardService.Inspect(project, noisy).Select(x => x.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var canonicalCodes = BomReleaseGuardService.Inspect(project, canonical).Select(x => x.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray();

            Require(noisyCodes.SequenceEqual(canonicalCodes, StringComparer.Ordinal), "Handle normalization changed BOM diagnostics for equivalent live-handle sets.");
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

            public EnumerationFailSet(HashSet<string> inner) => _inner = inner;

            public bool EnumerationAttempted { get; private set; }
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

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
