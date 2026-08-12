using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class TktVariantCommands
    {
        [CommandMethod("QS3DGLASSWALL", CommandFlags.UsePickSet)]
        public void CaptureGlassWall() => Capture(ElementCategory.GlassWall, "Vách Kính");

        [CommandMethod("QS3DWALLPIER", CommandFlags.UsePickSet)]
        public void CaptureWallPier() => Capture(ElementCategory.WallPier, "Trụ Tường");

        private static void Capture(ElementCategory category, string label)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    FinalizeUi(document, label + ": đã ghi 0 cấu kiện semantic.");
                    return;
                }

                var projectExistedBeforeCapture = ProjectContextCoordinator.TryGetReadOnly(document, out _);
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var rollback = ProjectStateSnapshot.Capture(project);
                var count = 0;
                try
                {
                    var active = ProjectFamilyActivationService.GetActive(project);
                    var family = active != null && active.Category == category
                        ? active
                        : project.Families.FirstOrDefault(x => x.Category == category);
                    if (family == null)
                    {
                        family = new ProjectFamily(Guid.NewGuid().ToString("N"), label, category);
                        project.Families.Add(family);
                    }

                    EnsureDefault(family, "HeightM", "3.6");
                    EnsureDefault(family, "AxisLeftOffsetM", "0");
                    EnsureDefault(family, "AxisRightOffsetM", "0");
                    if (category == ElementCategory.GlassWall)
                    {
                        EnsureDefault(family, "ThicknessM", "0.012");
                        EnsureDefault(family, "Material", "Kính");
                        EnsureDefault(family, "CurtainMaxPanelWidthM", "1.2");
                        EnsureDefault(family, "CurtainMaxPanelHeightM", "1.5");
                        EnsureDefault(family, "CurtainPerimeterFrameWidthM", "0.05");
                        EnsureDefault(family, "CurtainMullionWidthM", "0.05");
                        EnsureDefault(family, "CurtainTransomWidthM", "0.05");
                        EnsureDefault(family, "CurtainFrameDepthM", "0.05");
                        EnsureDefault(family, "CurtainFrameMaterial", "Nhôm");
                    }
                    else
                    {
                        EnsureDefault(family, "ThicknessM", "0.2");
                        EnsureDefault(family, "Material", "Gạch");
                        EnsureDefault(family, "WallPierProfileMode", "Rectangular");
                        EnsureDefault(family, "WallPierChamferM", "0.02");
                    }

                    ProjectFamilyActivationService.SetActive(project, family.Id);
                    foreach (var snapshot in snapshots)
                        if (SemanticCaptureService.CaptureSnapshot(document, snapshot, category)) count++;
                }
                catch (System.Exception operationError)
                {
                    RestoreVariantOrThrow(document, project, rollback, projectExistedBeforeCapture, operationError, label);
                    throw;
                }

                FinalizeUi(document, label + ": đã ghi " + count + " cấu kiện semantic.");
            }
            catch (System.Exception ex)
            {
                ReportError(document, label + " lỗi: " + ex.Message);
            }
        }

        private static void RestoreVariantOrThrow(
            Document document,
            ProjectState project,
            ProjectStateSnapshot rollback,
            bool projectExistedBeforeCapture,
            System.Exception operationError,
            string label)
        {
            System.Exception? restoreError = null;
            System.Exception? forgetError = null;
            try { rollback.Restore(project); }
            catch (System.Exception error) { restoreError = error; }
            if (!projectExistedBeforeCapture)
            {
                try { ProjectContextCoordinator.Forget(document); }
                catch (System.Exception error) { forgetError = error; }
            }

            if (restoreError != null || forgetError != null)
            {
                var errors = forgetError == null
                    ? new[] { operationError, restoreError! }
                    : restoreError == null
                        ? new[] { operationError, forgetError }
                        : new[] { operationError, restoreError, forgetError };
                throw new InvalidOperationException(label + " thất bại và rollback project không hoàn tất đầy đủ.", new AggregateException(errors));
            }
        }

        private static void FinalizeUi(Document document, string status)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (System.Exception uiError)
            {
                try { document.Editor.WriteMessage("\n[QS3D] Variant capture đã hoàn tất; cảnh báo UI: " + uiError.Message); }
                catch { }
            }
        }

        private static void ReportError(Document document, string status)
        {
            try { PaletteCoordinator.SetStatus(status); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { }
        }

        private static void EnsureDefault(ProjectFamily family, string key, string value)
        {
            if (!family.Properties.ContainsKey(key)) family.Properties[key] = value;
        }
    }
}
