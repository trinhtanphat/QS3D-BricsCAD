using System;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSourceHandleProvenanceIntegritySmoke
    {
        public static void Run()
        {
            BlankHandleFailsClosed();
            PaddedHandleFailsClosed();
            OverlongHandleFailsClosed();
            DuplicateHandleFailsClosed();
            ValidHandlesPreserveRecordOrder();
        }

        private static void BlankHandleFailsClosed() => ThrowsInvalid(BuildRecord(""));

        private static void PaddedHandleFailsClosed() => ThrowsInvalid(BuildRecord(" ABCD "));

        private static void OverlongHandleFailsClosed() => ThrowsInvalid(BuildRecord(new string('A', 129)));

        private static void DuplicateHandleFailsClosed() => ThrowsInvalid(BuildRecord("ABCD", "abcd"));

        private static void ValidHandlesPreserveRecordOrder()
        {
            var target = TargetWithRecord(BuildRecord("B2", "A1"));
            var handles = ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SRC", "E1");
            if (handles.Count != 2 ||
                !string.Equals(handles[0], "B2", StringComparison.Ordinal) ||
                !string.Equals(handles[1], "A1", StringComparison.Ordinal))
                throw new InvalidOperationException("Valid provenance handle order or values changed.");
        }

        private static void ThrowsInvalid(string record)
        {
            var target = TargetWithRecord(record);
            try
            {
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SRC", "E1");
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException("Expected invalid persisted provenance handle state to fail closed.");
        }

        private static ProjectState TargetWithRecord(string record)
        {
            var target = new ProjectState("target-provenance-integrity", "Target provenance integrity");
            target.Metadata[ElementRecordKey("SRC", "E1")] = record;
            return target;
        }

        private static string BuildRecord(params string[] handles)
        {
            var builder = new StringBuilder("v1.")
                .Append(Field("E1")).Append('.')
                .Append(Field("FP")).Append('.')
                .Append(Field("Project")).Append('.')
                .Append(Field(handles.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            foreach (var handle in handles) builder.Append('.').Append(Field(handle));
            return builder.ToString();
        }

        private static string ElementRecordKey(string sourceProjectId, string sourceElementId)
        {
            return ProjectInterchangeSourceHandleProvenance.MetadataPrefix + Token(sourceProjectId) + ".Element." + Token(sourceElementId);
        }

        private static string Token(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant()))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string Field(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    internal static class ProjectInterchangeSourceHandleProvenanceIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectInterchangeSourceHandleProvenanceIntegritySmoke.Run();
        }
    }
}
