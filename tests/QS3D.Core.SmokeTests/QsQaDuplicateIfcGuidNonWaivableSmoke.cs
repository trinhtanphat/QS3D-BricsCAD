using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQaDuplicateIfcGuidNonWaivableSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var now = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);
            var first = ValidElement("QA2-GUID-A", "GUID-SHARED");
            var second = ValidElement("QA2-GUID-B", " guid-shared ");
            var waivers = new[]
            {
                new QsQaWaiver("QA2.DUPLICATE_IFC_GUID", first.Id, "Invalid identity waiver attempt", "lead.qs", now.AddHours(-1), now.AddDays(1)),
                new QsQaWaiver("QA2.DUPLICATE_IFC_GUID", second.Id, "Invalid identity waiver attempt", "lead.qs", now.AddHours(-1), now.AddDays(1))
            };

            var decision = new QsQaGate2().Evaluate(
                new[] { first, second },
                QsQaRuleProfile.SolibriQuantityStrict(),
                waivers,
                now);

            var duplicates = decision.ActiveFindings.Where(x => x.RuleId == "QA2.DUPLICATE_IFC_GUID").ToList();
            Expect(decision.Status == QsQaGateStatus.Blocked, "duplicate IFC identity must remain a hard-gate failure");
            Expect(duplicates.Count == 2, "every duplicate IFC GUID participant must remain active");
            Expect(decision.WaivedFindings.All(x => x.RuleId != "QA2.DUPLICATE_IFC_GUID"), "duplicate IFC GUID conflicts must never enter waived findings");
            Expect(!decision.CanTakeoff && !decision.CanBoq && !decision.CanEstimate, "duplicate IFC GUID conflicts must block all guarded quantity workflows");
        }

        private static QsModelElementSnapshot ValidElement(string id, string guid)
        {
            return new QsModelElementSnapshot(
                id,
                "Wall",
                "Concrete",
                "A-WALL",
                "L01",
                4d,
                0.2d,
                3d,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "IfcGuid", guid },
                    { "IfcPset.Pset_Qto", "present" },
                    { "IfcPset.Pset_Identity", "present" },
                    { "IfcRel.SpatialContainer", "L01" },
                    { "IfcRel.TypeAssignment", "Wall" }
                });
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QA2 duplicate IFC GUID smoke failed: " + message);
        }
    }
}
