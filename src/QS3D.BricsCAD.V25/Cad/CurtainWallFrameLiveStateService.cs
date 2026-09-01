using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CurtainWallFrameLiveStateService
    {
        private const string HandlesKey = "GeneratedCurtainFrameHandles";
        private const string FingerprintKey = "GeneratedCurtainFrameLiveFingerprint";

        public static int StampSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var stamped = new List<Tuple<ProjectElement, string>>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var source = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased || (!(source is Line) && !(source is Polyline))) continue;
                    var handle = source.Handle.ToString();
                    var matches = project.Elements
                        .Where(x => x.Category == ElementCategory.GlassWall &&
                                    x.Properties.TryGetValue(HandlesKey, out var raw) && !string.IsNullOrWhiteSpace(raw) &&
                                    x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0) continue;
                    if (matches.Count > 1) throw new InvalidOperationException("GlassWall source " + handle + " has ambiguous curtain-frame ownership.");
                    var element = matches[0];
                    stamped.Add(Tuple.Create(element, CurtainWallFrameLiveFingerprint.Compute(document, transaction, project, element, source)));
                }
                transaction.Commit();
            }
            foreach (var item in stamped)
                item.Item1.Properties[FingerprintKey] = item.Item2;
            if (stamped.Count > 0) project.Touch();
            return stamped.Count;
        }

        public static int TryStampSelected(Document document, ProjectState project, out string warning)
        {
            warning = string.Empty;
            try
            {
                return StampSelected(document, project);
            }
            catch (Exception)
            {
                // Frame/host geometry has already committed by the time command orchestration calls
                // this health stamp. Missing fingerprint is a diagnosable health warning, not a
                // reason to report an otherwise valid native geometry commit as failed. Keep the
                // user-visible warning stable so host/native exception detail is not disclosed.
                warning = "Live curtain fingerprint chưa được cập nhật; hãy chạy lại Curtain Frames 3D hoặc Health trước khi phát hành.";
                return 0;
            }
        }

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements.Where(x => x.Category == ElementCategory.GlassWall && x.Properties.TryGetValue(HandlesKey, out var raw) && !string.IsNullOrWhiteSpace(raw)))
                {
                    if (!element.Properties.TryGetValue(FingerprintKey, out var stored) || string.IsNullOrWhiteSpace(stored))
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_LIVE_FINGERPRINT_MISSING", HealthSeverity.Warning, "Thiếu live CAD fingerprint cho curtain frames; rebuild curtain frames để nâng metadata.", element.Id));
                        continue;
                    }
                    var ids = CadHandleService.Resolve(document, element.SourceHandles);
                    if (ids.Count != 1)
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_LIVE_SOURCE_INVALID", HealthSeverity.Error, "Curtain frame live check cần đúng một live GlassWall LINE hoặc POLYLINE source.", element.Id));
                        continue;
                    }
                    var source = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased || (!(source is Line) && !(source is Polyline)))
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_LIVE_SOURCE_INVALID", HealthSeverity.Error, "Curtain frame live source không còn là LINE/POLYLINE hợp lệ.", element.Id));
                        continue;
                    }
                    try
                    {
                        var current = CurtainWallFrameLiveFingerprint.Compute(document, transaction, project, element, source);
                        if (!string.Equals(current, stored.Trim(), StringComparison.OrdinalIgnoreCase))
                            issues.Add(new ModelHealthIssue("CURTAIN_FRAME_LIVE_GEOMETRY_STALE", HealthSeverity.Warning, "GlassWall/opening CAD geometry đã thay đổi trực tiếp sau lần dựng curtain frames; rebuild curtain frames trước khi phát hành bản vẽ.", element.Id));
                    }
                    catch (Exception ex)
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_LIVE_GEOMETRY_INVALID", HealthSeverity.Warning, "Không thể kiểm tra live curtain geometry: " + ex.Message, element.Id));
                    }
                }
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }
    }
}
