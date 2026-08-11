#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "_projectAffinityBound",
        "_projectId",
        "BindProjectAffinityIfPresent()",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
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

    if text.count("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)") < 2:
        print("ERROR: project affinity must be captured and revalidated before later modeless interaction.")
        return 1

    print("PASS: document-bound modeless windows fail closed when their semantic ProjectId changes.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
