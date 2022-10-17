using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Java.Interop;
using Java.Lang;
using Java.Security;
using Java.Security.Spec;
using Newtonsoft.Json;
using Plugin.CurrentActivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Plugin.InAppBilling
{
    [Preserve(AllMembers = true)]
    public class InAppBillingImplementation : BaseInAppBilling
    {
        const string SKU_DETAILS_LIST = "DETAILS_LIST";
        const string SKU_ITEM_ID_LIST = "ITEM_ID_LIST";
        const string ITEM_TYPE_INAPP = "inapp";
        const string ITEM_TYPE_SUBSCRIPTION = "subs";
        const string RESPONSE_CODE = "RESPONSE_CODE";
        const string RESPONSE_BUY_INTENT = "BUY_INTENT";
        const string RESPONSE_IAP_DATA = "INAPP_PURCHASE_DATA";
        const string RESPONSE_IAP_DATA_SIGNATURE = "INAPP_DATA_SIGNATURE";
        const string RESPONSE_IAP_DATA_SIGNATURE_LIST = "INAPP_DATA_SIGNATURE_LIST";
        const string RESPONSE_IAP_PURCHASE_ITEM_LIST = "INAPP_PURCHASE_ITEM_LIST";
        const string RESPONSE_IAP_PURCHASE_DATA_LIST = "INAPP_PURCHASE_DATA_LIST";
        const string RESPONSE_IAP_CONTINUATION_TOKEN = "INAPP_CONTINUATION_TOKEN";
        const int PURCHASE_REQUEST_CODE = 1001;
        const int RESPONSE_CODE_RESULT_USER_CANCELED = 1;
        const int RESPONSE_CODE_RESULT_SERVICE_UNAVAILABLE = 2;
        InAppBillingServiceConnection serviceConnection;
        static TaskCompletionSource<PurchaseResponse> tcsPurchase;

        Activity Context => CrossCurrentActivity.Current.Activity;

        public override bool InTestingMode { get; set; }

        public override async Task<IEnumerable<InAppBillingProduct>> GetProductInfoAsync(ItemType itemType, params string[] productIds)
        {
            if (serviceConnection.Service == null)
                throw new InAppBillingPurchaseException(PurchaseError.BillingUnavailable, "You are not connected to the Google Play App store.");

            var products = (IEnumerable<Product>)null;
            var itemType1 = itemType;

            switch (itemType1)
            {
                case ItemType.InAppPurchase:
                    products = await GetProductInfoAsync(productIds, "inapp");
                    break;
                case ItemType.Subscription:
                    products = await GetProductInfoAsync(productIds, "subs");
                    break;
            }

            IEnumerable<InAppBillingProduct> appBillingProducts = products != null ? products.Select(product => new InAppBillingProduct()
            {
                Name = product.Title,
                Description = product.Description,
                CurrencyCode = product.CurrencyCode,
                LocalizedPrice = product.Price,
                ProductId = product.ProductId,
                MicrosPrice = product.MicrosPrice
            }) : null;

            products = null;

            return appBillingProducts;
        }

        Task<IEnumerable<Product>> GetProductInfoAsync(string[] productIds, string itemType)
        {
            return Task.Factory.StartNew<IEnumerable<Product>>(() =>
           {
               Bundle skusBundle = new Bundle();
               skusBundle.PutStringArrayList("ITEM_ID_LIST", productIds);
               Bundle skuDetails = serviceConnection.Service.GetSkuDetails(3, Context.PackageName, itemType, skusBundle);
               if (!skuDetails.ContainsKey("DETAILS_LIST"))
                   return null;
               IList<string> stringArrayList = skuDetails.GetStringArrayList("DETAILS_LIST");
               if (stringArrayList == null || !stringArrayList.Any())
                   return null;
               List<Product> productList = new List<Product>(stringArrayList.Count);
               foreach (string str in stringArrayList)
                   productList.Add(JsonConvert.DeserializeObject<Product>(str));
               return productList;
           });
        }

        public override async Task<IEnumerable<InAppBillingPurchase>> GetPurchasesAsync(ItemType itemType, IInAppBillingVerifyPurchase verifyPurchase = null)
        {
            if (serviceConnection.Service == null)
                throw new InAppBillingPurchaseException(PurchaseError.BillingUnavailable, "You are not connected to the Google Play App store.");

            List<Purchase> purchases = null;
            ItemType itemType1 = itemType;

            switch (itemType1)
            {
                case ItemType.InAppPurchase:
                    purchases = await GetPurchasesAsync("inapp", verifyPurchase);
                    break;
                case ItemType.Subscription:
                    purchases = await GetPurchasesAsync("subs", verifyPurchase);
                    break;
            }

            if (purchases == null)
                return null;

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var results = purchases.Select(p =>
            {
                InAppBillingPurchase appBillingPurchase = new InAppBillingPurchase();
                appBillingPurchase.TransactionDateUtc = epoch + TimeSpan.FromMilliseconds(p.PurchaseTime);
                appBillingPurchase.Id = p.OrderId;
                appBillingPurchase.ProductId = p.ProductId;
                appBillingPurchase.AutoRenewing = p.AutoRenewing;
                appBillingPurchase.PurchaseToken = p.PurchaseToken;
                appBillingPurchase.State = itemType == ItemType.InAppPurchase ? p.State : p.SubscriptionState;
                appBillingPurchase.ConsumptionState = p.ConsumedState;
                appBillingPurchase.Payload = p.DeveloperPayload ?? string.Empty;
                return appBillingPurchase;
            });

            return results;
        }

        Task<List<Purchase>> GetPurchasesAsync(string itemType, IInAppBillingVerifyPurchase verifyPurchase)
        {
            return Task.Run(async () =>
            {
                string continuationToken = string.Empty;
                List<Purchase> purchases = new List<Purchase>();
                do
                {
                    Bundle ownedItems = serviceConnection.Service.GetPurchases(3, Context.PackageName, itemType, null);
                    int response = GetResponseCodeFromBundle(ownedItems);
                    if ((uint)response <= 0U)
                    {
                        if (!ValidOwnedItems(ownedItems))
                        {
                            Console.WriteLine("Invalid purchases");
                            return purchases;
                        }
                        IList<string> items = ownedItems.GetStringArrayList("INAPP_PURCHASE_ITEM_LIST");
                        IList<string> dataList = ownedItems.GetStringArrayList("INAPP_PURCHASE_DATA_LIST");
                        IList<string> signatures = ownedItems.GetStringArrayList("INAPP_DATA_SIGNATURE_LIST");
                        for (int i = 0; i < items.Count; ++i)
                        {
                            string data = dataList[i];
                            string sign = signatures[i];
                            bool flag = verifyPurchase == null;
                            if (!flag)
                                flag = await verifyPurchase.VerifyPurchase(data, sign, null, null);
                            if (flag)
                            {
                                Purchase purchase = JsonConvert.DeserializeObject<Purchase>(data);
                                purchases.Add(purchase);
                                purchase = null;
                            }
                            data = null;
                            sign = null;
                        }
                        continuationToken = ownedItems.GetString("INAPP_CONTINUATION_TOKEN");
                        ownedItems = null;
                        items = null;
                        dataList = null;
                        signatures = null;
                    }
                    else
                        break;
                }
                while (!string.IsNullOrWhiteSpace(continuationToken));
                return purchases;
            });
        }

        public override async Task<InAppBillingPurchase> PurchaseAsync(string productId, ItemType itemType, string payload, IInAppBillingVerifyPurchase verifyPurchase = null)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload), "Payload can not be null");
            if (serviceConnection.Service == null)
                throw new InAppBillingPurchaseException(PurchaseError.BillingUnavailable, "You are not connected to the Google Play App store.");
            Purchase purchase = null;
            ItemType itemType1 = itemType;
            switch (itemType1)
            {
                case ItemType.InAppPurchase:
                    purchase = await PurchaseAsync(productId, "inapp", payload, verifyPurchase);
                    break;
                case ItemType.Subscription:
                case ItemType.Voucher:
                    purchase = await PurchaseAsync(productId, "subs", payload, verifyPurchase);
                    break;
            }
            if (purchase == null)
                return null;
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            InAppBillingPurchase appBillingPurchase = new InAppBillingPurchase();
            appBillingPurchase.TransactionDateUtc = epoch + TimeSpan.FromMilliseconds(purchase.PurchaseTime);
            appBillingPurchase.Id = purchase.OrderId;
            appBillingPurchase.AutoRenewing = purchase.AutoRenewing;
            appBillingPurchase.PurchaseToken = purchase.PurchaseToken;
            appBillingPurchase.State = itemType == ItemType.InAppPurchase ? purchase.State : purchase.SubscriptionState;
            appBillingPurchase.ConsumptionState = purchase.ConsumedState;
            appBillingPurchase.ProductId = purchase.ProductId;
            appBillingPurchase.Payload = purchase.DeveloperPayload ?? string.Empty;
            return appBillingPurchase;
        }

        async Task<Purchase> PurchaseAsync(string productSku, string itemType, string payload, IInAppBillingVerifyPurchase verifyPurchase)
        {
            if (tcsPurchase != null && !tcsPurchase.Task.IsCompleted)
                return null;
            tcsPurchase = new TaskCompletionSource<PurchaseResponse>();
            Bundle buyIntentBundle = serviceConnection.Service.GetBuyIntent(3, Context.PackageName, productSku, itemType, payload);
            int responseCodeFromBundle = GetResponseCodeFromBundle(buyIntentBundle);
            List<Purchase> purchases1;
            Purchase purchase1;
            switch (responseCodeFromBundle)
            {
                case 1:
                    throw new InAppBillingPurchaseException(PurchaseError.UserCancelled);
                case 2:
                    throw new InAppBillingPurchaseException(PurchaseError.ServiceUnavailable);
                case 3:
                    throw new InAppBillingPurchaseException(PurchaseError.BillingUnavailable);
                case 4:
                    throw new InAppBillingPurchaseException(PurchaseError.ItemUnavailable);
                case 5:
                    throw new InAppBillingPurchaseException(PurchaseError.DeveloperError);
                case 6:
                    throw new InAppBillingPurchaseException(PurchaseError.GeneralError);
                case 7:
                    purchases1 = await GetPurchasesAsync(itemType, verifyPurchase);
                    purchase1 = purchases1.FirstOrDefault(p => p.ProductId == productSku && payload.Equals(p.DeveloperPayload));
                    return purchase1;
                default:
                    purchases1 = null;
                    purchase1 = null;
                    if (buyIntentBundle.GetParcelable("BUY_INTENT") is PendingIntent pendingIntent)
                        Context.StartIntentSenderForResult(pendingIntent.IntentSender, 1001, new Intent(), 0, 0, 0);
                    PurchaseResponse result = await tcsPurchase.Task;
                    if (result == null)
                        return null;
                    string data = result.PurchaseData;
                    string sign = result.DataSignature;
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        List<Purchase> purchases2 = await GetPurchasesAsync(itemType, verifyPurchase);
                        Purchase purchase2 = purchases2.FirstOrDefault(p => p.ProductId == productSku && payload.Equals(p.DeveloperPayload));
                        return purchase2;
                    }
                    bool flag = verifyPurchase == null;
                    if (!flag)
                        flag = await verifyPurchase.VerifyPurchase(data, sign, null, null);
                    if (flag)
                    {
                        Purchase purchase2 = JsonConvert.DeserializeObject<Purchase>(data);
                        if (purchase2.ProductId == productSku && payload.Equals(purchase2.DeveloperPayload))
                            return purchase2;
                        purchase2 = null;
                    }
                    return null;
            }
        }

        public override Task<bool> ConnectAsync()
        {
            serviceConnection = new InAppBillingServiceConnection(Context);
            return serviceConnection.ConnectAsync();
        }

        public override async Task DisconnectAsync()
        {
            try
            {
                if (serviceConnection == null)
                    return;
                await serviceConnection.DisconnectAsync();
                serviceConnection.Dispose();
                serviceConnection = null;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Unable to disconned: " + ex.Message);
            }
        }

        public override Task<InAppBillingPurchase> ConsumePurchaseAsync(string productId, string purchaseToken)
        {
            if (serviceConnection.Service == null)
                throw new InAppBillingPurchaseException(PurchaseError.BillingUnavailable, "You are not connected to the Google Play App store.");
            if (!ParseConsumeResult(serviceConnection.Service.ConsumePurchase(3, Context.PackageName, purchaseToken)))
                return null;
            InAppBillingPurchase result = new InAppBillingPurchase();
            result.Id = string.Empty;
            result.PurchaseToken = purchaseToken;
            result.State = PurchaseState.Purchased;
            result.ConsumptionState = ConsumptionState.Consumed;
            result.AutoRenewing = false;
            result.Payload = string.Empty;
            result.ProductId = productId;
            result.TransactionDateUtc = DateTime.UtcNow;
            return Task.FromResult(result);
        }

        bool ParseConsumeResult(int response)
        {
            switch (response)
            {
                case 0:
                    return true;
                case 1:
                    throw new InAppBillingPurchaseException(PurchaseError.UserCancelled);
                case 2:
                    throw new InAppBillingPurchaseException(PurchaseError.ServiceUnavailable);
                case 3:
                    throw new InAppBillingPurchaseException(PurchaseError.BillingUnavailable);
                case 4:
                    throw new InAppBillingPurchaseException(PurchaseError.ItemUnavailable);
                case 5:
                    throw new InAppBillingPurchaseException(PurchaseError.DeveloperError);
                case 6:
                    throw new InAppBillingPurchaseException(PurchaseError.GeneralError);
                default:
                    return false;
            }
        }

        public override async Task<InAppBillingPurchase> ConsumePurchaseAsync(string productId, ItemType itemType, string payload, IInAppBillingVerifyPurchase verifyPurchase)
        {
            if (serviceConnection.Service == null)
                throw new InAppBillingPurchaseException(PurchaseError.BillingUnavailable, "You are not connected to the Google Play App store.");
            if (payload == null)
                throw new ArgumentNullException(nameof(payload), "Payload can not be null");
            IEnumerable<InAppBillingPurchase> purchases = await GetPurchasesAsync(itemType, verifyPurchase);
            InAppBillingPurchase purchase = purchases.FirstOrDefault(p => p.ProductId == productId && p.Payload == payload);
            if (purchase == null)
            {
                Console.WriteLine("Unable to find a purchase with matching product id and payload");
                return null;
            }
            int response = serviceConnection.Service.ConsumePurchase(3, Context.PackageName, purchase.PurchaseToken);
            bool result = ParseConsumeResult(response);
            return result ? purchase : null;
        }

        public static void HandleActivityResult(int requestCode, Result resultCode, Intent data)
        {
            try
            {
                if (1001 != requestCode || data == null)
                    return;
                int intExtra = data.GetIntExtra("RESPONSE_CODE", 0);
                switch (intExtra)
                {
                    case 0:
                        string stringExtra1 = data.GetStringExtra("INAPP_PURCHASE_DATA");
                        string stringExtra2 = data.GetStringExtra("INAPP_DATA_SIGNATURE");
                        TaskCompletionSource<PurchaseResponse> tcsPurchase = InAppBillingImplementation.tcsPurchase;
                        if (tcsPurchase == null)
                            break;
                        tcsPurchase.TrySetResult(new PurchaseResponse()
                        {
                            PurchaseData = stringExtra1,
                            DataSignature = stringExtra2
                        });
                        break;
                    case 1:
                        InAppBillingImplementation.tcsPurchase.SetException(new InAppBillingPurchaseException(PurchaseError.UserCancelled));
                        break;
                    case 2:
                        InAppBillingImplementation.tcsPurchase.SetException(new InAppBillingPurchaseException(PurchaseError.ServiceUnavailable));
                        break;
                    default:
                        InAppBillingImplementation.tcsPurchase.SetException(new InAppBillingPurchaseException(PurchaseError.GeneralError, intExtra.ToString()));
                        break;
                }
            }
            finally
            {
                if (!InAppBillingImplementation.tcsPurchase.Task.IsCompleted)
                    InAppBillingImplementation.tcsPurchase.SetResult(null);
            }
        }

        static bool ValidOwnedItems(Bundle purchased) => purchased.ContainsKey("INAPP_PURCHASE_ITEM_LIST") && purchased.ContainsKey("INAPP_PURCHASE_DATA_LIST") && purchased.ContainsKey("INAPP_DATA_SIGNATURE_LIST");

        static int GetResponseCodeFromBundle(Bundle bunble)
        {
            object obj = bunble.Get("RESPONSE_CODE");
            if (obj == null)
                return 0;
            return obj is Number ? ((Number)obj).IntValue() : 6;
        }

        [Preserve(AllMembers = true)]
        class PurchaseResponse
        {
            public string PurchaseData { get; set; }

            public string DataSignature { get; set; }
        }

        [Preserve(AllMembers = true)]
        class InAppBillingServiceConnection : Java.Lang.Object, IServiceConnection, IJavaObject, IDisposable, IJavaPeerable
        {
            TaskCompletionSource<bool> tcsConnect;

            public InAppBillingServiceConnection(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) => Context = Application.Context;

            public InAppBillingServiceConnection() => Context = Application.Context;

            public InAppBillingServiceConnection(Context context) => Context = context;

            public Context Context { get; set; }

            public IInAppBillingService Service { get; set; }

            public bool IsConnected { get; set; }

            public Task<bool> ConnectAsync()
            {
                if (IsConnected)
                    return Task.FromResult(true);
                tcsConnect = new TaskCompletionSource<bool>();
                Intent intent = new Intent("ir.cafebazaar.pardakht.InAppBillingService.BIND");
                intent.SetPackage("com.farsitel.bazaar");
                if (!Context.PackageManager.QueryIntentServices(intent, 0).Any())
                    return Task.FromResult(false);
                Context.BindService(intent, this, Bind.AutoCreate);
                return tcsConnect.Task;
            }

            public Task DisconnectAsync()
            {
                if (!IsConnected)
                    return Task.CompletedTask;
                Context.UnbindService(this);
                IsConnected = false;
                Service = null;
                return Task.CompletedTask;
            }

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                Service = IInAppBillingServiceStub.AsInterface(service);
                if (Service.IsBillingSupported(3, Context.PackageName, "subs") == 0)
                {
                    IsConnected = true;
                    tcsConnect.TrySetResult(true);
                }
                else
                    tcsConnect.TrySetResult(false);
            }

            public void OnServiceDisconnected(ComponentName name)
            {
            }
        }

        [Preserve(AllMembers = true)]
        class Product
        {
            [JsonConstructor]
            public Product()
            {
            }

            [JsonProperty(PropertyName = "title")]
            public string Title { get; set; }

            string _Price;
            [JsonProperty(PropertyName = "price")]
            public string Price
            {
                get
                {
                    return _Price;
                }
                set
                {
                    _Price = value;
                    if (long.TryParse(ExtractNumbers(PersianToEnglishNumberCharacter(_Price)), out long price))
                    {
                        MicrosPrice = price * 100000;
                    }
                }
            }

            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "description")]
            public string Description { get; set; }

            [JsonProperty(PropertyName = "productId")]
            public string ProductId { get; set; }

            [JsonProperty(PropertyName = "price_currency_code")]
            public string CurrencyCode { get; set; } = "IRR";

            [JsonProperty(PropertyName = "price_amount_micros")]
            public long MicrosPrice { get; set; }

            public override string ToString() => string.Format("[Product: Title={0}, Price={1}, Type={2}, Description={3}, ProductId={4}]", Title, Price, Type, Description, ProductId);

            static string PersianToEnglishNumberCharacter(string persianNumbers)
            {
                string EnglishNumbers = "";

                for (int i = 0; i < persianNumbers.Length; i++)
                {
                    if (char.IsDigit(persianNumbers[i]))
                    {
                        EnglishNumbers += char.GetNumericValue(persianNumbers, i);
                    }
                    else
                    {
                        EnglishNumbers += persianNumbers[i].ToString();
                    }
                }
                return EnglishNumbers;
            }
            static string ExtractNumbers(string text)
            {
                if (text == null)
                    return text;
                return string.Join(string.Empty, Regex.Matches(text, @"\d+").OfType<Match>().Select(m => m.Value));
            }
        }

        [Preserve(AllMembers = true)]
        class Purchase
        {
            [JsonConstructor]
            public Purchase()
            {
            }

            [JsonProperty(PropertyName = "autoRenewing")]
            public bool AutoRenewing { get; set; }

            [JsonProperty(PropertyName = "packageName")]
            public string PackageName { get; set; }

            [JsonProperty(PropertyName = "orderId")]
            public string OrderId { get; set; }

            [JsonProperty(PropertyName = "productId")]
            public string ProductId { get; set; }

            [JsonProperty(PropertyName = "developerPayload")]
            public string DeveloperPayload { get; set; }

            [JsonProperty(PropertyName = "purchaseTime")]
            public long PurchaseTime { get; set; }

            [JsonProperty(PropertyName = "purchaseState")]
            public int PurchaseState { get; set; }

            [JsonProperty(PropertyName = "purchaseToken")]
            public string PurchaseToken { get; set; }

            [JsonProperty(PropertyName = "consumptionState")]
            public int ConsumptionState { get; set; }

            [JsonProperty(PropertyName = "paymentState")]
            public int PaymentState { get; set; }

            [JsonIgnore]
            public PurchaseState State
            {
                get
                {
                    if (PurchaseState == 0)
                        return InAppBilling.PurchaseState.Purchased;
                    if (PurchaseState == 1)
                        return InAppBilling.PurchaseState.Canceled;
                    return PurchaseState == 2 ? InAppBilling.PurchaseState.Refunded : InAppBilling.PurchaseState.Unknown;
                }
            }

            [JsonIgnore]
            public ConsumptionState ConsumedState => ConsumptionState != 0 ? InAppBilling.ConsumptionState.Consumed : InAppBilling.ConsumptionState.NoYetConsumed;

            [JsonIgnore]
            public PurchaseState SubscriptionState
            {
                get
                {
                    if (PaymentState == 0)
                        return InAppBilling.PurchaseState.PaymentPending;
                    if (PaymentState == 1)
                        return InAppBilling.PurchaseState.Purchased;
                    return PaymentState == 2 ? InAppBilling.PurchaseState.FreeTrial : InAppBilling.PurchaseState.Unknown;
                }
            }

            public override string ToString() => string.Format("[Purchase: PackageName={0}, OrderId={1}, ProductId={2}, DeveloperPayload={3}, PurchaseTime={4}, PurchaseState={5}, PurchaseToken={6}]", PackageName, OrderId, ProductId, DeveloperPayload, PurchaseTime, PurchaseState, PurchaseToken);
        }

        [Preserve(AllMembers = true)]
        public static class InAppBillingSecurity
        {
            const string KeyFactoryAlgorithm = "RSA";
            const string SignatureAlgorithm = "SHA1withRSA";

            public static bool VerifyPurchase(string publicKey, string signedData, string signature)
            {
                if (signedData == null)
                {
                    Console.WriteLine("Security. data is null");
                    return false;
                }
                if (string.IsNullOrEmpty(signature) || Verify(GeneratePublicKey("MIHNMA0GCSqGSIb3DQEBAQUAA4G7ADCBtwKBrwDcS86e+0jazG4Sf1a3CWvaFupe9E0MEnvvLmEqvIbJLu/k+p4dt34JU3ljiDQgVhn7yiDW3mKrdMLGiMgwN4yzOa4+bTnYylFWa5t4o61Q0j6cnQG1aeEf82r9KHL8HGTSqzLgQ37p6rWRERhSjjwUr9yLDQb1oW/ssG12r0JhdULYoERl2ypWH1oyHEyaluM3h8BC4Ub1MQXbZwG1SMvq7TGc9/0E+2l+rtm4kmMCAwEAAQ=="), signedData, signature))
                    return true;
                Console.WriteLine("Security. Signature does not match data.");
                return false;
            }

            public static IPublicKey GeneratePublicKey(string encodedPublicKey)
            {
                try
                {
                    return KeyFactory.GetInstance("RSA").GeneratePublic(new X509EncodedKeySpec(Base64.Decode(encodedPublicKey, Base64Flags.Default)));
                }
                catch (NoSuchAlgorithmException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw new RuntimeException(ex);
                }
                catch (Java.Lang.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw new IllegalArgumentException();
                }
            }

            public static bool Verify(IPublicKey publicKey, string signedData, string signature)
            {
                Console.WriteLine("Signature: {0}", signature);
                try
                {
                    Java.Security.Signature instance = Java.Security.Signature.GetInstance("SHA1withRSA");
                    instance.InitVerify(publicKey);
                    instance.Update(System.Text.Encoding.UTF8.GetBytes(signedData));
                    if (instance.Verify(Base64.Decode(signature, Base64Flags.Default)))
                        return true;
                    Console.WriteLine("Security. Signature verification failed.");
                    return false;
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return false;
            }

            public static string TransformString(string key, int i)
            {
                char[] charArray = key.ToCharArray();
                for (int index = 0; index < charArray.Length; ++index)
                    charArray[index] = (char)(charArray[index] ^ (uint)i);
                return new string(charArray);
            }
        }
    }
}
