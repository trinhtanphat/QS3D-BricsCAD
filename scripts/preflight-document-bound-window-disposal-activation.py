#!/usr/bin/env python3
"""Guard document-bound modeless activation against native document disposal races."""

# Lane-Key: issue-3621 — H.3 routes native document lifecycle through one shared coordinator
# while WPF registrations resolve live wrappers without dereferencing a stale retained wrapper.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
NATIVE_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundNativeLifecycleCoordinator.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    require(start >= 0, f"{signature} is missing.")
    brace = source.find("{", start)
    require(brace >= 0, f"{signature} body is missing.")
    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise AssertionError(f"{signature} body is unterminated.")


require(NATIVE_SOURCE.exists(), "H.3 shared native lifecycle coordinator is missing.")
source = SOURCE.read_text(encoding="utf-8")
native = NATIVE_SOURCE.read_text(encoding="utf-8")
barrier = "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;"

require("private readonly object _documentAccessGate = new object();" in source,
        "Document-bound modeless lifetime must serialize live Document access against teardown.")
require("private readonly Document _document;" not in source,
        "WPF modeless affinity must not retain the managed Document wrapper used by the old crash path.")
require("private readonly Document _lifecycleDocument;" in source,
        "A lifecycle-only wrapper remains available only to shared lifecycle ownership and identity comparison.")
require("private int _hostQuitStarted;" not in source,
        "Host quit state must be plugin-global, not copied into every modeless registration.")
require("private IDisposable? _nativeLifecycleSubscription;" in source,
        "Per-window lifetime must own only a managed native-lifecycle subscription token.")

attach = method_block(source, "public void Attach(Document document)")
for marker in (
    "ModelessHostQuiescenceCoordinator.EnsureInitialized();",
    "BindProjectAffinityIfPresent();",
    "_nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(",
    "OnBeginDocumentClose,",
    "OnDocumentCloseAborted,",
    "OnDocumentToBeDestroyed);",
    "ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;",
    "_window.Activated += OnWindowActivated;",
):
    require(marker in attach, f"Attach is missing disposal-safe lifecycle marker: {marker}")
require(
    attach.index("ModelessHostQuiescenceCoordinator.EnsureInitialized();")
    < attach.index("BindProjectAffinityIfPresent();")
    < attach.index("_nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(")
    < attach.index("ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;")
    < attach.index("_window.Activated += OnWindowActivated;"),
    "Global/shared document barriers must be installed before WPF activation can be observed.",
)
for forbidden in (
    "BcadApplication.BeginQuit +=", "BcadApplication.QuitWillStart +=", "BcadApplication.QuitAborted +=",
    "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
):
    require(forbidden not in attach,
            f"Per-window Attach must not subscribe a native reactor directly: {forbidden}")

for marker in (
    "lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
    "List<WeakReference<Callbacks>>",
    "TrySnapshotDestroyByLifecycleDocument",
    "TrySnapshotDestroyByNativeIdentity",
):
    require(marker in native, f"Shared native ownership marker is missing: {marker}")

resolve = method_block(source, "private bool TryResolveLiveDocument(out Document document)")
for marker in (
    "foreach (Document candidate in BcadApplication.DocumentManager)",
    "candidate.IsDisposed",
    "MatchesNativeDatabase(candidate)",
    "document = candidate;",
):
    require(marker in resolve, f"Live managed-wrapper resolution is missing: {marker}")
require("_lifecycleDocument." not in resolve,
        "Live document resolution may compare lifecycle-wrapper identity but must never dereference the retained wrapper.")
require("ProjectContextCoordinator.TryGetReadOnly(_lifecycleDocument" not in resolve,
        "Live document resolution must never route the retained lifecycle wrapper through project context.")

ensure = method_block(source, "private bool EnsureProjectAffinity()")
require("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;" in ensure,
        "Project-affinity reads must fail closed immediately during host quiescence.")
require("lock (_documentAccessGate)" in ensure,
        "Project-affinity reads must run under the document access gate.")
for marker in (
    "Volatile.Read(ref _invalidated) != 0",
    "TryResolveLiveDocument(out var document)",
):
    require(marker in ensure, f"Disposal-safe affinity path is missing: {marker}")

affinity_marker = (
    "MatchesBoundDocumentAffinity(document)"
    if "MatchesBoundDocumentAffinity(document)" in ensure
    else "ProjectContextCoordinator.TryGetReadOnly(document, out var project)"
)
require(affinity_marker in ensure,
        "Disposal-safe affinity path must revalidate the resolved live document before interaction.")
require(
    ensure.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;")
    < ensure.index("lock (_documentAccessGate)")
    < ensure.index("Volatile.Read(ref _invalidated) != 0")
    < ensure.index("TryResolveLiveDocument(out var document)")
    < ensure.index(affinity_marker),
    "Host quiescence, invalidation, and live-wrapper resolution must precede semantic project/drawing reads.",
)

if affinity_marker == "MatchesBoundDocumentAffinity(document)":
    semantic = method_block(source, "private bool MatchesBoundDocumentAffinity(Document candidate)")
    for marker in (
        "ProjectContextCoordinator.TryGetReadOnly(candidate, out var project)",
        "project.ProjectId ?? string.Empty",
        "project.DrawingFingerprint ?? string.Empty",
    ):
        require(marker in semantic, f"Semantic wrapper-drift validation is missing: {marker}")
    require("_lifecycleDocument." not in semantic,
            "Semantic affinity validation must not dereference the retained lifecycle wrapper.")
    require("ProjectContextCoordinator.GetOrCreate" not in semantic and "ProjectContextCoordinator.Get(" not in semantic,
            "Semantic affinity validation must remain read-only.")

require("ProjectContextCoordinator.TryGetReadOnly(_lifecycleDocument" not in source,
        "The lifecycle-only wrapper must never enter project/path affinity code.")

begin_close = method_block(source, "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)")
for marker in (
    barrier,
    "var deferForFinalDocument = !HasAnotherLiveDocument();",
    "lock (_documentAccessGate)",
    "Volatile.Write(ref _documentCloseStarted, 1)",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in begin_close, f"BeginDocumentClose barrier is missing: {marker}")
require(
    begin_close.index(barrier)
    < begin_close.index("var deferForFinalDocument = !HasAnotherLiveDocument();")
    < begin_close.index("lock (_documentAccessGate)")
    < begin_close.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < begin_close.index("TryCloseWindow(deferForFinalDocument);"),
    "BeginDocumentClose must cross global quiescence before DocumentManager access and preserve ordinary close behavior.",
)
require("_lifecycleDocument." not in begin_close,
        "Managed BeginDocumentClose must not dereference the retained lifecycle wrapper once close starts.")
require("DetachNativeLifecycleSubscription" not in begin_close,
        "Managed BeginDocumentClose must not tear down shared native ownership from a native callback.")

teardown = method_block(source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
for marker in (
    barrier,
    "var deferForFinalDocument = !HasAnotherLiveDocument();",
    "lock (_documentAccessGate)",
    "Volatile.Write(ref _documentCloseStarted, 1)",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in teardown, f"DocumentToBeDestroyed barrier is missing: {marker}")
require("e.Document" not in teardown and "MatchesNativeDatabase(" not in teardown,
        "DocumentToBeDestroyed must rely on coordinator-owned affinity without reopening the event Document.")
require(
    teardown.index(barrier)
    < teardown.index("var deferForFinalDocument = !HasAnotherLiveDocument();")
    < teardown.index("lock (_documentAccessGate)")
    < teardown.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < teardown.index("TryCloseWindow(deferForFinalDocument);"),
    "DocumentToBeDestroyed must cross global quiescence before DocumentManager access and preserve ordinary close behavior.",
)
require("_lifecycleDocument." not in teardown,
        "Native destruction fallback must never touch the retained lifecycle wrapper.")
require("DetachNativeLifecycleSubscription" not in teardown,
        "Managed destruction fallback must leave native ownership to the shared coordinator.")

safe_detach = method_block(source, "private void DetachDocumentLifecycleHandlersIfSafe()")
require(barrier in safe_detach,
        "Normal lifecycle detach must own a fresh host-quiescence barrier.")
require("Volatile.Read(ref _documentCloseStarted) != 0" in safe_detach,
        "Window.Closed after native close starts must not mutate lifecycle ownership.")
require("DetachNativeLifecycleSubscription();" in safe_detach,
        "Normal live-document detach must release the managed subscription token.")
for forbidden in ("BeginDocumentClose -=", "CloseAborted -=", "DocumentToBeDestroyed -="):
    require(forbidden not in safe_detach,
            f"Per-window detach must not remove a native reactor directly: {forbidden}")

abort = method_block(source, "private void OnDocumentCloseAborted(object? sender, EventArgs e)")
require(barrier in abort,
        "Document CloseAborted must not mutate lifecycle ownership while host quiescence is active.")
require("DetachDocumentLifecycleHandlersAfterAbort();" in abort,
        "An aborted ordinary close must release the managed subscription while the document is live again.")

host_abort = method_block(source, "private void OnHostQuiescenceAborted(object? sender, EventArgs e)")
require("TryRecoverAfterQuitAbort();" in host_abort,
        "Managed host-abort notification must schedule stale-registration recovery.")
require("BcadApplication." not in host_abort,
        "Managed host-abort callback must not own native application lifecycle work.")

detach = method_block(source, "private void Detach()")
require(barrier in detach,
        "Full detach must fail closed while host quiescence is active.")
require("DetachDocumentLifecycleHandlersIfSafe();" in detach,
        "Full detach must release the managed native-lifecycle subscription when safe.")
require("ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted;" in detach,
        "Full detach must release the managed host-abort notification on ordinary window closure.")
for forbidden in (
    "BcadApplication.BeginQuit -=", "BcadApplication.QuitWillStart -=", "BcadApplication.QuitAborted -=",
    "_lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted -= OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
):
    require(forbidden not in detach,
            f"Per-window Detach must not release native reactors directly: {forbidden}")

print("[OK] Modeless affinity resolves a live wrapper under the document-access barrier and consumes shared native lifecycle ownership without stale-wrapper dereference.")
