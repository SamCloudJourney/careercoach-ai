# Patch diff: `vmr/net10.0` v10.0.14 → v10.0.15

Upstream `https://github.com/dotnet/dotnet.git`. Product files only (tests, eng, docs, version bumps excluded). Full diff: `notes/patch-diffs/raw/vmr_net10.0__v10.0.14..v10.0.15.diff` (regenerate with `scripts/08-patch-diff.sh`).

| Component | Tier | Product files | +/- lines | Test files |
|---|---|---|---|---|
| `vstest` | tier3_toolchain | 32 | +1181 / -322 | 44 |
| `runtime` | tier1_core | 76 | +922 / -351 | 45 |
| `(vmr root)` | unclassified | 22 | +860 / -81 | 5 |
| `aspnetcore` | tier1_core | 48 | +692 / -169 | 48 |
| `sdk` | tier2_shipped_product | 17 | +443 / -40 | 46 |
| `templating` | tier2_shipped_product | 9 | +299 / -52 | 18 |
| `razor` | tier3_toolchain | 12 | +330 / -11 | 7 |
| `arcade` | out_of_scope_in_tree | 12 | +185 / -132 | 19 |
| `nuget-client` | tier3_toolchain | 7 | +70 / -100 | 75 |
| `efcore` | tier3_toolchain | 5 | +33 / -12 | 23 |
| `roslyn` | tier3_toolchain | 4 | +4 / -4 | 15 |
| `winforms` | tier2_shipped_product | 2 | +2 / -2 | 13 |
| `windowsdesktop` | tier2_shipped_product | 1 | +3 / -0 | 14 |
| `command-line-api` | tier3_toolchain | 0 | +0 / -0 | 12 |
| `emsdk` | tier3_toolchain | 0 | +0 / -0 | 12 |
| `fsharp` | tier3_toolchain | 0 | +0 / -0 | 3 |
| `msbuild` | tier3_toolchain | 0 | +0 / -0 | 7 |
| `scenario-tests` | out_of_scope_in_tree | 0 | +0 / -0 | 12 |
| `source-build-assets` | out_of_scope_in_tree | 0 | +0 / -0 | 12 |
| `sourcelink` | tier3_toolchain | 0 | +0 / -0 | 1 |
| `symreader` | tier3_toolchain | 0 | +0 / -0 | 35 |
| `wpf` | tier2_shipped_product | 0 | +0 / -0 | 10 |
| `xdt` | tier3_toolchain | 0 | +0 / -0 | 9 |

## Highest-churn product files in tier 1/2 components

| +/- | File |
|---|---|
| +300/-7 | `src/sdk/documentation/manpages/sdk/dotnet-run.1` |
| +164/-0 | `src/templating/src/Microsoft.TemplateEngine.Edge/Installers/NuGet/DotnetNuGetPackageVerifier.cs` |
| +118/-42 | `src/runtime/src/libraries/System.Net.HttpListener/src/System/Net/Managed/HttpConnection.cs` |
| +142/-0 | `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/UserIdentitySnapshot.cs` |
| +122/-18 | `src/aspnetcore/src/SignalR/server/StackExchangeRedis/src/Internal/RedisSubscriptionManager.cs` |
| +67/-34 | `src/templating/src/Microsoft.TemplateEngine.Edge/Installers/NuGet/NugetApiPackageManager.cs` |
| +61/-18 | `src/runtime/src/libraries/System.Threading.RateLimiting/src/System/Threading/RateLimiting/DefaultPartitionedRateLimiter.cs` |
| +54/-18 | `src/aspnetcore/src/submodules/googletest/googletest/include/gtest/internal/gtest-internal.h` |
| +64/-4 | `src/runtime/src/libraries/System.Net.HttpListener/src/System/Net/Managed/ChunkedInputStream.cs` |
| +35/-32 | `src/runtime/src/libraries/System.Net.HttpListener/src/System/Net/Managed/ChunkStream.cs` |
| +13/-50 | `src/runtime/src/mono/browser/runtime/jiterpreter-support.ts` |
| +12/-47 | `src/aspnetcore/src/SignalR/server/StackExchangeRedis/src/RedisHubLifetimeManager.cs` |
| +43/-14 | `src/sdk/documentation/manpages/sdk/dotnet-sln.1` |
| +45/-9 | `src/runtime/src/native/libs/System.Security.Cryptography.Native/openssl.c` |
| +49/-4 | `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionDispatcher.cs` |
| +42/-2 | `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionManager.cs` |
| +25/-18 | `src/runtime/src/native/libs/System.Security.Cryptography.Native/apibridge.c` |
| +41/-1 | `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionContext.cs` |
| +30/-12 | `src/runtime/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs` |
| +28/-13 | `src/templating/tools/Shared/Microsoft.TemplateEngine.CommandUtils/StreamForwarder.cs` |
| +28/-13 | `src/runtime/src/libraries/System.Formats.Tar/src/System/Formats/Tar/TarHeader.Write.cs` |
| +23/-18 | `src/aspnetcore/src/submodules/googletest/googletest/include/gtest/gtest-typed-test.h` |
| +29/-7 | `src/runtime/src/coreclr/vm/exceptionhandling.cpp` |
| +34/-0 | `src/runtime/src/native/minipal/time.c` |
| +34/-0 | `src/runtime/src/installer/pkg/sfx/Microsoft.NETCore.App/Microsoft.NETCore.App.Host.sfxproj` |
| +22/-11 | `src/runtime/src/libraries/System.Security.Cryptography/src/Microsoft/Win32/SafeHandles/NCryptSafeHandles.cs` |
| +32/-0 | `src/runtime/src/libraries/System.Formats.Tar/src/System/Formats/Tar/TarHelpers.cs` |
| +16/-16 | `src/aspnetcore/package-lock.json` |
| +31/-0 | `src/runtime/src/installer/pkg/sfx/Microsoft.NETCore.App/Directory.Build.targets` |
| +30/-0 | `src/runtime/src/coreclr/jit/objectalloc.cpp` |
| +29/-1 | `src/runtime/src/coreclr/gc/vxsort/vxsort.h` |
| +27/-2 | `src/aspnetcore/src/submodules/MessagePack-CSharp/src/MessagePack.UnityClient/Assets/Scripts/MessagePack/MessagePackReader.cs` |
| +26/-3 | `src/sdk/documentation/manpages/sdk/dotnet-test.1` |
| +20/-0 | `src/aspnetcore/src/Servers/Kestrel/Core/src/KestrelServerOptions.cs` |
| +19/-0 | `src/aspnetcore/src/Shared/CookiePathEscaper.cs` |
| +13/-6 | `src/runtime/src/native/libs/verify-so.sh` |
| +10/-9 | `src/runtime/src/mono/browser/runtime/jiterpreter.ts` |
| +13/-5 | `src/templating/tools/Microsoft.TemplateEngine.Authoring.CLI/Commands/ExecutableCommand.cs` |
| +4/-14 | `src/runtime/src/native/libs/configure.cmake` |
| +1/-17 | `src/runtime/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/CborConformanceLevel.cs` |
| +14/-3 | `src/runtime/src/native/libs/System.Security.Cryptography.Native/opensslshim.h` |
| +11/-6 | `src/runtime/src/native/libs/System.Native/pal_io.c` |
| +12/-3 | `src/aspnetcore/src/Servers/Kestrel/Transport.NamedPipes/src/Internal/NamedPipeConnectionListener.cs` |
| +14/-0 | `src/runtime/src/coreclr/tools/aot/ILCompiler.ReadyToRun/JitInterface/CorInfoImpl.ReadyToRun.cs` |
| +12/-2 | `src/runtime/src/mono/browser/runtime/jiterpreter-jit-call.ts` |
| +11/-3 | `src/sdk/documentation/manpages/sdk/dotnet-new-install.1` |
| +7/-6 | `src/runtime/src/mono/browser/runtime/memory.ts` |
| +12/-0 | `src/sdk/.config/dotnet-tools.json` |
| +12/-0 | `src/runtime/src/native/libs/System.Security.Cryptography.Native/osslcompat_40.h` |
| +10/-2 | `src/sdk/documentation/manpages/sdk/dotnet-reference-add.1` |
| +10/-2 | `src/sdk/documentation/manpages/sdk/dotnet-format.1` |
| +8/-4 | `src/runtime/src/mono/browser/runtime/strings.ts` |
| +9/-2 | `src/aspnetcore/src/Servers/Kestrel/Core/src/Internal/Http2/Http2Stream.cs` |
| +7/-4 | `src/runtime/src/mono/browser/runtime/jiterpreter-interp-entry.ts` |
| +6/-5 | `src/runtime/src/mono/browser/runtime/jiterpreter-trace-generator.ts` |
| +5/-6 | `src/aspnetcore/src/submodules/googletest/googletest/src/gtest-typed-test.cc` |
| +4/-7 | `src/runtime/src/coreclr/tools/aot/ILCompiler.Compiler/Compiler/DependencyAnalysis/NativeLayoutVertexNode.cs` |
| +10/-0 | `src/aspnetcore/src/SignalR/server/StackExchangeRedis/src/Internal/IRedisFeature.cs` |
| +10/-0 | `src/aspnetcore/src/Servers/Kestrel/Core/src/Internal/Http/Http1Connection.cs` |
| +5/-5 | `src/runtime/src/mono/browser/runtime/web-socket.ts` |

## Security-signal hotspots (added lines matching validation / bounds / limits / unsafe vocabulary)

A ranking for where to read first, not a verdict. Confirm each one by reading the hunk in the raw diff.

- **34** `src/vstest/src/Microsoft.TestPlatform.CrossPlatEngine/PostProcessing/ArtifactProcessingManager.cs`
  - `using System.Security;`
  - `EqtTrace.Error($"ArtifactProcessingManager: Ignoring unsafe testSessionCorrelationId '{testSessionCorrelationId}'.");`
  - `// A malformed value must not take the whole run down, the user-private temporary directory`
- **29** `src/runtime/src/libraries/System.Threading.RateLimiting/src/System/Threading/RateLimiting/DefaultPartitionedRateLimiter.cs`
  - `private readonly Dictionary<TKey, Lazy<LimiterEntry>> _limiters;`
  - `private readonly List<KeyValuePair<TKey, Lazy<LimiterEntry>>> _cachedLimiters = new();`
  - `_limiters = new Dictionary<TKey, Lazy<LimiterEntry>>(equalityComparer);`
- **13** `src/templating/src/Microsoft.TemplateEngine.Edge/Installers/NuGet/DotnetNuGetPackageVerifier.cs`
  - `using System.Security;`
  - `_ = packagePath ?? throw new ArgumentNullException(nameof(packagePath));`
  - `_ = repository ?? throw new ArgumentNullException(nameof(repository));`
- **7** `src/runtime/src/coreclr/vm/exceptionhandling.cpp`
  - `ExInfo exInfo(GetThread(), ex.GetExceptionRecord(), ex.GetContextRecord(), ExKind::Throw);`
  - `DispatchManagedException(throwable, &exInfo);`
  - `DispatchManagedException(throwable, &exInfo);`
- **7** `src/vstest/src/Microsoft.TestPlatform.CommunicationUtilities/SocketServer.cs`
  - `lock (_stateSyncObject)`
  - `throw new ObjectDisposedException(nameof(SocketServer));`
  - `lock (_stateSyncObject)`
- **5** `src/runtime/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs`
  - `Interlocked.Add(ref coherentState._cacheSize, -priorEntry.Size);`
  - `Interlocked.Add(ref coherentState._cacheSize, -entry.Size);`
  - `// freed by the replace), so a same-or-smaller replacement at the size limit is admitted.`
- **5** `src/runtime/src/libraries/System.Formats.Tar/src/System/Formats/Tar/TarHelpers.cs`
  - `/// <summary>Parses a uid or gid extended attribute value. See <see cref="ParseUidGid"/> for how out of range values are handled.</summary>`
  - `/// overflow check, which matches how <see cref="TarWriter"/> stores uid and gid values read from the file system.`
  - `/// Values that don't fit in 32 bits can't be a valid id and are rejected.`
- **4** `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionDispatcher.cs`
  - `if (await RejectIfConnectionUserChangedAsync(connection, context))`
  - `if (await RejectIfConnectionUserChangedAsync(connection, context))`
  - `if (await RejectIfConnectionUserChangedAsync(connection, context))`
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
- **3** `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionContext.cs`
  - `lock (_userLock)`
  - `lock (_userLock)`
  - `lock (_userLock)`
- **3** `src/runtime/src/libraries/System.Net.HttpListener/src/System/Net/Managed/HttpConnection.cs`
  - `/// -> ..., which could otherwise grow the stack without bound for a client that keeps sending`
  - `// checked against the timeout from the request's very first read.`
  - `/// this read loop (bound to a context, or the connection was torn down).`
- **2** `src/aspnetcore/src/Caching/SqlServer/src/Microsoft.Extensions.Caching.SqlServer.csproj`
  - `<PackageReference Include="System.Security.Cryptography.Pkcs" Version="$(SqlClientSystemSecurityCryptographyPkcs9Version)" AllowExplicitRefe`
  - `<PackageReference Include="System.Security.Cryptography.Pkcs" Version="$(SqlClientSystemSecurityCryptographyPkcs8Version)" AllowExplicitRefe`
- **2** `src/aspnetcore/src/Http/Http/src/Features/FormOptions.cs`
  - `/// A limit for the number of header lines to allow in each multipart section. Headers with the same name`
  - `/// count separately. Form sections that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.`
- **2** `src/aspnetcore/src/Mvc/Mvc.Core/src/RequestFormLimitsAttribute.cs`
  - `/// A limit for the number of header lines to allow in each multipart section. Headers with the same name`
  - `/// count separately. Form sections that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.`
- **2** `src/aspnetcore/src/Servers/Kestrel/Core/src/Internal/Http/Http1Connection.cs`
  - `ThrowRequestTargetRejected(target);`
  - `ThrowRequestTargetRejected(target);`
- **2** `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/UserIdentitySnapshot.cs`
  - `using System.Security.Claims;`
  - `&& current._allAuthenticatedClaims.AsSpan().SequenceEqual(incoming._allAuthenticatedClaims);`
- **2** `src/aspnetcore/src/SignalR/server/StackExchangeRedis/src/Internal/RedisSubscriptionManager.cs`
  - `throw;`
  - `throw;`
- **2** `src/templating/src/Microsoft.TemplateEngine.Edge/Installers/NuGet/NugetApiPackageManager.cs`
  - `throw new DownloadException(packageMetadata.Identity.Id, packageMetadata.Identity.Version.ToNormalizedString(), new[] { source.Source }, e);`
  - `throw;`
- **2** `src/vstest/azure-pipelines-official.yml`
  - `# PULL REQUEST VALIDATION, ported from 18.12. A pull request build must not sign and must not`
  - `# Raised from 120. Build 3076344 was killed at the 120 minute limit with Unit Test still`
- **2** `src/vstest/src/Microsoft.TestPlatform.CommunicationUtilities/SocketCommunicationManager.cs`
  - `_connectionCancellation.Dispose();`
  - `_connectionCancellation.Dispose();`
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
- **1** `src/aspnetcore/src/Antiforgery/src/Internal/DefaultAntiforgeryTokenStore.cs`
  - `options.Path = CookiePathEscaper.Escape(pathBase);`
- **1** `src/aspnetcore/src/Http/Http.Abstractions/src/Metadata/IFormOptionsMetadata.cs`
  - `/// A limit for the number of header lines to allow in each multipart section.`
- **1** `src/aspnetcore/src/Http/WebUtilities/src/MultipartReader.cs`
  - `/// The limit for the number of header lines to read per multipart section.`
- **1** `src/aspnetcore/src/Mvc/Mvc.ViewFeatures/src/CookieTempDataProvider.cs`
  - `cookieOptions.Path = CookiePathEscaper.Escape(pathBase);`
- **1** `src/aspnetcore/src/ProjectTemplates/Web.ProjectTemplates/content/BlazorWeb-CSharp/BlazorWebCSharp.1/Components/Account/Pages/ForgotPassword.razor`
  - `throw new InvalidOperationException("Users must have an email.");`
- **1** `src/aspnetcore/src/ProjectTemplates/Web.ProjectTemplates/content/BlazorWeb-CSharp/BlazorWebCSharp.1/Components/Account/Pages/ResendEmailConfirmation.razor`
  - `throw new InvalidOperationException("Users must have an email.");`
- **1** `src/aspnetcore/src/Security/Authentication/Core/src/RequestPathBaseCookieBuilder.cs`
  - `path = CookiePathEscaper.Escape(originalPathBase + AdditionalPath);`
- **1** `src/aspnetcore/src/Servers/Kestrel/Transport.NamedPipes/src/Internal/NamedPipeConnectionListener.cs`
  - `brokenStream.Dispose();`
- **1** `src/aspnetcore/src/Shared/CookiePathEscaper.cs`
  - `public static string? Escape(string? value)`
- **1** `src/aspnetcore/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionManager.cs`
  - `using System.Security.Claims;`
- **1** `src/aspnetcore/src/Tools/dotnet-sql-cache/src/dotnet-sql-cache.csproj`
  - `<PackageReference Include="System.Security.Cryptography.Pkcs" Version="$(SqlClientSystemSecurityCryptographyPkcs9Version)" AllowExplicitRefe`
- **1** `src/aspnetcore/src/submodules/MessagePack-CSharp/src/MessagePack.UnityClient/Assets/Scripts/MessagePack/MessagePackReader.cs`
  - `ThrowInsufficientBufferUnless(`
- **1** `src/efcore/Directory.Packages.props`
  - `<PackageVersion Include="System.Security.Cryptography.Pkcs" Version="10.0.13" />`
