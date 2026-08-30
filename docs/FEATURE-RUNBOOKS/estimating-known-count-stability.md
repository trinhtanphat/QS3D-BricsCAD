# Estimating known-Count stability

Lane-Key: `issue-4786`

Runtime: `NOT_APPLICABLE` — this is deterministic Core/commercial input-integrity behavior.

`EstimatingPortfolio` and `BulkRateAssignmentRequest` accept caller-controlled enumerables. When an input exposes a supported known Count surface, the admitted Count is a traversal-wide integrity contract rather than a one-time allocation hint.

For counted inputs, production must rebind all supported Count surfaces **before every MoveNext**, again **after every successful MoveNext**, and reject any instability **before IEnumerator.Current** is observed. This includes transient Count drift, negative Count, conflicting Count surfaces, source-set/value drift, known-count overrun, under-yield, and existing hard-cap violations. A hostile collection must not be able to change Count temporarily and restore it before the final post-traversal check.

The contract applies independently to portfolio lines, bulk selected-line IDs, and bulk unit-rate assignments. Stable counted inputs retain deterministic ordering and duplicate/provenance validation. Pure streaming inputs remain supported and continue to use the independent 10,000 portfolio/selected-line and 256 unit-rate hard caps.

Deterministic smoke uses multi-interface hostile collections whose Count changes after the first `Current`. The affected traversal must fail before the second `MoveNext`, proving metadata instability is detected while it is observable rather than after it has been restored.
