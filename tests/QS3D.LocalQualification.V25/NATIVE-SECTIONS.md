# LOCAL-022 native cross-section qualification

Native marker V2 extends the existing native API matrix without changing the
physical UI driver, frozen product or installed package. V1 markers remain valid
only for their historical bounded evidence; they cannot qualify V2 assertions.

For every initially placed box/tapered solid, both regenerated solids, both saved
solids and both freshly reopened solids, the separate probe reads the actual
native `Solid3d` in an open/close transaction. It requests horizontal sections at
25%,50%,75% of H1 and, when H2 is positive,25%,50%,75% of H2. Expected lower-stage
length/width are L1/W1; expected upper-stage dimensions linearly interpolate to
L2/W2 independently of product volume or generated bounds.

Every returned region must have the expected area, perimeter, WCS bounds and
four unique straight boundary edges connecting the expected corners. Checks
reject null/nonfinite/wrong/duplicate/missing responses. Edge traversal direction
and order are irrelevant. Planes, regions, exploded entities and collections are
disposed, including failure paths; nothing is appended to the drawing database.

The sampled quarter-planes avoid coincident solid boundary faces. This is finite
cross-section coverage, **not complete BREP topology** or proof of every possible
height. UI, Quantity/Unicode/DPI/private-DWG and published-source gates remain
separate. Host-free doubles exercise the actual adapter but are not native PASS.

References: [Bricsys Solid3d.GetSection](https://developer.bricsys.com/bricscad/help/en_US/V24/DevRef/source/html/29d68a74-b331-1174-ef75-58060801e142.htm)
describes native plane/solid intersection; the installed V25/V26 SDK builds
validate the APIs used by this standalone probe.

Use the existing wrapper's explicit `-NativeApi` on a committed/pushed exact
harness and a fresh disposable allocation. V25 must pass all V2 run/save/cold
checks and cleanup before V26; the wrapper reads actual phase markers and rejects
older V1 receipts as section proof. Preserve earlier consumed allocations.
