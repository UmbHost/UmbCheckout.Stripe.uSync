using Microsoft.Extensions.DependencyInjection;
using UmbCheckout.Shared;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace UmbCheckout.Stripe.uSync
{
    public class UmbCheckoutStripeuSyncManifest : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IPackageManifestReader, UmbCheckoutStripeuSyncReader>();
        }
    }

    internal sealed class UmbCheckoutStripeuSyncReader : IPackageManifestReader
    {
        public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        {
            List<PackageManifest> manifest = [
                new()
                {
                    Id = $"{Shared.Consts.PackageName}.{Stripe.Consts.AppSettingsSectionName}.uSync",
                    Name = $"{Shared.Consts.PackageName}.{Stripe.Consts.AppSettingsSectionName}.uSync",
                    AllowTelemetry = true,
                    Version = UmbCheckoutVersion.Version.ToString(3),
                    Extensions = []
                }
            ];

            return Task.FromResult(manifest.AsEnumerable());
        }
    }
}
