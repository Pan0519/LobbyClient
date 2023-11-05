using Response = Network.ServerResponse;
using System.Collections.Generic;

namespace LobbyLogic.NetWork.ResponseStruct
{
    #region Activity
    public class BaseInitActivityResponse : Response
    {
        public int Level;
        public decimal CompleteReward;
        public ProgressData Progress;
        public bool IsEnd;
        public BannerData Banner;
    }

    public class BannerData
    {
        public decimal Reward;
        public ActivityReward[] Item;
    }

    public class RookieInitActivityResponse : BaseInitActivityResponse
    {
        public int[] ClickHistory;
        public int[][] ExtraBonus;
    }

    public class SendSelectBaseResponse : Response
    {
        public ActivityReward[] RewardResult;
        public bool IsLevelUp;
        public int Ticket;
        public string RefreshData;
    }

    public class AppleFrameSelectResponse : SendSelectBaseResponse
    {
        public long AvailableTime;
        public long CountDownTime;
        public string RewardPackId;
    }

    public class ProgressData
    {
        public int Value;
        public int Target;
    }

    public class GetBagItemResponse : Response
    {
        public int amount;
    }

    public class GetBagResponse : Response
    {
        public int revision;
        public Dictionary<string, int> props;
    }

    public class GetActivityResponse : Response
    {
        public Activity activity;
    }

    public class Activity
    {
        public string serial;
        public string activityId;
        public string theme;
        public string startAt;
        public string endAt;
    }

    public class AppleFarmInitResponse : RookieInitActivityResponse
    {
        public long FinalReward;
        public ActivityReward[] FinalItem;
        public ActivityReward[] CompleteItem;
        public TreasureBox[] TreasureBox;
        public Dictionary<string, decimal> JackPotReward;
        public Dictionary<string, bool[]> JackPotCollection;
        public bool IsRecircle;
        public BoostsData BoostsData;
    }

    public class TreasureBox
    {
        public string Type;
        public long AvailableStartTime;
    }

    public class BoostsData
    {
        /// <summary>
        /// TimeSpan(TicketBooster)
        /// </summary>
        public long SpinBoost;
        /// <summary>
        /// TimeSpan(PrizeBooster)
        /// </summary>
        public long CoinBoost;
        /// <summary>
        /// Times(GoldenTicketBooster)
        /// </summary>
        public long PickBoost;
    }
    public class BoxResponse : Response
    {
        public ActivityReward[] RewardResult;
        public string RewardPackId;
    }
    public class AppleFarmBoxResponse : BoxResponse
    {
        public int JackPotReward;
        public BoostsData BoostsData;
    }
    #region Journey
    public class JourneyInitResponse : BaseInitActivityResponse
    {
        public int Round;
        public ActivityReward[] CompleteItem;
        public TreasureBox[] TreasureBox;
        public bool IsRecircle;
        public JourneyBoosterData BoostsData;
        public int MapIndex;
        public BossData BossData;
        public long[][] History;
    }

    public class JourneyPlayResponse : Response
    {
        //public long[][] RewardResult;
        public ActivityReward[] RewardResult;
        public string RewardPackId;
        public int[] DiceIndex;
        public int ProgressValue;
        public int Ticket;
        public long AvailableTime;
        public long CountDownTime;
        public BossData BossData;
    }

    public class JourneyBossPlayResponse : SendSelectBaseResponse
    {
        public string RewardPackId;
        public long AvailableTime;
        public long CountDownTime;
        public BossData BossData;
        public ActivityReward[] StageResult;
    }

    public class JourneyBoosterData
    {
        public long DiceBoost;
        public long CoinBoost;
        public long FrenzyDice;
    }
    public class BossData : BaseReward
    {
        public int BossID;
        public int MaxHP;
        public int Attack;
        public int[] TotalDiceIndex;
    }
    #endregion

    #region MagicForest
    public class MagicForestInitResponse : BaseInitActivityResponse
    {
        public int DoorNum;
        public int Round;
        public MagicForestStageReward[] StageRewards;
        public MagicForestBossData MagicForestBossData;
        public ForestBoosterData BoostsData;
        public ActivityReward[] DoorHistory;
        public ActivityReward[] BossHistory;
        public Dictionary<string, long> JackPotReward;
        public Dictionary<string, int> JackPotCount;
        public bool IsRecircle;
    }

    public class ForestBoosterData
    {
        public long MagnifierBooster;
        public long PrizeBooster;
        public long GoldenMallet;
    }

    public class MagicForestStageReward : BaseReward
    {
        public int StageNum;
        public long Bonus;
    }
    public class MagicForestBossData : BaseReward
    {
        public int BossID;
        public int Max;
        public int Count;
        public long GoldenMallet;
    }

    public class MagicForestPlayResponse : Response
    {
        public ActivityReward[] RewardResult;
        public string RewardPackId;
        public int Ticket;
        public bool IsGift;
        public MagicForestBossData BossData;
        public int NextDoorNum;
        public MagicForestStageReward StageReward;
        public bool IsLevelUp;
        public Dictionary<string, ulong> JPReward;
    }

    public class MagicForestBossPlayResponse : Response
    {
        public ActivityReward[] RewardResult;
        public string RewardPackId;
        public int Ticket;
        public bool IsPass;
        public bool IsLevelUp;
        public bool IsEnd;
        public int NextDoorNum;
        public Dictionary<string, long> JPReward;
    }

    public class MagicForestBossUseResponse : MagicForestBossPlayResponse
    {
        public long GoldenMallet;
    }

    #endregion

    public class ActivityReward
    {
        public string Kind;
        public string Type;
        public decimal Amount;
        public ulong getAmount { get { return (ulong)Amount; } }
    }

    public class BaseReward
    {
        public decimal CompleteReward;
        public ActivityReward[] CompleteItem;
        public ulong getCompleteReward { get { return (ulong)CompleteReward; } }
    }
    #endregion
}
