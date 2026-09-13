# Solibri QA Gate 2.0 — IFC Pset evidence validation

## Purpose

Strict QA2 requires `Pset_Qto` and `Pset_Identity` before quantity workflows may proceed. Previously any non-blank `IfcPset.<name>` value counted as valid evidence, including explicit negative placeholders emitted by import adapters.

## Validation contract

`QA2.MISSING_PSET` now also covers unusable Pset evidence. The following trimmed, case-insensitive values are treated as negative evidence: `0`, `false`, `missing`, `none`, `n/a`, `na`, `null`, `absent`, and `no`.

Positive compatibility markers such as `present`, `true`, `1`, adapter object references, and serialized non-empty payloads remain accepted. This keeps existing integrations working while preventing explicit negative evidence from satisfying the strict profile.

## Gate and waiver behavior

The rule ID remains `QA2.MISSING_PSET` and the Solibri strict profile keeps its configurable `Error` severity. At the default `Error` blocking threshold an unusable required Pset blocks Takeoff, BOQ, and Estimate through the existing hard gate. Because this is a completeness defect rather than a structural identity conflict, the existing audited exception/waiver mechanism remains available.

## Migration guidance

IFC adapters should omit a missing Pset or emit a canonical negative marker rather than inventing a positive-looking placeholder. Adapters that already emit `present`, `true`, `1`, an object reference, or a serialized payload require no migration.

Do not use values such as `false`, `missing`, or `n/a` to mean “Pset key was inspected”; QA2 interprets them as evidence that the required property set is not usable.

## Validation

Run `python scripts/preflight-qa2-pset-evidence.py` and the registered Core smoke suite. Protected Shared Branch/Integration CI remains the merge authority; licensed BricsCAD runtime behavior is outside this host-neutral QA2 change.
