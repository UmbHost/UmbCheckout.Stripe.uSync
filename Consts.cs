namespace UmbCheckout.Stripe.uSync
{
    internal static class Consts
    {
        public const string Group = "UmbCheckout";

        public static class Settings
        {
            public const string ItemType = "Settings";

            public const string SerializerName = "UmbCheckout Stripe Settings Serializer";

            public const string HandlerName = "Stripe Settings";

            public const string SerializerFolder = "UmbCheckoutStripeSettings";

            public const string EntityType = "UmbCheckout-StripeSettings";

            public const string FileName = "UmbCheckoutStripeSettings";
        }

        public static class ShippingRate
        {
            public const string ItemType = "ShippingRate";

            public const string SerializerName = "UmbCheckout Stripe Shipping Rates Serializer";

            public const string HandlerName = "Stripe Shipping Rate";

            public const string SerializerFolder = "UmbCheckoutStripeShippingRates";

            public const string EntityType = "UmbCheckout-StripeShipping";
        }
    }
}
