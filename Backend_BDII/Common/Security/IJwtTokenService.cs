namespace Backend_BDII.Common.Security;

public interface IJwtTokenService
{
    string GenerateToken(JwtUser user);
}