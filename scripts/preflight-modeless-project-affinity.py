#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"


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


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "_projectAffinityBound",
        "_projectId",
        "_drawingFingerprint",
        "BindProjectAffinityIfPresent()",
        "TryResolveLiveDocument(out var document)",
        "MatchesBoundDocumentAffinity(document)",
        "ProjectContextCoordinator.TryGetReadOnly(candidate, out var project)",
        "_window.Activated += OnWindowActivated",
        "_window.PreviewMouseDown += OnPreviewMouseDown",
        "_window.PreviewKeyDown += OnPreviewKeyDown",
        "if (!EnsureProjectAffinity()) e.Handled = true",
        "CloseForProjectChange()",
        "_window.Activated -= OnWindowActivated",
        "_window.PreviewMouseDown -= OnPreviewMouseDown",
        "_window.PreviewKeyDown -= OnPreviewKeyDown",
    ]
    missing = [needle for needle in required if needle not in text]
    if missing:
        print("ERROR: modeless project/drawing-affinity contract is incomplete:")
        for needle in missing:
            print(" - missing:", needle)
        return 1

    resolver = method_block(text, "private bool TryResolveLiveDocument(out Document document)")
    for marker in (
        "ReferenceEquals(candidate, _lifecycleDocument)",
        "MatchesNativeDatabase(candidate)",
        "if (ReferenceEquals(candidate, _lifecycleDocument)) continue",
        "if (!MatchesNativeDatabase(candidate)) continue",
        "if (!MatchesBoundDocumentAffinity(candidate)) continue",
    ):
        require(marker in resolver, f"TryResolveLiveDocument is missing: {marker}")

    require(
        resolver.index("ReferenceEquals(candidate, _lifecycleDocument)")
        < resolver.index("if (ReferenceEquals(candidate, _lifecycleDocument)) continue")
        < resolver.index("if (!MatchesBoundDocumentAffinity(candidate)) continue"),
        "Original managed-wrapper affinity must be attempted before semantically admitting wrapper drift.",
    )

    semantic_match = method_block(text, "private bool MatchesBoundDocumentAffinity(Document candidate)")
    for marker in (
        "_projectAffinityBound",
        "_projectId",
        "_drawingFingerprint",
        "ProjectContextCoordinator.TryGetReadOnly(candidate, out var project)",
        "project.ProjectId ?? string.Empty",
        "project.DrawingFingerprint ?? string.Empty",
        "StringComparison.OrdinalIgnoreCase",
    ):
        require(marker in semantic_match, f"MatchesBoundDocumentAffinity is missing: {marker}")
    require(
        semantic_match.index("ProjectContextCoordinator.TryGetReadOnly(candidate, out var project)")
        < semantic_match.index("project.ProjectId ?? string.Empty")
        < semantic_match.index("project.DrawingFingerprint ?? string.Empty"),
        "Wrapper-drift affinity proof must read ProjectId and DrawingFingerprint from the admitted read-only project context.",
    )
    require(
        "ProjectContextCoordinator.GetOrCreate" not in semantic_match
        and "ProjectContextCoordinator.Get(" not in semantic_match,
        "Wrapper-drift affinity proof must remain read-only and must not create project state.",
    )

    ensure = method_block(text, "private bool EnsureProjectAffinity()")
    for marker in (
        "lock (_documentAccessGate)",
        "Volatile.Read(ref _invalidated) != 0",
        "TryResolveLiveDocument(out var document)",
        "MatchesBoundDocumentAffinity(document)",
        "CloseForProjectChange()",
    ):
        require(marker in ensure, f"EnsureProjectAffinity is missing: {marker}")

    require(
        ensure.index("lock (_documentAccessGate)")
        < ensure.index("Volatile.Read(ref _invalidated) != 0")
        < ensure.index("TryResolveLiveDocument(out var document)")
        < ensure.index("MatchesBoundDocumentAffinity(document)"),
        "Project affinity must observe invalidation, resolve the wrapper safely, then revalidate drawing affinity.",
    )

    bind_live = method_block(text, "private void BindProjectAffinityIfPresent(Document document)")
    for marker in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "project.ProjectId ?? string.Empty",
        "project.DrawingFingerprint ?? string.Empty",
        "_projectId = projectId",
        "_drawingFingerprint = drawingFingerprint",
        "_projectAffinityBound = true",
    ):
        require(marker in bind_live, f"Initial semantic affinity capture is missing: {marker}")
    require(
        "ProjectContextCoordinator.GetOrCreate" not in bind_live
        and "ProjectContextCoordinator.Get(" not in bind_live,
        "Initial modeless affinity capture must remain read-only.",
    )

    require(
        text.count("ProjectContextCoordinator.TryGetReadOnly(") == 2,
        "Modeless affinity must use exactly two read-only project-context paths: initial capture and wrapper-drift proof.",
    )
    require(
        "ProjectContextCoordinator.TryGetReadOnly(_lifecycleDocument" not in text,
        "The retained lifecycle wrapper must not be dereferenced through project context after lifecycle binding.",
    )

    print("PASS: document-bound modeless windows prefer the original managed wrapper and admit wrapper drift only after read-only ProjectId + DrawingFingerprint proof.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
