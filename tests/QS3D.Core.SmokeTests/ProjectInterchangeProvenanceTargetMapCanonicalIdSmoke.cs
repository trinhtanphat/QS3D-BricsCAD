using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeProvenanceTargetMapCanonicalIdSmoke
    {
        public static void Run()
        {
            PaddedPersistedTargetIdFailsClosed();
            StoreStillNormalizesCallerIds();
        }

        private static void PaddedPersistedTargetIdFailsClosed()
        {
            var target = TargetWithElement("T1");
            target.Metadata[ElementRecordKey("SRC", "E1")] = Record("SRC", "FP", "E1", " T1 ");

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SRC", "E1"));
        }

        private static void StoreStillNormalizesCallerIds()
        {
            var target = TargetWithElement("T1");
            ProjectInterchangeProvenanceTargetMap.Store(
                target,
                " SRC ",
                " FP ",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [" E1 "] = " T1 "
                });

            var actual = ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SRC", "E1");
            if (!string.Equals("T1", actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Public target-map Store normalization changed.");
        }

        private static ProjectState TargetWithElement(string targetElementId)
        {
            var target = new ProjectState("target-map-canonical", "Target map canonical");
            target.Elements.Add(new ProjectElement(targetElementId, ElementCategory.Beam));
            return target;
        }

        private static string ElementRecordKey(string sourceProjectId, string sourceElementId)
        {
            return ProjectInterchangeProvenanceTargetMap.MetadataPrefix + Token(sourceProjectId) + ".Element." + Token(sourceElementId);
        }

        private static string Record(string sourceProjectId, string fingerprint, string sourceElementId, string targetElementId)
        {
            return "v1." + Field(sourceProjectId) + "." + Field(fingerprint) + "." + Field(sourceElementId) + "." + Field(targetElementId);
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

    internal static class ProjectInterchangeProvenanceTargetMapCanonicalIdSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectInterchangeProvenanceTargetMapCanonicalIdSmoke.Run();
        }
    }
}
