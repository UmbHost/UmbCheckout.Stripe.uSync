using global::uSync.BackOffice;
using UmbCheckout.Shared.Persistence;
using UmbCheckout.Stripe.Notifications;
using UmbCheckout.Stripe.uSync.Handlers;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace UmbCheckout.Stripe.uSync.Composers
{
    public class RegisterNotifications : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationHandler<OnShippingRateSavedNotification, ShippingRateHandler>();
            builder.AddNotificationHandler<OnShippingRateDeletedNotification, ShippingRateHandler>();
            // A fresh install's first-boot import runs before the tables are migrated at startup
            builder.EnsureUmbCheckoutTablesOn<uSyncImportStartingNotification>();
        }
    }
}
