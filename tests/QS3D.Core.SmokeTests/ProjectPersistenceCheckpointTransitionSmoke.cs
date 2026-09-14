using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceCheckpointTransitionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RestoresVerifiedPriorGenerationFromExactCurrentCheckpoint();
            RejectsProjectRevisionDriftAfterGuard();
            RejectsTargetSemanticMismatch();
        }

        private static void RestoresVerifiedPriorGenerationFromExactCurrentCheckpoint()
        {
            var project = new ProjectState("P-CHECKPOINT-TRANSITION", "Checkpoint transition");
            var owner = new ProjectElement("E1", ElementCategory.GlassWall);
            owner.SetProperty("GeneratedSolidHandle", "A1");
            owner.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(owner);
            project.Touch();

            var beforeVersion = project.ChangeVersion;
            var before = ProjectPersistenceCheckpoint.Capture(project, new[] { owner.Id });
            True(before.Matches(project), "Before checkpoint did not match its source state.");
            owner.SetProperty("GeneratedSolidHandle", "B2");
            project.Touch();
            var afterVersion = project.ChangeVersion;
            var after = ProjectPersistenceCheckpoint.Capture(project, new[] { owner.Id });
            True(after.Matches(project), "Current checkpoint did not match committed newer state.");
            True(afterVersion > beforeVersion, "Fixture did not advance the project revision.");

            var guard = after.PrepareTransitionRestore(project);
            owner.SetProperty("GeneratedSolidHandle", "A1");
            before.RestoreTransition(project, guard);

            Equal("A1", owner.Properties["GeneratedSolidHandle"], "Transition restore changed target semantic state.");
            Equal(beforeVersion, project.ChangeVersion, "Transition restore did not restore target project revision.");
            True(before.Matches(project), "Transition restore did not reproduce the verified target checkpoint.");
        }

        private static void RejectsProjectRevisionDriftAfterGuard()
        {
            var project = NewTransitionProject("P-CHECKPOINT-TRANSITION-REVISION", out var owner);
            var target = ProjectPersistenceCheckpoint.Capture(project, new[] { owner.Id });
            owner.SetProperty("GeneratedSolidHandle", "B2");
            project.Touch();
            var current = ProjectPersistenceCheckpoint.Capture(project, new[] { owner.Id });
            var guard = current.PrepareTransitionRestore(project);
            project.Name = "Newer project revision";
            var newerVersion = project.ChangeVersion;
            Throws<InvalidOperationException>(() => target.RestoreTransition(project, guard));
            Equal(newerVersion, project.ChangeVersion, "Rejected transition changed newer project revision.");
            Equal("B2", owner.Properties["GeneratedSolidHandle"], "Rejected transition changed current semantic state.");
        }

        private static void RejectsTargetSemanticMismatch()
        {
            var project = NewTransitionProject("P-CHECKPOINT-TRANSITION-SEMANTIC", out var owner);
            var target = ProjectPersistenceCheckpoint.Capture(project, new[] { owner.Id });
            owner.SetProperty("GeneratedSolidHandle", "B2");
            project.Touch();
            var current = ProjectPersistenceCheckpoint.Capture(project, new[] { owner.Id });
            var guard = current.PrepareTransitionRestore(project);
            var currentVersion = project.ChangeVersion;
            Throws<InvalidOperationException>(() => target.RestoreTransition(project, guard));
            Equal(currentVersion, project.ChangeVersion, "Semantic rejection changed project revision.");
            Equal("B2", owner.Properties["GeneratedSolidHandle"], "Semantic rejection changed current ownership.");
        }

        private static ProjectState NewTransitionProject(string id, out ProjectElement owner)
        {
            var project = new ProjectState(id, "Checkpoint transition");
            owner = new ProjectElement("E1", ElementCategory.GlassWall);
            owner.SetProperty("GeneratedSolidHandle", "A1");
            owner.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(owner);
            project.Touch();
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectPersistenceCheckpointTransitionSmoke expected " + typeof(T).Name + ".");
        }
        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("ProjectPersistenceCheckpointTransitionSmoke " + message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectPersistenceCheckpointTransitionSmoke " + message +
                    " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
