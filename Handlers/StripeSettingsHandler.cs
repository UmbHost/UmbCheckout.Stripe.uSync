using Microsoft.Extensions.Logging;
using UmbCheckout.Stripe.Interfaces;
using UmbCheckout.Stripe.Models;
using UmbCheckout.Stripe.Notifications;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace UmbCheckout.Stripe.uSync.Handlers
{
    [SyncHandler("umbCheckoutStripeHander", Consts.Settings.HandlerName, Consts.Settings.SerializerFolder, 1,
        Icon = "icon-settings usync-addon-icon", EntityType = Consts.Settings.EntityType)]
    public class StripeSettingsHandler : SyncHandlerRoot<UmbCheckoutStripeSettings, UmbCheckoutStripeSettings>, ISyncHandler,
        INotificationAsyncHandler<OnStripeSettingsSavedNotification>
    {
        public override string Group => Consts.Group;

        private readonly IStripeSettingsService _stripeSettingsService;
        public StripeSettingsHandler(ILogger<SyncHandlerRoot<UmbCheckoutStripeSettings, UmbCheckoutStripeSettings>> logger, AppCaches appCaches, IShortStringHelper shortStringHelper, ISyncFileService syncFileService, ISyncEventService mutexService, ISyncConfigService uSyncConfig, ISyncItemFactory itemFactory, IStripeSettingsService stripeSettingsService) : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
        {
            _stripeSettingsService = stripeSettingsService;

            ItemContainerType = UmbracoObjectTypes.Unknown;
        }

        public override async Task<IEnumerable<uSyncAction>> ExportAllAsync(UmbCheckoutStripeSettings? parent, string[] folders, HandlerSettings config,
            SyncUpdateCallback? callback)
        {
            var item = await _stripeSettingsService.GetStripeSettings();

            var actions = new List<uSyncAction>();
            if (item != null)
            {
                actions.AddRange(await ExportAsync(item, folders, config));
            }

            return actions;
        }

        public async Task HandleAsync(OnStripeSettingsSavedNotification notification, CancellationToken cancellationToken)
        {
            if (!ShouldProcessEvent()) return;

            try
            {
                if (notification.StripeSettings != null)
                {
                    var handlerFolders = GetDefaultHandlerFolders();
                    var attempts = await ExportAsync(notification.StripeSettings, handlerFolders, DefaultConfig);
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        if (attempt.FileName is null) continue;
                        await CleanUpAsync(notification.StripeSettings, attempt.FileName, handlerFolders.Last());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
            }
        }

        protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(UmbCheckoutStripeSettings parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
            => Task.FromResult(Enumerable.Empty<uSyncAction>());

        protected override Task<IEnumerable<UmbCheckoutStripeSettings>> GetChildItemsAsync(UmbCheckoutStripeSettings? parent)
            => Task.FromResult(Enumerable.Empty<UmbCheckoutStripeSettings>());

        protected override Task<IEnumerable<UmbCheckoutStripeSettings>> GetFoldersAsync(UmbCheckoutStripeSettings? parent)
            => Task.FromResult(Enumerable.Empty<UmbCheckoutStripeSettings>());

        protected override async Task<UmbCheckoutStripeSettings?> GetFromServiceAsync(UmbCheckoutStripeSettings? item)
            => await _stripeSettingsService.GetStripeSettings() ?? new UmbCheckoutStripeSettings();

        protected override string GetItemName(UmbCheckoutStripeSettings item)
            => item.Id.ToString();

        protected override string GetItemFileName(UmbCheckoutStripeSettings item)
            => Consts.Settings.FileName;
    }
}
