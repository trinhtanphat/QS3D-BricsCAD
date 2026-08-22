using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BomReleaseGuardSmoke
    {
        public static void Run()
        {
            BasicBomGuard();
            NonCanonicalPropertyKeyBlocksRelease();
            NonCanonicalQuantityKeyBlocksRelease();
            MalformedDiagnosticKeysBlockReleaseAndAreRedacted();
            MalformedQuantityKeysSuppressValueDiagnostics();
            CanonicalUnicodeDiagnosticKeysRemainValid();
            RoomFinishProvenanceReachesReleaseGuard();
            ProvenanceConflictDoesNotCrashReleaseGuard();
            NullSemanticEntryBlocksReleaseWithoutCrashing();
            ExceptionDetailIsRedactedFromReleaseIssues();
        }

        private static void BasicBomGuard()
        {
            var project = new ProjectState("bom", "BOM release guard");
            project.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));
            var element = new ProjectElement("beam-1", ElementCategory.Beam, "beam", string.Empty, string.Empty);
            element.SourceHandles.Add("1A");
            element.SetQuantity("NetConcreteM3", 1.25d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            Empty(BomReleaseGuardService.Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

            element.MarkDirty(ElementDirtyFlags.Quantity);
            Has(BomReleaseGuardService.Inspect(project), "BOM_QUANTITY_DIRTY");
            element.MarkClean(ElementDirtyFlags.All);

            element.Quantities["NetConcreteM3"] = double.NaN;
            Has(BomReleaseGuardService.Inspect(project), "BOM_QUANTITY_NONFINITE");
            element.Quantities["NetConcreteM3"] = 1.25d;

            element.SourceHandles.Clear();
            Has(BomReleaseGuardService.Inspect(project), "BOM_TRACEABILITY_MISSING");
            element.Properties["GeneratedSolidHandle"] = "2B";
            element.Properties["PhysicalOpeningCutSolidHandle"] = "2B";
            Equal(1, Count(BomReleaseGuardService.Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase)), "BOM_GENERATED_HANDLE_MISSING"));
            var live = new HashSet<string>(new[] { "2B" }, StringComparer.OrdinalIgnoreCase);
            if (BomReleaseGuardService.Inspect(project, live).Any(x => x.Code == "BOM_GENERATED_HANDLE_MISSING"))
                throw new Exception("Live generated Handle must satisfy the BOM release guard.");

            var caseSensitiveLive = new HashSet<string>(new[] { "2b" }, StringComparer.Ordinal);
            if (BomReleaseGuardService.Inspect(project, caseSensitiveLive).Any(x => x.Code == "BOM_GENERATED_HANDLE_MISSING"))
                throw new Exception("BOM generated Handle liveness must be case-insensitive regardless of the caller set comparer.");

            var paddedCaseSensitiveLive = new HashSet<string>(new[] { " 2b ", "   " }, StringComparer.Ordinal);
            if (BomReleaseGuardService.Inspect(project, paddedCaseSensitiveLive).Any(x => x.Code == "BOM_GENERATED_HANDLE_MISSING"))
                throw new Exception("BOM generated Handle liveness must trim surrounding whitespace and ignore blank caller entries.");

            element.Properties["GeneratedFuturePanelHandles"] = "3C;3D;3d";
            var partialFuture = new HashSet<string>(new[] { "2B", "3C" }, StringComparer.OrdinalIgnoreCase);
            Has(BomReleaseGuardService.Inspect(project, partialFuture), "BOM_GENERATED_HANDLE_MISSING");
            var allFuture = new HashSet<string>(new[] { "2B", "3C", "3D" }, StringComparer.OrdinalIgnoreCase);
            if (BomReleaseGuardService.Inspect(project, allFuture).Any(x => x.Code == "BOM_GENERATED_HANDLE_MISSING"))
                throw new Exception("Future Generated*Handles owner slot must use the shared BOM liveness registry without a hard-coded family update.");
        }

        private static void NonCanonicalPropertyKeyBlocksRelease()
        {
            var project = ProjectWithBeam("bom-property-key", "beam-property-key");
            var element = project.Elements[0];
            element.Properties[" MaterialName "] = "C30";

            var issues = BomReleaseGuardService.Inspect(project);
            Has(issues, "BOM_PROPERTY_KEY_INVALID");
            if (!issues.Any(x => x.Code == "BOM_PROPERTY_KEY_INVALID" && x.Severity == HealthSeverity.Error && x.ElementId == element.Id))
                throw new Exception("Non-canonical property key must be an Error-level BOM release blocker for its owning element.");
        }

        private static void NonCanonicalQuantityKeyBlocksRelease()
        {
            var project = ProjectWithBeam("bom-key", "beam-key", addCanonicalQuantity: false);
            var element = project.Elements[0];
            element.Quantities[" NetConcreteM3 "] = 1.25d;

            var issues = BomReleaseGuardService.Inspect(project);
            Has(issues, "BOM_QUANTITY_KEY_INVALID");
            if (!issues.Any(x => x.Code == "BOM_QUANTITY_KEY_INVALID" && x.Severity == HealthSeverity.Error && x.ElementId == element.Id))
                throw new Exception("Non-canonical quantity key must be an Error-level BOM release blocker for its owning element.");
        }

        private static void MalformedDiagnosticKeysBlockReleaseAndAreRedacted()
        {
            var invalidKeys = new[]
            {
                "Bad\0Key",
                "Bad\u0001Key",
                "Bad\tKey",
                "Bad\nKey",
                "Bad\u001fKey",
                "Bad\u007fKey",
                "Bad\u0085Key",
                "Bad\u009fKey",
                "Bad" + ((char)0xfffe) + "Key",
                "Bad" + ((char)0xffff) + "Key",
                "Bad" + new string((char)0xd800, 1) + "Key",
                "Bad" + new string((char)0xdc00, 1) + "Key"
            };

            foreach (var invalidKey in invalidKeys)
            {
                var propertyProject = ProjectWithBeam("bom-bad-property", "beam-bad-property");
                var propertyElement = propertyProject.Elements[0];
                propertyElement.Properties[invalidKey] = "value";
                var propertyIssues = BomReleaseGuardService.Inspect(propertyProject);
                Equal(1, Count(propertyIssues, "BOM_PROPERTY_KEY_INVALID"));
                AssertKeyNotReflected(propertyIssues, invalidKey);

                var quantityProject = ProjectWithBeam("bom-bad-quantity", "beam-bad-quantity", addCanonicalQuantity: false);
                var quantityElement = quantityProject.Elements[0];
                quantityElement.Quantities[invalidKey] = double.NaN;
                var quantityIssues = BomReleaseGuardService.Inspect(quantityProject);
                Equal(1, Count(quantityIssues, "BOM_QUANTITY_KEY_INVALID"));
                Equal(0, Count(quantityIssues, "BOM_QUANTITY_NONFINITE"));
                AssertKeyNotReflected(quantityIssues, invalidKey);
            }
        }

        private static void MalformedQuantityKeysSuppressValueDiagnostics()
        {
            var malformedCases = new[]
            {
                (Key: " BadFinite ", Value: 1.25d),
                (Key: "Bad\nNaN", Value: double.NaN),
                (Key: "Bad\u0085PositiveInfinity", Value: double.PositiveInfinity),
                (Key: "Bad" + ((char)0xffff) + "NegativeInfinity", Value: double.NegativeInfinity)
            };

            foreach (var malformedCase in malformedCases)
            {
                var project = ProjectWithBeam("bom-precedence", "beam-precedence", addCanonicalQuantity: false);
                project.Elements[0].Quantities[malformedCase.Key] = malformedCase.Value;

                var issues = BomReleaseGuardService.Inspect(project);
                Equal(1, Count(issues, "BOM_QUANTITY_KEY_INVALID"));
                Equal(0, Count(issues, "BOM_QUANTITY_NONFINITE"));
                AssertKeyNotReflected(issues, malformedCase.Key);
            }

            var canonical = ProjectWithBeam("bom-canonical-nonfinite", "beam-canonical-nonfinite", addCanonicalQuantity: false);
            canonical.Elements[0].Quantities["NetConcreteM3"] = double.PositiveInfinity;
            var canonicalIssues = BomReleaseGuardService.Inspect(canonical);
            Equal(0, Count(canonicalIssues, "BOM_QUANTITY_KEY_INVALID"));
            Equal(1, Count(canonicalIssues, "BOM_QUANTITY_NONFINITE"));
        }

        private static void CanonicalUnicodeDiagnosticKeysRemainValid()
        {
            var project = ProjectWithBeam("bom-unicode-key", "beam-unicode-key", addCanonicalQuantity: false);
            var element = project.Elements[0];
            element.Properties["VậtLiệu_😀"] = "C30";
            element.Quantities["KhốiLượng_😀"] = 1.25d;

            var issues = BomReleaseGuardService.Inspect(project);
            Equal(0, Count(issues, "BOM_PROPERTY_KEY_INVALID"));
            Equal(0, Count(issues, "BOM_QUANTITY_KEY_INVALID"));
            Equal(0, Count(issues, "BOM_QUANTITY_NONFINITE"));
        }

        private static ProjectState ProjectWithBeam(string projectId, string elementId, bool addCanonicalQuantity = true)
        {
            var project = new ProjectState(projectId, "BOM key guard");
            project.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));
            var element = new ProjectElement(elementId, ElementCategory.Beam, "beam", string.Empty, string.Empty);
            element.SourceHandles.Add("1A");
            if (addCanonicalQuantity) element.SetQuantity("NetConcreteM3", 1.25d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return project;
        }

        private static void AssertKeyNotReflected(IReadOnlyList<ModelHealthIssue> issues, string invalidKey)
        {
            foreach (var issue in issues)
                if (issue.Message.IndexOf(invalidKey, StringComparison.Ordinal) >= 0)
                    throw new Exception("Malformed BOM diagnostic key must never be reflected in an issue message.");
        }

        private static void RoomFinishProvenanceReachesReleaseGuard()
        {
            var project = new ProjectState("finish-release", "Finish release guard");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 1"));
            project.Families.Add(new ProjectFamily("finish", "Sơn", ElementCategory.WallFinish));
            var orphan = new ProjectElement("finish-orphan", ElementCategory.WallFinish, "finish", "f1", "z1");
            orphan.Properties[AutoRoomLifecycle.RoomSourceIdKey] = "missing-room";
            orphan.SetQuantity("NetFinishAreaM2", 12d);
            orphan.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(orphan);

            var issues = BomReleaseGuardService.Inspect(project);
            Has(issues, "ORPHAN_ROOM_FINISH");
            Has(issues, "BOM_EMPTY");
        }

        private static void ProvenanceConflictDoesNotCrashReleaseGuard()
        {
            var project = new ProjectState("finish-conflict-release", "Finish conflict release guard");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 1"));
            project.Families.Add(new ProjectFamily("room", "Phòng", ElementCategory.Room));
            project.Families.Add(new ProjectFamily("finish", "Sơn", ElementCategory.WallFinish));
            project.Elements.Add(new ProjectElement("room-a", ElementCategory.Room, "room", "f1", "z1"));
            project.Elements.Add(new ProjectElement("room-b", ElementCategory.Room, "room", "f1", "z1"));
            var finish = new ProjectElement("finish-conflict", ElementCategory.WallFinish, "finish", "f1", "z1");
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = "room-a";
            finish.Properties["ParentRoomId"] = "room-b";
            finish.SetQuantity("NetFinishAreaM2", 10d);
            finish.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(finish);

            var issues = BomReleaseGuardService.Inspect(project);
            Has(issues, "ROOM_PROVENANCE_CONFLICT");
            Has(issues, "BOM_EXCLUSION_FAILED");
            Has(issues, "BOM_REPORT_FAILED");
            MessageEquals(issues, "BOM_EXCLUSION_FAILED", "Không thể quyết định an toàn cấu kiện có được đưa vào BQ hay không.");
            MessageEquals(issues, "BOM_REPORT_FAILED", "Không thể dựng bảng khối lượng an toàn.");
        }

        private static void NullSemanticEntryBlocksReleaseWithoutCrashing()
        {
            var project = new ProjectState("bom-null", "BOM null guard");
            project.Elements.Add(null!);
            var issues = BomReleaseGuardService.Inspect(project);
            Has(issues, "BOM_NULL_ELEMENT");
            Has(issues, "BOM_ROOM_FINISH_HEALTH_FAILED");
            Has(issues, "BOM_CURTAIN_PANEL_HEALTH_FAILED");
            Equal(1, Count(issues, "BOM_ROOM_FINISH_HEALTH_FAILED"));
            Equal(1, Count(issues, "BOM_CURTAIN_PANEL_HEALTH_FAILED"));
            MessageEquals(issues, "BOM_ROOM_FINISH_HEALTH_FAILED", "Không thể chạy chẩn đoán Room Finish an toàn; phát hành BQ bị chặn.");
            MessageEquals(issues, "BOM_CURTAIN_PANEL_HEALTH_FAILED", "Không thể chạy chẩn đoán Curtain Panel an toàn; phát hành BQ bị chặn.");
            if (!issues.Any(x => x.Code == "BOM_NULL_ELEMENT" && x.Severity == HealthSeverity.Error) ||
                !issues.Any(x => x.Code == "BOM_ROOM_FINISH_HEALTH_FAILED" && x.Severity == HealthSeverity.Error) ||
                !issues.Any(x => x.Code == "BOM_CURTAIN_PANEL_HEALTH_FAILED" && x.Severity == HealthSeverity.Error))
                throw new Exception("Malformed nested health-provider state must remain Error-level release blockers.");
        }

        private static void ExceptionDetailIsRedactedFromReleaseIssues()
        {
            var project = new ProjectState("bom-redaction", "BOM redaction");
            project.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));

            var first = new ProjectElement("duplicate-beam", ElementCategory.Beam, "beam", string.Empty, string.Empty);
            first.SetQuantity("NetConcreteM3", 1d);
            first.MarkClean(ElementDirtyFlags.All);
            var second = new ProjectElement("duplicate-beam", ElementCategory.Beam, "beam", string.Empty, string.Empty);
            second.SetQuantity("NetConcreteM3", 2d);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);

            var issues = BomReleaseGuardService.Inspect(project);
            Has(issues, "BOM_TRACEABILITY_FAILED");
            MessageEquals(issues, "BOM_TRACEABILITY_FAILED", "Không thể dựng provenance Handle an toàn cho cấu kiện.");
        }

        private static void Empty(IReadOnlyList<ModelHealthIssue> issues)
        {
            if (issues.Count != 0) throw new Exception("Clean BOM fixture unexpectedly produced: " + string.Join(", ", issues.Select(x => x.Code)));
        }

        private static void Has(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal))) throw new Exception("Expected BOM issue " + code + ".");
        }

        private static void MessageEquals(IReadOnlyList<ModelHealthIssue> issues, string code, string expected)
        {
            var matches = issues.Where(x => string.Equals(x.Code, code, StringComparison.Ordinal)).ToList();
            if (matches.Count == 0) throw new Exception("Expected BOM issue " + code + ".");
            foreach (var issue in matches)
                if (!string.Equals(issue.Message, expected, StringComparison.Ordinal))
                    throw new Exception("Expected redacted message for " + code + ", got: " + issue.Message);
        }

        private static int Count(IReadOnlyList<ModelHealthIssue> issues, string code) =>
            issues.Count(x => string.Equals(x.Code, code, StringComparison.Ordinal));

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
