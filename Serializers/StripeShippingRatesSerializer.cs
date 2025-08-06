using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using UmbCheckout.Stripe.Interfaces;
using UmbCheckout.Stripe.Models;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace UmbCheckout.Stripe.uSync.Serializers
{
    [SyncSerializer("23708103-0827-4937-B8B9-87D72764B821", Consts.ShippingRate.SerializerName, Consts.ShippingRate.ItemType)]
    public class StripeShippingRatesSerializer : SyncSerializerRoot<ShippingRate>, ISyncSerializer<ShippingRate>
    {
        private readonly IStripeShippingRateDatabaseService _stripeShippingRateDatabaseService;
        private readonly IShortStringHelper _shortStringHelper;
        public StripeShippingRatesSerializer(ILogger<SyncSerializerRoot<ShippingRate>> logger, IStripeShippingRateDatabaseService stripeShippingRateDatabaseService, IShortStringHelper shortStringHelper) : base(logger)
        {
            _stripeShippingRateDatabaseService = stripeShippingRateDatabaseService;
            _shortStringHelper = shortStringHelper;
        }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(ShippingRate item, SyncSerializerOptions options)
        {
            var node = new XElement(ItemType,
                new XAttribute("Id", item.Id),
                new XAttribute("Alias", ItemAlias(item)),
                new XAttribute("Key", ItemKey(item)));
            node.Add(new XElement("Name", item.Name));
            node.Add(new XElement("Value", item.Value));


            return Task.FromResult(SyncAttempt<XElement>.Succeed(item.Name, node, typeof(ShippingRate), ChangeType.Export));
        }

        protected override Task<SyncAttempt<ShippingRate>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
        {
            var item = new ShippingRate
            {
                Name = node!.Element("Name").ValueOrDefault<string>(string.Empty),
                Value = node!.Element("Value").ValueOrDefault<string>(string.Empty),
                Id = node.Attribute("Id").ValueOrDefault(0),
                Key = node.GetKey()
            };

            return Task.FromResult(SyncAttempt<ShippingRate>.Succeed(Consts.ShippingRate.ItemType, item, ChangeType.Import, Array.Empty<uSyncChange>()));
        }

        public override async Task<ShippingRate?> FindItemAsync(Guid key) =>
            await _stripeShippingRateDatabaseService.GetShippingRate(key) ?? new ShippingRate();

        public override Task<ShippingRate?> FindItemAsync(string alias)
        {
            throw new NotImplementedException();
        }

        public override async Task SaveItemAsync(ShippingRate item) => await _stripeShippingRateDatabaseService.UpdateShippingRate(item);

        public override async Task DeleteItemAsync(ShippingRate item) =>
            await _stripeShippingRateDatabaseService.DeleteShippingRate(item.Key);

        public override string ItemAlias(ShippingRate item) => item.Name.ToSafeAlias(_shortStringHelper);

        public override Guid ItemKey(ShippingRate item) =>
        item.Key;
    }
}
