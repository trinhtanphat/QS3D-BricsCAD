using System;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSourceHandleProvenanceUtf8Smoke
    {
        public static void Run()
        {
            InvalidUtf8HandleFailsClosed();
            ValidUnicodeIdentityAndAsciiHandleRemainReadable();
        }

        private static void InvalidUtf8HandleFailsClosed()
        {
            const string sourceProjectId = "SRC";
            const string sourceElementId = "E1";
            var target = new ProjectState("target-invalid-provenance", "Target invalid provenance");
            var key = ElementRecordKey(sourceProjectId, sourceElementId);
            target.Metadata[key] = "v1." +
                Field(sourceElementId) + "." +
                Field("FP") + "." +
                Field("Project") + "." +
                Field("1") + "." +
                "wyg="; // bytes C3 28: valid Base64 but invalid UTF-8.

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, sourceProjectId, sourceElementId));
        }

        private static void ValidUnicodeIdentityAndAsciiHandleRemainReadable()
        {
            const string sourceProjectId = "Dự án nguồn";
            const string sourceElementId = "Phần tử 01";
            const string sourceHandle = "ABCD";
            var target = new ProjectState("target-valid-provenance", "Target valid provenance");
            var key = ElementRecordKey(sourceProjectId, sourceElementId);
            target.Metadata[key] = "v1." +
                Field(sourceElementId) + "." +
                Field("Bản-vẽ") + "." +
                Field("drawing-local") + "." +
                Field("1") + "." +
                Field(sourceHandle);

            var handles = ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, sourceProjectId, sourceElementId);
            if (handles.Count != 1 || !string.Equals(sourceHandle, handles[0], StringComparison.Ordinal))
                throw new InvalidOperationException("Valid UTF-8 interchange provenance no longer round-trips.");
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class ProjectInterchangeSourceHandleProvenanceUtf8SmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectInterchangeSourceHandleProvenanceUtf8Smoke.Run();
        }
    }
}
