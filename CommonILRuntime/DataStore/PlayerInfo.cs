using UniRx;
using UnityEngine;
using System;
using Services;
using Debug = UnityLogUtility.Debug;
using CommonILRuntime.PlayerProp;

namespace CommonService
{
    public class PlayerInfo
    {
        #region PlayerInfoSubject
        public Subject<int> playerGameStateSubject { get; private set; } = new Subject<int>();
        public Subject<float> playerExpSubject { get; private set; } = new Subject<float>();
        public Subject<long> playerLvUpExpSubject { get; private set; } = new Subject<long>();
        public Subject<Sprite> headImageSubject { get; private set; } = new Subject<Sprite>();
        public Subject<string> nameSubject { get; private set; } = new Subject<string>();
        public Subject<int> lvSubject { get; private set; } = new Subject<int>();
        public Subject<DateTime> expBoostEndSubject { get; private set; } = new Subject<DateTime>();
        public Subject<DateTime> lvupBoostEndSubject { get; private set; } = new Subject<DateTime>();
        public Subject<DateTime> highRollerEndTimeSubject { get; private set; } = new Subject<DateTime>();
        public Subject<bool> isLvUpSubject { get; private set; } = new Subject<bool>();
        public Subject<bool> checkHighRollerPermissionSub { get; private set; } = new Subject<bool>();
        public Subject<long> addPassPointSub { get; private set; } = new Subject<long>();
        //public Subject<int> vipChangedSubject { get; private set; } = new Subject<int>();
        #endregion
        #region baseInfo
        public long playerExp { get; private set; }
        public long LvUpExp { get; private set; }
        public string playerName { get; private set; }
        //to do 需將nowGameState改為私有，不再提供外部存取，新架構不再直接取用
        public int nowGameState { get; private set; }
        public int level { get; private set; } = 1;
        public string userID { get; private set; }
        public string PhoneNumber { get; private set; }
        public string Email { get; private set; }
        public DateTime expBoostEndTime { get; private set; }
        public DateTime lvUpBoostEndTime { get; private set; }
        public DateTime highRollerEndTime { get; private set; }
        public long coinExchangeRate { get; private set; }
        public int iconIndex { get; private set; }
        public string fbImageUrl { get; private set; } = string.Empty;
        public Sprite headSprite { get; private set; }
        public bool isBindPhone { get; set; } = false;
        public bool isBindFB { get; private set; } = false;
        public bool hasHighRollerPermission { get; private set; } = false;
        public DateTime createTime { get; private set; } = DateTime.Now;
        #endregion
        public MailVerifiedState mailVerifiedState { get; private set; }
        public Subject<MailVerifiedState> bindMailSubject { get; private set; } = new Subject<MailVerifiedState>();

        public PlayerWallet myWallet = new PlayerWallet(new Wallet() { revision = -1, coin = 0 });
        public ulong playerMoney { get { return myWallet.coin; } }

        public PlayerVip myVip = new PlayerVip(new VipInfo() { points = 0, level = 1, levelUpPoints = 100 });

        public void mailBind()
        {
            mailVerifiedState = MailVerifiedState.Verified;
            bindMailSubject.OnNext(mailVerifiedState);
        }

        /// <summary>
        /// 此處需用server state運作
        /// </summary>
        /// <param name="state"></param>
        public void setPlayerGameStateAndSubject(int state)
        {
            nowGameState = (int)state;
            playerGameStateSubject.OnNext(nowGameState);
        }

        public void addPlayerExp(long addExp)
        {
            setPlayerExpAndSubject(playerExp + addExp);
        }

        public void setPlayerExpAndSubject(long totalExp)
        {
            playerExp = totalExp;
            playerExpSubject.OnNext(totalExp);
        }

        public void setLvUpExp(long upExp)
        {
            LvUpExp = upExp;
            playerLvUpExpSubject.OnNext(LvUpExp);
        }

        public void setIconIdx(int index)
        {
            iconIndex = index;
            callHeadChanged();
        }
        public void setLv(int lv)
        {
            level = lv;
            lvSubject.OnNext(lv);
        }
        public void setName(string name)
        {
            playerName = name;
            nameSubject.OnNext(name);
        }
        public void setUserID(string id)
        {
            PlayerPrefs.SetString(ApplicationConfig.TempUserIDKey, id);
            userID = id;
        }

        public void setFBImageUrl(string url)
        {
            fbImageUrl = string.IsNullOrEmpty(url) ? string.Empty : $"{url}?type=large";
            callHeadChanged();
        }
        public void setCreateTime(string time)
        {
            createTime = UtilServices.strConvertToDateTime(time, DateTime.Now);
        }

        public void setIsBindingFB(bool isBinding)
        {
            isBindFB = isBinding;
        }
        Sprite[] headSprites = null;
        public void callHeadChanged()
        {
            if (iconIndex >= 0)
            {
                if (null == headSprites)
                {
                    headSprites = ResourceManager.instance.loadAll("prefab/player_head/player_head");
                }
                headSprite = Array.Find(headSprites, sprite => sprite.name.Equals($"head_{iconIndex}"));
                headImageSubject.OnNext(headSprite);
                return;
            }
            WebRequestTextureScheduler.instance.request(fbImageUrl, (texture) =>
            {
                headSprite = Util.getSpriteFromTexture(texture);
                headImageSubject.OnNext(headSprite);
            }).download();
        }

        public void UpdateBindingPhoneInfo(string phone)
        {
            PhoneNumber = phone;
            isBindPhone = true;
        }

        public void UpdateBindingEmailInfo(string email)
        {
            Email = email;
            mailBind();
        }

        public void setCoinExchangeRate(long rate)
        {
            coinExchangeRate = rate;
        }

        public void setExpBoostEndTime(string endTime)
        {
            expBoostEndTime = UtilServices.strConvertToDateTime(endTime, DateTime.UtcNow);
            expBoostEndSubject.OnNext(expBoostEndTime);
        }

        public void setLvupBoostEndTime(string endTime)
        {
            lvUpBoostEndTime = UtilServices.strConvertToDateTime(endTime, DateTime.UtcNow);
            lvupBoostEndSubject.OnNext(lvUpBoostEndTime);
        }

        public void setHighRollerEndTime(string endTime)
        {
            highRollerEndTime = UtilServices.strConvertToDateTime(endTime, DateTime.UtcNow);
            highRollerEndTimeSubject.OnNext(highRollerEndTime);
        }

        public void setIsLvUP(bool isLvUP)
        {
            isLvUpSubject.OnNext(isLvUP);
        }

        public void checkHasHighRollerPermission(bool hasAccessInfo)
        {
            hasHighRollerPermission = hasAccessInfo;
            checkHighRollerPermissionSub.OnNext(hasAccessInfo);
        }

        public void addPassPoint(int point)
        {
            addPassPointSub.OnNext(point);
        }
    }

    public class Wallet : IPropertyStruct
    {
        public long revision;
        public long getRevision() { return revision; }
        public decimal coin;
        public ulong getCoin() { return (ulong)coin; }
    }

    public class VipInfo : IPropertyStruct   //me  傳來的
    {
        public long revision;
        public long getRevision() { return revision; }
        public int level;
        public int points;
        public int levelUpPoints;
    }

    public enum MailVerifiedState
    {
        None,
        UnVerified,
        Verified,
    }
}

