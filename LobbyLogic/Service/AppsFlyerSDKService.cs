using System.Collections.Generic;
using Services;

namespace Service
{
    class AppsFlyerSDKService
    {
        public static AppsFlyerSDKService instance { get { return _instance; } }
        static AppsFlyerSDKService _instance = new AppsFlyerSDKService();

        bool isAlreadyInit = false;

        bool isCanSendEvent
        {
            get
            {
                return ApplicationConfig.environment == ApplicationConfig.Environment.Prod && ApplicationConfig.isLoadFromAB;
            }
        }

        public void initSDK()
        {
            if (isAlreadyInit || isCanSendEvent)
            {
                return;
            }

            isAlreadyInit = true;
            var rootUI = DontDestroyRootUI.instance;
            AppsFlySDK.instance.initSDK("Uj6RDP2zqoKd4p4sY7ojcj", string.Empty);
        }

        public void sendRegisterEvent(string loginType)
        {
            sendEvent("af_complete_registration", new Dictionary<string, string>() { { "af_registration_method", loginType } });
        }

        public void sendPurchaseEvent(string productID)
        {
            Dictionary<string, string> purchaseEvent = new Dictionary<string, string>();
            var productIDSplite = productID.Split('_');
            string price_reveue = productIDSplite[productIDSplite.Length - 1];
            purchaseEvent.Add("af_currency", "USD");
            purchaseEvent.Add("af_revenue", price_reveue);
            purchaseEvent.Add("af_quantity", "1");
            sendEvent("af_purchase", purchaseEvent);
        }

        void sendEvent(string eventName, Dictionary<string, string> eventValue)
        {
            if (!isCanSendEvent)
            {
                return;
            }
            AppsFlySDK.instance.onSendEvent(eventName, eventValue);
        }
    }
}
