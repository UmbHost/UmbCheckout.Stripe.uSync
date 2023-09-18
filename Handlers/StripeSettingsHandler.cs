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
using uSync.Core;

namespace UmbCheckout.Stripe.uSync.Handlers
{
    [SyncHandler("umbCheckoutStripeHander", Consts.Settings.HandlerName, Consts.Settings.SerializerFolder, 1,
        Icon = "icon-settings usync-addon-icon", EntityType = Consts.Settings.EntityType)]
    public class StripeSettingsHandler : SyncHandlerRoot<UmbCheckoutStripeSettings, UmbCheckoutStripeSettings>, ISyncHandler,
        INotificationHandler<OnStripeSettingsSavedNotification>
    {
        public override string Group => Consts.Group;

        private readonly IStripeSettingsService _stripeSettingsService;
        public StripeSettingsHandler(ILogger<SyncHandlerRoot<UmbCheckoutStripeSettings, UmbCheckoutStripeSettings>> logger, AppCaches appCaches, IShortStringHelper shortStringHelper, SyncFileService syncFileService, uSyncEventService mutexService, uSyncConfigService uSyncConfig, ISyncItemFactory itemFactory, IStripeSettingsService stripeSettingsService) : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
        {
            _stripeSettingsService = stripeSettingsService;

            itemContainerType = UmbracoObjectTypes.Unknown;
        }

        public override IEnumerable<uSyncAction> ExportAll(UmbCheckoutStripeSettings parent, string folder, HandlerSettings config,
            SyncUpdateCallback callback)
        {
            var item = _stripeSettingsService.GetStripeSettings().Result;

            var actions = new List<uSyncAction>();
            if (item != null)
            {
                actions.AddRange(Export(item, Path.Combine(rootFolder, DefaultFolder), DefaultConfig));
            }

            return actions;
        }

        public void Handle(OnStripeSettingsSavedNotification notification)
        {
            if (!ShouldProcess()) return;

            try
            {
                if (notification.StripeSettings != null)
                {
                    var attempts = Export(notification.StripeSettings, Path.Combine(rootFolder, DefaultFolder), DefaultConfig);
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        CleanUp(notification.StripeSettings, attempt.FileName, Path.Combine(rootFolder, DefaultFolder));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "uSync Save error");
            }
        }

        protected override IEnumerable<uSyncAction> DeleteMissingItems(UmbCheckoutStripeSettings parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
            => Enumerable.Empty<uSyncAction>();

        protected override IEnumerable<UmbCheckoutStripeSettings> GetChildItems(UmbCheckoutStripeSettings parent)
            => Enumerable.Empty<UmbCheckoutStripeSettings>();

        protected override IEnumerable<UmbCheckoutStripeSettings> GetFolders(UmbCheckoutStripeSettings parent)
            => Enumerable.Empty<UmbCheckoutStripeSettings>();

        protected override UmbCheckoutStripeSettings GetFromService(UmbCheckoutStripeSettings item)
            => _stripeSettingsService.GetStripeSettings().Result ?? new UmbCheckoutStripeSettings();

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
