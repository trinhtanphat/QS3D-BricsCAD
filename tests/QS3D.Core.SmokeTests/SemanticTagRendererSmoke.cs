using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticTagRendererSmoke
    {
        public static void Run()
        {
            StableSemanticReferencesRender();
            OptionalPropertyAndQuantityRender();
            GeneratedOwnershipCannotLeakIntoTag();
            NativeHandleMetadataCannotLeakIntoTag();
            UnsupportedTokenFailsClosed();
            MalformedBraceGrammarFailsClosed();
            MissingReferenceFailsClosed();
            DetachedElementWithSameIdFailsClosed();
            DuplicateElementIdFailsClosed();
            AmbiguousReferencesFailClosed();
        }

        private static void StableSemanticReferencesRender()
        {
            var fixture = BuildFixture();
            var text = SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{Category} | {Family} | {Floor} | {Zone} | {Id}");
            if (text != "Beam | B300x500 | L02 | Zone A | E-001")
                throw new Exception("Unexpected semantic tag output: " + text);
        }

        private static void OptionalPropertyAndQuantityRender()
        {
            var fixture = BuildFixture();
            fixture.Element.SetProperty("Mark", "B-12");
            fixture.Element.SetQuantity("VolumeM3", 1.25d);
            var text = SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{P:Mark} • V={Q:VolumeM3} • missing={P:Optional}");
            if (text != "B-12 • V=1.25 • missing=") throw new Exception("Property/quantity tag output is not deterministic: " + text);
        }

        private static void GeneratedOwnershipCannotLeakIntoTag()
        {
            var fixture = BuildFixture();
            fixture.Element.Properties["GeneratedSolidHandle"] = "ABCD";
            var failed = false;
            try { SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{P:GeneratedSolidHandle}"); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception("Semantic tag must not expose generated CAD owner handles.");
        }

        private static void NativeHandleMetadataCannotLeakIntoTag()
        {
            var fixture = BuildFixture();
            fixture.Element.Properties["CadHandle"] = "ABCD";
            fixture.Element.Properties["SourceHandleRef"] = "EF12";
            MustFail(
                () => SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{P:cAdHaNdLe}"),
                "Semantic tag must not expose arbitrary CAD handle metadata.");
            MustFail(
                () => SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{P:SOURCEHANDLEREF}"),
                "Semantic tag must reject handle-bearing property names case-insensitively.");
        }

        private static void UnsupportedTokenFailsClosed()
        {
            var fixture = BuildFixture();
            var failed = false;
            try { SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{NativeObjectId}"); }
            catch (FormatException) { failed = true; }
            if (!failed) throw new Exception("Unknown semantic tag tokens must fail closed.");
        }

        private static void MalformedBraceGrammarFailsClosed()
        {
            var fixture = BuildFixture();
            MustFormatFail(() => SemanticTagRenderer.ValidateTemplate("abc}"), "A stray closing brace must fail template validation.");
            MustFormatFail(() => SemanticTagRenderer.ValidateTemplate("{Id}}"), "A trailing closing brace must fail template validation.");
            MustFormatFail(() => SemanticTagRenderer.ValidateTemplate("{{Id}"), "Nested/opening brace ambiguity must fail template validation.");
            MustFormatFail(() => SemanticTagRenderer.ValidateTemplate("prefix {Id"), "An unclosed semantic token must fail template validation.");
            MustFormatFail(() => SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{Id}}"), "Rendering must enforce the same brace grammar as validation.");
        }

        private static void MissingReferenceFailsClosed()
        {
            var fixture = BuildFixture();
            fixture.Element.FamilyId = "missing";
            var failed = false;
            try { SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{Family}"); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception("Missing semantic references must not render as valid documentation.");
        }

        private static void DetachedElementWithSameIdFailsClosed()
        {
            var fixture = BuildFixture();
            var detached = new ProjectElement("e-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A");
            var failed = false;
            try { SemanticTagRenderer.Render(fixture.Project, detached, "{Id}"); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception("A detached semantic instance must not be accepted only because its ID matches a project element.");
        }

        private static void DuplicateElementIdFailsClosed()
        {
            var fixture = BuildFixture();
            fixture.Project.Elements.Add(new ProjectElement("e-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A"));
            var failed = false;
            try { SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{Id}"); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception("Ambiguous semantic element IDs must not produce documentation labels.");
        }

        private static void AmbiguousReferencesFailClosed()
        {
            var familyFixture = BuildFixture();
            familyFixture.Project.Families.Add(new ProjectFamily("fam-b", "Duplicate Family", ElementCategory.Beam));
            MustFail(() => SemanticTagRenderer.Render(familyFixture.Project, familyFixture.Element, "{Family}"), "Ambiguous Family IDs must fail closed.");

            var floorFixture = BuildFixture();
            floorFixture.Project.Floors.Add(new FloorDefinition("f-02", "Duplicate Floor", 7.2d));
            MustFail(() => SemanticTagRenderer.Render(floorFixture.Project, floorFixture.Element, "{Floor}"), "Ambiguous Floor IDs must fail closed.");

            var zoneFixture = BuildFixture();
            zoneFixture.Project.Zones.Add(new ZoneDefinition("z-a", "Duplicate Zone"));
            MustFail(() => SemanticTagRenderer.Render(zoneFixture.Project, zoneFixture.Element, "{Zone}"), "Ambiguous Zone IDs must fail closed.");
        }

        private static void MustFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception(message);
        }

        private static void MustFormatFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (FormatException) { failed = true; }
            if (!failed) throw new Exception(message);
        }

        private static Fixture BuildFixture()
        {
            var project = new ProjectState("P-001", "Documentation Smoke");
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Families.Add(new ProjectFamily("FAM-B", "B300x500", ElementCategory.Beam));
            var element = new ProjectElement("E-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A");
            project.Elements.Add(element);
            return new Fixture(project, element);
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ProjectElement element) { Project = project; Element = element; }
            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
