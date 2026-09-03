using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportRevisionReviewSmoke
    {
        public static void Run()
        {
            AddedRemovedChangedRowsUseStableElementKeys();
            FamilyIdentityCasingUsesSemanticIdentity();
            CaptureAndCompareDoNotMutateLiveProjects();
            NestedMutationDuringCaptureFailsClosed();
            ProjectAndSnapshotIdentityFailClosed();
            NonFiniteAndInvalidMagnitudeFailClosed();
        }

        private static void AddedRemovedChangedRowsUseStableElementKeys()
        {
            var beforeProject = Project("quantity-review", ("E-REM", 5d), ("E-CHG", 2d));
            var afterProject = Project("quantity-review", ("E-CHG", 3d), ("E-ADD", 1d));
            var service = new QuantityReportRevisionService();

            var before = service.Capture(beforeProject, "BASELINE");
            var after = service.Capture(afterProject, "CURRENT");
            var diff = service.Compare(before, after);

            Equal("quantity-review", diff.ProjectId);
            Equal("BASELINE", diff.BeforeSnapshotId);
            Equal("CURRENT", diff.AfterSnapshotId);
            Equal(3, diff.SemanticDeltaCount);
            Equal(1, diff.AddedCount);
            Equal(1, diff.RemovedCount);
            Equal(1, diff.ChangedCount);
            Equal("E-ADD", diff.Changes[0].StableKey);
            Equal(QuantityReportRevisionChangeKind.Added, diff.Changes[0].Kind);
            Equal("E-REM", diff.Changes[1].StableKey);
            Equal(QuantityReportRevisionChangeKind.Removed, diff.Changes[1].Kind);
            Equal("E-CHG", diff.Changes[2].StableKey);
            Equal(QuantityReportRevisionChangeKind.Changed, diff.Changes[2].Kind);
            True(diff.Changes[2].ChangedFields.SequenceEqual(new[] { "LengthM" }));
            Near(2d, diff.Changes[2].Before!.LengthM);
            Near(3d, diff.Changes[2].After!.LengthM);

            True(before.Rows.Select(x => x.StableKey).SequenceEqual(new[] { "E-CHG", "E-REM" }));
            True(after.Rows.Select(x => x.StableKey).SequenceEqual(new[] { "E-ADD", "E-CHG" }));
        }

        private static void FamilyIdentityCasingUsesSemanticIdentity()
        {
            var service = new QuantityReportRevisionService();
            var baseline = service.Capture(ProjectWithFamilyId("family-identity-review", "family", ("E1", 2d)), "R1");
            var caseOnly = service.Capture(ProjectWithFamilyId("family-identity-review", "FAMILY", ("E1", 2d)), "R2");
            var caseOnlyDiff = service.Compare(baseline, caseOnly);

            Equal(0, caseOnlyDiff.SemanticDeltaCount);
            Equal(0, caseOnlyDiff.Changes.Count);

            var differentFamily = service.Capture(ProjectWithFamilyId("family-identity-review", "other-family", ("E1", 2d)), "R3");
            var differentFamilyDiff = service.Compare(baseline, differentFamily);

            Equal(1, differentFamilyDiff.SemanticDeltaCount);
            Equal(1, differentFamilyDiff.ChangedCount);
            True(differentFamilyDiff.Changes.Single().ChangedFields.SequenceEqual(new[] { "FamilyId" }));
        }

        private static void CaptureAndCompareDoNotMutateLiveProjects()
        {
            var beforeProject = Project("read-only-review", ("E1", 2d));
            var afterProject = Project("read-only-review", ("E1", 2.5d));
            var beforeState = State(beforeProject);
            var afterState = State(afterProject);
            var service = new QuantityReportRevisionService();

            var baseline = service.Capture(beforeProject, "R1");
            var candidate = service.Capture(afterProject, "R2");
            var diff = service.Compare(baseline, candidate);

            Equal(1, diff.ChangedCount);
            AssertState(beforeProject, beforeState);
            AssertState(afterProject, afterState);
        }

        private static void NestedMutationDuringCaptureFailsClosed()
        {
            var project = ProjectWithFamilyId("quantity-review-nested-race", "family-before", ("E1", 2d));
            project.Families.Add(new ProjectFamily("family-after", "Family after", ElementCategory.Beam));
            var element = project.Elements.Single();
            element.Properties["Trigger"] = "1";
            var beforeProjectRevision = project.ChangeVersion;
            ReplaceElementProperties(
                element,
                new MutatingDictionary(
                    element.Properties,
                    () => element.FamilyId = "family-after"));

            Throws<InvalidOperationException>(() => new QuantityReportRevisionService().Capture(project, "RACE"));

            Equal(beforeProjectRevision, project.ChangeVersion);
            Equal("family-after", element.FamilyId);
        }

        private static void ProjectAndSnapshotIdentityFailClosed()
        {
            var service = new QuantityReportRevisionService();
            var first = service.Capture(Project("project-a", ("E1", 1d)), "R1");
            var otherProject = service.Capture(Project("project-b", ("E1", 2d)), "R2");
            Throws<InvalidOperationException>(() => service.Compare(first, otherProject));

            var sameIdentity = service.Capture(Project("project-a", ("E1", 2d)), "r1");
            Throws<InvalidOperationException>(() => service.Compare(first, sameIdentity));
            Throws<InvalidOperationException>(() => service.Capture(Project("project-a", ("E1", 1d)), " padded "));
        }

        private static void NonFiniteAndInvalidMagnitudeFailClosed()
        {
            var service = new QuantityReportRevisionService();
            Throws<InvalidOperationException>(() => service.Capture(Project("not-finite", ("E1", double.NaN)), "R1"));
            Throws<InvalidOperationException>(() => service.Capture(Project("negative", ("E1", -double.MaxValue)), "R2"));
        }

        private static ProjectState Project(string id, params (string Id, double LengthM)[] elements) =>
            ProjectWithFamilyId(id, "family", elements);

        private static ProjectState ProjectWithFamilyId(string id, string familyId, params (string Id, double LengthM)[] elements)
        {
            var project = new ProjectState(id, id);
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            project.Families.Add(new ProjectFamily(familyId, "Family", ElementCategory.Beam));
            foreach (var value in elements)
            {
                var element = new ProjectElement(value.Id, ElementCategory.Beam, familyId, "floor", "zone");
                element.Quantities["LengthM"] = value.LengthM;
                element.SourceHandles.Add("H-" + value.Id);
                project.Elements.Add(element);
            }
            return project;
        }

        private static void ReplaceElementProperties(ProjectElement element, IDictionary<string, string> properties)
        {
            var field = typeof(ProjectElement).GetField("<Properties>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectElement Properties backing field was not found.");
            field.SetValue(element, properties);
        }

        private static LiveState State(ProjectState project)
        {
            var element = project.Elements.Single();
            return new LiveState(project.ChangeVersion, project.UpdatedUtc, project.AuditEvents.Count, element.Dirty, element.UpdatedUtc, element.Quantities["LengthM"]);
        }

        private static void AssertState(ProjectState project, LiveState expected)
        {
            var element = project.Elements.Single();
            Equal(expected.ChangeVersion, project.ChangeVersion);
            Equal(expected.UpdatedUtc, project.UpdatedUtc);
            Equal(expected.AuditCount, project.AuditEvents.Count);
            Equal(expected.Dirty, element.Dirty);
            Equal(expected.ElementUpdatedUtc, element.UpdatedUtc);
            Near(expected.LengthM, element.Quantities["LengthM"]);
        }

        private readonly struct LiveState
        {
            public LiveState(long changeVersion, DateTime updatedUtc, int auditCount, ElementDirtyFlags dirty, DateTime elementUpdatedUtc, double lengthM)
            {
                ChangeVersion = changeVersion;
                UpdatedUtc = updatedUtc;
                AuditCount = auditCount;
                Dirty = dirty;
                ElementUpdatedUtc = elementUpdatedUtc;
                LengthM = lengthM;
            }

            public long ChangeVersion { get; }
            public DateTime UpdatedUtc { get; }
            public int AuditCount { get; }
            public ElementDirtyFlags Dirty { get; }
            public DateTime ElementUpdatedUtc { get; }
            public double LengthM { get; }
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

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}