# Agent status-marker semantics

This document defines the meaning of owner-facing progress markers. It does not define product UI requirements.

## Marker meanings

In agent/GitHub progress reporting:

- `✅` — verified satisfied/successful;
- `❌` — verified failed/blocked/unsatisfied;
- `⏳` — genuinely pending/in progress;
- `➖` — not applicable.

These symbols are control-plane/reporting semantics by default.

Do not turn wording such as `X đỏ`, `V xanh`, `red X`, `green V` into QS3D product artwork, Ribbon icons, branding, assets or production source changes unless the owner explicitly names a product surface and requests that product change.

## Ambiguity

If the wording could mean either agent-progress reporting or a product/UI artifact, fail closed on product mutation until product intent is explicit or independently established by a current registered requirement.

Historical agent-created product interpretations are not proof of current owner intent.

## Pending CI reporting

Pending CI does **not** by itself require a full intermediate lifecycle report. `AGENTS.md` and `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md` remain terminal-first.

However, **when an intermediate progress update is emitted** and CI is pending, describe the exact available evidence instead of saying only `CI pending`.

Include, when observable:

- CI category (branch, protected PR, integration, release);
- workflow/run identifier;
- exact tested SHA/candidate;
- current job/step;
- important completed gates;
- remaining gate that unlocks the next lifecycle action.

If tooling does not expose a requested detail, state that observability limit instead of inventing it.

## Product/UI interpretation

Only treat red/green marker wording as a QS3D UI/art requirement when the owner explicitly anchors it to a named product surface or provides unambiguous product evidence.

Normal product-boundary, reservation, CI, protected-main and runtime rules still apply to that real product task.