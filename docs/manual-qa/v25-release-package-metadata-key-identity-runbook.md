# V25 package metadata key-identity qualification

Scope: REMOTE_SAFE validation of `PACKAGE-METADATA.json` admission for the V25 release package. This lane does not sign, publish, dispatch a release, or claim licensed BricsCAD runtime PASS.

## Contract

Before `ConvertFrom-Json` materializes metadata, `scripts/assert-v25-release-package-identity.ps1` must reject any metadata whose root is not exactly one JSON object or whose top-level property names collide under ordinal-ignore-case identity after JSON string escape decoding.

Examples that must be rejected include duplicate `productVersion` keys, case variants such as `productVersion` plus `PRODUCTVERSION`, and escaped aliases such as `productVersion` plus `product\u0056ersion`.

The existing strict UTF-8, bounded metadata size, held-generation binding, product/target identity, exact source SHA, ProductVersion, AssemblyVersion, and held plugin/Core assembly identity checks remain authoritative after unique-key admission.

## Remote qualification

Run the auto-discovered feature preflight and the protected preflight/core workflow on the exact PR head. Treat any mutation-control or aggregate source-guard failure as RED. Refresh the carrier non-force onto current protected `main`, rerun exact-head checks, and merge only with expected-head protection after all required checks are terminal GREEN.

No signing credentials, timestamping, release tag creation, package publication, installer execution, or licensed BricsCAD interaction is part of this runbook.
