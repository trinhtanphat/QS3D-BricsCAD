#!/usr/bin/env python3
"""Hermetic source contract for V25 qualification profile isolation."""
from __future__ import annotations

import hashlib
import json
import tempfile
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "v25-profile-sandbox.ps1"
WRAPPER = ROOT / "scripts" / "test-bricscad-v25-runtime.ps1"
CORE = ROOT / "scripts" / "test-bricscad-v25-runtime-core.ps1"
RUNBOOK = ROOT / "docs" / "V25-PROFILE-SANDBOX-CONTRACT.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"missing {label}: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise AssertionError(f"forbidden {label}: {token}")


@dataclass(frozen=True)
class Snapshot:
    cur_exists: bool
    cur_kind: str | None
    cur_value: object | None
    names: tuple[str, ...]


class FakeProfiles:
    """Small deterministic model of the helper's protected state boundary."""

    def __init__(self, names: list[str], cur: tuple[str, object] | None) -> None:
        self.names = set(names)
        self.cur = cur

    def capture(self) -> Snapshot:
        cur = self.cur
        return Snapshot(
            cur_exists=cur is not None,
            cur_kind=None if cur is None else cur[0],
            cur_value=None if cur is None else cur[1],
            names=tuple(sorted(self.names)),
        )

    def allocate(self, source: str, candidates: list[str]) -> str:
        if source not in self.names:
            raise RuntimeError("source profile missing")
        for candidate in candidates:
            if candidate in self.names:
                continue
            self.names.add(candidate)
            return candidate
        raise RuntimeError("nonce collision budget exhausted")

    def restore(self, before: Snapshot, nonce: str) -> None:
        if nonce in before.names or not nonce.startswith("QS3D-AUTO-"):
            raise RuntimeError("not runner-owned")
        # Mirror the production safety order: make the protected pointer safe first,
        # then remove only the runner-owned nonce profile.
        self.cur = None if not before.cur_exists else (before.cur_kind or "", before.cur_value)
        self.names.discard(nonce)
        if self.capture() != before:
            raise RuntimeError("protected profile boundary drift")


def model_regressions() -> None:
    # Normal pass / mutation by host: exact pointer is restored before nonce removal.
    state = FakeProfiles(["Default", "QS3D-V25-TEST"], ("String", "Default"))
    before = state.capture()
    nonce = state.allocate("QS3D-V25-TEST", ["QS3D-AUTO-a"])
    state.cur = ("String", nonce)
    state.restore(before, nonce)
    assert state.capture() == before

    # Failure before spawn still owns a nonce that finally cleanup can remove.
    state = FakeProfiles(["Default", "Source"], None)
    before = state.capture()
    nonce = state.allocate("Source", ["QS3D-AUTO-b"])
    state.restore(before, nonce)
    assert state.capture() == before

    # Failure after spawn / timeout uses the same finally contract; extra profile drift fails closed.
    state = FakeProfiles(["Default", "Source"], ("ExpandString", "%TEMP%"))
    before = state.capture()
    nonce = state.allocate("Source", ["QS3D-AUTO-c"])
    state.cur = ("String", nonce)
    state.names.add("UnexpectedHostProfile")
    try:
        state.restore(before, nonce)
    except RuntimeError as exc:
        assert "drift" in str(exc)
    else:
        raise AssertionError("unexpected profile inventory drift must fail closed")

    # Pre-existing nonce collision is skipped, never overwritten or deleted.
    state = FakeProfiles(["Source", "QS3D-AUTO-collision"], ("String", "Source"))
    before = state.capture()
    nonce = state.allocate("Source", ["QS3D-AUTO-collision", "QS3D-AUTO-fresh"])
    assert nonce == "QS3D-AUTO-fresh"
    state.restore(before, nonce)
    assert "QS3D-AUTO-collision" in state.names
    assert state.capture() == before


def source_contract() -> None:
    helper = HELPER.read_text(encoding="utf-8")
    wrapper = WRAPPER.read_text(encoding="utf-8")
    core = CORE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    for token, label in [
        ("Software\\Bricsys\\BricsCAD\\V25x64\\en_US\\Profiles", "V25-only registry root"),
        ("Assert-Qs3dNoBricsCadProcess", "zero-process guard"),
        ("DoNotExpandEnvironmentNames", "exact registry string capture"),
        ("GetValueKind('CurProfile')", "CurProfile type capture"),
        ("ProfileInventorySha256", "sanitized inventory hash"),
        ("Copy-Qs3dRegistryTree", "nonce profile clone"),
        ("$script:Qs3dV25NoncePrefix", "runner-owned nonce prefix"),
        ("DeleteSubKeyTree($nonceName, $false)", "nonce-only delete"),
        ("SetValue('CurProfile'", "exact pointer restore"),
        ("DeleteValue('CurProfile', $false)", "absent pointer restore"),
        ("profile sandbox cleanup could not restore", "fail-closed restore"),
    ]:
        require(helper, token, label)

    forbid(helper, "V26x64", "cross-major registry mutation")
    forbid(helper, "DeleteSubKeyTree($SourceProfile", "source profile deletion")

    restore_body = helper[helper.index("function Restore-Qs3dV25ProfileSandbox") :]
    set_pointer = restore_body.index("$profiles.SetValue('CurProfile'")
    delete_pointer = restore_body.index("$profiles.DeleteValue('CurProfile', $false)")
    delete_nonce = restore_body.index("$profiles.DeleteSubKeyTree($nonceName, $false)")
    if max(set_pointer, delete_pointer) > delete_nonce:
        raise AssertionError("protected CurProfile restoration must precede runner-owned nonce deletion")

    for token, label in [
        ("test-bricscad-v25-runtime-core.ps1", "stable runtime core delegation"),
        ("New-Qs3dV25ProfileSandbox", "sandbox allocation before runtime"),
        ("Profile = $effectiveProfile", "nonce profile launch"),
        ("Restore-Qs3dV25ProfileSandbox", "finally restoration"),
        ("Assert-Qs3dNoBricsCadProcess", "zero-process restoration boundary"),
        ("CloseMainWindow()", "graceful owned-host shutdown"),
        ("Microsoft.PowerShell.Management\\Stop-Process", "bounded force fallback"),
        ("profile-sandbox-metadata.json", "sanitized local evidence"),
        ("profile_inventory_before_sha256", "inventory evidence"),
        ("force_close_fallback_used", "shutdown evidence"),
    ]:
        require(wrapper, token, label)

    if wrapper.index("New-Qs3dV25ProfileSandbox") > wrapper.index(". $coreScript @coreArgs"):
        raise AssertionError("profile sandbox must be allocated before host launch")
    if wrapper.index("Restore-Qs3dV25ProfileSandbox") < wrapper.index("finally {"):
        raise AssertionError("profile restoration must remain in finally")

    for token, label in [
        (". ./scripts/v25-profile-sandbox.ps1", "dot-source composition example"),
        ("New-Qs3dV25ProfileSandbox", "allocation contract"),
        ("Restore-Qs3dV25ProfileSandbox", "finally cleanup contract"),
        ("QS3D-AUTO-", "runner-owned nonce boundary"),
        ("restore `CurProfile` before deleting the nonce", "cleanup ordering rule"),
        ("does not make other `/P` launchers profile-safe", "bounded migration rule"),
    ]:
        require(runbook, token, label)

    # Preserve the mature runtime validation in an isolated core instead of reimplementing it.
    for token in [
        "Start-Process -FilePath $bricscadExe",
        "QS3DRUNTIMEPROBE",
        "ribbon_ready",
        "workspace_palette_visible",
        "PrintWindow",
    ]:
        require(core, token, f"legacy runtime contract {token}")


def deterministic_evidence_shape() -> None:
    values = ["Default", "QS3D-V25-TEST"]
    digest1 = hashlib.sha256("\0".join(values).encode("utf-8")).hexdigest()
    digest2 = hashlib.sha256("\0".join(values).encode("utf-8")).hexdigest()
    assert digest1 == digest2 and len(digest1) == 64
    with tempfile.TemporaryDirectory() as temp:
        p = Path(temp) / "evidence.json"
        p.write_text(json.dumps({"inventory_sha256": digest1}, sort_keys=True), encoding="utf-8")
        loaded = json.loads(p.read_text(encoding="utf-8"))
        assert set(loaded) == {"inventory_sha256"}
        assert loaded["inventory_sha256"] == digest1


def main() -> int:
    model_regressions()
    source_contract()
    deterministic_evidence_shape()
    print("V25 profile sandbox preflight PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())