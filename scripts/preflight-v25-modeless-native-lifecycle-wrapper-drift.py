#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundNativeLifecycleCoordinator.cs"
LIFETIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
coordinator = COORDINATOR.read_text(encoding="utf-8")
lifetime = LIFETIME.read_text(encoding="utf-8")
errors = []

def require(condition: bool, message: str) -> None:
    if not condition: errors.append(message)

def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        errors.append(signature + " is missing"); return ""
    brace = source.find("{", start)
    if brace < 0:
        errors.append(signature + " body is missing"); return ""
    depth = 0
    for index in range(brace, len(source)):
        if source[index] == "{": depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0: return source[start:index + 1]
    errors.append(signature + " body is unterminated"); return ""

register = method_block(coordinator, "internal static IDisposable Register(")
for token in ("Func<Document, bool> replacementAffinity", "entry.EnsureLifecycleDocument(lifecycleDocument, replacementAffinity);"):
    require(token in register, "registration must require/apply replacement affinity: " + token)
rebind = method_block(coordinator, "internal static void Rebind(")
for token in ("Func<Document, bool> replacementAffinity", "entry.EnsureLifecycleDocument(lifecycleDocument, replacementAffinity);"):
    require(token in rebind, "coordinator rebind contract missing: " + token)

require("private Document _lifecycleDocument;" in lifetime, "modeless lifetime must keep a movable current wrapper")
attach = method_block(lifetime, "public void Attach(Document document)")
for token in ("if (_attached)", "ReferenceEquals(document, _lifecycleDocument)", "MatchesBoundDocumentAffinity(document)", "DocumentBoundNativeLifecycleCoordinator.Rebind(", "_lifecycleDocument = document;"):
    require(token in attach, "repeated Attach wrapper-rebind contract missing: " + token)
if all(token in attach for token in ("DocumentBoundNativeLifecycleCoordinator.Rebind(", "_lifecycleDocument = document;")):
    require(attach.index("DocumentBoundNativeLifecycleCoordinator.Rebind(") < attach.index("_lifecycleDocument = document;"), "Attach must publish wrapper only after native rebind")

resolve = method_block(lifetime, "private bool TryResolveLiveDocument(out Document document)")
for token in ("MatchesBoundDocumentAffinity(candidate)", "DocumentBoundNativeLifecycleCoordinator.Rebind(", "_lifecycleDocument = candidate;", "document = candidate;"):
    require(token in resolve, "live wrapper resolution rebind contract missing: " + token)
if all(token in resolve for token in ("DocumentBoundNativeLifecycleCoordinator.Rebind(", "_lifecycleDocument = candidate;", "document = candidate;")):
    rebind_index = resolve.index("DocumentBoundNativeLifecycleCoordinator.Rebind(")
    publication_index = resolve.index("_lifecycleDocument = candidate;", rebind_index)
    output_index = resolve.index("document = candidate;", publication_index + len("_lifecycleDocument = candidate;"))
    require(rebind_index < publication_index < output_index, "live resolution must rebind before publication/output")

require("private readonly List<Document> _pendingNativeDetachDocuments" in coordinator,
        "coordinator must track every wrapper whose native unsubscribe rollback remains pending")
require("private void RememberPendingNativeDetach(Document lifecycleDocument)" in coordinator,
        "coordinator must retain pending native detach ownership without last-failure overwrite")

ensure = method_block(coordinator, "public void EnsureLifecycleDocument(")
for token in (
    "Document previousDocument;", "TryClearPendingNativeDetaches()", "EnsureCurrentNativeHandlersAttached();", "previousDocument = _lifecycleDocument;",
    "if (!replacementAffinity(lifecycleDocument))", "if (!Entries.TryGetValue(NativeDatabaseIdentity, out var current) || !ReferenceEquals(current, this))",
    "if (!ReferenceEquals(_lifecycleDocument, previousDocument))", "AttachNativeHandlers(lifecycleDocument);",
    "if (!TryDetachNativeHandlers(previousDocument))", "if (!TryDetachNativeHandlers(lifecycleDocument))",
    "RememberPendingNativeDetach(lifecycleDocument);", "_lifecycleDocument = lifecycleDocument;",
): require(token in ensure, "wrapper rebind rollback/current-handler contract missing: " + token)
require(ensure.count("EnsureCurrentNativeHandlersAttached();") >= 2,
        "current wrapper handlers must be repaired after pending cleanup on both pre-proof and post-proof validation paths")
if all(token in ensure for token in ("AttachNativeHandlers(lifecycleDocument);", "if (!TryDetachNativeHandlers(previousDocument))", "_lifecycleDocument = lifecycleDocument;")):
    require(ensure.index("AttachNativeHandlers(lifecycleDocument);") < ensure.index("if (!TryDetachNativeHandlers(previousDocument))") < ensure.index("_lifecycleDocument = lifecycleDocument;"), "replacement attach -> stale detach -> publish ordering is required")

repair_current = method_block(coordinator, "private void EnsureCurrentNativeHandlersAttached()")
for token in ("if (_nativeHandlersAttached) return;", "AttachNativeHandlers(_lifecycleDocument);"):
    require(token in repair_current, "current lifecycle handler repair missing: " + token)

attach_native = method_block(coordinator, "public void AttachNativeHandlers(Document lifecycleDocument)")
for token in ("TryClearPendingNativeDetaches()", "lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;", "lifecycleDocument.CloseAborted += OnDocumentCloseAborted;", "RememberPendingNativeDetach(lifecycleDocument);"):
    require(token in attach_native, "native attach must clean/track incomplete ownership: " + token)
try_detach = method_block(coordinator, "private bool TryDetachNativeHandlers(Document lifecycleDocument)")
for token in ("lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;", "lifecycleDocument.CloseAborted -= OnDocumentCloseAborted;", "RememberPendingNativeDetach(lifecycleDocument);", "ForgetPendingNativeDetach(lifecycleDocument);", "return true;", "return false;"):
    require(token in try_detach, "native detach retry ownership contract missing: " + token)
clear_pending = method_block(coordinator, "private bool TryClearPendingNativeDetaches()")
for token in ("_pendingNativeDetachDocuments.ToArray()", "TryDetachNativeHandlers(pending)", "if (ReferenceEquals(pending, _lifecycleDocument)) _nativeHandlersAttached = false;", "_pendingNativeDetachDocuments.Count == 0"):
    require(token in clear_pending, "pending cleanup must preserve truthful current-handler ownership: " + token)

unregister = method_block(coordinator, "private static void Unregister(Entry entry, Callbacks callbacks)")
require("if (!entry.DetachNativeHandlersIfSafe()) return;" in unregister, "unregister must retain entry when native detach cannot complete")
if all(token in unregister for token in ("if (!entry.DetachNativeHandlersIfSafe()) return;", "Entries.Remove(entry.NativeDatabaseIdentity);")):
    require(unregister.index("if (!entry.DetachNativeHandlersIfSafe()) return;") < unregister.index("Entries.Remove(entry.NativeDatabaseIdentity);"), "native handlers must detach before dictionary ownership is removed")

require("if (HasDifferentLiveLifecycleDocument(document, identity)) return;" in coordinator, "stale destroy fallback must preserve live replacement lifecycle")
has_live = method_block(coordinator, "private static bool HasDifferentLiveLifecycleDocument(")
for token in ("if (ReferenceEquals(lifecycleDocument, destroyingDocument)) return false;", "if (!ReferenceEquals(candidate, lifecycleDocument)) continue;", "entry.ForgetPendingNativeDetachAfterDestroy(destroyingDocument);", "return true;"):
    require(token in has_live, "stale destroy fencing/release missing: " + token)
forget_destroyed = method_block(coordinator, "public void ForgetPendingNativeDetachAfterDestroy(Document destroyedDocument)")
require("ForgetPendingNativeDetach(destroyedDocument);" in forget_destroyed, "destroyed stale wrapper must release pending managed ownership")

if errors:
    print("ERROR: V25 modeless native-lifecycle wrapper-drift preflight failed:", file=sys.stderr)
    for error in errors: print(" - " + error, file=sys.stderr)
    raise SystemExit(1)
print("V25 modeless native-lifecycle wrapper-drift preflight passed.")