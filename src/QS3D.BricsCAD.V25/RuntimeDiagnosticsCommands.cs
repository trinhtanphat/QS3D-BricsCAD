using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RuntimeDiagnosticsCommands
    {
        [CommandMethod("QS3DRUNTIMECHECK", CommandFlags.Modal)]
        public void RuntimeCheck()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var pluginAssembly = typeof(RuntimeDiagnosticsCommands).Assembly;
                var coreAssembly = typeof(ProjectState).Assembly;
                var brxAssembly = typeof(Application).Assembly;
                var tdAssembly = typeof(Database).Assembly;
                var pluginVersion = VersionText(pluginAssembly);
                var coreVersion = VersionText(coreAssembly);
                var brxVersion = VersionText(brxAssembly);
                var tdVersion = VersionText(tdAssembly);
                var pluginDirectory = Path.GetDirectoryName(pluginAssembly.Location) ?? string.Empty;
                var metadataPath = Path.Combine(pluginDirectory, "PACKAGE-METADATA.json");
                var metadata = ReadPackageMetadata(metadataPath);
                var project = ProjectContextCoordinator.GetOrCreate(document);

                var v25Runtime = Major(brxAssembly) == 25 && Major(tdAssembly) == 25;
                var x64Runtime = Environment.Is64BitProcess;
                var packageVersionMatches = string.IsNullOrWhiteSpace(metadata.Version) ||
                    string.Equals(NormalizeVersion(metadata.Version), NormalizeVersion(pluginVersion), StringComparison.OrdinalIgnoreCase);
                var packageSigned = string.Equals(metadata.SignatureStatus, "Valid", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(metadata.SignerThumbprint);

                document.Editor.WriteMessage("\nQS3D Runtime Check");
                document.Editor.WriteMessage("\n  Plugin: " + pluginVersion + " • Core: " + coreVersion);
                document.Editor.WriteMessage("\n  BrxMgd: " + brxVersion + " • TD_Mgd: " + tdVersion);
                document.Editor.WriteMessage("\n  Runtime: " + (v25Runtime ? "V25" : "NOT V25") + " • " + (x64Runtime ? "x64" : "NOT x64"));
                document.Editor.WriteMessage("\n  Project: " + project.Elements.Count + " element(s) • " + project.Families.Count + " family/families");
                if (File.Exists(metadataPath))
                {
                    document.Editor.WriteMessage("\n  Package metadata: " + (packageVersionMatches ? "version OK" : "VERSION MISMATCH") +
                        " • signature=" + (packageSigned ? "signed" : "unsigned/not recorded"));
                    if (!string.IsNullOrWhiteSpace(metadata.SignerThumbprint))
                        document.Editor.WriteMessage("\n  Signer thumbprint: " + metadata.SignerThumbprint);
                }
                else
                {
                    document.Editor.WriteMessage("\n  Package metadata: not found beside plugin (manual NETLOAD/dev layout).");
                }

                var ok = v25Runtime && x64Runtime && packageVersionMatches;
                var summary = ok
                    ? "QS3DRUNTIMECHECK PASS: adapter/runtime architecture is consistent. Run QS3DRELEASECHECK plus the licensed V25 scenario suite for release qualification."
                    : "QS3DRUNTIMECHECK FAIL: runtime/package mismatch detected; do not qualify this installation for release.";
                document.Editor.WriteMessage("\n" + summary);
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DRUNTIMECHECK error: " + ex.Message);
            }
        }

        private sealed class PackageMetadata
        {
            public string Version { get; set; } = string.Empty;
            public string SignatureStatus { get; set; } = string.Empty;
            public string SignerThumbprint { get; set; } = string.Empty;
        }

        private static PackageMetadata ReadPackageMetadata(string path)
        {
            if (!File.Exists(path)) return new PackageMetadata();
            var text = File.ReadAllText(path);
            return new PackageMetadata
            {
                Version = JsonString(text, "version"),
                SignatureStatus = JsonString(text, "pluginSignatureStatus"),
                SignerThumbprint = FirstNonEmpty(
                    JsonString(text, "signedPayloadSignerThumbprint"),
                    JsonString(text, "pluginSignerThumbprint"))
            };
        }

        private static string JsonString(string json, string property)
        {
            var match = Regex.Match(
                json,
                "\\\"" + Regex.Escape(property) + "\\\"\\s*:\\s*\\\"(?<value>[^\\\"]*)\\\"",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
        }

        private static string FirstNonEmpty(string first, string second) =>
            !string.IsNullOrWhiteSpace(first) ? first : second;

        private static int Major(Assembly assembly) => assembly.GetName().Version?.Major ?? -1;

        private static string VersionText(Assembly assembly) =>
            assembly.GetName().Version?.ToString() ?? "unknown";

        private static string NormalizeVersion(string value)
        {
            if (!Version.TryParse(value, out var version)) return value.Trim();
            return version.Major + "." + version.Minor + "." + Math.Max(0, version.Build);
        }
    }
}
