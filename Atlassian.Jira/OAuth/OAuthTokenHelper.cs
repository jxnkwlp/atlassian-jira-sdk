﻿using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using RestSharp;
using RestSharp.Authenticators;

namespace Atlassian.Jira.OAuth
{
    /// <summary>
    /// Helper to create and send request for the OAuth authentification process.
    /// </summary>
    public static class OAuthTokenHelper
    {
        /// <summary>
        /// Generate a request token for the OAuth authentification process.
        /// </summary>
        /// <param name="oAuthRequestTokenSettings"> The request token settings.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>The <see cref="OAuthRequestToken" /> containing the request token, the consumer token and the authorize url.</returns>
        public static Task<OAuthRequestToken> GenerateRequestTokenAsync(OAuthRequestTokenSettings oAuthRequestTokenSettings, CancellationToken cancellationToken = default(CancellationToken))
        {
            var authenticator = OAuth1Authenticator.ForRequestToken(
                oAuthRequestTokenSettings.ConsumerKey,
                oAuthRequestTokenSettings.ConsumerSecret,
                oAuthRequestTokenSettings.CallbackUrl);

            authenticator.SignatureMethod = oAuthRequestTokenSettings.SignatureMethod.ToOAuthSignatureMethod();

            var restClient =
                new RestClient(
                    oAuthRequestTokenSettings.Url,
                    restClientOptions => restClientOptions.Authenticator = authenticator);

            return GenerateRequestTokenAsync(
                restClient,
                oAuthRequestTokenSettings.RequestTokenUrl,
                oAuthRequestTokenSettings.AuthorizeUrl,
                cancellationToken);
        }

        /// <summary>
        /// Generate a request token for the OAuth authentification process.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="requestTokenUrl">The relative url to request the token to Jira.</param>
        /// <param name="authorizeTokenUrl">The relative url to authorize the token.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>The <see cref="OAuthRequestToken" /> containing the request token, the consumer token and the authorize url.</returns>
        public static async Task<OAuthRequestToken> GenerateRequestTokenAsync(
            RestClient restClient,
            string requestTokenUrl,
            string authorizeTokenUrl,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var requestTokenResponse = await restClient.ExecutePostAsync(
                new RestRequest(requestTokenUrl),
                cancellationToken).ConfigureAwait(false);

            if (requestTokenResponse.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var requestTokenQuery = HttpUtility.ParseQueryString(requestTokenResponse.Content.Trim());

            var oauthToken = requestTokenQuery["oauth_token"];
            var authorizeUri = $"{restClient.Options.BaseUrl}/{authorizeTokenUrl}?oauth_token={oauthToken}";

            return new OAuthRequestToken(
                authorizeUri,
                oauthToken,
                requestTokenQuery["oauth_token_secret"],
                requestTokenQuery["oauth_callback_confirmed"]);
        }


        /// <summary>
        /// Obtain the access token from an authorized request token.
        /// </summary>
        /// <param name="oAuthAccessTokenSettings">The settings to obtain the access token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The access token from Jira.
        /// Return null if the token was not returned by Jira or the token secret for the request token and the access token don't match.</returns>
        public static Task<OAuthAccessToken> ObtainOAuthAccessTokenAsync(OAuthAccessTokenSettings oAuthAccessTokenSettings, CancellationToken cancellationToken)
        {
            var authenticator = OAuth1Authenticator.ForAccessToken(
                oAuthAccessTokenSettings.ConsumerKey,
                oAuthAccessTokenSettings.ConsumerSecret,
                oAuthAccessTokenSettings.OAuthRequestToken,
                oAuthAccessTokenSettings.OAuthTokenSecret,
                oAuthAccessTokenSettings.OAuthVerifier);
            authenticator.SignatureMethod = oAuthAccessTokenSettings.SignatureMethod.ToOAuthSignatureMethod();

            var restClient =
                new RestClient(
                    oAuthAccessTokenSettings.Url,
                    restClientOptions => restClientOptions.Authenticator = authenticator);

            return ObtainOAuthAccessTokenAsync(
                restClient,
                oAuthAccessTokenSettings.AccessTokenUrl,
                oAuthAccessTokenSettings.OAuthTokenSecret,
                cancellationToken);
        }

        /// <summary>
        /// Obtain the access token from an authorized request token.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="accessTokenUrl">The relative path to the url to request the access token to Jira.</param>
        /// <param name="oAuthTokenSecret">The OAuth token secret generated by Jira.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The access token from Jira.
        /// Return null if the token was not returned by Jira or the token secret for the request token and the access token don't match.</returns>
        public static async Task<OAuthAccessToken> ObtainOAuthAccessTokenAsync(
            RestClient restClient,
            string accessTokenUrl,
            string oAuthTokenSecret,
            CancellationToken cancellationToken)
        {
            var accessTokenResponse = await restClient.ExecutePostAsync(
                new RestRequest(accessTokenUrl, Method.Post),
                cancellationToken).ConfigureAwait(false);

            if (accessTokenResponse.StatusCode != HttpStatusCode.OK)
            {
                // The token has not been authorize or something went wrong
                return null;
            }

            var accessTokenQuery = HttpUtility.ParseQueryString(accessTokenResponse.Content.Trim());

            if (oAuthTokenSecret != accessTokenQuery["oauth_token_secret"])
            {
                // The request token secret and access token secret do not match.
                return null;
            }

            var accessToken = accessTokenQuery["oauth_token"];
            var expiry = DateTimeOffset.UtcNow.AddSeconds(int.Parse(accessTokenQuery["oauth_expires_in"], CultureInfo.InvariantCulture));

            return new OAuthAccessToken(accessToken, oAuthTokenSecret, expiry);
        }
    }
}
