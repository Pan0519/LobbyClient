using Common.VIP;
using Lobby.VIP.UI;

namespace Lobby.VIP
{
    public static class VipTestDataCreator
    {
        public static VipDashboardData make()
        {
            VipDashboardData data = new VipDashboardData();
            data.levels = new VipLevelData[7];

            for (int levelIdx = 0; levelIdx < data.levels.Length; levelIdx++)
            {
                data.levels[levelIdx] = makeLevelData(levelIdx + 1);
            }

            return data;
        }

        public static VipUiInfo makePlayerVipInfo()
        {
            VipUiInfo p = new VipUiInfo();
            p.level = 1;
            p.points = 55;
            return p;
        }

        static VipLevelData makeLevelData(int level)
        {
            VipLevelData data = new VipLevelData();

            data.title = new VipTitle();
            data.title.level = level;
            data.title.points = level * 100;
            data.profits = new VipProfit[7];

            for (int i = 0; i < data.profits.Length; i++)
            {
                data.profits[i] = makeProfitData(i, level);
            }

            return data;
        }

        static VipProfit makeProfitData(int profitId, int vipId)
        {
            VipProfit data = new VipProfit();
            data.setProfitData((VipProfitDef)profitId, vipId * 10 + profitId * 5);
            return data;
        }
    }
}
