#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

errors = []


def require(pattern: str, message: str) -> None:
    if re.search(pattern, text, flags=re.MULTILINE | re.DOTALL) is None:
        errors.append(message)


# Prompt-bearing Direct Draw commands must bind their interaction to the exact
# native Database generation, not merely the managed Document wrapper. Guard is
# the common command boundary and must capture the native identity before any
# host-pumping point/double prompt executes.
require(
    r"Guard\s*\(\s*Document\s+document\s*,\s*string\s+operation\s*,\s*Action<IntPtr>\s+action\s*\).*?GetNativeDatabaseIdentity\(\s*document\s*\).*?action\(\s*nativeDatabaseIdentity\s*\)",
    "Direct Draw Guard must capture native database identity before invoking prompt-bearing command work",
)
require(
    r"IsActiveDocumentGeneration\s*\(\s*Document\s+document\s*,\s*IntPtr\s+nativeDatabaseIdentity\s*\).*?MdiActiveDocument.*?document\.Database.*?UnmanagedObject\s*==\s*nativeDatabaseIdentity",
    "Direct Draw must compare both managed active Document and exact native database identity",
)

# The post-prompt freshness fence must receive the pre-prompt native identity
# and reject same-wrapper database replacement before geometry can be handed to
# ExecuteDirect.
require(
    r"RequirePromptContextUnchanged\s*\(\s*Document\s+document\s*,\s*IntPtr\s+nativeDatabaseIdentity\s*,.*?RequireActiveDocumentGeneration\(\s*document\s*,\s*nativeDatabaseIdentity",
    "prompt freshness must reject a successor native database generation",
)
require(
    r"RequirePromptContextUnchanged\(\s*document\s*,\s*nativeDatabaseIdentity\s*,\s*promptUnit\s*,\s*promptUcs",
    "prompt-bearing Direct Draw commands must pass their pre-prompt native identity into freshness checks",
)

# ExecuteDirect owns semantic/native mutation. It must consume the same captured
# identity and fence it before project mutation/source creation rather than
# recapturing a successor generation after prompts have completed.
require(
    r"ExecuteDirect\s*\(\s*Document\s+document\s*,.*?IntPtr\s+nativeDatabaseIdentity\s*=.*?RequireActiveDocumentGeneration\(\s*document\s*,\s*nativeDatabaseIdentity.*?ResolveForMutation",
    "ExecuteDirect must fence the captured generation before project/source mutation",
)
require(
    r"ExecuteDirect\(.*?projectPreview\s*,\s*nativeDatabaseIdentity\s*:\s*nativeDatabaseIdentity\s*\)",
    "Direct Draw command paths must hand the pre-prompt native identity into ExecuteDirect",
)

# Error cleanup mutates CAD and semantic state. CAD cleanup itself is a native /
# re-entrant boundary, so semantic snapshot restore must freshly revalidate the
# captured generation after cleanup. Forget(document) must revalidate again
# after restore so neither operation can mutate successor state.
require(
    r"if\s*\(generationIsCurrent\)\s*\{\s*try\s*\{\s*EraseDirectDrawCad\(.*?\}\s*catch.*?if\s*\(\s*IsActiveDocumentGeneration\(\s*document\s*,\s*nativeDatabaseIdentity\s*\)\s*\)\s*\{\s*try\s*\{\s*rollback\.Restore\(project\);\s*\}\s*catch.*?\}\s*\}",
    "Direct Draw rollback restore must freshly revalidate native generation after CAD cleanup",
)
require(
    r"rollback\.Restore\(project\).*?if\s*\(\s*!projectExistedBeforeAuthoring\s*&&\s*IsActiveDocumentGeneration\(\s*document\s*,\s*nativeDatabaseIdentity\s*\)\s*\)\s*ProjectContextCoordinator\.Forget\(document\)",
    "Direct Draw cleanup must freshly revalidate native generation after rollback before forgetting document project state",
)

# Native/semantic commit is already durable before UI finalization. Success UI
# must be best-effort and generation-bound: a successor DB must not receive
# selection/regen/status, and stale UI must not turn committed work into failure.
require(
    r"FinalizeUi\s*\(\s*Document\s+document\s*,\s*IntPtr\s+nativeDatabaseIdentity\s*,.*?IsActiveDocumentGeneration\(\s*document\s*,\s*nativeDatabaseIdentity\s*\).*?PaletteCoordinator\.RefreshProject\(\).*?SetImpliedSelection.*?Regen",
    "post-commit Direct Draw UI must be bound to the committed native database generation",
)
require(
    r"catch\s*\(Exception\)\s*\{\s*if\s*\(IsActiveDocumentGeneration\(\s*document\s*,\s*nativeDatabaseIdentity\s*\)\).*?ReportOperationFailure",
    "operation failure publication must not leak into a successor native database generation",
)

if errors:
    print("ERROR: V25 Direct Draw document-generation preflight failed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("V25 Direct Draw document-generation preflight passed.")