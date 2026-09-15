using Microsoft.Extensions.DependencyInjection;
using UmbCheckout.Stripe.uSync.Manifests;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;

namespace UmbCheckout.Stripe.uSync.Composers
{
    /// <summary>
    /// Registers the UmbCheckout Stripe uSync package manifest
    /// </summary>
    public class UmbCheckoutStripeuSyncComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IPackageManifestReader, UmbCheckoutStripeuSyncPackageManifestReader>();
        }
    }
}
