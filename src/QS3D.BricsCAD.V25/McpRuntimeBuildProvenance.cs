using System;
using System.Collections.Generic;
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
        private const string BuildShaMetadataKey = "QS3D.BuildSha";
        private const string BuildUtcMetadataKey = "QS3D.BuildUtc";
        private static readonly Regex ExactGitCommitRegex = new Regex(
            "^[0-9a-fA-F]{40}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
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
                var embedded = ReadAssemblyMetadata(assembly);
                var buildSha = NormalizeGitCommit(GetMetadataValue(embedded, BuildShaMetadataKey));
                var buildUtc = NormalizeUtc(GetMetadataValue(embedded, BuildUtcMetadataKey));

                var assemblyPath = assembly.Location;
                if (!string.IsNullOrWhiteSpace(assemblyPath))
                {
                    var directory = Path.GetDirectoryName(Path.GetFullPath(assemblyPath));
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        var metadata = ReadBoundedMetadata(Path.Combine(directory, MetadataFileName));
                        if (!string.IsNullOrEmpty(metadata))
                        {
                            if (string.IsNullOrEmpty(buildSha))
                            {
                                var commitMatch = GitCommitRegex.Match(metadata);
                                if (commitMatch.Success)
                                    buildSha = NormalizeGitCommit(commitMatch.Groups["value"].Value);
                            }
                            if (string.IsNullOrEmpty(buildUtc))
                            {
                                var utcMatch = GeneratedUtcRegex.Match(metadata);
                                if (utcMatch.Success)
                                    buildUtc = NormalizeUtc(utcMatch.Groups["value"].Value);
                            }
                        }
                    }
                }

                return new McpRuntimeBuildIdentity(buildSha, buildId, buildUtc);
            }
            catch
            {
                return new McpRuntimeBuildIdentity(string.Empty, buildId, string.Empty);
            }
        }

        private static IDictionary<string, string> ReadAssemblyMetadata(Assembly assembly)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                foreach (var attribute in assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false))
                {
                    var metadata = attribute as AssemblyMetadataAttribute;
                    if (metadata == null || string.IsNullOrWhiteSpace(metadata.Key)) continue;
                    if (!result.ContainsKey(metadata.Key)) result[metadata.Key] = metadata.Value ?? string.Empty;
                }
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
            return result;
        }

        private static string GetMetadataValue(IDictionary<string, string> metadata, string key)
        {
            string value;
            return metadata != null && metadata.TryGetValue(key, out value) ? value : string.Empty;
        }

        private static string NormalizeGitCommit(string value)
        {
            var candidate = (value ?? string.Empty).Trim();
            return ExactGitCommitRegex.IsMatch(candidate) ? candidate.ToLowerInvariant() : string.Empty;
        }

        private static string NormalizeUtc(string value)
        {
            DateTime parsedUtc;
            if (!DateTime.TryParse(
                    (value ?? string.Empty).Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsedUtc))
                return string.Empty;
            return parsedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
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
