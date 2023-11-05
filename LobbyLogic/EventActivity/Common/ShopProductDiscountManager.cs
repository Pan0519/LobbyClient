using System.Collections.Generic;
using System.Threading.Tasks;
using Debug = UnityLogUtility.Debug;

namespace Event.Common
{
    public static class ShopProductDiscountManager
    {
        static ProductDiscount productDiscounts = null;

        public static async Task<string> getNormalID(string discountID)
        {
            if (null == productDiscounts)
            {
                productDiscounts = await loadProductJson();
                productDiscounts.convertSaleProduct();
            }
            return productDiscounts.getNormalID(discountID);
        }

        static async Task<ProductDiscount> loadProductJson()
        {
            string jsonStr = await WebRequestText.instance.loadTextFromServer("product_discount");
            return LitJson.JsonMapper.ToObject<ProductDiscount>(jsonStr);
        }
    }
    public class ProductDiscount
    {
        public string FirstDiscount;
        public SaleProduct[] ProductSale;
        Dictionary<string, string> saleProducts = new Dictionary<string, string>();
        public void convertSaleProduct()
        {
            for (int i = 0; i < ProductSale.Length; ++i)
            {
                var productData = ProductSale[i];
                if (saleProducts.ContainsKey(productData.DiscountID))
                {
                    Debug.LogError($"ProductDiscount Json get same discountID : {productData.DiscountID}");
                    continue;
                }
                saleProducts.Add(productData.DiscountID, productData.NormalID);
            }
        }

        public string getNormalID(string discountID)
        {
            string result = string.Empty;
            if (!saleProducts.TryGetValue(discountID, out result))
            {
                Debug.LogError($"get {discountID}'s normalID is empty");
            }
            return result;
        }
    }

    public class SaleProduct
    {
        public string NormalID;
        public string DiscountID;
    }
}
