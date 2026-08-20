using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationSubsetSmoke
    {
        public static void Run()
        {
            RegeneratesOnlyRequestedElements();
            UsesCurrentDirtyLinkedOpeningSemantics();
            RejectsMalformedRequestedIds();
            MarkChangedRejectsNonCanonicalElementIds();
            RejectsUnknownTarget();
            RejectsDuplicateProjectIds();
            RejectsInvalidKnownTargetCountsBeforeEnumeration();
            RejectsKnownTargetCountTraversalMismatch();
        }

        private static void RegeneratesOnlyRequestedElements()
        {
            var project = new ProjectState("regen-subset", "Targeted regeneration");
            var selected = new ProjectElement("selected", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            selected.SetProperty("LengthM", "12.5");
            selected.SetProperty("AreaM2", "3.25");
            project.Elements.Add(selected);

            var unrelated = new ProjectElement("unrelated", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            unrelated.SetProperty("LengthM", "99");
            unrelated.SetProperty("AreaM2", "8");
            project.Elements.Add(unrelated);

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            var count = engine.RegenerateDirtySubset(project, new[] { selected.Id });

            Equal(1, count);
            Equal(ElementDirtyFlags.None, selected.Dirty);
            True(unrelated.Dirty != ElementDirtyFlags.None);
            True(!unrelated.Quantities.ContainsKey("Count"));
            Near(12.5d, selected.Quantities["LengthM"]);
            Near(3.25d, selected.Quantities["AreaM2"]);
            Near(1d, selected.Quantities["Count"]);
        }

        private static void UsesCurrentDirtyLinkedOpeningSemantics()
        {
            var project = new ProjectState("regen-subset-linked-opening", "Targeted wall regeneration with dirty opening");
            var wall = new ProjectElement("wall", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.SetProperty("LengthM", "5");
            wall.SetProperty("HeightM", "3");
            wall.SetProperty("ThicknessM", "0.2");
            project.Elements.Add(wall);

            var opening = new ProjectElement("opening", ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            opening.SetProperty("HostWallId", wall.Id);
            opening.SetProperty("WidthM", "1");
            opening.SetProperty("HeightM", "2");
            opening.SetQuantity("OpeningAreaM2", 2d);
            opening.MarkClean(ElementDirtyFlags.All);
            opening.SetProperty("WidthM", "2");
            project.Elements.Add(opening);

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            var count = engine.RegenerateDirtySubset(project, new[] { wall.Id });

            Equal(1, count);
            Near(4d, wall.Quantities["OpeningAreaM2"]);
            Near(2.2d, wall.Quantities["NetVolumeM3"]);
            Near(2d, opening.Quantities["OpeningAreaM2"]);
            True(opening.Dirty != ElementDirtyFlags.None);
        }

        private static void RejectsMalformedRequestedIds()
        {
            var project = new ProjectState("regen-target-guard", "Canonical subset targets");
            var selected = new ProjectElement("Selected", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            selected.SetProperty("LengthM", "4");
            project.Elements.Add(selected);
            var dirtyBefore = selected.Dirty;

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { " Selected " }));
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { "Selected", "selected" }));
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { string.Empty }));

            Equal(dirtyBefore, selected.Dirty);
            True(!selected.Quantities.ContainsKey("Count"));
        }

        private static void MarkChangedRejectsNonCanonicalElementIds()
        {
            var project = new ProjectState("regen-mark-changed-id", "Canonical MarkChanged identity");
            var selected = new ProjectElement("Selected", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            selected.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(selected);
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            var versionBefore = project.ChangeVersion;

            Throws<ArgumentException>(() => engine.MarkChanged(project, " Selected ", ElementDirtyFlags.Quantity));
            Throws<ArgumentException>(() => engine.MarkChanged(project, string.Empty, ElementDirtyFlags.Quantity));
            Throws<KeyNotFoundException>(() => engine.MarkChanged(project, "Missing", ElementDirtyFlags.Quantity));
            Equal(ElementDirtyFlags.None, selected.Dirty);
            True(project.ChangeVersion == versionBefore);

            engine.MarkChanged(project, "selected", ElementDirtyFlags.None);
            Equal(ElementDirtyFlags.None, selected.Dirty);
            True(project.ChangeVersion == versionBefore);

            engine.MarkChanged(project, "selected", ElementDirtyFlags.Quantity);
            True((selected.Dirty & ElementDirtyFlags.Quantity) != 0);
            True(project.ChangeVersion > versionBefore);
        }

        private static void RejectsUnknownTarget()
        {
            var project = new ProjectState("regen-unknown", "Unknown target");
            project.Elements.Add(new ProjectElement("present", ElementCategory.CustomQuantity));
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Throws<KeyNotFoundException>(() => engine.RegenerateDirtySubset(project, new[] { "missing" }));
        }

        private static void RejectsDuplicateProjectIds()
        {
            var project = new ProjectState("regen-duplicate", "Duplicate target ownership");
            var first = new ProjectElement("DUP", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            first.SetProperty("LengthM", "1");
            var second = new ProjectElement("dup", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            second.SetProperty("LengthM", "2");
            project.Elements.Add(first);
            project.Elements.Add(second);

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Throws<InvalidOperationException>(() => engine.RegenerateDirtySubset(project, new[] { "dup" }));
        }

        private static void RejectsInvalidKnownTargetCountsBeforeEnumeration()
        {
            var project = TwoElementProject("regen-target-count-preflight");
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());

            var negative = new MultiCountTargetIds(new[] { "A" }, -1, -1, -1, throwOnEnumeration: true);
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, negative));
            True(!negative.EnumerationRequested);

            var conflicting = new MultiCountTargetIds(new[] { "A" }, 1, 2, 1, throwOnEnumeration: true);
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, conflicting));
            True(!conflicting.EnumerationRequested);
        }

        private static void RejectsKnownTargetCountTraversalMismatch()
        {
            var project = TwoElementProject("regen-target-count-traversal");
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());

            var under = new MultiCountTargetIds(new[] { "A" }, 2, 2, 2, throwOnEnumeration: false);
            Throws<InvalidOperationException>(() => engine.RegenerateDirtySubset(project, under));
            Equal(1, under.EnumerationRequestCount);
            True(!project.Elements[0].Quantities.ContainsKey("Count"));
            True(!project.Elements[1].Quantities.ContainsKey("Count"));

            var over = new MultiCountTargetIds(new[] { "A", "B" }, 1, 1, 1, throwOnEnumeration: false);
            Throws<InvalidOperationException>(() => engine.RegenerateDirtySubset(project, over));
            Equal(1, over.EnumerationRequestCount);
            True(!project.Elements[0].Quantities.ContainsKey("Count"));
            True(!project.Elements[1].Quantities.ContainsKey("Count"));
        }

        private static ProjectState TwoElementProject(string id)
        {
            var project = new ProjectState(id, "Known target count integrity");
            var first = new ProjectElement("A", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            first.SetProperty("LengthM", "1");
            var second = new ProjectElement("B", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            second.SetProperty("LengthM", "2");
            project.Elements.Add(first);
            project.Elements.Add(second);
            return project;
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(ElementDirtyFlags expected, ElementDirtyFlags actual)
        {
            if (expected != actual) throw new Exception("Expected dirty " + expected + ", got " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class MultiCountTargetIds : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly string[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public MultiCountTargetIds(string[] items, int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public bool EnumerationRequested { get; private set; }
            public int EnumerationRequestCount { get; private set; }
            int ICollection<string>.Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<string>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationRequested = true;
                EnumerationRequestCount++;
                if (_throwOnEnumeration) throw new Exception("Enumerator must not be requested.");
                return ((IEnumerable<string>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }
    }
}
