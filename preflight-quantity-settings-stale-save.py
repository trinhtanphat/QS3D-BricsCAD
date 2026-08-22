#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MAIN = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.UnsavedChanges.cs"
errors = []

if not MAIN.is_file():
    errors.append("missing QuantitySettingsWindow.xaml.cs")
else:
    main = MAIN.read_text(encoding="utf-8")
    save_start = main.find("private void Save_Click(object sender, RoutedEventArgs e)")
    next_method = main.find("private static bool IsUnsupportedSettingsSchema", save_start)
    if save_start < 0 or next_method <= save_start:
        errors.append("cannot isolate existing Quantity Settings Save_Click")
    else:
        save = main[save_start:next_method]
        if save.count("_store.Save(current);") != 1:
            errors.append("existing Save_Click must remain the single normal persistence implementation")
        if "if (_persistentSettingsWriteBlocked)" not in save:
            errors.append("existing Save_Click must retain future-schema read-only blocking")

if not CODE.is_file():
    errors.append("missing QuantitySettingsWindow.UnsavedChanges.cs")
else:
    code = CODE.read_text(encoding="utf-8")
    required = (
        "private bool _persistedSettingsBaselineVerified;",
        "_persistedSettingsBaselineVerified = TryVerifyPersistedSettingsBaseline(baseline);",
        "SaveSettingsButton.Click -= Save_Click;",
        "SaveSettingsButton.Click += QuantitySettingsGuardedSave_Click;",
        "SaveSettingsButton.Click += QuantitySettingsSaveBaseline_Click;",
        "private void QuantitySettingsGuardedSave_Click(object sender, RoutedEventArgs e)",
        "EnsurePersistedSettingsFreshBeforeSave();",
        "private void EnsurePersistedSettingsFreshBeforeSave()",
        "private bool TryVerifyPersistedSettingsBaseline(QuantityCalculationSettings baseline)",
        "private void AcceptPersistedSettingsBaseline(QuantityCalculationSettings settings)",
        "if (_persistedSettingsBaselineVerified) throw;",
        "if (!SettingsEquivalent(persisted, baseline))",
        "_persistedSettingsBaselineVerified = true;",
    )
    for token in required:
        if token not in code:
            errors.append("Quantity Settings stale-save guard missing token: " + token)

    init = code.find("private void InitializeUnsavedChangesTracking()")
    normal = code.find("private void QuantitySettingsGuardedSave_Click", init)
    post = code.find("private void QuantitySettingsSaveBaseline_Click", normal)
    close = code.find("private void QuantitySettingsWindow_Closing", post)
    ensure = code.find("private void EnsurePersistedSettingsFreshBeforeSave", close)
    verify = code.find("private bool TryVerifyPersistedSettingsBaseline", ensure)
    accept = code.find("private void AcceptPersistedSettingsBaseline", verify)
    equivalent = code.find("private static bool SettingsEquivalent", accept)
    if min(init, normal, post, close, ensure, verify, accept, equivalent) < 0 or not init < normal < post < close < ensure < verify < accept < equivalent:
        errors.append("Quantity Settings stale-save helper layout drifted")

    if init >= 0 and normal > init:
        handler = code[init:normal]
        detach = handler.find("SaveSettingsButton.Click -= Save_Click;")
        guarded = handler.find("SaveSettingsButton.Click += QuantitySettingsGuardedSave_Click;")
        baseline = handler.find("SaveSettingsButton.Click += QuantitySettingsSaveBaseline_Click;")
        if min(detach, guarded, baseline) < 0 or not detach < guarded < baseline:
            errors.append("normal Save must replace direct Save_Click with freshness wrapper before post-save baseline tracking")

    if normal >= 0 and post > normal:
        handler = code[normal:post]
        readonly = handler.find("if (_persistentSettingsWriteBlocked)")
        readonly_save = handler.find("Save_Click(sender, e);", readonly)
        fresh = handler.find("EnsurePersistedSettingsFreshBeforeSave();", readonly_save)
        save = handler.find("Save_Click(sender, e);", fresh)
        if min(readonly, readonly_save, fresh, save) < 0 or not readonly < readonly_save < fresh < save:
            errors.append("guarded normal Save must preserve read-only message path and check persisted freshness before persistence")
        if "_store.Save(" in handler:
            errors.append("normal freshness wrapper must delegate to existing Save_Click instead of duplicating persistence")

    if close >= 0 and ensure > close:
        handler = code[close:ensure]
        decision = handler.find("MessageBoxButton.YesNoCancel")
        discard = handler.find("answer == MessageBoxResult.No", decision)
        fresh = handler.find("EnsurePersistedSettingsFreshBeforeSave();", discard)
        save = handler.find("_store.Save(current);", fresh)
        accept_baseline = handler.find("AcceptPersistedSettingsBaseline(current);", save)
        if min(decision, discard, fresh, save, accept_baseline) < 0 or not decision < discard < fresh < save < accept_baseline:
            errors.append("close-time Save must revalidate persisted freshness after user chooses Save and advance baseline only after persistence")

    if ensure >= 0 and verify > ensure:
        handler = code[ensure:verify]
        baseline = handler.find("var baseline = _persistedSettingsBaseline;")
        load = handler.find("persisted = _store.Load();")
        normalize = handler.find("persisted.NormalizeAndValidate();", load)
        verified_fail = handler.find("if (_persistedSettingsBaselineVerified) throw;", normalize)
        recovery_return = handler.find("return;", verified_fail)
        compare = handler.find("if (!SettingsEquivalent(persisted, baseline))", recovery_return)
        verified = handler.find("_persistedSettingsBaselineVerified = true;", compare)
        if min(baseline, load, normalize, verified_fail, recovery_return, compare, verified) < 0 or not baseline < load < normalize < verified_fail < recovery_return < compare < verified:
            errors.append("freshness helper must load/normalize persisted state, block failures for verified baselines, preserve initial unreadable recovery, compare state, then verify baseline")
        if "_store.Save(" in handler:
            errors.append("freshness helper must be read-only")

    if verify >= 0 and accept > verify:
        handler = code[verify:accept]
        for token in ("var persisted = _store.Load();", "persisted.NormalizeAndValidate();", "return SettingsEquivalent(baseline, persisted);", "return false;"):
            if token not in handler:
                errors.append("initial baseline verification missing: " + token)

    if accept >= 0 and equivalent > accept:
        handler = code[accept:equivalent]
        if "_persistedSettingsBaseline = settings.Clone();" not in handler or "_persistedSettingsBaselineVerified = true;" not in handler:
            errors.append("successful persistence must advance and verify the structural baseline")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: QS3DSETUP intercepts normal and close-time saves with structural persisted-state freshness checks, blocks external changes, preserves initial unreadable recovery, and advances the verified baseline only after successful persistence.")
