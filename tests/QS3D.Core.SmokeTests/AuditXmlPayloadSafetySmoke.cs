using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditXmlPayloadSafetySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesValidPayloadThroughQsdbRoundTrip();
            RejectsInvalidNewPayloadBeforeMutation();
            RejectsInvalidActionBeforeMutation();
            RejectsMalformedExistingPayloadOnReadAndRecord();
        }

        private static void PreservesValidPayloadThroughQsdbRoundTrip()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-audit-xml-valid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "fixture.qsdb");
            try
            {
                var project = new ProjectState("AUDIT-XML-VALID", "Audit XML valid");
                var audit = AuditTrail.ForProject(project);
                const string elementId = "E-1";
                const string detail = " line1\tline2\nline3\r ";
                const string actor = " user@example.test ";
                const string correlationId = "corr-1";
                audit.Record("AUDIT_XML_VALID", elementId, detail, actor, correlationId);

                new QsdbProjectStore().SaveNew(project, path);
                var loaded = new QsdbProjectStore().Load(path);
                if (loaded.AuditEvents.Count != 1)
                    throw new InvalidOperationException("Valid audit XML payload did not round-trip exactly once.");
                var item = loaded.AuditEvents[0];
                if (!string.Equals(item.ElementId, elementId, StringComparison.Ordinal) ||
                    !string.Equals(item.Detail, detail, StringComparison.Ordinal) ||
                    !string.Equals(item.Actor, actor, StringComparison.Ordinal) ||
                    !string.Equals(item.CorrelationId, correlationId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Valid audit XML payload text changed during QSDB round-trip.");
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void RejectsInvalidNewPayloadBeforeMutation()
        {
            ExpectRejectedPayload("elementId", (audit, invalid) => audit.Record("AUDIT_XML", invalid, "detail"));
            ExpectRejectedPayload("detail", (audit, invalid) => audit.Record("AUDIT_XML", "E1", invalid));
            ExpectRejectedPayload("actor", (audit, invalid) => audit.Record("AUDIT_XML", "E1", "detail", invalid));
            ExpectRejectedPayload("correlationId", (audit, invalid) => audit.Record("AUDIT_XML", "E1", "detail", "actor", invalid));
        }

        private static void ExpectRejectedPayload(string label, Action<AuditTrail, string> record)
        {
            var project = new ProjectState("AUDIT-XML-" + label.ToUpperInvariant(), "Audit XML " + label);
            var audit = AuditTrail.ForProject(project);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            try
            {
                record(audit, "bad\u0001payload");
                throw new InvalidOperationException("AuditTrail.Record must reject XML-invalid " + label + " before mutation.");
            }
            catch (ArgumentException)
            {
            }

            if (project.AuditEvents.Count != 0)
                throw new InvalidOperationException("Rejected audit " + label + " still appended an event.");
            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdatedUtc)
                throw new InvalidOperationException("Rejected audit " + label + " changed project freshness.");
        }

        private static void RejectsInvalidActionBeforeMutation()
        {
            var project = new ProjectState("AUDIT-XML-ACTION", "Audit XML action");
            var audit = AuditTrail.ForProject(project);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            try
            {
                audit.Record("bad\uD800action", "E1", "detail");
                throw new InvalidOperationException("AuditTrail.Record must reject an XML-invalid action before mutation.");
            }
            catch (ArgumentException)
            {
            }

            if (project.AuditEvents.Count != 0 || project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdatedUtc)
                throw new InvalidOperationException("Rejected XML-invalid audit action mutated project state.");
        }

        private static void RejectsMalformedExistingPayloadOnReadAndRecord()
        {
            var project = new ProjectState("AUDIT-XML-EXISTING", "Audit XML existing");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = "EXISTING",
                ElementId = "E1",
                Detail = "bad\u0001detail",
                Actor = "actor",
                CorrelationId = "corr"
            });
            var audit = AuditTrail.ForProject(project);

            try
            {
                _ = audit.Events;
                throw new InvalidOperationException("AuditTrail.Events must fail visibly on XML-invalid stored payload.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("XML", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            try
            {
                audit.Record("NEXT", "E2", "valid");
                throw new InvalidOperationException("AuditTrail.Record must reject malformed existing XML payload before appending.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("XML", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }

            if (project.AuditEvents.Count != 1)
                throw new InvalidOperationException("Malformed existing audit history was mutated during rejected record.");
            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdatedUtc)
                throw new InvalidOperationException("Rejected record over malformed audit history changed project freshness.");
        }
    }
}
