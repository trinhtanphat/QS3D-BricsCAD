using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class PhysicalOpeningCutLiveStateService
    {
        private const string FingerprintKey = "PhysicalOpeningCutLiveFingerprint";
        private const string LiveModeKey = "PhysicalOpeningCutLiveMode";
        private const string CurvedMode = "CurvedCenterlineFootprint";

        private sealed class PendingStamp
        {
            public ProjectElement Host { get; set; } = null!;
            public string Fingerprint { get; set; } = string.Empty;
            public string Mode { get; set; } = string.Empty;
        }

        public static int StampStraight(Document document, ProjectState project, IReadOnlyCollection<string>? openingIds)
        {
            var requested = openingIds == null
                ? null
                : new HashSet<string>(openingIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
            return Stamp(document, project, requested, curved: false);
        }

        public static int StampCurved(Document document, ProjectState project) => Stamp(document, project, null, curved: true);

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var host in project.Elements
                    .Where(x => HasValue(x, "PhysicalOpeningCutSolidHandle") || HasValue(x, "PhysicalOpeningCutFingerprint"))
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var hasCutSolid = host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle", out var cutSolid) && !string.IsNullOrWhiteSpace(cutSolid);
                    var hasCutFingerprint = host.Properties.TryGetValue("PhysicalOpeningCutFingerprint", out var cutFingerprint) && !string.IsNullOrWhiteSpace(cutFingerprint);
                    if (hasCutSolid != hasCutFingerprint)
                    {
                        issues.Add(new ModelHealthIssue(
                            "PHYSICAL_OPENING_CUT_STATE_INCOMPLETE",
                            HealthSeverity.Error,
                            "Physical opening cut metadata thiếu handle hoặc fingerprint; Build 3D + Cut lại host trước khi phát hành.",
                            host.Id));
                        continue;
                    }

                    if (!host.Properties.TryGetValue(FingerprintKey, out var stored) || string.IsNullOrWhiteSpace(stored))
                    {
                        issues.Add(new ModelHealthIssue(
                            "PHYSICAL_OPENING_CUT_LIVE_FINGERPRINT_MISSING",
                            HealthSeverity.Warning,
                            "Host đã có physical opening cut nhưng thiếu live-input fingerprint; Build 3D + Cut lại để nâng metadata trước khi phát hành.",
                            host.Id));
                        continue;
                    }

                    if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var generated) || string.IsNullOrWhiteSpace(generated) ||
                        !string.Equals(generated.Trim(), cutSolid!.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new ModelHealthIssue(
                            "PHYSICAL_OPENING_CUT_SOLID_MISMATCH",
                            HealthSeverity.Error,
                            "Physical opening cut metadata không còn trỏ tới generated host solid hiện tại; Build 3D + Cut lại host.",
                            host.Id));
                        continue;
                    }

                    try
                    {
                        RequireOwnedGeneratedSolid(document, transaction, project, host, generated, "inspect physical opening live state");

                        var sourceIds = CadHandleService.Resolve(document, host.SourceHandles);
                        if (sourceIds.Count != 1)
                            throw new InvalidOperationException("Host cần đúng một live CAD source để kiểm tra physical opening cut.");
                        var source = transaction.GetObject(sourceIds[0], OpenMode.ForRead, false) as Entity;
                        if (source == null || source.IsErased || (!(source is Line) && !(source is Polyline)))
                            throw new InvalidOperationException("Host source không còn là LINE/POLYLINE hợp lệ.");

                        var sourceIsCurved = source is Polyline polyline && PhysicalOpeningCutLiveFingerprint.HasBulge(polyline);
                        if (!PhysicalOpeningCutTargetState.TryRead(host, out var cutOpeningIds))
                        {
                            issues.Add(new ModelHealthIssue(
                                "PHYSICAL_OPENING_CUT_TARGET_STATE_MISSING",
                                HealthSeverity.Warning,
                                "Host physical-cut thiếu tập opening chính xác đã thực sự được khoét; Build 3D + Cut lại để nâng metadata trước khi phát hành.",
                                host.Id));
                            continue;
                        }
                        var fingerprintOpenings = PhysicalOpeningCutTargetState.Resolve(project, host, cutOpeningIds);

                        var current = PhysicalOpeningCutLiveFingerprint.Compute(document, transaction, project, host, source, fingerprintOpenings);
                        if (!string.Equals(current, stored.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            issues.Add(new ModelHealthIssue(
                                "PHYSICAL_OPENING_CUT_LIVE_STALE",
                                HealthSeverity.Warning,
                                "Host/opening CAD geometry hoặc thông số của tập physical cut đã thay đổi sau lần khoét; Build 3D + Cut lại trước khi phát hành.",
                                host.Id));
                            continue;
                        }

                        var expectedMode = sourceIsCurved ? "CurvedInputV1" : "StraightInputV1";
                        if (!host.Properties.TryGetValue(LiveModeKey, out var liveMode) || !string.Equals(liveMode?.Trim(), expectedMode, StringComparison.OrdinalIgnoreCase))
                        {
                            issues.Add(new ModelHealthIssue(
                                "PHYSICAL_OPENING_CUT_LIVE_MODE_MISMATCH",
                                HealthSeverity.Warning,
                                "Physical opening live-input mode không khớp host source hiện tại; Build 3D + Cut lại host.",
                                host.Id));
                        }
                    }
                    catch (Exception ex)
                    {
                        issues.Add(new ModelHealthIssue(
                            "PHYSICAL_OPENING_CUT_LIVE_INVALID",
                            HealthSeverity.Warning,
                            "Không thể kiểm tra live physical opening cut: " + ex.Message,
                            host.Id));
                    }
                }
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }

        private static int Stamp(Document document, ProjectState project, HashSet<string>? requestedOpeningIds, bool curved)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var groups = project.Elements
                .Where(IsOpening)
                .Where(x => requestedOpeningIds == null || requestedOpeningIds.Contains(x.Id))
                .Where(x => x.Properties.TryGetValue("HostWallId", out var hostId) && !string.IsNullOrWhiteSpace(hostId))
                .GroupBy(x => x.Properties["HostWallId"], StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (groups.Count == 0) return 0;

            var pending = new List<PendingStamp>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var group in groups)
                {
                    var host = project.FindElement(group.Key);
                    if (host == null) continue;
                    var hasCutSolid = host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle", out var cutSolid) && !string.IsNullOrWhiteSpace(cutSolid);
                    var hasCutFingerprint = host.Properties.TryGetValue("PhysicalOpeningCutFingerprint", out var cutFingerprint) && !string.IsNullOrWhiteSpace(cutFingerprint);
                    if (hasCutSolid != hasCutFingerprint)
                        throw new InvalidOperationException("Host " + host.Id + " có physical opening metadata không đầy đủ; không stamp live state.");
                    if (!hasCutSolid) continue;
                    if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var generated) || string.IsNullOrWhiteSpace(generated)) continue;
                    if (!string.Equals(generated.Trim(), cutSolid!.Trim(), StringComparison.OrdinalIgnoreCase)) continue;

                    RequireOwnedGeneratedSolid(document, transaction, project, host, generated, "stamp physical opening live state");

                    var sourceIds = CadHandleService.Resolve(document, host.SourceHandles);
                    if (sourceIds.Count != 1) continue;
                    var source = transaction.GetObject(sourceIds[0], OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased || (!(source is Line) && !(source is Polyline))) continue;

                    var sourceIsCurved = source is Polyline polyline && PhysicalOpeningCutLiveFingerprint.HasBulge(polyline);
                    if (sourceIsCurved != curved) continue;
                    if (curved)
                    {
                        if (!host.Properties.TryGetValue("PhysicalOpeningCutMode", out var cutMode) ||
                            !string.Equals(cutMode?.Trim(), CurvedMode, StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    else if (host.Properties.TryGetValue("PhysicalOpeningCutMode", out var cutMode) &&
                             string.Equals(cutMode?.Trim(), CurvedMode, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!PhysicalOpeningCutTargetState.TryRead(host, out var cutOpeningIds)) continue;
                    var fingerprintOpenings = PhysicalOpeningCutTargetState.Resolve(project, host, cutOpeningIds);
                    var fingerprint = PhysicalOpeningCutLiveFingerprint.Compute(document, transaction, project, host, source, fingerprintOpenings);
                    pending.Add(new PendingStamp
                    {
                        Host = host,
                        Fingerprint = fingerprint,
                        Mode = curved ? "CurvedInputV1" : "StraightInputV1"
                    });
                }
                transaction.Commit();
            }

            var changed = 0;
            foreach (var item in pending)
            {
                var sameFingerprint = item.Host.Properties.TryGetValue(FingerprintKey, out var existingFingerprint) &&
                    string.Equals(existingFingerprint?.Trim(), item.Fingerprint, StringComparison.OrdinalIgnoreCase);
                var sameMode = item.Host.Properties.TryGetValue(LiveModeKey, out var existingMode) &&
                    string.Equals(existingMode?.Trim(), item.Mode, StringComparison.OrdinalIgnoreCase);
                if (sameFingerprint && sameMode) continue;
                item.Host.Properties[FingerprintKey] = item.Fingerprint;
                item.Host.Properties[LiveModeKey] = item.Mode;
                changed++;
            }
            if (changed > 0) project.Touch();
            return changed;
        }

        private static void RequireOwnedGeneratedSolid(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement host,
            string generatedHandle,
            string operation)
        {
            var ids = CadHandleService.Resolve(document, new[] { generatedHandle });
            if (ids.Count != 1)
                throw new InvalidOperationException("Generated host solid " + generatedHandle.Trim() + " không resolve duy nhất cho " + host.Id + ".");
            var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
            if (solid == null || solid.IsErased)
                throw new InvalidOperationException("Generated host solid không còn live cho " + host.Id + ".");
            GeneratedGeometryService.RequireMatchingOwnership(solid, project, host, operation + " " + generatedHandle.Trim());
        }

        private static bool HasValue(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);

        private static bool IsOpening(ProjectElement element) =>
            element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening;
    }
}
