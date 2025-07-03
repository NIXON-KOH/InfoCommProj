using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; 
using System.Linq; 

public class JwtHelper
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtHelper(string secret, string issuer, string audience)
    {
        _secret = secret ?? throw new ArgumentNullException(nameof(secret));
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _audience = audience ?? throw new ArgumentNullException(nameof(audience));
    }

    public string CreateToken(IEnumerable<Claim> claims, double expiresInMinutes)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var allClaims = new List<Claim>(claims);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: allClaims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }


    public string CreateTokenFromJsonClaims(string claimsJson, double expiresInMinutes)
    {
        if (string.IsNullOrEmpty(claimsJson))
        {
            claimsJson = "{}"; 
        }

        Dictionary<string, object> claimsDictionary;
        try
        {
            claimsDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(claimsJson);
            if (claimsDictionary == null)
            {
                throw new JsonException("JSON string did not deserialize to a dictionary.");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing claims JSON: {ex.Message}");
            throw new ArgumentException("Invalid JSON format for claims.", nameof(claimsJson), ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while parsing claims JSON: {ex.Message}");
            throw;
        }

        var claimsList = new List<Claim>();
        foreach (var pair in claimsDictionary)
        {

            string claimValue = pair.Value?.ToString() ?? string.Empty;
            claimsList.Add(new Claim(pair.Key, claimValue));
        }


        claimsList.RemoveAll(c => c.Type == JwtRegisteredClaimNames.Iss);
        claimsList.Add(new Claim(JwtRegisteredClaimNames.Iss, _issuer));

        claimsList.RemoveAll(c => c.Type == JwtRegisteredClaimNames.Aud); 
        claimsList.Add(new Claim(JwtRegisteredClaimNames.Aud, _audience));

        claimsList.RemoveAll(c => c.Type == JwtRegisteredClaimNames.Exp);
        claimsList.RemoveAll(c => c.Type == JwtRegisteredClaimNames.Nbf);


        var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer, 
            audience: _audience, 
            claims: claimsList, 
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes), 
            signingCredentials: credentials);


        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secret);

        var validationParameters = new TokenValidationParameters
        {

            ValidateIssuerSigningKey = true, 
            IssuerSigningKey = new SymmetricSecurityKey(key), 

            
            ValidateIssuer = true,     
            ValidIssuer = _issuer,     

            ValidateAudience = true,   
            ValidAudience = _audience,

            ValidateLifetime = true,    
            ClockSkew = TimeSpan.Zero  
        };

        try
        {

            SecurityToken validatedToken; 
            var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
          
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            Console.WriteLine($"Token validation failed: {ex.Message}");
            throw; 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred during token validation: {ex.Message}");
            throw;
        }
    }

    public JwtSecurityToken DecodeToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token cannofat be null or empty.", nameof(token));
        }
        var tokenHandler = new JwtSecurityTokenHandler();

        if (!tokenHandler.CanReadToken(token))
        {
            throw new ArgumentException("The token is not in a valid JWT format.");
        }

        var jwtToken = tokenHandler.ReadJwtToken(token);
        return jwtToken; 
    }
}