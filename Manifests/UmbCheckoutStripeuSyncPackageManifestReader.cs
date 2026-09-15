using UmbCheckout.Shared;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace UmbCheckout.Stripe.uSync.Manifests
{
    /// <summary>
    /// Registers the UmbCheckout Stripe uSync package manifest for telemetry purposes
    /// </summary>
    internal class UmbCheckoutStripeuSyncPackageManifestReader : IPackageManifestReader
    {
        public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        {
            IEnumerable<PackageManifest> manifests =
            [
                new()
                {
                    Id = $"{Shared.Consts.PackageName}.{Stripe.Consts.AppSettingsSectionName}.uSync",
                    Name = $"{Shared.Consts.PackageName}.{Stripe.Consts.AppSettingsSectionName}.uSync",
                    Version = UmbCheckoutVersion.Version.ToString(3),
                    AllowTelemetry = true,
                    Extensions = []
                }
            ];

            return Task.FromResult(manifests);
        }
    }
}
