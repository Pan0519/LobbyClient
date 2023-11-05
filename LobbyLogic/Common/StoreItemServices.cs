using System.Collections.Generic;
using Service;
using Shop;
using LobbyLogic.NetWork.ResponseStruct;
using UnityEngine;
using UniRx;
using System.Threading.Tasks;
using Network;

namespace Lobby.Common
{
    public static class StoreItemServices
    {
        public static Subject<StoreItemData> buyItemResponse = new Subject<StoreItemData>();

        public static List<StoreItemData> convertProductToStoreItem(StoreProduct[] products)
        {
            List<StoreItemData> result = new List<StoreItemData>();
            for (int i = 0; i < products.Length; ++i)
            {
                var product = products[i];
                var platformProduct = IAPSDKServices.instance.getMatchProduct(product.productId);
                if (null == platformProduct)
                {
                    Debug.LogError($"{product.productId} can't find in platform");
                    continue;
                }
                result.Add(new StoreItemData()
                {
                    product = product,
                    platformProduct = platformProduct,
                });
            }
            return result;
        }

        public static async Task<StoreItemData> sendBuyItem(StoreItemData buyItemData)
        {
            BuyProductResponse response = await AppManager.lobbyServer.sendStoreOrder(buyItemData.product.sku);
            buyItemData.orderID = response.id;
            if (string.IsNullOrEmpty(response.id))
            {
                Debug.Log($"buy {buyItemData.product.productId},OrderId is empty");
                return null;
            }
            IAPSDKServices.instance.buyProduct(buyItemData.product.productId, response.id);
            return buyItemData;
        }

        public static async Task<CommonRewardsResponse> receiptSubscribe(string receipt, StoreItemData buyItemData)
        {
            Debug.Log($"receiptSubscribe : {buyItemData.orderID}");
            OnlyResultResponse receiptResponse = await AppManager.lobbyServer.patchReceipt(buyItemData.orderID, receipt);
            CommonRewardsResponse redeemResponse = await StoreItemServices.sendStoreRedeem(receiptResponse, buyItemData.orderID);
            if (null == redeemResponse)
            {
                IAPSDKServices.instance.showErrorReceipt(receipt);
                return null;
            }
            IAPSDKServices.instance.confirmPendingPurchase(buyItemData.platformProduct);
            return redeemResponse;
        }

        public static async Task<CommonRewardsResponse> sendStoreRedeem(OnlyResultResponse receiptResponse, string orderID)
        {
            if (Result.OK != receiptResponse.result)
            {
                Debug.LogError($"receiptResponse result is error {receiptResponse.result}");
                return null;
            }
            var redeemResponse = await AppManager.lobbyServer.sendStoreRedeem(orderID);
            if (Result.OK != redeemResponse.result)
            {
                Debug.LogError($"redeemResponse result is error {redeemResponse.result}");
                return null;
            }

            return redeemResponse;
        }

        public static void iapFailed(string errorMsg, string orderID)
        {
            Debug.LogError($"iapFailed Msg {errorMsg}");
            if (string.IsNullOrEmpty(orderID))
            {
                return;
            }
            AppManager.lobbyServer.sendStoreCancel(orderID);
        }
    }
}
