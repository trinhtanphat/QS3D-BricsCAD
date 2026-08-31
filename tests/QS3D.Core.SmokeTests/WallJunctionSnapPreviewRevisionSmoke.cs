using System;
using System.Globalization;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionSnapPreviewRevisionSmoke
    {
        private const string PreviewPlanHashKey = "WallJunctionSnapPreviewPlanHash";
        private const string PreviewSourceFingerprintKey = "WallJunctionSnapPreviewSourceFingerprint";
        private const string PreviewCountKey = "WallJunctionSnapPreviewCount";
        private const string PreviewUtcKey = "WallJunctionSnapPreviewUtc";
        private const string PreviewProjectIdKey = "WallJunctionSnapPreviewProjectId";
        private const string PreviewChangeVersionKey = "WallJunctionSnapPreviewChangeVersion";

        public static void Run()
        {
            PreviewPublicationUsesTwoBoundedRevisionsAndKeepsApprovalFresh();
            PreviewCleanupUsesOneBoundedRevisionForEmptyAndAppliedPlans();
            OrdinaryProjectMetadataStillMarksSemanticStateDirty();
            PreviewPrefixLookalikeStillMarksSemanticStateDirty();
        }

        private static void PreviewPublicationUsesTwoBoundedRevisionsAndKeepsApprovalFresh()
        {
            var project = NewProject();
            var before = project.ChangeVersion;

            PublishPreviewLikeProduction(project, "plan-a", "source-a", 1);

            Require(project.ChangeVersion == before + 2L,
                "preview publication must consume exactly audit + final publication revisions.");
            Require(project.Metadata.TryGetValue(PreviewChangeVersionKey, out var text),
                "preview approval version was not published.");
            Require(long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var approved),
                "preview approval version is not an integer.");
            Require(approved == project.ChangeVersion,
                "preview approval version must equal the final project ChangeVersion.");
        }

        private static void PreviewCleanupUsesOneBoundedRevisionForEmptyAndAppliedPlans()
        {
            var emptyPlanProject = NewProject();
            PublishPreviewLikeProduction(emptyPlanProject, "plan-empty", "source-empty", 0);
            var beforeEmptyCleanup = emptyPlanProject.ChangeVersion;
            Require(ClearPreviewLikeProduction(emptyPlanProject), "empty-plan preview cleanup removed no keys.");
            emptyPlanProject.Touch();
            Require(emptyPlanProject.ChangeVersion == beforeEmptyCleanup + 1L,
                "empty-plan cleanup must consume only its explicit publication revision.");

            var appliedProject = NewProject();
            PublishPreviewLikeProduction(appliedProject, "plan-apply", "source-apply", 1);
            var beforeApplyCleanup = appliedProject.ChangeVersion;
            Require(ClearPreviewLikeProduction(appliedProject), "apply preview cleanup removed no keys.");
            AuditTrail.ForProject(appliedProject).Record(
                "wall.junction.snap.apply",
                string.Empty,
                "1 endpoint edit(s)");
            Require(appliedProject.ChangeVersion == beforeApplyCleanup + 1L,
                "applied preview cleanup must consume only the apply audit revision.");
        }

        private static void OrdinaryProjectMetadataStillMarksSemanticStateDirty()
        {
            var project = NewProject();
            var before = project.ChangeVersion;
            project.Metadata["WallJunctionToleranceM"] = "0.005";
            Require(project.ChangeVersion == before + 1L,
                "ordinary project metadata must continue to advance ChangeVersion.");
        }

        private static void PreviewPrefixLookalikeStillMarksSemanticStateDirty()
        {
            const string key = "WallJunctionSnapPreviewCustomerData";
            var project = NewProject();

            var beforeSet = project.ChangeVersion;
            project.Metadata[key] = "alpha";
            Require(project.ChangeVersion == beforeSet + 1L,
                "public preview-prefix lookalike set must advance ChangeVersion.");

            var beforeUpdate = project.ChangeVersion;
            project.Metadata[key] = "beta";
            Require(project.ChangeVersion == beforeUpdate + 1L,
                "public preview-prefix lookalike update must advance ChangeVersion.");

            var beforeRemove = project.ChangeVersion;
            Require(project.Metadata.Remove(key), "public preview-prefix lookalike remove found no key.");
            Require(project.ChangeVersion == beforeRemove + 1L,
                "public preview-prefix lookalike remove must advance ChangeVersion.");

            var beforeAdd = project.ChangeVersion;
            project.Metadata.Add(key, "gamma");
            Require(project.ChangeVersion == beforeAdd + 1L,
                "public preview-prefix lookalike Add must advance ChangeVersion.");

            var beforeClear = project.ChangeVersion;
            project.Metadata.Clear();
            Require(project.ChangeVersion == beforeClear + 1L,
                "clearing public preview-prefix lookalike metadata must advance ChangeVersion.");
        }

        private static ProjectState NewProject()
        {
            return new ProjectState("WALL-SNAP-REVISION-SMOKE", "Wall Snap Revision Smoke");
        }

        private static void PublishPreviewLikeProduction(ProjectState project, string planHash, string sourceFingerprint, int count)
        {
            project.Metadata[PreviewPlanHashKey] = planHash;
            project.Metadata[PreviewSourceFingerprintKey] = sourceFingerprint;
            project.Metadata[PreviewCountKey] = count.ToString(CultureInfo.InvariantCulture);
            project.Metadata[PreviewUtcKey] = "2026-08-23T00:00:00.0000000Z";
            project.Metadata[PreviewProjectIdKey] = project.ProjectId;
            AuditTrail.ForProject(project).Record(
                "wall.junction.snap.preview",
                string.Empty,
                count.ToString(CultureInfo.InvariantCulture) + " endpoint edit(s)");
            var approvedVersion = checked(project.ChangeVersion + 1L);
            project.Metadata[PreviewChangeVersionKey] = approvedVersion.ToString(CultureInfo.InvariantCulture);
            project.Touch();
        }

        private static bool ClearPreviewLikeProduction(ProjectState project)
        {
            var changed = false;
            changed |= project.Metadata.Remove(PreviewPlanHashKey);
            changed |= project.Metadata.Remove(PreviewSourceFingerprintKey);
            changed |= project.Metadata.Remove(PreviewCountKey);
            changed |= project.Metadata.Remove(PreviewUtcKey);
            changed |= project.Metadata.Remove(PreviewProjectIdKey);
            changed |= project.Metadata.Remove(PreviewChangeVersionKey);
            return changed;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Wall Junction Snap preview revision smoke failed: " + message);
        }
    }
}