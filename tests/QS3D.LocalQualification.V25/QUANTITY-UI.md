# LOCAL-022 bounded BQ scenario

Run `run-local022-ui-qualification.ps1 -NativeApi -QuantityUi` with the existing
frozen package, pushed clean harness, fresh allocation and temporary-autostart
authorization. V25 precedes V26; V26 requires a cleaned, same-product V25 native
receipt **and both quantity markers**. A historical native or authoring UI PASS
cannot satisfy this new scenario.

The existing native commands prepare two isolated disposable footings, regenerate
their Family to 4/2/2/1/1/1 metres, verify native geometry, and save. `QL22QTY`
then queues the real production `QS3DBQ` command. It reads the actual bound WPF
window/DataGrid, never constructs a substitute report or invokes a button.

The analytic expectation is 38/3 m³ per footing, 76/3 m³ total: an 8 m³ box plus
a homothetic rectangular frustum `(8 + 2 + sqrt(8*2))/3` per element. Two unique
element IDs and their actual source footprints must match every displayed row.
Gross/net evidence is required and deduction must be zero. Do not replace this
expectation with the current product's output if it differs.

External physical operation, after observing the real window:

1. Read summary: one row, count 2, displayed concrete total 25.333 m³ (localized).
2. Click **Tính lại**; fresh row objects and unchanged values are required.
3. Click **Diễn giải chi tiết**; two rows, count 1 and 12.667 m³ each.
4. Click **Khối lượng** to return to the grouped summary.
5. Select its row and click **Định vị**. Actual implied native selection must be
   the two source footprint handles. Default Bám3D also locates on row selection.
6. Close the BQ window. The observer verifies model/project/disk nonmutation and
   exits only its owned disposable session.
7. The runner opens a fresh process and checks the persisted native fixture plus
   the real BQ summary again. Observe it and close BQ to complete the cold cell.

`quantity.private.txt` / `quantityreopen.private.txt` record observed product
events and actual aggregate values, not synthetic physical-input ACKs. Use the
supported computer-use tool for gestures and visual verification. The observer
contains no button invocation, row selection or mode setter. No PowerShell UI
driver, proxy-dialog automation or capture is used in this mode.

The 55-minute in-process bound fits within the outer 60-minute process timeout;
it is a failure ceiling, not an intended run duration. Any wrong value fails
immediately, preserves the failure marker and exits. A failed earlier native
phase, early window close, missing quantity marker or failed cleanup prevents
the final receipt from passing. Earlier accepted authoring allocations remain
unchanged and need not be replayed for this reporting cell.

This is bounded footing BQ evidence only, not Excel/BBS/all reporting, HiDPI,
private-DWG, published-release or MCP qualification.
