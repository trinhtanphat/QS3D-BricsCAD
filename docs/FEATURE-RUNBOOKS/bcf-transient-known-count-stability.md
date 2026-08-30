# BCF transient known-Count stability

## Contract

`BcfIssueExchangeContract.MaterializeBounded<T>` treats supported collection Count surfaces as admission evidence. Once admitted, those surfaces must remain stable for the entire traversal and must be rebound before semantic `Current` is read.

For counted inputs the traversal order is:

1. rebind the admitted Count contract before `MoveNext()`;
2. call `MoveNext()`;
3. after a successful move, rebind Count again before overrun/cap admission and before `Current`;
4. materialize the item only after those checks;
5. retain under-yield and final Count stability validation after traversal.

Transient growth, shrink, negative Count or cross-interface conflict must fail closed before a BCF topic, viewpoint, comment or component is semantically consumed. Pure streaming inputs retain bounded traversal without a synthetic Count requirement.

## Deterministic regression

`BcfTransientKnownCountSmoke` uses an adversarial collection implementing generic, read-only and non-generic Count surfaces. The enumerator makes Count transient immediately after the first successful `MoveNext()` and restores it only when `Current` is read. Growth, shrink, negative and conflicting Count modes must therefore reject with exactly one `MoveNext` call and zero `Current` reads. Stable counted and pure-streaming controls remain accepted.

## Acceptance boundary

This is deterministic Core/BCF data-integrity work. No licensed BricsCAD or private-DWG runtime acceptance is required or claimed.
