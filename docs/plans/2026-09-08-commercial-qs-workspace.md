# Commercial QS workspace

Issue: #6101

## Goal

Expose the merged commercial QS domain through one BricsCAD modeless product surface for Variation, IPC and Final Account, with deterministic CSV export and without duplicating commercial settlement arithmetic in the V25 adapter.

## Product surface

- `QS3DCOMMERCIAL` opens one document-bound modeless window per active native database.
- **Variation** accepts a review register and constructs `CommercialVariation` / `CommercialVariationRegister` in Core. The selected variation remains visible above all tabs while IPC or Final Account is prepared.
- **IPC** converts progress rows through `ProgressClaimService.Evaluate(...)`, then delegates certificate settlement to `InterimPaymentCertificateService.Create(...)`.
- **Final Account** delegates reconciliation to `FinalAccountService.Reconcile(...)`.
- **Export CSV** serializes only validated Core inputs/results held by the window; it does not recalculate gross, net, retention, amount due or recovery due.

## Source guard

`scripts/preflight-commercial-qs-workspace.py` requires the command, modeless lifecycle, all four product actions, Core authority calls, document-bound lifetime and export path. It also rejects adapter-side assignments to key settlement result fields.

The guard was added before the production surface so the initial commit was deliberately RED while the three production files were absent; the implementation then satisfies the same guard.

## Input formats

Variation row:

`id | description | proposed | approved | status | revision`

IPC progress row:

`item id | unit | contract qty | unit rate | previous qty | this-period qty`

IPC variation certification row:

`variation id | previous certified | certified this period`

Numbers are parsed using invariant decimal formatting. Currency is canonicalized to uppercase before Core validation.

## Verification boundary

Hosted CI can prove source integration, source guards, Core smoke tests and V25 compile references. It cannot prove interactive licensed BricsCAD V25/V26 runtime behavior. Licensed runtime qualification remains fail-closed / `PENDING_NATIVE` unless a corresponding licensed host produces evidence; this workspace does not weaken that boundary.
