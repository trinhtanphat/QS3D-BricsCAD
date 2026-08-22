using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationManagerProjectionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DeterministicOrderingAndFilters();
            NonActionableRowsFailClosed();
            DuplicateIdentityFailsClosed();
        }

        private static void DeterministicOrderingAndFilters()
        {
            var rows = new[]
            {
                New("B", CoordinationFindingKind.Clearance, CoordinationFindingStatus.Open, CoordinationFindingSeverity.Medium, "F2", "Pipe", "Beam", "R-CLEAR", true, true, false),
                New("A", CoordinationFindingKind.HardClash, CoordinationFindingStatus.Open, CoordinationFindingSeverity.Critical, "F1", "Duct", "Wall", "R-HARD", true, true, false),
                New("C", CoordinationFindingKind.Duplicate, CoordinationFindingStatus.Reviewed, CoordinationFindingSeverity.High, "F1", "Column", "Column", "R-DUP", true, true, false)
            };

            var forward = CoordinationManagerProjection.Build(rows);
            var reverse = CoordinationManagerProjection.Build(rows.Reverse());
            Equal(string.Join(",", forward.Select(x => x.Id)), string.Join(",", reverse.Select(x => x.Id)), "order stable");
            Equal("A,C,B", string.Join(",", forward.Select(x => x.Id)), "severity order");

            var filtered = CoordinationManagerProjection.Build(rows, new CoordinationManagerFilter
            {
                FloorId = "f1",
                MinimumSeverity = CoordinationFindingSeverity.High
            });
            Equal("A,C", string.Join(",", filtered.Select(x => x.Id)), "composed filter");

            filtered = CoordinationManagerProjection.Build(rows, new CoordinationManagerFilter { Category = "wall" });
            Equal("A", string.Join(",", filtered.Select(x => x.Id)), "category either side");

            filtered = CoordinationManagerProjection.Build(rows, new CoordinationManagerFilter { RuleId = "r-clear" });
            Equal("B", string.Join(",", filtered.Select(x => x.Id)), "rule filter");
        }

        private static void NonActionableRowsFailClosed()
        {
            var unresolved = New("U", CoordinationFindingKind.HardClash, CoordinationFindingStatus.Open, CoordinationFindingSeverity.High, "F1", "Pipe", "Wall", "R1", false, true, false);
            var stale = New("S", CoordinationFindingKind.Clearance, CoordinationFindingStatus.Open, CoordinationFindingSeverity.Medium, "F1", "Duct", "Beam", "R2", true, true, true);
            var actionable = New("A", CoordinationFindingKind.HardClash, CoordinationFindingStatus.Open, CoordinationFindingSeverity.Low, "F1", "Pipe", "Beam", "R3", true, true, false);

            Equal(false, unresolved.IsActionable, "unresolved actionable");
            Equal("REFERENCE_A_UNRESOLVED", unresolved.NonActionableReason, "unresolved reason");
            Equal(false, stale.IsActionable, "stale actionable");
            Equal("STALE", stale.NonActionableReason, "stale reason");

            var all = CoordinationManagerProjection.Build(new[] { unresolved, stale, actionable });
            Equal(3, all.Count, "non-actionable visible by default");

            var selectable = CoordinationManagerProjection.Build(new[] { unresolved, stale, actionable }, new CoordinationManagerFilter { IncludeNonActionable = false });
            Equal(1, selectable.Count, "selectable count");
            Equal("A", selectable[0].Id, "selectable id");

            Throws<ArgumentException>(() => New("X", CoordinationFindingKind.HardClash, CoordinationFindingStatus.Open, CoordinationFindingSeverity.Low, "F1", "Pipe", "Beam", "R", true, true, false, "SHOULD_NOT_EXIST"));
        }

        private static void DuplicateIdentityFailsClosed()
        {
            var a = New("DUP", CoordinationFindingKind.HardClash, CoordinationFindingStatus.Open, CoordinationFindingSeverity.Low, "F1", "Pipe", "Beam", "R", true, true, false);
            var b = New("dup", CoordinationFindingKind.Clearance, CoordinationFindingStatus.Open, CoordinationFindingSeverity.Low, "F2", "Pipe", "Wall", "R2", true, true, false);
            Throws<InvalidOperationException>(() => CoordinationManagerProjection.Build(new[] { a, b }));
            Throws<ArgumentException>(() => New(" BAD ", CoordinationFindingKind.HardClash, CoordinationFindingStatus.Open, CoordinationFindingSeverity.Low, "F1", "Pipe", "Beam", "R", true, true, false));
        }

        private static CoordinationManagerFinding New(
            string id,
            CoordinationFindingKind kind,
            CoordinationFindingStatus status,
            CoordinationFindingSeverity severity,
            string floor,
            string categoryA,
            string categoryB,
            string rule,
            bool aResolved,
            bool bResolved,
            bool stale,
            string? reason = null)
        {
            return new CoordinationManagerFinding(id, kind, status, severity, floor, categoryA, categoryB, rule, aResolved, bResolved, stale, reason);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("CoordinationManagerProjectionSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("CoordinationManagerProjectionSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
