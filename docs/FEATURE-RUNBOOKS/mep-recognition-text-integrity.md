# MEP recognition text integrity

## Scope

This runbook covers the Core admission boundary for configurable MEP recognition rule text: rule `Id`, `Category`, and recognition token values. It is a repository-safe deterministic integrity contract and does not constitute licensed BricsCAD runtime qualification.

## Defect boundary

Recognition profiles can be created directly through the public Core constructors, independently of the V25/V26 XML profile store. Before issue #4405, `MepRecognitionRule` rejected blank and control-bearing text but accepted malformed UTF-16 such as lone high/low surrogates. That allowed an invalid canonical in-memory profile to exist and deferred failure until a later XML persistence/serialization boundary.

Core admission is now authoritative: malformed UTF-16 is rejected when the rule is constructed. The persistence adapter may still apply its own XML/profile validation, but it must not be the first layer that discovers a malformed Core recognition identity.

## Required behavior

- Blank/whitespace-only text remains invalid.
- Existing surrounding-whitespace normalization remains unchanged.
- Control characters remain invalid.
- A high surrogate must be followed immediately by a low surrogate.
- A lone low surrogate is invalid.
- Valid supplementary-plane scalar values represented by a high+low surrogate pair are preserved exactly.
- Rule/token cardinality remains governed independently by `MepRecognitionLimits.MaxRules` and `MepRecognitionLimits.MaxTokensPerRule`; this contract does not weaken or reorder those limits.
- Case-insensitive duplicate-token semantics, priority ordering, ambiguity behavior, source flags, discipline/category semantics, and MEP-kind validation remain unchanged.

## Deterministic regression

`MepRecognitionSmoke.RecognitionTextIntegrity()` covers:

1. lone high surrogate in rule identity;
2. lone low surrogate in category;
3. broken surrogate pair in token text;
4. exact preservation of a valid supplementary-plane value across rule Id, Category, and token;
5. successful recognition through a valid supplementary-plane token.

The smoke remains registered through the existing `MepRecognitionSmoke.Run()` entry in the Core smoke registry.

## Source guard

`scripts/preflight-mep-recognition-text-integrity.py` is auto-discovered by aggregate preflight. It pins the Core surrogate-pair validation shape and the executable regression controls. The separate `preflight-mep-recognition-input-bounds.py` continues to own hostile-enumerable/cardinality semantics from issue #4400.

## Validation and acceptance

A source candidate is repository-qualified only when current exact-head Shared CI passes both protected `preflight` and `core`, including deterministic smoke and the applicable V25 compile/build checks. No hosted/static result from this runbook may be promoted to `LOCAL_PASS`, private-DWG evidence, or licensed BricsCAD runtime evidence.
