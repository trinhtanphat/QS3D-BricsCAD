#!/usr/bin/env python3
"""Deterministic source guard for AuditLogWindow project-affine refresh."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/AuditLogWindow.xaml.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, message: str) -> int:
    index = text.find(token)
    if index < 0:
        fail(message)
    return index


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    resolver_start = require(text, "private bool TryResolveBoundDocument(out Document document)", "missing AuditLogWindow document resolver")
    resolver_end = require(text[resolver_start:], "private bool MatchesBoundProjectAffinity(Document candidate)", "missing project-affinity verifier") + resolver_start
    resolver = text[resolver_start:resolver_end]

    exact_wrapper = require(resolver, "if (lifecycleDocument != null && ReferenceEquals(candidate, lifecycleDocument))", "missing exact managed-wrapper branch")
    native_check = require(resolver[exact_wrapper:], "if (database.UnmanagedObject != _nativeDatabaseIdentity) continue;", "exact-wrapper branch must retain native database identity check") + exact_wrapper
    semantic_check = require(resolver[exact_wrapper:], "if (HasBoundProjectAffinity && !MatchesBoundProjectAffinity(candidate)) continue;", "exact-wrapper branch must revalidate captured project affinity") + exact_wrapper
    publish = require(resolver[exact_wrapper:], "document = candidate;", "exact-wrapper branch must resolve the proven candidate") + exact_wrapper

    if not (native_check < semantic_check < publish):
        fail("same-wrapper resolution must prove native identity and captured project affinity before publishing the document")

    require(text, "private bool HasBoundProjectAffinity =>", "missing explicit captured-project-affinity predicate")
    require(text, "!string.IsNullOrWhiteSpace(_boundProjectId)", "captured project affinity must require ProjectId")
    require(text, "!string.IsNullOrWhiteSpace(_boundDrawingFingerprint)", "captured project affinity must require drawing fingerprint")

    wrapper_drift = require(resolver, "if (database.UnmanagedObject != _nativeDatabaseIdentity) continue;\n                        if (!MatchesBoundProjectAffinity(candidate)) continue;", "wrapper-drift path must remain project-affine")
    if wrapper_drift <= exact_wrapper:
        fail("wrapper-drift affinity check must remain after the exact-wrapper branch")

    reload_start = require(text, "private void Reload()", "missing AuditLogWindow reload path")
    reload_end = require(text[reload_start:], "private bool TryResolveBoundDocument", "missing reload/resolver boundary") + reload_start
    reload_block = text[reload_start:reload_end]
    resolve = require(reload_block, "if (!TryResolveBoundDocument(out var document))", "Reload must resolve the bound document before reading project state")
    read_only = require(reload_block, "ProjectContextCoordinator.TryGetReadOnly(document, out var project)", "Reload must obtain project context read-only")
    audit_read = require(reload_block, "project.AuditEvents", "Reload must read audit events only after affinity resolution")
    if not (resolve < read_only < audit_read):
        fail("Reload ordering must remain resolve-affinity -> read-only project -> audit read")

    print("PASS: AuditLogWindow refresh remains native- and project-affine without auto-rebinding no-project windows")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
