from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
BOOTSTRAP = WORKFLOWS / "_bootstrap-security-hardening.yml"

PINS = {
    "actions/checkout": ("3d3c42e5aac5ba805825da76410c181273ba90b1", "v7.0.1"),
    "actions/setup-python": ("5fda3b95a4ea91299a34e894583c3862153e4b97", "v7"),
    "actions/setup-dotnet": ("a98b56852c35b8e3190ac28c8c2271da59106c68", "v6"),
    "actions/upload-artifact": ("043fb46d1a93c77aae656e7c1c64a875d1fc6a0a", "v7"),
    "actions/download-artifact": ("3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c", "v8"),
    "actions/cache": ("55cc8345863c7cc4c66a329aec7e433d2d1c52a9", "v6.1.0"),
    "actions/cache/restore": ("55cc8345863c7cc4c66a329aec7e433d2d1c52a9", "v6.1.0"),
    "actions/cache/save": ("55cc8345863c7cc4c66a329aec7e433d2d1c52a9", "v6.1.0"),
}

USES_LINE = re.compile(
    r"^(?P<indent>\s*-\s+uses:\s*)(?P<action>[^@\s#]+)@(?P<ref>[^\s#]+)(?:\s+#.*)?$"
)
FULL_SHA = re.compile(r"^[0-9a-fA-F]{40}$")

OLD_HTTP_MIRROR = "http://103.9.157.20/BricsCAD-V25.2.10-1-en_US%28x64%29.msi"
OFFICIAL_HTTPS = "https://storage.googleapis.com/production-boa-storage/ftp/release/en_US/BricsCAD/Windows/25.2.10/BricsCAD-V25.2.10-1-en_US%28x64%29.msi"

changed: list[Path] = []
unresolved: list[str] = []

for workflow in sorted(WORKFLOWS.glob("*.yml")):
    if workflow == BOOTSTRAP:
        continue
    original = workflow.read_text(encoding="utf-8")
    lines: list[str] = []
    for line_number, line in enumerate(original.splitlines(), start=1):
        match = USES_LINE.match(line)
        if not match:
            lines.append(line)
            continue

        action = match.group("action")
        ref = match.group("ref")
        if action.startswith("./"):
            lines.append(line)
            continue
        pin = PINS.get(action)
        if pin:
            sha, version = pin
            lines.append(f"{match.group('indent')}{action}@{sha} # {version}")
            continue
        if FULL_SHA.fullmatch(ref):
            lines.append(line)
            continue
        unresolved.append(f"{workflow.relative_to(ROOT)}:{line_number}: {action}@{ref}")
        lines.append(line)

    updated = "\n".join(lines) + ("\n" if original.endswith(("\n", "\r\n")) else "")
    updated = updated.replace(OLD_HTTP_MIRROR, OFFICIAL_HTTPS)
    updated = updated.replace(
        "$mirrorUri.Scheme -ne 'http' -or $mirrorUri.Host -ne '103.9.157.20'",
        "$mirrorUri.Scheme -ne 'https' -or $mirrorUri.Host -ne 'storage.googleapis.com'",
    )

    if workflow.name == "ci.yml":
        anchor = (
            "      - name: Repository professionalism gate\n"
            "        run: python scripts/preflight-repository-professionalism.py\n"
        )
        gate = (
            anchor
            + "      - name: Immutable GitHub Actions supply-chain gate\n"
            + "        run: python scripts/check-actions-pinned.py\n"
        )
        if "Immutable GitHub Actions supply-chain gate" not in updated:
            if anchor not in updated:
                raise SystemExit("Could not locate CI professionalism gate insertion anchor.")
            updated = updated.replace(anchor, gate, 1)

    if updated != original:
        workflow.write_text(updated, encoding="utf-8", newline="\n")
        changed.append(workflow)

if unresolved:
    print("Refusing to leave mutable external actions unresolved:")
    for item in unresolved:
        print(f" - {item}")
    raise SystemExit(1)

# The bootstrap is intentionally one-shot. Its removal is part of the same hardening commit.
if BOOTSTRAP.exists():
    BOOTSTRAP.unlink()
    changed.append(BOOTSTRAP)

print("Hardened workflow files:")
for path in changed:
    print(f" - {path.relative_to(ROOT)}")
