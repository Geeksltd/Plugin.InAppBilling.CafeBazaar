using System.Threading.Tasks;

namespace Plugin.InAppBilling
{
    public interface IInAppBillingVerifyPurchase
    {
        Task<bool> VerifyPurchase(string signedData, string signature, string productId = null, string transactionId = null);
    }
}
