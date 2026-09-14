#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "upload-v25-held-release-asset.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def validate_source(helper: str) -> None:
    required = (
        ("$uploadBaseUri = $null", "helper must parse UploadBase before authorization"),
        ("[Uri]::TryCreate($UploadBase, [UriKind]::Absolute, [ref]$uploadBaseUri)", "UploadBase must be one absolute URI"),
        ("$uploadBaseUri.Scheme", "upload endpoint scheme must be admitted"),
        ("'https'", "upload endpoint must require HTTPS"),
        ("$uploadBaseUri.Host", "upload endpoint host must be admitted"),
        ("'uploads.github.com'", "upload endpoint must be pinned to uploads.github.com"),
        ("$uploadBaseUri.IsDefaultPort", "upload endpoint must reject non-default ports"),
        ("$uploadBaseUri.UserInfo", "upload endpoint must reject embedded credentials"),
        ("$uploadBaseUri.Fragment", "upload endpoint must reject fragments"),
        ("$uploadBaseUri.Query", "upload endpoint must reject a preexisting query"),
        ("[System.Net.Http.HttpClientHandler]::new()", "helper must own an HTTP handler so redirect policy is explicit"),
        ("$handler.AllowAutoRedirect = $false", "release-asset upload must fail closed on redirects"),
        ("[System.Net.Http.HttpClient]::new($handler)", "HTTP client must use the redirect-disabled handler"),
        ("[System.Net.HttpStatusCode]::Created", "release-asset upload must require HTTP 201 Created"),
    )
    for token, message in required:
        require(token in helper, message)

    require("$response.IsSuccessStatusCode" not in helper, "generic 2xx admission must not replace exact HTTP 201 Created")

    parse_pos = helper.index("[Uri]::TryCreate($UploadBase, [UriKind]::Absolute, [ref]$uploadBaseUri)")
    host_pos = helper.index("'uploads.github.com'")
    handler_pos = helper.index("[System.Net.Http.HttpClientHandler]::new()")
    redirect_pos = helper.index("$handler.AllowAutoRedirect = $false")
    header_pos = helper.index("foreach ($key in $Headers.Keys)")
    post_pos = helper.index("$client.PostAsync($uploadUri, $content)")
    status_pos = helper.index("[System.Net.HttpStatusCode]::Created")
    response_parse_pos = helper.index("$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()")
    require(
        parse_pos < host_pos < handler_pos < redirect_pos < header_pos < post_pos < status_pos < response_parse_pos,
        "endpoint admission must precede redirect policy, authorization, POST, exact status admission, then response parsing",
    )


def main() -> None:
    helper = HELPER.read_text(encoding="utf-8")
    validate_source(helper)

    mutations = (
        ("$handler.AllowAutoRedirect = $false", "$handler.AllowAutoRedirect = $true", "redirect weakening"),
        ("'uploads.github.com'", "'example.invalid'", "host weakening"),
        ("[System.Net.HttpStatusCode]::Created", "[System.Net.HttpStatusCode]::OK", "status weakening"),
    )
    for old, new, label in mutations:
        require(old in helper, f"mutation fixture lost source token for {label}")
        mutant = helper.replace(old, new, 1)
        try:
            validate_source(mutant)
        except SystemExit:
            pass
        else:
            raise SystemExit(f"FAIL: endpoint guard mutation self-test did not reject {label}")

    print("PASS: V25 cloud release upload endpoint is HTTPS/GitHub-bound, redirect-disabled, and exact-201 admitted before asset identity parsing")


if __name__ == "__main__":
    main()
