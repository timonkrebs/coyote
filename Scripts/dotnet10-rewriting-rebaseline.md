# .NET 10 upgrade — rewriting re-baseline (Phase 5)

This is the follow-up work for the .NET 10 upgrade that **must run on a machine with the
.NET 10 SDK installed** (this could not be done in the environment where the rest of the
upgrade was prepared, because no SDK was available there).

The build/CI/source changes for net10.0 are already committed. What remains is to
**regenerate and manually review the IL rewriting diff-log baseline**, because Coyote is an
IL rewriter and its correctness on a new runtime is only established by inspecting what it
actually rewrites — not by "the build is green".

## Why this is required

`Tests/compare-rewriting-diff-logs.ps1` compares the rewritten IL against **hardcoded SHA256
golden hashes** and is pinned to `net8.0`. Two things change those hashes:

1. Building with the **.NET 10 SDK's newer Roslyn** can emit different IL even for the
   existing `net8.0` target, so the current `net8.0` hashes may no longer match.
2. The new `net10.0` target produces its own, different `*.diff.json` files.

So the diff-log gate cannot merely be re-run — it must be re-baselined, and the new baseline
must be **reviewed by a human** before being trusted. That review is the single most important
validation step of the whole upgrade: it is the only place where "an `await` silently stopped
being intercepted" becomes visible.

## Prerequisites

- .NET 10 SDK `10.0.301` or newer (matches `global.json`).
- .NET 8 runtime installed as well (the test matrix still builds and runs `net8.0`).
- PowerShell 7+ (`pwsh`).

## Steps

1. **Build everything with the .NET 10 SDK.**
   ```pwsh
   ./Scripts/build.ps1 -ci
   ```
   Confirm `bin/net10.0/` and `bin/net8.0/` are both produced (and `bin/net462/` on Windows).

2. **Verify the rewritten IL is valid (ilverify).** `Scripts/run-tests.ps1` already installs
   `dotnet-ilverify 10.0.0` and points at the `net10.0` bins/reference assemblies. Run the
   suite once and confirm the ilverify step reports no corrupted rewriting:
   ```pwsh
   ./Scripts/run-tests.ps1 -ci
   ```
   If ilverify reports "unresolved token" errors, the reference paths in `run-tests.ps1`
   (`FindDotNetRuntimePath` / `$runtime_version`) are not resolving the net10 runtime packs —
   fix those before trusting any diff.

3. **Regenerate the diff logs for net10.0.** Point the two diff-log scripts at `net10.0`:
   - `Tests/get-rewriting-diff-logs.ps1` — change `$framework = "net8.0"` to `"net10.0"`.
   - `Tests/compare-rewriting-diff-logs.ps1` — change `$framework = "net8.0"` to `"net10.0"`.

   Then collect the freshly produced diff files:
   ```pwsh
   ./Tests/get-rewriting-diff-logs.ps1
   ```

4. **MANUAL REVIEW (do not skip).** Inspect each collected
   `Microsoft.Coyote.<project>.diff.json` and confirm:
   - `Task`, `ValueTask`, their awaiters, and the async builders
     (`AsyncTaskMethodBuilder`, `AsyncValueTaskMethodBuilder`) are still being rewritten.
   - No BCL concurrency call site (`Task`, `Monitor`, `Interlocked`, `SemaphoreSlim`,
     `ConcurrentDictionary/Queue/Bag`, etc.) is left **un-rewritten** — a missing overload on
     a Coyote replacement type shows up here as a call that was not converted.
   - No spurious/incorrect rewrites were introduced.

   For any un-rewritten concurrency call, add the missing overload to the corresponding
   replacement type under `Source/Test/Rewriting/Types/**`, rebuild, and re-collect.

5. **Update the golden hashes.** Once the diffs are confirmed correct, compute the new
   SHA256 hashes and paste them into `$expected_hashes` in
   `Tests/compare-rewriting-diff-logs.ps1`:
   ```pwsh
   $framework = "net10.0"
   $map = [ordered]@{
       "rewriting"         = "Tests.Rewriting"
       "rewriting-helpers" = "Tests.Rewriting"
       "testing"           = "Tests.BugFinding"
       "actors"            = "Tests.Actors.BugFinding"
       "actors-testing"    = "Tests.Actors.BugFinding"
   }
   # Uses the same project-name resolution as compare-rewriting-diff-logs.ps1.
   foreach ($kvp in @(
       @{ key="rewriting";         proj="Tests.Rewriting";          name="Tests.Rewriting" },
       @{ key="rewriting-helpers"; proj="Tests.Rewriting";          name="Tests.Rewriting.Helpers" },
       @{ key="testing";           proj="Tests.BugFinding";         name="Tests.BugFinding" },
       @{ key="actors";            proj="Tests.Actors.BugFinding";  name="Tests.Actors" },
       @{ key="actors-testing";    proj="Tests.Actors.BugFinding";  name="Tests.Actors.BugFinding" })) {
       $path = "$PSScriptRoot/../Tests/$($kvp.proj)/bin/$framework/Microsoft.Coyote.$($kvp.name).diff.json"
       "{0,-18} {1}" -f $kvp.key, (Get-FileHash $path).Hash
   }
   ```
   (Adjust `$PSScriptRoot` if you run this outside `Tests/`.)

6. **Re-run the gate and the full suite** to confirm green:
   ```pwsh
   ./Tests/compare-rewriting-diff-logs.ps1
   ./Scripts/run-tests.ps1 -ci
   ```

7. **Re-validate `HttpClient.Control()`** — `Source/Test/Rewriting/Types/Net/Http/HttpClient.cs`
   reflects on the **private** `HttpMessageInvoker` fields `_disposed`, `_handler`,
   `_disposeHandler` by literal name. Confirm those fields still exist on the .NET 10
   `HttpMessageInvoker` (a rename would make `Control()` silently return an un-instrumented
   client). Add a null-guard/fallback if they changed.

## Open decisions for the reviewer

- **Keep validating `net8.0` too?** The diff-log gate currently validates one framework at a
  time. Consider extending both diff-log scripts to iterate `net10.0` *and* `net8.0` (with a
  hash set per framework) so both shipped targets stay covered, rather than only net10.
- **Runtime-async watch-item.** .NET 10 keeps the classic async state-machine lowering by
  default, so interception is intact. "Runtime async" (which bypasses `AsyncTaskMethodBuilder`
  entirely) is opt-in on net10 and becomes default in net11 — that flip will be a hard blocker
  requiring new interception logic, not a config tweak. Track it before adopting net11.
- **Dependency hygiene (optional Phase 3).** `Microsoft.NET.Test.Sdk` (17.4.0 → 18.x),
  `xunit` (→ 2.9.3), `xunit.runner.visualstudio` (→ 3.1.5), and the net10-aligned
  `10.0.x` servicing patches of DependencyModel / ConfigurationManager / System.Text.Json.
