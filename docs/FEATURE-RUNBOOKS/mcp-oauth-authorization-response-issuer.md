# MCP OAuth authorization response issuer binding

Issue: #5695  
Ownership-Key: `mcp.oauth.authorization-response-issuer`

## Problem

The embedded OAuth authorization server already published an RFC 8414 `issuer`, but authorization responses did not advertise or return the RFC 9207 `iss` parameter. A client that validates authorization-response issuer identity can therefore reject the flow even though resource binding, DCR and PKCE are otherwise correct.

This source gap is separate from Cloudflare DNS/TLS/public reachability. Hosted CI cannot prove the live public endpoint or ChatGPT browser flow.

## Contract

- Derive the OAuth issuer from the already validated public MCP resource authority.
- Authorization-server metadata advertises `authorization_response_iss_parameter_supported: true`.
- Successful authorization-code redirects include URL-encoded `iss` equal to that issuer.
- Redirect-based OAuth errors after a validated ChatGPT redirect include the same `iss`.
- Preserve exact ChatGPT redirect allowlisting, exact `/mcp` resource binding, `qs3d:mcp` scope rules, PKCE S256, state echo, authorization-code replay protection, refresh rotation and process binding.
- Do not relax missing or mismatched `resource`, `redirect_uri`, `code_challenge` or `code_challenge_method` validation.

## Verification

Regression-first head: `a443aa6b87a6a1aabcb6058c4f9c05fa9ac55ca0`.

Shared CI run `33885311153` failed at `All discovered feature source guards` while reservation, generic source, PowerShell and package-integrity gates passed, establishing the focused RFC 9207 guard as RED before the production change.

After the production fix, require a fresh exact-head Shared CI run with both `preflight` and `core` successful before merge. Real Cloudflare public endpoint + ChatGPT OAuth + licensed BricsCAD remains LOCAL_ONLY and must be recorded separately; no hosted result is a runtime PASS.
