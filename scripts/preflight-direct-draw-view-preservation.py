#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawCommands.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")
    require('document.SendStringToExecute("QS3DVIEW3D ' not in text,
            "Direct Draw must not force QS3DVIEW3D after authoring; preserve the user's camera/zoom.")
    require("CadHandleService.Select(document, new[] { generatedHandle })" in text,
            "Direct Draw must keep selecting/highlighting generated native geometry.")
    require("document.Editor.SetImpliedSelection(new[] { sourceId })" in text,
            "Direct Draw must preserve the source-selection fallback when no generated handle is available.")
    require("document.Editor.Regen();" in text,
            "Direct Draw must still regenerate the editor after the selection/highlight update.")
    print("Direct Draw view-preservation preflight passed.")


if __name__ == "__main__":
    main()
