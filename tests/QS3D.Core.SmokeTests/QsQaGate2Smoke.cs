using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQaGate2Smoke
    {
        internal static void Run()
        {
            BlocksTakeoffBoqAndEstimateOnCriticalRelationshipFailure();
            HonorsExplicitUnexpiredWaiver();
            RejectsExpiredWaiver();
            AppliesConfigurableSeverityThreshold();
            DetectsIfcPsetSpatialTypeAndGuidConsistency();
            HardGateDemandFailsClosedForGuardedWorkflows();
            GuardedExecutorBlocksBeforeWorkflowInvocation();
        }

        private static void BlocksTakeoffBoqAndEstimateOnCriticalRelationshipFailure()
        {
            var element = ValidElement("E1", "GUID-1", includeTypeRelationship: false);
            var decision = new QsQaGate2().Evaluate(new[] { element }, QsQaRuleProfile.SolibriQuantityStrict(), null!, Utc(2026, 9, 12));

            Expect(decision.Status == QsQaGateStatus.Blocked, "missing required relationship must block");
            Expect(!decision.CanTakeoff, "hard gate must block takeoff");
            Expect(!decision.CanBoq, "hard gate must block BOQ");
            Expect(!decision.CanEstimate, "hard gate must block estimate");
            Expect(decision.ActiveFindings.Any(x => x.RuleId == "QA2.MISSING_RELATIONSHIP" && x.Severity == QsQaSeverity.Critical), "critical relationship finding expected");
        }

        private static void HonorsExplicitUnexpiredWaiver()
        {
            var profile = new QsQaRuleProfile(
                new string[0],
                new[] { "TypeAssignment" },
                new Dictionary<string, QsQaSeverity> { { "QA2.MISSING_RELATIONSHIP", QsQaSeverity.Error } },
                QsQaSeverity.Error);
            var now = Utc(2026, 9, 12);
            var element = ValidElement("E2", "GUID-2", includeTypeRelationship: false);
            var waiver = new QsQaWaiver("QA2.MISSING_RELATIONSHIP", "E2", "Legacy IFC awaiting author correction", "lead.qs", now.AddDays(-1), now.AddDays(7));
            var decision = new QsQaGate2().Evaluate(new[] { element }, profile, new[] { waiver }, now);

            Expect(decision.Status == QsQaGateStatus.Pass, "valid waiver should remove the finding from gate calculation");
            Expect(decision.CanBoq && decision.CanEstimate && decision.CanTakeoff, "valid waiver should release all guarded workflows");
            Expect(decision.WaivedFindings.Count == 1, "waived finding must remain auditable");
            Expect(decision.ActiveFindings.Count == 0, "waived finding must not remain active");
        }

        private static void RejectsExpiredWaiver()
        {
            var profile = new QsQaRuleProfile(
                new string[0],
                new[] { "TypeAssignment" },
                null!,
                QsQaSeverity.Error);
            var now = Utc(2026, 9, 12);
            var element = ValidElement("E3", "GUID-3", includeTypeRelationship: false);
            var waiver = new QsQaWaiver("QA2.MISSING_RELATIONSHIP", "E3", "Temporary exception", "lead.qs", now.AddDays(-10), now.AddDays(-1));
            var decision = new QsQaGate2().Evaluate(new[] { element }, profile, new[] { waiver }, now);

            Expect(decision.Status == QsQaGateStatus.Blocked, "expired waiver must not release gate");
            Expect(decision.WaivedFindings.Count == 0, "expired waiver must not hide finding");
        }

        private static void AppliesConfigurableSeverityThreshold()
        {
            var profile = new QsQaRuleProfile(
                new string[0],
                new[] { "TypeAssignment" },
                new Dictionary<string, QsQaSeverity> { { "QA2.MISSING_RELATIONSHIP", QsQaSeverity.Warning } },
                QsQaSeverity.Error);
            var element = ValidElement("E4", "GUID-4", includeTypeRelationship: false);
            var decision = new QsQaGate2().Evaluate(new[] { element }, profile, null!, Utc(2026, 9, 12));

            Expect(decision.Status == QsQaGateStatus.PassWithWarnings, "rule severity override must be honored");
            Expect(decision.CanTakeoff && decision.CanBoq && decision.CanEstimate, "warning must not block an Error-threshold gate");
        }

        private static void DetectsIfcPsetSpatialTypeAndGuidConsistency()
        {
            var a = ValidElement("E5", "GUID-X", includeTypeRelationship: true);
            var badProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "IfcGuid", "GUID-X" },
                { "IfcPset.Pset_Identity", "present" },
                { "IfcRel.SpatialContainer", "L99" },
                { "IfcRel.TypeAssignment", "Door" }
            };
            var b = new QsModelElementSnapshot("E6", "Wall", "Concrete", "A-WALL", "L02", 4d, 0.2d, 3d, badProperties);
            var decision = new QsQaGate2().Evaluate(new[] { a, b }, QsQaRuleProfile.SolibriQuantityStrict(), null!, Utc(2026, 9, 12));

            Expect(decision.ActiveFindings.Any(x => x.RuleId == "QA2.DUPLICATE_IFC_GUID"), "duplicate IFC GUID must be detected");
            Expect(decision.ActiveFindings.Any(x => x.RuleId == "QA2.MISSING_PSET" && x.ElementId == "E6"), "missing required Pset must be detected");
            Expect(decision.ActiveFindings.Any(x => x.RuleId == "QA2.SPATIAL_MISMATCH" && x.ElementId == "E6"), "storey/spatial mismatch must be detected");
            Expect(decision.ActiveFindings.Any(x => x.RuleId == "QA2.TYPE_ASSIGNMENT_MISMATCH" && x.ElementId == "E6"), "element type/IFC type assignment mismatch must be detected");
            Expect(decision.Status == QsQaGateStatus.Blocked, "IFC consistency failures must block strict profile");
        }

        private static void HardGateDemandFailsClosedForGuardedWorkflows()
        {
            var blocked = new QsQaGate2().Evaluate(
                new[] { ValidElement("E7", "GUID-7", includeTypeRelationship: false) },
                QsQaRuleProfile.SolibriQuantityStrict(),
                null!,
                Utc(2026, 9, 12));

            ExpectThrows(() => blocked.DemandAllowed(QsQaGuardedWorkflow.Takeoff), "takeoff demand must fail closed");
            ExpectThrows(() => blocked.DemandAllowed(QsQaGuardedWorkflow.Boq), "BOQ demand must fail closed");
            ExpectThrows(() => blocked.DemandAllowed(QsQaGuardedWorkflow.Estimate), "estimate demand must fail closed");

            var allowed = new QsQaGate2().Evaluate(
                new[] { ValidElement("E8", "GUID-8", includeTypeRelationship: true) },
                QsQaRuleProfile.SolibriQuantityStrict(),
                null!,
                Utc(2026, 9, 12));
            allowed.DemandAllowed(QsQaGuardedWorkflow.Takeoff);
            allowed.DemandAllowed(QsQaGuardedWorkflow.Boq);
            allowed.DemandAllowed(QsQaGuardedWorkflow.Estimate);
        }

        private static void GuardedExecutorBlocksBeforeWorkflowInvocation()
        {
            var blocked = new QsQaGate2().Evaluate(
                new[] { ValidElement("E9", "GUID-9", includeTypeRelationship: false) },
                QsQaRuleProfile.SolibriQuantityStrict(),
                null!,
                Utc(2026, 9, 12));
            var executor = new QsQaGuardedExecutor();

            foreach (var workflow in new[] { QsQaGuardedWorkflow.Takeoff, QsQaGuardedWorkflow.Boq, QsQaGuardedWorkflow.Estimate })
            {
                var invoked = false;
                ExpectThrows(() => executor.Execute(blocked, workflow, () => { invoked = true; return 42; }), workflow + " guarded execution must fail closed");
                Expect(!invoked, workflow + " work must not run when QA is blocked");
            }

            var allowed = new QsQaGate2().Evaluate(
                new[] { ValidElement("E10", "GUID-10", includeTypeRelationship: true) },
                QsQaRuleProfile.SolibriQuantityStrict(),
                null!,
                Utc(2026, 9, 12));
            var count = 0;
            var value = executor.Execute(allowed, QsQaGuardedWorkflow.Takeoff, () => { count++; return 7; });
            executor.Execute(allowed, QsQaGuardedWorkflow.Boq, () => count++);
            executor.Execute(allowed, QsQaGuardedWorkflow.Estimate, () => count++);

            Expect(value == 7, "guarded generic execution must return workflow result");
            Expect(count == 3, "allowed workflows must execute exactly once each");
        }

        private static QsModelElementSnapshot ValidElement(string id, string guid, bool includeTypeRelationship)
        {
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "IfcGuid", guid },
                { "IfcPset.Pset_Qto", "present" },
                { "IfcPset.Pset_Identity", "present" },
                { "IfcRel.SpatialContainer", "L01" }
            };
            if (includeTypeRelationship) properties["IfcRel.TypeAssignment"] = "Wall";
            return new QsModelElementSnapshot(id, "Wall", "Concrete", "A-WALL", "L01", 4d, 0.2d, 3d, properties);
        }

        private static DateTime Utc(int year, int month, int day)
        {
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static void ExpectThrows(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("QsQaGate2Smoke: " + message);
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QsQaGate2Smoke: " + message);
        }
    }
}
