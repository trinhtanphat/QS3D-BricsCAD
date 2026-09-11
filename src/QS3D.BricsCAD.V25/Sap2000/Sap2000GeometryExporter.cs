using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Interoperability.Sap2000;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.Sap2000
{
    internal static class Sap2000GeometryExporter
    {
        public static Sap2000ExportPlan BuildFromSelection(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var editor = document.Editor;
            var options = new PromptSelectionOptions
            {
                MessageForAdding = "\nChọn LINE hoặc 2D POLYLINE để xuất sang SAP2000: "
            };

            var selection = editor.GetSelection(options);
            if (selection.Status == PromptStatus.Cancel)
            {
                return new Sap2000ExportPlan(
                    new Sap2000FrameMember[0],
                    new Sap2000AreaMember[0],
                    new[] { "Đã hủy chọn đối tượng." });
            }

            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                throw new InvalidOperationException("Không lấy được tập đối tượng để xuất SAP2000.");
            }

            // Reuse QS3D's canonical unit-resolution policy instead of maintaining a
            // second INSUNITS table. This also honors an explicit project unit override.
            var metresPerDrawingUnit = CadUnitService.DrawingUnitsToMeters(document, 1d);
            var frames = new List<Sap2000FrameMember>();
            var areas = new List<Sap2000AreaMember>();
            var warnings = new List<string>();

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (SelectedObject selected in selection.Value)
                {
                    if (selected == null || selected.ObjectId.IsNull)
                    {
                        continue;
                    }

                    var entity = transaction.GetObject(selected.ObjectId, OpenMode.ForRead, false) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    var line = entity as Line;
                    if (line != null)
                    {
                        TryAddLine(line, metresPerDrawingUnit, frames, warnings);
                        continue;
                    }

                    var polyline = entity as Polyline;
                    if (polyline != null)
                    {
                        TryAddPolyline(polyline, metresPerDrawingUnit, frames, areas, warnings);
                        continue;
                    }

                    warnings.Add("Bỏ qua " + entity.GetType().Name + " handle " + entity.Handle + ": MVP chỉ hỗ trợ LINE và 2D POLYLINE.");
                }
            }

            return new Sap2000ExportPlan(frames, areas, warnings);
        }

        private static void TryAddLine(
            Line line,
            double metresPerDrawingUnit,
            ICollection<Sap2000FrameMember> frames,
            ICollection<string> warnings)
        {
            try
            {
                frames.Add(new Sap2000FrameMember(
                    "QS3D_F_" + line.Handle,
                    ToSapPoint(line.StartPoint, metresPerDrawingUnit),
                    ToSapPoint(line.EndPoint, metresPerDrawingUnit)));
            }
            catch (ArgumentException ex)
            {
                warnings.Add("Bỏ qua LINE " + line.Handle + ": " + ex.Message);
            }
        }

        private static void TryAddPolyline(
            Polyline polyline,
            double metresPerDrawingUnit,
            ICollection<Sap2000FrameMember> frames,
            ICollection<Sap2000AreaMember> areas,
            ICollection<string> warnings)
        {
            if (polyline.NumberOfVertices < 2)
            {
                warnings.Add("Bỏ qua POLYLINE " + polyline.Handle + ": cần ít nhất 2 đỉnh.");
                return;
            }

            if (polyline.Closed)
            {
                if (polyline.NumberOfVertices < 3)
                {
                    warnings.Add("Bỏ qua POLYLINE kín " + polyline.Handle + ": area cần ít nhất 3 đỉnh.");
                    return;
                }

                var vertices = new List<Sap2000Point3>(polyline.NumberOfVertices);
                for (var index = 0; index < polyline.NumberOfVertices; index++)
                {
                    vertices.Add(ToSapPoint(polyline.GetPoint3dAt(index), metresPerDrawingUnit));
                }

                try
                {
                    areas.Add(new Sap2000AreaMember("QS3D_A_" + polyline.Handle, vertices));
                }
                catch (ArgumentException ex)
                {
                    warnings.Add("Bỏ qua area " + polyline.Handle + ": " + ex.Message);
                }

                return;
            }

            for (var index = 0; index < polyline.NumberOfVertices - 1; index++)
            {
                try
                {
                    frames.Add(new Sap2000FrameMember(
                        "QS3D_F_" + polyline.Handle + "_" + (index + 1),
                        ToSapPoint(polyline.GetPoint3dAt(index), metresPerDrawingUnit),
                        ToSapPoint(polyline.GetPoint3dAt(index + 1), metresPerDrawingUnit)));
                }
                catch (ArgumentException ex)
                {
                    warnings.Add("Bỏ qua segment " + polyline.Handle + "/" + (index + 1) + ": " + ex.Message);
                }
            }
        }

        private static Sap2000Point3 ToSapPoint(Point3d point, double metresPerDrawingUnit)
        {
            return new Sap2000Point3(
                point.X * metresPerDrawingUnit,
                point.Y * metresPerDrawingUnit,
                point.Z * metresPerDrawingUnit);
        }
    }
}
