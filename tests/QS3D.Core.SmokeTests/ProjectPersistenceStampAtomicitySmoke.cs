using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampAtomicitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            FailedMarkSavedDoesNotPublishScalarBaseline();
            FailedMarkSavedDoesNotPublishMetadataBaseline();
        }

        private static void FailedMarkSavedDoesNotPublishScalarBaseline()
        {
            var project = new ProjectState("P-STAMP-SCALAR", "Persistence stamp scalar atomicity");
            var stamp = new ProjectPersistenceStamp(project);
            var savedVersion = stamp.SavedChangeVersion;

            project.DrawingFingerprint = "DWG-AFTER-SAVE";
            Require(project.ChangeVersion != savedVersion, "Scalar control must advance the live project version.");
            AddOversizedNestedFamilyProperties(project);

            ExpectInvalidOperation(() => stamp.MarkSaved(project), "Oversized nested content must fail MarkSaved.");
            Require(stamp.SavedChangeVersion == savedVersion,
                "Failed MarkSaved must preserve the last successful SavedChangeVersion.");
        }

        private static void FailedMarkSavedDoesNotPublishMetadataBaseline()
        {
            var project = new ProjectState("P-STAMP-METADATA", "Persistence stamp metadata atomicity");
            var stamp = new ProjectPersistenceStamp(project);

            project.Metadata["AtomicityProbe"] = "changed-after-save";
            Require(stamp.RequiresSave(project), "Tracked metadata mutation must require save before the failure probe.");
            AddOversizedNestedFamilyProperties(project);

            ExpectInvalidOperation(() => stamp.MarkSaved(project), "Oversized nested content must fail MarkSaved.");
            project.Families.Clear();

            Require(stamp.RequiresSave(project),
                "Failed MarkSaved must not accept staged metadata into the saved baseline.");
            stamp.MarkSaved(project);
            Require(!stamp.RequiresSave(project), "A later successful MarkSaved must publish the complete new baseline.");
        }

        private static void AddOversizedNestedFamilyProperties(ProjectState project)
        {
            var family = new ProjectFamily("F-ATOMICITY", "Atomicity failure probe", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            for (var i = 0; i <= 10_000; i++)
                family.Properties.Add("P-" + i.ToString("D5"), "value");
        }

        private static void ExpectInvalidOperation(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
