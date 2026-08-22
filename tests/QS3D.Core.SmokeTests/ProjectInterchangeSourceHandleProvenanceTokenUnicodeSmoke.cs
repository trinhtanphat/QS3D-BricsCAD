using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSourceHandleProvenanceTokenUnicodeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MalformedProjectLookupCannotAliasLiteralReplacementCharacter();
            MalformedElementLookupFailsClosedWithoutMutation();
            SupplementaryUnicodeIdentitiesRoundTrip();
        }

        private static void MalformedProjectLookupCannotAliasLiteralReplacementCharacter()
        {
            const string validProjectId = "source-\uFFFD";
            const string validElementId = "element-\uFFFD";
            var target = StoredProvenance(validProjectId, validElementId, "ABCD");

            Equal("ABCD", string.Join("|",
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, validProjectId, validElementId)));

            AssertRejectedWithoutMutation(target, () =>
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "source-\uD800", validElementId));
            AssertRejectedWithoutMutation(target, () =>
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "source-\uDC00", validElementId));

            Equal("ABCD", string.Join("|",
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, validProjectId, validElementId)));
        }

        private static void MalformedElementLookupFailsClosedWithoutMutation()
        {
            const string validProjectId = "source-\uFFFD";
            const string validElementId = "element-\uFFFD";
            var target = StoredProvenance(validProjectId, validElementId, "BEEF");

            AssertRejectedWithoutMutation(target, () =>
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, validProjectId, "element-\uD800"));
            AssertRejectedWithoutMutation(target, () =>
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, validProjectId, "element-\uDC00"));
        }

        private static void SupplementaryUnicodeIdentitiesRoundTrip()
        {
            const string sourceProjectId = "Project-\uD83D\uDE80";
            const string sourceElementId = "Element-\uD83E\uDDF1";
            var target = StoredProvenance(sourceProjectId, sourceElementId, "FACE");

            Equal("FACE", string.Join("|",
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(
                    target,
                    "project-\uD83D\uDE80",
                    "element-\uD83E\uDDF1")));
        }

        private static ProjectState StoredProvenance(string sourceProjectId, string sourceElementId, string handle)
        {
            var source = new ProjectState(sourceProjectId, "Unicode provenance source")
            {
                DrawingFingerprint = "SOURCE-DWG",
                UpdatedUtc = new DateTime(2026, 8, 15, 4, 30, 0, DateTimeKind.Utc)
            };
            var element = new ProjectElement(sourceElementId, ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            element.SourceHandles.Add(handle);
            source.Elements.Add(element);

            var target = new ProjectState("TARGET-TOKEN-UNICODE", "Target token Unicode");
            ProjectInterchangeSourceHandleProvenance.Store(
                target,
                ProjectInterchangeJsonExporter.Build(source));
            return target;
        }

        private static void AssertRejectedWithoutMutation(ProjectState target, Action action)
        {
            var beforeMetadata = new Dictionary<string, string>(target.Metadata, StringComparer.OrdinalIgnoreCase);
            var beforeAuditCount = target.AuditEvents.Count;
            var beforeChangeVersion = target.ChangeVersion;
            var beforeUpdatedUtc = target.UpdatedUtc;

            Throws<EncoderFallbackException>(action);

            if (target.Metadata.Count != beforeMetadata.Count ||
                beforeMetadata.Any(x => !target.Metadata.TryGetValue(x.Key, out var value) ||
                                        !string.Equals(x.Value, value, StringComparison.Ordinal)))
                throw new InvalidOperationException("Malformed provenance lookup identity must not mutate metadata.");
            if (target.AuditEvents.Count != beforeAuditCount)
                throw new InvalidOperationException("Malformed provenance lookup identity must not append audit evidence.");
            if (target.ChangeVersion != beforeChangeVersion)
                throw new InvalidOperationException("Malformed provenance lookup identity must not advance project revision.");
            if (target.UpdatedUtc != beforeUpdatedUtc)
                throw new InvalidOperationException("Malformed provenance lookup identity must not change project time.");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
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
