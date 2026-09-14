# QuantBIM standalone V2 generation admission

Issue #7117 adds the import-side generation boundary for standalone QuantBIM selection interchange.

`QuantBimSelectionInterchangeV2Codec` remains the only V2 transport authority. It authenticates, bounds and canonicalizes the wrapped V1 package. `QuantBimSelectionInterchangeV2Admission` then binds that decoded package to the exact `QuantBimStandaloneIfcSession` already open in the workbench.

Admission fails closed when the document path differs, the IFC revision differs, a selection GUID is absent, or a GUID resolves more than once in the bound document. Successful admission canonicalizes GUID spelling from the bound IFC generation and returns an `IfcSelectionSet` for the existing filter/QTO/BOQ/evidence workflows.

This boundary deliberately does not parse IFC, calculate quantities, rebuild BOQ data, or create a second interchange model. Scene meshes remain visualization-only and do not become quantity authority. The implementation is in `QS3D.Core` and has no BricsCAD, AutoCAD, WPF or WinForms dependency.

Validation consists of the module-initialized smoke `QsQuantBimSelectionInterchangeV2AdmissionSmoke` plus `scripts/preflight-quantbim-selection-interchange-v2-admission.py`. The smoke covers valid exact-generation admission, stale revision refusal, foreign path refusal, unknown GUID refusal, and the parser-level duplicate IFC GlobalId invariant.
