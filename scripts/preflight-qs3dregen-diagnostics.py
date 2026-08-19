#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
ENGINE = ROOT / "src" / "QS3D.Core" / "Services" / "RegenerationEngine.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"


def fail(message: str) -> None:
    print(f"QS3DREGEN diagnostics preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def command_block(source: str, command: str, next_command: str) -> str:
    start_marker = f'[CommandMethod("{command}"'
    end_marker = f'[CommandMethod("{next_command}"'
    start = source.find(start_marker)
    end = source.find(end_marker, start + len(start_marker))
    if start < 0 or end < 0:
        fail(f"cannot isolate {command} command block")
    return source[start:end]


def helper_block(source: str, start_marker: str, end_marker: str, label: str) -> str:
    start = source.find(start_marker)
    end = source.find(end_marker, start + len(start_marker))
    if start < 0 or end < 0:
        fail(f"cannot isolate {label} helper block")
    return source[start:end]


commands = COMMANDS.read_text(encoding="utf-8")
engine = ENGINE.read_text(encoding="utf-8")
inbox = INBOX.read_text(encoding="utf-8")

regen = command_block(commands, "QS3DREGEN", "QS3DSAVE")
refresh = command_block(commands, "QS3DREFRESH", "QS3DTAKEOFF")
helpers = helper_block(
    commands,
    "private static void GuardRegeneration",
    "private static void Guard(Document",
    "GuardRegeneration",
)
generic_guard = helper_block(
    commands,
    "private static void Guard(Document",
    "private static void ReportCommandFailure",
    "generic Guard",
)

if 'GuardRegeneration(doc, "QS3DREGEN"' not in regen:
    fail("QS3DREGEN is not routed through the regeneration-specific failure boundary")
if 'ExistingProjectMutationContext.Require(doc, "Regenerate")' not in regen:
    fail("QS3DREGEN no longer requires an existing mutation-safe project")
if "var count = RegenerateProject(project);" not in regen:
    fail("QS3DREGEN no longer executes live semantic regeneration")
if 'FinalizeCommittedUi(doc, "QS3DREGEN"' not in regen:
    fail("QS3DREGEN post-commit UI work is no longer warning-only")

if 'GuardRegeneration(doc, "QS3DREFRESH"' not in refresh:
    fail("QS3DREFRESH is not routed through the regeneration-specific failure boundary")
if 'ExistingProjectMutationContext.Require(doc, "Refresh")' not in refresh:
    fail("QS3DREFRESH lost the existing-project mutation guard")
if "count = RegenerateProject(project);" not in refresh:
    fail("QS3DREFRESH no longer shares the live regeneration path")
if 'FinalizeCommittedUi(doc, "QS3DREFRESH"' not in refresh:
    fail("QS3DREFRESH can still report post-commit UI failures as regeneration failures")

required_helper_fragments = (
    "catch (CommandUserException expected)",
    "DescribeRegenerationFailure(error)",
    "Semantic regeneration failed and project rollback also failed.",
    "QS3DRELOAD rồi QS3DHEALTH",
    "SafeRegenerationDiagnostic",
    ".Replace('\\r', ' ')",
    ".Replace('\\n', ' ')",
    ".Replace('\\t', ' ')",
    "message.Length > 240",
    "current.GetType().Name",
)
for fragment in required_helper_fragments:
    if fragment not in helpers:
        fail(f"regeneration diagnostic contract missing: {fragment}")

for forbidden in (".StackTrace", ".ToString()", "GetBaseException().ToString"):
    if forbidden in helpers:
        fail(f"regeneration diagnostics must not expose stack traces: {forbidden}")

# Isolate the real generic Guard rather than accepting a matching dead string elsewhere
# in Commands.cs. This prevents the regression check itself from being bypassed while
# keeping unrelated command failure policy deliberately generic.
if 'catch (CommandUserException expected)' not in generic_guard:
    fail("generic Guard no longer preserves expected command-user diagnostics")
if 'catch (System.Exception)' not in generic_guard:
    fail("generic Guard no longer has its unexpected-exception boundary")
if 'ReportCommandFailure(document, operation, "không thể hoàn tất thao tác.")' not in generic_guard:
    fail("generic Guard was broadened; unrelated command error policy must remain unchanged")
if "DescribeRegenerationFailure" in generic_guard or "SafeRegenerationDiagnostic" in generic_guard:
    fail("generic Guard must not inherit regeneration-specific diagnostic exposure")

engine_contract = (
    "ProjectStateSnapshot.Capture(project)",
    "snapshot.Restore(project)",
    "Semantic regeneration failed and project rollback also failed.",
)
for fragment in engine_contract:
    if fragment not in engine:
        fail(f"Core transactional regeneration contract missing: {fragment}")

if "LOCAL-001" not in inbox or "QS3DREGEN" not in inbox or "QS3DREFRESH" not in inbox:
    fail("existing V25 local-runtime qualification queue no longer covers QS3DREGEN/QS3DREFRESH")

print("QS3DREGEN diagnostics preflight passed.")