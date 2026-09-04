#!/usr/bin/env python3
"""Fail closed unless embedded MCP OAuth authorization responses bind RFC 9207 issuer identity."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOAuthAuthorizationServer.cs"


def fail(message: str) -> None:
    print("ERROR: MCP OAuth authorization-response issuer preflight failed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(label + " is missing: " + needle)


def section(text: str, start: str, end: str, label: str) -> str:
    start_at = text.find(start)
    end_at = text.find(end, start_at + len(start)) if start_at >= 0 else -1
    if start_at < 0 or end_at <= start_at:
        fail("could not isolate " + label)
    return text[start_at:end_at]


def main() -> int:
    if not SOURCE.is_file():
        fail("OAuth authorization-server source is missing")
    text = SOURCE.read_text(encoding="utf-8")

    require(
        text,
        "var issuer = resourceUri.GetLeftPart(UriPartial.Authority);",
        "issuer derivation from the validated public MCP resource",
    )

    metadata = section(
        text,
        'string.Equals(path, "/.well-known/oauth-authorization-server"',
        'string.Equals(path, "/oauth/register"',
        "authorization-server metadata branch",
    )
    require(metadata, '\\"issuer\\":\\"', "RFC 8414 issuer metadata")
    require(
        metadata,
        '\\"authorization_response_iss_parameter_supported\\":true',
        "RFC 9207 metadata support advertisement",
    )

    authorize_route = section(
        text,
        'string.Equals(path, "/oauth/authorize"',
        'string.Equals(path, "/oauth/token"',
        "authorization endpoint routing",
    )
    require(
        authorize_route,
        "Authorize(query, resource, issuer, signingSecret)",
        "validated issuer handoff into authorization processing",
    )

    authorize = section(
        text,
        "private static McpOAuthHttpResponse Authorize(",
        "private static McpOAuthHttpResponse ExchangeToken(",
        "authorization response builder",
    )
    require(authorize, "string issuer", "authorization response issuer parameter")
    require(authorize, 'Required(values, "resource"', "OAuth resource indicator admission")
    require(authorize, 'Required(values, "code_challenge"', "PKCE challenge admission")
    require(authorize, 'Required(values, "code_challenge_method"', "PKCE method admission")
    require(authorize, 'string.Equals(challengeMethod, "S256"', "PKCE S256 enforcement")
    require(
        authorize,
        'location += "&iss=" + Uri.EscapeDataString(issuer);',
        "successful authorization response issuer",
    )

    error_calls = authorize.count("RedirectOAuthError(")
    issuer_error_calls = authorize.count("RedirectOAuthError(redirect, values, issuer,")
    if error_calls == 0 or issuer_error_calls != error_calls:
        fail("every redirect OAuth error must receive the exact validated issuer")

    redirect_error = section(
        text,
        "private static McpOAuthHttpResponse RedirectOAuthError(",
        "private static McpOAuthHttpResponse Redirect(",
        "redirect OAuth error builder",
    )
    require(redirect_error, "string issuer", "redirect error issuer parameter")
    require(
        redirect_error,
        'location += "&iss=" + Uri.EscapeDataString(issuer);',
        "redirect error RFC 9207 issuer",
    )

    print("PASS OAuth authorization success/error responses bind the advertised RFC 9207 issuer")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
