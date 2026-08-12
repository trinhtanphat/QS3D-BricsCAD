using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateNameInvariantSmoke
    {
        internal static void Run()
        {
            ConstructorKeepsLegacyBlankFallback();
            SetterNormalizesAcceptedNames();
            SetterRejectsBlankWithoutCorruptingState();
        }

        private static void ConstructorKeepsLegacyBlankFallback()
        {
            var project = new ProjectState("project-1", "   ");
            Expect(project.Name == "QS3D Project", "Blank constructor names must retain the existing QS3D Project fallback.");
        }

        private static void SetterNormalizesAcceptedNames()
        {
            var project = new ProjectState("project-2", "Initial");
            project.Name = "  Updated Project  ";
            Expect(project.Name == "Updated Project", "Project name assignments must be trimmed.");
        }

        private static void SetterRejectsBlankWithoutCorruptingState()
        {
            var project = new ProjectState("project-3", "Stable Project");
            ExpectArgumentException(() => project.Name = "   ", "Whitespace-only project names must be rejected.");
            Expect(project.Name == "Stable Project", "Rejected project names must not mutate the existing name.");

            ExpectArgumentException(() => project.Name = null!, "Null project names must be rejected.");
            Expect(project.Name == "Stable Project", "Rejected null project names must not mutate the existing name.");
        }

        private static void ExpectArgumentException(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
