using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogXmlTextSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsXmlInvalidMaterialFields();
            InvalidRenameIsAtomic();
            ValidRenameStillUpdatesCatalogAndReferences();
        }

        private static void RejectsXmlInvalidMaterialFields()
        {
            ThrowsArgument(() => new ProjectMaterial("mat-\uFFFE", "Valid", "m2", "desc", false));
            ThrowsArgument(() => new ProjectMaterial("mat-name", "Invalid\uFFFE", "m2", "desc", false));
            ThrowsArgument(() => new ProjectMaterial("mat-unit", "Valid", "m\uFFFF", "desc", false));
            ThrowsArgument(() => new ProjectMaterial("mat-desc", "Valid", "m2", "bad\uFFFEtext", false));
        }

        private static void InvalidRenameIsAtomic()
        {
            var project = new ProjectState("material-xml-atomic", "Material XML atomicity");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-1", "Old material", "m2", "valid");

            var element = new ProjectElement("SLAB-1", ElementCategory.Slab);
            element.SetProperty("Material", "Old material");
            project.Elements.Add(element);

            Require(project.Metadata.TryGetValue(ProjectMaterialCatalog.MetadataKey, out var metadataBefore),
                "Material catalog metadata was not created for the atomicity regression.");
            var versionBefore = project.ChangeVersion;
            var updatedBefore = project.UpdatedUtc;
            var elementUpdatedBefore = element.UpdatedUtc;
            var dirtyBefore = element.Dirty;

            ThrowsArgument(() =>
                ProjectMaterialCatalog.UpsertCustom(project, "mat-1", "New\uFFFE material", "m2", "valid"));

            Require(project.Metadata.TryGetValue(ProjectMaterialCatalog.MetadataKey, out var metadataAfter),
                "Rejected material rename removed catalog metadata.");
            Require(string.Equals(metadataBefore, metadataAfter, StringComparison.Ordinal),
                "Rejected XML-invalid material rename changed persisted catalog metadata.");
            Require(project.ChangeVersion == versionBefore,
                "Rejected XML-invalid material rename changed the project revision.");
            Require(project.UpdatedUtc == updatedBefore,
                "Rejected XML-invalid material rename changed the project timestamp.");
            Require(element.Properties.TryGetValue("Material", out var materialName) && materialName == "Old material",
                "Rejected XML-invalid material rename changed an element material reference.");
            Require(element.UpdatedUtc == elementUpdatedBefore,
                "Rejected XML-invalid material rename changed the element timestamp.");
            Require(element.Dirty == dirtyBefore,
                "Rejected XML-invalid material rename changed element dirty flags.");

            var custom = ProjectMaterialCatalog.GetCustom(project);
            Require(custom.Count == 1 && custom[0].Id == "mat-1" && custom[0].Name == "Old material",
                "Rejected XML-invalid material rename changed the material catalog view.");
        }

        private static void ValidRenameStillUpdatesCatalogAndReferences()
        {
            var project = new ProjectState("material-xml-valid", "Material XML valid control");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-1", "Old material", "m2", "before");

            var element = new ProjectElement("SLAB-1", ElementCategory.Slab);
            element.SetProperty("Material", "Old material");
            project.Elements.Add(element);

            var renamed = ProjectMaterialCatalog.UpsertCustom(project, "mat-1", "New material", "m2", "after");
            Require(renamed.Name == "New material" && renamed.Description == "after",
                "Valid material rename did not return the updated material.");
            Require(element.Properties.TryGetValue("Material", out var materialName) && materialName == "New material",
                "Valid material rename did not update an element material reference.");

            var custom = ProjectMaterialCatalog.GetCustom(project);
            Require(custom.Count == 1 && custom[0].Name == "New material" && custom[0].Description == "after",
                "Valid material rename did not persist the updated catalog entry.");
        }

        private static void ThrowsArgument(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Expected ArgumentException.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
