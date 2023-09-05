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

        protected override SyncAttempt<XElement> SerializeCore(UmbCheckoutStripeSettings item, SyncSerializerOptions options)
        {
            var node = new XElement(ItemType,
                new XAttribute("Id", item.Id),
                new XAttribute("Alias", ItemAlias(item)),
                new XAttribute("Key", ItemKey(item)));
            node.Add(new XElement("UseLiveApiDetails", item.UseLiveApiDetails));


            return SyncAttempt<XElement>.Succeed(Consts.Settings.ItemType, node, typeof(UmbCheckoutStripeSettings), ChangeType.Export);
        }

        protected override SyncAttempt<UmbCheckoutStripeSettings> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var item = new UmbCheckoutStripeSettings
            {
                Id = node.Attribute("Id").ValueOrDefault(0),
                Key = node.GetKey(),
                UseLiveApiDetails = node!.Element("UseLiveApiDetails").ValueOrDefault<bool>(false)
            };

            return SyncAttempt<UmbCheckoutStripeSettings>.Succeed(Consts.Settings.ItemType, item, ChangeType.Import, Array.Empty<uSyncChange>());
        }

        public override UmbCheckoutStripeSettings FindItem(int id)
        {
            throw new NotImplementedException();
        }

        public override UmbCheckoutStripeSettings FindItem(Guid key) =>
            _stripeSettingsService.GetStripeSettings().Result ?? new UmbCheckoutStripeSettings();

        public override UmbCheckoutStripeSettings FindItem(string alias)
        {
            throw new NotImplementedException();
        }

        public override void SaveItem(UmbCheckoutStripeSettings item) => _stripeSettingsService.UpdateStripeSettings(item);

        public override void DeleteItem(UmbCheckoutStripeSettings item) =>
            throw new NotImplementedException();

        public override string ItemAlias(UmbCheckoutStripeSettings item) => "UmbCheckoutStripeSettings";

        public override Guid ItemKey(UmbCheckoutStripeSettings item) =>
        item.Key;
    }
}
