# LOCAL-022 private diagnostics

These are hardened source successors to the three allocation-local diagnostic
prototypes. They are not acceptance tests and never issue an action ACK or PASS.
The original prototypes and raw evidence remain ignored under `artifacts/`.

- `read-owned-waitchain.ps1`: read-only WCT sample of at most eight busy threads.
  No cycle in that sample is not proof that every thread is deadlock-free.
- `capture-owned-native-dump.ps1`: opt-in private memory/stack dump. Requires
  `-ConfirmPrivateMemoryDump` after the operator obtains explicit consent. It
  can briefly pause the host. It does not enable debug privileges, inject input,
  attach a debugger, terminate CAD or change product state.
- `read-native-dump.ps1`: opens only an allocation-local offline dump with its
  exact supplied SHA-256, writing a new private log. Thread indices are bounded
  integers, default0; there is no arbitrary command or live-attach parameter.

Use Windows x64 PowerShell7 in a fresh process. Live helpers currently support
only the initial `ui.scr` phase, not native API or cold reopen. Before use, bind
the direct allocation directory, its SHA-256, RunId, sole owned PID/parent,
seven-digit UTC creation timestamp from CIM and exact executable from read-only
process inspection. Allocation and recovery hashes bind the nonce profile and
derived drawing/startup-script paths, not startup-script bytes. Host bytes, full
arguments and process creation time are checked. The dump call also checks creation
time on its opened process handle at CIM's microsecond precision, and loads its
Windows dump library only from System32. Consumed
allocations, wrong identities, redirected paths and existing outputs fail closed.

`OutputPath` must be a new direct allocation file ending `.private.dmp`.
Offline `DumpPath` and `LogPath` must be direct files ending `.private.dmp` and
`.private.txt`. Supply local existing `DebuggerDirectory` and `SymbolDirectory`.
The reader requires the exact six Microsoft-signed x64 DLL hashes in its source;
dependencies are not redistributed or downloaded. Different debugger versions
require a reviewed pin update. Symbols are local-only; inherited symbol-server
environment settings are temporarily cleared in the helper process and restored.
The COM reader retains the prototype's pinned IDebugClient/IDebugControl slots.
Paths passed to that legacy ANSI interface must be ASCII (enforced by the reader).

Never commit dumps, private logs, PDB caches, customer drawings, binaries, licence
data or credentials. Only reviewed sanitized diagnoses belong in the validation
note. Diagnostics do not change the frozen product or justify a retry by themselves.

Run `pwsh -NoProfile -File ./test-diagnostic-guards.ps1` for actual guard and
negative-entrypoint tests and compile-only checks of all three embedded C# units.
They do not call native WCT/dump/debugger APIs. This
successor's live capture/offline reader execution still needs an explicitly scoped
diagnostic session; host-free tests alone do not certify native functionality.

WCT samples use the SDK's `WCT_NETWORK_IO_FLAG` without out-of-process COM/CS
expansion. References: [Microsoft WCT API](https://learn.microsoft.com/en-us/windows/win32/api/wct/nf-wct-getthreadwaitchain),
[SDK wct.h](https://github.com/microsoft/win32metadata/blob/main/generation/WinSDK/RecompiledIdlHeaders/um/wct.h),
[DLL search policy](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.defaultdllimportsearchpathsattribute),
[local symbol paths](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/symbol-path).
