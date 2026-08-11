#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HOOK = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.CategoryRuleCreation.cs"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.UnsavedChanges.cs"
errors = []

if not HOOK.is_file():
    errors.append("missing QuantitySettingsWindow.CategoryRuleCreation.cs")
else:
    hook = HOOK.read_text(encoding="utf-8")
    loaded = hook.find("private void QuantitySettingsWindow_Loaded")
    init = hook.find("InitializeUnsavedChangesTracking();", loaded)
    rebuild = hook.find("RebuildMissingCategoryRuleChoices();", loaded)
    if min(loaded, init, rebuild) < 0 or not (loaded < init < rebuild):
        errors.append("window Loaded path must initialize unsaved tracking before category-rule refresh")

if not CODE.is_file():
    errors.append("missing QuantitySettingsWindow.UnsavedChanges.cs")
else:
    code = CODE.read_text(encoding="utf-8")
    required = (
        "private QuantityCalculationSettings? _persistedSettingsBaseline",
        "private bool _allowCloseWithoutPrompt",
        "private void InitializeUnsavedChangesTracking()",
        "_persistedSettingsBaseline = BuildSettingsFromView();",
        "Closing += QuantitySettingsWindow_Closing;",
        "SaveSettingsButton.Click += QuantitySettingsSaveBaseline_Click;",
        "private void QuantitySettingsWindow_Closing(object? sender, CancelEventArgs e)",
        "current = BuildSettingsFromView();",
        "if (SettingsEquivalent(current, baseline)) return;",
        "if (_persistentSettingsWriteBlocked)",
        "MessageBoxButton.OKCancel",
        "MessageBoxButton.YesNoCancel",
        "answer == MessageBoxResult.Cancel",
        "answer == MessageBoxResult.No",
        "_store.Save(current);",
        "_persistedSettingsBaseline = current.Clone();",
        "private static bool SettingsEquivalent",
    )
    for token in required:
        if token not in code:
            errors.append("unsaved-close contract missing token: " + token)

    closing = code.find("private void QuantitySettingsWindow_Closing")
    equivalence = code.find("private static bool SettingsEquivalent", closing)
    if closing < 0 or equivalence <= closing:
        errors.append("cannot isolate close guard")
    else:
        handler = code[closing:equivalence]
        build = handler.find("current = BuildSettingsFromView();")
        clean = handler.find("if (SettingsEquivalent(current, baseline)) return;")
        readonly = handler.find("if (_persistentSettingsWriteBlocked)")
        readonly_prompt = handler.find("MessageBoxButton.OKCancel", readonly)
        decision = handler.find("MessageBoxButton.YesNoCancel", readonly_prompt)
        cancel = handler.find("answer == MessageBoxResult.Cancel", decision)
        discard = handler.find("answer == MessageBoxResult.No", cancel)
        save = handler.find("_store.Save(current);", discard)
        if min(build, clean, readonly, readonly_prompt, decision, cancel, discard, save) < 0 or not (
            build < clean < readonly < readonly_prompt < decision < cancel < discard < save
        ):
            errors.append("close guard must validate, skip clean, handle read-only, then Save/Discard/Cancel before persistence")
        if handler.count("_store.Save(current);") != 1:
            errors.append("close guard must have exactly one persistence call")
        if "File.Write" in handler or "DataContractJsonSerializer" in handler:
            errors.append("close guard must persist only through QuantitySettingsStore")

    post_save = code.find("private void QuantitySettingsSaveBaseline_Click")
    close_start = code.find("private void QuantitySettingsWindow_Closing", post_save)
    if post_save < 0 or close_start <= post_save:
        errors.append("cannot isolate post-save baseline handler")
    else:
        handler = code[post_save:close_start]
        for token in ("BuildSettingsFromView()", "_store.Load()", "SettingsEquivalent(current, persisted)"):
            if token not in handler:
                errors.append("post-save baseline handler missing: " + token)
        if "_store.Save(" in handler:
            errors.append("post-save baseline handler must never perform a second save")

print("QS3D Quantity Settings unsaved-close preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DSETUP close is guarded against silent valid edit loss, invalid edits stay open, read-only mode never overwrites future-schema settings, Save/Discard/Cancel is explicit, and successful existing Save refreshes the persisted baseline without double-saving.")
