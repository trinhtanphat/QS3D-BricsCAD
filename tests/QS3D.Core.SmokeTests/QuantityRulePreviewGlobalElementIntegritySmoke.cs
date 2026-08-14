using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRulePreviewGlobalElementIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsUnrelatedDuplicateElementIds();
            RejectsNullElementCollectionMembers();
            PreservesCanonicalPreview();
        }

        private static void RejectsUnrelatedDuplicateElementIds()
        {
            var project = new ProjectState("QPREVIEW-DUP", "Quantity preview duplicate");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("e1", ElementCategory.Beam));
            var target = new ProjectElement("E2", ElementCategory.Beam);
            project.Elements.Add(target);
            var service = new QuantityRulePreviewService();

            ThrowsDuplicateExact(() => service.PreviewElement(project, target));
            ThrowsDuplicateExact(() => service.PreviewProject(project));
        }

        private static void RejectsNullElementCollectionMembers()
        {
            var project = new ProjectState("QPREVIEW-NULL", "Quantity preview null element");
            var target = new ProjectElement("E1", ElementCategory.Beam);
            project.Elements.Add(target);
            project.Elements.Add(null!);
            var service = new QuantityRulePreviewService();

            ThrowsNullExact(() => service.PreviewElement(project, target));
            ThrowsNullExact(() => service.PreviewProject(project));

            var singleton = new ProjectState("QPREVIEW-NULL-ONLY", "Quantity preview singleton null");
            singleton.Elements.Add(null!);
            ThrowsNullExact(() => service.PreviewProject(singleton));
        }

        private static void PreservesCanonicalPreview()
        {
            var project = new ProjectState("QPREVIEW-VALID", "Quantity preview valid");
            var first = new ProjectElement("E1", ElementCategory.Beam);
            var second = new ProjectElement("E2", ElementCategory.Column);
            project.Elements.Add(first);
            project.Elements.Add(second);
            var service = new QuantityRulePreviewService();

            var elementPreview = service.PreviewElement(project, second);
            if (!string.Equals(elementPreview.ProjectId, project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(elementPreview.ElementId, "E2", StringComparison.Ordinal) ||
                elementPreview.SourceChangeVersion != project.ChangeVersion ||
                elementPreview.HasChanges)
                throw new InvalidOperationException("Canonical element preview must preserve zero-change preview semantics.");

            var projectPreview = service.PreviewProject(project);
            if (projectPreview.Elements.Count != 2 || projectPreview.HasChanges || projectPreview.SourceChangeVersion != project.ChangeVersion)
                throw new InvalidOperationException("Canonical project preview must preserve deterministic zero-change semantics.");
        }

        private static void ThrowsDuplicateExact(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "Project contains duplicate element id: e1", StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Quantity-rule preview returned an unexpected duplicate-Element integrity error.", ex);
            }
            throw new InvalidOperationException("Quantity-rule preview must reject unrelated duplicate Element ids.");
        }

        private static void ThrowsNullExact(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "Project contains a null element.", StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Quantity-rule preview returned an unexpected null-Element integrity error.", ex);
            }
            throw new InvalidOperationException("Quantity-rule preview must reject null Element collection members.");
        }
    }
}
