using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    internal sealed class McpOAuthHttpResponse
    {
        internal int StatusCode;
        internal string Reason = string.Empty;
        internal string Body = string.Empty;
        internal string ContentType = "application/json; charset=utf-8";
        internal readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Small embedded OAuth 2.1 authorization server for the single-user QS3D desktop MCP.
    /// It is deliberately limited to ChatGPT public clients, authorization-code + PKCE S256,
    /// exact resource binding and explicit local BricsCAD consent.
    /// </summary>
    internal static class McpOAuthAuthorizationServer
    {
        internal const string RequiredScope = "qs3d:mcp";
        internal const string TokenEndpointAuthMethod = "none";
        internal const string AuthorizationCodeGrant = "authorization_code";
        internal const string RefreshTokenGrant = "refresh_token";
        internal const string ChatGptCallbackPrefix = "https://chatgpt.com/connector/oauth/";

        internal static readonly TimeSpan AuthorizationCodeLifetime = TimeSpan.FromMinutes(5);
        internal static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromHours(1);
        internal static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
        internal static readonly TimeSpan ClientRegistrationLifetime = TimeSpan.FromDays(3650);

        private const int MaxFormBytes = 32 * 1024;
        private const int MaxParameterCount = 32;
        private const int MaxParameterLength = 8192;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly ConcurrentDictionary<string, long> ConsumedAuthorizationCodes =
            new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, long> ConsumedRefreshTokens =
            new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private static readonly string ProcessNonce = CreateRandomToken(24);

        internal static bool TryHandle(
            string method,
            string path,
            string query,
            IDictionary<string, string> headers,
            string body,
            string publicMcpUrl,
            string signingSecret,
            out McpOAuthHttpResponse response)
        {
            response = null!;
            string resource;
            Uri resourceUri;
            if (!ValidatePublicMcpResource(publicMcpUrl, out resource, out resourceUri)) return false;
            if (string.IsNullOrWhiteSpace(signingSecret)) return false;

            var issuer = resourceUri.GetLeftPart(UriPartial.Authority);
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(path, "/.well-known/oauth-protected-resource", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(path, "/.well-known/oauth-protected-resource/mcp", StringComparison.OrdinalIgnoreCase)))
            {
                response = Json(200, "OK",
                    "{\"resource\":\"" + JsonEscape(resource) + "\","
                    + "\"authorization_servers\":[\"" + JsonEscape(issuer) + "\"],"
                    + "\"scopes_supported\":[\"" + RequiredScope + "\"]}");
                return true;
            }

            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                && string.Equals(path, "/.well-known/oauth-authorization-server", StringComparison.OrdinalIgnoreCase))
            {
                response = Json(200, "OK",
                    "{\"issuer\":\"" + JsonEscape(issuer) + "\","
                    + "\"authorization_endpoint\":\"" + JsonEscape(issuer + "/oauth/authorize") + "\","
                    + "\"token_endpoint\":\"" + JsonEscape(issuer + "/oauth/token") + "\","
                    + "\"registration_endpoint\":\"" + JsonEscape(issuer + "/oauth/register") + "\","
                    + "\"response_types_supported\":[\"code\"],"
                    + "\"grant_types_supported\":[\"" + AuthorizationCodeGrant + "\",\"" + RefreshTokenGrant + "\"],"
                    + "\"token_endpoint_auth_methods_supported\":[\"" + TokenEndpointAuthMethod + "\"],"
                    + "\"code_challenge_methods_supported\":[\"S256\"],"
                    + "\"scopes_supported\":[\"" + RequiredScope + "\",\"" + OfflineAccessScope + "\"]}");
                return true;
            }

            if (string.Equals(path, "/oauth/register", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    response = OAuthError(405, "Method Not Allowed", "invalid_request", "registration requires POST");
                    response.Headers["Allow"] = "POST";
                    return true;
                }
                if (!IsJsonContentType(headers))
                {
                    response = OAuthError(415, "Unsupported Media Type", "invalid_client_metadata", "registration requires application/json");
                    return true;
                }
                response = RegisterClient(body, resource, signingSecret);
                return true;
            }

            if (string.Equals(path, "/oauth/authorize", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    response = OAuthError(405, "Method Not Allowed", "invalid_request", "authorization requires GET");
                    response.Headers["Allow"] = "GET";
                    return true;
                }
                response = Authorize(query, resource, signingSecret);
                return true;
            }

            if (string.Equals(path, "/oauth/token", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    response = OAuthError(405, "Method Not Allowed", "invalid_request", "token endpoint requires POST");
                    response.Headers["Allow"] = "POST";
                    return true;
                }
                if (!IsFormContentType(headers))
                {
                    response = OAuthError(415, "Unsupported Media Type", "invalid_request", "token endpoint requires application/x-www-form-urlencoded");
                    return true;
                }
                response = ExchangeToken(body, resource, signingSecret);
                return true;
            }

            return false;
        }

        // offline_access is an authorization-server grant hint, not an MCP resource permission.
        // Keep this declaration after protected-resource metadata so source guards can prove the
        // protected resource advertises only the actual qs3d:mcp permission.
        internal const string OfflineAccessScope = "offline_access";

        internal static bool TryValidateAccessToken(
            IDictionary<string, string> headers,
            string publicMcpUrl,
            string signingSecret)
        {
            string authorization;
            if (headers == null || !headers.TryGetValue("Authorization", out authorization)) return false;
            const string prefix = "Bearer ";
            if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            var token = authorization.Substring(prefix.Length).Trim();
            if (token.Length == 0) return false;

            string resource;
            Uri ignored;
            if (!ValidatePublicMcpResource(publicMcpUrl, out resource, out ignored)) return false;
            string[] fields;
            long expiry;
            if (!TryReadSignedToken(token, "access", signingSecret, out fields, out expiry)) return false;
            if (fields.Length != 6) return false;
            string clientId;
            string tokenResource;
            string scope;
            if (!TryDecodeField(fields[3], out clientId)
                || !TryDecodeField(fields[4], out tokenResource)
                || !TryDecodeField(fields[5], out scope)) return false;
            if (string.IsNullOrWhiteSpace(clientId)) return false;
            return ConstantTimeEquals(tokenResource, resource)
                   && ConstantTimeEquals(scope, RequiredScope)
                   && IsValidClient(clientId, resource, signingSecret, null);
        }

        internal static string BuildBearerChallenge(string publicMcpUrl)
        {
            string resource;
            Uri uri;
            if (!ValidatePublicMcpResource(publicMcpUrl, out resource, out uri))
                return "Bearer scope=\"" + RequiredScope + "\"";
            var metadata = uri.GetLeftPart(UriPartial.Authority) + "/.well-known/oauth-protected-resource/mcp";
            return "Bearer resource_metadata=\"" + metadata + "\", scope=\"" + RequiredScope + "\"";
        }

        private static McpOAuthHttpResponse RegisterClient(string body, string resource, string signingSecret)
        {
            if (body == null || Encoding.UTF8.GetByteCount(body) > MaxFormBytes)
                return OAuthError(400, "Bad Request", "invalid_client_metadata", "registration body exceeds bounds");

            string rawRedirects;
            bool found;
            string error;
            if (!McpTopLevelJson.TryFindPropertyValue(body, "redirect_uris", out rawRedirects, out found, out error))
                return OAuthError(400, "Bad Request", "invalid_client_metadata", error);
            if (!found) return OAuthError(400, "Bad Request", "invalid_client_metadata", "redirect_uris is required");

            List<string> redirects;
            if (!TryParseJsonStringArray(rawRedirects, 4, 2048, "redirect_uris", out redirects, out error) || redirects.Count != 1)
                return OAuthError(400, "Bad Request", "invalid_redirect_uri", string.IsNullOrWhiteSpace(error) ? "exactly one redirect URI is required" : error);
            var redirect = redirects[0];
            if (!IsAllowedChatGptRedirect(redirect))
                return OAuthError(400, "Bad Request", "invalid_redirect_uri", "redirect URI is not an allowed ChatGPT connector callback");

            var requestedAuthMethod = McpTopLevelJson.ExtractString(body, "token_endpoint_auth_method");
            if (!string.IsNullOrWhiteSpace(requestedAuthMethod)
                && !string.Equals(requestedAuthMethod, TokenEndpointAuthMethod, StringComparison.Ordinal))
                return OAuthError(400, "Bad Request", "invalid_client_metadata", "only public OAuth clients are supported");

            string rawGrantTypes;
            if (!McpTopLevelJson.TryFindPropertyValue(body, "grant_types", out rawGrantTypes, out found, out error))
                return OAuthError(400, "Bad Request", "invalid_client_metadata", error);
            if (found)
            {
                List<string> grantTypes;
                if (!TryParseJsonStringArray(rawGrantTypes, 4, 64, "grant_types", out grantTypes, out error))
                    return OAuthError(400, "Bad Request", "invalid_client_metadata", error);
                if (grantTypes.Count == 0 || !ContainsOrdinal(grantTypes, AuthorizationCodeGrant))
                    return OAuthError(400, "Bad Request", "invalid_client_metadata", "grant_types must include authorization_code");
                foreach (var grantType in grantTypes)
                {
                    if (!string.Equals(grantType, AuthorizationCodeGrant, StringComparison.Ordinal)
                        && !string.Equals(grantType, RefreshTokenGrant, StringComparison.Ordinal))
                        return OAuthError(400, "Bad Request", "invalid_client_metadata", "grant_types contains an unsupported grant");
                }
                if (HasDuplicateOrdinal(grantTypes))
                    return OAuthError(400, "Bad Request", "invalid_client_metadata", "grant_types contains duplicate values");
            }

            string rawResponseTypes;
            if (!McpTopLevelJson.TryFindPropertyValue(body, "response_types", out rawResponseTypes, out found, out error))
                return OAuthError(400, "Bad Request", "invalid_client_metadata", error);
            if (found)
            {
                List<string> responseTypes;
                if (!TryParseJsonStringArray(rawResponseTypes, 4, 64, "response_types", out responseTypes, out error))
                    return OAuthError(400, "Bad Request", "invalid_client_metadata", error);
                if (responseTypes.Count != 1 || !string.Equals(responseTypes[0], "code", StringComparison.Ordinal))
                    return OAuthError(400, "Bad Request", "invalid_client_metadata", "response_types must contain only code");
            }

            var expires = UnixNow() + (long)ClientRegistrationLifetime.TotalSeconds;
            var clientId = CreateSignedToken(
                new[] { "v1", "client", expires.ToString(CultureInfo.InvariantCulture), EncodeField(resource), EncodeField(redirect) },
                signingSecret);
            return Json(201, "Created",
                "{\"client_id\":\"" + JsonEscape(clientId) + "\","
                + "\"client_id_issued_at\":" + UnixNow().ToString(CultureInfo.InvariantCulture) + ","
                + "\"client_secret_expires_at\":0,"
                + "\"redirect_uris\":[\"" + JsonEscape(redirect) + "\"],"
                + "\"token_endpoint_auth_method\":\"" + TokenEndpointAuthMethod + "\","
                + "\"grant_types\":[\"" + AuthorizationCodeGrant + "\",\"" + RefreshTokenGrant + "\"],"
                + "\"response_types\":[\"code\"]}");
        }

        private static McpOAuthHttpResponse Authorize(string query, string resource, string signingSecret)
        {
            Dictionary<string, string> values;
            string error;
            if (!ParseFormEncoded(query, out values, out error))
                return OAuthError(400, "Bad Request", "invalid_request", error);

            string clientId;
            string redirect;
            string responseType;
            string requestedResource;
            string scope;
            string challenge;
            string challengeMethod;
            if (!Required(values, "client_id", out clientId)
                || !Required(values, "redirect_uri", out redirect)
                || !Required(values, "response_type", out responseType)
                || !Required(values, "resource", out requestedResource)
                || !Required(values, "scope", out scope)
                || !Required(values, "code_challenge", out challenge)
                || !Required(values, "code_challenge_method", out challengeMethod))
                return OAuthError(400, "Bad Request", "invalid_request", "authorization request is missing a required parameter");

            if (!IsValidClient(clientId, resource, signingSecret, redirect))
                return OAuthError(400, "Bad Request", "invalid_request", "client registration is invalid for this resource");
            if (!string.Equals(responseType, "code", StringComparison.Ordinal))
                return RedirectOAuthError(redirect, values, "unsupported_response_type", "only authorization code is supported");
            if (!ConstantTimeEquals(requestedResource, resource))
                return RedirectOAuthError(redirect, values, "invalid_target", "resource does not match the active QS3D MCP endpoint");
            string normalizedScope;
            if (!TryNormalizeAuthorizationScope(scope, out normalizedScope))
                return RedirectOAuthError(redirect, values, "invalid_scope", "requested scope is not supported");
            if (!string.Equals(challengeMethod, "S256", StringComparison.Ordinal)
                || !IsValidPkceChallenge(challenge))
                return RedirectOAuthError(redirect, values, "invalid_request", "PKCE S256 is required");

            var consent = McpOAuthConsent.RequestApproval(resource, normalizedScope);
            if (consent == McpOAuthConsentResult.Denied)
                return RedirectOAuthError(redirect, values, "access_denied", "local QS3D authorization was denied");
            if (consent != McpOAuthConsentResult.Approved)
                return RedirectOAuthError(redirect, values, "temporarily_unavailable", "local QS3D authorization is unavailable");

            var expires = UnixNow() + (long)AuthorizationCodeLifetime.TotalSeconds;
            var code = CreateSignedToken(
                new[]
                {
                    "v1", "code", expires.ToString(CultureInfo.InvariantCulture), EncodeField(ProcessNonce),
                    EncodeField(clientId), EncodeField(resource), EncodeField(redirect), EncodeField(normalizedScope), EncodeField(challenge)
                },
                signingSecret);
            var location = redirect + "?code=" + Uri.EscapeDataString(code);
            string state;
            if (values.TryGetValue("state", out state)) location += "&state=" + Uri.EscapeDataString(state);
            return Redirect(location);
        }

        private static McpOAuthHttpResponse ExchangeToken(string body, string resource, string signingSecret)
        {
            Dictionary<string, string> values;
            string error;
            if (!ParseFormEncoded(body, out values, out error))
                return OAuthError(400, "Bad Request", "invalid_request", error);

            string grantType;
            string clientId;
            string requestedResource;
            if (!Required(values, "grant_type", out grantType)
                || !Required(values, "client_id", out clientId)
                || !Required(values, "resource", out requestedResource))
                return OAuthError(400, "Bad Request", "invalid_request", "token request is missing a required parameter");
            if (!ConstantTimeEquals(requestedResource, resource))
                return OAuthError(400, "Bad Request", "invalid_target", "resource does not match the active QS3D MCP endpoint");
            if (!IsValidClient(clientId, resource, signingSecret, null))
                return OAuthError(400, "Bad Request", "invalid_client", "public client registration is invalid");

            if (string.Equals(grantType, AuthorizationCodeGrant, StringComparison.Ordinal))
                return ExchangeAuthorizationCode(values, clientId, resource, signingSecret);
            if (string.Equals(grantType, RefreshTokenGrant, StringComparison.Ordinal))
                return ExchangeRefreshToken(values, clientId, resource, signingSecret);
            return OAuthError(400, "Bad Request", "unsupported_grant_type", "grant type is not supported");
        }

        private static McpOAuthHttpResponse ExchangeAuthorizationCode(
            IDictionary<string, string> values,
            string clientId,
            string resource,
            string signingSecret)
        {
            string code;
            string verifier;
            string redirect;
            if (!Required(values, "code", out code)
                || !Required(values, "code_verifier", out verifier)
                || !Required(values, "redirect_uri", out redirect))
                return OAuthError(400, "Bad Request", "invalid_grant", "authorization code exchange is incomplete");
            if (!ValidatePkceVerifier(verifier))
                return OAuthError(400, "Bad Request", "invalid_grant", "PKCE verifier is invalid");

            string[] fields;
            long expiry;
            if (!TryReadSignedToken(code, "code", signingSecret, out fields, out expiry) || fields.Length != 9)
                return OAuthError(400, "Bad Request", "invalid_grant", "authorization code is invalid or expired");
            string processNonce;
            string codeClient;
            string codeResource;
            string codeRedirect;
            string codeScope;
            string challenge;
            if (!TryDecodeField(fields[3], out processNonce)
                || !TryDecodeField(fields[4], out codeClient)
                || !TryDecodeField(fields[5], out codeResource)
                || !TryDecodeField(fields[6], out codeRedirect)
                || !TryDecodeField(fields[7], out codeScope)
                || !TryDecodeField(fields[8], out challenge))
                return OAuthError(400, "Bad Request", "invalid_grant", "authorization code payload is invalid");
            string normalizedCodeScope;
            if (!TryNormalizeAuthorizationScope(codeScope, out normalizedCodeScope)
                || !ConstantTimeEquals(codeScope, normalizedCodeScope)
                || !ConstantTimeEquals(processNonce, ProcessNonce)
                || !ConstantTimeEquals(codeClient, clientId)
                || !ConstantTimeEquals(codeResource, resource)
                || !ConstantTimeEquals(codeRedirect, redirect)
                || !ConstantTimeEquals(ComputePkceChallenge(verifier), challenge))
                return OAuthError(400, "Bad Request", "invalid_grant", "authorization code binding check failed");

            CleanupConsumedCodes();
            if (!ConsumedAuthorizationCodes.TryAdd(HashForCache(code), expiry))
                return OAuthError(400, "Bad Request", "invalid_grant", "authorization code was already used");
            var includeRefreshToken = HasOfflineAccess(normalizedCodeScope);
            return IssueTokenPair(clientId, resource, signingSecret, normalizedCodeScope, includeRefreshToken);
        }

        private static McpOAuthHttpResponse ExchangeRefreshToken(
            IDictionary<string, string> values,
            string clientId,
            string resource,
            string signingSecret)
        {
            string refresh;
            if (!Required(values, "refresh_token", out refresh))
                return OAuthError(400, "Bad Request", "invalid_grant", "refresh token is required");
            string[] fields;
            long expiry;
            if (!TryReadSignedToken(refresh, "refresh", signingSecret, out fields, out expiry) || fields.Length != 7)
                return OAuthError(400, "Bad Request", "invalid_grant", "refresh token is invalid or expired");
            string processNonce;
            string tokenClient;
            string tokenResource;
            string tokenScope;
            if (!TryDecodeField(fields[3], out processNonce)
                || !TryDecodeField(fields[4], out tokenClient)
                || !TryDecodeField(fields[5], out tokenResource)
                || !TryDecodeField(fields[6], out tokenScope))
                return OAuthError(400, "Bad Request", "invalid_grant", "refresh token binding check failed");
            string normalizedTokenScope;
            if (!TryNormalizeAuthorizationScope(tokenScope, out normalizedTokenScope)
                || !ConstantTimeEquals(tokenScope, normalizedTokenScope)
                || !HasOfflineAccess(normalizedTokenScope)
                || !ConstantTimeEquals(processNonce, ProcessNonce)
                || !ConstantTimeEquals(tokenClient, clientId)
                || !ConstantTimeEquals(tokenResource, resource))
                return OAuthError(400, "Bad Request", "invalid_grant", "refresh token binding check failed");

            var grantedScope = normalizedTokenScope;
            string requestedScope;
            if (values.TryGetValue("scope", out requestedScope))
            {
                string normalizedRequestedScope;
                if (!TryNormalizeAuthorizationScope(requestedScope, out normalizedRequestedScope)
                    || (HasOfflineAccess(normalizedRequestedScope) && !HasOfflineAccess(normalizedTokenScope)))
                    return OAuthError(400, "Bad Request", "invalid_scope", "requested refresh scope exceeds the original grant");
                grantedScope = normalizedRequestedScope;
            }

            CleanupConsumedRefreshTokens();
            if (!ConsumedRefreshTokens.TryAdd(HashForCache(refresh), expiry))
                return OAuthError(400, "Bad Request", "invalid_grant", "refresh token was already used");
            var includeRefreshToken = HasOfflineAccess(grantedScope);
            return IssueTokenPair(clientId, resource, signingSecret, grantedScope, includeRefreshToken);
        }

        private static McpOAuthHttpResponse IssueTokenPair(
            string clientId,
            string resource,
            string signingSecret,
            string grantedScope,
            bool includeRefreshToken)
        {
            string normalizedScope;
            if (!TryNormalizeAuthorizationScope(grantedScope, out normalizedScope)
                || !ConstantTimeEquals(grantedScope, normalizedScope))
                return OAuthError(400, "Bad Request", "invalid_scope", "granted OAuth scope is invalid");

            var now = UnixNow();
            var accessExpiry = now + (long)AccessTokenLifetime.TotalSeconds;
            var access = CreateSignedToken(
                new[] { "v1", "access", accessExpiry.ToString(CultureInfo.InvariantCulture), EncodeField(clientId), EncodeField(resource), EncodeField(RequiredScope) },
                signingSecret);
            var body = "{\"access_token\":\"" + JsonEscape(access) + "\","
                       + "\"token_type\":\"Bearer\","
                       + "\"expires_in\":" + ((long)AccessTokenLifetime.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            if (includeRefreshToken)
            {
                var refreshExpiry = now + (long)RefreshTokenLifetime.TotalSeconds;
                var refresh = CreateSignedToken(
                    new[] { "v1", "refresh", refreshExpiry.ToString(CultureInfo.InvariantCulture), EncodeField(ProcessNonce), EncodeField(clientId), EncodeField(resource), EncodeField(normalizedScope) },
                    signingSecret);
                body += ",\"refresh_token\":\"" + JsonEscape(refresh) + "\"";
            }
            body += ",\"scope\":\"" + JsonEscape(normalizedScope) + "\"}";
            var response = Json(200, "OK", body);
            response.Headers["Cache-Control"] = "no-store";
            response.Headers["Pragma"] = "no-cache";
            return response;
        }

        private static bool IsValidClient(string clientId, string resource, string signingSecret, string? expectedRedirect)
        {
            string[] fields;
            long ignored;
            if (!TryReadSignedToken(clientId, "client", signingSecret, out fields, out ignored) || fields.Length != 5) return false;
            string tokenResource;
            string redirect;
            if (!TryDecodeField(fields[3], out tokenResource) || !TryDecodeField(fields[4], out redirect)) return false;
            if (!ConstantTimeEquals(tokenResource, resource) || !IsAllowedChatGptRedirect(redirect)) return false;
            return expectedRedirect == null || ConstantTimeEquals(redirect, expectedRedirect);
        }

        internal static bool IsAllowedChatGptRedirect(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(ChatGptCallbackPrefix, StringComparison.Ordinal)) return false;
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri)) return false;
            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(uri.Host, "chatgpt.com", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment)
                || !uri.IsDefaultPort) return false;
            var suffix = value.Substring(ChatGptCallbackPrefix.Length);
            if (suffix.Length < 6 || suffix.Length > 128 || suffix.IndexOf('/') >= 0) return false;
            foreach (var ch in suffix)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_')
                    continue;
                return false;
            }
            return true;
        }

        internal static bool ValidatePublicMcpResource(string value, out string canonical, out Uri uri)
        {
            canonical = string.Empty;
            uri = null!;
            Uri parsed;
            if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out parsed)) return false;
            if (!string.Equals(parsed.Scheme, "https", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(parsed.Host)
                || !string.IsNullOrEmpty(parsed.UserInfo)
                || !string.IsNullOrEmpty(parsed.Query)
                || !string.IsNullOrEmpty(parsed.Fragment)
                || !string.Equals(parsed.AbsolutePath, "/mcp", StringComparison.Ordinal)) return false;
            canonical = parsed.GetLeftPart(UriPartial.Authority) + "/mcp";
            uri = new Uri(canonical, UriKind.Absolute);
            return true;
        }

        internal static bool ValidatePkceVerifier(string verifier)
        {
            if (string.IsNullOrEmpty(verifier) || verifier.Length < 43 || verifier.Length > 128) return false;
            foreach (var ch in verifier)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')
                    || ch == '-' || ch == '.' || ch == '_' || ch == '~') continue;
                return false;
            }
            return true;
        }

        internal static string ComputePkceChallenge(string verifier)
        {
            using (var sha = SHA256.Create())
                return Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier ?? string.Empty)));
        }

        private static bool IsValidPkceChallenge(string challenge)
        {
            if (string.IsNullOrEmpty(challenge) || challenge.Length != 43) return false;
            foreach (var ch in challenge)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_') continue;
                return false;
            }
            return true;
        }

        internal static bool ParseFormEncoded(string encoded, out Dictionary<string, string> values, out string error)
        {
            values = new Dictionary<string, string>(StringComparer.Ordinal);
            error = string.Empty;
            encoded = encoded ?? string.Empty;
            if (Encoding.UTF8.GetByteCount(encoded) > MaxFormBytes)
            {
                error = "OAuth parameters exceed configured bounds.";
                return false;
            }
            if (encoded.Length == 0) return true;
            var parts = encoded.Split('&');
            if (parts.Length > MaxParameterCount)
            {
                error = "OAuth parameter count exceeds configured bounds.";
                return false;
            }
            foreach (var part in parts)
            {
                if (part.Length == 0) continue;
                var equals = part.IndexOf('=');
                var rawName = equals < 0 ? part : part.Substring(0, equals);
                var rawValue = equals < 0 ? string.Empty : part.Substring(equals + 1);
                string name;
                string value;
                if (!TryDecodeFormComponent(rawName, out name) || !TryDecodeFormComponent(rawValue, out value))
                {
                    error = "OAuth parameter encoding is invalid.";
                    return false;
                }
                if (name.Length == 0 || name.Length > 128 || value.Length > MaxParameterLength)
                {
                    error = "OAuth parameter exceeds configured bounds.";
                    return false;
                }
                if (values.ContainsKey(name))
                {
                    error = "duplicate OAuth parameter: " + name;
                    return false;
                }
                values[name] = value;
            }
            return true;
        }

        private static bool TryDecodeFormComponent(string input, out string decoded)
        {
            decoded = string.Empty;
            try
            {
                var bytes = new List<byte>(input.Length);
                for (var i = 0; i < input.Length; i++)
                {
                    var ch = input[i];
                    if (ch == '+') { bytes.Add((byte)' '); continue; }
                    if (ch == '%')
                    {
                        if (i + 2 >= input.Length) return false;
                        int high = Hex(input[i + 1]);
                        int low = Hex(input[i + 2]);
                        if (high < 0 || low < 0) return false;
                        bytes.Add((byte)((high << 4) | low));
                        i += 2;
                        continue;
                    }
                    if (ch > 0x7f) return false;
                    bytes.Add((byte)ch);
                }
                decoded = StrictUtf8.GetString(bytes.ToArray());
                return decoded.IndexOf('\0') < 0;
            }
            catch { return false; }
        }

        private static int Hex(char ch)
        {
            if (ch >= '0' && ch <= '9') return ch - '0';
            if (ch >= 'a' && ch <= 'f') return ch - 'a' + 10;
            if (ch >= 'A' && ch <= 'F') return ch - 'A' + 10;
            return -1;
        }

        private static bool TryParseJsonStringArray(
            string raw,
            int maxItems,
            int maxItemLength,
            string fieldName,
            out List<string> values,
            out string error)
        {
            values = new List<string>();
            error = string.Empty;
            raw = (raw ?? string.Empty).Trim();
            if (raw.Length < 2 || raw[0] != '[' || raw[raw.Length - 1] != ']')
            {
                error = fieldName + " must be a JSON string array";
                return false;
            }
            var index = 1;
            while (true)
            {
                SkipJsonWhitespace(raw, ref index);
                if (index >= raw.Length - 1) return true;
                string value;
                if (!TryReadJsonString(raw, ref index, out value))
                {
                    error = fieldName + " contains an invalid string";
                    return false;
                }
                if (value.Length > maxItemLength)
                {
                    error = fieldName + " value exceeds length bound";
                    return false;
                }
                values.Add(value);
                if (values.Count > maxItems)
                {
                    error = fieldName + " contains too many entries";
                    return false;
                }
                SkipJsonWhitespace(raw, ref index);
                if (index >= raw.Length)
                {
                    error = fieldName + " array is incomplete";
                    return false;
                }
                if (raw[index] == ']')
                {
                    if (index == raw.Length - 1) return true;
                    error = fieldName + " has trailing content";
                    return false;
                }
                if (raw[index] != ',')
                {
                    error = fieldName + " requires commas between strings";
                    return false;
                }
                index++;
                SkipJsonWhitespace(raw, ref index);
                if (index >= raw.Length - 1)
                {
                    error = fieldName + " cannot end with a trailing comma";
                    return false;
                }
            }
        }

        private static bool ContainsOrdinal(IList<string> values, string expected)
        {
            foreach (var value in values)
                if (string.Equals(value, expected, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool HasDuplicateOrdinal(IList<string> values)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
                if (!seen.Add(value)) return true;
            return false;
        }

        private static bool TryReadJsonString(string raw, ref int index, out string value)
        {
            value = string.Empty;
            if (index >= raw.Length || raw[index] != '"') return false;
            index++;
            var builder = new StringBuilder();
            while (index < raw.Length)
            {
                var ch = raw[index++];
                if (ch == '"') { value = builder.ToString(); return true; }
                if (ch < 0x20) return false;
                if (ch != '\\') { builder.Append(ch); continue; }
                if (index >= raw.Length) return false;
                ch = raw[index++];
                switch (ch)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (index + 4 > raw.Length) return false;
                        int code;
                        if (!int.TryParse(raw.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)) return false;
                        builder.Append((char)code);
                        index += 4;
                        break;
                    default: return false;
                }
            }
            return false;
        }

        private static void SkipJsonWhitespace(string raw, ref int index)
        {
            while (index < raw.Length && (raw[index] == ' ' || raw[index] == '\t' || raw[index] == '\r' || raw[index] == '\n')) index++;
        }

        private static bool Required(IDictionary<string, string> values, string name, out string value)
        {
            value = string.Empty;
            return values != null && values.TryGetValue(name, out value) && !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryNormalizeAuthorizationScope(string value, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var hasRequired = false;
            var hasOffline = false;
            foreach (var part in parts)
            {
                if (string.Equals(part, RequiredScope, StringComparison.Ordinal))
                {
                    if (hasRequired) return false;
                    hasRequired = true;
                    continue;
                }
                if (string.Equals(part, OfflineAccessScope, StringComparison.Ordinal))
                {
                    if (hasOffline) return false;
                    hasOffline = true;
                    continue;
                }
                return false;
            }
            if (!hasRequired) return false;
            normalized = hasOffline ? RequiredScope + " " + OfflineAccessScope : RequiredScope;
            return true;
        }

        private static bool HasOfflineAccess(string normalizedScope)
        {
            return ConstantTimeEquals(normalizedScope, RequiredScope + " " + OfflineAccessScope);
        }

        private static bool IsJsonContentType(IDictionary<string, string> headers)
        {
            return HasMediaType(headers, "application/json");
        }

        private static bool IsFormContentType(IDictionary<string, string> headers)
        {
            return HasMediaType(headers, "application/x-www-form-urlencoded");
        }

        private static bool HasMediaType(IDictionary<string, string> headers, string expected)
        {
            string value;
            if (headers == null || !headers.TryGetValue("Content-Type", out value) || string.IsNullOrWhiteSpace(value)) return false;
            var separator = value.IndexOf(';');
            var media = (separator < 0 ? value : value.Substring(0, separator)).Trim();
            return string.Equals(media, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static McpOAuthHttpResponse RedirectOAuthError(
            string redirect,
            IDictionary<string, string> values,
            string code,
            string description)
        {
            if (!IsAllowedChatGptRedirect(redirect))
                return OAuthError(400, "Bad Request", code, description);
            var location = redirect + "?error=" + Uri.EscapeDataString(code)
                           + "&error_description=" + Uri.EscapeDataString(description);
            string state;
            if (values != null && values.TryGetValue("state", out state)) location += "&state=" + Uri.EscapeDataString(state);
            return Redirect(location);
        }

        private static McpOAuthHttpResponse Redirect(string location)
        {
            var response = new McpOAuthHttpResponse
            {
                StatusCode = 302,
                Reason = "Found",
                Body = string.Empty,
                ContentType = "text/plain; charset=utf-8",
            };
            response.Headers["Location"] = location;
            response.Headers["Cache-Control"] = "no-store";
            return response;
        }

        private static McpOAuthHttpResponse OAuthError(int status, string reason, string code, string description)
        {
            var response = Json(status, reason,
                "{\"error\":\"" + JsonEscape(code) + "\",\"error_description\":\"" + JsonEscape(description) + "\"}");
            response.Headers["Cache-Control"] = "no-store";
            response.Headers["Pragma"] = "no-cache";
            return response;
        }

        private static McpOAuthHttpResponse Json(int status, string reason, string body)
        {
            return new McpOAuthHttpResponse
            {
                StatusCode = status,
                Reason = reason,
                Body = body,
                ContentType = "application/json; charset=utf-8",
            };
        }

        private static string CreateSignedToken(string[] fields, string signingSecret)
        {
            var payload = string.Join("|", fields);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            byte[] signature;
            using (var hmac = new HMACSHA256(DeriveSigningKey(signingSecret)))
                signature = hmac.ComputeHash(payloadBytes);
            return Base64Url(payloadBytes) + "." + Base64Url(signature);
        }

        private static bool TryReadSignedToken(string token, string expectedKind, string signingSecret, out string[] fields, out long expiry)
        {
            fields = new string[0];
            expiry = 0;
            if (string.IsNullOrWhiteSpace(token) || token.Length > 16384) return false;
            var dot = token.IndexOf('.');
            if (dot <= 0 || dot != token.LastIndexOf('.')) return false;
            byte[] payloadBytes;
            byte[] signature;
            if (!TryBase64UrlDecode(token.Substring(0, dot), out payloadBytes)
                || !TryBase64UrlDecode(token.Substring(dot + 1), out signature)) return false;
            byte[] expected;
            using (var hmac = new HMACSHA256(DeriveSigningKey(signingSecret)))
                expected = hmac.ComputeHash(payloadBytes);
            if (!ConstantTimeEquals(signature, expected)) return false;
            string payload;
            try { payload = StrictUtf8.GetString(payloadBytes); } catch { return false; }
            fields = payload.Split('|');
            if (fields.Length < 3
                || !string.Equals(fields[0], "v1", StringComparison.Ordinal)
                || !string.Equals(fields[1], expectedKind, StringComparison.Ordinal)
                || !long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out expiry)
                || expiry <= UnixNow()) return false;
            return true;
        }

        private static byte[] DeriveSigningKey(string signingSecret)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes("qs3d-mcp-oauth-v1:" + (signingSecret ?? string.Empty)));
        }

        private static string CreateRandomToken(int byteCount)
        {
            var bytes = new byte[byteCount];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return Base64Url(bytes);
        }

        private static string EncodeField(string value)
        {
            return Base64Url(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static bool TryDecodeField(string encoded, out string value)
        {
            value = string.Empty;
            byte[] bytes;
            if (!TryBase64UrlDecode(encoded, out bytes)) return false;
            try { value = StrictUtf8.GetString(bytes); return true; } catch { return false; }
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static bool TryBase64UrlDecode(string value, out byte[] bytes)
        {
            bytes = new byte[0];
            if (string.IsNullOrEmpty(value)) return false;
            foreach (var ch in value)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_') continue;
                return false;
            }
            var base64 = value.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 0: break;
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
                default: return false;
            }
            try { bytes = Convert.FromBase64String(base64); return true; } catch { return false; }
        }

        private static bool ConstantTimeEquals(string left, string right)
        {
            return ConstantTimeEquals(Encoding.UTF8.GetBytes(left ?? string.Empty), Encoding.UTF8.GetBytes(right ?? string.Empty));
        }

        private static bool ConstantTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null) return false;
            var diff = left.Length ^ right.Length;
            var max = Math.Max(left.Length, right.Length);
            for (var i = 0; i < max; i++)
            {
                var a = i < left.Length ? left[i] : (byte)0;
                var b = i < right.Length ? right[i] : (byte)0;
                diff |= a ^ b;
            }
            return diff == 0;
        }

        private static string HashForCache(string value)
        {
            using (var sha = SHA256.Create()) return Base64Url(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
        }

        private static void CleanupConsumedCodes()
        {
            if (ConsumedAuthorizationCodes.Count < 256) return;
            var now = UnixNow();
            foreach (var pair in ConsumedAuthorizationCodes)
            {
                long ignored;
                if (pair.Value <= now) ConsumedAuthorizationCodes.TryRemove(pair.Key, out ignored);
            }
            if (ConsumedAuthorizationCodes.Count <= 1024) return;
            foreach (var pair in ConsumedAuthorizationCodes)
            {
                long ignored;
                ConsumedAuthorizationCodes.TryRemove(pair.Key, out ignored);
                if (ConsumedAuthorizationCodes.Count <= 768) break;
            }
        }

        private static void CleanupConsumedRefreshTokens()
        {
            if (ConsumedRefreshTokens.Count < 256) return;
            var now = UnixNow();
            foreach (var pair in ConsumedRefreshTokens)
            {
                long ignored;
                if (pair.Value <= now) ConsumedRefreshTokens.TryRemove(pair.Key, out ignored);
            }
            if (ConsumedRefreshTokens.Count <= 1024) return;
            foreach (var pair in ConsumedRefreshTokens)
            {
                long ignored;
                ConsumedRefreshTokens.TryRemove(pair.Key, out ignored);
                if (ConsumedRefreshTokens.Count <= 768) break;
            }
        }

        private static long UnixNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private static string JsonEscape(string value)
        {
            var builder = new StringBuilder();
            foreach (var ch in value ?? string.Empty)
            {
                switch (ch)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (ch < 0x20) builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(ch);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
