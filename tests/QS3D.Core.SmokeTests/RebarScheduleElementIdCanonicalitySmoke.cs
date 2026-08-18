using System;
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
            DirectBuilderRejectsSurroundingWhitespace();
            DirectBuilderRejectsControlCharactersWithoutEchoingRawIdentity();
            ProjectBuilderRejectsNoncanonicalIdentityBeforeRowEmission();
            ProjectBuilderPreservesCaseInsensitiveDuplicateSemantics();
        }

        private static void CanonicalIdentityRemainsStableAndSuppliesFallbackMark()
        {
            var rows = RebarScheduleBuilder.Build(new[] { Input("E1") });
            Require(rows.Count == 1, "Canonical rebar schedule input did not emit exactly one row.");
            Require(rows[0].ElementId == "E1", "Canonical rebar schedule ElementId changed.");
            Require(rows[0].BarMark == "E1", "Blank BarMark did not fall back to the canonical ElementId.");
        }

        private static void DirectBuilderRejectsSurroundingWhitespace()
        {
            RejectDirect(" E1");
            RejectDirect("E1 ");
        }

        private static void DirectBuilderRejectsControlCharactersWithoutEchoingRawIdentity()
        {
            const string invalidId = "E\u0001X";
            var error = Capture<ArgumentException>(() => RebarScheduleBuilder.Build(new[] { Input(invalidId) }));
            Require(error.Message.IndexOf(invalidId, StringComparison.Ordinal) < 0,
                "Malformed identity diagnostic echoed the hostile raw ElementId.");
        }

        private static void ProjectBuilderRejectsNoncanonicalIdentityBeforeRowEmission()
        {
            const string invalidId = " P1";
            var project = new ProjectState("rebar-schedule-id-project", "Rebar schedule identity");
            var element = new ProjectElement(invalidId, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "1D12";
            element.Properties["RebarCuttingLengthM"] = "1";
            project.Elements.Add(element);

            var error = Capture<InvalidOperationException>(() => ProjectRebarScheduleBuilder.Build(project));
            Require(error.Message.IndexOf(invalidId, StringComparison.Ordinal) < 0,
                "Project schedule diagnostic echoed the hostile raw ElementId.");
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

        private static ProjectElement ProjectElement(string elementId)
        {
            var element = new ProjectElement(elementId, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "1D12";
            element.Properties["RebarCuttingLengthM"] = "1";
            return element;
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
