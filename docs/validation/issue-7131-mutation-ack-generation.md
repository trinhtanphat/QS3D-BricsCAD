# Issue 7131 — durable mutation ACK generation affinity

## Root cause
`MarkApplied` records the applied-time stable drawing fingerprint, but durable promotion previously selected records only by managed `Document` reference and overwrote that identity with the save-time fingerprint.
A same managed wrapper can therefore survive native database replacement and incorrectly promote an ACK from the predecessor generation as durable in the successor generation.

## Contract
- Build a stable save-time identity before entering ledger mutation.
- Select only Applied records for the same managed document.
- Revalidate every candidate's applied-time stable fingerprint against the save-time fingerprint before changing any ACK state.
- If any candidate cannot prove the same identity, leave all candidates Applied and report zero durable promotions.
- Never overwrite applied-time provenance during promotion.
- Preserve existing atomic rollback to Applied if durable ledger persistence fails.

## Safety review
This change does not retry or replay CAD mutations, does not add a writer, and does not change application/document/native command authority.
A verified CAD save remains a verified save even if ACK durability cannot be proven; only the acknowledgement promotion fails closed.

## Runtime classification
Source/static and deterministic preflight evidence is REMOTE_SAFE.
Same-wrapper database replacement, native save timing, and licensed BricsCAD V25/V26 behavior remain LOCAL_ONLY / NO_RESULT until exercised in the real host.
