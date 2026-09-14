#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts/publish-v26-release.ps1"
HELPER = ROOT / "scripts/invoke-v26-held-release-upload.ps1"


def validate(publisher: str, helper: str) -> list[str]:
    errors: list[str] = []

    required_publisher = [
        "& .\\scripts\\invoke-v26-held-release-upload.ps1",
        "$admittedAssets[$name] = $admittedAsset",
        "$expectedLength = [int64]$admittedAssets[$expectedAsset].Length",
        "$expectedHash = [string]$admittedAssets[$expectedAsset].Sha256",
        "-AdmittedAssets $admittedAssets",
    ]
    for token in required_publisher:
        if token not in publisher:
            errors.append(f"V26 publisher missing held-generation token: {token}")

    for forbidden in [
        "Invoke-RestMethod -Method Post -Uri ($uploadBase + '?name=' + [Uri]::EscapeDataString($name)) -Headers $headers -ContentType $contentType -InFile $asset",
        "$localLength = [int64](Get-Item -LiteralPath $localAsset).Length",
        "verify-v26-held-file.ps1 -Operation Hash -Path $localAsset",
    ]:
        if forbidden in publisher:
            errors.append(f"V26 publisher still reopens/uploads by pathname: {forbidden}")

    required_helper = [
        "[IO.FileShare]::Read",
        "[Security.Cryptography.SHA256]::Create()",
        "$held.Stream.Position = 0",
        "Add-Type -AssemblyName System.Net.Http",
        "[System.Net.Http.StreamContent]::new($held.Stream)",
        "UploadedAssetId",
        "Sha256",
        "CanonicalPath",
        "LastWriteTimeUtcTicks",
        "ReparsePoint",
        "[Uri]::TryCreate($UploadBase, [UriKind]::Absolute, [ref]$uploadUri)",
        "[string]::Equals($uploadUri.Scheme, 'https', [StringComparison]::OrdinalIgnoreCase)",
        "[string]::Equals($uploadUri.Host, 'uploads.github.com', [StringComparison]::OrdinalIgnoreCase)",
        "$uploadUri.IsDefaultPort",
        "[string]::IsNullOrEmpty($uploadUri.UserInfo)",
        "[string]::IsNullOrEmpty($uploadUri.Fragment)",
        "[string]::IsNullOrEmpty($uploadUri.Query)",
        "[System.Net.Http.HttpClientHandler]::new()",
        "$handler.AllowAutoRedirect = $false",
        "[System.Net.Http.HttpClient]::new($handler)",
        "$response.StatusCode -ne [System.Net.HttpStatusCode]::Created",
        "ConvertFrom-Json -ErrorAction Stop",
        "throw \"V26 held asset upload returned invalid JSON for $name.\"",
        "[string]::Equals([string]$uploaded.state, 'uploaded', [StringComparison]::Ordinal)",
        "$expectedDigest = 'sha256:' + $hashHex.ToLowerInvariant()",
        "$uploadedDigest = ([string]$uploaded.digest).Trim()",
        "$uploadedDigest -notmatch '^sha256:[0-9A-Fa-f]{64}$'",
        "[string]::Equals($uploadedDigest, $expectedDigest, [StringComparison]::OrdinalIgnoreCase)",
    ]
    for token in required_helper:
        if token not in helper:
            errors.append(f"V26 held-upload helper missing invariant token: {token}")

    if "$response.IsSuccessStatusCode" in helper:
        errors.append("V26 held-upload helper must require exact HTTP 201, not generic 2xx")
    if re.search(r"throw[^\r\n]*\$responseBody", helper):
        errors.append("V26 held-upload helper must not expose remote response bodies in thrown/public errors")

    if helper:
        bootstrap_pos = helper.find("Add-Type -AssemblyName System.Net.Http")
        first_http_type_pos = helper.find("[System.Net.Http.")
        if bootstrap_pos < 0 or first_http_type_pos < 0 or bootstrap_pos >= first_http_type_pos:
            errors.append("V26 held-upload helper must preload System.Net.Http before the first System.Net.Http type resolution")

        hash_pos = helper.find("ComputeHash($held.Stream)")
        rewind_pos = helper.find("$held.Stream.Position = 0")
        content_pos = helper.find("[System.Net.Http.StreamContent]::new($held.Stream)")
        send_pos = helper.find("SendAsync")
        dispose_pos = helper.rfind("$held.Stream.Dispose()")
        if min(hash_pos, rewind_pos, content_pos, send_pos, dispose_pos) < 0 or not (
            hash_pos < rewind_pos < content_pos < send_pos < dispose_pos
        ):
            errors.append("V26 held-upload helper must hash -> rewind -> stream-upload -> dispose the same admitted generation")

        parse_pos = helper.find("[Uri]::TryCreate($UploadBase, [UriKind]::Absolute, [ref]$uploadUri)")
        scheme_pos = helper.find("[string]::Equals($uploadUri.Scheme, 'https', [StringComparison]::OrdinalIgnoreCase)")
        host_pos = helper.find("[string]::Equals($uploadUri.Host, 'uploads.github.com', [StringComparison]::OrdinalIgnoreCase)")
        port_pos = helper.find("$uploadUri.IsDefaultPort")
        user_pos = helper.find("[string]::IsNullOrEmpty($uploadUri.UserInfo)")
        fragment_pos = helper.find("[string]::IsNullOrEmpty($uploadUri.Fragment)")
        query_pos = helper.find("[string]::IsNullOrEmpty($uploadUri.Query)")
        auth_pos = helper.find("DefaultRequestHeaders.Authorization")
        handler_pos = helper.find("[System.Net.Http.HttpClientHandler]::new()")
        redirect_pos = helper.find("$handler.AllowAutoRedirect = $false")
        client_pos = helper.find("[System.Net.Http.HttpClient]::new($handler)")
        request_pos = helper.find("[System.Net.Http.HttpRequestMessage]::new")
        status_pos = helper.find("$response.StatusCode -ne [System.Net.HttpStatusCode]::Created")
        body_pos = helper.find("ReadAsStringAsync")
        json_pos = helper.find("ConvertFrom-Json -ErrorAction Stop")
        state_pos = helper.find("[string]::Equals([string]$uploaded.state, 'uploaded', [StringComparison]::Ordinal)")
        digest_pos = helper.find("$expectedDigest = 'sha256:' + $hashHex.ToLowerInvariant()")
        required_positions = [
            parse_pos,
            scheme_pos,
            host_pos,
            port_pos,
            user_pos,
            fragment_pos,
            query_pos,
            auth_pos,
            handler_pos,
            redirect_pos,
            client_pos,
            request_pos,
            send_pos,
            status_pos,
            body_pos,
            json_pos,
            state_pos,
            digest_pos,
        ]
        if min(required_positions) < 0:
            errors.append("V26 held-upload helper is missing endpoint/status/ack ordering evidence")
        else:
            if not (parse_pos < scheme_pos < host_pos < port_pos < user_pos < fragment_pos < query_pos < auth_pos):
                errors.append("V26 upload URI authority must be fully admitted before bearer authorization")
            if not (handler_pos < redirect_pos < client_pos < auth_pos < request_pos < send_pos < status_pos < body_pos < json_pos < state_pos < digest_pos):
                errors.append("V26 upload must disable redirects, require exact 201, parse bounded JSON, then bind state/digest")

    return errors


def require_mutation_rejected(name: str, publisher: str, helper: str) -> None:
    if not validate(publisher, helper):
        print(f"ERROR: mutation escaped V26 held-upload guard: {name}")
        sys.exit(1)


def main() -> int:
    errors: list[str] = []
    if not PUBLISHER.is_file():
        errors.append("missing scripts/publish-v26-release.ps1")
        publisher = ""
    else:
        publisher = PUBLISHER.read_text(encoding="utf-8")

    if not HELPER.is_file():
        errors.append("missing scripts/invoke-v26-held-release-upload.ps1")
        helper = ""
    else:
        helper = HELPER.read_text(encoding="utf-8")

    errors.extend(validate(publisher, helper))
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    mutations = {
        "redirect enabled": helper.replace("$handler.AllowAutoRedirect = $false", "$handler.AllowAutoRedirect = $true", 1),
        "host weakened": helper.replace("[string]::Equals($uploadUri.Host, 'uploads.github.com', [StringComparison]::OrdinalIgnoreCase)", "$uploadUri.Host -like '*.github.com'", 1),
        "port admission removed": helper.replace("-or -not $uploadUri.IsDefaultPort", "", 1),
        "userinfo admission removed": helper.replace("-or -not [string]::IsNullOrEmpty($uploadUri.UserInfo)", "", 1),
        "fragment admission removed": helper.replace("-or -not [string]::IsNullOrEmpty($uploadUri.Fragment)", "", 1),
        "query admission removed": helper.replace("-or -not [string]::IsNullOrEmpty($uploadUri.Query)", "", 1),
        "generic 2xx": helper.replace("$response.StatusCode -ne [System.Net.HttpStatusCode]::Created", "-not $response.IsSuccessStatusCode", 1),
        "response leak": helper.replace("throw \"V26 held asset upload failed for $name with HTTP $([int]$response.StatusCode).\"", "throw \"V26 held asset upload failed for $name with HTTP $([int]$response.StatusCode): $responseBody\"", 1),
        "uploaded state removed": helper.replace("if (-not [string]::Equals([string]$uploaded.state, 'uploaded', [StringComparison]::Ordinal)) {", "if ($false) {", 1),
        "digest equality removed": helper.replace("-not [string]::Equals($uploadedDigest, $expectedDigest, [StringComparison]::OrdinalIgnoreCase)", "$false", 1),
        "raw JSON parse": helper.replace("$uploaded = $responseBody | ConvertFrom-Json -ErrorAction Stop", "$uploaded = $responseBody | ConvertFrom-Json", 1),
    }
    for name, mutated in mutations.items():
        if mutated == helper:
            print(f"ERROR: mutation fixture did not alter source: {name}")
            return 1
        require_mutation_rejected(name, publisher, mutated)

    print("PASS V26 held release upload generation, endpoint, redirect, status, state, digest, and error authority")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
