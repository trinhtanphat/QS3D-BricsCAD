using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSourceHandleProvenanceScopeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            const string sourceProjectId = "SRC-PROJECT";
            const string sourceElementId = "SRC-ELEMENT";
            const string handle = "1A2B";

            var missingTarget = new ProjectState("TARGET-MISSING", "Target Missing");
            True(
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(
                    missingTarget,
                    sourceProjectId,
                    sourceElementId).Count == 0,
                "Missing source-handle provenance must remain an empty read.");

            var target = new ProjectState("TARGET", "Target");
            var key = ProjectInterchangeSourceHandleProvenance.MetadataPrefix +
                Token(sourceProjectId) + ".Element." + Token(sourceElementId);

            target.Metadata[key] = EncodeRecord(
                sourceElementId,
                "DWG-FINGERPRINT",
                "drawing-local",
                "1",
                handle);

            var valid = ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(
                target,
                sourceProjectId,
                sourceElementId);
            True(valid.Count == 1 && string.Equals(valid[0], handle, StringComparison.Ordinal),
                "Drawing-local source-handle provenance must remain readable.");

            target.Metadata[key] = EncodeRecord(
                sourceElementId,
                "DWG-FINGERPRINT",
                "project-global",
                "1",
                handle);

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(
                    target,
                    sourceProjectId,
                    sourceElementId));
        }

        private static string Token(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant()))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string EncodeRecord(params string[] fields)
        {
            return "v1." + string.Join(
                ".",
                fields.Select(x => Convert.ToBase64String(Encoding.UTF8.GetBytes(x ?? string.Empty))));
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

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}