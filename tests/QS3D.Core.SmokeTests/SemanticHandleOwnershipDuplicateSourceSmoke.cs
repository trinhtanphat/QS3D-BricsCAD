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
            NumericAliasDuplicateFailsAcrossOwnershipEntryPoints();
            NumericAliasCrossOwnerAmbiguityFailsAcrossOwnershipEntryPoints();
            NumericAliasCaptureReusesExistingOwner();
            NumericAliasUserSelectionRemainsNormalized();
            NumericAliasSourceGeneratedCollisionFailsClosed();
            MalformedTextIdentityCompatibilityIsPreserved();
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

        private static void NumericAliasDuplicateFailsAcrossOwnershipEntryPoints()
        {
            var project = NewProject("A", "00a");
            AssertDuplicateStoredHandlesFail(project, "0xA");
        }

        private static void NumericAliasCrossOwnerAmbiguityFailsAcrossOwnershipEntryPoints()
        {
            var project = new ProjectState("P-NUMERIC-ALIAS-OWNERS", "Numeric alias owners");
            var first = new ProjectElement("E-1", ElementCategory.Beam);
            first.SourceHandles.Add("A");
            var second = new ProjectElement("E-2", ElementCategory.Beam);
            second.SourceHandles.Add("0xA");
            project.Elements.Add(first);
            project.Elements.Add(second);
            var beforeVersion = project.ChangeVersion;

            ThrowsAmbiguous(() => SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, "00a"));
            ThrowsAmbiguous(() => SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "A", ElementCategory.Beam, "E-NEW"));
            ThrowsAmbiguous(() => SemanticHandleOwnershipResolver.Resolve(project, new[] { "0xA" }));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void NumericAliasCaptureReusesExistingOwner()
        {
            var project = NewProject("00a");
            var element = project.Elements[0];
            var beforeVersion = project.ChangeVersion;

            var owner = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, "0xA");
            if (!ReferenceEquals(owner, element))
                throw new Exception("Numeric SourceHandle alias must resolve to the existing semantic owner.");

            var captureTarget = SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "A", ElementCategory.Beam, "BEAM-A");
            if (!ReferenceEquals(captureTarget, element))
                throw new Exception("Numeric SourceHandle alias capture must reuse the existing semantic owner.");
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void NumericAliasUserSelectionRemainsNormalized()
        {
            var project = NewProject("A");
            var element = project.Elements[0];
            var selected = SemanticHandleOwnershipResolver.Resolve(project, new[] { "A", "00a", "0xA" });
            Equal(1, selected.Count);
            if (!ReferenceEquals(selected[0], element))
                throw new Exception("Numeric SourceHandle aliases in caller selection must resolve to one semantic owner.");
        }

        private static void NumericAliasSourceGeneratedCollisionFailsClosed()
        {
            var project = NewProject("00a");
            project.Elements[0].Properties["GeneratedSolidHandle"] = "A";
            var beforeVersion = project.ChangeVersion;

            ThrowsOwnershipCollision(() => SemanticHandleOwnershipResolver.Resolve(project, new[] { "0xA" }));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void MalformedTextIdentityCompatibilityIsPreserved()
        {
            var project = NewProject("NOT-HEX", "0");
            var element = project.Elements[0];
            var beforeVersion = project.ChangeVersion;

            if (!ReferenceEquals(SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, " not-hex "), element))
                throw new Exception("Malformed textual SourceHandle compatibility changed unexpectedly.");
            if (!ReferenceEquals(SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "NOT-HEX", ElementCategory.Beam, "E-1"), element))
                throw new Exception("Malformed textual SourceHandle capture compatibility changed unexpectedly.");
            if (!ReferenceEquals(SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, " 0 "), element))
                throw new Exception("Zero textual SourceHandle compatibility changed unexpectedly.");

            var selected = SemanticHandleOwnershipResolver.Resolve(project, new[] { "not-hex", "NOT-HEX", "0" });
            Equal(1, selected.Count);
            if (!ReferenceEquals(selected[0], element))
                throw new Exception("Malformed textual SourceHandle selection compatibility changed unexpectedly.");
            Equal(beforeVersion, project.ChangeVersion);
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

            var captureTarget = SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, " 1a ", ElementCategory.Beam, "E-1");
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

        private static void ThrowsAmbiguous(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("multiple semantic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ex.Message.IndexOf("ambiguously owned", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception("Numeric SourceHandle cross-owner refusal lost its ownership diagnostic.", ex);
            }
            throw new Exception("Expected numeric SourceHandle cross-owner ambiguity refusal.");
        }

        private static void ThrowsOwnershipCollision(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("conflicting ownership channels", StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new Exception("Numeric SourceHandle/generated-owner collision lost its channel diagnostic.", ex);
            }
            throw new Exception("Expected numeric SourceHandle/generated-owner collision refusal.");
        }
    }
}
