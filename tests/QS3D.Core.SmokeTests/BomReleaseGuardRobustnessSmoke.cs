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
        private const string LimitMessage =
            "Live generated handle count must not exceed 10000 for BOM release diagnostics.";

        public static void Run()
        {
            ExactBoundaryIsAccepted();
            KnownOversizeRejectsBeforeEnumeration();
            DishonestCountCannotBypassStreamingLimit();
            RejectionDoesNotMutateProject();
            NullAndEmptyInputsRemainValid();
            CanonicalHandleBehaviorRemainsCompatible();
        }

        private static void ExactBoundaryIsAccepted()
        {
            var handles = Enumerable.Range(0, 10000)
                .Select(i => "H" + i.ToString("X"))
                .ToArray();
            var set = new TrackingSet(handles, 10000);

            BomReleaseGuardService.Inspect(new ProjectState("bom-boundary", "BOM boundary"), set);

            Equal(10000, set.EnumeratedCount, "exact boundary enumeration");
        }

        private static void KnownOversizeRejectsBeforeEnumeration()
        {
            var set = new TrackingSet(new[] { "A" }, 10001, throwOnEnumeration: true);
            var ex = Throws<InvalidOperationException>(() =>
                BomReleaseGuardService.Inspect(new ProjectState("bom-oversize", "BOM oversize"), set));

            Equal(LimitMessage, ex.Message, "known oversize message");
            Equal(0, set.EnumeratedCount, "known oversize must reject before enumeration");
        }

        private static void DishonestCountCannotBypassStreamingLimit()
        {
            var handles = Enumerable.Range(0, 10002)
                .Select(i => "D" + i.ToString("X"))
                .ToArray();
            var set = new TrackingSet(handles, 1);
            var ex = Throws<InvalidOperationException>(() =>
                BomReleaseGuardService.Inspect(new ProjectState("bom-dishonest", "BOM dishonest count"), set));

            Equal(LimitMessage, ex.Message, "dishonest count message");
            Equal(10001, set.EnumeratedCount, "streaming guard must stop at item 10001");
        }

        private static void RejectionDoesNotMutateProject()
        {
            var project = new ProjectState("bom-atomic", "BOM atomicity");
            project.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));
            var element = new ProjectElement("beam-atomic", ElementCategory.Beam, "beam", string.Empty, string.Empty);
            element.SourceHandles.Add("1A");
            element.SetQuantity("NetConcreteM3", 1.25d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var familyCount = project.Families.Count;
            var elementCount = project.Elements.Count;
            var sourceHandle = element.SourceHandles[0];
            var quantity = element.Quantities["NetConcreteM3"];
            var dirty = element.Dirty;

            Throws<InvalidOperationException>(() =>
                BomReleaseGuardService.Inspect(project, new TrackingSet(new[] { "1A" }, 10001, throwOnEnumeration: true)));

            Equal(familyCount, project.Families.Count, "family count after rejection");
            Equal(elementCount, project.Elements.Count, "element count after rejection");
            Equal(sourceHandle, element.SourceHandles[0], "source handle after rejection");
            Equal(quantity, element.Quantities["NetConcreteM3"], "quantity after rejection");
            if (dirty != element.Dirty)
                throw new Exception("BOM live-handle rejection must not mutate element dirty state.");
        }

        private static void NullAndEmptyInputsRemainValid()
        {
            var project = new ProjectState("bom-null-empty", "BOM null/empty");
            BomReleaseGuardService.Inspect(project, null);
            BomReleaseGuardService.Inspect(project, new HashSet<string>());
        }

        private static void CanonicalHandleBehaviorRemainsCompatible()
        {
            var project = new ProjectState("bom-canonical", "BOM canonical handles");
            project.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));
            var element = new ProjectElement("beam-canonical", ElementCategory.Beam, "beam", string.Empty, string.Empty);
            element.SourceHandles.Add("1A");
            element.Properties["GeneratedSolidHandle"] = "2B";
            element.SetQuantity("NetConcreteM3", 1.25d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var callerSet = new HashSet<string>(new[] { " 2b ", "   ", "2B" }, StringComparer.Ordinal);
            var issues = BomReleaseGuardService.Inspect(project, callerSet);
            if (issues.Any(x => string.Equals(x.Code, "BOM_GENERATED_HANDLE_MISSING", StringComparison.Ordinal)))
                throw new Exception("Bounded ingestion must preserve trim/blank/case-insensitive generated-handle semantics.");
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual)
                throw new Exception(label + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(label + ": expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new Exception(label + ": expected " + expected + ", got " + actual + ".");
        }

        private sealed class TrackingSet : ISet<string>
        {
            private readonly IReadOnlyList<string> _items;
            private readonly HashSet<string> _membership;
            private readonly int _reportedCount;
            private readonly bool _throwOnEnumeration;

            public TrackingSet(IEnumerable<string> items, int reportedCount, bool throwOnEnumeration = false)
            {
                _items = items.ToArray();
                _membership = new HashSet<string>(_items, StringComparer.Ordinal);
                _reportedCount = reportedCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public int EnumeratedCount { get; private set; }
            public int Count => _reportedCount;
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                if (_throwOnEnumeration)
                    throw new Exception("Enumeration must not start for a known oversized set.");

                foreach (var item in _items)
                {
                    EnumeratedCount++;
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => _membership.Contains(item);
            public void CopyTo(string[] array, int arrayIndex) => _membership.CopyTo(array, arrayIndex);
            public bool IsProperSubsetOf(IEnumerable<string> other) => _membership.IsProperSubsetOf(other);
            public bool IsProperSupersetOf(IEnumerable<string> other) => _membership.IsProperSupersetOf(other);
            public bool IsSubsetOf(IEnumerable<string> other) => _membership.IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<string> other) => _membership.IsSupersetOf(other);
            public bool Overlaps(IEnumerable<string> other) => _membership.Overlaps(other);
            public bool SetEquals(IEnumerable<string> other) => _membership.SetEquals(other);
            public bool Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void SymmetricExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }
    }
}
