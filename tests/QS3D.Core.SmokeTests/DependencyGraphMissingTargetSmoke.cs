using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphMissingTargetSmoke
    {
        public static void Run()
        {
            MissingTargetFailsForDirtyElement();
            MissingTargetFailsForCleanElement();
            PresentCleanDependencyRemainsSatisfied();
        }

        private static void MissingTargetFailsForDirtyElement()
        {
            var child = Element("CHILD-DIRTY", "MISSING-DIRTY");
            ExpectMissingTarget(
                () => new DependencyGraph().TopologicalDirtyOrder(new[] { child }),
                "Dirty dependency ordering input must fail closed when a dependency target is absent.");
        }

        private static void MissingTargetFailsForCleanElement()
        {
            var child = Element("CHILD-CLEAN", "MISSING-CLEAN");
            child.MarkClean(ElementDirtyFlags.All);
            ExpectMissingTarget(
                () => new DependencyGraph().TopologicalDirtyOrder(new[] { child }),
                "Clean dependency ordering input must still enforce full semantic referential integrity.");
        }

        private static void PresentCleanDependencyRemainsSatisfied()
        {
            var host = Element("HOST");
            host.MarkClean(ElementDirtyFlags.All);
            var child = Element("CHILD", "host");

            var ordered = new DependencyGraph().TopologicalDirtyOrder(new[] { host, child });
            if (ordered.Count != 1 || !ReferenceEquals(child, ordered[0]))
                throw new Exception("A dependency that is present but clean must remain satisfied without entering the dirty ordering result.");
        }

        private static ProjectElement Element(string id, params string[] dependsOn)
        {
            var element = new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            foreach (var dependency in dependsOn) element.DependsOn.Add(dependency);
            return element;
        }

        private static void ExpectMissingTarget(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("depends on missing semantic element", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception(message + " Actual diagnostic: " + ex.Message);
            }

            throw new Exception(message);
        }
    }
}
