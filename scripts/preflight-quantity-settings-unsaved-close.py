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
        "private bool _persistedSettingsBaselineVerified",
        "private bool _allowCloseWithoutPrompt",
        "private void InitializeUnsavedChangesTracking()",
        "var baseline = BuildSettingsFromView();",
        "_persistedSettingsBaseline = baseline.Clone();",
        "_persistedSettingsBaselineVerified = TryVerifyPersistedSettingsBaseline(baseline);",
        "Closing += QuantitySettingsWindow_Closing;",
        "SaveSettingsButton.Click -= Save_Click;",
        "SaveSettingsButton.Click += QuantitySettingsGuardedSave_Click;",
        "SaveSettingsButton.Click += QuantitySettingsSaveBaseline_Click;",
        "private void QuantitySettingsGuardedSave_Click(object sender, RoutedEventArgs e)",
        "EnsurePersistedSettingsFreshBeforeSave();",
        "Save_Click(sender, e);",
        "private void QuantitySettingsWindow_Closing(object? sender, CancelEventArgs e)",
        "current = BuildSettingsFromView();",
        "if (SettingsEquivalent(current, baseline)) return;",
        "if (_persistentSettingsWriteBlocked)",
        "MessageBoxButton.OKCancel",
        "MessageBoxButton.YesNoCancel",
        "answer == MessageBoxResult.Cancel",
        "answer == MessageBoxResult.No",
        "_store.Save(current);",
        "AcceptPersistedSettingsBaseline(current);",
        "private void EnsurePersistedSettingsFreshBeforeSave()",
        "private static bool SettingsEquivalent",
    )
    for token in required:
        if token not in code:
            errors.append("unsaved-close contract missing token: " + token)

    init_start = code.find("private void InitializeUnsavedChangesTracking()")
    guarded_start = code.find("private void QuantitySettingsGuardedSave_Click", init_start)
    if init_start < 0 or guarded_start <= init_start:
        errors.append("cannot isolate unsaved tracking initialization")
    else:
        handler = code[init_start:guarded_start]
        baseline = handler.find("var baseline = BuildSettingsFromView();")
        verify = handler.find("_persistedSettingsBaselineVerified = TryVerifyPersistedSettingsBaseline(baseline);")
        detach = handler.find("SaveSettingsButton.Click -= Save_Click;")
        guard = handler.find("SaveSettingsButton.Click += QuantitySettingsGuardedSave_Click;")
        post = handler.find("SaveSettingsButton.Click += QuantitySettingsSaveBaseline_Click;")
        if min(baseline, verify, detach, guard, post) < 0 or not baseline < verify < detach < guard < post:
            errors.append("Save button must establish baseline then replace direct Save_Click with guarded Save before post-save baseline tracking")

    guarded = code.find("private void QuantitySettingsGuardedSave_Click")
    post_save = code.find("private void QuantitySettingsSaveBaseline_Click", guarded)
    if guarded < 0 or post_save <= guarded:
        errors.append("cannot isolate guarded normal Save handler")
    else:
        handler = code[guarded:post_save]
        blocked = handler.find("if (_persistentSettingsWriteBlocked)")
        blocked_save = handler.find("Save_Click(sender, e);", blocked)
        ensure = handler.find("EnsurePersistedSettingsFreshBeforeSave();", blocked_save)
        normal_save = handler.find("Save_Click(sender, e);", ensure)
        if min(blocked, blocked_save, ensure, normal_save) < 0 or not blocked < blocked_save < ensure < normal_save:
            errors.append("normal Save must preserve read-only message path and run freshness guard before the original persistence handler")

    close_start = code.find("private void QuantitySettingsWindow_Closing")
    freshness_start = code.find("private void EnsurePersistedSettingsFreshBeforeSave", close_start)
    if close_start < 0 or freshness_start <= close_start:
        errors.append("cannot isolate close guard")
    else:
        handler = code[close_start:freshness_start]
        build = handler.find("current = BuildSettingsFromView();")
        clean = handler.find("if (SettingsEquivalent(current, baseline)) return;")
        readonly = handler.find("if (_persistentSettingsWriteBlocked)")
        readonly_prompt = handler.find("MessageBoxButton.OKCancel", readonly)
        decision = handler.find("MessageBoxButton.YesNoCancel", readonly_prompt)
        cancel = handler.find("answer == MessageBoxResult.Cancel", decision)
        discard = handler.find("answer == MessageBoxResult.No", cancel)
        fresh = handler.find("EnsurePersistedSettingsFreshBeforeSave();", discard)
        save = handler.find("_store.Save(current);", fresh)
        accept = handler.find("AcceptPersistedSettingsBaseline(current);", save)
        if min(build, clean, readonly, readonly_prompt, decision, cancel, discard, fresh, save, accept) < 0 or not (
            build < clean < readonly < readonly_prompt < decision < cancel < discard < fresh < save < accept
        ):
            errors.append("close guard must validate, skip clean, handle read-only, decide Save/Discard/Cancel, revalidate persisted freshness, then save/advance baseline")
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
        for token in ("BuildSettingsFromView()", "_store.Load()", "SettingsEquivalent(current, persisted)", "AcceptPersistedSettingsBaseline(current)"):
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

print("PASS: QS3DSETUP keeps explicit Save/Discard/Cancel close safety, routes normal and close-time persistence through stale-settings freshness checks, preserves future-schema read-only blocking, and advances the baseline only after verified successful persistence.")
