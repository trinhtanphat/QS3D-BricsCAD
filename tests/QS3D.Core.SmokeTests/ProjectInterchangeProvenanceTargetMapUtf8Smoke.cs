using System;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeProvenanceTargetMapUtf8Smoke
    {
        public static void Run()
        {
            InvalidUtf8TargetIdFailsClosed();
            ValidUnicodeSourceIdentityRemainsReadable();
        }

        private static void InvalidUtf8TargetIdFailsClosed()
        {
            var target = TargetWithElement("T1");
            target.Metadata[ElementRecordKey("SRC", "E1")] = "v1." +
                Field("SRC") + "." +
                Field("FP") + "." +
                Field("E1") + "." +
                "wyg="; // bytes C3 28: valid Base64 but invalid UTF-8.

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SRC", "E1"));
        }

        private static void ValidUnicodeSourceIdentityRemainsReadable()
        {
            const string sourceProjectId = "Dự án nguồn";
            const string sourceElementId = "Phần tử 01";
            const string targetElementId = "T1";
            var target = TargetWithElement(targetElementId);
            target.Metadata[ElementRecordKey(sourceProjectId, sourceElementId)] = "v1." +
                Field(sourceProjectId) + "." +
                Field("Bản-vẽ") + "." +
                Field(sourceElementId) + "." +
                Field(targetElementId);

            var actual = ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, sourceProjectId, sourceElementId);
            if (!string.Equals(targetElementId, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid UTF-8 provenance target-map record no longer round-trips.");
        }

        private static ProjectState TargetWithElement(string targetElementId)
        {
            var target = new ProjectState("target-map-utf8", "Target map UTF8");
            target.Elements.Add(new ProjectElement(targetElementId, ElementCategory.Beam));
            return target;
        }

        private static string ElementRecordKey(string sourceProjectId, string sourceElementId)
        {
            return ProjectInterchangeProvenanceTargetMap.MetadataPrefix + Token(sourceProjectId) + ".Element." + Token(sourceElementId);
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

    internal static class ProjectInterchangeProvenanceTargetMapUtf8SmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectInterchangeProvenanceTargetMapUtf8Smoke.Run();
        }
    }
}
