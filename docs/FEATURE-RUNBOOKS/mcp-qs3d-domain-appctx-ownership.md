# MCP QS3D-domain application-context mutation ownership

## Failure class

The `qs3d_project_bind`, `qs3d_project_reload`, `qs3d_run_command`, and `qs3d_place_single_footing` tools are mutations. They must not execute through the bounded diagnostic CAD-context dispatcher, because a response timeout can occur after the callback has started and while project/native state is still changing.

That timing creates a ghost mutation: the caller sees failure and may release/retry ownership while the original callback continues. The resulting risks are duplicate sidecar writes, duplicate native-command admission, duplicate entity creation, stale active-document publication, and tool-result/CAD-state divergence.

## Required ownership contract

- Queue QS3D-domain mutations through a mutation-owned application-context work item.
- A bounded timeout may cancel only a still-queued callback before it crosses the start boundary.
- Once the callback becomes running, retain caller/process-global writer ownership until that exact callback reaches terminal completion.
- Never automatically retry or replay an uncertain started mutation.
- Capture the exact active managed `Document` and non-zero native `Database.UnmanagedObject` generation before mutation work.
- Revalidate the exact active managed document and native database generation before publishing success.
- Preserve project backing-store freshness checks for bind/reload and `McpCadMutationCoordinator` ownership for queued native commands.
- Preserve emergency-stop/current-mutation checks, bounded public errors, and fail-soft capability state.

## Runtime qualification

Source/static guards and protected CI are REMOTE_SAFE. Licensed BricsCAD scheduling, same-wrapper native database replacement/disposal timing, and AccessViolation evidence remain LOCAL_ONLY / NO_RESULT until executed in a licensed host.
