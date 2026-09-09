#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectPersistenceCheckpoint.cs"


def fail(message: str) -> None:
    print("ERROR: project persistence checkpoint owner-generation preflight failed: " + message)
    raise SystemExit(1)


def require(text: str, fragment: str, message: str) -> None:
    if fragment not in text:
        fail(message)


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    require(
        text,
        "private readonly ProjectState _projectOwner;",
        "checkpoint must retain the exact owning ProjectState generation",
    )
    require(
        text,
        "ProjectState projectOwner,",
        "checkpoint constructor must receive its owning ProjectState generation",
    )
    require(
        text,
        "_projectOwner = projectOwner ?? throw new ArgumentNullException(nameof(projectOwner));",
        "checkpoint constructor must fail closed on a missing project owner",
    )
    require(
        text,
        "return new ProjectPersistenceCheckpoint(\n                project,",
        "capture must bind the checkpoint to the exact source ProjectState instance",
    )
    require(
        text,
        "if (!ReferenceEquals(project, _projectOwner))\n                return false;",
        "Matches must reject a replacement ProjectState generation even when ProjectId is reused",
    )
    require(
        text,
        "if (!ReferenceEquals(project, _projectOwner))\n                throw new InvalidOperationException(\"Cannot restore a persistence checkpoint into a replacement project generation.\");",
        "Restore must reject a replacement ProjectState generation before any persistence mutation",
    )

    print("OK: ProjectPersistenceCheckpoint is generation-fenced to its captured ProjectState owner.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
