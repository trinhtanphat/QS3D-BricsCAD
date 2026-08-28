# CAD identification color-rule known Count early-drift contract

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4341`

Ownership-Key: `core.recognition.color-rule-known-count-overrun`

## Problem

`CadIdentificationOptions` accepts color rules from arbitrary enumerable sources and observes supported collection Count evidence before enumeration. Before this fix, an over-yielding source could advertise `Count = N`, yield `N+1` rules, and force validation/dictionary work on the unexpected rule before the constructor rejected the completed Count mismatch.

The trusted Count boundary must win before rule semantics: the first yielded item outside the advertised cardinality is not part of the accepted input shape.

## Contract

- Negative, conflicting, and `>256` supported Count evidence rejects before enumeration.
- For a valid known Count, the first rule at index `knownCount` rejects before the 256-rule cap, null validation, duplicate color-index validation, or dictionary insertion.
- Under-yield remains rejected after traversal.
- Pure streaming input without known Count evidence retains the independent 256-rule cap.
- Honest counted/streaming input retains classification-by-color behavior and existing validation.

## Deterministic evidence

`CadIdentificationColorRuleCountTraversalSmoke` covers under-yield, early over-yield, a `null` unexpected rule proving precedence, exact-count and streaming controls, pre-enumeration Count defects, and ordinary null/duplicate semantics.

`scripts/preflight-cad-identification-color-rule-known-count-early-drift.py` locks the guard ordering and regression controls.

## Runtime boundary

This is Core Recognition data integrity. No licensed BricsCAD host, private DWG, signing, packaging, or `LOCAL_PASS` evidence is required or claimed.
