using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverMissingDependencySmoke
    {
        internal static void Run()
        {
            RejectsMissingCanonicalDependency();
            PreservesValidDependencyTraversal();
            RejectsUnknownRoot();
        }

        private static void RejectsMissingCanonicalDependency()
        {
            var project = new ProjectState("P-LOCATE-MISSING", "Locate Missing Dependency");
            var root = new ProjectElement("E-ROOT", ElementCategory.Beam);
            root.SourceHandles.Add("AA");
            root.DependsOn.Add("E-MISSING");
            project.Elements.Add(root);

            var rejected = false;
            try
            {
                SourceHandleResolver.Resolve(project, new[] { "E-ROOT" });
            }
            catch (InvalidOperationException ex)
            {
                rejected = ex.Message.IndexOf("missing semantic element", StringComparison.OrdinalIgnoreCase) >= 0 &&
                           ex.Message.IndexOf("E-MISSING", StringComparison.Ordinal) >= 0;
                if (!rejected) throw;
            }

            if (!rejected)
                throw new InvalidOperationException("Locate must fail closed when an owned element depends on a missing semantic element.");
        }

        private static void PreservesValidDependencyTraversal()
        {
            var project = CreateValidProject();
            var handles = SourceHandleResolver.Resolve(project, new[] { "E-ROOT" });
            if (handles.Count != 2 ||
                !string.Equals(handles[0], "AA", StringComparison.Ordinal) ||
                !string.Equals(handles[1], "BB", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Valid Locate dependency traversal must preserve direct-then-dependent handle ordering.");
            }
        }

        private static void RejectsUnknownRoot()
        {
            var project = CreateValidProject();
            try
            {
                SourceHandleResolver.Resolve(project, new[] { "E-UNKNOWN" });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("root", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    ex.Message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    ex.Message.IndexOf("E-UNKNOWN", StringComparison.Ordinal) >= 0)
                    return;
                throw;
            }

            throw new InvalidOperationException("Locate must fail closed when a requested root semantic element does not exist.");
        }

        private static ProjectState CreateValidProject()
        {
            var project = new ProjectState("P-LOCATE-VALID", "Locate Valid Dependency");
            var root = new ProjectElement("E-ROOT", ElementCategory.Beam);
            root.SourceHandles.Add("AA");
            root.DependsOn.Add("E-HOST");

            var host = new ProjectElement("E-HOST", ElementCategory.Column);
            host.SourceHandles.Add("BB");

            project.Elements.Add(root);
            project.Elements.Add(host);
            return project;
        }
    }
}
