# QuantBIM standalone V2 evidence replay

Issue #7166 closes the import-side quantity trust gap after V2 generation admission.

`QuantBimSelectionInterchangeV2Admission` remains responsible for authenticated transport plus exact open-IFC path, revision and GUID membership. `QuantBimSelectionInterchangeV2EvidenceReplay` then republishes the admitted selection through the existing `QuantBimSelectionExportPublisher`, which in turn uses the canonical standalone IFC QTO and traceable-evidence authorities.

Imported BOQ/evidence text is never quantity authority merely because its V2 envelope is internally valid. The replay boundary requires byte-for-byte canonical BOQ CSV and evidence CSV agreement with a fresh publication from the exact open IFC generation, and also verifies canonical selected identities. A stale, incomplete or semantically tampered package therefore fails closed even when an attacker or external producer recomputes all transport hashes correctly.

The boundary deliberately does not parse IFC, calculate a second set of quantities, create a second BOQ model, or depend on scene meshes. It reuses the existing generation admission and publication/QTO/evidence pipeline. The implementation remains in `QS3D.Core` with no BricsCAD, AutoCAD, WPF or WinForms dependency, preserving standalone Windows workbench architecture.

Validation consists of the module-initialized smoke `QsQuantBimSelectionInterchangeV2EvidenceReplaySmoke` and `scripts/preflight-quantbim-selection-interchange-v2-evidence-replay.py`. The smoke proves valid canonical replay and rejection of an internally valid V2 package whose BOQ bytes were semantically altered.
