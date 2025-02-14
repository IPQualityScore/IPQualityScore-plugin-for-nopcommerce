﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Nop.Plugin.Misc.IPQualityScore.Models;

namespace Nop.Plugin.Misc.IPQualityScore.Services
{
    /// <summary>
    /// Provides an default implementation of the HTTP client to interact with the IPQualityScore endpoints
    /// </summary>
    public class IPQualityScoreApi : BaseHttpClient
    {
        #region Ctor

        public IPQualityScoreApi(IPQualityScoreSettings settings, HttpClient httpClient)
            : base(settings, httpClient)
        {
        }

        #endregion

        #region Methods

        public virtual Task<IPReputationResponse> GetIPReputationAsync(string ipAddress, IDictionary<string, string> queryString = null)
        {
            if (ipAddress is null)
                throw new ArgumentNullException(nameof(ipAddress));

            return GetAsync<IPReputationResponse>($"{Defaults.IPQualityScore.Api.Endpoints.IPReputationPath}/{ApiKey}/{ipAddress}", queryString);
        }

        public virtual Task<EmailReputationResponse> GetEmailReputationAsync(string email, IDictionary<string, string> queryString = null)
        {
            if (email is null)
                throw new ArgumentNullException(nameof(email));

            return GetAsync<EmailReputationResponse>($"{Defaults.IPQualityScore.Api.Endpoints.EmailReputationPath}/{ApiKey}/{email}", queryString);
        }

        #endregion
    }
}
