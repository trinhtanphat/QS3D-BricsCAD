using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectRecognitionInputFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StableBatchStillRuns();
            MutatingBatchIsRejected();
            MutatingEmptyBatchIsRejected();
        }

        private static void StableBatchStillRuns()
        {
            var project = new ProjectState("P-RECOGNITION-STABLE", "Recognition stable");
            var batch = new ProjectRecognitionService().SuggestBatch(
                project,
                new[] { new EntitySnapshot("10", "Line", "beam") });

            if (batch.Results.Count != 1 || !string.Equals(batch.Results[0].Handle, "10", StringComparison.Ordinal))
                throw new InvalidOperationException("Stable recognition batch no longer returns the expected snapshot result.");
            if (project.ChangeVersion != 0L)
                throw new InvalidOperationException("Stable recognition batch unexpectedly changed the project revision.");
        }

        private static void MutatingBatchIsRejected()
        {
            var project = new ProjectState("P-RECOGNITION-MUTATING", "Recognition mutating");
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => new ProjectRecognitionService().SuggestBatch(
                project,
                MutatingSnapshots(project, yieldSnapshot: true)));

            if (project.ChangeVersion != checked(beforeVersion + 1L))
                throw new InvalidOperationException("Caller-side mutation should remain visible after stale recognition input rejection.");
        }

        private static void MutatingEmptyBatchIsRejected()
        {
            var project = new ProjectState("P-RECOGNITION-EMPTY", "Recognition empty");
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => new ProjectRecognitionService().SuggestBatch(
                project,
                MutatingSnapshots(project, yieldSnapshot: false)));

            if (project.ChangeVersion != checked(beforeVersion + 1L))
                throw new InvalidOperationException("Mutating-empty recognition input did not preserve the caller-side project mutation.");
        }

        private static IEnumerable<EntitySnapshot> MutatingSnapshots(ProjectState project, bool yieldSnapshot)
        {
            project.Touch();
            if (yieldSnapshot)
                yield return new EntitySnapshot("11", "Line", "beam");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
