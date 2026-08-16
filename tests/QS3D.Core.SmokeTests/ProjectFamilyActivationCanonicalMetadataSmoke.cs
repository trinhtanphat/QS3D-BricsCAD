using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationCanonicalMetadataSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PaddedMetadataIsCanonicalized();
            CaseVariantMetadataIsCanonicalized();
            CanonicalRepeatedActivationIsNoOp();
            DuplicateFamilyIdsStillFailClosed();
        }

        private static void PaddedMetadataIsCanonicalized()
        {
            var project = Project(out var family);
            project.Metadata["ActiveFamilyId"] = " Family-A ";
            var before = project.ChangeVersion;

            ProjectFamilyActivationService.SetActive(project, "family-a");

            AssertMetadata(project, "Family-A", "Padded ActiveFamilyId metadata must be rewritten to the canonical family id.");
            Assert(project.ChangeVersion == before + 1L, "Canonicalizing padded ActiveFamilyId metadata must advance ChangeVersion exactly once.");
            Assert(ReferenceEquals(ProjectFamilyActivationService.GetActive(project), family), "Canonicalized ActiveFamilyId must still resolve the intended family.");
        }

        private static void CaseVariantMetadataIsCanonicalized()
        {
            var project = Project(out var family);
            project.Metadata["ActiveFamilyId"] = "family-a";
            var before = project.ChangeVersion;

            ProjectFamilyActivationService.SetActive(project, "Family-A");

            AssertMetadata(project, "Family-A", "Case-variant ActiveFamilyId metadata must be rewritten using exact canonical casing.");
            Assert(project.ChangeVersion == before + 1L, "Canonicalizing case-variant ActiveFamilyId metadata must advance ChangeVersion exactly once.");
            Assert(ReferenceEquals(ProjectFamilyActivationService.GetActive(project), family), "Case canonicalization must preserve the active family.");
        }

        private static void CanonicalRepeatedActivationIsNoOp()
        {
            var project = Project(out _);
            ProjectFamilyActivationService.SetActive(project, "Family-A");
            var before = project.ChangeVersion;

            ProjectFamilyActivationService.SetActive(project, " family-a ");

            AssertMetadata(project, "Family-A", "Repeated activation must preserve exact canonical ActiveFamilyId metadata.");
            Assert(project.ChangeVersion == before, "Repeated activation with already-canonical metadata must remain a no-op.");
        }

        private static void DuplicateFamilyIdsStillFailClosed()
        {
            var project = new ProjectState("family-activation-duplicate", "Family activation duplicate");
            project.Families.Add(new ProjectFamily("Family-A", "Family A", ElementCategory.Column));
            project.Families.Add(new ProjectFamily("family-a", "Family A duplicate", ElementCategory.Beam));
            var before = project.ChangeVersion;

            Capture<InvalidOperationException>(() => ProjectFamilyActivationService.SetActive(project, "Family-A"));

            Assert(!project.Metadata.ContainsKey("ActiveFamilyId"), "Duplicate family ids must fail before ActiveFamilyId metadata mutation.");
            Assert(project.ChangeVersion == before, "Duplicate family ids must fail without changing project persistence state.");
        }

        private static ProjectState Project(out ProjectFamily family)
        {
            var project = new ProjectState("family-activation-canonical", "Family activation canonical");
            family = new ProjectFamily("Family-A", "Family A", ElementCategory.Column);
            project.Families.Add(family);
            return project;
        }

        private static void AssertMetadata(ProjectState project, string expected, string message)
        {
            Assert(project.Metadata.TryGetValue("ActiveFamilyId", out var actual), message + " Metadata is missing.");
            Assert(string.Equals(actual, expected, StringComparison.Ordinal), message + " Actual='" + actual + "'.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
