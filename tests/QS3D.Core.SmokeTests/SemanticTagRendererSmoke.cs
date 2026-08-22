using System;
using System.Reflection;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticTagRendererSmoke
    {
        public static void Run()
        {
            StableSemanticReferencesRender();
            EmptyReferencesRenderEmpty();
            OptionalPropertyAndQuantityRender();
            GeneratedOwnershipCannotLeakIntoTag();
            NativeHandleMetadataCannotLeakIntoTag();
            UnsupportedTokenFailsClosed();
            MalformedBraceGrammarFailsClosed();
            MissingReferenceFailsClosed();
            NonCanonicalReferencesFailClosed();
            NonCanonicalOwnerIdsFailClosed();
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

        private static void EmptyReferencesRenderEmpty()
        {
            var fixture = BuildFixture();
            fixture.Element.FamilyId = string.Empty;
            fixture.Element.FloorId = string.Empty;
            fixture.Element.ZoneId = string.Empty;
            var text = SemanticTagRenderer.Render(fixture.Project, fixture.Element, "{Family}|{Floor}|{Zone}");
            if (text != "||") throw new Exception("Canonical empty semantic references must remain unassigned: " + text);
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

        private static void NonCanonicalReferencesFailClosed()
        {
            var familyFixture = BuildFixture();
            familyFixture.Element.FamilyId = " FAM-B";
            if (familyFixture.Element.FamilyId != "FAM-B") throw new Exception("FamilyId setter must canonicalize padded input.");
            SetRawElementRelation(familyFixture.Element, "_familyId", " FAM-B");
            MustFail(
                () => SemanticTagRenderer.Render(familyFixture.Project, familyFixture.Element, "{Family}"),
                "Whitespace-padded Family references must fail closed instead of being normalized during render.");

            var floorFixture = BuildFixture();
            floorFixture.Element.FloorId = "F-02 ";
            if (floorFixture.Element.FloorId != "F-02") throw new Exception("FloorId setter must canonicalize padded input.");
            SetRawElementRelation(floorFixture.Element, "_floorId", "F-02 ");
            MustFail(
                () => SemanticTagRenderer.Render(floorFixture.Project, floorFixture.Element, "{Floor}"),
                "Whitespace-padded Floor references must fail closed instead of being normalized during render.");

            var zoneFixture = BuildFixture();
            zoneFixture.Element.ZoneId = "\tZ-A";
            if (zoneFixture.Element.ZoneId != "Z-A") throw new Exception("ZoneId setter must canonicalize padded input.");
            SetRawElementRelation(zoneFixture.Element, "_zoneId", "\tZ-A");
            MustFail(
                () => SemanticTagRenderer.Render(zoneFixture.Project, zoneFixture.Element, "{Zone}"),
                "Whitespace-padded Zone references must fail closed instead of being normalized during render.");

            var blankFamilyFixture = BuildFixture();
            blankFamilyFixture.Element.FamilyId = "   ";
            if (blankFamilyFixture.Element.FamilyId != string.Empty) throw new Exception("FamilyId setter must canonicalize whitespace-only input.");
            SetRawElementRelation(blankFamilyFixture.Element, "_familyId", "   ");
            MustFail(
                () => SemanticTagRenderer.Render(blankFamilyFixture.Project, blankFamilyFixture.Element, "{Family}"),
                "Whitespace-only Family references must fail closed instead of being treated as unassigned.");

            var blankFloorFixture = BuildFixture();
            blankFloorFixture.Element.FloorId = "\t";
            if (blankFloorFixture.Element.FloorId != string.Empty) throw new Exception("FloorId setter must canonicalize whitespace-only input.");
            SetRawElementRelation(blankFloorFixture.Element, "_floorId", "\t");
            MustFail(
                () => SemanticTagRenderer.Render(blankFloorFixture.Project, blankFloorFixture.Element, "{Floor}"),
                "Whitespace-only Floor references must fail closed instead of being treated as unassigned.");

            var blankZoneFixture = BuildFixture();
            blankZoneFixture.Element.ZoneId = "  \t  ";
            if (blankZoneFixture.Element.ZoneId != string.Empty) throw new Exception("ZoneId setter must canonicalize whitespace-only input.");
            SetRawElementRelation(blankZoneFixture.Element, "_zoneId", "  \t  ");
            MustFail(
                () => SemanticTagRenderer.Render(blankZoneFixture.Project, blankZoneFixture.Element, "{Zone}"),
                "Whitespace-only Zone references must fail closed instead of being treated as unassigned.");
        }

        private static void NonCanonicalOwnerIdsFailClosed()
        {
            var familyFixture = BuildFixture();
            familyFixture.Project.Families.Clear();
            var paddedFamily = new ProjectFamily("FAM-B", "Padded Family", ElementCategory.Beam);
            SetRawOwnerId(paddedFamily, " FAM-B ");
            familyFixture.Project.Families.Add(paddedFamily);
            MustFail(
                () => SemanticTagRenderer.Render(familyFixture.Project, familyFixture.Element, "{Family}"),
                "Whitespace-padded Family owner IDs must fail closed instead of satisfying a canonical reference.");

            var floorFixture = BuildFixture();
            floorFixture.Project.Floors.Clear();
            var paddedFloor = new FloorDefinition("F-02", "Padded Floor", 3.6d);
            SetRawOwnerId(paddedFloor, " F-02 ");
            floorFixture.Project.Floors.Add(paddedFloor);
            MustFail(
                () => SemanticTagRenderer.Render(floorFixture.Project, floorFixture.Element, "{Floor}"),
                "Whitespace-padded Floor owner IDs must fail closed instead of satisfying a canonical reference.");

            var zoneFixture = BuildFixture();
            zoneFixture.Project.Zones.Clear();
            var paddedZone = new ZoneDefinition("Z-A", "Padded Zone");
            SetRawOwnerId(paddedZone, "\tZ-A");
            zoneFixture.Project.Zones.Add(paddedZone);
            MustFail(
                () => SemanticTagRenderer.Render(zoneFixture.Project, zoneFixture.Element, "{Zone}"),
                "Whitespace-padded Zone owner IDs must fail closed instead of satisfying a canonical reference.");
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

        private static void SetRawElementRelation(ProjectElement element, string fieldName, string rawId)
        {
            var field = typeof(ProjectElement).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectElement backing field is unavailable for malformed semantic reference fixture: " + fieldName + ".");
            field.SetValue(element, rawId);
        }

        private static void SetRawOwnerId(object owner, string rawId)
        {
            var field = owner.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("Owner ID backing field is unavailable for the malformed-state fixture.");
            field.SetValue(owner, rawId);
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
