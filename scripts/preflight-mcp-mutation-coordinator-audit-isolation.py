from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")

helper = re.search(
    r"private static void SafeAudit\(Action<string>\? audit, string message\)\s*\{\s*if \(audit == null\) return;\s*try \{ audit\(message\); \}\s*catch \{ \}\s*\}",
    text,
    re.S,
)
if not helper:
    raise SystemExit("FAIL: fail-soft SafeAudit helper is missing")

# Diagnostics are optional and must not control process-global writer state. Direct
# Action<string>.Invoke calls in this coordinator reintroduce half-published lease,
# mutation, modal, native-command, or cleanup state when an audit sink throws.
direct_patterns = [
    r"(?<!SafeAudit\()audit\?\.Invoke\(",
    r"\.Audit\?\.Invoke\(",
    r"_audit\?\.Invoke\(",
]
for pattern in direct_patterns:
    if re.search(pattern, text):
        raise SystemExit(f"FAIL: direct coordinator audit callback remains: {pattern}")

required_messages = [
    "writer lease acquired",
    "writer lease release deferred",
    "writer lease released",
    "writer mutation entered",
    "interactive modal entered",
    "native command queued",
    "native command handler cleanup failed during reset",
    "native command handler rollback failed",
    "native command barrier armed",
    "native command started",
    "native command terminal handler cleanup failed",
    "native command reservation cleanup failed",
    "interactive modal exited",
    "writer mutation exited",
]
for message in required_messages:
    if message not in text:
        raise SystemExit(f"FAIL: lifecycle diagnostic message disappeared: {message}")

# Finally-bound writer release remains mandatory for modal scope cleanup. MutationScope
# must restore logical operation identity before releasing the gate; SafeAudit may occur
# between those two only because it is guaranteed fail-soft.
modal = re.search(r"private sealed class InteractiveModalScope.*?public void Dispose\(\)\s*\{(?P<body>.*?)\n\s*\}\n\s*\}", text, re.S)
if not modal or "finally" not in modal.group("body") or "MutationGate.Release();" not in modal.group("body"):
    raise SystemExit("FAIL: interactive-modal gate release is no longer finally-bound")

mutation = re.search(r"private sealed class MutationScope.*?public void Dispose\(\)\s*\{(?P<body>.*?)\n\s*\}\n\s*\}", text, re.S)
if not mutation:
    raise SystemExit("FAIL: MutationScope.Dispose body not found")
mutation_body = mutation.group("body")
for token in ["CurrentOperationId.Value = _previousOperationId", "SafeAudit(_audit, \"writer mutation exited\")", "MutationGate.Release();"]:
    if token not in mutation_body:
        raise SystemExit(f"FAIL: mutation-scope cleanup contract missing: {token}")

print("PASS: mutation coordinator diagnostics are fail-soft across writer state and cleanup boundaries")
