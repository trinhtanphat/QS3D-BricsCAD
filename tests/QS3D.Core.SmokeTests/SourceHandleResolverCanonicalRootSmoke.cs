using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverCanonicalRootSmoke
    {
        public static void Run()
        {
            var project = new ProjectState("P1", "Locate canonical root identity");
            var element = new ProjectElement(
                "element-1",
                ElementCategory.ArchitecturalWall,
                string.Empty,
                string.Empty,
                string.Empty);
            element.SourceHandles.Add("AB12");
            project.Elements.Add(element);

            var canonical = SourceHandleResolver.Resolve(project, new[] { "element-1" });
            if (canonical.Count != 1 || !string.Equals(canonical[0], "AB12", StringComparison.Ordinal))
                throw new Exception("Canonical Locate root id must continue resolving its source handle.");

            var blank = SourceHandleResolver.Resolve(project, new[] { "   " });
            if (blank.Count != 0)
                throw new Exception("Blank Locate root ids must retain existing skip semantics.");

            ExpectInvalidOperation(
                () => SourceHandleResolver.Resolve(project, new[] { " element-1 " }),
                "non-canonical semantic element id");

            var oversized = Enumerable.Repeat("element-1", 10001).ToArray();
            ExpectInvalidOperation(
                () => SourceHandleResolver.Resolve(project, oversized),
                "cannot exceed 10000 input entries");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment)
        {
            try
            {
                action();
                throw new Exception("Expected Locate validation to fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Locate validation failed with an unexpected diagnostic: " + ex.Message);
            }
        }
    }
}
