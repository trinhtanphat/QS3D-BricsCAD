#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


def command_block(source, command_name, next_command_name):
    start = source.find('[CommandMethod("' + command_name + '"')
    end = source.find('[CommandMethod("' + next_command_name + '"', start)
    if start < 0 or end < 0:
        errors.append("cannot isolate command block: " + command_name)
        return ""
    return source[start:end]


if not SOURCE.is_file():
    errors.append("missing Commands.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

if not INBOX.is_file():
    errors.append("missing docs/LOCAL-AGENT-INBOX.md")
    inbox = ""
else:
    inbox = INBOX.read_text(encoding="utf-8")

regen = command_block(source, "QS3DREGEN", "QS3DSAVE")
refresh = command_block(source, "QS3DREFRESH", "QS3DTAKEOFF")

require(regen, 'ExistingProjectMutationContext.Require(doc, "Regenerate")', "QS3DREGEN existing-project mutation boundary")
require(regen, "RegenerateProject(project)", "QS3DREGEN regeneration")
if "ProjectContextCoordinator.GetOrCreate" in regen:
    errors.append("QS3DREGEN must not create/cache a replacement project")

for token, label in [
    ("ProjectContextCoordinator.TryGetReadOnly(doc, out _)", "QS3DREFRESH read-only project probe"),
    ('ExistingProjectMutationContext.Require(doc, "Refresh")', "QS3DREFRESH existing-project mutation boundary"),
    ("RegenerateProject(project)", "QS3DREFRESH regeneration"),
    ("PaletteCoordinator.RefreshAll()", "QS3DREFRESH UI refresh"),
]:
    require(refresh, token, label)
if "ProjectContextCoordinator.GetOrCreate" in refresh:
    errors.append("QS3DREFRESH must not create/cache a replacement project")

probe = refresh.find("ProjectContextCoordinator.TryGetReadOnly(doc, out _)")
<<<<<<< HEAD
bind = refresh.find('ExistingProjectMutationContext.Require(doc, "Refresh")')
regenerate = refresh.find("RegenerateProject(project)")
=======
bind = refresh.find('ExistingProjectMutationContext.Require(doc, "Refresh")', probe)
regenerate = refresh.find("RegenerateProject(project)", bind)
>>>>>>> origin/main
refresh_ui = refresh.find("PaletteCoordinator.RefreshAll()", regenerate)
if min(probe, bind, regenerate, refresh_ui) < 0 or not probe < bind < regenerate < refresh_ui:
    errors.append("QS3DREFRESH must probe read-only state, bind canonical state before mutation, then refresh UI")

# The command intentionally has an earlier UI-only branch when there is no active document.
# That branch must remain mutation-free and must not be mistaken for the post-regeneration RefreshAll.
no_doc = refresh.find("if (doc == null) { PaletteCoordinator.RefreshAll(); return; }")
guard = refresh.find('Guard(doc, "QS3DREFRESH"')
if min(no_doc, guard) < 0 or not no_doc < guard:
    errors.append("QS3DREFRESH must retain a UI-only no-active-document branch before project lifecycle work")

for token, label in [
    ("LOCAL-001 — exact V25 build/load baseline", "canonical local baseline item"),
    ("QS3DREGEN", "local regeneration command scenario"),
    ("QS3DREFRESH", "local refresh command scenario"),
    ("absent-sidecar refusal/no-new-project result", "no replacement project local evidence"),
]:
    require(inbox, token, label)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DREGEN binds canonical existing project state; QS3DREFRESH keeps its no-document UI-only branch, stays non-creating without a project, and binds canonical state before optional regeneration; LOCAL-001 owns native V25 proof.")
