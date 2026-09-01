# Commercial audit known-Count stability

Lane-Key: `issue-4789`

Runtime: `NOT_APPLICABLE` — deterministic Core/commercial correctness only.

`CommercialAuditLog.AppendBatch` and `CommercialGuard.Snapshot` consume caller-controlled enumerables. When an input exposes a supported known Count surface, that Count is a traversal-wide integrity contract rather than only an allocation hint.

For counted inputs, production must rebind all supported Count surfaces **before every MoveNext**, again **after every successful MoveNext**, and reject instability **before Current** is observed. This includes transient Count drift, negative Count, conflicting Count surfaces, source-set/value drift, known-count overrun, under-yield, and existing hard-cap violations. Drift that appears after one `Current` and restores inside the next `MoveNext` must still fail closed before that next move is attempted.

The contract applies independently to audit batch records and source-revision snapshots stored in `CommercialAuditRecord`. Existing duplicate event-id checks, null rejection, append atomicity, source-revision order, hard caps, and streaming inputs remain unchanged.

Hosted deterministic smoke/source guards are authoritative for this Core-only package. No licensed BricsCAD runtime or private DWG evidence is required or claimed.
