using UmbCheckout.Shared;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;

namespace UmbCheckout.Stripe.uSync
{
    public class UmbCheckoutStripeuSyncManifest : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ManifestFilters().Append<UmbCheckoutStripeuSyncManifestFilter>();
        }
    }

    public class UmbCheckoutStripeuSyncManifestFilter : IManifestFilter
    {
        public void Filter(List<PackageManifest> manifests)
        {
            manifests.Add(new PackageManifest
            {
                PackageName = $"{Shared.Consts.PackageName}.{Stripe.Consts.AppSettingsSectionName}.uSync",
                Version = UmbCheckoutVersion.Version.ToString(3),
                AllowPackageTelemetry = true,
                BundleOptions = BundleOptions.None
            });
        }
    }
}
