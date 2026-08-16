using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticCaptureTargetCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalIdResolvesCaseInsensitively();
            MissingCanonicalIdRemainsMissing();
            ExistingSourceOwnerStillResolves();
            RejectsLeadingWhitespaceBeforeOwnershipTraversal();
            RejectsTrailingWhitespace();
            RejectsTwoSidedWhitespace();
            RejectsTabPadding();
            RejectsBlankCanonicalId();
            CategoryMismatchStillFailsClosed();
            RejectionDoesNotMutateProject();
        }

        private static void CanonicalIdResolvesCaseInsensitively()
        {
            var project = ProjectWith(new ProjectElement("ELEMENT-1", ElementCategory.Beam));
            var resolved = SemanticHandleOwnershipResolver.ResolveCaptureTarget(
                project,
                "A1",
                ElementCategory.Beam,
                "element-1");
            Same(project.Elements[0], resolved, "canonical case-insensitive target");
        }

        private static void MissingCanonicalIdRemainsMissing()
        {
            var project = ProjectWith(new ProjectElement("ELEMENT-1", ElementCategory.Beam));
            var resolved = SemanticHandleOwnershipResolver.ResolveCaptureTarget(
                project,
                "A1",
                ElementCategory.Beam,
                "ELEMENT-2");
            if (resolved != null) throw new Exception("Missing canonical target must remain unresolved.");
        }

        private static void ExistingSourceOwnerStillResolves()
        {
            var owner = new ProjectElement("ELEMENT-1", ElementCategory.Beam);
            owner.SourceHandles.Add("A1");
            var project = ProjectWith(owner);
            var resolved = SemanticHandleOwnershipResolver.ResolveCaptureTarget(
                project,
                "a1",
                ElementCategory.Beam,
                "element-1");
            Same(owner, resolved, "existing source owner");
        }

        private static void RejectsLeadingWhitespaceBeforeOwnershipTraversal()
        {
            var first = new ProjectElement("ELEMENT-1", ElementCategory.Beam);
            var second = new ProjectElement("ELEMENT-2", ElementCategory.Beam);
            first.SourceHandles.Add("A1");
            second.SourceHandles.Add("A1");
            var project = ProjectWith(first, second);

            var ex = ExpectArgument(() => SemanticHandleOwnershipResolver.ResolveCaptureTarget(
                project,
                "A1",
                ElementCategory.Beam,
                " ELEMENT-1"));
            Contains(ex.Message, "Canonical element ID", "leading whitespace diagnostic");
        }

        private static void RejectsTrailingWhitespace()
        {
            RejectPadding("ELEMENT-1 ", "trailing whitespace");
        }

        private static void RejectsTwoSidedWhitespace()
        {
            RejectPadding(" ELEMENT-1 ", "two-sided whitespace");
        }

        private static void RejectsTabPadding()
        {
            RejectPadding("\tELEMENT-1\t", "tab padding");
        }

        private static void RejectsBlankCanonicalId()
        {
            var project = ProjectWith(new ProjectElement("ELEMENT-1", ElementCategory.Beam));
            var ex = ExpectArgument(() => SemanticHandleOwnershipResolver.ResolveCaptureTarget(
                project,
                "A1",
                ElementCategory.Beam,
                "   "));
            Contains(ex.Message, "Canonical element ID", "blank canonical id diagnostic");
        }

        private static void CategoryMismatchStillFailsClosed()
        {
            var project = ProjectWith(new ProjectElement("ELEMENT-1", ElementCategory.Beam));
            var ex = ExpectInvalid(() => SemanticHandleOwnershipResolver.ResolveCaptureTarget(
                project,
                "A1",
                ElementCategory.Column,
                "ELEMENT-1"));
            Contains(ex.Message, "category", "category mismatch diagnostic");
        }

        private static void RejectionDoesNotMutateProject()
        {
            var element = new ProjectElement("ELEMENT-1", ElementCategory.Beam);
            var project = ProjectWith(element);
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.Elements.Count;

            ExpectArgument(() => SemanticHandleOwnershipResolver.ResolveCaptureTarget(
                project,
                "A1",
                ElementCategory.Beam,
                " ELEMENT-1 "));

            if (project.ChangeVersion != beforeVersion)
                throw new Exception("Rejected capture target ID must not advance project ChangeVersion.");
            if (project.Elements.Count != beforeCount || !ReferenceEquals(project.Elements[0], element))
                throw new Exception("Rejected capture target ID must not mutate project element ownership.");
        }

        private static void RejectPadding(string canonicalId, string label)
        {
            var project = ProjectWith(new ProjectElement("ELEMENT-1", ElementCategory.Beam));
            var ex = ExpectArgument(() => SemanticHandleOwnershipResolver.ResolveCaptureTarget(
                project,
                "A1",
                ElementCategory.Beam,
                canonicalId));
            Contains(ex.Message, "Canonical element ID", label + " diagnostic");
        }

        private static ProjectState ProjectWith(params ProjectElement[] elements)
        {
            var project = new ProjectState("PROJECT-1", "Smoke");
            foreach (var element in elements) project.Elements.Add(element);
            return project;
        }

        private static ArgumentException ExpectArgument(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                return ex;
            }
            throw new Exception("Expected ArgumentException.");
        }

        private static InvalidOperationException ExpectInvalid(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }
            throw new Exception("Expected InvalidOperationException.");
        }

        private static void Same(object expected, object? actual, string label)
        {
            if (!ReferenceEquals(expected, actual))
                throw new Exception(label + " did not resolve the expected semantic element instance.");
        }

        private static void Contains(string text, string expected, string label)
        {
            if (text.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new Exception(label + " missing expected text: " + expected + ". Actual: " + text);
        }
    }
}
