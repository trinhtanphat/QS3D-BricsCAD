using System;
using System.Collections;
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
                                "SEMANTIC_TAG_HANDLE_INVALID",
                                HealthSeverity.Error,
                                "GeneratedSemanticTagHandles chứa handle không phải hexadecimal metadata hợp lệ: " + handle + ". Health chỉ báo lỗi, không sửa/xóa CAD.",
                                element.Id));
                            continue;
                        }

                        ObjectId id;
                        try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                        catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
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
                        catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
                        {
                            AddMissing(issues, element, handle);
                            continue;
                        }
                        if (entity == null || entity.IsErased)
                        {
                            AddMissing(issues, element, handle);
                            continue;
                        }

                        if (!GeneratedGeometryService.HasMatchingOwnership(entity, project, element))
                        {
                            issues.Add(new ModelHealthIssue(
                                "SEMANTIC_TAG_OWNERSHIP_MISMATCH",
                                HealthSeverity.Error,
                                "Generated semantic tag còn sống nhưng XData ownership không khớp project/element/category hiện tại: " + handle + ".",
                                element.Id));
                        }

                        if (entity is MText mtext)
                        {
                            InspectMText(document, element, mtext, issues);
                        }
                        else if (entity is MLeader mleader)
                        {
                            InspectMLeader(document, element, mleader, issues);
                        }
                        else
                        {
                            issues.Add(new ModelHealthIssue(
                                "SEMANTIC_TAG_TYPE_MISMATCH",
                                HealthSeverity.Error,
                                "GeneratedSemanticTagHandles trỏ tới live CAD object nhưng không phải MText/MLeader: " + handle + ". Health chỉ báo lỗi, không sửa/xóa CAD.",
                                element.Id));
                        }
                    }
                }
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }

        private static void AddMissing(ICollection<ModelHealthIssue> issues, ProjectElement element, string handle)
        {
            issues.Add(new ModelHealthIssue(
                "SEMANTIC_TAG_MISSING",
                HealthSeverity.Error,
                "Không còn resolve được generated semantic tag MText/MLeader: " + handle + ". Health chỉ báo lỗi; dùng QS3DTAGREFRESH/QS3DTAGLEADERREFRESH/QS3DTAGREMOVE để xử lý có chủ ý.",
                element.Id));
        }

        private static void InspectMText(
            Document document,
            ProjectElement element,
            MText tag,
            ICollection<ModelHealthIssue> issues)
        {
            var kind = Property(element, GeneratedSemanticTagHealthService.ArtifactKindKey);
            if (kind.Length > 0 && !string.Equals(kind, GeneratedSemanticTagHealthService.MTextArtifactKind, StringComparison.Ordinal))
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_TAG_MTEXT_KIND_MISMATCH",
                    HealthSeverity.Error,
                    "Live MText semantic tag không khớp GeneratedSemanticTagArtifactKind metadata.",
                    element.Id));
            }
            InspectContent(element, tag.Contents ?? string.Empty, "SEMANTIC_TAG_MTEXT_CONTENT_DRIFT", issues);
            InspectTextHeight(document, element, tag.TextHeight, "SEMANTIC_TAG_MTEXT_HEIGHT_DRIFT", issues);
            InspectPoint(element, tag.Location, GeneratedSemanticTagHealthService.PositionXKey, GeneratedSemanticTagHealthService.PositionYKey, GeneratedSemanticTagHealthService.PositionZKey, "SEMANTIC_TAG_MTEXT_POSITION_DRIFT", "Live semantic tag MText Location không còn khớp drawing-local WCS position đã ghi nhận.", issues);

            if (TryFinite(element, GeneratedSemanticTagHealthService.RotationKey, out var expectedRotation) && Finite(tag.Rotation))
            {
                var delta = AngleDistance(tag.Rotation, expectedRotation);
                if (delta > 1e-8d)
                    issues.Add(new ModelHealthIssue("SEMANTIC_TAG_MTEXT_ROTATION_DRIFT", HealthSeverity.Warning, "Live semantic tag MText Rotation không còn khớp rotation đã ghi nhận.", element.Id));
            }
            InspectNormal(element, tag.Normal, "SEMANTIC_TAG_MTEXT_NORMAL_DRIFT", issues);
        }

        private static void InspectMLeader(
            Document document,
            ProjectElement element,
            MLeader tag,
            ICollection<ModelHealthIssue> issues)
        {
            if (!string.Equals(Property(element, GeneratedSemanticTagHealthService.ArtifactKindKey), GeneratedSemanticTagHealthService.MLeaderArtifactKind, StringComparison.Ordinal))
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_TAG_MLEADER_KIND_MISMATCH",
                    HealthSeverity.Error,
                    "Live MLeader semantic tag yêu cầu GeneratedSemanticTagArtifactKind=MLeader.",
                    element.Id));
            }

            var content = tag.MText;
            if (content == null)
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_TAG_MLEADER_CONTENT_MISSING",
                    HealthSeverity.Error,
                    "Live semantic MLeader không còn MText content.",
                    element.Id));
            }
            else
            {
                InspectContent(element, content.Contents ?? string.Empty, "SEMANTIC_TAG_MLEADER_CONTENT_DRIFT", issues);
            }

            InspectTextHeight(document, element, tag.TextHeight, "SEMANTIC_TAG_MLEADER_HEIGHT_DRIFT", issues);
            InspectPoint(element, tag.TextLocation, GeneratedSemanticTagHealthService.LeaderTextXKey, GeneratedSemanticTagHealthService.LeaderTextYKey, GeneratedSemanticTagHealthService.LeaderTextZKey, "SEMANTIC_TAG_MLEADER_TEXT_POSITION_DRIFT", "Live semantic MLeader TextLocation không còn khớp drawing-local WCS metadata.", issues);
            InspectNormal(element, tag.Normal, "SEMANTIC_TAG_MLEADER_NORMAL_DRIFT", issues);

            ArrayList leaderIndexes;
            try { leaderIndexes = tag.GetLeaderIndexes(); }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_MLEADER_GEOMETRY_INVALID", HealthSeverity.Error, "Không đọc được leader cluster indexes: " + ex.Message, element.Id));
                return;
            }
            if (leaderIndexes == null || leaderIndexes.Count != 1 || !(leaderIndexes[0] is int leaderIndex))
            {
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_MLEADER_CLUSTER_COUNT_DRIFT", HealthSeverity.Warning, "Semantic MLeader source-ready contract yêu cầu đúng một leader cluster.", element.Id));
                return;
            }

            ArrayList lineIndexes;
            try { lineIndexes = tag.GetLeaderLineIndexes(leaderIndex); }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_MLEADER_GEOMETRY_INVALID", HealthSeverity.Error, "Không đọc được leader-line indexes: " + ex.Message, element.Id));
                return;
            }
            if (lineIndexes == null || lineIndexes.Count != 1 || !(lineIndexes[0] is int lineIndex))
            {
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_MLEADER_LINE_COUNT_DRIFT", HealthSeverity.Warning, "Semantic MLeader source-ready contract yêu cầu đúng một leader line.", element.Id));
                return;
            }

            try
            {
                InspectPoint(element, tag.GetFirstVertex(lineIndex), GeneratedSemanticTagHealthService.LeaderTargetXKey, GeneratedSemanticTagHealthService.LeaderTargetYKey, GeneratedSemanticTagHealthService.LeaderTargetZKey, "SEMANTIC_TAG_MLEADER_TARGET_DRIFT", "Live semantic MLeader first vertex không còn khớp stored target WCS.", issues);
                var last = tag.GetLastVertex(lineIndex);
                if (!PointsClose(last, tag.TextLocation))
                    issues.Add(new ModelHealthIssue("SEMANTIC_TAG_MLEADER_LAST_VERTEX_DRIFT", HealthSeverity.Warning, "Semantic MLeader last vertex không còn khớp TextLocation.", element.Id));
            }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_MLEADER_GEOMETRY_INVALID", HealthSeverity.Error, "Không đọc được semantic MLeader vertices: " + ex.Message, element.Id));
            }

            // Keep the literal writer-owned key visible in this runtime source guard.
            if (Property(element, "GeneratedSemanticTagLeaderTargetHandle").Length == 0)
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_MLEADER_TARGET_HANDLE_MISSING", HealthSeverity.Error, "GeneratedSemanticTagLeaderTargetHandle is required for semantic MLeader associativity.", element.Id));
        }

        private static void InspectContent(ProjectElement element, string actual, string code, ICollection<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.TextKey, out var raw)) return;
            var expected = EncodePlainMText(raw ?? string.Empty);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, "Live semantic tag content không còn khớp rendered text đã ghi nhận; refresh tag trước khi phát hành.", element.Id));
        }

        private static void InspectTextHeight(Document document, ProjectElement element, double actual, string code, ICollection<ModelHealthIssue> issues)
        {
            if (!TryFinite(element, GeneratedSemanticTagHealthService.TextHeightKey, out var heightM) || !(heightM > 0d)) return;
            var expected = CadUnitService.MetersToDrawingUnits(document, heightM);
            if (!Finite(expected) || !(expected > 0d)) return;
            var tolerance = Math.Max(1e-8d, Math.Abs(expected) * 1e-8d);
            if (!Finite(actual) || Math.Abs(actual - expected) > tolerance)
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, "Live semantic tag text height không còn khớp semantic tag metadata.", element.Id));
        }

        private static void InspectPoint(
            ProjectElement element,
            Point3d actual,
            string xKey,
            string yKey,
            string zKey,
            string code,
            string message,
            ICollection<ModelHealthIssue> issues)
        {
            if (!TryFinite(element, xKey, out var x) || !TryFinite(element, yKey, out var y) || !TryFinite(element, zKey, out var z)) return;
            var expected = new Point3d(x, y, z);
            if (!PointsClose(actual, expected))
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, message, element.Id));
        }

        private static bool PointsClose(Point3d first, Point3d second)
        {
            var dx = first.X - second.X;
            var dy = first.Y - second.Y;
            var dz = first.Z - second.Z;
            var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var scale = Math.Max(1d, Math.Max(Math.Abs(second.X), Math.Max(Math.Abs(second.Y), Math.Abs(second.Z))));
            var tolerance = Math.Max(1e-7d, scale * 1e-10d);
            return Finite(distance) && distance <= tolerance;
        }

        private static void InspectNormal(ProjectElement element, Vector3d normal, string code, ICollection<ModelHealthIssue> issues)
        {
            var length = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
            if (!Finite(length) || !(length > 0d) || Math.Abs(normal.X / length) > 1e-9d || Math.Abs(normal.Y / length) > 1e-9d || Math.Abs(normal.Z / length - 1d) > 1e-9d)
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, "Live semantic tag Normal không còn là +Z theo drawing-local WCS contract.", element.Id));
        }

        private static bool TryFinite(ProjectElement element, string key, out double value)
        {
            value = 0d;
            return element.Properties.TryGetValue(key, out var raw) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && Finite(value);
        }

        private static double AngleDistance(double first, double second)
        {
            var delta = (first - second) % (Math.PI * 2d);
            if (delta > Math.PI) delta -= Math.PI * 2d;
            if (delta < -Math.PI) delta += Math.PI * 2d;
            return Math.Abs(delta);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool IsRecoverableDiagnosticFailure(Exception exception)
        {
            return !(exception is OutOfMemoryException) && !(exception is StackOverflowException) && !(exception is AccessViolationException);
        }

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;

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
