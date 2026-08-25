Status: ACTIVE
Lane-Key: issue-bcf-standard-package-vocabulary
Issue: #3919
Owner: Worker 4 / API & Interop
Baseline: main@5b1e8fd85f5aac50ae47d8684c01b82e274783e8
Canonical branch: agent/api-interop/bcf-standard-package-vocabulary
Estimated scope: 3–5 engineer-hours

Reserved scope:
- src/QS3D.Core/Export/BcfZipPackage.cs
- focused BCF ZIP deterministic smoke coverage under tests/QS3D.Core.SmokeTests/
- directly coupled BCF package interoperability documentation/guards only if required

Objective:
Remove the proprietary extensions.xml requirement from the QS3D BCF 3.0 package subset while preserving strict legacy-package validation when extensions.xml is present. Standard packages without that root file must be able to use canonical TopicType/TopicStatus tokens; QS3D writes must stop emitting the proprietary root vocabulary file. Preserve fail-closed behavior for unsupported package entries and do not broaden this carrier to project.bcfp, snapshots, other BCF schema features, IFC, UI, updater, reporting, persistence or licensed runtime qualification.

Validation / integration:
One canonical PR; deterministic Core smoke; automatic exact-head CI; reconcile latest main non-force if required; protected preflight + core and clean reviews before expected-head merge. Never claim licensed BricsCAD runtime PASS from remote evidence.
