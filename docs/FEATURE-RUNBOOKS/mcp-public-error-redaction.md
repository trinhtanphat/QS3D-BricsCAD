# MCP public error redaction

## Scope
This carrier hardens public MCP error serialization in both embedded server generations. It does not change CAD mutation behavior, retry policy, writer ownership, or tool success semantics.

## Contract
All public `ToolError`, JSON-RPC error, HTTP protocol error, and auxiliary public exception-message fields pass through `SanitizePublicError` before JSON escaping. The sanitizer redacts Authorization/Bearer credentials, common API-key/token/secret/password forms, local filesystem paths, and control characters, then bounds the result to 512 characters.

MCP error code, capability lane, repair metadata shape, JSON-RPC numeric code, and `isError` semantics remain unchanged. Self-healing repair metadata does not embed the raw exception message.

## Failure behavior
Malformed or hostile exception text is sanitized and bounded; it is never used to trigger a retry or replay CAD work. Internal diagnostic recording remains separate from the public response boundary.
