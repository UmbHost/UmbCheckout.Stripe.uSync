using UmbCheckout.Shared.Notifications;
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
using uSync.Core.Serialization;

namespace UmbCheckout.Stripe.uSync.Handlers
{
    [SyncHandler("umbCheckoutStripeHander", Consts.Settings.HandlerName, Consts.Settings.SerializerFolder, 1,
        Icon = "icon-settings usync-addon-icon", EntityType = Consts.Settings.EntityType)]
    public class StripeSettingsHandler : SyncHandlerRoot<UmbCheckoutStripeSettings, UmbCheckoutStripeSettings>, ISyncHandler,
        INotificationHandler<OnStripeSettingsSavedNotification>
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
            var item = await UmbCheckoutHooks.ReadStoredAsync(() => _stripeSettingsService.GetStripeSettingsAsync());

            var actions = new List<uSyncAction>();
            if (item != null)
            {
                actions.AddRange(await ExportItemAsync(item));
            }

            return actions;
        }

        private async Task<IEnumerable<uSyncAction>> ExportItemAsync(UmbCheckoutStripeSettings item)
        {
            var folder = Path.Combine(RootFolders.First(), DefaultFolder);
            var attempt = await serializer.SerializeAsync(item, new SyncSerializerOptions());
            if (attempt.Success && attempt.Item != null)
            {
                var filename = Path.Combine(folder, $"{GetItemFileName(item)}.config");
                await syncFileService.SaveXElementAsync(attempt.Item, filename);
                return new[] { uSyncAction.SetAction(true, GetItemName(item), change: ChangeType.Export, filename: filename) };
            }
            return Enumerable.Empty<uSyncAction>();
        }

        public void Handle(OnStripeSettingsSavedNotification notification)
        {
            if (!ShouldProcess()) return;

            try
            {
                if (notification.StripeSettings != null)
                {
                    ExportItemAsync(notification.StripeSettings).GetAwaiter().GetResult();
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
            => await UmbCheckoutHooks.ReadStoredAsync(() => _stripeSettingsService.GetStripeSettingsAsync()) ?? new UmbCheckoutStripeSettings();

        protected override string GetItemName(UmbCheckoutStripeSettings item)
            => item.Id.ToString();

        protected override string GetItemFileName(UmbCheckoutStripeSettings item)
            => Consts.Settings.FileName;

        private bool ShouldProcess()
        {
            if (_mutexService.IsPaused) return false;
            if (!DefaultConfig.Enabled) return false;
            return true;
        }
    }
}
