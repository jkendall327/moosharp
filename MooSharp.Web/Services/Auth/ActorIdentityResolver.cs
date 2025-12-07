using System.Security.Claims;

namespace MooSharp.Web.Services.Auth;

public class ActorIdentityResolver
{
    /// <summary>
    /// Extracts the Game Actor GUID from the ASP.NET User Principal.
    /// Throws if the user is not authenticated or claims are missing.
    /// </summary>
    public Guid? GetActorId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub");

        if (sub is null)
        {
            return null;
        }

        return Guid.TryParse(sub.Value, out var id) ? id : null;
    }
}