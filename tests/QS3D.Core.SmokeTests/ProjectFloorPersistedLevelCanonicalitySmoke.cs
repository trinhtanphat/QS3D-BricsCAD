using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorPersistedLevelCanonicalitySmoke
    {
        internal static void Run()
        {
            PaddedBottomReferenceFailsClosed();
            PaddedTopReferenceFailsClosed();
            WhitespaceOnlyReferenceFailsClosed();
            PaddedBottomAssignmentFailsBeforeMutation();
            CanonicalReferencesRemainSupported();
        }

        private static void PaddedBottomReferenceFailsClosed()
        {
            var fixture = NewFixture();
            fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey] = " " + fixture.Lower.Id + " ";
            var beforeVersion = fixture.Project.ChangeVersion;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.ReferenceCount(fixture.Project, fixture.Lower.Id));

            Contains("must not contain leading/trailing whitespace", error.Message,
                "Padded persisted BottomLevelId must fail closed instead of aliasing the canonical floor id.");
            Equal(beforeVersion, fixture.Project.ChangeVersion,
                "Padded BottomLevelId rejection must not mutate project version.");
        }

        private static void PaddedTopReferenceFailsClosed()
        {
            var fixture = NewFixture();
            fixture.Element.Properties[ProjectFloorService.TopLevelIdKey] = " " + fixture.Upper.Id + " ";
            var beforeVersion = fixture.Project.ChangeVersion;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.ReferenceCount(fixture.Project, fixture.Upper.Id));

            Contains("must not contain leading/trailing whitespace", error.Message,
                "Padded persisted TopLevelId must fail closed instead of aliasing the canonical floor id.");
            Equal(beforeVersion, fixture.Project.ChangeVersion,
                "Padded TopLevelId rejection must not mutate project version.");
        }

        private static void WhitespaceOnlyReferenceFailsClosed()
        {
            var fixture = NewFixture();
            fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey] = "   ";

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.ReferencesVerticalLevel(fixture.Element, fixture.Lower.Id));

            Contains("must be empty or a canonical level id", error.Message,
                "Whitespace-only persisted level references must not silently alias absence.");
        }

        private static void PaddedBottomAssignmentFailsBeforeMutation()
        {
            var fixture = NewFixture();
            fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey] = " " + fixture.Lower.Id + " ";
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeTop = fixture.Element.Properties.ContainsKey(ProjectFloorService.TopLevelIdKey)
                ? fixture.Element.Properties[ProjectFloorService.TopLevelIdKey]
                : null;

            Capture<InvalidOperationException>(() =>
                ProjectFloorService.AssignTopLevel(fixture.Project, fixture.Upper.Id, new[] { fixture.Element }));

            Equal(beforeVersion, fixture.Project.ChangeVersion,
                "Padded BottomLevelId must fail before Floor mutation touches the project.");
            var afterTop = fixture.Element.Properties.ContainsKey(ProjectFloorService.TopLevelIdKey)
                ? fixture.Element.Properties[ProjectFloorService.TopLevelIdKey]
                : null;
            Equal(beforeTop, afterTop,
                "Padded BottomLevelId rejection must preserve TopLevelId.");
        }

        private static void CanonicalReferencesRemainSupported()
        {
            var fixture = NewFixture();
            fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey] = fixture.Lower.Id;
            fixture.Element.Properties[ProjectFloorService.TopLevelIdKey] = fixture.Upper.Id;

            Equal(1, ProjectFloorService.ReferenceCount(fixture.Project, fixture.Lower.Id),
                "Canonical BottomLevelId must remain a recognized reference.");
            Equal(1, ProjectFloorService.ReferenceCount(fixture.Project, fixture.Upper.Id),
                "Canonical TopLevelId must remain a recognized reference.");
            True(ProjectFloorService.ReferencesVerticalLevel(fixture.Element, fixture.Lower.Id),
                "Canonical BottomLevelId must remain visible to vertical-reference checks.");
            True(ProjectFloorService.ReferencesVerticalLevel(fixture.Element, fixture.Upper.Id),
                "Canonical TopLevelId must remain visible to vertical-reference checks.");
        }

        private static Fixture NewFixture()
        {
            var project = new ProjectState("floor-persisted-level-canonicality", "Floor persisted level canonicality");
            var lower = new FloorDefinition("F1", "Floor 1", 0d);
            var upper = new FloorDefinition("F2", "Floor 2", 3d);
            project.Floors.Add(lower);
            project.Floors.Add(upper);
            project.ActiveFloorId = lower.Id;

            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return new Fixture(project, lower, upper, element);
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception(message + " Actual: " + (actual ?? "<null>"));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private sealed class Fixture
        {
            internal Fixture(ProjectState project, FloorDefinition lower, FloorDefinition upper, ProjectElement element)
            {
                Project = project;
                Lower = lower;
                Upper = upper;
                Element = element;
            }

            internal ProjectState Project { get; }
            internal FloorDefinition Lower { get; }
            internal FloorDefinition Upper { get; }
            internal ProjectElement Element { get; }
        }
    }
}
