# Test strategy

## Tier 1 — pure deterministic tests

No BricsCAD process:

- geometry
- unit conversion
- formula parser/evaluator
- rebar notation
- rebar weight
- aggregation

These should be fast enough for every PR after CI is enabled.

## Tier 2 — adapter compile contract

Licensed V25 Windows runner:

- BrxMgd reference
- TD_Mgd reference
- WPF XAML compile
- V25 namespace/API compatibility
- x64 output

## Tier 3 — BricsCAD process smoke tests

Automate where stable:

- launch V25
- load plugin
- run `QS3D`
- run `QS3DINSPECT`
- verify palette creation
- open controlled DWG
- select known entity
- verify handle/type/layer/length
- unload/exit cleanly

## Tier 4 — DWG regression corpus

Real project drawings with expected results:

- 2D architectural
- structural
- Xref-heavy
- old DWG
- unusual layers
- Unicode/SHX text
- blocks/dynamic blocks
- hatch/region
- 3D solids

Every expected quantity stores:
- source drawing
- entity handle/fingerprint
- expected result
- tolerance
- unit
- reason

## Release rule

Do not ship a quantity feature because it “looks right”.
Every deterministic calculator needs a regression vector.
