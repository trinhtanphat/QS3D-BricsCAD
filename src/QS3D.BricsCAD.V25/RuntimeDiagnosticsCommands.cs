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
                var hasProject = ProjectContextCoordinator.TryGetReadOnly(document, out var project);

                var v25Runtime = Major(brxAssembly) == 25 && Major(tdAssembly) == 25;
                var x64Runtime = Environment.Is64BitProcess;
                var packageVersionMatches = string.IsNullOrWhiteSpace(metadata.AssemblyVersion) ||
                    string.Equals(NormalizeVersion(metadata.AssemblyVersion), NormalizeVersion(pluginVersion), StringComparison.OrdinalIgnoreCase);
                var signatureMetadataRecorded =
                    string.Equals(metadata.SignatureStatus, "Valid", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(metadata.SignerThumbprint);

                document.Editor.WriteMessage("\nQS3D Runtime Check");
                document.Editor.WriteMessage("\n  Plugin assembly: " + pluginVersion + " • Core assembly: " + coreVersion);
                if (!string.IsNullOrWhiteSpace(metadata.ProductVersion))
                    document.Editor.WriteMessage("\n  Product version: " + metadata.ProductVersion);
                document.Editor.WriteMessage("\n  BrxMgd: " + brxVersion + " • TD_Mgd: " + tdVersion);
                document.Editor.WriteMessage("\n  Runtime: " + (v25Runtime ? "V25" : "NOT V25") + " • " + (x64Runtime ? "x64" : "NOT x64"));
                document.Editor.WriteMessage(hasProject
                    ? "\n  Project: " + project.Elements.Count + " element(s) • " + project.Families.Count + " family/families"
                    : "\n  Project: not loaded/persisted • runtime diagnostics remain read-only and do not create project state.");
                if (File.Exists(metadataPath))
                {
                    document.Editor.WriteMessage("\n  Package metadata: " + (packageVersionMatches ? "assembly version OK" : "ASSEMBLY VERSION MISMATCH") +
                        " • signature metadata=" + (signatureMetadataRecorded ? "recorded" : "not recorded"));
                    if (!string.IsNullOrWhiteSpace(metadata.SignerThumbprint))
                        document.Editor.WriteMessage("\n  Recorded signer thumbprint: " + metadata.SignerThumbprint);
                    document.Editor.WriteMessage("\n  Authenticode: metadata only here; cryptographic publisher/timestamp verification belongs to the signed installer/release gate.");
                }
                else
                {
                    document.Editor.WriteMessage("\n  Package metadata: not found beside plugin (manual NETLOAD/dev layout).");
                }

                var ok = v25Runtime && x64Runtime && packageVersionMatches;
                var summary = ok
                    ? "QS3DRUNTIMECHECK PASS: adapter/runtime architecture is consistent. Run QS3DRELEASECHECK plus the licensed V25 scenario suite; use the signed installer/release gate for Authenticode verification."
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
            public string ProductVersion { get; set; } = string.Empty;
            public string AssemblyVersion { get; set; } = string.Empty;
            public string SignatureStatus { get; set; } = string.Empty;
            public string SignerThumbprint { get; set; } = string.Empty;
        }

        private static PackageMetadata ReadPackageMetadata(string path)
        {
            if (!File.Exists(path)) return new PackageMetadata();
            var text = File.ReadAllText(path);
            return new PackageMetadata
            {
                ProductVersion = JsonString(text, "productVersion"),
                AssemblyVersion = JsonString(text, "version"),
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
            if (!Version.TryParse(value, out var version) || version == null) return value.Trim();
            return version.Major + "." + version.Minor + "." + Math.Max(0, version.Build);
        }
    }
}
