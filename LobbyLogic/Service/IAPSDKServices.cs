using System;
using System.Collections.Generic;
using UniRx;
using Services;
using UnityEngine.Purchasing;
using Debug = UnityLogUtility.Debug;

namespace Service
{
    public class IAPSDKServices
    {
        static IAPSDKServices _instance = new IAPSDKServices();
        public static IAPSDKServices instance { get { return _instance; } }

        List<IDisposable> disposables = new List<IDisposable>();

        Dictionary<string, Product> platformProductDicts = new Dictionary<string, Product>();
        public Subject<string> receiptSub { get; private set; } = new Subject<string>();
        public Subject<string> iapFailed { get; private set; } = new Subject<string>();
        public Subject<Product[]> initProducts { get; private set; } = new Subject<Product[]>();

        public void init()
        {
            disposables.Add(IAPSDK.Instance.ProductReceipt.Subscribe(receiptSub));
            disposables.Add(IAPSDK.Instance.FailedError.Subscribe(iapFailed));
            disposables.Add(IAPSDK.Instance.InitProducts.Subscribe(productsSub));
            IAPSDK.Instance.initialize();
        }

        public void clearSubscribes()
        {
            UtilServices.disposeSubscribes(disposables.ToArray());
            disposables.Clear();
        }

        void productsSub(Product[] products)
        {
            if (null == platformProductDicts || platformProductDicts.Count <= 0)
            {
                parseProduct(products);
            }
            
            initProducts.OnNext(products);
        }

        void parseProduct(Product[] products)
        {
            for (int i = 0; i < products.Length; ++i)
            {
                Product product = products[i];
                if (platformProductDicts.ContainsKey(product.definition.id))
                {
                    Debug.LogError($"Product has Same ProductID:{product.definition.id}");
                    continue;
                }
                platformProductDicts.Add(product.definition.id, product);
            }
        }

        public Product getMatchProduct(string productID)
        {
            Product result = null;
            platformProductDicts.TryGetValue(productID, out result);
            return result;
        }

        public void confirmPendingPurchase(Product product)
        {
            IAPSDK.Instance.ConfirmPendingPurchase(product);
        }

        public void buyProduct(string productId, string payload)
        {
            IAPSDK.Instance.buyProduct(productId, payload);
        }

        public void showErrorReceipt(string receipt)
        {
            Dictionary<string, string> parseReceipt = LitJson.JsonMapper.ToObject<Dictionary<string, string>>(receipt);
            var receiptData = parseReceipt.GetEnumerator();
            while (receiptData.MoveNext())
            {
                Debug.Log($"ShowErrorReceipt:\nKey :{receiptData.Current.Key}\nValue : {receiptData.Current.Value}");
            }
        }

        /// <summary>
        /// 商品金額後兩位數為 00 的話 不顯示
        /// </summary>
        public string substringPriceTxt(string price)
        {
            if (price.EndsWith(".00"))
            {
                int spliteId = price.LastIndexOf('.');
                price = price.Substring(0, spliteId);
            }
            return price;
        }

    }
}
