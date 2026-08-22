#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
errors = []

recognition = ADAPTER / "UI/RecognitionWindow.xaml.cs"
revision = ADAPTER / "UI/RevisionWindow.xaml.cs"
health = ADAPTER / "UI/ModelHealthWindow.xaml.cs"
review_commands = ADAPTER / "ReviewCommands.cs"
health_all = ADAPTER / "HealthAllCommands.cs"

required_files = [recognition, revision, health, review_commands, health_all]
for path in required_files:
    if not path.is_file():
        errors.append("missing required modeless binding source: " + str(path.relative_to(ROOT)))

if not errors:
    recognition_text = recognition.read_text(encoding="utf-8")
    revision_text = revision.read_text(encoding="utf-8")
    health_text = health.read_text(encoding="utf-8")
    review_text = review_commands.read_text(encoding="utf-8")
    health_all_text = health_all.read_text(encoding="utf-8")

    for needle in (
        "RecognitionWindow(Document document",
        "_document = document ?? throw new ArgumentNullException(nameof(document));",
        "ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)",
    ):
        if needle not in recognition_text:
            errors.append("RecognitionWindow explicit document contract missing: " + needle)
    if "_document = BcadApplication.DocumentManager.MdiActiveDocument" in recognition_text:
        errors.append("RecognitionWindow must not capture ambient MdiActiveDocument in its constructor")

    for needle in (
        "RevisionWindow(Document document",
        "_document = document ?? throw new ArgumentNullException(nameof(document));",
        "ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)",
    ):
        if needle not in revision_text:
            errors.append("RevisionWindow explicit document contract missing: " + needle)
    if "_document = BcadApplication.DocumentManager.MdiActiveDocument" in revision_text:
        errors.append("RevisionWindow must not capture ambient MdiActiveDocument in its constructor")

    for needle in (
        "ModelHealthWindow(Document document",
        "_document = document ?? throw new ArgumentNullException(nameof(document));",
        "ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)",
    ):
        if needle not in health_text:
            errors.append("ModelHealthWindow explicit document contract missing: " + needle)

    for needle in (
        "new RecognitionWindow(doc, batch.Results, apply, locate)",
        "new RevisionWindow(doc, before, after, rows, locate)",
    ):
        if needle not in review_text:
            errors.append("ReviewCommands must pass the source Document explicitly: " + needle)

    if "new ModelHealthWindow(document, issues" not in health_all_text:
        errors.append("HealthAllCommands must pass the source Document explicitly")

    explicit_doc_first = r"doc(?:ument)?\b"

    # Production adapter call sites must not use the legacy ambient ModelHealthWindow overload.
    old_health_call = re.compile(r"new\s+ModelHealthWindow\s*\(\s*(?!" + explicit_doc_first + r")")
    for path in sorted(ADAPTER.rglob("*.cs")):
        if path == health:
            continue
        text = path.read_text(encoding="utf-8")
        if "new ModelHealthWindow" not in text:
            continue
        if old_health_call.search(text):
            errors.append("ambient ModelHealthWindow call site remains: " + str(path.relative_to(ROOT)))

    # Recognition/Revision production call sites must use an explicit source Document as first argument.
    old_recognition_call = re.compile(r"new\s+RecognitionWindow\s*\(\s*(?!" + explicit_doc_first + r")")
    old_revision_call = re.compile(r"new\s+RevisionWindow\s*\(\s*(?!" + explicit_doc_first + r")")
    for path in sorted(ADAPTER.rglob("*.cs")):
        if path in (recognition, revision):
            continue
        text = path.read_text(encoding="utf-8")
        if old_recognition_call.search(text):
            errors.append("RecognitionWindow call site is not explicitly document-bound: " + str(path.relative_to(ROOT)))
        if old_revision_call.search(text):
            errors.append("RevisionWindow call site is not explicitly document-bound: " + str(path.relative_to(ROOT)))

print("QS3D modeless review document-binding preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: Recognition, Revision and every production Model Health window are explicitly bound to the source BricsCAD Document; modeless locate/apply actions cannot silently retarget a newly active DWG.")
