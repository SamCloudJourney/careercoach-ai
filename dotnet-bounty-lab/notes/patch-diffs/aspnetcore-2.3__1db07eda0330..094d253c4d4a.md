# Patch diff: `aspnetcore-2.3` 1db07eda0330 → 094d253c4d4a

Upstream `https://github.com/dotnet/aspnetcore.git`. Product files only (tests, eng, docs, version bumps excluded). Full diff: `notes/patch-diffs/raw/aspnetcore-2.3__1db07eda0330..094d253c4d4a.diff` (regenerate with `scripts/08-patch-diff.sh`).

| Component | Tier | Product files | +/- lines | Test files |
|---|---|---|---|---|
| `aspnetcore` | tier1_core | 33 | +2127 / -924 | 10 |

## Highest-churn product files in tier 1/2 components

| +/- | File |
|---|---|
| +931/-442 | `src/Middleware/SpaServices/src/npm/aspnet-webpack/package-lock.json` |
| +340/-313 | `eng/Baseline.Designer.props` |
| +264/-0 | `src/Shared/PooledMemoryStream.cs` |
| +105/-105 | `eng/Baseline.xml` |
| +187/-0 | `src/SignalR/common/Http.Connections/src/Internal/UserIdentitySnapshot.cs` |
| +53/-41 | `src/Http/WebUtilities/src/FileBufferingReadStream.cs` |
| +55/-2 | `src/SignalR/common/Http.Connections/src/Internal/HttpConnectionDispatcher.cs` |
| +49/-0 | `src/SignalR/common/Http.Connections/src/Internal/HttpConnectionContext.cs` |
| +22/-2 | `src/SignalR/common/Http.Connections/src/Internal/HttpConnectionManager.cs` |
| +21/-0 | `src/Servers/Kestrel/Core/src/KestrelServerOptions.cs` |
| +20/-0 | `.azure/pipelines/ci.yml` |
| +10/-0 | `src/Servers/Kestrel/Core/src/Internal/Http/Http1Connection.cs` |
| +6/-3 | `src/Middleware/SpaServices/src/npm/aspnet-webpack/package.json` |
| +8/-0 | `src/SignalR/common/Http.Connections/src/Internal/HttpConnectionDispatcher.Log.cs` |
| +8/-0 | `src/Servers/Kestrel/Core/src/Internal/Http2/Http2Connection.cs` |
| +7/-1 | `src/Identity/UI/src/Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs` |
| +7/-0 | `src/Middleware/SpaServices/src/npm/aspnet-webpack/.npmrc` |
| +7/-0 | `.npmrc` |
| +6/-1 | `src/Servers/HttpSys/src/RequestProcessing/Request.cs` |
| +4/-0 | `eng/PatchConfig.props` |
| +3/-1 | `src/Servers/HttpSys/src/AsyncAcceptContext.cs` |
| +2/-2 | `build/dependencies.props` |
| +2/-2 | `Directory.Build.targets` |
| +1/-1 | `version.props` |
| +1/-1 | `src/SignalR/common/SignalR.Common/src/Microsoft.AspNetCore.SignalR.Common.csproj` |
| +1/-1 | `src/SignalR/common/Protocols.Json/src/Microsoft.AspNetCore.SignalR.Protocols.Json.csproj` |
| +1/-1 | `src/SignalR/common/Http.Connections/src/Microsoft.AspNetCore.Http.Connections.csproj` |
| +1/-1 | `src/SignalR/common/Http.Connections.Common/src/Microsoft.AspNetCore.Http.Connections.Common.csproj` |
| +1/-1 | `src/Security/Authentication/OAuth/src/Microsoft.AspNetCore.Authentication.OAuth.csproj` |
| +1/-1 | `src/Middleware/NodeServices/src/Microsoft.AspNetCore.NodeServices.csproj` |
| +1/-1 | `src/Features/JsonPatch/src/Microsoft.AspNetCore.JsonPatch.csproj` |
| +1/-1 | `build/tools/docker/ubuntu.18.04/Dockerfile` |
| +1/-0 | `src/Http/WebUtilities/src/Microsoft.AspNetCore.WebUtilities.csproj` |

## Security-signal hotspots (added lines matching validation / bounds / limits / unsafe vocabulary)

A ranking for where to read first, not a verdict. Confirm each one by reading the hunk in the raw diff.

- **29** `src/Shared/PooledMemoryStream.cs`
  - `throw new ArgumentNullException(nameof(bytePool));`
  - `throw new ArgumentOutOfRangeException(nameof(maxCapacity));`
  - `ThrowIfDisposed();`
- **28** `eng/Baseline.Designer.props`
  - `<BaselinePackageReference Include="System.Security.Cryptography.Xml" Version="[8.0.4, )" />`
  - `<BaselinePackageReference Include="System.Security.Cryptography.Xml" Version="[8.0.4, )" />`
  - `<BaselinePackageReference Include="System.Security.Cryptography.Xml" Version="[8.0.4, )" />`
- **6** `src/Middleware/SpaServices/src/npm/aspnet-webpack/package-lock.json`
  - `"resolved": "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public-npm/npm/registry/call-bound/-/call-bound-1.0.4.tgz",`
  - `"resolved": "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public-npm/npm/registry/escape-html/-/escape-html-1.0.3.tgz",`
  - `"resolved": "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public-npm/npm/registry/json-schema-traverse/-/json-schema-traverse-`
- **5** `src/SignalR/common/Http.Connections/src/Internal/HttpConnectionDispatcher.cs`
  - `using System.Security.Claims;`
  - `if (await RejectIfConnectionUserChangedAsync(connection, context))`
  - `if (await RejectIfConnectionUserChangedAsync(connection, context))`
- **3** `src/SignalR/common/Http.Connections/src/Internal/HttpConnectionContext.cs`
  - `lock (_userLock)`
  - `lock (_userLock)`
  - `lock (_userLock)`
- **2** `src/Servers/Kestrel/Core/src/Internal/Http/Http1Connection.cs`
  - `ThrowRequestTargetRejected(target);`
  - `ThrowRequestTargetRejected(target);`
- **1** `eng/Baseline.xml`
  - `<Package Id="Microsoft.Owin.Security.Interop" Version="2.3.13" />`
- **1** `src/Identity/UI/src/Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs`
  - `throw new InvalidOperationException("Users must have an email.");`
- **1** `src/Servers/Kestrel/Core/src/Internal/Http2/Http2Connection.cs`
  - `throw new Http2StreamErrorException(_currentHeadersStream.StreamId,`
- **1** `src/SignalR/common/Http.Connections/src/Internal/HttpConnectionDispatcher.Log.cs`
  - `LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(14, "UserNameChanged"), "Rejecting the request because the name of the us`
- **1** `src/SignalR/common/Http.Connections/src/Internal/HttpConnectionManager.cs`
  - `using System.Security.Claims;`
- **1** `src/SignalR/common/Http.Connections/src/Internal/UserIdentitySnapshot.cs`
  - `using System.Security.Claims;`
