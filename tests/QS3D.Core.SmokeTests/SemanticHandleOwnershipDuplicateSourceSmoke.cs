using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleOwnershipDuplicateSourceSmoke
    {
        internal static void Run()
        {
            ExactDuplicateFailsAcrossOwnershipEntryPoints();
            CaseOnlyDuplicateFailsAcrossOwnershipEntryPoints();
            CanonicalUniqueHandlesStillResolve();
            DuplicateUserSelectionRemainsNormalized();
        }

        private static void ExactDuplicateFailsAcrossOwnershipEntryPoints()
        {
            var project = NewProject("1A", "1A");
            AssertDuplicateStoredHandlesFail(project, "1A");
        }

        private static void CaseOnlyDuplicateFailsAcrossOwnershipEntryPoints()
        {
            var project = NewProject("1A", "1a");
            AssertDuplicateStoredHandlesFail(project, "1A");
        }

        private static void AssertDuplicateStoredHandlesFail(ProjectState project, string queryHandle)
        {
            var beforeVersion = project.ChangeVersion;
            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, queryHandle));
            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, queryHandle, ElementCategory.Beam, "E-1"));
            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.Resolve(project, new[] { queryHandle }));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void CanonicalUniqueHandlesStillResolve()
        {
            var project = NewProject("1A", "2B");
            var element = project.Elements[0];
            var beforeVersion = project.ChangeVersion;

            var owner = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, " 1a ");
            if (!ReferenceEquals(owner, element))
                throw new Exception("Canonical unique SourceHandle owner resolution changed unexpectedly.");

            var captureTarget = SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, " 1a ", ElementCategory.Beam, " E-1 ");
            if (!ReferenceEquals(captureTarget, element))
                throw new Exception("Canonical unique SourceHandle capture-target resolution changed unexpectedly.");

            var selected = SemanticHandleOwnershipResolver.Resolve(project, new[] { "2b" });
            Equal(1, selected.Count);
            if (!ReferenceEquals(selected[0], element))
                throw new Exception("Canonical unique selected SourceHandle resolution changed unexpectedly.");
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void DuplicateUserSelectionRemainsNormalized()
        {
            var project = NewProject("1A");
            var element = project.Elements[0];
            var selected = SemanticHandleOwnershipResolver.Resolve(project, new[] { "1a", " 1A ", "1A" });
            Equal(1, selected.Count);
            if (!ReferenceEquals(selected[0], element))
                throw new Exception("Duplicate user selection inputs must remain normalized to one semantic owner.");
        }

        private static ProjectState NewProject(params string[] storedHandles)
        {
            var project = new ProjectState("P-DUP-SOURCE", "Duplicate source ownership");
            var element = new ProjectElement("E-1", ElementCategory.Beam);
            foreach (var handle in storedHandles) element.SourceHandles.Add(handle);
            project.Elements.Add(element);
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf("duplicate SourceHandles", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Duplicate stored SourceHandle rejection must explain the duplicate ownership state.", ex);
                return;
            }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }
    }
}
