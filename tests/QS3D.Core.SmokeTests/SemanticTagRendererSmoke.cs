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
            UnsupportedTokenFailsClosed();
            MissingReferenceFailsClosed();
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

        private static void UnsupportedTokenFailsClosed()
        {
            var fixture = BuildFixture();
            var failed = false;
            try { SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{NativeObjectId}"); }
            catch (FormatException) { failed = true; }
            if (!failed) throw new Exception("Unknown semantic tag tokens must fail closed.");
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
