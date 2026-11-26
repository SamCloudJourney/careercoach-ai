# Patch diff: `vmr/net10.0-sdk4xx` v10.0.403 → v10.0.404

Upstream `https://github.com/dotnet/dotnet.git`. Product files only (tests, eng, docs, version bumps excluded). Full diff: `notes/patch-diffs/raw/vmr_net10.0-sdk4xx__v10.0.403..v10.0.404.diff` (regenerate with `scripts/08-patch-diff.sh`).

| Component | Tier | Product files | +/- lines | Test files |
|---|---|---|---|---|
| `fsharp` | tier3_toolchain | 233 | +18439 / -4078 | 645 |
| `vstest` | tier3_toolchain | 34 | +1036 / -449 | 24 |
| `(vmr root)` | unclassified | 27 | +1039 / -87 | 7 |
| `arcade` | out_of_scope_in_tree | 13 | +190 / -136 | 19 |
| `templating` | tier2_shipped_product | 6 | +256 / -34 | 11 |
| `sdk` | tier2_shipped_product | 11 | +142 / -20 | 24 |
| `roslyn` | tier3_toolchain | 5 | +6 / -3 | 45 |
| `nuget-client` | tier3_toolchain | 1 | +2 / -2 | 9 |
| `msbuild` | tier3_toolchain | 0 | +0 / -0 | 3 |
| `scenario-tests` | out_of_scope_in_tree | 0 | +0 / -0 | 3 |
| `source-build-assets` | out_of_scope_in_tree | 0 | +0 / -0 | 9 |
| `sourcelink` | tier3_toolchain | 0 | +0 / -0 | 5 |

## Highest-churn product files in tier 1/2 components

| +/- | File |
|---|---|
| +164/-0 | `src/templating/src/Microsoft.TemplateEngine.Edge/Installers/NuGet/DotnetNuGetPackageVerifier.cs` |
| +67/-34 | `src/templating/src/Microsoft.TemplateEngine.Edge/Installers/NuGet/NugetApiPackageManager.cs` |
| +73/-0 | `src/sdk/src/Dotnet.Watch/Watch/Utilities/TemporaryFileManager.cs` |
| +27/-0 | `src/sdk/src/Layout/redist/targets/OverlaySdkOnLKG.targets` |
| +5/-18 | `src/sdk/src/Dotnet.Watch/Watch/HotReload/HotReloadDotNetWatcher.cs` |
| +13/-1 | `src/sdk/src/Tasks/sdk-tasks/TarGzFileCreateFromDirectory.cs` |
| +9/-0 | `src/templating/tools/Shared/Microsoft.TemplateEngine.CommandUtils/ArgumentEscaper.cs` |
| +9/-0 | `src/templating/azure-pipelines.yml` |
| +8/-0 | `src/sdk/build/RunTestsOnHelix.sh` |
| +6/-0 | `src/templating/azure-pipelines-pr.yml` |
| +6/-0 | `src/sdk/src/Dotnet.Watch/Watch/Context/DotNetWatchContext.cs` |
| +4/-0 | `src/sdk/src/Cli/dotnet/Commands/VSTest/VSTestCommand.cs` |
| +3/-0 | `src/sdk/.gitignore` |
| +1/-1 | `src/sdk/src/Cli/dotnet/Parser.cs` |
| +1/-0 | `src/templating/src/Microsoft.TemplateEngine.Edge/Microsoft.TemplateEngine.Edge.csproj` |
| +1/-0 | `src/sdk/src/Layout/redist/targets/GenerateArchives.targets` |
| +1/-0 | `src/sdk/src/Cli/dotnet/Commands/Workload/Install/NetSdkMsiInstallerClient.cs` |

## Security-signal hotspots (added lines matching validation / bounds / limits / unsafe vocabulary)

A ranking for where to read first, not a verdict. Confirm each one by reading the hunk in the raw diff.

- **34** `src/vstest/src/Microsoft.TestPlatform.CrossPlatEngine/PostProcessing/ArtifactProcessingManager.cs`
  - `using System.Security;`
  - `EqtTrace.Error($"ArtifactProcessingManager: Ignoring unsafe testSessionCorrelationId '{testSessionCorrelationId}'.");`
  - `// A malformed value must not take the whole run down, the user-private temporary directory`
- **18** `src/fsharp/src/Compiler/TypedTree/GeneratedNames.fs`
  - `NormalizedBasicName: string`
  - `NormalizedBasicName: string`
  - `NormalizedBasicName: string`
- **18** `src/fsharp/src/Compiler/TypedTree/SynthesizedTypeMaps.fs`
  - `/// Replay buckets are keyed by line-normalized basic name. Bucket values remain the`
  - `| Some replayName when replayName.NormalizedBasicName = mapKey -> Some replayName.ReplayOrdinal`
  - `| Some replayName when replayName.NormalizedBasicName = mapKey -> Some [ replayName.ReplayOrdinal ]`
- **13** `src/templating/src/Microsoft.TemplateEngine.Edge/Installers/NuGet/DotnetNuGetPackageVerifier.cs`
  - `using System.Security;`
  - `_ = packagePath ?? throw new ArgumentNullException(nameof(packagePath));`
  - `_ = repository ?? throw new ArgumentNullException(nameof(repository));`
- **12** `src/fsharp/src/Compiler/AbstractIL/FSharpDeltaMetadataWriter.fs`
  - `let private validatePrimaryKeyOrder tableName getPrimaryKey rows =`
  - `let normalizedExternalRowCounts =`
  - `DeltaMetadataSerializer.computeMetadataSizes emptyMirror normalizedExternalRowCounts`
- **11** `eng/tools/tasks/Microsoft.DotNet.UnifiedBuild.Tasks/CreateSourceArtifact.cs`
  - `throw new InvalidDataException($"Unexpected end of tar archive after reading {bytesRead} bytes of a header.");`
  - `NormalizeOwnership(headerBuffer);`
  - `long paddedDataLength = checked((dataLength + TarBlockSize - 1) / TarBlockSize * TarBlockSize);`
- **8** `src/fsharp/src/Compiler/Checking/Expressions/CheckComputationExpressions.fs`
  - `match (List.map (BindingNormalization.NormalizeBinding ValOrMemberBinding cenv ceenv.env) binds) with`
  - `| [ NormalizedBinding(_, SynBindingKind.Normal, false, false, _, _, _, _, _, _, _, _) ] when not isRec -> ()`
  - `| normalizedBindings ->`
- **7** `src/fsharp/src/Compiler/AbstractIL/DeltaMetadataTables.fs`
  - `invalidArg (nameof value) "Compressed integer is too large for CLI metadata."`
  - `let mutable maxLength = 1`
  - `maxLength <- max maxLength neededLength`
- **7** `src/fsharp/src/Compiler/AbstractIL/EncMethodDebugInformation.fs`
  - `/// depth-2 keys (>= 0x10000) never collide. Fails closed (None) past the limits: chains`
  - `// int32 and a negative key slips past the <= MaxOccurrenceKey bound, failing OPEN.`
  - `raise (InvalidDataException $"invalid EnC %s{blobName} blob: unexpected data at offset %d{offset}")`
- **7** `src/fsharp/src/Compiler/Service/ServiceParseTreeWalk.fs`
  - `yield dive x x.Range traverseSynExpr`
  - `yield dive () m (fun () -> visitor.VisitExpr(path, traverseSynExpr, traverseSynExpr, expr))`
  - `yield dive expr expr.Range traverseSynExpr`
- **7** `src/vstest/src/Microsoft.TestPlatform.CommunicationUtilities/SocketServer.cs`
  - `lock (_stateSyncObject)`
  - `throw new ObjectDisposedException(nameof(SocketServer));`
  - `lock (_stateSyncObject)`
- **6** `src/sdk/src/Dotnet.Watch/Watch/Utilities/TemporaryFileManager.cs`
  - `using System.Security.AccessControl;`
  - `using System.Security.Principal;`
  - `public void Dispose()`
- **5** `src/fsharp/src/Compiler/TypedTree/TypedTreeOps.FreeVars.fs`
  - `let rec foldTypeButNotConstraints normalizeType f z ty =`
  - `let ty = normalizeType ty`
  - `| TType_forall(_, bodyTy) -> foldTypeButNotConstraints normalizeType f z bodyTy`
- **4** `src/fsharp/src/Compiler/AbstractIL/DeltaMetadataSerializer.fs`
  - `let normalizedExternal =`
  - `DeltaIndexSizing.compute rowCounts normalizedExternal heapSizes isEncDelta`
  - `invalidArg "element" $"String heap offset index out of range: {value} (offsetCount={input.StringHeapOffsets.Length})"`
- **4** `src/fsharp/src/Compiler/Checking/Expressions/CheckExpressions.fs`
  - `// Resolve the caller argument bound to 'paramName' and return the type of its type-checked expression.`
  - `| NormalizedBinding (_, _, _, _, [], _, _, _, SynPat.Named(SynIdent(id,_), _, _, _), NormalizedBindingRhs(_, _, rhsExpr), _, _) -> id.idText`
  - `// Format the already-checked hole via a synthesized 'String.Format', binding its boxed value`
- **4** `src/fsharp/src/Compiler/SyntaxTree/XmlDocIncludeExpander.fs`
  - `// Materialize inside the try: XPathSelectElements is lazily enumerated and throws`
  - `Result.Error $"Invalid XPath expression '{xpath}': {ex.Message}"`
  - `/// Genuine failure (missing file, invalid/empty XPath, cycle): the short reason, framed and warned by the caller.`
- **3** `eng/common/core-templates/job/helix-job-monitor.yml`
  - `- pwsh: throw "azureSubscription must be set when useEntraAuthentication is true."`
  - `displayName: Validate Helix Entra authentication`
  - `throw "The Helix Azure service connection did not provide a service principal or tenant ID."`
- **3** `eng/common/core-templates/steps/send-to-helix.yml`
  - `- pwsh: throw "HelixAzureSubscription must be set when HelixUseEntraAuthentication is true."`
  - `displayName: Validate Helix Entra authentication`
  - `throw "The Helix Azure service connection did not provide a service principal or tenant ID."`
- **3** `eng/extract-sdk-archive.proj`
  - `<_SdkExtractDir>$([MSBuild]::NormalizeDirectory('$(DotNetSdkExtractDir)'))</_SdkExtractDir>`
  - `Condition="'$(_SdkArchiveReady)' != 'true' and $([MSBuild]::NormalizeDirectory('$(MSBuildBinPath)').StartsWith('$(_SdkExtractDir)'))" />`
  - `<_SdkArtifactsObjDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsObjDir)'))</_SdkArtifactsObjDir>`
- **3** `src/fsharp/src/Compiler/AbstractIL/DeltaIndexSizing.fs`
  - `/// has enough rows to overflow the available bits after the tag.`
  - `let limit = pown 2 (16 - tagBits)`
  - `referenceExceedsLimit tableRowCounts externalRowCounts limit tables`
- **3** `src/fsharp/src/Compiler/AbstractIL/EncMethodDebugInformation.fsi`
  - `/// closed (None) past the limits: chains deeper than 2, ordinals > 0xFFFF, or keys`
  - `/// validations). An empty (or null) blob yields no states.`
  - `/// None means either the record is absent or invalid; callers must fall back to IL`
- **3** `src/fsharp/src/Compiler/Facilities/DiagnosticsLogger.fsi`
  - `/// Same as 'NormalizeErrorString', but applied to the parts of a rich message, so that the`
  - `/// classification of each part is preserved. Parts left empty by normalization are dropped.`
  - `val NormalizeErrorRichText: text: RichText -> RichText`
- **3** `src/fsharp/src/Compiler/Optimize/Optimizer.fs`
  - `/// Lookup information about values, without reporting values that are not bound yet`
  - `// Normalize the elided unit parameter of a zero-parameter Invoke (e.g. System.Action) exactly as`
  - `// Values bound within the body being walked have no info yet, but the walk covers them anyway.`
- **3** `src/fsharp/src/Compiler/Symbols/XmlDocInheritance.fs`
  - `/// Bounds non-tail recursion on deep acyclic explicit-cref chains, which would otherwise raise an`
  - `// and the whole doc is re-wrapped in an implicit <summary> and XML-escaped. Normalise`
  - `// FSharpSymbol.XmlDoc. Besides XML parse errors, the caller-supplied resolveCref can throw`
- **3** `src/sdk/src/Tasks/sdk-tasks/TarGzFileCreateFromDirectory.cs`
  - `/// Whether to normalize archive ownership for source-build artifacts.`
  - `public bool NormalizeSourceBuildOwnership { get; set; }`
  - `string ownershipArguments = NormalizeSourceBuildOwnership`
- **2** `src/fsharp/.github/workflows/check_release_notes.yml`
  - `echo "::error::Cannot safely validate a PR with 3,000 or more changed files."`
  - `# "Resource not accessible by integration"), even though release-notes validation`
- **2** `src/fsharp/src/Compiler/CodeGen/ILBaselineReader.fs`
  - `invalidArg (nameof bytes) "invalid metadata stream header"`
  - `// Failing before decoding prevents malformed offsets from reading an adjacent heap.`
- **2** `src/fsharp/src/Compiler/Facilities/DiagnosticsLogger.fs`
  - `let NormalizeErrorRichText (text: RichText) =`
  - `// 'NormalizeErrorString' trims the message as a whole, so the trimmed range is computed over all`
- **2** `src/fsharp/src/Compiler/Optimize/DelegateForwarding.fs`
  - `// each bound value maps to the expression flowing into it, resolved when the arguments are matched.`
  - `/// leading formals consumed by a bound Target. The return type must match exactly for a non-generic`
- **2** `src/fsharp/src/Compiler/Symbols/SymbolHelpers.fs`
  - `// text merely mentions "<inheritdoc" but it gets XML-escaped into an implicit <summary>`
  - `// whole thing in an implicit <summary> and XML-escaping the inherited markup.`
- **2** `src/fsharp/src/Compiler/SyntaxTree/ParseHelpers.fs`
  - `/// hole. '%%' is a literal escape, not a specifier.`
  - `i <- i + 2 // '%%' escape, keep scanning`
- **2** `src/fsharp/src/Compiler/TypedTree/TypedTreeOps.FreeVars.fsi`
  - `/// Utilities for traversing and simplifying types`
  - `/// Fold normalized type structure without following type-parameter constraints.`
- **2** `src/fsharp/src/FSharp.Core/async.fsi`
  - `/// <p>Exceptions thrown by <c>GetResult()</c> are propagated directly.</p>`
  - `/// Any exception thrown by <c>handler</c> will propagate.</param>`
- **2** `src/fsharp/src/FSharp.Core/tasks.fsi`
  - `/// Any exception thrown by <c>handler</c> will propagate.</param>`
  - `/// Any exception thrown by <c>handler</c> will propagate.</param>`
- **2** `src/templating/src/Microsoft.TemplateEngine.Edge/Installers/NuGet/NugetApiPackageManager.cs`
  - `throw new DownloadException(packageMetadata.Identity.Id, packageMetadata.Identity.Version.ToNormalizedString(), new[] { source.Source }, e);`
  - `throw;`
- **1** `eng/pipelines/templates/steps/prepare-source-built-test-sdk.yml`
  - `echo "Invalid Microsoft.NET.Test.Sdk version in '$manifest': '$test_sdk_version'." >&2`
- **1** `eng/tools/tasks/Microsoft.DotNet.UnifiedBuild.Tasks/LogRepoBuildDiagnostics.cs`
  - `throw new FileNotFoundException($"Repo build log file not found: '{LogFile}'.", LogFile);`
- **1** `repo-projects/Directory.Build.props`
  - `so each ';' is backtick-escaped to survive as a single argument. On other platforms the`
- **1** `src/arcade/Directory.Packages.props`
  - `<PackageVersion Include="System.Security.Cryptography.Pkcs" Version="$(SystemSecurityCryptographyPkcsVersion)" />`
- **1** `src/arcade/src/Microsoft.DotNet.Helix/JobMonitor/Services/HelixService.cs`
  - `throw;`
