using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.IPQualityScore.Areas.Admin.Models;
using Nop.Plugin.Misc.IPQualityScore.Models;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.Misc.IPQualityScore.Services
{
    public class IPQualityScoreService
    {
        #region Fields

        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPQualityScoreApi _iPQualityScoreApi;
        private readonly IPQualityScoreSettings _iPQualityScoreSettings;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IUserAgentHelper _userAgentHelper;
        private readonly IWidgetPluginManager _widgetPluginManager;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public IPQualityScoreService(
            IAddressService addressService,
            ICountryService countryService,
            ICustomerService customerService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPQualityScoreApi iPQualityScoreApi,
            IPQualityScoreSettings iPQualityScoreSettings,
            ILocalizationService localizationService,
            ILogger logger,
            IUserAgentHelper userAgentHelper,
            IWidgetPluginManager widgetPluginManager,
            IWorkContext workContext,
            IWebHelper webHelper)
        {
            _addressService = addressService;
            _countryService = countryService;
            _customerService = customerService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _iPQualityScoreApi = iPQualityScoreApi;
            _iPQualityScoreSettings = iPQualityScoreSettings;
            _localizationService = localizationService;
            _logger = logger;
            _userAgentHelper = userAgentHelper;
            _widgetPluginManager = widgetPluginManager;
            _workContext = workContext;
            _webHelper = webHelper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the value indicating whether to the request can be validated using IPQS.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <returns>The value indicating whether to the request can be validated using IPQS.</returns>
        public virtual bool CanValidateRequest(ActionContext actionContext)
        {
            if (actionContext is null)
                throw new ArgumentNullException(nameof(actionContext));

            var customer = _workContext.CurrentCustomer;
            if (customer.IsSystemAccount)
            {
                if (!_userAgentHelper.IsSearchEngine())
                    return false;
                else
                {
                    if (_iPQualityScoreSettings.AllowCrawlers)
                        return false;
                }
            }

            if (_workContext.IsAdmin)
                return false;

            if (!actionContext.ModelState.IsValid)
            {
                // don't validate invalid post actions
                return false;
            }

            var routeName = GetCurrentRouteName(actionContext.HttpContext);
            if (routeName == Defaults.PreventFraudRouteName)
                return false;

            var ipAddress = _webHelper.GetCurrentIpAddress();
            if (string.IsNullOrEmpty(ipAddress) || ipAddress.Equals("127.0.0.1"))
                return false;

            if (!(_widgetPluginManager.LoadPluginBySystemName(Defaults.SystemName) is IPQualityScorePlugin plugin) || !_widgetPluginManager.IsPluginActive(plugin))
                return false;

            if (!_iPQualityScoreSettings.IPReputationEnabled)
                return false;

            if (_customerService.IsAdmin(customer))
                return false;

            return IsConfigured();
        }

        /// <summary>
        /// Returns the value indicating whether to the request is valid.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <returns>The <see cref="Task"/> containing the value indicating whether to the request is valid.</returns>
        public virtual async Task<bool> ValidateRequestAsync(ActionContext actionContext)
        {
            if (actionContext is null)
                throw new ArgumentNullException(nameof(actionContext));

            var query = GetIpReputationQuery(actionContext.HttpContext);
            var response = await GetIPReputationAsync(query);

            return IsValidResponse(response);
        }

        /// <summary>
        /// Returns the value indicating whether to the order can be validated using IPQS.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="actionContext">The action context.</param>
        /// <returns>The value indicating whether to the order can be validated using IPQS.</returns>
        public virtual bool CanValidateOrder(Order order, ActionContext actionContext)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            if (!_iPQualityScoreSettings.OrderScoringEnabled)
                return false;

            return CanValidateRequest(actionContext);
        }

        /// <summary>
        /// Returns the value indicating whether to the order is valid.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="actionContext">The action context.</param>
        /// <returns>The <see cref="Task"/> containing the value indicating whether to the order is valid.</returns>
        public virtual async Task<bool> ValidateOrderAsync(Order order, ActionContext actionContext)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            if (actionContext is null)
                throw new ArgumentNullException(nameof(actionContext));

            var query = GetIpReputationTransactionQuery(order, actionContext.HttpContext);
            var response = await GetIPReputationAsync(query);

            return IsValidTransactionalResponse(response);
        }

        /// <summary>
        /// Approves the order.
        /// </summary>
        /// <param name="order">The order.</param>
        public virtual void ApproveOrder(Order order)
        {
            SetOrderStatus(order, _iPQualityScoreSettings.ApproveStatusId);
        }

        /// <summary>
        /// Rejectes the order.
        /// </summary>
        /// <param name="order">The order.</param>
        public virtual void RejectOrder(Order order)
        {
            SetOrderStatus(order, _iPQualityScoreSettings.RejectStatusId);
            
            _orderService.InsertOrderNote(new OrderNote
            {
                OrderId = order.Id,
                DisplayToCustomer = _iPQualityScoreSettings.InformCustomerAboutFraud,
                CreatedOnUtc = DateTime.UtcNow,
                Note = string.Format(
                    _localizationService.GetResource("Plugins.Misc.IPQualityScore.Order.MessageToCustomerWhenFraudIsDetected"),
                    _localizationService.GetLocalizedEnum(order.OrderStatus))
            });
        }

        /// <summary>
        /// Returns the value indicating whether to the email can be validated using IPQS.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <returns>The value indicating whether to the email can be validated using IPQS.</returns>
        public virtual bool CanValidateEmailForRequest(ActionContext actionContext)
        {
            if (actionContext is null)
                throw new ArgumentNullException(nameof(actionContext));

            if (actionContext.HttpContext.Request.Method != HttpMethods.Post)
                return false;

            if (!actionContext.ModelState.IsValid)
            {
                // don't validate email for invalid POST requests
                return false;
            }

            var routeName = GetCurrentRouteName(actionContext.HttpContext);
            if (routeName == Defaults.PreventFraudRouteName)
                return false;

            if (!Defaults.EmailValidationRouteNames.Contains(routeName))
                return false;

            if (!actionContext.HttpContext.Request.Form.TryGetValue("Email", out var email))
                return false;

            if (!(_widgetPluginManager.LoadPluginBySystemName(Defaults.SystemName) is IPQualityScorePlugin plugin) || !_widgetPluginManager.IsPluginActive(plugin))
                return false;

            if (!_iPQualityScoreSettings.EmailValidationEnabled)
                return false;

            var customer = _workContext.CurrentCustomer;
            if (customer.IsSystemAccount || _customerService.IsAdmin(customer))
                return false;

            if (customer.Email == email)
                return false;

            return IsConfigured();
        }

        /// <summary>
        /// Returns the value indicating whether to the email is valid.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <returns>The <see cref="Task"/> containing the value indicating whether to the email is valid.</returns>
        public virtual async Task<bool> ValidateEmailForRequestAsync(ActionContext actionContext)
        {
            if (actionContext is null)
                throw new ArgumentNullException(nameof(actionContext));

            if (!actionContext.HttpContext.Request.Form.TryGetValue("Email", out var email))
                return false;

            var query = new Dictionary<string, string>()
            {
                ["timeout"] = _iPQualityScoreSettings.EmailReputationTimeout.ToString(),
                ["strictness"] = _iPQualityScoreSettings.EmailReputationStrictness.ToString(),
                ["abuse_strictness"] = _iPQualityScoreSettings.AbuseStrictness.ToString(),
            };

            EmailReputationResponse response = null;
            try
            {
                response = await _iPQualityScoreApi.GetEmailReputationAsync(email, query);
            }
            catch (ApiException ex)
            {
                if (_iPQualityScoreSettings.LogRequestErrors)
                    _logger.Error($"{Defaults.SystemName}: Error when calling the Email Reputation endpoint for email '{email}'.", ex, _workContext.CurrentCustomer);
            }

            if (response is null)
            {
                // don't validate request if the HTTP error occurs
                return true;
            }

            var isFraud = response.FraudScore >= _iPQualityScoreSettings.EmailReputationFraudScoreForBlocking;

            return response.Success && response.Valid && !response.Disposable && !isFraud;
        }

        /// <summary>
        /// Returns the value indicating whether to the IPQualityScore device fingerprint can be displayed in public store.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>The value indicating whether to the IPQualityScore device fingerprint can be displayed in public store.</returns>
        public virtual bool CanDisplayDeviceFingerprint(HttpContext httpContext)
        {
            if (httpContext is null)
                throw new ArgumentNullException(nameof(httpContext));

            if (!(_widgetPluginManager.LoadPluginBySystemName(Defaults.SystemName) is IPQualityScorePlugin plugin) || !_widgetPluginManager.IsPluginActive(plugin))
                return false;

            if (!_iPQualityScoreSettings.DeviceFingerprintEnabled)
                return false;

            var routeName = GetCurrentRouteName(httpContext);
            if (routeName == Defaults.PreventFraudRouteName)
                return false;

            if (!Defaults.DeviceFingerprintRouteNames.Contains(routeName))
                return false;

            var customer = _workContext.CurrentCustomer;
            if (customer.IsSystemAccount || _customerService.IsAdmin(customer))
                return false;

            return IsConfigured();
        }

        #endregion

        #region Utilities

        private async Task<IPReputationResponse> GetIPReputationAsync(IDictionary<string, string> query)
        {
            var ipAddress = _webHelper.GetCurrentIpAddress();

            try
            {
                return await _iPQualityScoreApi.GetIPReputationAsync(ipAddress, query);
            }
            catch (ApiException ex)
            {
                if (_iPQualityScoreSettings.LogRequestErrors)
                    _logger.Error($"{Defaults.SystemName}: Error when calling the IP Reputation endpoint for IP '{ipAddress}'.", ex, _workContext.CurrentCustomer);
            }

            return null;
        }

        private IDictionary<string, string> GetIpReputationQuery(HttpContext httpContext)
        {
            var language = _workContext.WorkingLanguage;
            var query = new Dictionary<string, string>()
            {
                ["mobile"] = _userAgentHelper.IsMobileDevice().ToString(),
                ["strictness"] = _iPQualityScoreSettings.IPReputationStrictness.ToString(),
                ["allow_public_access_points"] = _iPQualityScoreSettings.AllowPublicAccessPoints.ToString(),
                ["lighter_penalties"] = _iPQualityScoreSettings.LighterPenalties.ToString(),
                ["user_language"] = language.LanguageCulture
            };

            if (httpContext.Request.Headers.TryGetValue("User-Agent", out var userAgentRaw))
                query["user_agent"] = userAgentRaw.ToString();

            return query;
        }

        private IDictionary<string, string> GetIpReputationTransactionQuery(Order order, HttpContext httpContext)
        {
            var query = GetIpReputationQuery(httpContext);

            var billingAddress = _addressService.GetAddressById(order.BillingAddressId);
            if (billingAddress != null)
            {
                if (!string.IsNullOrWhiteSpace(billingAddress.Email))
                    query["billing_email"] = billingAddress.Email;

                if (!string.IsNullOrWhiteSpace(billingAddress.PhoneNumber))
                    query["billing_phone"] = billingAddress.PhoneNumber;

                if (billingAddress.CountryId.HasValue)
                {
                    var country = _countryService.GetCountryById(billingAddress.CountryId.Value);
                    if (country != null)
                        query["billing_country"] = country.TwoLetterIsoCode;
                }
            }

            if (order.ShippingAddressId.HasValue && !order.PickupInStore)
            {
                var shippingAddress = _addressService.GetAddressById(order.ShippingAddressId.Value);
                if (shippingAddress != null)
                {
                    if (!string.IsNullOrWhiteSpace(shippingAddress.Email))
                        query["shipping_email"] = shippingAddress.Email;

                    if (!string.IsNullOrWhiteSpace(shippingAddress.PhoneNumber))
                        query["shipping_phone"] = shippingAddress.PhoneNumber;

                    if (shippingAddress.CountryId.HasValue)
                    {
                        var country = _countryService.GetCountryById(shippingAddress.CountryId.Value);
                        if (country != null)
                            query["shipping_country"] = country.TwoLetterIsoCode;
                    }
                }
            }

            return query;
        }

        private bool IsValidResponse(IPReputationResponse response)
        {
            if (response is null)
            {
                // don't validate request if the HTTP error occurs
                return true;
            }

            var isAbuseVelocity = response.AbuseVelocity?.Equals("high", StringComparison.InvariantCultureIgnoreCase) == true;
            var isProxy = _iPQualityScoreSettings.ProxyBlockingEnabled && response.IsProxy;
            var isVpn = _iPQualityScoreSettings.VpnBlockingEnabled && response.IsVpn;
            var isTor = _iPQualityScoreSettings.TorBlockingEnabled && response.IsTor;
            var isFraud = response.FraudScore >= _iPQualityScoreSettings.IPReputationFraudScoreForBlocking;

            if (_iPQualityScoreSettings.AllowCrawlers && response.IsCrawler)
                return response.Success;

            return response.Success && !isFraud && !isProxy && !isVpn && !isTor;
        }

        private bool IsValidTransactionalResponse(IPReputationResponse response)
        {
            var isValidRequest = IsValidResponse(response);
            if (isValidRequest && response?.TransactionDetails != null)
            {
                var details = response.TransactionDetails;

                var isValidBillingEmail = !details.ValidBillingEmail.HasValue || details.ValidBillingEmail.Value;
                var isValidBillingPhone = !details.ValidBillingPhone.HasValue || details.ValidBillingPhone.Value;
                var isValidBillingPhoneCountry = !details.BillingPhoneCountry.HasValue || details.BillingPhoneCountry.Value;
                var isValidBillingData = isValidBillingEmail && isValidBillingPhone && isValidBillingPhoneCountry;

                var isValidShippingEmail = !details.ValidShippingEmail.HasValue || details.ValidShippingEmail.Value;
                var isValidShippingPhone = !details.ValidShippingPhone.HasValue || details.ValidShippingPhone.Value;
                var isValidShippingPhoneCountry = !details.ShippingPhoneCountry.HasValue || details.ShippingPhoneCountry.Value;
                var isValidShippingData = isValidShippingEmail && isValidShippingPhone && isValidShippingPhoneCountry;

                var isFraud = details.RiskScore >= _iPQualityScoreSettings.RiskScoreForBlocking;

                return !isFraud;
            }

            return isValidRequest;
        }

        private void SetOrderStatus(Order order, int statusId)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            order.OrderStatusId = statusId;
            _orderService.UpdateOrder(order);
            _orderProcessingService.CheckOrderStatus(order);
        }

        private string GetCurrentRouteName(HttpContext httpContext)
        {
            return httpContext
                .GetEndpoint()?.Metadata
                .GetMetadata<RouteNameMetadata>()?.RouteName;
        }

        private bool IsConfigured()
        {
            var validator = EngineContext.Current.Resolve<IValidator<ConfigurationModel>>();
            var validationResult = validator.Validate(new ConfigurationModel
            {
                ApiKey = _iPQualityScoreSettings.ApiKey,
                IPReputationFraudScoreForBlocking = _iPQualityScoreSettings.IPReputationFraudScoreForBlocking,
                IPReputationStrictness = _iPQualityScoreSettings.IPReputationStrictness,
                TransactionStrictness = _iPQualityScoreSettings.TransactionStrictness,
                RiskScoreForBlocking = _iPQualityScoreSettings.RiskScoreForBlocking,
                IPReputationEnabled = _iPQualityScoreSettings.IPReputationEnabled,
                OrderScoringEnabled = _iPQualityScoreSettings.OrderScoringEnabled,
                EmailValidationEnabled = _iPQualityScoreSettings.EmailValidationEnabled,
                EmailReputationFraudScoreForBlocking = _iPQualityScoreSettings.EmailReputationFraudScoreForBlocking,
                EmailReputationStrictness = _iPQualityScoreSettings.EmailReputationStrictness,
                AbuseStrictness = _iPQualityScoreSettings.AbuseStrictness,
                DeviceFingerprintEnabled = _iPQualityScoreSettings.DeviceFingerprintEnabled,
                DeviceFingerprintFraudChance = _iPQualityScoreSettings.DeviceFingerprintFraudChance,
                DeviceFingerprintTrackingCode = _iPQualityScoreSettings.DeviceFingerprintTrackingCode,
            });

            return validationResult.IsValid;
        }

        #endregion
    }
}
