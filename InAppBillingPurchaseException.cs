using System;

namespace Plugin.InAppBilling
{
    public class InAppBillingPurchaseException : Exception
    {
        public PurchaseError PurchaseError { get; }

        public InAppBillingPurchaseException(PurchaseError error, Exception ex) : base("Unable to process purchase.", ex) => PurchaseError = error;

        public InAppBillingPurchaseException(PurchaseError error) : base("Unable to process purchase.") => PurchaseError = error;

        public InAppBillingPurchaseException(PurchaseError error, string message) : base(message) => PurchaseError = error;
    }
}
