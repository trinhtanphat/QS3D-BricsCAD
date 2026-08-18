using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantityEngine2Commands
    {
        [CommandMethod("QS3DQUANTITYENGINE2", CommandFlags.Modal)]
        public void CalculateQuantity()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!DrawingUnitWorkflow.EnsureResolved(document, "QS3DQUANTITYENGINE2")) return;

                var project = ExistingProjectMutationContext.Require(document, "Tính khối lượng (Engine2)");
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                    .RegenerateDirty(project);
                var rows = ProjectQuantityReportBuilder.Group(project);
                var summary = QuantityEngine2Summary.Build(rows, regenerated);
                if (summary.ElementCount == 0)
                    throw new InvalidOperationException(
                        "Chưa có cấu kiện hợp lệ để tính khối lượng. Hãy capture/tạo cấu kiện QS3D rồi chạy lại Engine2.");

                try
                {
                    PaletteCoordinator.RefreshProject();
                    PaletteCoordinator.SetStatus(summary.StatusText);
                }
                catch
                {
                    // Quantity calculation already succeeded. A palette refresh problem
                    // must not hide the authoritative result popup from the user.
                }

                QuantityCalculationResultWindow.ShowSuccess(summary);
            }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3D Tính khối lượng (Engine2) lỗi: " + ex.Message); }
                catch { }
                QuantityCalculationResultWindow.ShowError(ex.Message);
            }
        }
    }

    internal sealed class QuantityEngine2Summary
    {
        private QuantityEngine2Summary(
            int elementCount,
            double concreteM3,
            double deductionM3,
            double formworkM2,
            double beamWallLengthM,
            double outerPerimeterM,
            double innerPerimeterM,
            int regeneratedCount)
        {
            ElementCount = elementCount;
            ConcreteM3 = concreteM3;
            DeductionM3 = deductionM3;
            FormworkM2 = formworkM2;
            BeamWallLengthM = beamWallLengthM;
            OuterPerimeterM = outerPerimeterM;
            InnerPerimeterM = innerPerimeterM;
            RegeneratedCount = regeneratedCount;
        }

        public int ElementCount { get; }
        public double ConcreteM3 { get; }
        public double DeductionM3 { get; }
        public double FormworkM2 { get; }
        public double BeamWallLengthM { get; }
        public double OuterPerimeterM { get; }
        public double InnerPerimeterM { get; }
        public int RegeneratedCount { get; }
        public bool ReusedExistingResult => RegeneratedCount == 0;

        public string StatusText => ReusedExistingResult
            ? "Tính khối lượng: dùng lại kết quả hiện hành — model chưa đổi."
            : "Tính khối lượng: đã cập nhật " + RegeneratedCount + " lượt cấu kiện dirty.";

        public static QuantityEngine2Summary Build(IReadOnlyList<QuantityReportRow> rows, int regeneratedCount)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (regeneratedCount < 0) throw new ArgumentOutOfRangeException(nameof(regeneratedCount));

            var elementCount = 0;
            var concrete = new QuantityReportMath.FiniteAccumulator();
            var deduction = new QuantityReportMath.FiniteAccumulator();
            var formwork = new QuantityReportMath.FiniteAccumulator();
            var beamWallLength = new QuantityReportMath.FiniteAccumulator();
            var outerPerimeter = new QuantityReportMath.FiniteAccumulator();
            var innerPerimeter = new QuantityReportMath.FiniteAccumulator();

            foreach (var row in rows)
            {
                if (row == null) throw new InvalidOperationException("Quantity Engine2 summary cannot contain a null report row.");

                elementCount = QuantityReportMath.AddCount(elementCount, row.Count);
                concrete.Add(row.NetConcreteM3, "Engine2 concrete");
                deduction.Add(row.DeductionM3, "Engine2 deduction");
                formwork.Add(row.FormworkM2, "Engine2 formwork");

                if (IsBeamOrWall(row.Category))
                    beamWallLength.Add(row.LengthM, "Engine2 beam/wall length");

                if (IsSlabOrFoundation(row.Category))
                {
                    outerPerimeter.Add(row.OuterPerimeterM, "Engine2 outer perimeter");
                    innerPerimeter.Add(row.InnerPerimeterM, "Engine2 inner perimeter");
                }
            }

            return new QuantityEngine2Summary(
                elementCount,
                concrete.Value("Engine2 concrete"),
                deduction.Value("Engine2 deduction"),
                formwork.Value("Engine2 formwork"),
                beamWallLength.Value("Engine2 beam/wall length"),
                outerPerimeter.Value("Engine2 outer perimeter"),
                innerPerimeter.Value("Engine2 inner perimeter"),
                regeneratedCount);
        }

        private static bool IsBeamOrWall(string category) =>
            string.Equals(category, ElementCategory.Beam.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, ElementCategory.StructuralWall.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, ElementCategory.ArchitecturalWall.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, ElementCategory.GlassWall.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, ElementCategory.WallPier.ToString(), StringComparison.OrdinalIgnoreCase);

        private static bool IsSlabOrFoundation(string category) =>
            string.Equals(category, ElementCategory.Slab.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, ElementCategory.Foundation.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
