using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleCaptureTargetCanonicalIdSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalIdResolvesExistingTarget();
            PaddedCanonicalIdFailsClosed();
            BlankCanonicalIdRemainsRejected();
        }

        private static void CanonicalIdResolvesExistingTarget()
        {
            var project = NewProject();
            var element = new ProjectElement("element-1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);

            var resolved = SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "ABCD", ElementCategory.Beam, "element-1");
            if (!ReferenceEquals(element, resolved))
                throw new InvalidOperationException("Canonical capture target ID must resolve the existing semantic element.");
        }

        private static void PaddedCanonicalIdFailsClosed()
        {
            var project = NewProject();
            project.Elements.Add(new ProjectElement("element-1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));

            Throws<ArgumentException>(() =>
                SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "ABCD", ElementCategory.Beam, " element-1 "));
        }

        private static void BlankCanonicalIdRemainsRejected()
        {
            var project = NewProject();
            Throws<ArgumentException>(() =>
                SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "ABCD", ElementCategory.Beam, "   "));
        }

        private static ProjectState NewProject()
        {
            return new ProjectState("capture-target-canonical-id", "Capture target canonical ID regression");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("SemanticHandleCaptureTargetCanonicalIdSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
