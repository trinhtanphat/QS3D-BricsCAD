#!/usr/bin/env python3
from pathlib import Path
import sys

SOURCE = Path("src/QS3D.BricsCAD.V25/Cad/CadSelectionGuard.cs")


def fail(message: str) -> None:
    print(f"preflight-v25-selection-active-document-affinity: FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")
    method_start = text.find("public static ObjectId[] AcquireCurrentSelection(Document document)")
    if method_start < 0:
        fail("AcquireCurrentSelection not found")
    method_end = text.find("\n        }\n    }\n}", method_start)
    if method_end < 0:
        fail("AcquireCurrentSelection body boundary not found")
    body = text[method_start:method_end]

    prompt = "var selection = editor.GetSelection();"
    publish = "editor.SetImpliedSelection(objectIds);"
    guard = "RequireActiveDocument(document);"
    if prompt not in body or publish not in body:
        fail("interactive selection/publish contract changed")
    if body.count(guard) < 2:
        fail("active-document affinity must be checked both before and after interactive selection")
    if body.find(guard) > body.find(prompt):
        fail("missing active-document check before interactive selection")
    second_guard = body.find(guard, body.find(prompt) + len(prompt))
    if second_guard < 0 or second_guard > body.find(publish):
        fail("missing active-document revalidation after prompt and before PICKFIRST publication")

    helper = "if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))"
    if helper not in text:
        fail("active-document helper must compare captured document by reference")

    print("preflight-v25-selection-active-document-affinity: PASS")


if __name__ == "__main__":
    main()
