using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSemanticMutationExecutorSmoke
    {
        internal static void Run()
        {
            SuccessfulMutationRecordsOrderedPhases();
            MutationExceptionRestoresCompleteProjectState();
            PreCommitFaultRollsBackCompletedInterchangeMutation();
            InvalidOperationNameFailsBeforeMutation();
        }

        private static void SuccessfulMutationRecordsOrderedPhases()
        {
            var project = new ProjectState("P", "Project");
            var journal = new ProjectSemanticMutationJournal();
            var result = ProjectSemanticMutationExecutor.Execute(
                project,
                "smoke-success",
                () =>
                {
                    project.Metadata["A"] = "1";
                    project.Touch();
                    return 42;
                },
                () =>
                {
                    if (project.Metadata["A"] != "1") throw new Exception("Mutation result unavailable during validation.");
                },
                journal);

            Equal(42, result);
            Equal("1", project.Metadata["A"]);
            Equal("Planned|Running|Validating|Committed", string.Join("|", journal.Entries.Select(x => x.Phase.ToString())));
            Equal("1|2|3|4", string.Join("|", journal.Entries.Select(x => x.Sequence.ToString())));
        }

        private static void MutationExceptionRestoresCompleteProjectState()
        {
            var project = BaselineProject();
            var originalElement = project.FindElement("E1") ?? throw new Exception("Baseline element missing.");
            var originalUpdated = project.UpdatedUtc;
            var originalVersion = project.ChangeVersion;
            var originalAuditCount = project.AuditEvents.Count;
            var journal = new ProjectSemanticMutationJournal();

            Throws<InvalidOperationException>(() => ProjectSemanticMutationExecutor.Execute<int>(
                project,
                "smoke-rollback",
                () =>
                {
                    originalElement.Properties["Mark"] = "MUTATED";
                    project.Metadata["Injected"] = "yes";
                    project.Elements.Add(new ProjectElement("E2", ElementCategory.Column));
                    project.Touch();
                    throw new InvalidOperationException("injected mutation failure");
                },
                journal));

            Equal(1, project.Elements.Count);
            Equal("BASE", (project.FindElement("E1") ?? throw new Exception("Restored element missing.")).Properties["Mark"]);
            False(project.Metadata.ContainsKey("Injected"));
            Equal(originalUpdated, project.UpdatedUtc);
            Equal(originalVersion, project.ChangeVersion);
            Equal(originalAuditCount, project.AuditEvents.Count);
            Equal("Planned|Running|RollingBack|RolledBack", string.Join("|", journal.Entries.Select(x => x.Phase.ToString())));
        }

        private static void PreCommitFaultRollsBackCompletedInterchangeMutation()
        {
            var target = new ProjectState("TARGET", "Target");
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = "SOURCE-DWG",
                UpdatedUtc = new DateTime(2026, 8, 11, 1, 30, 0, DateTimeKind.Utc)
            };
            var sourceElement = new ProjectElement("E2", ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            sourceElement.SourceHandles.Add("SOURCE-H2");
            sourceElement.Properties["Mark"] = "SOURCE";
            source.Elements.Add(sourceElement);
            var json = ProjectInterchangeJsonExporter.Build(source);
            var journal = new ProjectSemanticMutationJournal();
            var beforeUpdated = target.UpdatedUtc;
            var beforeVersion = target.ChangeVersion;

            Throws<InvalidOperationException>(() => ProjectSemanticMutationExecutor.Execute<ProjectInterchangeImportCoordinatorResult>(
                target,
                "interchange-before-commit-fault",
                () => ProjectInterchangeImportCoordinator.Execute(
                    target,
                    json,
                    new ProjectInterchangeImportRequest
                    {
                        Mode = ProjectInterchangeImportExecutionMode.AppendOnly,
                        PreserveSourceHandleProvenance = true
                    },
                    ProjectInterchangeNativeCleanupAuthorization.None),
                () => { throw new InvalidOperationException("injected post-import validation fault"); },
                journal));

            Equal(0, target.Elements.Count);
            Equal(0, target.Metadata.Count);
            Equal(0, target.AuditEvents.Count);
            Equal(beforeUpdated, target.UpdatedUtc);
            Equal(beforeVersion, target.ChangeVersion);
            Equal(0, ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E2").Count);
            Equal("Planned|Running|Validating|RollingBack|RolledBack", string.Join("|", journal.Entries.Select(x => x.Phase.ToString())));
        }

        private static void InvalidOperationNameFailsBeforeMutation()
        {
            var project = BaselineProject();
            var invoked = false;
            Throws<ArgumentException>(() => ProjectSemanticMutationExecutor.Execute(
                project,
                "   ",
                () =>
                {
                    invoked = true;
                    return 1;
                }));
            False(invoked);
            Equal("BASE", (project.FindElement("E1") ?? throw new Exception("Baseline element missing.")).Properties["Mark"]);
        }

        private static ProjectState BaselineProject()
        {
            var project = new ProjectState("P", "Project")
            {
                UpdatedUtc = new DateTime(2026, 8, 11, 1, 25, 0, DateTimeKind.Utc)
            };
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["Mark"] = "BASE";
            project.Elements.Add(element);
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected condition to be false.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectSemanticMutationExecutorSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectSemanticMutationExecutorSmoke.Run();
    }
}
