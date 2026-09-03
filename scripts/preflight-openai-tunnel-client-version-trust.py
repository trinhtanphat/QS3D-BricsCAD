#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOpenAiSecureTunnel.cs"


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8") if SOURCE.exists() else ""
    errors = []

    if not SOURCE.exists():
        errors.append("missing McpOpenAiSecureTunnel.cs")
    else:
        for token in (
            "MinimumSupportedTunnelClientVersion",
            "new Version(0, 0, 11)",
            "TryParseTunnelClientVersion",
            "IsSupportedTunnelClientVersion",
            "tunnel-client version is unsupported",
            "mcp.extra_headers",
            "mcp.discovery_extra_headers",
        ):
            if token not in source:
                errors.append(f"version/capability trust contract missing: {token}")

        trust_start = source.find("private static bool TryVerifyClientTrust")
        trust_end = source.find("private static uint VerifyAuthenticode", trust_start)
        trust_body = source[trust_start:trust_end] if trust_start >= 0 and trust_end > trust_start else ""
        read_version = trust_body.find("TryReadVersion")
        support_check = trust_body.find("IsSupportedTunnelClientVersion")
        signature_check = trust_body.find("VerifyAuthenticode")
        if read_version < 0:
            errors.append("TryVerifyClientTrust must read tunnel-client --version")
        if support_check < 0:
            errors.append("TryVerifyClientTrust must reject unsupported tunnel-client versions")
        if signature_check < 0:
            errors.append("TryVerifyClientTrust must preserve Authenticode verification")
        if read_version >= 0 and support_check >= 0 and support_check < read_version:
            errors.append("minimum capability check must run after version is read")
        if support_check >= 0 and signature_check >= 0 and support_check > signature_check:
            errors.append("minimum capability check must fail closed before signer/hash acceptance")

    if errors:
        print("FAIL: OpenAI tunnel-client version/trust capability guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: OpenAI tunnel-client trust requires >=0.0.11 capability before signer/hash acceptance.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
