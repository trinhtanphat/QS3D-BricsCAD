#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateElementStructuralRevisionSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
CURTAIN = ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallScheduleReplacementGenerationFenceSmoke.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL project-state element structural revision: missing {label}: {token}")


def require_order(text: str, before: str, after: str, label: str) -> None:
    left = text.find(before)
    right = text.find(after)
    if left < 0 or right < 0 or left >= right:
        raise SystemExit(f"FAIL project-state element structural revision: invalid ordering for {label}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")
    curtain = CURTAIN.read_text(encoding="utf-8")

    require(source, "internal sealed class StructuralRevisionList<T> : IList<T> where T : class", "structural list type")
    require(source, "private readonly Action _beforeMutation;", "revision callback")
    require(source, "if (ReferenceEquals(previous, value)) return;", "same-instance no-op")
    require(source, "_beforeMutation();", "fail-before-mutation revision admission")
    require(source, "Elements = new StructuralRevisionList<ProjectElement>(Touch);", "ProjectState Elements ownership")
    if "Elements = new List<ProjectElement>();" in source:
        raise SystemExit("FAIL project-state element structural revision: plain List<ProjectElement> remains")

    structural = source[source.find("internal sealed class StructuralRevisionList<T>"):source.find("public sealed class ProjectState")]
    for token in ("public void Add(T item)", "public void Insert(int index, T item)", "public bool Remove(T item)", "public void RemoveAt(int index)", "public void Clear()"):
        require(structural, token, token)
    require_order(structural, "_beforeMutation();\n                _items[index] = value;", "public int Count", "index replacement touches before write")
    require_order(structural, "_beforeMutation();\n            _items.Add(item);", "public void Clear()", "Add touches before write")
    require_order(structural, "_beforeMutation();\n            _items.Insert(index, item);", "public bool Remove(T item)", "Insert touches before write")
    require_order(structural, "if (index < 0 || index > _items.Count) throw new ArgumentOutOfRangeException(nameof(index));", "_beforeMutation();\n            _items.Insert(index, item);", "Insert validates index before revision")
    require_order(structural, "if (index < 0 || index >= _items.Count) throw new ArgumentOutOfRangeException(nameof(index));", "_beforeMutation();\n            _items.RemoveAt(index);", "RemoveAt validates index before revision")

    require(smoke, "StructuralMutationsAdvanceExactlyOnce();", "deterministic structural smoke")
    require(smoke, "NoOpMutationsDoNotAdvance();", "no-op smoke")
    require(smoke, "RejectedMutationsDoNotAdvance();", "rejected-mutation atomicity smoke")
    require(smoke, "RevisionOverflowFailsBeforeMutation();", "overflow atomicity smoke")
    require(registration, "ProjectStateElementStructuralRevisionSmoke.Run();", "smoke registration")
    require(curtain, "Equal(checked(originalVersion + 1L), project.ChangeVersion);", "historical Curtain replacement expectation")
    if "Equal(originalVersion, project.ChangeVersion);" in curtain:
        raise SystemExit("FAIL project-state element structural revision: historical smoke still requires the revision bypass")

    print("PASS project-state element structural revision guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
