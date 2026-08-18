#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
LOCALIZATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "UiLocalization.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "UiLanguageWindow.cs"
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "UiLanguageCommands.cs"
POLISH = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ProductionUiPolish.cs"


def require(path, needles):
    if not path.is_file():
        raise RuntimeError(f"missing required source file: {path.relative_to(ROOT)}")
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            raise RuntimeError(
                f"{path.relative_to(ROOT)} is missing localization contract token: {needle}"
            )
    return text


def main():
    localization = require(
        LOCALIZATION,
        [
            'Vietnamese = "vi-VN"',
            'English = "en-US"',
            'ChineseSimplified = "zh-CN"',
            'ChineseTraditional = "zh-TW"',
            'Russian = "ru-RU"',
            'LanguageFileName = "ui-language.txt"',
            '"简体中文"',
            '"繁體中文"',
            '"Русский"',
            'return Vietnamese;',
            'source.IsExpression',
            'SetCurrentValue',
            'CurrentUICulture',
        ],
    )
    require(
        WINDOW,
        [
            'UiLocalization.SupportedLanguages',
            'UiLocalization.SetLanguage(languageCode)',
            'UiLocalization.T("Chọn ngôn ngữ giao diện")',
        ],
    )
    require(
        COMMAND,
        [
            '[CommandMethod("QS3DLANGUAGE", CommandFlags.Modal)]',
            'Application.ShowModalWindow(new UiLanguageWindow())',
        ],
    )
    polish = require(
        POLISH,
        [
            'Interlocked.CompareExchange(ref _registered, 1, 0)',
            'Interlocked.Exchange(ref _registered, 0)',
            'UiLocalization.RegisterAndApply(root)',
            'UiLocalization.Apply(root)',
        ],
    )

    if "Thread.CurrentThread.CurrentUICulture" in localization:
        raise RuntimeError("QS3D localization must not mutate BricsCAD process-wide CurrentUICulture.")
    if polish.index("Interlocked.Exchange(ref _registered, 0)") < polish.index("catch"):
        raise RuntimeError("ProductionUiPolish retry reset must live on the registration failure path.")

    print("PASS: persisted multilingual QS3D UI and bootstrap retry contracts are present.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except RuntimeError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
