// Lab.Target - a deliberately "normal" ASP.NET Core app built only from in-box defaults.
//
// It is NOT intentionally vulnerable. Any bypass, desync or crash the probes find here is
// behaviour of ASP.NET Core / Kestrel itself with default settings, which is exactly what
// the .NET bounty pays for ("vulnerabilities which disable ... built in mitigation" are out
// of scope, so nothing here weakens a default).
//
// Listeners (loopback only):
//   :5101  HTTP/1.1            raw request parsing, smuggling, chunked bodies
//   :5102  HTTP/2 (h2c)        frame/HPACK handling, stream lifecycle
//   :5103  HTTP/1.1 + HTTP/2   app surface: auth, antiforgery, SignalR, static files

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lab.Common;
using Lab.Target;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

LabEnvironment.PrintBanner("Lab.Target (ASP.NET Core defaults)");

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

int Port(string name, int fallback) => builder.Configuration.GetValue($"Ports:{name}", fallback);
var h1Port = Port("Http1", 5101);
var h2Port = Port("Http2", 5102);
var appPort = Port("App", 5103);

builder.WebHost.ConfigureKestrel(k =>
{
    k.Listen(System.Net.IPAddress.Loopback, h1Port, o => o.Protocols = HttpProtocols.Http1);
    k.Listen(System.Net.IPAddress.Loopback, h2Port, o => o.Protocols = HttpProtocols.Http2);
    k.Listen(System.Net.IPAddress.Loopback, appPort, o => o.Protocols = HttpProtocols.Http1AndHttp2);
});

// A per-process signing key: nothing secret is committed, and tokens die with the process.
var signingKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = "lab-key-1" };
const string Issuer = "bounty-lab";
const string Audience = "bounty-lab-api";

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    })
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = signingKey,
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("admin", p => p.RequireRole("admin"))
    .AddPolicy("api", p => p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser())
    .AddPolicy("api-admin", p => p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireRole("admin"));

builder.Services.AddAntiforgery();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ---------------------------------------------------------------- raw protocol surface
app.Map("/echo/{**rest}", Echo.HandleAsync);
app.Map("/echo", Echo.HandleAsync);
app.MapGet("/health", (bool? gc) =>
{
    if (gc == true) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    using var proc = System.Diagnostics.Process.GetCurrentProcess();
    return Results.Ok(new
    {
        runtime = Environment.Version.ToString(),
        aspnetcore = LabEnvironment.AspNetCoreVersion,
        workingSetBytes = proc.WorkingSet64,
        gcHeapBytes = GC.GetTotalMemory(false),
        threads = proc.Threads.Count,
    });
});

// ---------------------------------------------------------------- authN / authZ surface
app.MapPost("/login", async (HttpContext ctx, string user) =>
{
    // Lab login: any name, never admin. A probe that reaches /admin has found a bypass.
    var id = new ClaimsIdentity([new Claim(ClaimTypes.Name, user), new Claim(ClaimTypes.Role, "user")],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(new ClaimsPrincipal(id));
    return Results.Ok(new { user, role = "user" });
}).DisableAntiforgery();

app.MapGet("/admin", () => Results.Text("ADMIN-SECRET-ROOT")).RequireAuthorization("admin");
app.MapGet("/admin/{**path}", (string? path) => Results.Text($"ADMIN-SECRET:{path}")).RequireAuthorization("admin");

app.MapPost("/api/token", (string user) =>
{
    // Issues a valid, NON-admin token. Probes mutate it to look for validation gaps.
    var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Issuer,
        Audience = Audience,
        Subject = new ClaimsIdentity([new Claim("sub", user), new Claim("role", "user")]),
        Expires = DateTime.UtcNow.AddMinutes(10),
        SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
    });
    return Results.Text(token);
}).DisableAntiforgery();

app.MapGet("/api/me", (ClaimsPrincipal u) => Results.Ok(new
{
    sub = u.FindFirstValue("sub"),
    roles = u.FindAll("role").Select(c => c.Value),
})).RequireAuthorization("api");

app.MapGet("/api/admin", () => Results.Text("API-ADMIN-SECRET")).RequireAuthorization("api-admin");

// Antiforgery: a form-bound POST must carry a valid token.
app.MapGet("/form", (HttpContext ctx, IAntiforgery af) =>
{
    var t = af.GetAndStoreTokens(ctx);
    return Results.Ok(new { field = t.FormFieldName, token = t.RequestToken });
});
app.MapPost("/form", ([Microsoft.AspNetCore.Mvc.FromForm] string comment) => Results.Text($"ACCEPTED:{comment}"));

// ---------------------------------------------------------------- realtime surface
app.MapHub<LabHub>("/hub");

app.Run();
