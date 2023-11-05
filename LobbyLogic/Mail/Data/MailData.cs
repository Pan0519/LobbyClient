using System;
using Services;
using CommonILRuntime.Outcome;
using LobbyLogic.NetWork.ResponseStruct;

namespace Lobby.Mail
{
    public enum MailType
    {
        COUPON = 0,
        SYSTEM,
        ACTIVITYPROP,
        PUZZLEPACK,
        VIPPOINT,


        OTHER
    }

    public enum RewardType
    {
        COIN,
        ITEM
    }

    public class Sender
    {
        public string name;
        public string headImgUrl;
    }

    public interface IMessage
    {
        string getId();
        MailType getType();
        DateTime getExpiredTime();
    }

    public class Message
    {
        public string Id;
        public MailType type;

        public string getId() { return Id; }
        public MailType getType() { return type; }

        public MailType convertFromServerType(string serverType)
        {
            switch (serverType)
            {
                case "coupon":
                    return MailType.COUPON;
                default:
                    return MailType.SYSTEM;
            }
        }

        public DateTime convertTime(string expiredAtTime)
        {
            return UtilServices.strConvertToDateTime(expiredAtTime, DateTime.UtcNow);
        }
    }

    public class SystemMessage : Message, IMessage
    {
        public string title;
        public string context;
        public DateTime endTime;
        public CommonReward[] rewards;

        public DateTime getExpiredTime() { return endTime; }

        public void convertServerMailData(MailData serverMailData)
        {
            type = convertFromServerType(serverMailData.type);
            Id = serverMailData.id;
            title = serverMailData.title;
            context = serverMailData.content;
            endTime = convertTime(serverMailData.expiry);
            rewards = serverMailData.rewards;
        }
    }

    public class CouponMessage : Message, IMessage
    {
        public int bonus;
        public DateTime expiredAt;
        public DateTime getExpiredTime() { return expiredAt; }

        public void convertServerCouponData(Coupon serverCouponData)
        {
            type = convertFromServerType(serverCouponData.type);
            Id = serverCouponData.id;
            bonus = serverCouponData.bonus;
            expiredAt = convertTime(serverCouponData.expiredAt);
        }
    }
}
