using System;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailActionCanonicalitySmoke
    {
        internal static void Run()
        {
            RejectsInvalidActionsBeforeProjectMutation();
            CanonicalizesValidActionAndPreservesPayload();
        }

        private static void RejectsInvalidActionsBeforeProjectMutation()
        {
            var project = new ProjectState("audit-action-invalid", "Audit action invalid");
            var trail = AuditTrail.ForProject(project);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeCount = project.AuditEvents.Count;

            Throws<ArgumentException>(() => trail.Record(null!, "E1", "detail"));
            Require(project.ChangeVersion == beforeVersion, "Null action changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Null action changed project timestamp.");
            Require(project.AuditEvents.Count == beforeCount, "Null action appended an audit event.");

            Throws<ArgumentException>(() => trail.Record(string.Empty, "E1", "detail"));
            Require(project.ChangeVersion == beforeVersion, "Empty action changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Empty action changed project timestamp.");
            Require(project.AuditEvents.Count == beforeCount, "Empty action appended an audit event.");

            Throws<ArgumentException>(() => trail.Record(" \t\r\n ", "E1", "detail"));
            Require(project.ChangeVersion == beforeVersion, "Whitespace action changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Whitespace action changed project timestamp.");
            Require(project.AuditEvents.Count == beforeCount, "Whitespace action appended an audit event.");

            Throws<ArgumentException>(() => trail.Record("APPLY\u0001_TEMPLATE", "E1", "detail"));
            Require(project.ChangeVersion == beforeVersion, "Control-character action changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Control-character action changed project timestamp.");
            Require(project.AuditEvents.Count == beforeCount, "Control-character action appended an audit event.");
        }

        private static void CanonicalizesValidActionAndPreservesPayload()
        {
            var project = new ProjectState("audit-action-valid", "Audit action valid");
            var trail = AuditTrail.ForProject(project);
            var beforeVersion = project.ChangeVersion;

            // ElementId and CorrelationId are persisted identity fields and therefore must
            // already be canonical. Action still trims by contract, while Detail/Actor keep
            // their free-form payload semantics.
            trail.Record("  APPLY_TEMPLATE  ", "E1", " detail ", " actor ", "corr");

            Require(project.ChangeVersion == beforeVersion + 1L, "Valid audit record did not advance project revision exactly once.");
            Require(project.AuditEvents.Count == 1, "Valid audit record did not append exactly one event.");
            var item = project.AuditEvents[0];
            Require(item.Action == "APPLY_TEMPLATE", "Audit action was not canonicalized by trimming outer whitespace.");
            Require(item.ElementId == "E1", "Audit canonical element id payload changed.");
            Require(item.Detail == " detail ", "Audit detail payload semantics changed.");
            Require(item.Actor == " actor ", "Audit actor payload semantics changed.");
            Require(item.CorrelationId == "corr", "Audit canonical correlation payload changed.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
