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
            node.Add(new XElement("CollectPhoneNumber", item.CollectPhoneNumber));
            node.Add(new XElement("ShippingAllowedCountries", item.ShippingAllowedCountries));
            node.Add(new XElement("CollectPromotionalEmailsConsent", item.CollectPromotionalEmailsConsent));
            node.Add(new XElement("AllowPromotionalCodes", item.AllowPromotionalCodes));
            node.Add(new XElement("EnableAbandonedCartRecovery", item.EnableAbandonedCartRecovery));
            node.Add(new XElement("AllowPromotionalCodesOnRecoveredCarts", item.AllowPromotionalCodesOnRecoveredCarts));


            return Task.FromResult(SyncAttempt<XElement>.Succeed(Consts.Settings.ItemType, node, typeof(UmbCheckoutStripeSettings), ChangeType.Export));
        }

        protected override Task<SyncAttempt<UmbCheckoutStripeSettings>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
        {
            var item = new UmbCheckoutStripeSettings
            {
                Id = node.Attribute("Id").ValueOrDefault(0),
                Key = node.GetKey(),
                UseLiveApiDetails = node!.Element("UseLiveApiDetails").ValueOrDefault(false),
                CollectPhoneNumber = node!.Element("CollectPhoneNumber").ValueOrDefault(false),
                CollectPromotionalEmailsConsent = node!.Element("CollectPromotionalEmailsConsent").ValueOrDefault(false),
                AllowPromotionalCodes = node!.Element("AllowPromotionalCodes").ValueOrDefault(false),
                EnableAbandonedCartRecovery = node!.Element("EnableAbandonedCartRecovery").ValueOrDefault(false),
                AllowPromotionalCodesOnRecoveredCarts = node!.Element("AllowPromotionalCodesOnRecoveredCarts").ValueOrDefault(false),
                ShippingAllowedCountries = node!.Element("ShippingAllowedCountries")?.Value
            };

            return Task.FromResult(SyncAttempt<UmbCheckoutStripeSettings>.Succeed(Consts.Settings.ItemType, item, ChangeType.Import, Array.Empty<uSyncChange>()));
        }

        public override async Task<UmbCheckoutStripeSettings?> FindItemAsync(Guid key) =>
            await _stripeSettingsService.GetStripeSettings() ?? new UmbCheckoutStripeSettings();

        public override Task<UmbCheckoutStripeSettings?> FindItemAsync(string alias)
        {
            throw new NotImplementedException();
        }

        public override async Task SaveItemAsync(UmbCheckoutStripeSettings item) => await _stripeSettingsService.UpdateStripeSettings(item);

        public override Task DeleteItemAsync(UmbCheckoutStripeSettings item) =>
            throw new NotImplementedException();

        public override string ItemAlias(UmbCheckoutStripeSettings item) => "UmbCheckoutStripeSettings";

        public override Guid ItemKey(UmbCheckoutStripeSettings item) =>
        item.Key;
    }
}
