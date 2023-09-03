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
        }
    }
}
