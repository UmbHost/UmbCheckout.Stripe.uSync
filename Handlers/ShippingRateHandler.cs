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
        INotificationAsyncHandler<OnShippingRateSavedNotification>, INotificationAsyncHandler<OnShippingRateDeletedNotification>
    {
        public override string Group => Consts.Group;

        private readonly IStripeShippingRateDatabaseService _stripeShippingRateDatabaseService;

        public ShippingRateHandler(ILogger<SyncHandlerRoot<ShippingRate, ShippingRate>> logger, AppCaches appCaches, IShortStringHelper shortStringHelper, ISyncFileService syncFileService, ISyncEventService mutexService, ISyncConfigService uSyncConfig, ISyncItemFactory itemFactory, IStripeShippingRateDatabaseService stripeShippingRateDatabaseService) : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
        {
            ItemContainerType = UmbracoObjectTypes.Unknown;
            _stripeShippingRateDatabaseService = stripeShippingRateDatabaseService;
        }

        public override async Task<IEnumerable<uSyncAction>> ExportAllAsync(ShippingRate? parent, string[] folders, HandlerSettings config,
            SyncUpdateCallback? callback)
        {
            try
            {
                var items = await _stripeShippingRateDatabaseService.GetShippingRates();

                var actions = new List<uSyncAction>();
                foreach (var item in items)
                {
                    actions.AddRange(await ExportAsync(item, folders, DefaultConfig));
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
            => Task.FromResult(Enumerable.Empty<uSyncAction>());

        protected override Task<IEnumerable<ShippingRate>> GetChildItemsAsync(ShippingRate? parent)
            => Task.FromResult(Enumerable.Empty<ShippingRate>());

        protected override Task<IEnumerable<ShippingRate>> GetFoldersAsync(ShippingRate? parent)
            => Task.FromResult(Enumerable.Empty<ShippingRate>());

        protected override async Task<ShippingRate?> GetFromServiceAsync(ShippingRate? item)
            => item is null ? null : await _stripeShippingRateDatabaseService.GetShippingRate(item.Key) ?? new ShippingRate();

        protected override string GetItemName(ShippingRate item)
            => item.Name;

        protected override string GetItemFileName(ShippingRate item)
            => item.Name.ToSafeFileName(shortStringHelper);

        public async Task HandleAsync(OnShippingRateSavedNotification notification, CancellationToken cancellationToken)
        {
            if (!ShouldProcessEvent()) return;

            try
            {
                if (notification.ShippingRate != null)
                {
                    var handlerFolders = GetDefaultHandlerFolders();
                    var attempts = await ExportAsync(notification.ShippingRate, handlerFolders, DefaultConfig);
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        if (attempt.FileName is null) continue;
                        await CleanUpAsync(notification.ShippingRate, attempt.FileName, handlerFolders.Last());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
            }
        }

        public async Task HandleAsync(OnShippingRateDeletedNotification notification, CancellationToken cancellationToken)
        {
            if (!ShouldProcessEvent()) return;

            try
            {
                if (notification.ShippingRate != null)
                {
                    var handlerFolders = GetDefaultHandlerFolders();
                    var filename = await GetPathAsync(handlerFolders.Last(), notification.ShippingRate,
                        DefaultConfig.GuidNames, DefaultConfig.UseFlatStructure);
                    var attempt = await serializer.SerializeEmptyAsync(notification.ShippingRate, SyncActionType.Delete, string.Empty);
                    if (attempt.Success)
                    {
                        await syncFileService.SaveXElementAsync(attempt.Item, filename);
                        await CleanUpAsync(notification.ShippingRate, filename, handlerFolders.Last());
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
