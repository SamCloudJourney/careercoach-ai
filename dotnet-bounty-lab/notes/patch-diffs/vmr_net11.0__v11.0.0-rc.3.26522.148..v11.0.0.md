# Patch diff: `vmr/net11.0` v11.0.0-rc.3.26522.148 → v11.0.0

Upstream `https://github.com/dotnet/dotnet.git`. Product files only (tests, eng, docs, version bumps excluded). Full diff: `notes/patch-diffs/raw/vmr_net11.0__v11.0.0-rc.3.26522.148..v11.0.0.diff` (regenerate with `scripts/08-patch-diff.sh`).

| Component | Tier | Product files | +/- lines | Test files |
|---|---|---|---|---|
| `vstest` | tier3_toolchain | 99 | +14335 / -6070 | 122 |
| `sdk` | tier2_shipped_product | 163 | +5188 / -4576 | 208 |
| `runtime` | tier1_core | 164 | +4560 / -1529 | 116 |
| `roslyn` | tier3_toolchain | 163 | +2401 / -1025 | 112 |
| `aspnetcore` | tier1_core | 107 | +1907 / -1064 | 141 |
| `diagnostics` | tier3_toolchain | 56 | +724 / -583 | 158 |
| `fsharp` | tier3_toolchain | 23 | +344 / -429 | 16 |
| `efcore` | tier3_toolchain | 28 | +354 / -63 | 53 |
| `nuget-client` | tier3_toolchain | 30 | +281 / -127 | 20 |
| `msbuild` | tier3_toolchain | 8 | +214 / -57 | 27 |
| `(vmr root)` | unclassified | 9 | +215 / -49 | 7 |
| `arcade` | out_of_scope_in_tree | 14 | +127 / -53 | 19 |
| `winforms` | tier2_shipped_product | 2 | +2 / -2 | 5 |
| `sourcelink` | tier3_toolchain | 1 | +2 / -1 | 13 |
| `windowsdesktop` | tier2_shipped_product | 1 | +3 / -0 | 13 |
| `command-line-api` | tier3_toolchain | 0 | +0 / -0 | 1 |
| `emsdk` | tier3_toolchain | 0 | +0 / -0 | 11 |
| `scenario-tests` | out_of_scope_in_tree | 0 | +0 / -0 | 6 |
| `symreader` | tier3_toolchain | 0 | +0 / -0 | 20 |
| `wpf` | tier2_shipped_product | 0 | +0 / -0 | 2 |
| `xdt` | tier3_toolchain | 0 | +0 / -0 | 4 |

## Highest-churn product files in tier 1/2 components

| +/- | File |
|---|---|
| +751/-475 | `src/runtime/src/libraries/System.Numerics.Tensors/src/System/Numerics/Tensors/netcore/Tensor.cs` |
| +23/-1043 | `src/sdk/documentation/manpages/sdk/dotnet-test.1` |
| +700/-132 | `src/runtime/src/libraries/System.Numerics.Tensors/src/System/Numerics/Tensors/netcore/TensorOperation.cs` |
| +665/-30 | `src/sdk/src/Dotnet.Watch/HotReloadClient/Web/BrowserRefreshServer.cs` |
| +690/-0 | `src/sdk/src/StaticWebAssetsSdk/Targets/HotReload/HotReload-browser-tools.js` |
| +0/-647 | `src/sdk/src/StaticWebAssetsSdk/Targets/DotNetWatch/dotnet-watch-browser-tools.js` |
| +0/-614 | `src/sdk/src/Dotnet.Watch/HotReloadClient/Web/AbstractBrowserRefreshServer.cs` |
| +341/-69 | `src/runtime/src/libraries/System.Numerics.Tensors/src/System/Numerics/Tensors/netcore/TensorShape.cs` |
| +43/-231 | `src/aspnetcore/src/submodules/googletest/googletest/src/gtest-port.cc` |
| +0/-271 | `src/sdk/src/Dotnet.Watch/Watch/Browser/BrowserToolsBuildOutputs.cs` |
| +257/-11 | `src/sdk/src/Cli/dotnet/ToolPackage/ToolPackageDownloaderBase.cs` |
| +254/-0 | `src/sdk/src/Dotnet.Watch/HotReloadClient/Web/BrowserToolsBuildOutputs.cs` |
| +227/-0 | `src/sdk/src/Cli/Microsoft.DotNet.Cli.Utils/FileLockOwnerFinder.Linux.cs` |
| +222/-0 | `src/sdk/src/StaticWebAssetsSdk/Targets/Microsoft.NET.Sdk.StaticWebAssets.HotReload.targets` |
| +0/-222 | `src/sdk/src/StaticWebAssetsSdk/Targets/Microsoft.NET.Sdk.StaticWebAssets.DotNetWatch.targets` |
| +122/-94 | `src/aspnetcore/src/submodules/googletest/googletest/include/gtest/internal/gtest-port.h` |
| +133/-63 | `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionDispatcher.cs` |
| +156/-30 | `src/sdk/documentation/manpages/sdk/dotnet-environment-variables.7` |
| +139/-39 | `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionContext.cs` |
| +175/-0 | `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/UserIdentitySnapshot.cs` |
| +164/-0 | `src/sdk/src/TemplateEngine/Microsoft.TemplateEngine.Edge/Installers/NuGet/DotnetNuGetPackageVerifier.cs` |
| +118/-42 | `src/runtime/src/libraries/System.Net.HttpListener/src/System/Net/Managed/HttpConnection.cs` |
| +91/-65 | `src/runtime/src/tasks/AndroidAppBuilder/Templates/monodroid-nativeaot.cs` |
| +122/-33 | `src/sdk/src/Cli/dotnet/ToolPackage/ToolPackageDownloader.cs` |
| +121/-24 | `src/runtime/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/Validator.cs` |
| +143/-0 | `src/sdk/src/WasmSdk/Sdk/HotReload/Microsoft.NET.Sdk.WebAssembly.HotReload.lib.module.js.template` |
| +141/-0 | `src/sdk/src/StaticWebAssetsSdk/Tasks/EnsureHotReloadBrowserToolsKey.cs` |
| +0/-141 | `src/sdk/src/StaticWebAssetsSdk/Tasks/EnsureDotNetWatchBrowserToolsKey.cs` |
| +122/-18 | `src/aspnetcore/src/SignalR/server/StackExchangeRedis/src/Internal/RedisSubscriptionManager.cs` |
| +98/-36 | `src/sdk/documentation/manpages/sdk/dotnet-pack.1` |
| +103/-29 | `src/runtime/src/coreclr/System.Private.CoreLib/src/System/StubHelpers.cs` |
| +110/-18 | `src/runtime/src/libraries/System.Numerics.Tensors/src/System/Numerics/Tensors/netcore/TensorPrimitives.Tan.cs` |
| +60/-68 | `src/sdk/documentation/manpages/sdk/dotnet-run.1` |
| +109/-17 | `src/runtime/src/libraries/System.Diagnostics.Process/src/Microsoft/Win32/SafeHandles/SafeProcessHandle.Windows.cs` |
| +118/-0 | `src/sdk/src/Dotnet.Watch/HotReloadClient/Web/HttpContextWrappers.cs` |
| +117/-0 | `src/sdk/src/Dotnet.Watch/HotReloadClient/Web/NetFrameworkWebSocketServer.cs` |
| +114/-1 | `src/runtime/src/libraries/Microsoft.Extensions.Options/src/OptionsBuilder.cs` |
| +87/-28 | `src/runtime/src/coreclr/vm/olevariant.cpp` |
| +89/-25 | `src/runtime/src/libraries/System.Private.CoreLib/src/System/Decimal.DecCalc.cs` |
| +0/-114 | `src/sdk/src/WasmSdk/Sdk/DotNetWatch/Microsoft.NET.Sdk.WebAssembly.DotNetWatch.lib.module.js.template` |
| +73/-40 | `src/sdk/documentation/manpages/sdk/dotnet-build.1` |
| +67/-36 | `src/runtime/src/libraries/System.Private.CoreLib/src/System/Threading/NamedMutex.Unix.cs` |
| +67/-34 | `src/sdk/src/TemplateEngine/Microsoft.TemplateEngine.Edge/Installers/NuGet/NugetApiPackageManager.cs` |
| +98/-0 | `src/sdk/src/Cli/Microsoft.DotNet.Cli.Utils/Windows/FileLockOwnerFinder.Windows.cs` |
| +69/-29 | `src/runtime/src/coreclr/vm/ilmarshalers.cpp` |
| +55/-40 | `src/sdk/documentation/manpages/sdk/dotnet-new-sdk-templates.7` |
| +91/-2 | `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionManager.cs` |
| +23/-67 | `src/sdk/documentation/manpages/sdk/dotnet-nuget-trust.1` |
| +77/-9 | `src/aspnetcore/src/Components/Web/src/Virtualization/VirtualizeJsInterop.cs` |
| +76/-8 | `src/runtime/src/libraries/Common/src/System/Number.Parsing.Common.cs` |
| +70/-12 | `src/aspnetcore/src/SignalR/clients/csharp/Http.Connections.Client/src/Internal/LongPollingTransport.cs` |
| +58/-21 | `src/aspnetcore/src/submodules/googletest/googletest/include/gtest/internal/gtest-internal.h` |
| +75/-3 | `src/runtime/src/libraries/System.Numerics.Tensors/src/System/ThrowHelper.cs` |
| +45/-32 | `src/sdk/documentation/manpages/sdk/dotnet-vstest.1` |
| +39/-38 | `src/sdk/documentation/manpages/sdk/dotnet-publish.1` |
| +62/-13 | `src/runtime/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.Multiplexing.Windows.cs` |
| +73/-0 | `src/sdk/src/Dotnet.Watch/Watch/Utilities/TemporaryFileManager.cs` |
| +44/-29 | `src/runtime/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/Task.cs` |
| +10/-63 | `src/runtime/src/libraries/System.Private.CoreLib/src/System/Number.Parsing.cs` |
| +51/-21 | `src/runtime/src/coreclr/jit/gentree.cpp` |

## Security-signal hotspots (added lines matching validation / bounds / limits / unsafe vocabulary)

A ranking for where to read first, not a verdict. Confirm each one by reading the hunk in the raw diff.

- **92** `src/runtime/src/libraries/System.Numerics.Tensors/src/System/Numerics/Tensors/netcore/TensorOperation.cs`
  - `// Three indexed traversals make binary slicing pay off at 16 elements rather than 32.`
  - `private static bool ValidateSourceOverlap<TSource, TDestination>(`
  - `ref TSource destinationReference = ref Unsafe.As<TDestination, TSource>(ref destination._reference);`
- **90** `src/runtime/src/libraries/System.Numerics.Tensors/src/System/Numerics/Tensors/netcore/Tensor.cs`
  - `ref readonly TensorShape shape = ref TensorShape.Normalize(tensors[0]._shape);`
  - `ref readonly TensorShape shape = ref TensorShape.Normalize(tensors[0]._shape);`
  - `ValidateConcatenationDestination(dimension, shape.Lengths, sumOfAxis, destination);`
- **66** `src/runtime/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/Validator.cs`
  - `///         Validation runs in three stages: property validation, type-level attribute validation, and object-level validation.`
  - `///         Object-level validation invokes <see cref="IValidatableObject.Validate" /> if implemented.`
  - `///         Validation runs in three stages: property validation, type-level attribute validation, and object-level validation.`
- **52** `src/runtime/src/libraries/Microsoft.Extensions.Options/src/OptionsBuilder.cs`
  - `/// When the validator implements <see cref="IAsyncValidateOptions{TOptions}"/>, the built-in asynchronous`
  - `/// options creation path invokes <see cref="IAsyncValidateOptions{TOptions}.ValidateAsync"/> instead of`
  - `/// invoking both validation methods. Synchronous options creation continues to invoke`
- **46** `src/runtime/src/libraries/System.Numerics.Tensors/src/System/Numerics/Tensors/netcore/TensorShape.cs`
  - `return checked(left * right);`
  - `throw new ArgumentOutOfRangeException();`
  - `internal static nint GetProduct(ReadOnlySpan<nint> lengths)`
- **34** `src/vstest/src/Microsoft.TestPlatform.CrossPlatEngine/PostProcessing/ArtifactProcessingManager.cs`
  - `using System.Security;`
  - `EqtTrace.Error($"ArtifactProcessingManager: Ignoring unsafe testSessionCorrelationId '{testSessionCorrelationId}'.");`
  - `// A malformed value must not take the whole run down, the user-private temporary directory`
- **25** `src/msbuild/src/Tasks/AssemblyDependency/ReferenceTable.cs`
  - `_cancellationToken.ThrowIfCancellationRequested();`
  - `_cancellationToken.ThrowIfCancellationRequested();`
  - `_cancellationToken.ThrowIfCancellationRequested();`
- **25** `src/sdk/src/Dotnet.Watch/HotReloadClient/Web/BrowserRefreshServer.cs`
  - `public virtual async ValueTask DisposeAsync()`
  - `lock (_stateGuard)`
  - `connection.Dispose();`
- **24** `src/msbuild/src/Tasks/AssemblyDependency/ResolveAssemblyReference.cs`
  - `_cancellationTokenSource.Token.ThrowIfCancellationRequested();`
  - `_cancellationTokenSource.Token.ThrowIfCancellationRequested();`
  - `_cancellationTokenSource.Token.ThrowIfCancellationRequested();`
- **23** `src/vstest/src/Microsoft.TestPlatform.Hashing.Source/XxHashShared.cs`
  - `internal static unsafe class XxHashShared`
  - `ReadOnlySpan<byte> DefaultSecret =>`
  - `byte* secret = stackalloc byte[SecretLengthBytes];`
- **22** `src/vstest/src/Microsoft.TestPlatform.Hashing.Source/XxHash128.cs`
  - `internal sealed unsafe class XxHash128`
  - `throw new ArgumentNullException(nameof(source));`
  - `ThrowDestinationTooShort();`
- **20** `src/sdk/src/Dotnet.Watch/HotReloadClient/Web/BrowserToolsBuildOutputs.cs`
  - `using System.Security.Cryptography;`
  - `throw Fail("the public and private key documents describe different keys");`
  - `throw Fail("the private key could not be imported");`
- **19** `src/runtime/src/coreclr/System.Private.CoreLib/src/System/StubHelpers.cs`
  - `? Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T)))`
  - `: Marshal.SizeOfHelper((RuntimeType)typeof(T), throwIfNotMarshalable: true);`
  - `=> TFixedBuffer.Enabled ? length : checked(length * Marshal.SystemMaxDBCSCharSize);`
- **18** `src/runtime/src/tasks/AndroidAppBuilder/Templates/monodroid-nativeaot.cs`
  - `unsafe`
  - `unsafe`
  - `internal static unsafe partial int AndroidCryptoNative_InitLibraryOnLoad(JavaVM* vm, void* reserved);`
- **17** `src/vstest/.github/workflows/daily-file-diet.lock.yml`
  - `bash "${RUNNER_TEMP}/gh-aw/actions/validate_prompt_placeholders.sh"`
  - `GH_AW_SAFE_OUTPUTS_CONFIG: "{\"create_issue\":{\"labels\":[\"agentic-workflows\"],\"max\":1,\"title_prefix\":\"[file-diet] \"},\"create_pull`
  - `"sanitize": true,`
- **17** `src/vstest/src/Microsoft.TestPlatform.Extensions.TestIdsLogger/TestIdsLogger.cs`
  - `ValidateArg.NotNull(events, nameof(events));`
  - `ValidateArg.NotNullOrEmpty(testResultsDirPath, nameof(testResultsDirPath));`
  - `ValidateArg.NotNull(parameters, nameof(parameters));`
- **14** `src/runtime/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/AsyncValidationAttribute.cs`
  - `///     Allows for providing a resource accessor function that is used by the <see cref="ValidationAttribute.ErrorMessageString" />`
  - `///         Synchronous validation consumers invoke this method. Provide a synchronous implementation when it`
  - `///         can evaluate the applicable rule, and return a validation error when the rule rejects the value.`
- **13** `src/sdk/src/Cli/Microsoft.DotNet.Cli.Utils/FileLockOwnerFinder.Linux.cs`
  - `ReadOnlySpan<char> lockEntry,`
  - `return ReadNextField(ref lockEntry).SequenceEqual("FLOCK") &&`
  - `ReadNextField(ref lockEntry).SequenceEqual("ADVISORY") &&`
- **13** `src/sdk/src/TemplateEngine/Microsoft.TemplateEngine.Edge/Installers/NuGet/DotnetNuGetPackageVerifier.cs`
  - `using System.Security;`
  - `_ = packagePath ?? throw new ArgumentNullException(nameof(packagePath));`
  - `_ = repository ?? throw new ArgumentNullException(nameof(repository));`
- **12** `src/vstest/.github/workflows/efficiency-improver.lock.yml`
  - `# run: "MAX_OPEN_PRS=2\nif [[ \"$GITHUB_EVENT_NAME\" != \"schedule\" ]]; then exit 0; fi\n# gh pr list exits with code 4 when --search retur`
  - `bash "${RUNNER_TEMP}/gh-aw/actions/validate_prompt_placeholders.sh"`
  - `GH_AW_SAFE_OUTPUTS_CONFIG: "{\"add_comment\":{\"hide_older_comments\":true,\"max\":10,\"target\":\"*\"},\"create_issue\":{\"labels\":[\"Area`
- **11** `src/sdk/src/StaticWebAssetsSdk/Targets/HotReload/HotReload-browser-tools.js`
  - `// Bounds how long the replay waits for the Hot Reload agent. Only spent when there is something to`
  - `overflow: scroll;`
  - `const initialized = new Promise((resolve, reject) => { completeInitialization = resolve; failInitialization = reject; });`
- **10** `src/diagnostics/src/SOS/SOS.Extensions/ThreadStackTraceService.cs`
  - `internal sealed unsafe class ThreadStackTraceService : CallableCOMWrapper, IThreadStackTraceService`
  - `private ref readonly IDebuggerThreadStackTraceServiceVTable VTable => ref Unsafe.AsRef<IDebuggerThreadStackTraceServiceVTable>(_vtable);`
  - `ArgumentOutOfRangeException.ThrowIfNegative(maxFrames);`
- **10** `src/runtime/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/IAsyncValidatableObject.cs`
  - `///         Provide a synchronous implementation of <see cref="IValidatableObject.Validate"/> when it can`
  - `///         evaluate the applicable rules, returning validation errors for rules that reject the object.`
  - `///         If a required rule applies but cannot be evaluated synchronously, throw`
- **9** `src/vstest/.github/workflows/issue-repro-triage.lock.yml`
  - `bash "${RUNNER_TEMP}/gh-aw/actions/validate_prompt_placeholders.sh"`
  - `GH_AW_SAFE_OUTPUTS_CONFIG: "{\"add_comment\":{\"hide_older_comments\":true,\"max\":10,\"target\":\"*\"},\"add_labels\":{\"max\":15},\"create`
  - `"sanitize": true,`
- **9** `src/vstest/src/Microsoft.TestPlatform.AdapterUtilities/TestIdProviderXxHash128.cs`
  - `/// its presence trips security tooling. The ids produced here are RFC 9562 version 8 UUIDs that`
  - `/// <exception cref="InvalidOperationException">Thrown if <see cref="GetHash"/> or <see cref="GetId"/> is called already.</exception>`
  - `/// <exception cref="ArgumentNullException">Thrown when <paramref name="str"/> is <see langword="null"/>.</exception>`
- **8** `src/aspnetcore/src/Components/Web/src/Virtualization/VirtualizeJsInterop.cs`
  - `disposedResult: null,`
  - `return InvokeAsync($"{JsFunctionsPrefix}.isFollowingTop", disposedResult: false, _selfReference);`
  - `return InvokeAsync($"{JsFunctionsPrefix}.isFollowingBottom", disposedResult: false, _selfReference);`
- **8** `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionContext.cs`
  - `using System.Security.Principal;`
  - `// True only when _negotiatedUser is a clone this connection must dispose.`
  - `// refresh callback can never hold the negotiate clone while a transport request disposes it.`
- **8** `src/runtime/src/coreclr/vm/exceptionhandling.cpp`
  - `ExInfo exInfo(GetThread(), ex.GetExceptionRecord(), ex.GetContextRecord(), ExKind::Throw);`
  - `DispatchManagedException(throwable, &exInfo);`
  - `DispatchManagedException(throwable, &exInfo);`
- **8** `src/runtime/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/ValidationException.cs`
  - `///     To include the failing validation attribute and invalid value in the exception, use`
  - `///     <see cref="ValidationException(string, ValidationAttribute, object)" />.`
  - `///     To include the failing validation attribute and invalid value in the exception, use the`
- **8** `src/runtime/src/libraries/System.Numerics.Tensors/src/System/ThrowHelper.cs`
  - `public static void ThrowArgument_OverlappingTensorLayoutsNotSupported() =>`
  - `throw new ArgumentException(SR.Argument_OverlappingTensorLayoutsNotSupported, "destination");`
  - `public static void ThrowIfArrayTypeMismatch<T>(Array? array, bool isReadOnly)`
- **8** `src/sdk/src/Cli/dotnet/ToolPackage/ToolPackageDownloaderBase.cs`
  - `using System.Security.Cryptography;`
  - `cancellationToken.ThrowIfCancellationRequested();`
  - `cancellationToken.ThrowIfCancellationRequested();`
- **8** `src/vstest/.github/workflows/code-simplifier.lock.yml`
  - `bash "${RUNNER_TEMP}/gh-aw/actions/validate_prompt_placeholders.sh"`
  - `GH_AW_SAFE_OUTPUTS_CONFIG: "{\"create_pull_request\":{\"expires\":24,\"labels\":[\"agentic-workflows\"],\"max\":1,\"max_patch_files\":100,\"`
  - `"sanitize": true,`
- **8** `src/vstest/.github/workflows/msbuild-quality-review.lock.yml`
  - `bash "${RUNNER_TEMP}/gh-aw/actions/validate_prompt_placeholders.sh"`
  - `GH_AW_SAFE_OUTPUTS_CONFIG: "{\"create_issue\":{\"expires\":168,\"labels\":[\"agentic-workflows\",\"Area: Engineering\"],\"max\":1,\"title_pr`
  - `"sanitize": true,`
- **8** `src/vstest/.github/workflows/pr-iteration.lock.yml`
  - `GH_AW_ALLOWED_DOMAINS: "*.vsblob.vsassets.io,api.nuget.org,api.snapcraft.io,archive.ubuntu.com,azure.archive.ubuntu.com,azuresearch-usnc.nug`
  - `bash "${RUNNER_TEMP}/gh-aw/actions/validate_prompt_placeholders.sh"`
  - `GH_AW_SAFE_OUTPUTS_CONFIG: "{\"add_comment\":{\"hide_older_comments\":true,\"max\":3,\"target\":\"*\"},\"create_report_incomplete_issue\":{}`
- **7** `src/aspnetcore/src/SignalR/clients/ts/signalr/src/HttpClient.ts`
  - `* @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} for any completed HTTP response, `
  - `* @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} for any completed HTTP response, `
  - `* @returns {Promise<HttpResponse>} A Promise that resolves with an {@link @microsoft/signalr.HttpResponse} for any completed HTTP response, `
- **7** `src/runtime/src/libraries/Common/src/Interop/OSX/System.Security.Cryptography.Native.Apple/Interop.Ecc.cs`
  - `private static unsafe partial int AppleCryptoNative_EccExportPublicKeyFromPrivateKey(`
  - `ReadOnlySpan<byte> privateKey,`
  - `unsafe`
- **7** `src/runtime/src/libraries/System.Numerics.Tensors/src/System/Numerics/Tensors/netcore/Tensor_1.cs`
  - `ThrowHelper.ThrowIfArrayTypeMismatch(array);`
  - `ThrowHelper.ThrowIfArrayTypeMismatch(array);`
  - `internal Tensor(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false)`
- **7** `src/vstest/.github/workflows/md-link-check-probe.yml`
  - `# from the checked-in fingerprint at eng/agentic-workflows/known-broken-md-links.txt.`
  - `# that cannot be fixed (a target that is simply gone, with no replacement), and those would`
  - `# Two probes must never race to dispatch the agent: the loser would see neither an open PR`
- **6** `src/aspnetcore/src/Validation/gen/Templates/ValidatablePropertyInfo.cs`
  - `// Collection union cases can have validation of their own as well as validatable elements.`
  - `if (IsUnionValue && validationOptions.TryGetValidatableTypeInfo(propertyValue.GetType(), out var collectionType))`
  - `await collectionType.ValidateAsync(propertyValue, context, cancellationToken);`
- **6** `src/roslyn/src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/Telemetry/LanguageServerTelemetry.cs`
  - `Contract.ThrowIfNull(session);`
  - `Contract.ThrowIfFalse(session.TryAddCommonProperty(`
  - `?? throw new InvalidOperationException("The language server assembly does not have an informational version.");`
