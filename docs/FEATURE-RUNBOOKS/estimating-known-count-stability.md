# Estimating known-Count stability

Lane-Key: `issue-4886`

Runtime: `NOT_APPLICABLE` — this is deterministic Core/commercial input-integrity behavior.

`EstimatingPortfolio` and `BulkRateAssignmentRequest` accept caller-controlled enumerables. When an input exposes a supported known Count surface, the admitted Count is a traversal-wide integrity contract rather than a one-time allocation hint.

For counted inputs, production must rebind all supported Count surfaces **before every MoveNext**, again **after every successful MoveNext**, and reject any instability **before IEnumerator.Current** is observed. After an admitted item is read, production must rebind the same Count contract **immediately after every successful Current** and **before semantic acceptance** such as null/token validation, duplicate-state mutation, or snapshot accumulation. This includes transient Count drift, negative Count, conflicting Count surfaces, source-set/value drift, known-count overrun, under-yield, and existing hard-cap violations. A hostile collection must not be able to change Count from `Current`, begin accepting that item, and restore metadata before the next traversal-edge check.

The contract applies independently to portfolio lines, bulk selected-line IDs, and bulk unit-rate assignments. Stable counted inputs retain deterministic ordering and duplicate/provenance validation. Pure streaming inputs remain supported and continue to use the independent 10,000 portfolio/selected-line and 256 unit-rate hard caps.

`EstimatingKnownCountStabilitySmoke` preserves historical multi-interface hostile collections whose Count changes after the first `Current` and proves the affected traversal fails before the second `MoveNext`.

`EstimatingCurrentCountAcceptanceSmoke` narrows the successful-`Current` boundary: a hostile counted source returns an otherwise-invalid item while exposing one Count-drift observation. Portfolio, selected-line, and unit-rate materializers must report the Count-integrity failure before ordinary null/token acceptance. Stable counted controls remain accepted.
