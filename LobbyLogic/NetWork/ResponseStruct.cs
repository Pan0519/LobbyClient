using Response = Network.ServerResponse;
using System.Collections.Generic;
using System;
using CommonService;
using CommonILRuntime.Outcome;

namespace LobbyLogic.NetWork.ResponseStruct
{
    #region Common
    public class OnlyResultResponse : Response
    {

    }

    public class TickResponse : Response
    {
        public string date;
    }

    public class LoginResponse : Response
    {
        public string sid;
        public BindingSetting settings;
    }

    public class DailyRewardResponse : Response
    {
        public DailyReward dailyReward;
    }

    public class DailyReward
    {
        public int cumulativeDays;
        public int resettableCumulativeDays;
        public DailyRewards[] rewards;
    }

    public class DailyRewards
    {
        public string type;
        public int cumulativeDays;
        public string rewardPackId;
    }

    public class BindingSetting
    {
        public Dictionary<string, int> bindingRewardWorths;
    }

    public class PlayerInfoResponse : Response
    {
        public string id;
        public string name;
        public int iconIndex;
        public int level;
        public long levelUpExp;
        public long exp;
        public string photoUrl;
        public Wallet wallet;
        public Dictionary<string, BindingInfo> bindings;
        public long coinExchangeRate;
        public string expBoostEndedAt;
        public string levelUpRewardBoostEndedAt;
        public string createdAt;
        public VipInfo vip;
        public LvupReward[] levelUpRewards;
        //public DailyReward dailyReward;
    }

    public class LvupReward
    {
        public string kind;
        public long amount;
    }

    public class BindingInfo
    {
        public string value;
        public bool hasAwarded;
    }
    public class BindingResponse : Response
    {
        public Award reward;
    }
    public class Award
    {
        public ulong coin;
        public Wallet wallet;
    }
    public class GuestLoginResponse : Response
    {
        public string token;
    }

    public class GameInfoResponse : Response
    {
        public GameInfoData[] games;
    }

    public class GameInfoData
    {
        public string id;
        public int requiredLevel; //等級限制
        public int priority; //優先度
        public string[] labels;
        public string[] tags;
        public string visibleAfter;
        public string visibleBefore;
        public string availableAfter;
        public string availableBefore;
        public long jackpotMultiplier;
    }

    public class WagerResponse : Response
    {
        public WagerExp exp;
        public Props props;
        public WagerBonusEnergy retentionBonusEnergy;
        public WagerAttachedAlbum album;
        public HighRollerBoardResultResponse highRoller;
    }

    public class WagerExp
    {
        public long amount;
        public string redeemAt;
        public WagerOutCome outcome;
    }

    public class WagerBonusEnergy
    {
        public int amount;
    }

    public class WagerOutCome
    {
        public WagerUser user;
    }
    public class WagerUser
    {
        public int revision;
        public long exp;
        public long levelUpExp;
        public int level;
        public bool isLevelUp;
        public string levelUpRewardPackId;
    }

    public class Props  //道具的結構定義
    {
        public string type;
        public int percentage;
        public string redeemAt;
        public Dictionary<string, Dictionary<string, object>> outcome;
    }

    public class WagerAttachedAlbum
    {
        public string[] items; //獲得的拼圖
        public DateTime redeemAt;
        public Dictionary<string, Dictionary<string, object>> outcome;
    }

    public class ActivityPropResponse : Response
    {
        public ActivityProp prop;
    }

    public class ActivityProp
    {
        public string type;
        public int amount;
        public int maximum;
        public int percentage;
    }

    public class ActivationResponse : Response
    {
        public string contentServer;
        public bool updateAvailable;
        public bool forceUpdate;
        public bool simplify;
    }
    #endregion

    #region Store
    public class BuyProductResponse : Response
    {
        public string id;
    }

    public class GetStoreResponse : Response
    {
        public StoreBouns bonus;
        public StoreProduct[] products;
    }
    public class StoreBouns
    {
        public string availableAfter;
        public long amount;
    }
    public class StoreProduct
    {
        public string sku;
        public string category;
        public string kind;
        public string productId;
        public int price;
        public decimal amount;
        public string[] labels;
        public Reward[] additions;
        public Dictionary<string, int> boosts;
        public ulong getAmount { get { return (ulong)amount; } }
    }

    public class GetBounsResponse : Response
    {
        public string availableAfter;
        public long amount;
    }
    public class PatchBounsResponse : Response
    {
        public string availableAfter;
        public string expBoostEndedAt;
        public int passPoints;
        public Wallet wallet;
        public HighRollerBoardResultResponse highRoller;
    }
    public class OrderResponse : Response
    {
        public string id;
    }

    public class CommonRewardsResponse : Response
    {
        public CommonReward[] rewards;
        public HighRollerBoardResultResponse highRoller;
    }

    public class GetSpecialOfferResponse : Response
    {
        //public StoreBouns bonus;
        public SpecialOfferFirst firstPurchase;
    }

    public class SpecialOfferFirst
    {
        public StoreProduct product;
    }

    #endregion

    #region Album
    public class AlbumVoucher
    {
        public string id;
        public string type;
        public DateTime expiry;
    }

    public class AlbumVouchersResponse : Response
    {
        public AlbumVoucher[] vouchers;
    }

    public class AlbumVoucherRedeemResponse : Response
    {
        public Dictionary<string, object>[] items;
    }

    public class AlbumRecycleResponse : Response
    {
        public int outerWheelResult = -1;
        public int innerWheelResult = -1;
        public CommonReward[] rewards;
        public string availableAfter;
    }
    #endregion

    #region ActivityStore
    public class ActivityStoreResponse : Response
    {
        public string salesType;
        public bool isFirstPurchase;
        public StoreProduct[] products;
    }
    #endregion

    #region Mail
    public class PeekMailCountResponse : Response
    {
        public int count;
    }

    public class GetMailsResponse : Response
    {
        public MailData[] messages;
    }

    public class MailData
    {
        public string id;
        public string type;
        public string title;
        public string content;
        public string expiry;
        public CommonReward[] rewards;
    }

    //public class RedeemMailResponse : Response
    //{
    //    public CommonReward[] rewards;
    //}
    #endregion

    #region Jigsaw
    public class JigsawAllSeasonAbstractResponse : Response
    {
        public JigsawSeasonAbstract[] seasons;
    }

    public class JigsawCurrentSeasonAbstractResponse : Response
    {
        public JigsawSeasonAbstract season;
    }

    public class JigsawRecycle : Response
    {
        public string availableAfter;
    }

    /// <summary>
    /// 拼圖單季概要
    /// </summary>
    public class JigsawSeasonAbstract
    {
        public string id;
        public DateTime startedAt;
        public DateTime endedAt;
        public JigsawAlbumAbstract[] albums;
        public long completeReward;
    }

    /// <summary>
    /// 單拼圖冊概要
    /// </summary>
    public class JigsawAlbumAbstract
    {
        public string id;
        public DateTime startedAt;
        public long completeReward;
    }

    public class JigsawAllAlbumProgressResponse : Response
    {
        public JigsawAlbumSummary[] summary;
    }

    public class JigsawAlbumSummary
    {
        public string albumId;  //ex: "00101"
        public int numCollected;
    }

    public class JigsawAlbumDetailResponse : Response
    {
        public JigsawPieceContent[] content;
    }

    public class JigsawPieceContent
    {
        public JigsawPieceContent(string id, int count)
        {
            this.id = id;
            this.count = count;
        }
        public string id;
        public int count;
    }

    public class JigsawAlbumRewardsResponse : Response
    {
        public JigsawRewardKind[] rewards;
    }

    public class JigsawRewardKind
    {
        public string id;
        public string source;
        public Reward[] rewards;
    }

    public class FantasyWheelItemData
    {
        public object kind;
        public object type;
    }

    public class RecycleWheelTableResponse : Response
    {
        public FantasyWheelItemData[] outerWheel;
        public FantasyWheelItemData[] innerWheel;
    }

    #endregion

    #region Popup
    public class PopupsResponse : Response
    {
        public PopupData[] popups;
    }

    public class PopupData
    {
        public string id;
        public int priority;
        public bool popup;
        public DateTime startedAt;
        public DateTime endedAt;
    }
    #endregion

    #region StayGame
    public class StayGameBonus : Response
    {
        public StayGameBonusInfo info;
    }
    public class StayGameBonusRedeem : Response
    {
        public StayGameBonusInfo info;
        public ulong bonusAmount;
        public float[] multipliers;
        public Wallet wallet;
        public HighRollerBoardResultResponse highRoller;
        public long passPoints;
    }

    public class StayGameBonusInfo
    {
        public string silverBoxAvailableAfter;
        public string goldenBoxAvailableAfter;
        public int wheelEnergy;
        public int diceEnergy;
        public int multiplierEnergy;
    }

    public class StayArcadeGameBonus : Response
    {
        public StayArcadeGameInfo info;
        public string type;
        public ulong? payment;
        public StayArcadeGamePending pending;
    }

    public class StayArcadeGamePending : Response
    {
        public decimal bonusAmount;
        public ulong[] multipliers;
        public ulong[] reelStrip;
        public ulong? passPoints;
    }

    public class StayArcadeGameCommit : StayArcadeGamePending
    {
        public StayArcadeGameInfo info;
    }

    public class StayArcadeGameInfo
    {
        public decimal[] multiplierEnergy;
    }

    public class StayArcadeGameRedeem : Response
    {
        public Wallet wallet;
    }
    #endregion

    #region Coupon
    public class Coupon
    {
        public string id;
        public string type;
        public int bonus;
        public string expiredAt;
    }

    public class GetCouponsResponse : Response
    {
        public Coupon[] coupons;
    }

    #endregion

    #region GoldEgg
    public class GoldEggResponse : Response
    {
        public int revision;
        public GoldEggData lowPool;
        public GoldEggData highPool;
    }
    public class GoldEggData
    {
        public string sku;
        public decimal amount;
        public decimal maximum;

        public ulong getAmount { get { return (ulong)amount; } }
        public ulong getMaximum { get { return (ulong)maximum; } }
    }
    public class ProductResponse : Response
    {
        public string sku;
        public string category;
        public string purchaseProductId;
        public Reward[] rewards;
    }
    #endregion

    #region RewardPacks
    public class RewardPacksResponse : Response
    {
        public string source;
        public string status;
        public int revision;
        public CommonReward[] rewards;
    }
    #endregion

    #region HighRoller
    public class HighRollerCheckExpireResponse : Response
    {
        public HighRollerBoardResultResponse highRoller;
    }

    public class HighRollerBoardResultResponse
    {
        public string awardBoardType;
        public int expireDays;
        public string productId;
        public int passPoints;
        public string rewardPackId;
    }

    public class HighRollerUserRecordResponse : Response
    {
        public bool accessExperienceUsed;
        public AccessInfo accessInfo;
        public long passPoints;
        public Vault vault;
        public int revision;
        public Cumulation cumulation;
    }
    public class HighRollerVaultResponse : Response
    {
        public HigherRollerVaultReturn highRoller;
    }
    public class HigherRollerVaultReturn
    {
        public decimal returnToPay;
        public ulong getReturnToPay { get { return (ulong)returnToPay; } }
    }

    public class HighRollerStoreResponse : Response
    {
        public StoreProduct[] products;
    }
    public class Cumulation
    {
        public int spinTimes;
    }
    public class Vault
    {
        public string expiredAt;
        public string lastBillingAt;
        public decimal returnToPay;
        public ulong getReturnToPay { get { return (ulong)returnToPay; } }
    }

    public class AccessInfo
    {
        public string expiredAt;
        public HighRollerAccessDetail[] details;
    }

    public class HighRollerAccessDetail
    {
        public int days;
        public string expiredAt;
    }
    #endregion

    #region Newbie
    public class NewbieAdventure : Response
    {
        public int stage;
        public int level;
        public NewbieRecord record;
        public string startAt;
        public string endAt;
        public string completedAt;
    }

    public class NewbieAdventureRecord
    {
        public int stage;
        public int level;
        public NewbieRecord record;
        public string startAt;
        public string endAt;
        public string completedAt;
        public string rewardedAt;
    }

    public class NewbieAdventureNotice : Response
    {
        public string noticedAt;
    }

    public class NewbieAdventureMissionProgress : Response
    {
        public NewbieAdventureMissionData[] missions;
        public string completedAt;
    }

    public class NewbieAdventureRedeem : Response
    {
        public string rewardedAt;
        public string rewardPackId;
        public NewbieAdventureRecord adventureRecord;
        public string completedAt;
    }

    public class NewbieRecord
    {
        public string kind;
        public string type;
        public MissionPack[] missions;
        public MissionReward[] rewards;
        public string startAt;
        public string noticedAt;
        public string completedAt;
        public string rewardedAt;
    }
    public class MissionPack
    {
        public MissionProgress progress;
        public MissionReward reward;
        public string completedAt;
        public string redeemAt;
    }
    public class MissionProgress : NewbieMissionData
    {
        public string kind;
        public string type;
    }

    public class NewbieAdventureMissionData
    {
        public string completedAt;
        public decimal[] amounts;
    }

    public class NewbieMissionData
    {
        public decimal[] conditions;
        public decimal[] amounts;
    }

    public class MissionReward
    {
        public string kind;
        public string type;
        public decimal amount;
    }

    #region OldNewbie
    public class NewbieTutorial : Response
    {
        public string status;
    }
    public class NewbieMission : Response
    {
        public string status;
        public MissionPack mission;
        public string rewardPackId;
    }

    #endregion

    #endregion
    #region DailyMission
    public class DailyMissionDataResponse : Response
    {
        public DailyMedalData medal;
        public DailyMissionInfo specialMission;
        public DailyMissionGeneralInfo generalMission;
    }

    public class DailyMedalData
    {
        public int count;
        public int max;
        public string nextResetAt;
        public DailyMissionStage[] stages;
    }

    public class DailyMissionStage
    {
        public int quantityReached;
        public string redeemAt;
        public MissionReward[] rewards;
    }

    public class DailyMissionInfo
    {
        public MissionPack mission;
        public string nextAssignAt;
        public int rewardMedalPoint;
    }

    public class DailyMissionGeneralInfo : DailyMissionInfo
    {
        public int index;
        public int max;
    }

    public class DailyMissionRewardResponse : Response
    {
        public string rewardPackId;
    }

    public class DailyMissionProgressResponse : Response
    {
        public DailyMissionProgressInfo generalMission;
        public DailyMissionProgressInfo specialMission;
    }

    public class DailyMissionProgressInfo
    {
        public decimal[] amounts;
        public string completedAt;
    }
    #endregion

    #region Chips Collect
    public class GetChipsCollect : Response
    {
        public ChipsCollect chipsCollect;
        public PendingChipCollect pendingChipCollect;
    }

    public class ChipsCollectRedeem : Response
    {
        public Collection collection;
    }

    public class PatchChipsCollectRedeem : Response
    {
        public ChipsCollect chipsCollect;
    }

    public class ChipsCollectGift
    {
        public string kind;
        public int amount;
    }

    public class Collection
    {
        public int collectIndex;
        public string rewardPackId;
    }

    public class ChipsCollect
    {
        public string[] chips;
        public ChipsCollectGift[] gifts;
        public string nextResetAt;
    }

    public class PendingChipCollect
    {
        public string chip;
        public ChipsCollectGift[] gifts;
        public int goldTransferIndex;
        public string nextResetAt;
    }
    #endregion

    #region Activity Quest
    //public class ActivityQuestResponse : Response
    //{
    //    public ActivityQuest[] ActivityQuestData;
    //}

    //public class ActivityQuest
    //{
    //    public string type;
    //    public int missionConditions;
    //    public int rewardConditions;
    //}
    #endregion
}
