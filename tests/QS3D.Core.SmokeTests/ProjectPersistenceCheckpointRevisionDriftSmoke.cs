using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceCheckpointRevisionDriftSmoke
    {
        public static void Run()
        {
            RejectsCapturedElementPersistenceDriftWithoutProjectRevisionChange();
            StableCaptureStillMatches();
        }

        private static void RejectsCapturedElementPersistenceDriftWithoutProjectRevisionChange()
        {
            var project = new ProjectState("P-CHECKPOINT-ELEMENT-DRIFT", "Checkpoint element drift");
            var first = new ProjectElement("E1", ElementCategory.GlassWall);
            var second = new ProjectElement("E2", ElementCategory.GlassWall);
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);
            project.Touch();

            var projectVersion = project.ChangeVersion;
            var projectUpdatedUtc = project.UpdatedUtc;
            var rejected = false;
            try
            {
                ProjectPersistenceCheckpoint.Capture(project, MutateCapturedElementAfterFirstYield(first));
            }
            catch (InvalidOperationException ex)
            {
                rejected = ex.Message.IndexOf("captured element persistence state is changing", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            Require(rejected, "Persistence checkpoint accepted captured element state that changed during lazy enumeration.");
            Require(project.ChangeVersion == projectVersion,
                "Element-only checkpoint drift fixture unexpectedly changed the project ChangeVersion.");
            Require(project.UpdatedUtc == projectUpdatedUtc,
                "Element-only checkpoint drift fixture unexpectedly changed the project UpdatedUtc.");
            Require((first.Dirty & ElementDirtyFlags.Quantity) != 0,
                "Element-only checkpoint drift fixture did not mutate captured persistence state.");
        }

        private static IEnumerable<string> MutateCapturedElementAfterFirstYield(ProjectElement first)
        {
            yield return "E1";
            first.MarkDirty(ElementDirtyFlags.Quantity);
            yield return "E2";
        }

        private static void StableCaptureStillMatches()
        {
            var project = new ProjectState("P-CHECKPOINT-ELEMENT-STABLE", "Checkpoint stable");
            var first = new ProjectElement("E1", ElementCategory.GlassWall);
            var second = new ProjectElement("E2", ElementCategory.GlassWall);
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);
            project.Touch();

            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, new[] { "e1", "E2" });
            Require(checkpoint.Matches(project), "Stable captured-element checkpoint no longer matches its source revision.");
            Require(checkpoint.ElementIds.Count == 2, "Stable captured-element checkpoint lost an owner.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
