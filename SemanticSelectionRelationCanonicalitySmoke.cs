using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionRelationCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = CreateProject(out var element);
            var canonical = SemanticSelectionInspector.Inspect(project, new[] { element.Id });
            if (!string.Equals(canonical.Family.Value, "FAM-1", StringComparison.Ordinal) ||
                !string.Equals(canonical.Floor.Value, "FLOOR-1", StringComparison.Ordinal) ||
                !string.Equals(canonical.Zone.Value, "ZONE-1", StringComparison.Ordinal))
                throw new Exception("Canonical selection relations must remain inspectable without normalization changes.");

            element.FamilyId = " FAM-1 ";
            Rejects(project, element, "whitespace-padded family id");
            element.FamilyId = "FAM-1";

            element.FloorId = " FLOOR-1 ";
            Rejects(project, element, "whitespace-padded floor id");
            element.FloorId = "FLOOR-1";

            element.ZoneId = " ZONE-1 ";
            Rejects(project, element, "whitespace-padded zone id");
            element.ZoneId = "ZONE-1";

            element.FamilyId = "   ";
            var blank = SemanticSelectionInspector.Inspect(project, new[] { element.Id });
            if (blank.Family.PresentCount != 0 || blank.Family.Value != string.Empty)
                throw new Exception("Whitespace-only selection relation must remain an allowed blank reference.");
        }

        private static ProjectState CreateProject(out ProjectElement element)
        {
            var project = new ProjectState("SELECTION-REL", "Selection Relation Canonicality");
            project.Families.Add(new ProjectFamily("FAM-1", "Family 1", ElementCategory.Wall));
            project.Floors.Add(new FloorDefinition("FLOOR-1", "Floor 1", 0d));
            project.Zones.Add(new ZoneDefinition("ZONE-1", "Zone 1"));
            element = new ProjectElement("E-1", ElementCategory.Wall, "FAM-1", "FLOOR-1", "ZONE-1");
            project.Elements.Add(element);
            return project;
        }

        private static void Rejects(ProjectState project, ProjectElement element, string label)
        {
            try
            {
                SemanticSelectionInspector.Inspect(project, new[] { element.Id });
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Semantic selection inspector accepted " + label + ".");
        }
    }
}
