using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceCheckpointSmoke
    {
        public static void Run()
        {
            RestoresExactSelectedStateWithoutTouchingAuditOrUnrelatedElements();
            RefusesProjectAndElementAffinityBeforeMutation();
            RejectsNonCanonicalCallerIds();
            RejectsKnownOversizeBeforeEnumeration();
            RejectsDishonestCountAtFirstDisallowedEntry();
            AcceptsExactBoundaryAndPreservesCaseInsensitiveIdentity();
            RestoresLongMaxValueWithoutOverflow();
        }

        private static void RestoresExactSelectedStateWithoutTouchingAuditOrUnrelatedElements()
        {
            var project = new ProjectState("P-CHECKPOINT", "Checkpoint");
            var owner = new ProjectElement("OWNER", ElementCategory.GlassWall);
            var unrelated = new ProjectElement("OTHER", ElementCategory.ArchitecturalWall);
            owner.MarkClean(ElementDirtyFlags.All);
            unrelated.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(owner);
            project.Elements.Add(unrelated);
            var audit = new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = "fixture.seed",
                ElementId = owner.Id,
                Detail = "checkpoint",
                Actor = string.Empty,
                CorrelationId = string.Empty
            };
            project.AuditEvents.Add(audit);
            project.Touch();

            var expectedVersion = project.ChangeVersion;
            var expectedProjectUtc = project.UpdatedUtc;
            var expectedOwnerDirty = owner.Dirty;
            var expectedOwnerUtc = owner.UpdatedUtc;
            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, new[] { owner.Id });
            True(checkpoint.Matches(project), "Fresh persistence checkpoint did not match its source state.");

            owner.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Quantity);
            unrelated.MarkDirty(ElementDirtyFlags.Quantity);
            project.Touch();
            var unrelatedDirty = unrelated.Dirty;
            var unrelatedUtc = unrelated.UpdatedUtc;

            False(checkpoint.Matches(project), "Changed project unexpectedly matched its earlier persistence checkpoint.");
            checkpoint.Restore(project);

            Equal(expectedVersion, project.ChangeVersion, "Project ChangeVersion was not restored exactly.");
            Equal(expectedProjectUtc, project.UpdatedUtc, "Project UpdatedUtc was not restored exactly.");
            Equal(expectedOwnerDirty, owner.Dirty, "Selected owner Dirty was not restored exactly.");
            Equal(expectedOwnerUtc, owner.UpdatedUtc, "Selected owner UpdatedUtc was not restored exactly.");
            Equal(unrelatedDirty, unrelated.Dirty, "Unrelated element Dirty was overwritten.");
            Equal(unrelatedUtc, unrelated.UpdatedUtc, "Unrelated element UpdatedUtc was overwritten.");
            Equal(1, project.AuditEvents.Count, "Audit history was mutated by persistence restore.");
            True(ReferenceEquals(audit, project.AuditEvents[0]), "Audit event identity was replaced by persistence restore.");
            True(checkpoint.Matches(project), "Restored project did not match the captured persistence checkpoint.");
        }

        private static void RefusesProjectAndElementAffinityBeforeMutation()
        {
            var source = new ProjectState("P-SOURCE", "Source");
            source.Elements.Add(new ProjectElement("E1", ElementCategory.GlassWall));
            source.Touch();
            var checkpoint = ProjectPersistenceCheckpoint.Capture(source, new[] { "E1" });

            var otherProject = new ProjectState("P-OTHER", "Other");
            var otherElement = new ProjectElement("E1", ElementCategory.GlassWall);
            otherProject.Elements.Add(otherElement);
            otherProject.Touch();
            var otherVersion = otherProject.ChangeVersion;
            var otherProjectUtc = otherProject.UpdatedUtc;
            var otherDirty = otherElement.Dirty;
            var otherElementUtc = otherElement.UpdatedUtc;
            Throws<InvalidOperationException>(() => checkpoint.Restore(otherProject));
            Equal(otherVersion, otherProject.ChangeVersion, "Cross-project refusal changed ChangeVersion.");
            Equal(otherProjectUtc, otherProject.UpdatedUtc, "Cross-project refusal changed UpdatedUtc.");
            Equal(otherDirty, otherElement.Dirty, "Cross-project refusal changed element Dirty.");
            Equal(otherElementUtc, otherElement.UpdatedUtc, "Cross-project refusal changed element UpdatedUtc.");

            var missingElementProject = new ProjectState("P-SOURCE", "Missing owner");
            missingElementProject.Touch();
            var missingVersion = missingElementProject.ChangeVersion;
            var missingUtc = missingElementProject.UpdatedUtc;
            Throws<InvalidOperationException>(() => checkpoint.Restore(missingElementProject));
            Equal(missingVersion, missingElementProject.ChangeVersion, "Missing-element refusal changed ChangeVersion.");
            Equal(missingUtc, missingElementProject.UpdatedUtc, "Missing-element refusal changed UpdatedUtc.");
        }

        private static void RejectsNonCanonicalCallerIds()
        {
            var project = new ProjectState("P-CANONICAL", "Canonical ids");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.GlassWall));
            project.Touch();
            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            foreach (var id in new[] { " E1", "E1 ", " E1 ", "\tE1", "E1\t" })
            {
                Throws<InvalidOperationException>(() => ProjectPersistenceCheckpoint.Capture(project, new[] { id }));
                Equal(version, project.ChangeVersion, "Padded-id refusal changed ChangeVersion.");
                Equal(updatedUtc, project.UpdatedUtc, "Padded-id refusal changed UpdatedUtc.");
            }

            var canonical = ProjectPersistenceCheckpoint.Capture(project, new[] { "e1" });
            Equal(1, canonical.ElementIds.Count, "Case-insensitive canonical lookup did not capture one element.");
            Equal("e1", canonical.ElementIds[0], "Checkpoint did not preserve canonical caller identity text.");
            Throws<InvalidOperationException>(() => ProjectPersistenceCheckpoint.Capture(project, new[] { "E1", "e1" }));
        }

        private static void RejectsKnownOversizeBeforeEnumeration()
        {
            var project = new ProjectState("P-KNOWN-OVERSIZE", "Known oversize");
            var source = new ThrowingOversizeCollection(10001);
            Throws<InvalidOperationException>(() => ProjectPersistenceCheckpoint.Capture(project, source));
            Equal(0, source.EnumerationCount, "Known oversized collection was enumerated before rejection.");
        }

        private static void RejectsDishonestCountAtFirstDisallowedEntry()
        {
            var project = BuildLargeProject("P-DISHONEST", 10000);
            var source = new DishonestReadOnlyCollection(1, 10001);
            Throws<InvalidOperationException>(() => ProjectPersistenceCheckpoint.Capture(project, source));
            Equal(10001, source.YieldCount, "Streaming guard did not stop at the first disallowed entry.");
            False(source.RequestedAfterLimit, "Streaming guard requested an entry after the first disallowed one.");
        }

        private static void AcceptsExactBoundaryAndPreservesCaseInsensitiveIdentity()
        {
            var project = BuildLargeProject("P-BOUNDARY", 10000);
            var ids = new List<string>(10000);
            for (var i = 0; i < 10000; i++) ids.Add("e" + i);
            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, ids);
            Equal(10000, checkpoint.ElementIds.Count, "Exact 10,000 checkpoint boundary was rejected or truncated.");
            True(checkpoint.Matches(project), "Exact-boundary checkpoint did not match its source project.");
        }

        private static ProjectState BuildLargeProject(string id, int count)
        {
            var project = new ProjectState(id, "Large checkpoint fixture");
            for (var i = 0; i < count; i++)
                project.Elements.Add(new ProjectElement("E" + i, ElementCategory.GlassWall));
            project.Touch();
            return project;
        }

        private static void RestoresLongMaxValueWithoutOverflow()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-persistence-checkpoint-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                File.WriteAllText(
                    path,
                    "<qs3d schema=\"3\" projectId=\"P-MAX\" name=\"Max checkpoint\" " +
                    "updatedUtc=\"2026-08-13T10:00:00.0000000Z\" changeVersion=\"9223372036854775807\">" +
                    "<metadata/><zones/><floors/><families/><rules/><elements>" +
                    "<element id=\"E-MAX\" category=\"GlassWall\" dirty=\"5\" updatedUtc=\"2026-08-13T09:00:00.0000000Z\">" +
                    "<handles/><dependencies/><properties/><quantities/></element>" +
                    "</elements><audit/></qs3d>");
                var project = new QsdbProjectStore().Load(path);
                var owner = project.FindElement("E-MAX") ?? throw new Exception("Max-version fixture owner is missing.");
                var checkpoint = ProjectPersistenceCheckpoint.Capture(project, new[] { owner.Id });

                checkpoint.Restore(project);
                Equal(long.MaxValue, project.ChangeVersion, "Max ChangeVersion was advanced or truncated.");
                Equal(ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations, owner.Dirty, "Max-version owner Dirty changed.");
                True(checkpoint.Matches(project), "Max-version checkpoint did not match after exact restore.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private sealed class ThrowingOversizeCollection : ICollection<string>
        {
            public ThrowingOversizeCollection(int count) { Count = count; }
            public int Count { get; }
            public bool IsReadOnly => true;
            public int EnumerationCount { get; private set; }
            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                throw new Exception("Known oversized collection must not be enumerated.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class DishonestReadOnlyCollection : IReadOnlyCollection<string>
        {
            private readonly int _actualCount;
            public DishonestReadOnlyCollection(int reportedCount, int actualCount)
            {
                Count = reportedCount;
                _actualCount = actualCount;
            }
            public int Count { get; }
            public int YieldCount { get; private set; }
            public bool RequestedAfterLimit { get; private set; }
            public IEnumerator<string> GetEnumerator()
            {
                for (var i = 0; i < _actualCount; i++)
                {
                    if (i > 10000) RequestedAfterLimit = true;
                    YieldCount++;
                    yield return i < 10000 ? "E" + i : "OVER-LIMIT";
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void False(bool value, string message) => True(!value, message);

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
