#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UPDATES = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates"

FILES = {
    "UpdateCenterWindow.cs": (
        "private UpdateCheckResult? _result;",
        "internal void Apply(UpdateCheckResult? result)",
        "private static UpdateCenterWindow? _window;",
        "internal static void Show(UpdateCheckResult? result = null, bool activate = true)",
        "if (ReferenceEquals(_window, window)) _window = null;",
    ),
    "UpdateCoordinator.cs": (
        "UpdateReleaseInfo? release",
        "internal UpdateReleaseInfo? Release { get; }",
        "private Dispatcher? _dispatcher;",
        "private Task<UpdateCheckResult>? _inFlight;",
        "internal event EventHandler<UpdateCheckResult>? StateChanged;",
        "internal event EventHandler<UpdateCheckResult>? AutomaticUpdateFound;",
        "var release = fresh.Release;",
        "if (!fresh.CanAutoInstall || release == null)",
    ),
    "SemanticReleaseVersion.cs": (
        "internal static bool TryParse(string? value, out SemanticReleaseVersion? version)",
        "internal static SemanticReleaseVersion FromRunningVersion(string? informationalVersion, Version? assemblyVersion)",
        "return new SemanticReleaseVersion(major, minor, patch, Array.Empty<string>(), text);",
        "public int CompareTo(SemanticReleaseVersion? other)",
    ),
    "SecureUpdateLauncher.cs": (
        "internal static bool TrySchedule(UpdateReleaseInfo? release, out string error)",
        "var manifestUri = release?.ManifestUri;",
        "bricscadPath = process.MainModule?.FileName ?? string.Empty;",
        "manifestUri.AbsoluteUri",
        "private static string NormalizeThumbprint(string? value)",
        "private static string PsLiteral(string? value)",
    ),
    "GitHubReleaseClient.cs": (
        "Uri? manifestUri",
        "internal Uri? ManifestUri { get; }",
        "SemanticReleaseVersion.TryParse(release.TagName, out var version) || version == null",
        "private static bool TryGitHubUri(string? value, out Uri? uri)",
        "private static string NormalizeNotes(string? value)",
        "public GitHubAssetDto?[]? Assets { get; set; }",
        "public string? BrowserDownloadUrl { get; set; }",
    ),
}


def main() -> int:
    errors = []
    texts = {}
    for name, required in FILES.items():
        path = UPDATES / name
        if not path.is_file():
            errors.append("missing updater source: " + name)
            continue
        text = path.read_text(encoding="utf-8")
        texts[name] = text
        for token in required:
            if token not in text:
                errors.append(name + " missing nullable-flow contract: " + token)
        for forbidden in ("#nullable disable", "null!"):
            if forbidden in text:
                errors.append(name + " must model optional state instead of suppressing nullable analysis: " + forbidden)

    client = texts.get("GitHubReleaseClient.cs", "")
    if "[DataMember(Name = \"tag_name\")] public string TagName" in client:
        errors.append("GitHub JSON DTOs must not claim untrusted optional strings are always present")
    coordinator = texts.get("UpdateCoordinator.cs", "")
    if "SecureUpdateLauncher.TrySchedule(fresh.Release" in coordinator:
        errors.append("update scheduling must use the same locally validated non-null release snapshot")

    if errors:
        for error in errors:
            print("ERROR:", error)
        print("FAILED with %d error(s)." % len(errors))
        return 1

    print(
        "PASS: updater UI/state, release parsing and security handoff model genuinely optional data explicitly, "
        "retain a single validated release/manifest snapshot, and do not suppress nullable analysis."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
