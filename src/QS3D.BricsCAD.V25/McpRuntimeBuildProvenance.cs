using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace QS3D.BricsCAD.V25
{
    internal sealed class McpRuntimeBuildIdentity
    {
        internal McpRuntimeBuildIdentity(string buildSha, string buildId, string buildUtc)
        {
            BuildSha = buildSha ?? string.Empty;
            BuildId = buildId ?? string.Empty;
            BuildUtc = buildUtc ?? string.Empty;
        }

        internal string BuildSha { get; private set; }
        internal string BuildId { get; private set; }
        internal string BuildUtc { get; private set; }
    }

    internal static class McpRuntimeBuildProvenance
    {
        internal const int MaxMetadataBytes = 64 * 1024;
        private const string MetadataFileName = "PACKAGE-METADATA.json";
        private static readonly Regex GitCommitRegex = new Regex(
            "\\\"gitCommit\\\"\\s*:\\s*\\\"(?<value>[0-9a-fA-F]{40})\\\"",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex GeneratedUtcRegex = new Regex(
            "\\\"generatedUtc\\\"\\s*:\\s*\\\"(?<value>[^\\\"\\\\]{1,80})\\\"",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Lazy<McpRuntimeBuildIdentity> Cached =
            new Lazy<McpRuntimeBuildIdentity>(LoadCurrent, true);

        internal static McpRuntimeBuildIdentity Current { get { return Cached.Value; } }

        private static McpRuntimeBuildIdentity LoadCurrent()
        {
            var assembly = typeof(McpRuntimeBuildProvenance).Assembly;
            var buildId = SafeModuleVersionId(assembly);
            try
            {
                var assemblyPath = assembly.Location;
                if (string.IsNullOrWhiteSpace(assemblyPath))
                    return new McpRuntimeBuildIdentity(string.Empty, buildId, string.Empty);

                var directory = Path.GetDirectoryName(Path.GetFullPath(assemblyPath));
                if (string.IsNullOrWhiteSpace(directory))
                    return new McpRuntimeBuildIdentity(string.Empty, buildId, string.Empty);

                var metadataPath = Path.Combine(directory, MetadataFileName);
                var metadata = ReadBoundedMetadata(metadataPath);
                if (string.IsNullOrEmpty(metadata))
                    return new McpRuntimeBuildIdentity(string.Empty, buildId, string.Empty);

                var commitMatch = GitCommitRegex.Match(metadata);
                var utcMatch = GeneratedUtcRegex.Match(metadata);
                if (!commitMatch.Success || !utcMatch.Success)
                    return new McpRuntimeBuildIdentity(string.Empty, buildId, string.Empty);

                DateTime parsedUtc;
                if (!DateTime.TryParse(
                        utcMatch.Groups["value"].Value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out parsedUtc))
                    return new McpRuntimeBuildIdentity(string.Empty, buildId, string.Empty);

                return new McpRuntimeBuildIdentity(
                    commitMatch.Groups["value"].Value.ToLowerInvariant(),
                    buildId,
                    parsedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            }
            catch
            {
                return new McpRuntimeBuildIdentity(string.Empty, buildId, string.Empty);
            }
        }

        private static string SafeModuleVersionId(Assembly assembly)
        {
            try { return assembly.ManifestModule.ModuleVersionId.ToString("D"); }
            catch { return string.Empty; }
        }

        private static string ReadBoundedMetadata(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length <= 0 || info.Length > MaxMetadataBytes) return string.Empty;

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length <= 0 || stream.Length > MaxMetadataBytes) return string.Empty;
                    var strictUtf8 = new UTF8Encoding(false, true);
                    using (var reader = new StreamReader(stream, strictUtf8, true, 4096, false))
                    {
                        var text = reader.ReadToEnd();
                        return strictUtf8.GetByteCount(text) <= MaxMetadataBytes ? text : string.Empty;
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
