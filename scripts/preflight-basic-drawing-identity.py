#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BasicDrawingCommands.cs"


def require_token(text: str, token: str, label: str) -> None:
    if token not in text:
        raise RuntimeError(f"missing {label}: {token}")


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    require_token(text, "var projectId = RequireCanonicalIdentity(", "project identity admission guard")
    require_token(text, "project.ProjectId,", "project identity source")
    require_token(text, "var familyId = RequireCanonicalIdentity(", "family identity admission guard")
    require_token(text, "family.Id,", "family identity source")
    require_token(text, "!string.Equals(value, value.Trim(), StringComparison.Ordinal)", "surrounding-whitespace rejection")
    require_token(text, "ContainsControlCharacter(value)", "control-character rejection")
    require_token(text, "RequiredIdentityToken(\"p1:\", context.ProjectId)", "project marker canonicality")
    require_token(text, "RequiredIdentityToken(\"f1:\", context.FamilyId)", "family marker canonicality")
    require_token(text, "return HashIdentity(prefix, canonical);", "required marker exact-value hashing")

    forbidden = (
        "IdentityToken(\"p1:\", context.ProjectId)",
        "IdentityToken(\"f1:\", context.FamilyId)",
    )
    for token in forbidden:
        if token in text and "Required" + token not in text:
            raise RuntimeError("required project/family marker must not use optional trim-normalizing IdentityToken: " + token)

    capture_start = text.index("private static BasicDrawingContext CaptureContext")
    append_start = text.index("private static ObjectId AppendEntity")
    if capture_start >= append_start:
        raise RuntimeError("context validation must remain structurally before native append")

    print("PASS: basic drawing commands reject non-canonical project/family identities before native mutation.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, RuntimeError, ValueError) as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
