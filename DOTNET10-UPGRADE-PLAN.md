# Plan: Upgrade Coyote to .NET 10

This document is the implementation plan for moving Coyote's primary target
framework from .NET 8 to .NET 10 (LTS, released November 2025, supported until
November 2028), following the same playbook used for the .NET 6 → .NET 8
upgrade in v1.7.11 (#507).

## 1. Goals

- Make `net10.0` the primary target framework across libraries, tools, tests,
  and samples.
- Keep `net8.0` as an opt-in secondary target (LTS until November 2026), taking
  over the role `net6.0` plays today.
- Drop `net6.0` (end of life since November 2024).
- Keep `netstandard2.0` and `net462` (Windows-only, opt-in) unchanged, so
  .NET Framework consumers and rewriting of `net462` assemblies keep working.
- Modernize the CI workflows enough to actually run (several actions in use
  have been hard-deprecated and no longer execute — see Phase 0).

Proposed package version after the upgrade: **1.8.0** (minor bump: adds a
target framework, drops an EOL one; `History.md` vNext already carries the
`netcoreapp3.1` drop).

## 2. Current state (what is wired where)

The framework matrix is centralized in `Common/build.props`:

- `TargetFrameworks` starts at `net8.0`, appends `netstandard2.0` (opt-out via
  `NetStandard2Supported=false`), appends `net6.0` when `BUILD_NET6=yes` or the
  pinned SDK is 6.x (`Net6Installed`/`Net6Supported`), and appends `net462` on
  Windows when `BUILD_NET462=yes` (`Framework462Installed`/`Framework462Supported`).
- `LangVersion` is `10.0` for `net8.0`/`net6.0` and `8.0` for everything else.
- `global.json` pins SDK `8.0.404`; `Scripts/build.ps1` and
  `Scripts/common.psm1` resolve the SDK/runtimes from it and pass
  `BUILD_NET6`/`BUILD_NET462` in CI runs.

Framework names are additionally hardcoded in: PowerShell scripts
(`Scripts/run-tests.ps1`, `Scripts/gen-docs.ps1`, `Scripts/run-benchmark*.ps1`,
`Scripts/common.psm1`, `Samples/Scripts/run-tests.ps1`,
`Tests/compare-rewriting-diff-logs.ps1`, `Tests/get-rewriting-diff-logs.ps1`),
CI definitions (`.github/workflows/*.yml`, `Scripts/CI/azure-nuget-sign-publish.yml`),
TFM-conditional `ItemGroup`s in `Source/Test/Test.csproj`,
`Tools/Coyote/Coyote.csproj`, `Tools/CLI/Coyote.CLI.csproj`, one runtime code
path (`Source/Test/Rewriting/RewritingOptions.cs` maps
`.NETCoreApp,Version=vX.Y` → TFM string), sample configs
(`Samples/Common/build.props`, `rewrite.coyote.json` files,
`Samples/CloudMessaging/*.cmd`), and ~30 docs pages.

Source-level conditionals (`#if NET8_0_OR_GREATER`, `#if NET`,
`#if NETSTANDARD2_0 || NETFRAMEWORK`) are version-open and automatically apply
to `net10.0`; no `#if` changes are required. There is no `BinaryFormatter`
usage, so the .NET 9+ removal does not affect Coyote.

## 3. Target framework matrix

| Target | Today | After upgrade | Rationale |
|---|---|---|---|
| `net10.0` | — | **primary** | Current LTS (EOL Nov 2028). Note: the .NET 10 SDK defaults to C# 14, but this plan pins `LangVersion` to 10.0 across TFMs (see Phase 1); adopting newer language syntax is a follow-up. |
| `net8.0` | primary | opt-in secondary (CI: `BUILD_NET8=yes`) | Still LTS until Nov 2026; gives consumers a migration window. Remove after Nov 2026. |
| `net6.0` | opt-in secondary | **dropped** | EOL Nov 2024. |
| `netstandard2.0` | yes | unchanged | Library compat + rewriting support for .NET Framework. |
| `net462` | opt-in, Windows CI | unchanged | Still a supported .NET Framework baseline. |

An alternative considered: target `net10.0` only (plus `netstandard2.0`/`net462`).
Rejected for now because the `Microsoft.Coyote.Tool`/`coyote` test+rewrite tool
runs *in-process* with the runtime of the assembly under test — keeping a
`net8.0` build of the tool lets users keep testing `net8.0` apps unrewritten
for .NET 10 until they migrate.

## 4. Phased execution

Ship as three PRs so failures are attributable:

| PR | Phases | Content |
|---|---|---|
| PR 1 | Phase 0 | CI unblock (deprecated actions). Must land first — CI is currently unable to run at all (see below). |
| PR 2 | Phases 1–4, plus the `History.md` and signing-pipeline items of Phase 5 | The upgrade itself: build infrastructure, source, tests/tooling, samples. |
| PR 3 | Remainder of Phase 5 | Docs sweep (CI-neutral; `paths-ignore` skips it). The `version.props` bump happens at release time, not in a PR. |

**Version placeholder convention.** Where this plan writes `10.0.x`,
`17.14.x`, `0.15.x`, etc., read "the latest matching release at implementation
time". The literal `x` form is valid syntax only in workflow SDK-setup inputs
(`actions/setup-dotnet` `dotnet-version`, Azure `UseDotNet`). NuGet version
fields — `PackageReference`, `dotnet tool install --version`,
`.config/dotnet-tools.json` — do not treat `x` as a wildcard: use an exact
version there (NuGet floating syntax like `10.0.*` exists for
`PackageReference`, but this repo's convention is exact pins, and tool
manifests accept only exact versions).

### Phase 0 — unblock and modernize CI (independent of .NET 10)

The workflows use actions that GitHub has disabled or deprecated:

- `actions/upload-artifact@v3` / `download-artifact@v3` — **shut off January
  2025; the `test-coyote.yml` jobs fail on these steps today.** → `@v4`.
  Breaking change to accommodate: v4 artifacts are immutable and a given name
  can be uploaded only once per run, so the three `build-and-test` matrix legs
  can no longer all upload `name: coyote-binaries`. Suffix the platform into
  the name (`coyote-binaries-${{ matrix.platform }}`) and have each
  `build-and-test-samples` matrix leg download its matching platform's
  artifact. (This is also more correct than v3's behavior, which silently
  merged the three uploads last-write-wins — only the Windows leg carries
  `net462` outputs.)
- `github/codeql-action@v1` — shut off; → `@v3` (`codeql-analysis.yml`).
- `actions/checkout@v2` → `@v4`, `actions/setup-dotnet@v1` → `@v4`,
  `NuGet/setup-nuget@v1` → `@v2`.

Files: `.github/workflows/test-coyote.yml`, `test-performance.yml`,
`codeql-analysis.yml` (`publish-docs.yml` is Python/mkdocs; update
checkout only). Keep SDK versions at 8.0.x/6.0.x in this phase so the change
is pure CI plumbing and the suite proves itself green once before the TFM flip.

### Phase 1 — build infrastructure flip

1. **`global.json`** — pin the .NET 10 SDK baseline and let it roll forward
   across feature bands:
   ```json
   { "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }
   ```
   `latestFeature` — not `latestPatch`, which only accepts newer patches
   within the 10.0.1xx feature band — also matches machines that have only a
   newer band installed (10.0.2xx/10.0.4xx), avoiding the hard-pin friction
   the repo has had with exact versions (`common.psm1` already fuzzy-matches
   by major.minor).

2. **`Common/build.props`**
   - TFM matrix block (lines 39–53): `net8.0` → `net10.0` as the base of
     `TargetFrameworks`; rename `Net6Supported`/`Net6Installed`/`BUILD_NET6` →
     `Net8Supported`/`Net8Installed`/`BUILD_NET8`; the
     `GlobalVersion.StartsWith('6.0')` probe becomes `StartsWith('8.0')`:
     ```xml
     <Net8Supported Condition="'$(Net8Supported)'==''">true</Net8Supported>
     <Net8Installed>false</Net8Installed>
     <Net8Installed Condition="$(GlobalVersion.StartsWith('8.0'))">true</Net8Installed>
     <Net8Installed Condition="'$(BUILD_NET8)'=='yes'">true</Net8Installed>
     <TargetFrameworks>net10.0</TargetFrameworks>
     <TargetFrameworks Condition="'$(NetStandard2Supported)'">$(TargetFrameworks);netstandard2.0</TargetFrameworks>
     <TargetFrameworks Condition="'$(Net8Installed)' and '$(Net8Supported)'">$(TargetFrameworks);net8.0</TargetFrameworks>
     ```
   - LangVersion block (lines 19–24): condition on `net10.0 or net8.0` →
     `10.0`, else `8.0`. Keeping `LangVersion` at 10.0 (rather than jumping to
     C# 14) keeps the source identical across TFMs and the diff minimal; a
     LangVersion raise can be its own change later.

3. **Rename `Net6Supported=false` → `Net8Supported=false`** in the nine
   projects that set it: `Tests/Tests.Runtime`, `Tests/Tests.Performance`,
   `Tests/Tests.Actors.BugFinding`, `Tests/Tests.Actors.Performance`,
   `Tests/Tests.BugFinding`, `Tests/Tests.Tools`, `Tests/Tests.Actors`,
   `Tools/GenDoc`, `Tools/BenchmarkRunner` (i.e. tests/tools build primary TFM
   only, as today).

4. **TFM-conditional `ItemGroup`s**
   - `Source/Test/Test.csproj`: add a `net10.0` group with
     `Microsoft.Extensions.DependencyModel` 10.0.x; retarget the existing 8.0
     group as the secondary; delete the `net6.0` group; bump the
     `netstandard2.0`-only `System.Text.Json` 8.0.5 → 10.0.x (stays
     netstandard2.0-compatible, keeps it on a serviced train past .NET 8 EOL).
   - `Tools/Coyote/Coyote.csproj`: add a `net10.0` group (both
     `FrameworkReference`s + `System.Configuration.ConfigurationManager`
     10.0.x, `PrivateAssets=all`); keep the `net8.0` group (bump
     ConfigurationManager to 8.0.1); delete the `net6.0` group; `net462` group
     unchanged.
   - `Tools/CLI/Coyote.CLI.csproj`: same treatment (no `net462` group here).
     Consider adding `<RollForward>Major</RollForward>` so the `dotnet tool`
     works on machines with only a newer runtime.

5. **Build/test scripts**
   - `Scripts/build.ps1`: `$version_net6 = FindMatchingVersion ... "6.0.0"` →
     `$version_net8 ... "8.0.0"`; `/p:BUILD_NET6=yes` → `/p:BUILD_NET8=yes`.
   - `Scripts/common.psm1` (`Invoke-CoyoteTool`, line 26): the "sign with snk
     except on modern .NET" condition gains `net10.0`; simplest is to invert
     it: sign only when `$framework -eq "net462"` on Windows.
   - `Scripts/run-tests.ps1`: `ValidateSet` → `("net10.0", "net8.0", "net462")`
     with default `net10.0`; the ilverify block's four hardcoded `net8.0`
     paths → `net10.0`; `dotnet tool install dotnet-ilverify --version 8.0.0`
     → an exact 10.0.N version (NuGet `--version` has no `x` wildcard).
   - `.config/dotnet-tools.json`: `dotnet-ilverify` 8.0.0 → the same exact
     10.0.N pin (tool manifests require exact versions); optionally bump
     `dotnet-counters`/`dotnet-dump`.
   - `Scripts/gen-docs.ps1`, `Scripts/run-benchmarks.ps1`,
     `Scripts/run-benchmark-history.ps1`: `net8.0` bin paths → `net10.0`.

### Phase 2 — source changes

1. `Source/Test/Rewriting/RewritingOptions.cs`
   (`TryResolveTargetFramework`, lines ~309–314): add the v10 mapping, drop v6:
   ```csharp
   resolvedTargetFramework = tokens[1] is "v10.0" ? "net10.0" :
       tokens[1] is "v8.0" ? "net8.0" :
       resolvedTargetFramework;
   ```
   Also update the XML-doc JSON example (`./bin/net8.0` → `./bin/net10.0`).
2. No other code changes expected: `#if NET8_0_OR_GREATER` /
   `#if NETSTANDARD2_0 || NETFRAMEWORK` guards apply correctly to `net10.0`.
3. Dependency bumps in `Source/Test/Test.csproj` (beyond TFM groups), low risk,
   recommended: `Mono.Cecil` 0.11.4 → 0.11.6 (bug fixes; validated against
   net10 assemblies by the whole rewriting test suite). Optional:
   `Microsoft.ApplicationInsights` 2.20.0 → latest 2.x. Leave
   `Microsoft.AspNetCore.Http.Abstractions` 2.2.0 and `xunit.abstractions`
   2.0.3 alone (compat shims, unchanged pattern).

### Phase 3 — tests and tooling

1. **Test packages** (all `Tests/*/*.csproj`): `Microsoft.NET.Test.Sdk`
   17.4.0 → 17.14.x — required; the 17.4-era testhost predates net10 —
   and `xunit` 2.4.2 → 2.9.x / `xunit.runner.visualstudio` 2.4.5 → 2.8.x
   (needed for reliable net10 test discovery/execution).
2. **BenchmarkDotNet**: 0.12.1 (`Tests/Tests.Performance`,
   `Tests/Tests.Actors.Performance`) and 0.13.1 (`Tools/BenchmarkRunner`) →
   0.15.x; older versions don't know the net10 runtime moniker. The
   perf workflow runs on a **self-hosted runner — the .NET 10 SDK must be
   installed on it** as an ops prerequisite.
3. **Regenerate IL-diff baselines** (expected churn, Windows):
   - Update `$framework = "net8.0"` → `"net10.0"` in
     `Tests/compare-rewriting-diff-logs.ps1` and
     `Tests/get-rewriting-diff-logs.ps1`.
   - Build + rewrite (`./Scripts/build.ps1 -ci`), then recompute the five
     SHA256 hashes (`Get-FileHash` over
     `Tests/<project>/bin/net10.0/Microsoft.Coyote.<project>.diff.json`) and
     replace `$expected_hashes` in `compare-rewriting-diff-logs.ps1`.
     New hashes are expected — the net10 BCL changes the rewritten IL — but
     eyeball the diff logs for anything structurally surprising first
     (`get-rewriting-diff-logs.ps1` exists for exactly this).
4. **Workflows** (`test-coyote.yml`, `codeql-analysis.yml`,
   `test-performance.yml`): SDK setup steps 8.0.x/6.0.x → 10.0.x/8.0.x
   (setup-dotnet@v4 accepts a multi-line `dotnet-version`, so the two steps can
   merge into one).

### Phase 4 — samples

- `Samples/Common/build.props`: `TargetFrameworks` `net8.0` → `net10.0`.
- `Samples/Scripts/run-tests.ps1`: `$framework = "net8.0"` → `"net10.0"`.
- `rewrite.coyote.json` (3 files: `Samples/Common/TestDriver`,
  `Samples/WebApps/ImageGalleryAspNet`,
  `Samples/WebApps/PetImagesAspNet/PetImages.Tests`): `bin/net8.0` paths →
  `bin/net10.0`.
- `Samples/CloudMessaging/run*.cmd` (3 files): `net8.0` paths → `net10.0`.
- `Samples/WebApps/PetImagesAspNet/PetImages.Tests`:
  `Microsoft.AspNetCore.TestHost` / `Microsoft.AspNetCore.Mvc.Testing`
  8.0.2 → 10.0.x (must match the runtime major). Test SDK/xunit bumps as in
  Phase 3; `MSTest` 2.2.8 in `ImageGalleryAspNet/Tests.Coyote` → 3.x
  recommended.

### Phase 5 — docs, release metadata, publishing

- **Docs sweep** (separate PR; `paths-ignore` means it skips CI): global
  replace of `net8.0` → `net10.0` bin paths and ".NET 8.0" phrasing across
  `docs/**` (~30 files: tutorials, how-tos, samples, `get-started`,
  `concepts/binary-rewriting.md`); regenerate `.dgml`/`.svg` assets only if
  trivially scriptable, otherwise leave (cosmetic).
- `History.md` vNext: "Added support for the `net10.0` target framework",
  "Dropped support for the end-of-life `net6.0` target framework", plus the
  dependency bumps.
- `Common/version.props`: `1.7.11` → `1.8.0` at release time.
- `Scripts/CI/azure-nuget-sign-publish.yml`: `UseDotNet` 8.0.x/6.0.x →
  10.0.x/8.0.x; signing `FolderPath` `bin\net8.0`/`bin\net6.0` →
  `bin\net10.0`/`bin\net8.0` (verify every FolderPath entry in that file).
- `.config/dotnet-tools.json`: `microsoft.coyote.cli` pin follows the release.

## 5. Validation

1. **Spike first** (before polishing the PR): flip `build.props` +
   `global.json` locally, `./Scripts/build.ps1`, then
   `./Scripts/run-tests.ps1` on Linux or Windows. This surfaces the two real
   risk areas — rewriting net10 IL with Mono.Cecil and the in-process test
   runner on the net10 runtime — in under an hour.
2. Full local matrix on Windows (the only place `net462` + snk signing +
   NuGet packing run): `./Scripts/build.ps1 -ci -nuget`,
   `./Tests/compare-rewriting-diff-logs.ps1`, `./Scripts/run-tests.ps1 -ci -cli`.
3. Samples: `./Samples/Scripts/build.ps1 -local -nuget`, `build-tests.ps1`,
   `run-tests.ps1` (exercises rewriting of ASP.NET Core 10 apps end to end).
4. CI green on all three OSes; ilverify step passes on the rewritten net10
   assemblies (new verification rules in ilverify 10 may need triage).
5. Grep gate before merge: `grep -rn "net6\.0"` returns nothing outside
   `History.md`; `grep -rn "net8\.0"` returns only the intentional secondary-
   target wiring (`build.props`, conditional ItemGroups, workflows, signing yml).
6. Pack + install smoke test of `Microsoft.Coyote.CLI` from `bin/nuget`
   (already covered by `run-tests.ps1 -cli` on Windows).

## 6. Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Mono.Cecil 0.11.x mis-writes net10 assemblies | Rewriting corrupts binaries | ilverify gate already in `run-tests.ps1`; bump to 0.11.6; spike early. |
| net10 BCL internals shift under intercepted APIs (Task/Monitor/Interlocked semantics) | Test flakiness or missed interceptions | Full bug-finding suite is the regression net; IL-diff logs reviewed by hand once. |
| `System.Threading.Lock` (new since .NET 9): C# 13+/14 `lock` on a `Lock` field bypasses `Monitor`, so Coyote won't model it | Silent loss of coverage in user code that adopts `Lock` | Out of scope here; file a follow-up issue to add a `Rewriting.Types` model. Same for new `Interlocked` overloads on small ints and `Task.WhenEach`. |
| Old test/benchmark tooling can't host net10 | Suites fail to launch | Required bumps in Phase 3 (Test.Sdk 17.14.x, xunit 2.9.x, BDN 0.15.x). |
| Self-hosted perf runner lacks .NET 10 | `test-performance.yml` fails | Install SDK on runner before merging (ops task). |
| `System.CommandLine` 2.0.0-beta4 on net10 | Low — API is self-contained | Works as-is; migrating to stable 2.0 is a breaking-API follow-up, not part of this upgrade. |
| Deprecated `FxCopAnalyzers` 2.9.2 | None at runtime; analyzer noise | Optional follow-up: replace with built-in NetAnalyzers; may surface new warnings (`TreatWarningsAsErrors` in Debug). |

## 7. Explicitly out of scope (follow-up issues to file)

1. Model `System.Threading.Lock` and other post-net8 concurrency APIs in the
   rewriting engine.
2. Migrate `System.CommandLine` beta4 → stable 2.0 (breaking API changes).
3. Replace `Microsoft.CodeAnalysis.FxCopAnalyzers` with NetAnalyzers; raise
   `LangVersion` beyond 10.0.
4. Drop `net8.0` secondary target after its EOL (November 2026).
5. `StyleCop.Analyzers` 1.1.118 → 1.2.0-beta (needed only if LangVersion is raised).
