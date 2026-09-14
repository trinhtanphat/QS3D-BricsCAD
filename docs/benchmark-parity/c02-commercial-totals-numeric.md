# C02 commercial totals numeric contract

This carrier hardens host-neutral construction/commercial totals.

Acceptance:
- reject NaN/Infinity commitment progress before storage;
- reject non-finite/out-of-range weighted-progress publication;
- preserve exact decimal commercial weights until the final published double ratio;
- make weighted progress invariant to commitment enumeration order;
- keep monetary total/forecast overflow fail-closed;
- preserve empty/zero-total semantics.
