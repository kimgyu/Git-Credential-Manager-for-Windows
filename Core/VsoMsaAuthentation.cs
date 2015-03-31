﻿using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class VsoMsaAuthentation : BaseVsoAuthentication, IVsoMsaAuthentication
    {
        public const string DefaultAuthorityHost = "https://login.windows.net/live.com";

        public VsoMsaAuthentation(string resource = null, string clientId = null)
            : base(resource, clientId)
        {
            this.LiveAuthority = new AzureAuthority();
        }
        /// <summary>
        /// Test constructor which allows for using fake credential stores
        /// </summary>
        /// <param name="personalAccessToken"></param>
        /// <param name="userCredential"></param>
        /// <param name="adaRefresh"></param>
        internal VsoMsaAuthentation(ICredentialStore personalAccessToken, ICredentialStore userCredential, ITokenStore adaRefresh, ILiveAuthority liveAuthority, IVsoAuthority vsoAuthority)
            : base(personalAccessToken, userCredential, adaRefresh, vsoAuthority)
        {
            this.LiveAuthority = liveAuthority;
        }

        internal ILiveAuthority LiveAuthority { get; set; }

        public bool InteractiveLogon(Uri targetUri)
        {
            const string QueryParameterDomainHints = "domain_hint=live.com&display=popup";

            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                Tokens tokens;
                if ((tokens = this.LiveAuthority.AcquireToken(this.ClientId, this.Resource, new Uri(RedirectUrl), QueryParameterDomainHints)) != null)
                {
                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return Task.Run(async () => { return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken); }).Result;
                }
            }
            catch (AdalException exception)
            {
                Debug.Write(exception);
            }

            return false;
        }

        public override async Task<bool> RefreshCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                Token refreshToken = null;
                Tokens tokens = null;
                if (this.AdaRefreshTokenStore.ReadToken(targetUri, out refreshToken))
                {
                    if ((tokens = await this.LiveAuthority.AcquireTokenByRefreshTokenAsync(this.ClientId, this.Resource, refreshToken)) != null)
                    {
                        return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }

            return false;
        }

        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            throw new NotSupportedException();
        }
    }
}
