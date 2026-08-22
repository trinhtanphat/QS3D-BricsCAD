using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using QS3D.Core.Units;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RoomBoundaryCommands
    {
        [CommandMethod("QS3DROOMAUTO", CommandFlags.UsePickSet)]
        public void DiscoverRooms()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                ProjectState? previewProject = null;
                string? expectedProjectId = null;
                if (ProjectContextCoordinator.TryGetReadOnly(document, out var existingPreview))
                {
                    previewProject = existingPreview;
                    expectedProjectId = existingPreview.ProjectId;
                }

                var tolerance = previewProject == null ? 0.005d : MetadataNumber(previewProject, "RoomBoundaryToleranceM", 0.005d, minimumExclusive: 0d);
                var arcSagitta = previewProject == null ? 0.002d : MetadataNumber(previewProject, "RoomBoundaryArcSagittaM", 0.002d, minimumExclusive: 0d);
                var splineChord = previewProject == null ? 0.02d : MetadataNumber(previewProject, "RoomBoundarySplineChordM", 0.02d, minimumExclusive: 0d);
                var segments = RoomBoundarySegmentReader.ReadCurrentSelection(document, arcSagitta, tolerance, splineChord);
                LengthUnit? selectionUnit = segments.Count == 0 ? (LengthUnit?)null : CadUnitService.GetLengthUnit(document);
                var minimumArea = previewProject == null ? 0.5d : MetadataNonNegative(previewProject, "RoomBoundaryMinimumAreaM2", 0.5d);
                var diagnostic = new RoomBoundaryDiagnosticService().Analyze(segments, tolerance, minimumArea);
                var boundaries = diagnostic.AcceptedBoundaries;
                if (boundaries.Count == 0)
                {
                    var detail = FormatRoomBoundaryDiagnostic(diagnostic);
                    document.Editor.WriteMessage("\nQS3DROOMAUTO: " + detail);
                    PaletteCoordinator.SetStatus("Room Auto: " + detail);
                    return;
                }
                if (!selectionUnit.HasValue)
                    throw new InvalidOperationException("Room boundary unit context không còn hợp lệ. Hãy chạy lại lệnh.");

                ProjectState project;
                if (expectedProjectId != null)
                {
                    project = ExistingProjectMutationContext.Require(document, "Room Auto");
                    if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("QS3D project đã thay đổi trong lúc đọc Room boundary. Hãy chạy lại lệnh.");
                }
                else
                {
                    // The preview was computed without a QS3D project. If one becomes
                    // visible before commit, fail closed rather than applying default
                    // preview settings to a newly appeared canonical project.
                    if (ProjectContextCoordinator.TryGetReadOnly(document, out _))
                        throw new InvalidOperationException("QS3D project đã xuất hiện trong lúc đọc Room boundary. Hãy chạy lại lệnh để dùng đúng project settings.");

                    // Creation-capable only after usable CAD input produced at least one closed face.
                    // Cancel/empty/no-face paths above must never bootstrap a blank project.
                    project = ProjectContextCoordinator.GetOrCreate(document);
                }

                EnsureBoundaryCommitFreshness(document, project, selectionUnit.Value, tolerance, arcSagitta, splineChord, minimumArea);

                var signatureCounts = boundaries
                    .Select(x => AutoRoomLifecycle.NormalizeSourceHandles(x.SourceIds))
                    .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    var family = ResolveRoomFamily(project);
                    var audit = AuditTrail.ForProject(project);
                    var activeRoomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var selectedSourceHandles = new HashSet<string>(segments.Where(x => !string.IsNullOrWhiteSpace(x.SourceId)).Select(x => x.SourceId.Trim()), StringComparer.OrdinalIgnoreCase);
                    var created = 0;
                    var updated = 0;
                    var refreshedFinishes = 0;

                    foreach (var boundary in boundaries)
                    {
                        var sourceSignature = AutoRoomLifecycle.NormalizeSourceHandles(boundary.SourceIds);
                        var expectedId = "ROOMAUTO-" + StableToken(IdentitySeed(project.ActiveFloorId, project.ActiveZoneId, boundary.Key));
                        var legacyId = "ROOMAUTO-" + StableToken(boundary.Key);
                        var element = project.FindElement(expectedId);
                        var resolvedById = element != null;

                        if (element == null && !string.Equals(legacyId, expectedId, StringComparison.OrdinalIgnoreCase))
                        {
                            var legacy = project.FindElement(legacyId);
                            if (legacy != null &&
                                string.Equals(legacy.FloorId, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(legacy.ZoneId, project.ActiveZoneId, StringComparison.OrdinalIgnoreCase))
                            {
                                element = legacy;
                                resolvedById = true;
                            }
                        }

                        if (element == null && sourceSignature.Length > 0 && signatureCounts.TryGetValue(sourceSignature, out var signatureCount) && signatureCount == 1)
                            element = AutoRoomLifecycle.FindBySourceSignature(project, sourceSignature, project.ActiveFloorId, project.ActiveZoneId);

                        var isNew = element == null;
                        if (element == null)
                        {
                            element = new ProjectElement(expectedId, ElementCategory.Room, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                            project.Elements.Add(element);
                            created++;
                        }
                        else
                        {
                            if (element.Category != ElementCategory.Room || !AutoRoomLifecycle.IsAutoRoom(element))
                                throw new InvalidOperationException("Boundary id/provenance collision with non-auto Room element: " + element.Id);
                            if (resolvedById && element.Properties.TryGetValue("BoundaryKey", out var existingBoundaryKey) &&
                                !string.IsNullOrWhiteSpace(existingBoundaryKey) &&
                                !string.Equals(existingBoundaryKey, boundary.Key, StringComparison.Ordinal))
                                throw new InvalidOperationException("Auto-room id hash collision detected: " + element.Id);
                            updated++;
                        }

                        if (!activeRoomIds.Add(element.Id))
                            throw new InvalidOperationException("Multiple discovered boundaries resolved to the same auto Room: " + element.Id);

                        element.Category = ElementCategory.Room;
                        element.FloorId = project.ActiveFloorId;
                        element.ZoneId = project.ActiveZoneId;
                        element.DrawingFingerprint = project.DrawingFingerprint;
                        element.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
                        AutoRoomLifecycle.MarkActive(element, sourceSignature);
                        AutoRoomLifecycle.SyncFamilyDefaults(project, element, family);
                        element.Properties["BoundaryKey"] = boundary.Key;
                        element.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = sourceSignature;
                        element.Properties["BoundaryVertexCount"] = boundary.Vertices.Count.ToString(CultureInfo.InvariantCulture);
                        element.Properties["BoundaryArcSagittaM"] = arcSagitta.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["BoundarySplineChordM"] = splineChord.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["AreaM2"] = boundary.Area.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["PerimeterM"] = boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture);
                        element.MarkDirty(ElementDirtyFlags.All);
                        refreshedFinishes += SemanticCaptureService.SyncExistingRoomFinishes(project, element);
                        audit.Record(isNew ? "RoomBoundaryCreate" : "RoomBoundaryUpdate", element.Id,
                            "area=" + boundary.Area.ToString("R", CultureInfo.InvariantCulture) +
                            ";perimeter=" + boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture) +
                            ";sources=" + boundary.SourceIds.Count.ToString(CultureInfo.InvariantCulture) +
                            ";arcSagitta=" + arcSagitta.ToString("R", CultureInfo.InvariantCulture) +
                            ";splineChord=" + splineChord.ToString("R", CultureInfo.InvariantCulture));
                    }

                    var staleRooms = AutoRoomLifecycle.MarkStaleForSelection(project, activeRoomIds, selectedSourceHandles, project.ActiveFloorId, project.ActiveZoneId, DateTime.UtcNow);
                    foreach (var stale in staleRooms)
                        audit.Record("RoomBoundaryStale", stale.Id, "topology changed within the selected boundary source set");

                    var regenerationTargets = new HashSet<string>(activeRoomIds, StringComparer.OrdinalIgnoreCase);
                    foreach (var stale in staleRooms)
                        regenerationTargets.Add(stale.Id);

                    var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                        .RegenerateDirtySubset(project, regenerationTargets);
                    project.Touch();
                    PaletteCoordinator.RefreshProject();
                    var message = "Room Auto: " + boundaries.Count + " face • mới " + created + " • cập nhật " + updated + " • stale " + staleRooms.Count + " • sync finish " + refreshedFinishes + " • regenerate " + regenerated + ".";
                    PaletteCoordinator.SetStatus(message);
                    document.Editor.WriteMessage("\nQS3D " + message);
                }
                catch (System.Exception operationError)
                {
                    try
                    {
                        rollback.Restore(project);
                        PaletteCoordinator.RefreshProject();
                    }
                    catch (System.Exception restoreError)
                    {
                        throw new InvalidOperationException("QS3DROOMAUTO failed and project rollback also failed.", new AggregateException(operationError, restoreError));
                    }
                    throw;
                }
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DROOMAUTO error: " + ex.Message);
                PaletteCoordinator.SetStatus("QS3DROOMAUTO lỗi: " + ex.Message);
            }
        }

        private static void EnsureBoundaryCommitFreshness(
            Document document,
            ProjectState project,
            LengthUnit selectionUnit,
            double tolerance,
            double arcSagitta,
            double splineChord,
            double minimumArea)
        {
            if (CadUnitService.GetLengthUnit(document) != selectionUnit)
                throw new InvalidOperationException("Drawing unit policy đã thay đổi trong lúc đọc Room boundary. Hãy chạy lại lệnh.");

            if (MetadataNumber(project, "RoomBoundaryToleranceM", 0.005d, minimumExclusive: 0d) != tolerance ||
                MetadataNumber(project, "RoomBoundaryArcSagittaM", 0.002d, minimumExclusive: 0d) != arcSagitta ||
                MetadataNumber(project, "RoomBoundarySplineChordM", 0.02d, minimumExclusive: 0d) != splineChord ||
                MetadataNonNegative(project, "RoomBoundaryMinimumAreaM2", 0.5d) != minimumArea)
                throw new InvalidOperationException("Room boundary settings đã thay đổi trong lúc đọc selection. Hãy chạy lại lệnh.");
        }

        private static string FormatRoomBoundaryDiagnostic(RoomBoundaryDiagnosticReport diagnostic)
        {
            switch (diagnostic.Reason)
            {
                case RoomBoundaryDiagnosticReason.NoInput:
                    return "không có boundary segment hợp lệ; chọn LINE, POLYLINE, ARC hoặc SPLINE plan-view tạo biên phòng.";
                case RoomBoundaryDiagnosticReason.InsufficientSegments:
                    return "chỉ đọc được " + diagnostic.InputSegmentCount + " boundary segment hợp lệ; cần ít nhất 3 segment để tạo face kín.";
                case RoomBoundaryDiagnosticReason.NoClosedFace:
                    return "đã đọc " + diagnostic.InputSegmentCount + " segment từ " + diagnostic.UniqueSourceCount + " nguồn nhưng không hình thành face kín; kiểm tra gap, giao cắt, đường hở và tính đồng phẳng của boundary.";
                case RoomBoundaryDiagnosticReason.BelowMinimumArea:
                    return "phát hiện " + diagnostic.CandidateBoundaryCount + " face topology nhưng tất cả đều không vượt ngưỡng RoomBoundaryMinimumAreaM2=" + diagnostic.MinimumArea.ToString("0.###", CultureInfo.InvariantCulture) + " m²; face lớn nhất=" + diagnostic.MaxCandidateArea.ToString("0.###", CultureInfo.InvariantCulture) + " m².";
                default:
                    return "không phát hiện Room boundary được chấp nhận.";
            }
        }

        private static ProjectFamily ResolveRoomFamily(ProjectState project)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == ElementCategory.Room) return active;
            }
            var existing = project.Families.FirstOrDefault(x => x.Category == ElementCategory.Room);
            if (existing != null) return existing;
            var created = new ProjectFamily("room-auto-boundary", "Phòng Auto Boundary", ElementCategory.Room);
            created.Properties["HeightM"] = "3.6";
            project.Families.Add(created);
            return created;
        }

        private static double MetadataNumber(ProjectState project, string key, double fallback, double minimumExclusive)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= minimumExclusive)
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static double MetadataNonNegative(ProjectState project, string key, double fallback)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static string IdentitySeed(string floorId, string zoneId, string boundaryKey)
            => (floorId ?? string.Empty).Trim().ToUpperInvariant() + "|" +
               (zoneId ?? string.Empty).Trim().ToUpperInvariant() + "|" +
               (boundaryKey ?? string.Empty);

        private static string StableToken(string value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty);
            }
        }
    }
}
