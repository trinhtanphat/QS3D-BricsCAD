using System.Security.Claims;
using Microsoft.AspNetCore.Http;

var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
Require(await Status(ApiAuthorization.Authorize(anonymous, "quantity:read")) == 401, "anonymous request must be 401");

var identity = new ClaimsIdentity(new[] {
    new Claim(ClaimTypes.NameIdentifier, "power-bi"),
    new Claim("scope", "project:read quantity:read"),
    new Claim("tenant_id", "tenant-a") }, "smoke");
var principal = new ClaimsPrincipal(identity);
Require(ApiAuthorization.Authorize(principal, "quantity:read") is null, "granted scope rejected");
Require(await Status(ApiAuthorization.Authorize(principal, "procurement:read")) == 403, "missing scope must be 403");
var context = ApiAuthorization.Context(principal);
Require(context.Subject == "power-bi" && context.TenantId == "tenant-a", "subject/tenant lost");
Require(context.Scopes.SequenceEqual(new[] { "project:read", "quantity:read" }), "scopes must be deterministic");
Console.WriteLine("QS3D Integration API smoke PASS");

static async Task<int> Status(IResult? result)
{
    if (result is null) return 200;
    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();
    await result.ExecuteAsync(context);
    return context.Response.StatusCode;
}
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
