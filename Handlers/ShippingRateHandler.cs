using Microsoft.Extensions.Logging;
using UmbCheckout.Stripe.Interfaces;
using UmbCheckout.Stripe.Models;
using UmbCheckout.Stripe.Notifications;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;

namespace UmbCheckout.Stripe.uSync.Handlers
{
    [SyncHandler("umbCheckoutStripeShippingHander", Consts.ShippingRate.HandlerName, Consts.ShippingRate.SerializerFolder, 1,
        Icon = "icon-truck usync-addon-icon", EntityType = Consts.ShippingRate.EntityType)]
    public class ShippingRateHandler : SyncHandlerRoot<ShippingRate, ShippingRate>, ISyncHandler, 
        INotificationHandler<OnShippingRateSavedNotification>
    {
        public override string Group => Consts.Group;

        private readonly IStripeShippingRateDatabaseService _stripeShippingRateDatabaseService;

        public ShippingRateHandler(ILogger<SyncHandlerRoot<ShippingRate, ShippingRate>> logger, AppCaches appCaches, IShortStringHelper shortStringHelper, SyncFileService syncFileService, uSyncEventService mutexService, uSyncConfigService uSyncConfig, ISyncItemFactory itemFactory, IStripeShippingRateDatabaseService stripeShippingRateDatabaseService) : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
        {
            itemContainerType = UmbracoObjectTypes.Unknown;
            _stripeShippingRateDatabaseService = stripeShippingRateDatabaseService;
        }

        public override IEnumerable<uSyncAction> ExportAll(ShippingRate parent, string folder, HandlerSettings config,
            SyncUpdateCallback callback)
        {
            try
            {
                var items = _stripeShippingRateDatabaseService.GetShippingRates().Result;

                var actions = new List<uSyncAction>();
                foreach (var item in items)
                {
                    actions.AddRange(Export(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig));
                }

                return actions;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
                throw;
            }
        }

        protected override IEnumerable<uSyncAction> DeleteMissingItems(ShippingRate parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
            => Enumerable.Empty<uSyncAction>();

        protected override IEnumerable<ShippingRate> GetChildItems(ShippingRate parent)
            => Enumerable.Empty<ShippingRate>();

        protected override IEnumerable<ShippingRate> GetFolders(ShippingRate parent)
            => Enumerable.Empty<ShippingRate>();

        protected override ShippingRate GetFromService(ShippingRate item)
            => _stripeShippingRateDatabaseService.GetShippingRate(item.Id).Result ?? new ShippingRate();

        protected override string GetItemName(ShippingRate item)
            => item.Name;

        protected override string GetItemFileName(ShippingRate item)
            => item.Name.ToSafeFileName(shortStringHelper);

        private bool ShouldProcess()
        {
            if (_mutexService.IsPaused) return false;
            if (!DefaultConfig.Enabled) return false;
            return true;
        }

        public void Handle(OnShippingRateSavedNotification notification)
        {
            if (!ShouldProcess()) return;

            try
            {
                var attempts = Export(notification.ShippingRate, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
                foreach (var attempt in attempts.Where(x => x.Success))
                {
                    CleanUp(notification.ShippingRate, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
            }
        }
    }
}
