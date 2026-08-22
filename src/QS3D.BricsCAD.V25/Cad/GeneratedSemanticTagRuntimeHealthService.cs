using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedSemanticTagRuntimeHealthService
    {
        private const string RotationKey = "GeneratedSemanticTagRotationRad";

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    if (handles.Length == 0) continue;

                    foreach (var handle in handles)
                    {
                        if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                        {
                            issues.Add(new ModelHealthIssue(
                                "SEMANTIC_TAG_MTEXT_HANDLE_INVALID",
                                HealthSeverity.Error,
                                "GeneratedSemanticTagHandles chứa handle không phải hexadecimal metadata hợp lệ: " + handle + ". Health chỉ báo lỗi, không sửa/xóa CAD.",
                                element.Id));
                            continue;
                        }

                        ObjectId id;
                        try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                        catch
                        {
                            AddMissing(issues, element, handle);
                            continue;
                        }
                        if (id.IsNull || !id.IsValid)
                        {
                            AddMissing(issues, element, handle);
                            continue;
                        }

                        Entity? entity;
                        try { entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity; }
                        catch
                        {
                            AddMissing(issues, element, handle);
                            continue;
                        }
                        if (entity == null || entity.IsErased)
                        {
                            AddMissing(issues, element, handle);
                            continue;
                        }
                        if (!(entity is MText tag))
                        {
                            issues.Add(new ModelHealthIssue(
                                "SEMANTIC_TAG_MTEXT_TYPE_MISMATCH",
                                HealthSeverity.Error,
                                "GeneratedSemanticTagHandles trỏ tới live CAD object nhưng không phải MText: " + handle + ". Health chỉ báo lỗi, không sửa/xóa CAD.",
                                element.Id));
                            continue;
                        }
                        if (!GeneratedGeometryService.HasMatchingOwnership(tag, project, element))
                        {
                            issues.Add(new ModelHealthIssue(
                                "SEMANTIC_TAG_MTEXT_OWNERSHIP_MISMATCH",
                                HealthSeverity.Error,
                                "Generated semantic tag MText còn sống nhưng XData ownership không khớp project/element/category hiện tại: " + handle + ".",
                                element.Id));
                        }

                        InspectContent(element, tag, issues);
                        InspectTextHeight(document, element, tag, issues);
                        InspectPlacement(element, tag, issues);
                    }
                }
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }

        private static void AddMissing(ICollection<ModelHealthIssue> issues, ProjectElement element, string handle)
        {
            issues.Add(new ModelHealthIssue(
                "SEMANTIC_TAG_MTEXT_MISSING",
                HealthSeverity.Error,
                "Không còn resolve được generated semantic tag MText: " + handle + ". Health chỉ báo lỗi; dùng QS3DTAGREFRESH/QS3DTAGREMOVE để xử lý có chủ ý.",
                element.Id));
        }

        private static void InspectContent(ProjectElement element, MText tag, ICollection<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.TextKey, out var raw)) return;
            var expected = EncodePlainMText(raw ?? string.Empty);
            var actual = tag.Contents ?? string.Empty;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_TAG_MTEXT_CONTENT_DRIFT",
                    HealthSeverity.Warning,
                    "Live semantic tag MText content không còn khớp rendered text đã ghi nhận; refresh tag trước khi phát hành.",
                    element.Id));
            }
        }

        private static void InspectTextHeight(Document document, ProjectElement element, MText tag, ICollection<ModelHealthIssue> issues)
        {
            if (!TryFinite(element, GeneratedSemanticTagHealthService.TextHeightKey, out var heightM) || !(heightM > 0d)) return;
            var expected = CadUnitService.MetersToDrawingUnits(document, heightM);
            if (!Finite(expected) || !(expected > 0d)) return;
            var tolerance = Math.Max(1e-8d, Math.Abs(expected) * 1e-8d);
            if (!Finite(tag.TextHeight) || Math.Abs(tag.TextHeight - expected) > tolerance)
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_TAG_MTEXT_HEIGHT_DRIFT",
                    HealthSeverity.Warning,
                    "Live semantic tag MText TextHeight không còn khớp semantic tag metadata.",
                    element.Id));
            }
        }

        private static void InspectPlacement(ProjectElement element, MText tag, ICollection<ModelHealthIssue> issues)
        {
            if (TryFinite(element, GeneratedSemanticTagHealthService.PositionXKey, out var x) &&
                TryFinite(element, GeneratedSemanticTagHealthService.PositionYKey, out var y) &&
                TryFinite(element, GeneratedSemanticTagHealthService.PositionZKey, out var z))
            {
                var expected = new Point3d(x, y, z);
                var actual = tag.Location;
                var dx = actual.X - expected.X;
                var dy = actual.Y - expected.Y;
                var dz = actual.Z - expected.Z;
                var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                var scale = Math.Max(1d, Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z))));
                var tolerance = Math.Max(1e-7d, scale * 1e-10d);
                if (!Finite(distance) || distance > tolerance)
                {
                    issues.Add(new ModelHealthIssue(
                        "SEMANTIC_TAG_MTEXT_POSITION_DRIFT",
                        HealthSeverity.Warning,
                        "Live semantic tag MText Location không còn khớp drawing-local WCS position đã ghi nhận.",
                        element.Id));
                }
            }

            if (TryFinite(element, RotationKey, out var expectedRotation) && Finite(tag.Rotation))
            {
                var delta = AngleDistance(tag.Rotation, expectedRotation);
                if (delta > 1e-8d)
                {
                    issues.Add(new ModelHealthIssue(
                        "SEMANTIC_TAG_MTEXT_ROTATION_DRIFT",
                        HealthSeverity.Warning,
                        "Live semantic tag MText Rotation không còn khớp rotation đã ghi nhận.",
                        element.Id));
                }
            }

            var normal = tag.Normal;
            var length = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
            if (!Finite(length) || !(length > 0d) ||
                Math.Abs(normal.X / length) > 1e-9d ||
                Math.Abs(normal.Y / length) > 1e-9d ||
                Math.Abs(normal.Z / length - 1d) > 1e-9d)
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_TAG_MTEXT_NORMAL_DRIFT",
                    HealthSeverity.Warning,
                    "Live semantic tag MText Normal không còn là +Z theo P0 drawing-local WCS contract.",
                    element.Id));
            }
        }

        private static bool TryFinite(ProjectElement element, string key, out double value)
        {
            value = 0d;
            return element.Properties.TryGetValue(key, out var raw) &&
                   double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   Finite(value);
        }

        private static double AngleDistance(double first, double second)
        {
            var delta = (first - second) % (Math.PI * 2d);
            if (delta > Math.PI) delta -= Math.PI * 2d;
            if (delta < -Math.PI) delta += Math.PI * 2d;
            return Math.Abs(delta);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static string EncodePlainMText(string value)
        {
            var text = value ?? string.Empty;
            var output = new StringBuilder(text.Length + 16);
            for (var index = 0; index < text.Length; index++)
            {
                var ch = text[index];
                if (ch == '\r')
                {
                    if (index + 1 < text.Length && text[index + 1] == '\n') index++;
                    output.Append("\\P");
                }
                else if (ch == '\n') output.Append("\\P");
                else if (ch == '\\') output.Append("\\\\");
                else if (ch == '{') output.Append("\\{");
                else if (ch == '}') output.Append("\\}");
                else output.Append(ch);
            }
            return output.ToString();
        }
    }
}
