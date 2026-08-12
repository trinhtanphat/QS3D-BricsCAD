# Rebar Notation Finite Bounds Plan

## Goal

Make `RebarNotationParser.Parse(...)` fail closed before unbounded compound-token allocation or regex work while preserving the current notation grammar and numeric semantics.

## Implementation

1. Add parser-local capacities of 4096 UTF-16 characters and 128 compound groups; do not redefine general project property limits.
2. Reject oversized notation before `Split('+')` so the parser never allocates an unbounded token array first.
3. After splitting the bounded string, reject more than 128 groups before regex parsing.
4. Preserve empty-segment rejection, whitespace behavior, finite-positive diameter/spacing parsing, checked set×quantity multiplication and existing error types for numeric overflow.
5. Add isolated module-initializer smoke coverage for exact length/group boundaries, boundary+1 rejection and ordinary spacing/count parsing.

## Safety

No CAD/native BricsCAD changes, no persistence schema change, no project-wide property policy change, no Actions dispatch and no runtime qualification claim. Re-fetch moving `main` before each write and preserve concurrent history.
