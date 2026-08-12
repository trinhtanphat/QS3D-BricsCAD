# Work claim — ElementInstance net concrete non-negative invariant

- Status: `RELEASED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:52:00+07:00`
- Released: `2026-08-12T10:56:00+07:00`
- Baseline main SHA: `1765f34f13baf916ba8c8893794d9364e6da8af5`
- Priority: evidence-driven remote-safe domain quantity integrity

## Initial reason

The initial audit observed that `ElementInstance` validates `GrossConcreteM3` and `DeductionM3` independently as finite non-negative measurements and that reporting requires a non-negative `NetConcreteM3`. The tentative scope considered failing closed when deduction exceeded gross.

## Coordination / release reason

After publishing this claim, the current source was re-read and already contained `Math.Max(0d, GrossConcreteM3 - DeductionM3)`. A targeted history check then found the authoritative earlier lane:

- claim `65b4adc6bcc68d3637f637f0709c6e11d0981b0d`;
- implementation `90e9b4a863cafbde898993a3395438ad24f4cd23`;
- regression `303c4e9dc15bc6c3f06cc4d81e1c98b5a8028424`;
- smoke registration `9aa4f73b268fbc8d22914e9fbb6051b45692e965`;
- completion `de81a936d5125654bd44176dead1c0a658781234`.

That completed lane explicitly establishes floor-zero semantics when deduction exceeds gross, with focused regression coverage. Replacing it with exception semantics would contradict an already-merged, tested repository contract.

No product source, tests, scripts or runtime behavior were changed under this claim. This reservation is therefore released rather than completed.

## Validation boundary

Remote/static verification only. No GitHub Actions were dispatched or rerun, and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.

## Completion condition

`RELEASED`: duplicate/contradictory ownership removed without overwriting the authoritative floor-zero implementation.
