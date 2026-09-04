#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
PROVENANCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpRuntimeBuildProvenance.cs"
PROJECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "QS3D.BricsCAD.V25.csproj"


def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except OSError as exc:
        raise SystemExit(f"ERROR: unable to read {path.relative_to(ROOT)}: {exc}")


def require(condition: bool, message: str, failures: list[str]) -> None:
    if not condition:
        failures.append(message)


server = read(SERVER)
provenance = read(PROVENANCE)
project = read(PROJECT)
failures: list[str] = []

connector_match = re.search(
    r'if \(string\.Equals\(tool, "connector_info".*?\n\s*}\n\s*if \(string\.Equals\(tool, "cad_writer_acquire"',
    server,
    re.DOTALL,
)
require(connector_match is not None, "connector_info block could not be bounded before cad_writer_acquire", failures)
if connector_match is not None:
    connector = connector_match.group(0)
    require(
        '\\"structuredContent\\":true' not in connector,
        "connector_info business payload must not reuse reserved MCP result key structuredContent",
        failures,
    )
    require(
        '\\"supportsStructuredContent\\":true' in connector,
        "connector_info must expose the capability as supportsStructuredContent",
        failures,
    )

tool_success_match = re.search(
    r'private static string ToolSuccess\(string jsonValue\).*?\n\s*}\n\s*private static bool LooksLikeJsonValue',
    server,
    re.DOTALL,
)
require(tool_success_match is not None, "ToolSuccess block could not be bounded", failures)
if tool_success_match is not None:
    require(
        '\\"structuredContent\\":{\\"data\\":' in tool_success_match.group(0),
        "ToolSuccess must preserve the canonical outer MCP structuredContent envelope",
        failures,
    )

require(
    'AssemblyMetadataAttribute' in provenance and '"QS3D.BuildSha"' in provenance and '"QS3D.BuildUtc"' in provenance,
    "runtime provenance must read embedded QS3D.BuildSha/QS3D.BuildUtc assembly metadata",
    failures,
)
require(
    '<AssemblyMetadata Include="QS3D.BuildSha"' in project,
    "V25 project must emit QS3D.BuildSha assembly metadata when an exact revision is available",
    failures,
)
require(
    '<AssemblyMetadata Include="QS3D.BuildUtc"' in project,
    "V25 project must emit QS3D.BuildUtc assembly metadata",
    failures,
)
require(
    "$(GITHUB_SHA)" in project and "$(SourceRevisionId)" in project,
    "V25 project must bind embedded SHA to CI or MSBuild source revision identity",
    failures,
)

if failures:
    for failure in failures:
        print(f"ERROR: {failure}")
    sys.exit(1)

print("MCP connector/runtime provenance preflight passed.")
