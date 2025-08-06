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
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace UmbCheckout.Stripe.uSync.Handlers
{
    [SyncHandler("umbCheckoutStripeShippingHander", Consts.ShippingRate.HandlerName, Consts.ShippingRate.SerializerFolder, 1,
        Icon = "icon-truck usync-addon-icon", EntityType = Consts.ShippingRate.EntityType)]
    public class ShippingRateHandler : SyncHandlerRoot<ShippingRate, ShippingRate>, ISyncHandler, 
        INotificationHandler<OnShippingRateSavedNotification>, INotificationHandler<OnShippingRateDeletedNotification>
    {
        public override string Group => Consts.Group;

        private readonly IStripeShippingRateDatabaseService _stripeShippingRateDatabaseService;

        public ShippingRateHandler(ILogger<SyncHandlerRoot<ShippingRate, ShippingRate>> logger, AppCaches appCaches, IShortStringHelper shortStringHelper, ISyncFileService syncFileService, ISyncEventService mutexService, ISyncConfigService uSyncConfig, ISyncItemFactory itemFactory, IStripeShippingRateDatabaseService stripeShippingRateDatabaseService) : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
        {
            ItemContainerType = UmbracoObjectTypes.Unknown;
            _stripeShippingRateDatabaseService = stripeShippingRateDatabaseService;
        }
        public override async Task<IEnumerable<uSyncAction>> ExportAllAsync(string[] folders, HandlerSettings settings, SyncUpdateCallback? callback)
        {
            try
            {
                var items = _stripeShippingRateDatabaseService.GetShippingRates().Result;

                var actions = new List<uSyncAction>();
                foreach (var item in items)
                {
                    actions.AddRange(await ExportAsync(item, RootFolders, DefaultConfig));
                }

                return actions;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
                throw;
            }
        }

        protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(ShippingRate parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
            => Task.FromResult<IEnumerable<uSyncAction>>([]);

        protected override Task<IEnumerable<ShippingRate>> GetChildItemsAsync(ShippingRate? parent)
            => Task.FromResult<IEnumerable<ShippingRate>>([]);

        protected override Task<IEnumerable<ShippingRate>> GetFoldersAsync(ShippingRate? parent)
            => Task.FromResult<IEnumerable<ShippingRate>>([]);

        protected override async Task<ShippingRate?> GetFromServiceAsync(ShippingRate? item)
            => await _stripeShippingRateDatabaseService.GetShippingRate(item.Key) ?? new ShippingRate();

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

        public async void Handle(OnShippingRateSavedNotification notification)
        {
            if (!ShouldProcess()) return;

            try
            {
                if (notification.ShippingRate != null)
                {
                    var attempts = await ExportAsync(notification.ShippingRate, RootFolders, DefaultConfig);
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        await CleanUpAsync(notification.ShippingRate, attempt.FileName, DefaultFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
            }
        }

        public async void Handle(OnShippingRateDeletedNotification notification)
        {
            if (!ShouldProcess()) return;

            try
            {
                if (notification.ShippingRate != null)
                {
                    var filename = await GetPathAsync(DefaultFolder, notification.ShippingRate,
                        DefaultConfig.GuidNames, DefaultConfig.UseFlatStructure);
                    var attempt = await serializer.SerializeEmptyAsync(notification.ShippingRate, SyncActionType.Delete, string.Empty);
                    if (attempt.Success)
                    {
                        await syncFileService.SaveXElementAsync(attempt.Item, filename);
                        await CleanUpAsync(notification.ShippingRate, filename, DefaultFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
            }
        }
    }
}
