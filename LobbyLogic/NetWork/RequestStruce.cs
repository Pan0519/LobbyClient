using System.Collections.Generic;
using CommonService;
using EventActivity;

namespace LobbyLogic.NetWork.RequestStruce
{
    public class LoginRequest
    {
        public string token;
        public string deviceId;
        public string method;
    }

    public class ModifyPlayerInfoRequest
    {
        public string name;
        public int iconIndex;
    }

    public class GuestLoginRequest
    {
        public string deviceId;
    }

    public class AskVerifyCode
    {
        public string phoneNumber;
        public string locale;
    }

    public class BindingPhoneNumber
    {
        public string phoneNumber;
        public string code;
    }

    public class AskEmailVerifyCode
    {
        public string email;
        public string locale;
    }
    public class BindingEmail
    {
        public string email;
        public string code;
        public bool acceptPromotions;
    }

    public class Activations
    {
        public string deviceId;
        public Dictionary<string, object> deviceDetail;
        public string from;
        public string via; // 遊戲啟動的方式
    }

    public class GetSingleItemAmount
    {
        public string propID;
    }

    public class ProductSKU
    {
        public string sku;
    }

    public class StoreReceipt
    {
        public string receipt;
    }

    public class StoreReceiptWithCoupon : StoreReceipt
    {
        public string couponId;
    }
    #region ActivityRequest
    public class ActivityRequestBase
    {
        public string SessionID = DataStore.getInstance.dataInfo.sessionSid;
        public string ActivitySerial = ActivityDataStore.nowActivityInfo.serial;
    }

    public class EventClickGameSendChoice : ActivityRequestBase
    {
        public int PlayIndex;
        public int ClickItem;
    }

    public class FrenzyJourneyPlayData : ActivityRequestBase
    {
        public int PlayIndex;
    }
    public class OpenEventBox : ActivityRequestBase
    {
        public int ClickItem;
    }

    public class LvUpRedeem
    {
        public string packetID;
    }

    public class PostEmpty
    {
        public byte empty;
    }

    public class StayGameRedeem
    {
        public string type;
    }

    public class BossUseItem : ActivityRequestBase
    {
        public int PlayIndex;
        public string ItemName;
    }

    public class ActivityStoreData
    {
        public string sessionID = DataStore.getInstance.dataInfo.sessionSid;
        public string activitySerial = ActivityDataStore.nowActivityInfo.serial;
        public string activityId = ActivityDataStore.nowActivityInfo.activityId;
    }

    #endregion

    public class VoucherRedeemRequestData
    {
        public VoucherRedeemRequestData(string itemId)
        {
            this.itemId = itemId;
        }

        public string itemId;
    }

    public class AlbumRecycleItem
    {
        public AlbumRecycleItem(string id, int amount)
        {
            this.id = id;
            this.amount = amount;
        }

        public string id;
        public int amount;
    }

    public class AlbumRecycleRequestData
    {
        public AlbumRecycleItem[] items;
    }
}
