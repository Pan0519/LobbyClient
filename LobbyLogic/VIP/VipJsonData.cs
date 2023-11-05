using Common.VIP;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Lobby.VIP
{
    public class VipLevelInfos
    {
        public List<VipLevelInfo> levelDatas;
    }

    public class VipLevelInfo
    {
        public int level;
        public int coinDeal;
        public int vipPoints;
        public int silverBox;
        public int goldenBox;
        public int storeBonus;
        public int cardscrushBonus;
    }

    public static class VipJsonData
    {
        //public static string jsonString = "{\"levelInfos\" : [" +
        //    "{\"level\":1," +
        //    "\"coinDeal\":100," +
        //    "\"vipPoints\":100," +
        //    "\"silverBox\":110," +
        //    "\"goldenBox\":110," +
        //    "\"storeBonus\":100}," +

        //    "{\"level\":2," +
        //    "\"coinDeal\":150," +
        //    "\"vipPoints\":200," +
        //    "\"silverBox\":130," +
        //    "\"goldenBox\":130," +
        //    "\"storeBonus\":200}," +

        //    "{\"level\":3," +
        //    "\"coinDeal\":260," +
        //    "\"vipPoints\":300," +
        //    "\"silverBox\":150," +
        //    "\"goldenBox\":150," +
        //    "\"storeBonus\":400}," +

        //    "{\"level\":4," +
        //    "\"coinDeal\":400," +
        //    "\"vipPoints\":400," +
        //    "\"silverBox\":200," +
        //    "\"goldenBox\":200," +
        //    "\"storeBonus\":700}," +

        //    "{\"level\":5," +
        //    "\"coinDeal\":700," +
        //    "\"vipPoints\":500," +
        //    "\"silverBox\":250," +
        //    "\"goldenBox\":250," +
        //    "\"storeBonus\":1000}," +

        //    "{\"level\":6," +
        //    "\"coinDeal\":1000," +
        //    "\"vipPoints\":600," +
        //    "\"silverBox\":300," +
        //    "\"goldenBox\":300," +
        //    "\"storeBonus\":2000}," +

        //    "{\"level\":7," +
        //    "\"coinDeal\":2000," +
        //    "\"vipPoints\":700," +
        //    "\"silverBox\":350," +
        //    "\"goldenBox\":350," +
        //    "\"storeBonus\":5000}" +
        //    "]" + 
        //    "}";

        public static async Task<UI.VipLevelData[]> getLevelInfos()
        {
            var jsonStr = await WebRequestText.instance.loadTextFromServer("vip_lv_info");
            //Debug.Log(jsonStr);
            VipLevelInfos infos = LitJson.JsonMapper.ToObject<VipLevelInfos>(jsonStr);
            return toUiLevelData(infos);
        }

        static UI.VipLevelData[] toUiLevelData(VipLevelInfos infos)
        {
            List<UI.VipLevelData> levelData = new List<UI.VipLevelData>();
            for (int i = 0; i < infos.levelDatas.Count; i++)
            {
                var info = infos.levelDatas[i];
                var data = levelInfoToLevelData(info);
                levelData.Add(data);
            }
            return levelData.ToArray();
        }

        static UI.VipLevelData levelInfoToLevelData(VipLevelInfo info)
        {
            UI.VipTitle title = new UI.VipTitle();
            title.level = info.level;

            List<UI.VipProfit> vipProfits = new List<UI.VipProfit>();
            vipProfits.Add(setProfitData(VipProfitDef.COIN_DEAL, info.coinDeal));
            vipProfits.Add(setProfitData(VipProfitDef.VIP_POINTS, info.vipPoints));
            vipProfits.Add(setProfitData(VipProfitDef.SILVER_BOX, info.silverBox));
            vipProfits.Add(setProfitData(VipProfitDef.GOLDEN_BOX, info.goldenBox));
            vipProfits.Add(setProfitData(VipProfitDef.STORE_BONUS, info.storeBonus));
            vipProfits.Add(setProfitData(VipProfitDef.CARD_CRUSH_BOUNS, info.cardscrushBonus));

            //UI.VipProfit coinDeal = new UI.VipProfit();
            //coinDeal.id = VipProfitDef.COIN_DEAL;
            //coinDeal.value = info.coinDeal;

            //UI.VipProfit vipPoints = new UI.VipProfit();
            //vipPoints.id = VipProfitDef.VIP_POINTS;
            //vipPoints.value = info.vipPoints;

            //UI.VipProfit silverBox = new UI.VipProfit();
            //silverBox.id = VipProfitDef.SILVER_BOX;
            //silverBox.value = info.silverBox;

            //UI.VipProfit goldenBox = new UI.VipProfit();
            //goldenBox.id = VipProfitDef.GOLDEN_BOX;
            //goldenBox.value = info.goldenBox;

            //UI.VipProfit storeBonus = new UI.VipProfit();
            //storeBonus.id = VipProfitDef.STORE_BONUS;
            //storeBonus.value = info.storeBonus;

            //UI.VipProfit cardCrushBouns = new UI.VipProfit();
            //cardCrushBouns.id = VipProfitDef.CARD_CRUSH_BOUNS;
            //cardCrushBouns.value = info.cardscrushBonus;

            UI.VipLevelData rtnData = new UI.VipLevelData();
            rtnData.title = title;
            rtnData.profits = vipProfits.ToArray();
                //new UI.VipProfit[6] { coinDeal, vipPoints, silverBox, goldenBox, storeBonus, cardCrushBouns };

            return rtnData;
        }

        static UI.VipProfit setProfitData(VipProfitDef id, int value)
        {
            var result = new UI.VipProfit();
            result.setProfitData(id, value);
            return result;
        }

        static void printInfo(VipLevelInfo info)
        {
            Debug.Log($"coinDeal: {info.coinDeal}");
            Debug.Log($"vipPoints: {info.vipPoints}");
            Debug.Log($"silverBox: {info.silverBox}");
            Debug.Log($"goldenBox: {info.goldenBox}");
            Debug.Log($"storeBonus: {info.storeBonus}");
        }
    }
}
