#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
STATUS = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
PROVENANCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpRuntimeBuildProvenance.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP runtime provenance preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def between(text: str, start: str, end: str) -> str:
    start_index = text.find(start)
    if start_index < 0:
        return ""
    end_index = text.find(end, start_index + len(start))
    return text[start_index:] if end_index < 0 else text[start_index:end_index]


server = SERVER.read_text(encoding="utf-8")
status = STATUS.read_text(encoding="utf-8")
if not PROVENANCE.exists():
    fail("missing McpRuntimeBuildProvenance.cs")
provenance = PROVENANCE.read_text(encoding="utf-8")

connector_block = between(
    server,
    'if (string.Equals(tool, "connector_info", StringComparison.Ordinal))',
    "var runtimeResult = McpCadAgentRuntime.Call(tool, arguments);",
)
tool_success_block = between(server, "private static string ToolSuccess", "private static bool LooksLikeJsonValue")
status_block = between(status, "private static string BuildStatusJson()", "private static string BuildActiveDocumentJson()")

if "return ToolSuccess(" not in connector_block:
    fail("connector_info must return the normal object ToolSuccess envelope")
if '"structuredContent"' not in tool_success_block or '"data"' not in tool_success_block:
    fail("ToolSuccess must expose structuredContent.data")
if "return true" in connector_block.lower() or "return false" in connector_block.lower():
    fail("connector_info must not collapse to a scalar boolean contract")

for field in ("buildSha", "buildId", "buildUtc"):
    if field not in status_block:
        fail(f"qs3d_status is missing {field}")
if "McpRuntimeBuildProvenance.Current" not in status_block:
    fail("qs3d_status must use the bounded runtime provenance helper")

requirements = {
    "package metadata file": "PACKAGE-METADATA.json",
    "source commit metadata": "gitCommit",
    "package generation metadata": "generatedUtc",
    "loaded module identity": "ModuleVersionId",
    "bounded metadata size": "MaxMetadataBytes",
    "40-hex source commit validation": "GitCommitRegex",
    "round-trip UTC validation": "DateTimeStyles.RoundtripKind",
}
for label, token in requirements.items():
    if token not in provenance:
        fail(f"provenance helper is missing {label}: {token}")

for forbidden in (
    "Environment.CurrentDirectory",
    "Directory.GetFiles(",
    "Directory.EnumerateFiles(",
    "Process.Start(",
    "cmd.exe",
    "powershell",
):
    if forbidden.lower() in provenance.lower():
        fail(f"provenance helper contains forbidden broad/local execution surface: {forbidden}")

print("MCP runtime provenance preflight passed; connector_info stays structured and qs3d_status exposes exact successor identity fields.")
