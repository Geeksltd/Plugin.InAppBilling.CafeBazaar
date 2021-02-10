using System;

namespace Plugin.InAppBilling
{
    public class InAppBillingPurchase
    {
        public string Id { get; set; }

        public DateTime TransactionDateUtc { get; set; }

        public string ProductId { get; set; }

        public bool AutoRenewing { get; set; }

        public string PurchaseToken { get; set; }

        public PurchaseState State { get; set; }

        public ConsumptionState ConsumptionState { get; set; }

        public string Payload { get; set; }

        public override string ToString() => string.Format("ProductId:{0} | AutoRenewing:{1} | State:{2} | Id:{3}", ProductId, AutoRenewing, State, Id);
    }
}
