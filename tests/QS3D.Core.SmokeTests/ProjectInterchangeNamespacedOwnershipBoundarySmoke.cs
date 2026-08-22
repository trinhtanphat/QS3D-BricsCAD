using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeNamespacedOwnershipBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExporterOmitsNamespacedOpeningOwnership();
            ValidatorAndTypedReaderRejectElementSmuggling();
            ValidatorAndTypedReaderRejectFamilySmuggling();
        }

        private static void ExporterOmitsNamespacedOpeningOwnership()
        {
            var project = BuildProject();
            project.Families[0].Properties["QS3D.PhysicalOpeningCut.FamilyOwner"] = "AA11";
            project.Elements[0].Properties["QS3D.PhysicalOpeningCut.State"] = "owned";
            project.Elements[0].Properties["QS3D.PhysicalOpeningCut.Owner"] = "BB22";

            var json = ProjectInterchangeJsonExporter.Build(project);
            False(json.Contains("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase));
            True(json.Contains("FamilySafe", StringComparison.Ordinal));
            True(json.Contains("ElementSafe", StringComparison.Ordinal));
        }

        private static void ValidatorAndTypedReaderRejectElementSmuggling()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildProject());
            var smuggled = json.Replace(
                "\"ElementSafe\":\"yes\"",
                "\"ElementSafe\":\"yes\",\"QS3D.PhysicalOpeningCut.State\":\"owned\"",
                StringComparison.Ordinal);
            True(!string.Equals(json, smuggled, StringComparison.Ordinal));

            var validation = ProjectInterchangeJsonValidator.Validate(smuggled);
            False(validation.IsValid);
            True(validation.Issues.Any(x => string.Equals(x.Code, "GENERATED_RUNTIME_PROPERTY", StringComparison.Ordinal)));
            Throws<InvalidDataException>(() => ProjectInterchangeValidatedSnapshotReader.Read(smuggled));
        }

        private static void ValidatorAndTypedReaderRejectFamilySmuggling()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildProject());
            var smuggled = json.Replace(
                "\"FamilySafe\":\"yes\"",
                "\"FamilySafe\":\"yes\",\"QS3D.PhysicalOpeningCut.Owner\":\"CC33\"",
                StringComparison.Ordinal);
            True(!string.Equals(json, smuggled, StringComparison.Ordinal));

            var validation = ProjectInterchangeJsonValidator.Validate(smuggled);
            False(validation.IsValid);
            True(validation.Issues.Any(x => string.Equals(x.Code, "GENERATED_RUNTIME_PROPERTY", StringComparison.Ordinal)));
            Throws<InvalidDataException>(() => ProjectInterchangeValidatedSnapshotReader.Read(smuggled));
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-OWNERSHIP-BOUNDARY", "Ownership boundary")
            {
                DrawingFingerprint = "drawing-fp"
            };
            project.Zones.Add(new ZoneDefinition("Z-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("F-1", "Floor 1", 0d));
            var family = new ProjectFamily("FAM-1", "Beam 1", ElementCategory.Beam);
            family.Properties["FamilySafe"] = "yes";
            project.Families.Add(family);
            var element = new ProjectElement("E-1", ElementCategory.Beam, family.Id, "F-1", "Z-1");
            element.Properties["ElementSafe"] = "yes";
            project.Elements.Add(element);
            return project;
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("ProjectInterchangeNamespacedOwnershipBoundarySmoke expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new InvalidOperationException("ProjectInterchangeNamespacedOwnershipBoundarySmoke expected false.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectInterchangeNamespacedOwnershipBoundarySmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
