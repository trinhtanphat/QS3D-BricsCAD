using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotDuplicateIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsDuplicateZones();
            RejectsDuplicateFloors();
            RejectsDuplicateFamilies();
            RejectsDuplicateElements();
            RejectsDuplicateQuantityRules();
            PreservesValidDetachedClone();
        }

        private static void RejectsDuplicateZones()
        {
            var project = NewProject("ZONE");
            project.Zones.Add(new ZoneDefinition("Z1", "Zone One"));
            project.Zones.Add(new ZoneDefinition("z1", "Zone Duplicate"));
            ExpectInvalid(project, "zone");
        }

        private static void RejectsDuplicateFloors()
        {
            var project = NewProject("FLOOR");
            project.Floors.Add(new FloorDefinition("F1", "Level One", 0d));
            project.Floors.Add(new FloorDefinition("f1", "Level Duplicate", 3d));
            ExpectInvalid(project, "floor");
        }

        private static void RejectsDuplicateFamilies()
        {
            var project = NewProject("FAMILY");
            project.Families.Add(new ProjectFamily("FM1", "Family One", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("fm1", "Family Duplicate", ElementCategory.Column));
            ExpectInvalid(project, "family");
        }

        private static void RejectsDuplicateElements()
        {
            var project = NewProject("ELEMENT");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("e1", ElementCategory.Column));
            ExpectInvalid(project, "element");
        }

        private static void RejectsDuplicateQuantityRules()
        {
            var project = NewProject("RULE");
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "A", "LengthM", "v1"));
            project.QuantityRules.Add(new QuantityRule("r1", ElementCategory.Column, "B", "HeightM", "v2"));
            ExpectInvalid(project, "quantity rule");
        }

        private static void PreservesValidDetachedClone()
        {
            var project = NewProject("VALID");
            var zone = new ZoneDefinition("Z1", "Zone One");
            var floor = new FloorDefinition("F1", "Level One", 1d);
            var family = new ProjectFamily("FM1", "Family One", ElementCategory.Beam);
            var element = new ProjectElement("E1", ElementCategory.Beam);
            var rule = new QuantityRule("R1", ElementCategory.Beam, "Length", "LengthM", "v1");
            project.Zones.Add(zone);
            project.Floors.Add(floor);
            project.Families.Add(family);
            project.Elements.Add(element);
            project.QuantityRules.Add(rule);

            var copy = ProjectStateSnapshot.CreateDetachedCopy(project);
            if (copy.Zones.Count != 1 || copy.Floors.Count != 1 || copy.Families.Count != 1 || copy.Elements.Count != 1 || copy.QuantityRules.Count != 1)
                throw new InvalidOperationException("Valid snapshot detached clone lost semantic entries.");
            if (ReferenceEquals(copy.Zones[0], zone) || ReferenceEquals(copy.Floors[0], floor) ||
                ReferenceEquals(copy.Families[0], family) || ReferenceEquals(copy.Elements[0], element) ||
                ReferenceEquals(copy.QuantityRules[0], rule))
                throw new InvalidOperationException("Valid snapshot detached clone must not alias canonical semantic entries.");
        }

        private static ProjectState NewProject(string suffix) =>
            new ProjectState("SNAPSHOT-DUP-" + suffix, "Snapshot duplicate " + suffix);

        private static void ExpectInvalid(ProjectState project, string label)
        {
            try
            {
                ProjectStateSnapshot.CreateDetachedCopy(project);
                throw new InvalidOperationException("Snapshot detached copy must reject duplicate " + label + " ids.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("duplicate " + label + " id", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Snapshot duplicate " + label + " rejection used an unexpected error.", ex);
            }
        }
    }
}
