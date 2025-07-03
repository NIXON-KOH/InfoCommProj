using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;


namespace TryMeBitch.Models
{
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

        // Existing method to create token from IEnumerable<Claim> (as shown previously)
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

        /// <summary>
        /// Creates (Encodes) a new JWT using claims provided as a JSON string.
        /// </summary>
        /// <param name="claimsJson">A JSON string representing the claims payload.</param>
        /// <param name="expiresInMinutes">Expiration time for the token in minutes.</param>
        /// <returns>The encoded JWT string.</returns>
        public string CreateTokenFromJsonClaims(string claimsJson, double expiresInMinutes)
        {
            if (string.IsNullOrEmpty(claimsJson))
            {
                claimsJson = "{}"; // Use an empty JSON object for empty claims
            }

            // --- Parse the JSON string into a dictionary ---
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
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }

            // --- Convert the dictionary to a List<Claim> ---
            var claimsList = new List<Claim>();
            foreach (var pair in claimsDictionary)
            {
                // Important: Claim values are strings. Convert the object value to string.
                // Handle potential nulls and complex JSON types (objects, arrays) if necessary.
                // For simple values (string, number, bool), ToString() often suffices.
                string claimValue = pair.Value?.ToString() ?? string.Empty;
                claimsList.Add(new Claim(pair.Key, claimValue));
            }

            // --- Add standard claims explicitly if not already present or to ensure correct values ---
            // Ensure standard claims from helper parameters overwrite or are added
            claimsList.RemoveAll(c => c.Type == JwtRegisteredClaimNames.Iss); // Remove if already present
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Iss, _issuer));

            claimsList.RemoveAll(c => c.Type == JwtRegisteredClaimNames.Aud); // Remove if already present
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Aud, _audience));

            // 'exp' and 'nbf' will be handled by the JwtSecurityToken constructor parameters below
            claimsList.RemoveAll(c => c.Type == JwtRegisteredClaimNames.Exp);
            claimsList.RemoveAll(c => c.Type == JwtRegisteredClaimNames.Nbf);


            // --- Create the Token using the List<Claim> ---
            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Use the constructor that takes IEnumerable<Claim>
            var token = new JwtSecurityToken(
                issuer: _issuer, // This sets the 'iss' claim
                audience: _audience, // This sets the 'aud' claim
                claims: claimsList, // Pass the claims list
                notBefore: DateTime.UtcNow, // This sets the 'nbf' claim
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes), // This sets the 'exp' claim
                signingCredentials: credentials);


            // --- Write the Token ---
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
            // Convert the secret string to a byte array to create the security key
            var key = Encoding.ASCII.GetBytes(_secret);

            var validationParameters = new TokenValidationParameters
            {
                // This is the crucial step where the secret key is used
                // to verify the token's signature.
                ValidateIssuerSigningKey = true, // Ensure the token is signed
                IssuerSigningKey = new SymmetricSecurityKey(key), // Provide the secret key for verification

                // These checks use claims within the token payload against expected values
                ValidateIssuer = true,      // Validate the 'iss' claim
                ValidIssuer = _issuer,      // Expected issuer value

                ValidateAudience = true,    // Validate the 'aud' claim
                ValidAudience = _audience,  // Expected audience value

                ValidateLifetime = true,    // Validate the 'exp' claim (expiration)
                ClockSkew = TimeSpan.Zero   // Default is 5 minutes, zero means no allowance for clock differences. Adjust if necessary.
            };

            try
            {
                // ValidateToken performs both decoding AND verification (signature, claims, lifetime).
                // If the token is valid, it returns a ClaimsPrincipal.
                // If validation fails (wrong signature, expired, incorrect issuer/audience, etc.),
                // it throws a SecurityTokenException or one of its derived types.
                SecurityToken validatedToken; // This will hold the validated token object
                var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                // The 'principal' object now contains the verified claims from the token.
                return principal;
            }
            catch (SecurityTokenException ex)
            {
                // Handle specific token validation errors

                throw; // Re-throw or handle as needed in your application logic
            }
            catch (Exception ex)
            {
                // Handle other potential errors
                
                throw;
            }
        }

        /// <summary>
        /// Decodes a JWT without verifying the signature or claims.
        /// This method only parses the token's structure to allow inspecting header and payload.
        /// It DOES NOT use the secret key.
        /// DO NOT use the result of this method for authentication or authorization checks.
        /// </summary>
        /// <param name="token">The JWT string to decode.</param>
        /// <returns>The decoded JwtSecurityToken object.</returns>
        public JwtSecurityToken DecodeToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token cannofat be null or empty.", nameof(token));
            }
            var tokenHandler = new JwtSecurityTokenHandler();

            // Check if the token string is in a readable JWT format (header.payload.signature)
            if (!tokenHandler.CanReadToken(token))
            {
                throw new ArgumentException("The token is not in a valid JWT format.");
            }

            // ReadJwtToken parses the token but skips signature validation.
            // The secret key is NOT used here.
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken; // Returns the token object with accessible Header and Payload
        }
    }
}
