using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Persistence;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class DrawingUnitWorkflow
    {
        public static bool EnsureResolved(Document document, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var readOnlyExportPreparation = string.Equals(operation, "QS3DED2", StringComparison.OrdinalIgnoreCase);
            var readOnlyBqPreparation = string.Equals(operation, "QS3DBQ", StringComparison.OrdinalIgnoreCase);
            var readOnlyQuantityPreparation = readOnlyExportPreparation || readOnlyBqPreparation;

            if (readOnlyBqPreparation && !ProjectContextCoordinator.TryGetReadOnly(document, out _))
            {
                document.Editor.WriteMessage("\nQS3DBQ: chưa có QS3D project hiện hữu; bảng tổng hợp chỉ đọc không tạo project mới.");
                return false;
            }

            if (CadUnitService.TryGetPolicy(document, out _, out var resolution))
            {
                // ED2 preparation must remain mutation-free until the user confirms an export path.
                // BQ is different: it already requires an existing QS3D project, so a compatible
                // legacy effective-unit assumption can be migrated to the canonical quantity binding
                // without creating a project, prompting for a unit, or changing live INSUNITS.
                if (!readOnlyExportPreparation)
                    PersistLegacyBindingIfNeeded(document, resolution);
                return true;
            }

            if (readOnlyQuantityPreparation)
            {
                if (readOnlyExportPreparation)
                    document.Editor.WriteMessage("\nQS3DED2: drawing unit is undefined/unsupported. Run QS3DUNITS first; ED2 export preparation does not create or persist project/unit state before Save confirmation.");
                else
                    document.Editor.WriteMessage("\nQS3DBQ: drawing unit is undefined/unsupported. Run QS3DUNITS first; BQ read-only preparation does not create or persist project/unit state.");
                return false;
            }

            document.Editor.WriteMessage("\n" + operation + ": INSUNITS is undefined/unsupported; choose the real drawing unit before quantity conversion.");
            return PromptAndPersist(document);
        }

        public static void Configure(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (CadUnitService.TryGetNativeLengthUnit(document, out var native))
            {
                var message = "QS3D units are controlled by INSUNITS: " + native + ". Change INSUNITS to change the drawing unit.";
                document.Editor.WriteMessage("\n" + message);
                PaletteCoordinator.SetStatus(message);
                return;
            }

            if (!PromptAndPersist(document))
                document.Editor.WriteMessage("\nQS3DUNITS cancelled; unit-dependent operations remain blocked.");
        }

        private static bool PromptAndPersist(Document document)
        {
            LengthUnit unit;
            if (!DrawingUnitAutomationConfirmation.TryConsume(document, out unit))
            {
                var prompt = document.Editor.GetKeywords(
                    "\nDrawing unit [Inch/Foot/Mile/Millimeter/Centimeter/Meter/Kilometer/Microinch/Mil/Yard/Angstrom/Nanometer/Micrometer/Decimeter/Decameter/Hectometer/Gigameter/AstronomicalUnit/LightYear/Parsec/USSurveyFoot/USSurveyInch/USSurveyYard/USSurveyMile]: ",
                    "Inch Foot Mile Millimeter Centimeter Meter Kilometer Microinch Mil Yard Angstrom Nanometer Micrometer Decimeter Decameter Hectometer Gigameter AstronomicalUnit LightYear Parsec USSurveyFoot USSurveyInch USSurveyYard USSurveyMile");
                if (prompt.Status != PromptStatus.OK) return false;
                if (!Enum.TryParse(prompt.StringResult, true, out unit))
                    throw new InvalidOperationException("Unsupported drawing unit: " + prompt.StringResult + ".");
            }
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("QS3DUNITS requires the DWG that started the prompt to remain active.");

            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            string path;
            try
            {
                DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(project.Metadata, project.Elements.Count > 0, unit);
                if (project.Elements.Count > 0)
                    DrawingUnitResolutionPolicy.BindQuantityUnit(project.Metadata, true, unit, DrawingUnitResolutionSource.ProjectOverride);
                DrawingUnitResolutionPolicy.SetProjectOverride(project.Metadata, unit);
                project.Touch();
                path = ProjectContextCoordinator.Save(document);
            }
            catch
            {
                rollback.Restore(project);
                throw;
            }
            var message = "QS3D drawing unit set to " + unit + " and saved in " + path + ".";
            document.Editor.WriteMessage("\n" + message);
            PaletteCoordinator.SetStatus(message);
            return true;
        }

        private static void PersistLegacyBindingIfNeeded(Document document, DrawingUnitResolution resolution)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var observedProject)) return;
            if (observedProject.Elements.Count == 0) return;

            var project = ExistingProjectMutationContext.Require(document, "Legacy drawing-unit binding");
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                if (!DrawingUnitResolutionPolicy.BindQuantityUnit(project.Metadata, true, resolution.Unit, resolution.Source)) return;
                project.Touch();
                ProjectContextCoordinator.Save(document);
            }
            catch
            {
                rollback.Restore(project);
                throw;
            }
        }
    }
}
