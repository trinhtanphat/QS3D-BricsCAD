using System;
using System.Globalization;
using QS3D.Core.Domain;
using QS3D.Core.Selection;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionBulkEditSmoke
    {
        public static void Run()
        {
            SourceDerivedAndOwnershipFieldsAreReadOnly();
            PropertyDirtyFlagsStayPrecise();
            SameInheritedValueMaterializesInstanceOverride();
            NumericMultiplyPreflightsBeforeMutation();
            NumericMultiplyUnderflowIsAtomic();
            NumericMultiplyPreservesLegitimateZeroAndSubnormal();
            FamilyAssignmentIsAllOrNothingAcrossCategories();
            FamilyIdentityMustBeCanonicalAndAtomic();
            DuplicateSelectionFailsBeforeMutation();
        }

        private static void SourceDerivedAndOwnershipFieldsAreReadOnly()
        {
            var project = BuildProject();
            var service = new SemanticSelectionBulkEditService();
            var version = project.ChangeVersion;
            MustFail(() => service.SetProperty(project, new[] { "B-1", "B-2" }, "LengthM", "8"));
            MustFail(() => service.SetProperty(project, new[] { "B-1" }, "GeneratedSolidHandle", "AB12"));
            MustFail(() => service.SetProperty(project, new[] { "B-1" }, "FamilyId", "F-B2"));
            Equal(version, project.ChangeVersion);
            Equal(false, project.Elements[0].Properties.ContainsKey("LengthM"));
            Equal(false, project.Elements[0].Properties.ContainsKey("GeneratedSolidHandle"));
        }

        private static void PropertyDirtyFlagsStayPrecise()
        {
            var project = BuildProject();
            foreach (var element in project.Elements) element.MarkClean(ElementDirtyFlags.All);
            var service = new SemanticSelectionBulkEditService();

            var mark = service.SetProperty(project, new[] { "B-1", "B-2" }, "Mark", "A");
            Equal(2, mark.ChangedCount);
            foreach (var element in project.Elements)
            {
                Equal(true, (element.Dirty & ElementDirtyFlags.Properties) != 0);
                Equal(true, (element.Dirty & ElementDirtyFlags.Quantity) != 0);
                Equal(false, (element.Dirty & ElementDirtyFlags.Geometry) != 0);
                element.MarkClean(ElementDirtyFlags.All);
            }

            var width = service.SetProperty(project, new[] { "B-1", "B-2" }, "WidthM", "0.35");
            Equal(2, width.ChangedCount);
            foreach (var element in project.Elements)
                Equal(true, (element.Dirty & ElementDirtyFlags.Geometry) != 0);
        }

        private static void SameInheritedValueMaterializesInstanceOverride()
        {
            var project = BuildProject();
            foreach (var element in project.Elements) element.MarkClean(ElementDirtyFlags.All);
            var version = project.ChangeVersion;
            var service = new SemanticSelectionBulkEditService();

            var result = service.SetProperty(project, new[] { "B-1", "B-2" }, "FireRating", "R60");
            Equal(2, result.ChangedCount);
            Equal(true, project.ChangeVersion > version);
            Equal("R60", project.Elements[0].Properties["FireRating"]);
            Equal("R60", project.Elements[1].Properties["FireRating"]);

            foreach (var element in project.Elements) element.MarkClean(ElementDirtyFlags.All);
            version = project.ChangeVersion;
            var noOp = service.SetProperty(project, new[] { "B-1", "B-2" }, "FireRating", "R60");
            Equal(0, noOp.ChangedCount);
            Equal(version, project.ChangeVersion);
            Equal(ElementDirtyFlags.None, project.Elements[0].Dirty);
            Equal(ElementDirtyFlags.None, project.Elements[1].Dirty);
        }

        private static void NumericMultiplyPreflightsBeforeMutation()
        {
            var project = BuildProject();
            project.Elements[0].SetProperty("WidthM", "0.2");
            project.Elements[1].SetProperty("WidthM", "bad");
            foreach (var element in project.Elements) element.MarkClean(ElementDirtyFlags.All);
            var version = project.ChangeVersion;
            var service = new SemanticSelectionBulkEditService();

            MustFail(() => service.MultiplyNumericProperty(project, new[] { "B-1", "B-2" }, "WidthM", 2d));
            Equal("0.2", project.Elements[0].Properties["WidthM"]);
            Equal("bad", project.Elements[1].Properties["WidthM"]);
            Equal(version, project.ChangeVersion);
        }

        private static void NumericMultiplyUnderflowIsAtomic()
        {
            var service = new SemanticSelectionBulkEditService();

            var parseProject = BuildProject();
            parseProject.Elements[0].SetProperty("WidthM", "0.2");
            parseProject.Elements[1].SetProperty("WidthM", "1e-4000");
            foreach (var element in parseProject.Elements) element.MarkClean(ElementDirtyFlags.All);
            var parseVersion = parseProject.ChangeVersion;

            MustFail(() => service.MultiplyNumericProperty(parseProject, new[] { "B-1", "B-2" }, "WidthM", 2d));
            Equal("0.2", parseProject.Elements[0].Properties["WidthM"]);
            Equal("1e-4000", parseProject.Elements[1].Properties["WidthM"]);
            Equal(parseVersion, parseProject.ChangeVersion);
            Equal(ElementDirtyFlags.None, parseProject.Elements[0].Dirty);
            Equal(ElementDirtyFlags.None, parseProject.Elements[1].Dirty);

            var productProject = BuildProject();
            var epsilonText = double.Epsilon.ToString("R", CultureInfo.InvariantCulture);
            productProject.Elements[0].SetProperty("WidthM", "0.2");
            productProject.Elements[1].SetProperty("WidthM", epsilonText);
            foreach (var element in productProject.Elements) element.MarkClean(ElementDirtyFlags.All);
            var productVersion = productProject.ChangeVersion;

            MustFail(() => service.MultiplyNumericProperty(productProject, new[] { "B-1", "B-2" }, "WidthM", 0.5d));
            Equal("0.2", productProject.Elements[0].Properties["WidthM"]);
            Equal(epsilonText, productProject.Elements[1].Properties["WidthM"]);
            Equal(productVersion, productProject.ChangeVersion);
            Equal(ElementDirtyFlags.None, productProject.Elements[0].Dirty);
            Equal(ElementDirtyFlags.None, productProject.Elements[1].Dirty);
        }

        private static void NumericMultiplyPreservesLegitimateZeroAndSubnormal()
        {
            var service = new SemanticSelectionBulkEditService();

            var zeroProject = BuildProject();
            zeroProject.Elements[0].SetProperty("WidthM", "0e-4000");
            zeroProject.Elements[0].MarkClean(ElementDirtyFlags.All);
            var zeroVersion = zeroProject.ChangeVersion;
            var zero = service.MultiplyNumericProperty(zeroProject, new[] { "B-1" }, "WidthM", 2d);
            Equal(0, zero.ChangedCount);
            Equal("0e-4000", zeroProject.Elements[0].Properties["WidthM"]);
            Equal(zeroVersion, zeroProject.ChangeVersion);
            Equal(ElementDirtyFlags.None, zeroProject.Elements[0].Dirty);

            var subnormalProject = BuildProject();
            var epsilonText = double.Epsilon.ToString("R", CultureInfo.InvariantCulture);
            var expected = (double.Epsilon * 2d).ToString("R", CultureInfo.InvariantCulture);
            subnormalProject.Elements[0].SetProperty("WidthM", epsilonText);
            subnormalProject.Elements[0].MarkClean(ElementDirtyFlags.All);
            var subnormal = service.MultiplyNumericProperty(subnormalProject, new[] { "B-1" }, "WidthM", 2d);
            Equal(1, subnormal.ChangedCount);
            Equal(expected, subnormalProject.Elements[0].Properties["WidthM"]);

            var zeroFactorProject = BuildProject();
            zeroFactorProject.Elements[0].SetProperty("WidthM", "2");
            zeroFactorProject.Elements[0].MarkClean(ElementDirtyFlags.All);
            var zeroFactor = service.MultiplyNumericProperty(zeroFactorProject, new[] { "B-1" }, "WidthM", 0d);
            Equal(1, zeroFactor.ChangedCount);
            Equal("0", zeroFactorProject.Elements[0].Properties["WidthM"]);
        }

        private static void FamilyAssignmentIsAllOrNothingAcrossCategories()
        {
            var project = BuildProject();
            var columnFamily = new ProjectFamily("F-C", "Column 400", ElementCategory.Column);
            project.Families.Add(columnFamily);
            var column = new ProjectElement("C-1", ElementCategory.Column, "F-C", "F-01", "Z-01");
            project.Elements.Add(column);
            var version = project.ChangeVersion;
            var service = new SemanticSelectionBulkEditService();

            MustFail(() => service.AssignFamily(project, new[] { "B-1", "C-1" }, "F-B2"));
            Equal("F-B", project.FindElement("B-1")!.FamilyId);
            Equal("F-C", project.FindElement("C-1")!.FamilyId);
            Equal(version, project.ChangeVersion);

            var changed = service.AssignFamily(project, new[] { "B-1", "B-2" }, "F-B2");
            Equal(2, changed.ChangedCount);
            Equal("F-B2", project.FindElement("B-1")!.FamilyId);
            Equal("F-B2", project.FindElement("B-2")!.FamilyId);
        }

        private static void FamilyIdentityMustBeCanonicalAndAtomic()
        {
            var semantic = new SemanticSelectionBulkEditService();

            var paddedTarget = BuildProject();
            var paddedTargetVersion = paddedTarget.ChangeVersion;
            MustFail(() => semantic.AssignFamily(paddedTarget, new[] { "B-1" }, " F-B2 "));
            Equal("F-B", paddedTarget.FindElement("B-1")!.FamilyId);
            Equal(paddedTargetVersion, paddedTarget.ChangeVersion);

            var paddedCurrent = BuildProject();
            var paddedCurrentElement = paddedCurrent.FindElement("B-1")!;
            paddedCurrentElement.FamilyId = " F-B ";
            var paddedCurrentVersion = paddedCurrent.ChangeVersion;
            MustFail(() => semantic.AssignFamily(paddedCurrent, new[] { "B-1" }, "F-B2"));
            Equal(" F-B ", paddedCurrentElement.FamilyId);
            Equal(paddedCurrentVersion, paddedCurrent.ChangeVersion);

            var inherited = BuildProject();
            var inheritedElement = inherited.FindElement("B-1")!;
            inheritedElement.FamilyId = " F-B ";
            var inheritedVersion = inherited.ChangeVersion;
            MustFail(() => semantic.MultiplyNumericProperty(inherited, new[] { "B-1" }, "WidthM", 2d));
            Equal(false, inheritedElement.Properties.ContainsKey("WidthM"));
            Equal(" F-B ", inheritedElement.FamilyId);
            Equal(inheritedVersion, inherited.ChangeVersion);

            var directTarget = BuildProject();
            var directTargetVersion = directTarget.ChangeVersion;
            MustFail(() => new BulkEditService().AssignFamily(directTarget, new[] { "B-1" }, " F-B2 "));
            Equal("F-B", directTarget.FindElement("B-1")!.FamilyId);
            Equal(directTargetVersion, directTarget.ChangeVersion);

            var directCurrent = BuildProject();
            var directCurrentElement = directCurrent.FindElement("B-1")!;
            directCurrentElement.FamilyId = " F-B ";
            var directCurrentVersion = directCurrent.ChangeVersion;
            MustFail(() => new BulkEditService().AssignFamily(directCurrent, new[] { "B-1" }, "F-B2"));
            Equal(" F-B ", directCurrentElement.FamilyId);
            Equal(directCurrentVersion, directCurrent.ChangeVersion);
        }

        private static void DuplicateSelectionFailsBeforeMutation()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            var service = new SemanticSelectionBulkEditService();
            MustFail(() => service.SetProperty(project, new[] { "B-1", "b-1" }, "Mark", "X"));
            Equal(version, project.ChangeVersion);
            Equal(false, project.FindElement("B-1")!.Properties.ContainsKey("Mark"));
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-BULK", "Bulk selection smoke");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone 01"));

            var family = new ProjectFamily("F-B", "Beam 200", ElementCategory.Beam);
            family.Properties["WidthM"] = "0.2";
            family.Properties["FireRating"] = "R60";
            project.Families.Add(family);

            var targetFamily = new ProjectFamily("F-B2", "Beam 300", ElementCategory.Beam);
            targetFamily.Properties["WidthM"] = "0.3";
            targetFamily.Properties["FireRating"] = "R90";
            project.Families.Add(targetFamily);

            project.Elements.Add(new ProjectElement("B-1", ElementCategory.Beam, "F-B", "F-01", "Z-01"));
            project.Elements.Add(new ProjectElement("B-2", ElementCategory.Beam, "F-B", "F-01", "Z-01"));
            return project;
        }

        private static void MustFail(Action action)
        {
            try { action(); }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is FormatException || ex is OverflowException) { return; }
            throw new Exception("Expected guarded multi-selection edit to fail.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
