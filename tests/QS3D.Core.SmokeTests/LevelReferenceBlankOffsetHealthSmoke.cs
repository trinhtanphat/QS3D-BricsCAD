using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class LevelReferenceBlankOffsetHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MissingOffsetRemainsDefaultZero();
            BlankBottomOffsetsFailVisible();
            BlankTopOffsetFailsVisible();
            BlankOffsetWithoutLevelRemainsConfigured();
        }

        private static void MissingOffsetRemainsDefaultZero()
        {
            var fixture = CreateFixture("missing");
            fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey] = "L0";

            var issues = new LevelReferenceHealthService().Inspect(fixture.Project);
            if (HasCode(issues, "BOTTOM_LEVEL_OFFSET_INVALID", fixture.Element.Id))
                throw new InvalidOperationException("A missing BottomLevelOffsetM must retain the canonical default-zero behavior.");
        }

        private static void BlankBottomOffsetsFailVisible()
        {
            foreach (var raw in new string?[] { null, string.Empty, "   " })
            {
                var fixture = CreateFixture("bottom-" + (raw == null ? "null" : raw.Length.ToString()));
                fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey] = "L0";
                fixture.Element.Properties[ProjectFloorService.BottomLevelOffsetKey] = raw!;

                var issues = new LevelReferenceHealthService().Inspect(fixture.Project);
                RequireCode(issues, "BOTTOM_LEVEL_OFFSET_INVALID", fixture.Element.Id);
            }
        }

        private static void BlankTopOffsetFailsVisible()
        {
            var fixture = CreateFixture("top-tab");
            fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey] = "L0";
            fixture.Element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0";
            fixture.Element.Properties[ProjectFloorService.TopLevelIdKey] = "L1";
            fixture.Element.Properties[ProjectFloorService.TopLevelOffsetKey] = "\t";

            var issues = new LevelReferenceHealthService().Inspect(fixture.Project);
            RequireCode(issues, "TOP_LEVEL_OFFSET_INVALID", fixture.Element.Id);
        }

        private static void BlankOffsetWithoutLevelRemainsConfigured()
        {
            var fixture = CreateFixture("without-level");
            fixture.Element.Properties[ProjectFloorService.BottomLevelOffsetKey] = string.Empty;

            var issues = new LevelReferenceHealthService().Inspect(fixture.Project);
            RequireCode(issues, "BOTTOM_LEVEL_OFFSET_WITHOUT_LEVEL", fixture.Element.Id);
        }

        private static Fixture CreateFixture(string suffix)
        {
            var project = new ProjectState("health-level-blank-offset-" + suffix, "Level blank offset " + suffix);
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 3d));
            var element = new ProjectElement("E-" + suffix, ElementCategory.Beam);
            project.Elements.Add(element);
            return new Fixture(project, element);
        }

        private static bool HasCode(IEnumerable<ModelHealthIssue> issues, string code, string elementId) =>
            issues.Any(issue =>
                string.Equals(issue.Code, code, StringComparison.Ordinal) &&
                issue.Severity == HealthSeverity.Error &&
                string.Equals(issue.ElementId, elementId, StringComparison.Ordinal));

        private static void RequireCode(IEnumerable<ModelHealthIssue> issues, string code, string elementId)
        {
            if (!HasCode(issues, code, elementId))
                throw new InvalidOperationException("Expected Level Reference health diagnostic " + code + " for " + elementId + ".");
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
