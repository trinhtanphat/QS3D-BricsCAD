using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleRequestedRootExistenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MissingRequestedRootFailsClosed();
            MixedValidAndMissingRootsFailClosed();
            ValidRequestedRootStillResolves();
        }

        private static void MissingRequestedRootFailsClosed()
        {
            var project = new ProjectState("P-LOCATE-MISSING-ROOT", "Locate missing root");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = project.UpdatedUtc;

            ThrowsMissingRoot(() => SourceHandleResolver.Resolve(project, new[] { "MISSING" }), "MISSING");

            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdated)
                throw new InvalidOperationException("Rejected missing Locate root mutated project persistence state.");
        }

        private static void MixedValidAndMissingRootsFailClosed()
        {
            var project = new ProjectState("P-LOCATE-MIXED-ROOTS", "Locate mixed roots");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            element.SourceHandles.Add("ABC");
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = project.UpdatedUtc;
            var beforeElementUpdated = element.UpdatedUtc;
            var beforeDirty = element.Dirty;

            ThrowsMissingRoot(() => SourceHandleResolver.Resolve(project, new[] { element.Id, "MISSING" }), "MISSING");

            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdated)
                throw new InvalidOperationException("Mixed valid/missing Locate roots mutated project persistence state.");
            if (element.Dirty != beforeDirty || element.UpdatedUtc != beforeElementUpdated)
                throw new InvalidOperationException("Mixed valid/missing Locate roots mutated the valid semantic element.");
        }

        private static void ValidRequestedRootStillResolves()
        {
            var project = new ProjectState("P-LOCATE-VALID-ROOT", "Locate valid root");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            element.SourceHandles.Add("ABC");
            project.Elements.Add(element);

            var handles = SourceHandleResolver.Resolve(project, new[] { "e1" });
            if (handles.Count != 1 || !string.Equals(handles[0], "ABC", StringComparison.Ordinal))
                throw new InvalidOperationException("Valid canonical Locate root no longer resolves its direct source handle.");
        }

        private static void ThrowsMissingRoot(Action action, string expectedId)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                var message = ex.Message ?? string.Empty;
                if (message.IndexOf("root", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    message.IndexOf(expectedId, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("Locate rejected a missing requested root with the wrong contract message.", ex);
            }

            throw new InvalidOperationException("Expected Locate to reject a missing requested semantic root.");
        }
    }
}
