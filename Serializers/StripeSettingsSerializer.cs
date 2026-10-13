using UmbCheckout.Shared.Notifications;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using UmbCheckout.Stripe.Interfaces;
using UmbCheckout.Stripe.Models;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace UmbCheckout.Stripe.uSync.Serializers
{
    [SyncSerializer("0DB481A7-F83B-4444-B45D-FB3CC81BC488", Consts.Settings.SerializerName, Consts.Settings.ItemType)]
    public class StripeSettingsSerializer : SyncSerializerRoot<UmbCheckoutStripeSettings>, ISyncSerializer<UmbCheckoutStripeSettings>
    {
        private readonly IStripeSettingsService _stripeSettingsService;
        public StripeSettingsSerializer(ILogger<SyncSerializerRoot<UmbCheckoutStripeSettings>> logger, IStripeSettingsService stripeSettingsService) : base(logger)
        {
            _stripeSettingsService = stripeSettingsService;
        }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(UmbCheckoutStripeSettings item, SyncSerializerOptions options)
        {
            var node = new XElement(ItemType,
                new XAttribute("Id", item.Id),
                new XAttribute("Alias", ItemAlias(item)),
                new XAttribute("Key", ItemKey(item)));
            node.Add(new XElement("UseLiveApiDetails", item.UseLiveApiDetails));
            node.Add(new XElement("ShippingAllowedCountries", item.ShippingAllowedCountries ?? string.Empty));
            node.Add(new XElement("CollectPhoneNumber", item.CollectPhoneNumber));
            node.Add(new XElement("CollectPromotionalEmailsConsent", item.CollectPromotionalEmailsConsent));
            node.Add(new XElement("AllowPromotionalCodes", item.AllowPromotionalCodes));
            node.Add(new XElement("EnableAbandonedCartRecovery", item.EnableAbandonedCartRecovery));
            node.Add(new XElement("AllowPromotionalCodesOnRecoveredCarts", item.AllowPromotionalCodesOnRecoveredCarts));


            return Task.FromResult(SyncAttempt<XElement>.Succeed(Consts.Settings.ItemType, node, typeof(UmbCheckoutStripeSettings), ChangeType.Export));
        }

        protected override async Task<SyncAttempt<UmbCheckoutStripeSettings>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
        {
            // Files from older versions lack the newer settings: a setting the file doesn't have keeps the site's current value
            // (the stored settings, or the defaults on a site with none), rather than being reset.
            var current = await UmbCheckoutHooks.ReadStoredAsync(() => _stripeSettingsService.GetStripeSettingsAsync()) ?? new UmbCheckoutStripeSettings();
            bool Flag(string name, bool currentValue) => node.Element(name) is { } element ? element.ValueOrDefault(false) : currentValue;

            var item = new UmbCheckoutStripeSettings
            {
                Id = node.Attribute("Id").ValueOrDefault(0),
                Key = node.GetKey(),
                UseLiveApiDetails = Flag("UseLiveApiDetails", current.UseLiveApiDetails),
                // An empty element clears the list; a missing one keeps it.
                ShippingAllowedCountries = node.Element("ShippingAllowedCountries") is { } countries
                    ? countries.Value is { Length: > 0 } list ? list : null
                    : current.ShippingAllowedCountries,
                CollectPhoneNumber = Flag("CollectPhoneNumber", current.CollectPhoneNumber),
                CollectPromotionalEmailsConsent = Flag("CollectPromotionalEmailsConsent", current.CollectPromotionalEmailsConsent),
                AllowPromotionalCodes = Flag("AllowPromotionalCodes", current.AllowPromotionalCodes),
                EnableAbandonedCartRecovery = Flag("EnableAbandonedCartRecovery", current.EnableAbandonedCartRecovery),
                AllowPromotionalCodesOnRecoveredCarts = Flag("AllowPromotionalCodesOnRecoveredCarts", current.AllowPromotionalCodesOnRecoveredCarts)
            };

            return SyncAttempt<UmbCheckoutStripeSettings>.Succeed(Consts.Settings.ItemType, item, ChangeType.Import, Array.Empty<uSyncChange>());
        }

        public override async Task<UmbCheckoutStripeSettings?> FindItemAsync(Guid key) =>
            await UmbCheckoutHooks.ReadStoredAsync(() => _stripeSettingsService.GetStripeSettingsAsync()) ?? new UmbCheckoutStripeSettings();

        public override Task<UmbCheckoutStripeSettings?> FindItemAsync(string alias) =>
            Task.FromResult<UmbCheckoutStripeSettings?>(null);

        public override async Task SaveItemAsync(UmbCheckoutStripeSettings item)
        {
            // A refused save (e.g. a site's Saving handler cancelled it) must show as a failed import, not a success.
            if (!await _stripeSettingsService.UpdateStripeSettingsAsync(item))
            {
                throw new InvalidOperationException("The Stripe settings were not saved.");
            }
        }

        public override Task DeleteItemAsync(UmbCheckoutStripeSettings item) =>
            Task.CompletedTask;

        public override string ItemAlias(UmbCheckoutStripeSettings item) => "UmbCheckoutStripeSettings";

        public override Guid ItemKey(UmbCheckoutStripeSettings item) =>
        item.Key;
    }
}
