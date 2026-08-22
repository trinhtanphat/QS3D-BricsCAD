using System;
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

                // A Touch()-based restore would overflow here. Exact internal restore
                // must remain a no-advance operation even at the version boundary.
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
