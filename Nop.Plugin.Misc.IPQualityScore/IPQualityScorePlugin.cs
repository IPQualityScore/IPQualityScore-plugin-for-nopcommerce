﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Orders;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;
using System;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.IPQualityScore
{
    /// <summary>
    /// Represents the IPQualityScore plugin
    /// </summary>
    public class IPQualityScorePlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
    {
        #region Fields

        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly WidgetSettings _widgetSettings;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => true;

        #endregion

        #region Ctor

        public IPQualityScorePlugin(
            IActionContextAccessor actionContextAccessor,
            ISettingService settingService,
            ILocalizationService localizationService,
            IUrlHelperFactory urlHelperFactory,
            WidgetSettings widgetSettings)
        {
            _actionContextAccessor = actionContextAccessor;
            _settingService = settingService;
            _localizationService = localizationService;
            _urlHelperFactory = urlHelperFactory;
            _widgetSettings = widgetSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a name of a view component for displaying widget
        /// </summary>
        /// <param name="widgetZone">Name of the widget zone</param>
        /// <returns>View component name</returns>
        public string GetWidgetViewComponentName(string widgetZone)
        {
            if (widgetZone == null)
                throw new ArgumentNullException(nameof(widgetZone));

            if (widgetZone.Equals(PublicWidgetZones.BodyStartHtmlTagAfter))
                return Defaults.DEVICE_FINGERPRINT_VIEW_COMPONENT_NAME;

            return string.Empty;
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public IList<string> GetWidgetZones()
        {
            return new List<string>
            {
                PublicWidgetZones.BodyStartHtmlTagAfter
            };
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return _urlHelperFactory
                .GetUrlHelper(_actionContextAccessor.ActionContext)
                .RouteUrl(Defaults.ConfigurationRouteName);
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new IPQualityScoreSettings
            {
                // Common
                LogRequestErrors = true,

                // IP Reputation
                IPReputationEnabled = true,
                IPReputationFraudScoreForBlocking = 85,
                ProxyBlockingEnabled = true,
                VpnBlockingEnabled = true,
                TorBlockingEnabled = true,
                IPReputationStrictness = 0,
                AllowPublicAccessPoints = true,
                LighterPenalties = true,
                AllowCrawlers = true,

                // Order Scoring
                OrderScoringEnabled = true,
                RiskScoreForBlocking = 85,
                TransactionStrictness = 0,
                ApproveStatusId = (int)OrderStatus.Processing,
                RejectStatusId = (int)OrderStatus.Cancelled,
                InformCustomerAboutFraud = true,

                // Email reputation
                EmailValidationEnabled = true,
                EmailReputationFraudScoreForBlocking = 85,
                EmailReputationStrictness = 0,
                AbuseStrictness = 0,
                EmailReputationTimeout = 20,

                // Device fingerprint
                DeviceFingerprintFraudChance = 85
            });

            if (!_widgetSettings.ActiveWidgetSystemNames.Contains(Defaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Add(Defaults.SystemName);
                _settingService.SaveSetting(_widgetSettings);
            }

            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Misc.IPQualityScore.DeviceFingerprint"] = "Device Fingerprint",
                ["Plugins.Misc.IPQualityScore.EmailReputation"] = "Email Validation",
                ["Plugins.Misc.IPQualityScore.IPReputation"] = "Proxy Detection & Fraud Prevention",
                ["Plugins.Misc.IPQualityScore.IPReputation.OrderScoring"] = "Order Fraud Detection",
                ["Plugins.Misc.IPQualityScore.PreventFraudPage.Content"] = "Unfortunately we were unable to process this action. Please contact us if this error persists.",
                ["Plugins.Misc.IPQualityScore.Order.MessageToCustomerWhenFraudIsDetected"] = "We detected fraudulent activity in relation to this order, so we changed the order status to '{0}'.",
                ["Plugins.Misc.IPQualityScore.Fields.AbuseStrictness"] = "Abuse strictness",
                ["Plugins.Misc.IPQualityScore.Fields.AbuseStrictness.Hint"] = "Set the strictness level for machine learning pattern recognition of abusive email addresses. Default level of 0 provides good coverage, however if you are filtering account applications and facing advanced fraudsters then we recommend increasing this value to level 1 or 2.",
                ["Plugins.Misc.IPQualityScore.Fields.AbuseStrictness.FromZeroToTwo"] = "The abuse strictness should be in range from 0 to 2.",
                ["Plugins.Misc.IPQualityScore.Fields.AllowPublicAccessPoints"] = "Allow public access points",
                ["Plugins.Misc.IPQualityScore.Fields.AllowPublicAccessPoints.Hint"] = "Allows corporate and public connections like Institutions, Hotels, Businesses, Universities, etc.",
                ["Plugins.Misc.IPQualityScore.Fields.ApiKey"] = "API key",
                ["Plugins.Misc.IPQualityScore.Fields.ApiKey.Hint"] = "Enter the key to sign the API requests.",
                ["Plugins.Misc.IPQualityScore.Fields.ApiKey.Instructions"] = "You can find it <a target=\"_blank\" href=\"https://www.ipqualityscore.com/user/settings#account\">here</a>. If you don't have an IPQS account, create a new one <a target=\"_blank\" href=\"https://www.ipqualityscore.com/create-account\">here</a>.",
                ["Plugins.Misc.IPQualityScore.Fields.ApiKey.Required"] = "The API token required.",
                ["Plugins.Misc.IPQualityScore.Fields.ApproveStatusId"] = "Approve status",
                ["Plugins.Misc.IPQualityScore.Fields.ApproveStatusId.Hint"] = "Change order status when order has been approved by IPQualityScore.",
                ["Plugins.Misc.IPQualityScore.Fields.AllowCrawlers"] = "Allow crawlers",
                ["Plugins.Misc.IPQualityScore.Fields.AllowCrawlers.Hint"] = "Check to don't block crawlers (Googlebot, Bingbot, Yandex, etc).",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintEnabled"] = "Enable the Device Fingerprint",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintEnabled.Hint"] = "Check to enable the Device Fingerprint on register, login, checkout and user account (info, change password) pages. IPQualityScore's Device Fingerprint Technology allows you to further analyze your users, transactions, ad traffic, and similar data to produce highly accurate Fraud Scores.",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintFraudChance"] = "Fraud chance",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintFraudChance.Hint"] = "How likely this device is to commit fraud or engage in abusive behavior. 0 = not likely, 100 = very likely. 25 is the median result. Fraud Scores >= 75 are suspicious, but not necessarily fraudulent. We recommend flagging or blocking traffic with Fraud Scores >= 85, but you may find it beneficial to use a higher or lower threshold.",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintFraudChance.FromZeroToOneHundred"] = "The fraud chance should be in range from 0 to 100.",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintTrackingCode"] = "Tracking code",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintTrackingCode.Hint"] = "Enter the Device Fingerprint JavaScript snippet code.",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintTrackingCode.Required"] = "The tracking code required.",
                ["Plugins.Misc.IPQualityScore.Fields.DeviceFingerprintTrackingCode.Instructions"] = "You can create a new one <a target=\"_blank\" href=\"https://www.ipqualityscore.com/user/tracker/new\">here</a>.",
                ["Plugins.Misc.IPQualityScore.Fields.EmailReputationFraudScoreForBlocking"] = "Fraud score",
                ["Plugins.Misc.IPQualityScore.Fields.EmailReputationFraudScoreForBlocking.Hint"] = "The overall Fraud Score of the user based on the email's reputation and recent behavior across the IPQS threat network. Scores are 0 - 100. Fraud Scores >= 75 are suspicious, but not necessarily fraudulent.",
                ["Plugins.Misc.IPQualityScore.Fields.EmailReputationFraudScoreForBlocking.FromZeroToOneHundred"] = "The fraud score should be in range from 0 to 100.",
                ["Plugins.Misc.IPQualityScore.Fields.EmailReputationStrictness"] = "Strictness",
                ["Plugins.Misc.IPQualityScore.Fields.EmailReputationStrictness.Hint"] = "Sets how strictly spam traps and honeypots are detected by our system, depending on how comfortable you are with identifying emails suspected of being a spam trap. 0 is the lowest level which will only return spam traps with high confidence. Strictness levels above 0 will return increasingly more strict results, with level 2 providing the greatest detection rates.",
                ["Plugins.Misc.IPQualityScore.Fields.EmailReputationStrictness.FromZeroToTwo"] = "The strictness should be in range from 0 to 2.",
                ["Plugins.Misc.IPQualityScore.Fields.EmailValidationEnabled"] = "Enable the Email Validation",
                ["Plugins.Misc.IPQualityScore.Fields.EmailValidationEnabled.Hint"] = "Check to enable the email validation on register and user account (info) pages.",
                ["Plugins.Misc.IPQualityScore.Fields.IPReputationEnabled"] = "Enable the IP Reputation",
                ["Plugins.Misc.IPQualityScore.Fields.IPReputationEnabled.Hint"] = "Check to enable the IP Reputation service. Detect bots, proxies, VPNs, and TOR connections in addition to residential proxies and private botnets on all pages of your store.",
                ["Plugins.Misc.IPQualityScore.Fields.IPReputationFraudScoreForBlocking"] = "Fraud score",
                ["Plugins.Misc.IPQualityScore.Fields.IPReputationFraudScoreForBlocking.Hint"] = "The overall fraud score of the user based on the IP, user agent, language, and any other optionally passed variables. Scores are 0 - 100. Fraud Scores >= 75 are suspicious, but not necessarily fraudulent. We recommend flagging or blocking traffic with Fraud Scores >= 85, but you may find it beneficial to use a higher or lower threshold.",
                ["Plugins.Misc.IPQualityScore.Fields.IPReputationFraudScoreForBlocking.FromZeroToOneHundred"] = "The fraud score should be in range from 0 to 100.",
                ["Plugins.Misc.IPQualityScore.Fields.IPReputationStrictness"] = "Strictness",
                ["Plugins.Misc.IPQualityScore.Fields.IPReputationStrictness.Hint"] = "How in depth (strict) do you want this query to be? Higher values take longer to process and may provide a higher false-positive rate. We recommend starting at \"0\", the lowest strictness setting, and increasing to \"1\", \"2\" or \"3\" depending on your levels of fraud.",
                ["Plugins.Misc.IPQualityScore.Fields.IPReputationStrictness.FromZeroToThree"] = "The strictness should be in range from 0 to 3.",
                ["Plugins.Misc.IPQualityScore.Fields.LighterPenalties"] = "Lighter penalties",
                ["Plugins.Misc.IPQualityScore.Fields.LighterPenalties.Hint"] = "Lowers scoring and proxy detection for mixed quality IP addresses to prevent false-positives.",
                ["Plugins.Misc.IPQualityScore.Fields.OrderScoringEnabled"] = "Enable the Order Fraud Detection",
                ["Plugins.Misc.IPQualityScore.Fields.OrderScoringEnabled.Hint"] = "Check to enable the Fraud Detection for orders. Additional user data for orders, payments, transactions, and personal user information can be analyzed to enhance Fraud Scores.",
                ["Plugins.Misc.IPQualityScore.Fields.ProxyBlockingEnabled"] = "Block proxy",
                ["Plugins.Misc.IPQualityScore.Fields.ProxyBlockingEnabled.Hint"] = "Check to block the proxy connections.",
                ["Plugins.Misc.IPQualityScore.Fields.RejectStatusId"] = "Reject status",
                ["Plugins.Misc.IPQualityScore.Fields.RejectStatusId.Hint"] = "Change order status when order has been rejected by IPQualityScore.",
                ["Plugins.Misc.IPQualityScore.Fields.RiskScoreForBlocking"] = "Risk score",
                ["Plugins.Misc.IPQualityScore.Fields.RiskScoreForBlocking.Hint"] = "Confidence that this user or transaction is exhibiting malicious behavior. Scores are 0 - 100, with 75+ as suspicious and 90+ as high risk. This value uses different calculations with less weight on the IP reputation compared to the overall \"Fraud Score\".",
                ["Plugins.Misc.IPQualityScore.Fields.RiskScoreForBlocking.FromZeroToOneHundred"] = "The risk score should be in range from 0 to 100.",
                ["Plugins.Misc.IPQualityScore.Fields.TorBlockingEnabled"] = "Block TOR",
                ["Plugins.Misc.IPQualityScore.Fields.TorBlockingEnabled.Hint"] = "Check to block the TOR connections.",
                ["Plugins.Misc.IPQualityScore.Fields.TransactionStrictness"] = "Transaction strictness",
                ["Plugins.Misc.IPQualityScore.Fields.TransactionStrictness.Hint"] = "Adjusts the weights for penalties applied due to irregularities and fraudulent patterns detected on order and transaction details that can be optionally provided on each API request.",
                ["Plugins.Misc.IPQualityScore.Fields.TransactionStrictness.FromZeroToTwo"] = "The transaction strictness should be in range from 0 to 2.",
                ["Plugins.Misc.IPQualityScore.Fields.VpnBlockingEnabled"] = "Block VPN",
                ["Plugins.Misc.IPQualityScore.Fields.VpnBlockingEnabled.Hint"] = "Check to block the VPN connections.",
                ["Plugins.Misc.IPQualityScore.Fields.BlockUserIfScriptIsBlocked"] = "Block user if the tracking code is blocked by user",
                ["Plugins.Misc.IPQualityScore.Fields.BlockUserIfScriptIsBlocked.Hint"] = "Check to block user if the tracking code is blocked  by user. Blocked user will redirect to the prevent fraud page.",
                ["Plugins.Misc.IPQualityScore.Fields.InformCustomerAboutFraud"] = "Inform the customer about fraud",
                ["Plugins.Misc.IPQualityScore.Fields.InformCustomerAboutFraud.Hint"] = "Check to inform the customer about potential fraud in the order notes.",
            });

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<IPQualityScoreSettings>();

            if (_widgetSettings.ActiveWidgetSystemNames.Contains(Defaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Remove(Defaults.SystemName);
                _settingService.SaveSetting(_widgetSettings);
            }

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.Misc.IPQualityScore");

            base.Uninstall();
        }

        #endregion
    }
}
