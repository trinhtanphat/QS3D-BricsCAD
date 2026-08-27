using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Geometry;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Reviewed command-line routes for creating native Grid systems from the canonical Core planner.
    /// The commands intentionally collect only bounded inputs, show a plan summary, and require an
    /// explicit Yes before GridSystemNativeMaterializer mutates CAD/project state.
    /// </summary>
    public sealed class GridSystemCreationCommands
    {
        private const int MaxStationsPerFamily = 1000;
        private const double CoordinateTolerance = 1e-8d;
        private const double DirectionTolerance = 1e-6d;

        [CommandMethod("QS3DGRIDSYSTEMRECT")]
        public void CreateRectangularSystem()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var editor = document.Editor;
                var units = CadUnitService.GetPolicy(document);
                var originResult = editor.GetPoint(new PromptPointOptions("\nGrid rectangular origin: "));
                if (originResult.Status != PromptStatus.OK) return;

                var axisOptions = new PromptPointOptions("\nPick positive U-axis direction point: ")
                {
                    BasePoint = originResult.Value,
                    UseBasePoint = true
                };
                var axisResult = editor.GetPoint(axisOptions);
                if (axisResult.Status != PromptStatus.OK) return;

                var dx = axisResult.Value.X - originResult.Value.X;
                var dy = axisResult.Value.Y - originResult.Value.Y;
                var axisLength = Math.Sqrt(dx * dx + dy * dy);
                if (!Finite(axisLength) || !(axisLength > CoordinateTolerance))
                    throw new InvalidOperationException("Grid U-axis direction is zero/degenerate.");

                var uAxis = new Point2(dx / axisLength, dy / axisLength);
                var vAxis = new Point2(-uAxis.Y, uAxis.X);
                var uCount = PromptPositiveInteger(editor, "\nU Grid count", 4);
                if (!uCount.HasValue) return;
                var vCount = PromptPositiveInteger(editor, "\nV Grid count", 4);
                if (!vCount.HasValue) return;
                var uSpacing = PromptPositiveDistance(editor, "\nU spacing in drawing units", 6000d);
                if (!uSpacing.HasValue) return;
                var vSpacing = PromptPositiveDistance(editor, "\nV spacing in drawing units", 6000d);
                if (!vSpacing.HasValue) return;
                var prefix = PromptPrefix(editor, "RECT");
                if (prefix == null) return;

                var uSpacingM = units.ToMeters(uSpacing.Value);
                var vSpacingM = units.ToMeters(vSpacing.Value);
                var uStations = BuildLinearStations(prefix + "-U", uCount.Value, uSpacingM);
                var vStations = BuildLinearStations(prefix + "-V", vCount.Value, vSpacingM);
                var originM = new Point2(units.ToMeters(originResult.Value.X), units.ToMeters(originResult.Value.Y));
                var input = new RectangularGridSystemInput
                {
                    OriginM = originM,
                    UAxis = uAxis,
                    VAxis = vAxis,
                    UStations = uStations,
                    VStations = vStations,
                    UMinM = -uSpacingM,
                    UMaxM = uSpacingM * uCount.Value,
                    VMinM = -vSpacingM,
                    VMaxM = vSpacingM * vCount.Value
                };
                var planned = GridSystemPlanner.PlanRectangular(input, CoordinateTolerance, DirectionTolerance);
                if (!Confirm(editor, "Rectangular", planned.Count, prefix)) return;

                var created = GridSystemNativeMaterializer.Materialize(
                    document,
                    planned,
                    units.ToMeters(originResult.Value.Z));
                editor.WriteMessage("\nQS3D Grid rectangular created " + created.ToString(CultureInfo.InvariantCulture) + " owned native sources.");
            }
            catch (Exception ex)
            {
                TryWriteFailure(document, "QS3DGRIDSYSTEMRECT lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DGRIDSYSTEMRADIAL")]
        public void CreateRadialSystem()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var editor = document.Editor;
                var units = CadUnitService.GetPolicy(document);
                var centerResult = editor.GetPoint(new PromptPointOptions("\nGrid radial center: "));
                if (centerResult.Status != PromptStatus.OK) return;

                var rayCount = PromptPositiveInteger(editor, "\nRay Grid count", 8);
                if (!rayCount.HasValue) return;
                var ringCount = PromptPositiveInteger(editor, "\nRing Grid count", 4);
                if (!ringCount.HasValue) return;
                var ringSpacing = PromptPositiveDistance(editor, "\nRing spacing in drawing units", 3000d);
                if (!ringSpacing.HasValue) return;
                var prefix = PromptPrefix(editor, "RAD");
                if (prefix == null) return;

                var spacingM = units.ToMeters(ringSpacing.Value);
                var rays = new List<GridAngularStation>(rayCount.Value);
                for (var i = 0; i < rayCount.Value; i++)
                {
                    var angle = (Math.PI * 2d * i) / rayCount.Value;
                    rays.Add(new GridAngularStation(prefix + "-RAY-" + (i + 1).ToString("D3", CultureInfo.InvariantCulture), angle));
                }
                var rings = new List<GridRadialStation>(ringCount.Value);
                for (var i = 0; i < ringCount.Value; i++)
                    rings.Add(new GridRadialStation(prefix + "-RING-" + (i + 1).ToString("D3", CultureInfo.InvariantCulture), spacingM * (i + 1)));

                var input = new RadialGridSystemInput
                {
                    CenterM = new Point2(units.ToMeters(centerResult.Value.X), units.ToMeters(centerResult.Value.Y)),
                    Rays = rays.AsReadOnly(),
                    Rings = rings.AsReadOnly(),
                    InnerRadiusM = 0d,
                    OuterRadiusM = spacingM * ringCount.Value
                };
                var planned = GridSystemPlanner.PlanRadial(input, CoordinateTolerance, CoordinateTolerance);
                if (!Confirm(editor, "Radial", planned.Count, prefix)) return;

                var created = GridSystemNativeMaterializer.Materialize(
                    document,
                    planned,
                    units.ToMeters(centerResult.Value.Z));
                editor.WriteMessage("\nQS3D Grid radial created " + created.ToString(CultureInfo.InvariantCulture) + " owned native sources.");
            }
            catch (Exception ex)
            {
                TryWriteFailure(document, "QS3DGRIDSYSTEMRADIAL lỗi: " + ex.Message);
            }
        }

        private static IReadOnlyList<GridLinearStation> BuildLinearStations(string prefix, int count, double spacingM)
        {
            var stations = new List<GridLinearStation>(count);
            for (var i = 0; i < count; i++)
                stations.Add(new GridLinearStation(prefix + "-" + (i + 1).ToString("D3", CultureInfo.InvariantCulture), spacingM * i));
            return stations.AsReadOnly();
        }

        private static int? PromptPositiveInteger(Editor editor, string label, int defaultValue)
        {
            var options = new PromptIntegerOptions(label + " <" + defaultValue.ToString(CultureInfo.InvariantCulture) + ">: ")
            {
                AllowNone = true,
                AllowNegative = false,
                AllowZero = false,
                DefaultValue = defaultValue,
                LowerLimit = 1,
                UpperLimit = MaxStationsPerFamily
            };
            var result = editor.GetInteger(options);
            if (result.Status == PromptStatus.None) return defaultValue;
            if (result.Status != PromptStatus.OK) return null;
            return result.Value;
        }

        private static double? PromptPositiveDistance(Editor editor, string label, double defaultValue)
        {
            var options = new PromptDoubleOptions(label + " <" + defaultValue.ToString("G17", CultureInfo.InvariantCulture) + ">: ")
            {
                AllowNone = true,
                AllowNegative = false,
                AllowZero = false,
                DefaultValue = defaultValue
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.None) return defaultValue;
            if (result.Status != PromptStatus.OK) return null;
            if (!Finite(result.Value) || !(result.Value > 0d))
                throw new InvalidOperationException(label + " must be finite and positive.");
            return result.Value;
        }

        private static string? PromptPrefix(Editor editor, string defaultValue)
        {
            var options = new PromptStringOptions("\nSemantic Grid id prefix <" + defaultValue + ">: ")
            {
                AllowSpaces = false,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var result = editor.GetString(options);
            if (result.Status != PromptStatus.OK) return null;
            var value = (result.StringResult ?? string.Empty).Trim();
            if (value.Length == 0) throw new InvalidOperationException("Grid semantic id prefix is required.");
            if (value.Length > 64) throw new InvalidOperationException("Grid semantic id prefix exceeds 64 characters.");
            return value;
        }

        private static bool Confirm(Editor editor, string kind, int count, string prefix)
        {
            var options = new PromptKeywordOptions(
                "\nReview " + kind + " Grid plan: " + count.ToString(CultureInfo.InvariantCulture) +
                " curves, semantic prefix '" + prefix + "'. Create? [Yes/No] <No>: ", "Yes No")
            {
                AllowNone = true
            };
            var result = editor.GetKeywords(options);
            return result.Status == PromptStatus.OK && string.Equals(result.StringResult, "Yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void TryWriteFailure(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
