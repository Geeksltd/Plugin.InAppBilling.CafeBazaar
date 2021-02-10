namespace Plugin.InAppBilling
{
    public enum PurchaseState
    {
        Purchased,
        Canceled,
        Refunded,
        Purchasing,
        Failed,
        Restored,
        Deferred,
        FreeTrial,
        PaymentPending,
        Unknown,
    }
}
