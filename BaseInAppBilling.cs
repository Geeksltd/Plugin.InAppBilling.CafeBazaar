using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.InAppBilling
{
    public abstract class BaseInAppBilling : IInAppBilling, IDisposable
    {
        bool disposed = false;

        public abstract bool InTestingMode { get; set; }

        public abstract Task<bool> ConnectAsync();

        public abstract Task DisconnectAsync();

        public abstract Task<IEnumerable<InAppBillingProduct>> GetProductInfoAsync(ItemType itemType, params string[] productIds);

        public abstract Task<IEnumerable<InAppBillingPurchase>> GetPurchasesAsync(ItemType itemType, IInAppBillingVerifyPurchase verifyPurchase = null);

        public abstract Task<InAppBillingPurchase> PurchaseAsync(string productId, ItemType itemType, string payload, IInAppBillingVerifyPurchase verifyPurchase = null);

        public abstract Task<InAppBillingPurchase> ConsumePurchaseAsync(string productId, string purchaseToken);

        public abstract Task<InAppBillingPurchase> ConsumePurchaseAsync(string productId, ItemType itemType, string payload, IInAppBillingVerifyPurchase verifyPurchase = null);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseInAppBilling() => Dispose(false);

        public virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (!disposing) ;
            disposed = true;
        }
    }
}
