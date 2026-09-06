using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeExportMaterializationBoundSmoke
    {
        public static void Run()
        {
            OversizedAggregateFailsDuringBoundedSerialization();
            OrdinarySnapshotRemainsDeterministicAndValid();
        }

        private static void OversizedAggregateFailsDuringBoundedSerialization()
        {
            var project = new ProjectState("P-BOUND", "Semantic Snapshot Bound");
            var family = new ProjectFamily("F-BOUND", "Bounded Family", ElementCategory.Beam);
            var value = new string('x', 32768);
            for (var i = 0; i < 600; i++)
                InjectLegacyFamilyProperty(family, "P" + i.ToString("D4", System.Globalization.CultureInfo.InvariantCulture), value);
            project.Families.Add(family);

            try
            {
                ProjectInterchangeJsonExporter.Build(project);
            }
            catch (InvalidDataException ex)
            {
                if (!ex.Message.Contains("semantic snapshot limit"))
                    throw new Exception("Oversized semantic snapshot must fail from the bounded exporter before final validation. Actual: " + ex.Message);
                return;
            }

            throw new Exception("Individually-valid aggregate larger than MaxFileBytes must fail closed during serialization.");
        }

        private static void OrdinarySnapshotRemainsDeterministicAndValid()
        {
            var project = new ProjectState("P-OK", "Semantic Snapshot Control");
            project.Zones.Add(new ZoneDefinition("Z-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("FL-1", "Level 1", 0d));
            var family = new ProjectFamily("F-1", "B300x500", ElementCategory.Beam);
            family.Properties["Material"] = "Concrete";
            project.Families.Add(family);
            var element = new ProjectElement("E-1", ElementCategory.Beam, "F-1", "FL-1", "Z-1");
            element.SetQuantity("VolumeM3", 1.25d);
            project.Elements.Add(element);

            var first = ProjectInterchangeJsonExporter.Build(project);
            var second = ProjectInterchangeJsonExporter.Build(project);
            if (!string.Equals(first, second, StringComparison.Ordinal))
                throw new Exception("Bounded serialization changed deterministic canonical output.");

            var validation = ProjectInterchangeJsonValidator.Validate(first);
            if (!validation.IsValid)
                throw new Exception("Ordinary bounded semantic snapshot must remain canonically valid.");
        }

        private static void InjectLegacyFamilyProperty(ProjectFamily family, string key, string value)
        {
            var innerField = family.Properties.GetType().GetField("_inner", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Interchange-bound legacy fixture could not locate the Family property backing dictionary.");
            var inner = innerField.GetValue(family.Properties) as Dictionary<string, string>
                ?? throw new InvalidOperationException("Interchange-bound legacy fixture Family property backing dictionary had an unexpected type.");
            inner[key] = value;
        }
    }
}
