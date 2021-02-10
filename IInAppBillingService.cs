using Android.OS;
using Android.Runtime;
using Java.Interop;
using System;

namespace Plugin.InAppBilling
{
    public interface IInAppBillingService : IInterface, IJavaObject, IDisposable, IJavaPeerable
    {
        int IsBillingSupported(int apiVersion, string packageName, string type);

        Bundle GetSkuDetails(int apiVersion, string packageName, string type, Bundle skusBundle);

        Bundle GetBuyIntent(int apiVersion, string packageName, string sku, string type, string developerPayload);

        Bundle GetPurchases(int apiVersion, string packageName, string type, string continuationToken);

        int ConsumePurchase(int apiVersion, string packageName, string purchaseToken);
    }
}
