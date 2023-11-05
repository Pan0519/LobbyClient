using Common.VIP;

namespace Lobby.VIP.UI
{
    public class VipTitle
    {
        public int level;
        public int points;
    }

    public class VipProfit
    {
        public VipProfitDef id { get; private set; }
        public int value { get; private set; }
        public void setProfitData(VipProfitDef id, int value)
        {
            this.id = id;
            this.value = value;
        }
    }

    public class VipLevelData
    {
        public VipTitle title;
        public VipProfit[] profits;
    }

    public class VipDashboardData
    {
        public VipLevelData[] levels;
    }

    public class VipUiInfo
    {
        public int level;
        public int points;
        public int levelUpPoints;
    }
}
