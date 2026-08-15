using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeProvenanceTargetMapUnicodeIntegritySmoke
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MalformedWriterInputsFailBeforeMutation();
            SupplementaryUnicodeRoundTripsExactly();
        }

        private static void MalformedWriterInputsFailBeforeMutation()
        {
            AssertRejectedWithoutMutation(target =>
                ProjectInterchangeProvenanceTargetMap.Store(
                    target,
                    "source-high-\uD800",
                    "drawing-fingerprint",
                    Mapping("source-element", "target-element")));

            AssertRejectedWithoutMutation(target =>
                ProjectInterchangeProvenanceTargetMap.Store(
                    target,
                    "source-project",
                    "drawing-low-\uDC00",
                    Mapping("source-element", "target-element")));

            AssertRejectedWithoutMutation(target =>
                ProjectInterchangeProvenanceTargetMap.Store(
                    target,
                    "source-project",
                    "drawing-fingerprint",
                    Mapping("source-high-\uD800", "target-element")));

            AssertRejectedWithoutMutation(target =>
                ProjectInterchangeProvenanceTargetMap.Store(
                    target,
                    "source-project",
                    "drawing-fingerprint",
                    Mapping("source-low-\uDC00", "target-element")));
        }

        private static void SupplementaryUnicodeRoundTripsExactly()
        {
            const string sourceProjectId = "source-rocket-\uD83D\uDE80";
            const string sourceFingerprint = "drawing-rocket-\uD83D\uDE80";
            const string sourceElementId = "source-element-rocket-\uD83D\uDE80";
            const string targetElementId = "target-element-rocket-\uD83D\uDE80";
            var target = TargetWithElement(targetElementId);

            var result = ProjectInterchangeProvenanceTargetMap.Store(
                target,
                sourceProjectId,
                sourceFingerprint,
                Mapping(sourceElementId, targetElementId));

            Equal(sourceProjectId, result.SourceProjectId,
                "Target-map result must preserve valid supplementary source project identity ordinally.");
            Equal(targetElementId,
                ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, sourceProjectId, sourceElementId),
                "Target-map public readback must preserve valid supplementary target identity ordinally.");

            var prefix = ProjectInterchangeProvenanceTargetMap.MetadataPrefix + Token(sourceProjectId);
            var projectRecord = target.Metadata.Single(x =>
                x.Key.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase) &&
                x.Key.EndsWith(".Project", StringComparison.OrdinalIgnoreCase));
            var projectFields = DecodeRecord(projectRecord.Value);
            Equal(sourceProjectId, projectFields[0],
                "Target-map record must preserve valid supplementary source project identity ordinally.");
            Equal(sourceFingerprint, projectFields[1],
                "Target-map record must preserve valid supplementary drawing fingerprint ordinally.");

            var elementRecord = target.Metadata.Single(x =>
                x.Key.StartsWith(prefix + ".Element.", StringComparison.OrdinalIgnoreCase));
            var elementFields = DecodeRecord(elementRecord.Value);
            Equal(sourceElementId, elementFields[2],
                "Target-map record must preserve valid supplementary source Element identity ordinally.");
            Equal(targetElementId, elementFields[3],
                "Target-map record must preserve valid supplementary target Element identity ordinally.");
        }

        private static void AssertRejectedWithoutMutation(Action<ProjectState> action)
        {
            var target = TargetWithElement("target-element");
            target.Metadata["sentinel"] = "keep";
            var beforeMetadata = new Dictionary<string, string>(target.Metadata, StringComparer.OrdinalIgnoreCase);
            var beforeAuditCount = target.AuditEvents.Count;
            var beforeChangeVersion = target.ChangeVersion;

            Throws<EncoderFallbackException>(() => action(target));

            if (target.Metadata.Count != beforeMetadata.Count ||
                beforeMetadata.Any(x => !target.Metadata.TryGetValue(x.Key, out var value) ||
                                        !string.Equals(x.Value, value, StringComparison.Ordinal)))
                throw new InvalidOperationException("Malformed target-map Unicode must not mutate project metadata.");
            if (target.AuditEvents.Count != beforeAuditCount)
                throw new InvalidOperationException("Malformed target-map Unicode must not append audit evidence.");
            if (target.ChangeVersion != beforeChangeVersion)
                throw new InvalidOperationException("Malformed target-map Unicode must not advance project revision.");
        }

        private static Dictionary<string, string> Mapping(string sourceElementId, string targetElementId)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [sourceElementId] = targetElementId
            };
        }

        private static ProjectState TargetWithElement(string targetElementId)
        {
            var target = new ProjectState("target-map-unicode", "Target map Unicode");
            target.Elements.Add(new ProjectElement(targetElementId, ElementCategory.Beam));
            return target;
        }

        private static string Token(string value)
        {
            return Convert.ToBase64String(StrictUtf8.GetBytes(value.Trim().ToUpperInvariant()))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static IReadOnlyList<string> DecodeRecord(string value)
        {
            var parts = value.Split(new[] { '.' }, StringSplitOptions.None);
            if (parts.Length < 2 || !string.Equals("v1", parts[0], StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected target-map test record shape.");
            return parts.Skip(1)
                .Select(x => StrictUtf8.GetString(Convert.FromBase64String(x)))
                .ToArray();
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(message);
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
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".",
                    ex);
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
