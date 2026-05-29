namespace Skylab.Shared.Infrastructure.Auth;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string TokenUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = "openid";
}