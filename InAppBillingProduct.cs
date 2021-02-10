namespace Plugin.InAppBilling
{
    public class InAppBillingProduct
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string ProductId { get; set; }

        public string LocalizedPrice { get; set; }

        public string CurrencyCode { get; set; }

        public long MicrosPrice { get; set; }
    }
}
