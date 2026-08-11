# Agent work claims

This directory records temporary agent ownership of implementation and qualification lanes.

Read [`docs/AGENT-WORK-REGISTRATION.md`](../AGENT-WORK-REGISTRATION.md) before creating or changing a claim. Every substantive lane requires a claim-only commit on `origin/main` before work starts.

Quick inspection:

```powershell
rg -n -C 3 '^- Status: `(ACTIVE|BLOCKED)`\r?$' docs/agent-work-claims
```

`ACTIVE` and `BLOCKED` scopes remain reserved. `COMPLETED` and `RELEASED` scopes are inactive historical records. Do not delete old claims and do not infer that an old reservation is abandoned without an explicit release or owner-coordinated takeover.
