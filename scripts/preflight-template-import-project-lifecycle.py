#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "TemplateCommands.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


if not SOURCE.is_file():
    errors.append("missing TemplateCommands.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

if not INBOX.is_file():
    errors.append("missing docs/LOCAL-AGENT-INBOX.md")
    inbox = ""
else:
    inbox = INBOX.read_text(encoding="utf-8")

start = source.find('[CommandMethod("QS3DTEMPLATEIMPORT"')
end = source.find("private static void FinalizeExportUi", start)
if start < 0 or end < 0:
    errors.append("cannot isolate QS3DTEMPLATEIMPORT source block")
    block = ""
else:
    block = source[start:end]

confirm = block.find("MessageBox.Show(confirmText")
bind = block.find('ExistingProjectMutationContext.Require(doc, "Template Import")')
snapshot = block.find("ProjectStateSnapshot.Capture(project)")
apply = block.find("store.Apply(project, profile)")
regen = block.find("RegenerateDirty(project)")
restore = block.find("rollback.Restore(project)")

if min(confirm, bind, snapshot, apply, regen, restore) < 0:
    errors.append("Template Import missing confirm/existing-project/snapshot/apply/regen/restore lifecycle token")
elif not confirm < bind < snapshot < apply < regen < restore:
    errors.append("Template Import must confirm before binding canonical existing project, snapshot before mutation, then rollback on failure")

if "ProjectContextCoordinator.GetOrCreate(doc)" in block:
    errors.append("Template Import must not create/cache a replacement project")

for token, label in [
    ('Guard(doc, "QS3DTEMPLATEIMPORT"', "command guard"),
    ('ExistingProjectMutationContext.Require(doc, "Template Import")', "existing-project mutation boundary"),
    ("Template import failed and project rollback also failed.", "rollback failure aggregation"),
]:
    require(block, token, label)

for token, label in [
    ("LOCAL-001 — exact V25 build/load baseline", "canonical local baseline item"),
    ("true writes must bind the canonical same-ProjectId project", "canonical true-write local scenario"),
    ("absent-sidecar refusal/no-new-project result", "no replacement project local evidence"),
]:
    require(inbox, token, label)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Template Import confirms first, binds only canonical existing project state, snapshots before mutation, rolls back on failure, and remains covered by the canonical LOCAL-001 V25 qualification invariant.")
