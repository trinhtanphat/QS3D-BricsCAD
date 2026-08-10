using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridAnnotationCommands
    {
        private const int MaxBatch = 2000;

        [CommandMethod("QS3DGRIDANNOTATE", CommandFlags.UsePickSet)]
        public void AnnotateSelected()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0) return;
                if (snapshots.Count > MaxBatch)
                    throw new InvalidOperationException("Grid annotation selection vượt giới hạn " + MaxBatch + " source entities.");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var selected = new List<ProjectElement>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var snapshot in snapshots)
                {
                    var matches = project.Elements
                        .Where(x => x.Category == ElementCategory.Grid &&
                                    x.SourceHandles.Any(h => string.Equals(h, snapshot.Handle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0)
                        throw new InvalidOperationException("Selection chứa entity chưa phải Grid source: " + snapshot.Handle + ". Chạy QS3DGRID trước.");
                    if (matches.Count > 1)
                        throw new InvalidOperationException("Grid source Handle " + snapshot.Handle + " thuộc nhiều semantic Grid; sửa ownership trước.");
                    if (seen.Add(matches[0].Id)) selected.Add(matches[0]);
                }

                var count = GridAnnotationBuilder.Build(document, project, selected);
                FinalizeUi(document, count, "selection");
            }
            catch (Exception ex)
            {
                ReportFailure(document, "QS3DGRIDANNOTATE lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DGRIDANNOTATEALL", CommandFlags.Modal)]
        public void AnnotateAll()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var grids = project.Elements
                    .Where(x => x.Category == ElementCategory.Grid &&
                                x.Properties.TryGetValue(GridNamingService.GridLabelKey, out var label) &&
                                !string.IsNullOrWhiteSpace(label))
                    .Take(MaxBatch + 1)
                    .ToList();
                if (grids.Count > MaxBatch)
                    throw new InvalidOperationException("Grid annotation project batch vượt giới hạn " + MaxBatch + ".");
                if (grids.Count == 0)
                {
                    TryWriteMessage(document, "\nQS3D Grid Annotation: chưa có Grid nào có GridLabel. Chạy QS3DGRIDNUMBER trước.");
                    return;
                }

                var count = GridAnnotationBuilder.Build(document, project, grids);
                FinalizeUi(document, count, "all labeled Grid");
            }
            catch (Exception ex)
            {
                ReportFailure(document, "QS3DGRIDANNOTATEALL lỗi: " + ex.Message);
            }
        }

        private static void FinalizeUi(Document document, int count, string scope)
        {
            var status = "Grid Annotation: đã replace " + count + " Grid (" + scope + ") bằng native bubble/text có QS3D ownership.";
            try { PaletteCoordinator.RefreshProject(); } catch { }
            try { SelectionSyncCoordinator.Refresh(document); } catch { }
            try { PaletteCoordinator.SetStatus(status); } catch { }
            TryWriteMessage(document, "\nQS3D " + status);
        }

        private static void ReportFailure(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWriteMessage(document, "\n" + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }
    }
}
