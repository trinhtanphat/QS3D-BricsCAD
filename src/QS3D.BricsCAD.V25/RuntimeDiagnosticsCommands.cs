using System;
using System.Diagnostics;
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
#if BRICSCAD_V26
        private const int ExpectedRuntimeMajor = 26;
        private const string ExpectedRuntimeLabel = "V26";
#else
        private const int ExpectedRuntimeMajor = 25;
        private const string ExpectedRuntimeLabel = "V25";
#endif

        [CommandMethod("QS3DVERSION", CommandFlags.Modal)]
        public void VersionCheck()
        {
            RuntimeCheck();
        }

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
                var pluginAssemblyVersion = VersionText(pluginAssembly);
                var pluginProductVersion = ProductVersionText(pluginAssembly);
                var coreAssemblyVersion = VersionText(coreAssembly);
                var coreProductVersion = ProductVersionText(coreAssembly);
                var brxVersion = VersionText(brxAssembly);
                var tdVersion = VersionText(tdAssembly);
                var pluginPath = pluginAssembly.Location ?? string.Empty;
                var pluginDirectory = Path.GetDirectoryName(pluginPath) ?? string.Empty;
                var pluginMvid = pluginAssembly.ManifestModule.ModuleVersionId.ToString("D");
                var processId = CurrentProcessId();
                var diskIdentity = ReadDiskIdentity(pluginPath);
                var metadataPath = Path.Combine(pluginDirectory, "PACKAGE-METADATA.json");
                var hasPackageMetadata = File.Exists(metadataPath);
                var metadata = ReadPackageMetadata(metadataPath);
                var hasProject = ProjectContextCoordinator.TryGetReadOnly(document, out var project);

                var expectedRuntime = Major(brxAssembly) == ExpectedRuntimeMajor && Major(tdAssembly) == ExpectedRuntimeMajor;
                var x64Runtime = Environment.Is64BitProcess;
                var diskVersionMatches =
                    diskIdentity.Exists &&
                    string.IsNullOrWhiteSpace(diskIdentity.Error) &&
                    !string.IsNullOrWhiteSpace(diskIdentity.ProductVersion) &&
                    ProductVersionsEqual(pluginProductVersion, diskIdentity.ProductVersion);
                var packageProductVersionMatches = !hasPackageMetadata ||
                    (!string.IsNullOrWhiteSpace(metadata.ProductVersion) &&
                     ProductVersionsEqual(metadata.ProductVersion, pluginProductVersion));
                var packageAssemblyVersionMatches = !hasPackageMetadata ||
                    (!string.IsNullOrWhiteSpace(metadata.AssemblyVersion) &&
                     AssemblyVersionsEqual(metadata.AssemblyVersion, pluginAssemblyVersion));
                var packageVersionMatches = packageProductVersionMatches && packageAssemblyVersionMatches;
                var signatureMetadataRecorded =
                    string.Equals(metadata.SignatureStatus, "Valid", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(metadata.SignerThumbprint);

                document.Editor.WriteMessage("\nQS3D Runtime Check");
                document.Editor.WriteMessage(
                    "\n  Running product: " + pluginProductVersion +
                    " • Assembly: " + pluginAssemblyVersion +
                    " • Core product: " + coreProductVersion +
                    " • Core assembly: " + coreAssemblyVersion);
                document.Editor.WriteMessage("\n  Loaded DLL: " + (string.IsNullOrWhiteSpace(pluginPath) ? "<unknown>" : pluginPath));
                document.Editor.WriteMessage("\n  Process: PID " + processId + " • MVID: " + pluginMvid);

                if (!diskIdentity.Exists)
                {
                    document.Editor.WriteMessage(
                        "\n  On-disk DLL: MISSING. This process still has QS3D loaded, but the original DLL path no longer exists.");
                    document.Editor.WriteMessage(
                        "\n  STALE PROCESS: close every BricsCAD process before replacing/deleting a QS3D folder, then start BricsCAD again and NETLOAD/install the intended build.");
                }
                else if (!string.IsNullOrWhiteSpace(diskIdentity.Error))
                {
                    document.Editor.WriteMessage("\n  On-disk DLL: unreadable identity • " + diskIdentity.Error);
                }
                else
                {
                    document.Editor.WriteMessage(
                        "\n  On-disk DLL: product " + EmptyAsUnknown(diskIdentity.ProductVersion) +
                        " • file " + EmptyAsUnknown(diskIdentity.FileVersion));
                    if (!diskVersionMatches)
                    {
                        document.Editor.WriteMessage(
                            "\n  STALE PROCESS: this BricsCAD process is running " + pluginProductVersion +
                            " but the DLL currently stored at the same path is " + EmptyAsUnknown(diskIdentity.ProductVersion) + ".");
                        document.Editor.WriteMessage(
                            "\n  .NET does not hot-reload an already NETLOADed QS3D assembly. Close ALL bricscad.exe processes, reopen BricsCAD, then load the new build.");
                    }
                }

                document.Editor.WriteMessage("\n  BrxMgd: " + brxVersion + " • TD_Mgd: " + tdVersion);
                document.Editor.WriteMessage("\n  Runtime: " + (expectedRuntime ? ExpectedRuntimeLabel : "NOT " + ExpectedRuntimeLabel) + " • " + (x64Runtime ? "x64" : "NOT x64"));
                document.Editor.WriteMessage(hasProject
                    ? "\n  Project: " + project.Elements.Count + " element(s) • " + project.Families.Count + " family/families"
                    : "\n  Project: not loaded/persisted • runtime diagnostics remain read-only and do not create project state.");

                if (hasPackageMetadata)
                {
                    document.Editor.WriteMessage(
                        "\n  Package metadata: product=" + (packageProductVersionMatches ? "OK" : "MISMATCH") +
                        " • assembly=" + (packageAssemblyVersionMatches ? "OK" : "MISMATCH") +
                        " • signature metadata=" + (signatureMetadataRecorded ? "recorded" : "not recorded"));
                    document.Editor.WriteMessage(
                        "\n  Package product: " + EmptyAsUnknown(metadata.ProductVersion) +
                        " • package assembly: " + EmptyAsUnknown(metadata.AssemblyVersion));
                    if (!string.IsNullOrWhiteSpace(metadata.SignerThumbprint))
                        document.Editor.WriteMessage("\n  Recorded signer thumbprint: " + metadata.SignerThumbprint);
                    document.Editor.WriteMessage("\n  Authenticode: metadata only here; cryptographic publisher/timestamp verification belongs to the signed installer/release gate.");
                }
                else
                {
                    document.Editor.WriteMessage("\n  Package metadata: not found beside plugin (manual NETLOAD/dev layout).");
                }

                var ok = expectedRuntime && x64Runtime && packageVersionMatches && diskVersionMatches;
                var summary = ok
                    ? "QS3DRUNTIMECHECK PASS: running product, on-disk DLL, package identity, adapter runtime and architecture are consistent."
                    : "QS3DRUNTIMECHECK FAIL: stale process or runtime/package identity mismatch detected. Close all BricsCAD processes before replacing QS3D binaries; do not qualify this installation for release.";
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

        private sealed class DiskIdentity
        {
            public bool Exists { get; set; }
            public string ProductVersion { get; set; } = string.Empty;
            public string FileVersion { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
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

        private static DiskIdentity ReadDiskIdentity(string path)
        {
            var result = new DiskIdentity();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return result;

            result.Exists = true;
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                result.ProductVersion = versionInfo.ProductVersion ?? string.Empty;
                result.FileVersion = versionInfo.FileVersion ?? string.Empty;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }
            return result;
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

        private static int CurrentProcessId()
        {
            using (var process = Process.GetCurrentProcess())
                return process.Id;
        }

        private static int Major(Assembly assembly) => assembly.GetName().Version?.Major ?? -1;

        private static string VersionText(Assembly assembly) =>
            assembly.GetName().Version?.ToString() ?? "unknown";

        private static string ProductVersionText(Assembly assembly)
        {
            foreach (var attribute in assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))
            {
                var informational = attribute as AssemblyInformationalVersionAttribute;
                if (informational != null && !string.IsNullOrWhiteSpace(informational.InformationalVersion))
                    return informational.InformationalVersion.Trim();
            }
            return VersionText(assembly);
        }

        private static bool ProductVersionsEqual(string left, string right) =>
            string.Equals(NormalizeProductVersion(left), NormalizeProductVersion(right), StringComparison.OrdinalIgnoreCase);

        private static bool AssemblyVersionsEqual(string left, string right) =>
            string.Equals(NormalizeAssemblyVersion(left), NormalizeAssemblyVersion(right), StringComparison.OrdinalIgnoreCase);

        private static string NormalizeProductVersion(string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            var buildMetadata = normalized.IndexOf('+');
            return buildMetadata >= 0 ? normalized.Substring(0, buildMetadata) : normalized;
        }

        private static string NormalizeAssemblyVersion(string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            Version version;
            if (!Version.TryParse(normalized, out version) || version == null) return normalized;
            return version.Major + "." +
                   version.Minor + "." +
                   Math.Max(0, version.Build) + "." +
                   Math.Max(0, version.Revision);
        }

        private static string EmptyAsUnknown(string value) =>
            string.IsNullOrWhiteSpace(value) ? "<unknown>" : value.Trim();
    }
}
