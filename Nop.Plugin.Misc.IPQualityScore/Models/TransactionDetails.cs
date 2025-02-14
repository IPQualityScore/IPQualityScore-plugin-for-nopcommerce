﻿using Newtonsoft.Json;

namespace Nop.Plugin.Misc.IPQualityScore.Models
{
    /// <summary>
    /// Represents a transaction details model
    /// </summary>
    public class TransactionDetails
    {
        #region Properties

        /// <summary>
        /// Gets or sets the confidence that this user or transaction is exhibiting malicious behavior.
        /// </summary>
        [JsonProperty("risk_score")]
        public double RiskScore { get; set; }

        /// <summary>
        /// Gets or sets the abusive check and reputation analysis for the email address.
        /// </summary>
        [JsonProperty("valid_billing_email")]
        public bool? ValidBillingEmail { get; set; }

        /// <summary>
        /// Gets or sets the valid & active phone number with the phone carrier (not disconnected).
        /// </summary>
        [JsonProperty("valid_billing_phone")]
        public bool? ValidBillingPhone { get; set; }

        /// <summary>
        /// Gets or sets the country associated with this phone number.
        /// </summary>
        [JsonProperty("billing_phone_country")]
        public bool? BillingPhoneCountry { get; set; }

        /// <summary>
        /// Gets or sets the abusive check and reputation analysis for the email address.
        /// </summary>
        [JsonProperty("valid_shipping_email")]
        public bool? ValidShippingEmail { get; set; }

        /// <summary>
        /// Gets or sets the valid & active phone number with the phone carrier (not disconnected).
        /// </summary>
        [JsonProperty("valid_shipping_phone")]
        public bool? ValidShippingPhone { get; set; }

        /// <summary>
        /// Gets or sets the country associated with this phone number.
        /// </summary>
        [JsonProperty("shipping_phone_country")]
        public bool? ShippingPhoneCountry { get; set; }

        #endregion
    }
}
