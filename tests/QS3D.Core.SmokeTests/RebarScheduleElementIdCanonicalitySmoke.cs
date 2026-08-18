using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarScheduleElementIdCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalIdentityRemainsStableAndSuppliesFallbackMark();
            DirectBuilderRejectsBlankIdentity();
            DirectBuilderRejectsSurroundingWhitespace();
            DirectBuilderRejectsCommonControlCharactersWithoutEchoingRawIdentity();
            ProjectBuilderRejectsNoncanonicalIdentityBeforeRowEmission();
            ProjectBuilderRejectsTrailingWhitespaceBeforeRowEmission();
            ProjectBuilderRejectsControlIdentityWithoutEchoingRawIdentity();
            ProjectBuilderPreservesCaseInsensitiveDuplicateSemantics();
        }

        private static void CanonicalIdentityRemainsStableAndSuppliesFallbackMark()
        {
            var rows = RebarScheduleBuilder.Build(new[] { Input("E1") });
            Require(rows.Count == 1, "Canonical rebar schedule input did not emit exactly one row.");
            Require(rows[0].ElementId == "E1", "Canonical rebar schedule ElementId changed.");
            Require(rows[0].BarMark == "E1", "Blank BarMark did not fall back to the canonical ElementId.");
        }

        private static void DirectBuilderRejectsBlankIdentity()
        {
            Capture<ArgumentException>(() => RebarScheduleBuilder.Build(new[] { Input(string.Empty) }));
            Capture<ArgumentException>(() => RebarScheduleBuilder.Build(new[] { Input("   ") }));
        }

        private static void DirectBuilderRejectsSurroundingWhitespace()
        {
            RejectDirect(" E1");
            RejectDirect("E1 ");
        }

        private static void DirectBuilderRejectsCommonControlCharactersWithoutEchoingRawIdentity()
        {
            RejectDirect("E\u0001X");
            RejectDirect("E\tX");
            RejectDirect("E\rX");
            RejectDirect("E\nX");
            RejectDirect("E\u007FX");
        }

        private static void ProjectBuilderRejectsNoncanonicalIdentityBeforeRowEmission()
        {
            const string invalidId = " P1";
            var project = ProjectWithMalformedRebarIdentity(invalidId, "rebar-schedule-id-project");
            var error = Capture<InvalidOperationException>(() => ProjectRebarScheduleBuilder.Build(project));
            Require(error.Message.IndexOf(invalidId, StringComparison.Ordinal) < 0,
                "Project schedule diagnostic echoed the hostile raw ElementId.");
        }

        private static void ProjectBuilderRejectsTrailingWhitespaceBeforeRowEmission()
        {
            const string invalidId = "P1 ";
            var project = ProjectWithMalformedRebarIdentity(invalidId, "rebar-schedule-trailing-id-project");
            var error = Capture<InvalidOperationException>(() => ProjectRebarScheduleBuilder.Build(project));
            Require(error.Message.IndexOf(invalidId, StringComparison.Ordinal) < 0,
                "Project schedule trailing-whitespace diagnostic echoed the hostile raw ElementId.");
        }

        private static void ProjectBuilderRejectsControlIdentityWithoutEchoingRawIdentity()
        {
            const string invalidId = "P\u0001X";
            var project = ProjectWithMalformedRebarIdentity(invalidId, "rebar-schedule-control-id-project");
            var error = Capture<InvalidOperationException>(() => ProjectRebarScheduleBuilder.Build(project));
            Require(error.Message.IndexOf(invalidId, StringComparison.Ordinal) < 0,
                "Project schedule control-identity diagnostic echoed the hostile raw ElementId.");
        }

        private static void ProjectBuilderPreservesCaseInsensitiveDuplicateSemantics()
        {
            var project = new ProjectState("rebar-schedule-duplicate-project", "Rebar schedule duplicate identity");
            project.Elements.Add(ProjectElement("E1"));
            project.Elements.Add(ProjectElement("e1"));
            Capture<InvalidOperationException>(() => ProjectRebarScheduleBuilder.Build(project));
        }

        private static RebarScheduleInput Input(string elementId)
        {
            return new RebarScheduleInput
            {
                ElementId = elementId,
                BarMark = " ",
                Notation = "1D12",
                CuttingLengthM = 1d
            };
        }

        private static ProjectState ProjectWithMalformedRebarIdentity(string invalidId, string projectId)
        {
            var project = new ProjectState(projectId, "Rebar schedule identity");
            var element = ProjectElement("P1");
            CorruptElementIdForLegacyStateTest(element, invalidId);
            project.Elements.Add(element);
            return project;
        }

        private static ProjectElement ProjectElement(string elementId)
        {
            var element = new ProjectElement(elementId, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "1D12";
            element.Properties["RebarCuttingLengthM"] = "1";
            return element;
        }

        private static void CorruptElementIdForLegacyStateTest(ProjectElement element, string invalidId)
        {
            var field = typeof(ProjectElement).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("ProjectElement Id backing field was not found for malformed-state smoke setup.");
            field.SetValue(element, invalidId);
            Require(string.Equals(element.Id, invalidId, StringComparison.Ordinal),
                "Malformed-state smoke setup did not preserve the intended noncanonical ElementId.");
        }

        private static void RejectDirect(string invalidId)
        {
            var error = Capture<ArgumentException>(() => RebarScheduleBuilder.Build(new[] { Input(invalidId) }));
            Require(error.Message.IndexOf(invalidId, StringComparison.Ordinal) < 0,
                "Malformed identity diagnostic echoed the hostile raw ElementId.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
