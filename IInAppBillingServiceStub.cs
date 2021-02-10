using Android.OS;
using Android.Runtime;
using Java.Interop;
using System;

namespace Plugin.InAppBilling
{
    public abstract class IInAppBillingServiceStub : Binder, IInterface, IJavaObject, IDisposable, IJavaPeerable, IInAppBillingService
    {
        const string descriptor = "com.android.vending.billing.IInAppBillingService";
        internal const int TransactionIsBillingSupported = 1;
        internal const int TransactionGetSkuDetails = 2;
        internal const int TransactionGetBuyIntent = 3;
        internal const int TransactionGetPurchases = 4;
        internal const int TransactionConsumePurchase = 5;

        public IInAppBillingServiceStub() => this.AttachInterface(this, "com.android.vending.billing.IInAppBillingService");

        public static IInAppBillingService AsInterface(IBinder obj)
        {
            if (obj == null)
                return null;
            IInterface @interface = obj.QueryLocalInterface("com.android.vending.billing.IInAppBillingService");
            return @interface != null && @interface is IInAppBillingService ? (IInAppBillingService)@interface : new Proxy(obj);
        }

        public IBinder AsBinder() => this;

        protected override bool OnTransact(int code, Parcel data, Parcel reply, int flags)
        {
            switch (code)
            {
                case 1:
                    data.EnforceInterface("com.android.vending.billing.IInAppBillingService");
                    int num1 = data.ReadInt();
                    string str1 = data.ReadString();
                    string str2 = data.ReadString();
                    int val1 = IsBillingSupported(num1, str1, str2);
                    reply.WriteNoException();
                    reply.WriteInt(val1);
                    data.WriteInt(num1);
                    data.WriteString(str1);
                    data.WriteString(str2);
                    return true;
                case 2:
                    data.EnforceInterface("com.android.vending.billing.IInAppBillingService");
                    int num2 = data.ReadInt();
                    string str3 = data.ReadString();
                    string str4 = data.ReadString();
                    Bundle skusBundle = data.ReadInt() != 0 ? (Bundle)Bundle.Creator.CreateFromParcel(data) : null;
                    Bundle skuDetails = GetSkuDetails(num2, str3, str4, skusBundle);
                    reply.WriteNoException();
                    if (skuDetails != null)
                    {
                        reply.WriteInt(1);
                        skuDetails.WriteToParcel(reply, ParcelableWriteFlags.ReturnValue);
                    }
                    else
                        reply.WriteInt(0);
                    data.WriteInt(num2);
                    data.WriteString(str3);
                    data.WriteString(str4);
                    return true;
                case 3:
                    data.EnforceInterface("com.android.vending.billing.IInAppBillingService");
                    int num3 = data.ReadInt();
                    string str5 = data.ReadString();
                    string str6 = data.ReadString();
                    string str7 = data.ReadString();
                    string str8 = data.ReadString();
                    Bundle buyIntent = GetBuyIntent(num3, str5, str6, str7, str8);
                    reply.WriteNoException();
                    if (buyIntent != null)
                    {
                        reply.WriteInt(1);
                        buyIntent.WriteToParcel(reply, ParcelableWriteFlags.ReturnValue);
                    }
                    else
                        reply.WriteInt(0);
                    data.WriteInt(num3);
                    data.WriteString(str5);
                    data.WriteString(str6);
                    data.WriteString(str7);
                    data.WriteString(str8);
                    return true;
                case 4:
                    data.EnforceInterface("com.android.vending.billing.IInAppBillingService");
                    int num4 = data.ReadInt();
                    string str9 = data.ReadString();
                    string str10 = data.ReadString();
                    string str11 = data.ReadString();
                    Bundle purchases = GetPurchases(num4, str9, str10, str11);
                    reply.WriteNoException();
                    if (purchases != null)
                    {
                        reply.WriteInt(1);
                        purchases.WriteToParcel(reply, ParcelableWriteFlags.ReturnValue);
                    }
                    else
                        reply.WriteInt(0);
                    data.WriteInt(num4);
                    data.WriteString(str9);
                    data.WriteString(str10);
                    data.WriteString(str11);
                    return true;
                case 5:
                    data.EnforceInterface("com.android.vending.billing.IInAppBillingService");
                    int num5 = data.ReadInt();
                    string str12 = data.ReadString();
                    string str13 = data.ReadString();
                    int val2 = ConsumePurchase(num5, str12, str13);
                    reply.WriteNoException();
                    reply.WriteInt(val2);
                    data.WriteInt(num5);
                    data.WriteString(str12);
                    data.WriteString(str13);
                    return true;
                case 1598968902:
                    reply.WriteString("com.android.vending.billing.IInAppBillingService");
                    return true;
                default:
                    return base.OnTransact(code, data, reply, flags);
            }
        }

        public abstract int IsBillingSupported(int apiVersion, string packageName, string type);

        public abstract Bundle GetSkuDetails(int apiVersion, string packageName, string type, Bundle skusBundle);

        public abstract Bundle GetBuyIntent(int apiVersion, string packageName, string sku, string type, string developerPayload);

        public abstract Bundle GetPurchases(int apiVersion, string packageName, string type, string continuationToken);

        public abstract int ConsumePurchase(int apiVersion, string packageName, string purchaseToken);

        public class Proxy : Java.Lang.Object, IInAppBillingService, IInterface, IJavaObject, IDisposable, IJavaPeerable
        {
            IBinder remote;

            public Proxy(IBinder remote) => this.remote = remote;

            public IBinder AsBinder() => remote;

            public string GetInterfaceDescriptor() => "com.android.vending.billing.IInAppBillingService";

            public int IsBillingSupported(int apiVersion, string packageName, string type)
            {
                Parcel data = Parcel.Obtain();
                Parcel reply = Parcel.Obtain();
                int num = 0;
                try
                {
                    data.WriteInterfaceToken("com.android.vending.billing.IInAppBillingService");
                    data.WriteInt(apiVersion);
                    data.WriteString(packageName);
                    data.WriteString(type);
                    remote.Transact(1, data, reply, TransactionFlags.None);
                    reply.ReadException();
                    num = reply.ReadInt();
                }
                finally
                {
                    reply.Recycle();
                    data.Recycle();
                }
                return num;
            }

            public Bundle GetSkuDetails(int apiVersion, string packageName, string type, Bundle skusBundle)
            {
                Parcel parcel1 = Parcel.Obtain();
                Parcel parcel2 = Parcel.Obtain();
                Bundle bundle = null;
                try
                {
                    parcel1.WriteInterfaceToken("com.android.vending.billing.IInAppBillingService");
                    parcel1.WriteInt(apiVersion);
                    parcel1.WriteString(packageName);
                    parcel1.WriteString(type);
                    if (skusBundle != null)
                    {
                        parcel1.WriteInt(1);
                        skusBundle.WriteToParcel(parcel1, ParcelableWriteFlags.None);
                    }
                    else
                        parcel1.WriteInt(0);
                    remote.Transact(2, parcel1, parcel2, TransactionFlags.None);
                    parcel2.ReadException();
                    bundle = parcel2.ReadInt() != 0 ? (Bundle)Bundle.Creator.CreateFromParcel(parcel2) : null;
                }
                finally
                {
                    parcel2.Recycle();
                    parcel1.Recycle();
                }
                return bundle;
            }

            public Bundle GetBuyIntent(int apiVersion, string packageName, string sku, string type, string developerPayload)
            {
                Parcel data = Parcel.Obtain();
                Parcel parcel = Parcel.Obtain();
                Bundle bundle = null;
                try
                {
                    data.WriteInterfaceToken("com.android.vending.billing.IInAppBillingService");
                    data.WriteInt(apiVersion);
                    data.WriteString(packageName);
                    data.WriteString(sku);
                    data.WriteString(type);
                    data.WriteString(developerPayload);
                    remote.Transact(3, data, parcel, TransactionFlags.None);
                    parcel.ReadException();
                    bundle = parcel.ReadInt() != 0 ? (Bundle)Bundle.Creator.CreateFromParcel(parcel) : null;
                }
                finally
                {
                    parcel.Recycle();
                    data.Recycle();
                }
                return bundle;
            }

            public Bundle GetPurchases(int apiVersion, string packageName, string type, string continuationToken)
            {
                Parcel data = Parcel.Obtain();
                Parcel parcel = Parcel.Obtain();
                Bundle bundle = null;
                try
                {
                    data.WriteInterfaceToken("com.android.vending.billing.IInAppBillingService");
                    data.WriteInt(apiVersion);
                    data.WriteString(packageName);
                    data.WriteString(type);
                    data.WriteString(continuationToken);
                    remote.Transact(4, data, parcel, TransactionFlags.None);
                    parcel.ReadException();
                    bundle = parcel.ReadInt() != 0 ? (Bundle)Bundle.Creator.CreateFromParcel(parcel) : null;
                }
                finally
                {
                    parcel.Recycle();
                    data.Recycle();
                }
                return bundle;
            }

            public int ConsumePurchase(int apiVersion, string packageName, string purchaseToken)
            {
                Parcel data = Parcel.Obtain();
                Parcel reply = Parcel.Obtain();
                int num = 0;
                try
                {
                    data.WriteInterfaceToken("com.android.vending.billing.IInAppBillingService");
                    data.WriteInt(apiVersion);
                    data.WriteString(packageName);
                    data.WriteString(purchaseToken);
                    remote.Transact(5, data, reply, TransactionFlags.None);
                    reply.ReadException();
                    num = reply.ReadInt();
                }
                finally
                {
                    reply.Recycle();
                    data.Recycle();
                }
                return num;
            }
        }
    }
}
