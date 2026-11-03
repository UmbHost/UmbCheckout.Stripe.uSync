using UmbCheckout.Shared.Notifications;
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
using uSync.Core.Serialization;

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
                var items = await UmbCheckoutHooks.ReadStoredAsync(() => _stripeShippingRateDatabaseService.GetShippingRatesAsync());

                var actions = new List<uSyncAction>();
                foreach (var item in items)
                {
                    actions.AddRange(await ExportItemAsync(item));
                }

                return actions;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
                throw;
            }
        }

        private async Task<IEnumerable<uSyncAction>> ExportItemAsync(ShippingRate item)
        {
            var folder = Path.Combine(RootFolders.First(), this.DefaultFolder);
            var attempt = await serializer.SerializeAsync(item, new SyncSerializerOptions());
            if (attempt.Success && attempt.Item != null)
            {
                var filename = Path.Combine(folder, $"{GetItemFileName(item)}.config");
                await syncFileService.SaveXElementAsync(attempt.Item, filename);
                return new[] { uSyncAction.SetAction(true, GetItemName(item), change: ChangeType.Export, filename: filename) };
            }
            return Enumerable.Empty<uSyncAction>();
        }

        protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(ShippingRate parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
            => Task.FromResult(Enumerable.Empty<uSyncAction>());

        protected override Task<IEnumerable<ShippingRate>> GetChildItemsAsync(ShippingRate? parent)
            => Task.FromResult(Enumerable.Empty<ShippingRate>());

        protected override Task<IEnumerable<ShippingRate>> GetFoldersAsync(ShippingRate? parent)
            => Task.FromResult(Enumerable.Empty<ShippingRate>());

        protected override async Task<ShippingRate?> GetFromServiceAsync(ShippingRate? item)
            => item != null ? await UmbCheckoutHooks.ReadStoredAsync(() => _stripeShippingRateDatabaseService.GetShippingRateAsync(item.Key)) ?? new ShippingRate() : new ShippingRate();

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

        public async Task HandleAsync(OnShippingRateSavedNotification notification, CancellationToken cancellationToken)
        {
            if (!ShouldProcess()) return;

            try
            {
                if (notification.ShippingRate != null)
                {
                    await ExportItemAsync(notification.ShippingRate);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
            }
        }

        public async Task HandleAsync(OnShippingRateDeletedNotification notification, CancellationToken cancellationToken)
        {
            if (!ShouldProcess()) return;

            try
            {
                if (notification.ShippingRate != null)
                {
                    var folder = Path.Combine(RootFolders.First(), this.DefaultFolder);
                    var filename = Path.Combine(folder, $"{GetItemFileName(notification.ShippingRate)}.config");
                    var attempt = await serializer.SerializeEmptyAsync(notification.ShippingRate, SyncActionType.Delete, string.Empty);
                    if (attempt.Success && attempt.Item != null)
                    {
                        await syncFileService.SaveXElementAsync(attempt.Item, filename);
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
