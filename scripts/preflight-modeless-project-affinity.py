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
        "BindProjectAffinityIfPresent()",
        "TryResolveLiveDocument(out var document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "string.Equals(project.ProjectId ?? string.Empty, _projectId, StringComparison.OrdinalIgnoreCase)",
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
        print("ERROR: modeless project-affinity contract is incomplete:")
        for needle in missing:
            print(" - missing:", needle)
        return 1

    ensure = method_block(text, "private bool EnsureProjectAffinity()")
    for marker in (
        "lock (_documentAccessGate)",
        "Volatile.Read(ref _invalidated) != 0",
        "TryResolveLiveDocument(out var document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "string.Equals(project.ProjectId ?? string.Empty, _projectId, StringComparison.OrdinalIgnoreCase)",
        "CloseForProjectChange()",
    ):
        require(marker in ensure, f"EnsureProjectAffinity is missing: {marker}")

    require(
        ensure.index("lock (_documentAccessGate)")
        < ensure.index("Volatile.Read(ref _invalidated) != 0")
        < ensure.index("TryResolveLiveDocument(out var document)")
        < ensure.index("ProjectContextCoordinator.TryGetReadOnly(document, out var project)"),
        "Project affinity must observe invalidation and resolve a live wrapper before semantic revalidation.",
    )

    bind_live = method_block(text, "private void BindProjectAffinityIfPresent(Document document)")
    require(
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" in bind_live,
        "Initial semantic affinity capture must use the resolved live document wrapper.",
    )

    require(
        text.count("ProjectContextCoordinator.TryGetReadOnly(document, out var project)") == 2,
        "Project affinity must be captured and revalidated through exactly the two live-document paths.",
    )
    require(
        "ProjectContextCoordinator.TryGetReadOnly(_document" not in text
        and "ProjectContextCoordinator.TryGetReadOnly(_lifecycleDocument" not in text,
        "Modeless affinity must never dereference a retained managed Document wrapper through the project coordinator.",
    )

    print("PASS: document-bound modeless windows capture/revalidate ProjectId through a live native-identity-matched Document and fail closed on drift.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
