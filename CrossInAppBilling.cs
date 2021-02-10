using System;
using System.Threading;

namespace Plugin.InAppBilling
{
    public class CrossInAppBilling
    {
        static Lazy<IInAppBilling> implementation = new Lazy<IInAppBilling>(() => CreateInAppBilling(), LazyThreadSafetyMode.PublicationOnly);

        public static bool IsSupported => implementation.Value != null;

        public static IInAppBilling Current => implementation.Value ?? throw NotImplementedInReferenceAssembly();

        static IInAppBilling CreateInAppBilling() => new InAppBillingImplementation();

        internal static Exception NotImplementedInReferenceAssembly() => new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");

        public static void Dispose()
        {
            if (implementation == null || !implementation.IsValueCreated)
                return;

            implementation.Value.Dispose();
            implementation = new Lazy<IInAppBilling>(() => CreateInAppBilling(), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
