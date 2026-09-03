#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
V26_PROJECT = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"
errors = []

recognition = ADAPTER / "UI/RecognitionWindow.xaml.cs"
revision = ADAPTER / "UI/RevisionWindow.xaml.cs"
health = ADAPTER / "UI/ModelHealthWindow.xaml.cs"
health_presenter = ADAPTER / "UI/ModelHealthWindowPresenter.cs"
review_commands = ADAPTER / "ReviewCommands.cs"
health_all = ADAPTER / "HealthAllCommands.cs"

required_files = [recognition, revision, health, health_presenter, review_commands, health_all, V26_PROJECT]
for path in required_files:
    if not path.is_file():
        errors.append("missing required modeless binding source: " + str(path.relative_to(ROOT)))

if not errors:
    recognition_text = recognition.read_text(encoding="utf-8")
    revision_text = revision.read_text(encoding="utf-8")
    health_text = health.read_text(encoding="utf-8")
    health_presenter_text = health_presenter.read_text(encoding="utf-8")
    review_text = review_commands.read_text(encoding="utf-8")
    health_all_text = health_all.read_text(encoding="utf-8")
    v26_project_text = V26_PROJECT.read_text(encoding="utf-8")

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
        "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "_canLocate = locate != null;",
        "DocumentBoundWindowLifetime.Attach(this, document);",
        "BcadApplication.DocumentManager.MdiActiveDocument",
        "database.UnmanagedObject == _nativeDatabaseIdentity",
        "var document = EnsureActiveAndCurrent();",
        "LocateCurrentElement(document, row);",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
    ):
        if needle not in revision_text:
            errors.append("RevisionWindow live document-affinity contract missing: " + needle)

    for forbidden in (
        "private readonly Document _document",
        "private readonly Action<QuantityRevisionRow>? _locate",
        "_locate = locate",
        "ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)",
        "ProjectContextCoordinator.TryGetReadOnly(_document",
    ):
        if forbidden in revision_text:
            errors.append("RevisionWindow must not retain/dereference stale managed document state: " + forbidden)

    if "if (!TryGetBoundActiveDocument(out var document)) return;" not in revision_text:
        errors.append("RevisionWindow activation refresh must ignore temporary cross-DWG activation rather than marking the source snapshot stale")

    for needle in (
        "ModelHealthWindow(Document document",
        "_document = document ?? throw new ArgumentNullException(nameof(document));",
        "DocumentBoundWindowLifetime.Attach(this, _document);",
        "ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)",
        "private static ModelHealthWindow? _pendingPublication;",
        "private static ModelHealthWindow? _published;",
        "ReservePublication(this);",
        "Loaded += OnPublicationLoaded;",
        "Closed += OnPublicationClosed;",
    ):
        if needle not in health_text:
            errors.append("ModelHealthWindow explicit document/publication contract missing: " + needle)

    for needle in (
        "Show(",
        "Document document,",
        "candidate = new ModelHealthWindow(document, issues, locate);",
        "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);",
        "if (!candidate.IsLoaded)",
    ):
        if needle not in health_presenter_text:
            errors.append("ModelHealthWindowPresenter explicit document/host-show contract missing: " + needle)

    for needle in (
        "new RecognitionWindow(doc, batch.Results, apply, locate)",
        "new RevisionWindow(doc, before, after, rows, locate)",
    ):
        if needle not in review_text:
            errors.append("ReviewCommands must pass the source Document explicitly: " + needle)

    canonical_revision_locate = 'Action<QuantityRevisionRow> locate = row => LocateCurrentElement(doc, row.ElementId, "Revision Locate");'
    if canonical_revision_locate not in review_text:
        errors.append("ReviewCommands Revision locate contract changed; update the window's action-local locate workflow deliberately")

    if "ModelHealthWindowPresenter.Show(document, issues, issue =>" not in health_all_text:
        errors.append("HealthAllCommands must pass the source Document explicitly through ModelHealthWindowPresenter")

    if '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"' not in v26_project_text:
        errors.append("V26 must continue compiling the shared V25 adapter source for modeless document-lifetime parity")

    explicit_doc_first = r"doc(?:ument)?\b"
    old_health_call = re.compile(r"new\s+ModelHealthWindow\s*\(\s*(?!" + explicit_doc_first + r")")
    for path in sorted(ADAPTER.rglob("*.cs")):
        if path == health:
            continue
        text = path.read_text(encoding="utf-8")
        if "new ModelHealthWindow" not in text:
            continue
        if old_health_call.search(text):
            errors.append("ambient ModelHealthWindow call site remains: " + str(path.relative_to(ROOT)))

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

print("PASS: Recognition and Model Health retain explicit source-document contracts; Model Health publication is window-owned/presenter-routed; Revision uses stable native identity plus action-time live Document resolution, with shared V26 source parity.")
