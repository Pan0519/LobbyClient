using LobbyLogic.NetWork.ResponseStruct;
using CommonService;
using DG.Tweening;
using UnityEngine;
using System;
using Debug = UnityLogUtility.Debug;

namespace LobbyLogic.Common
{
    public static class LobbyPlayerInfo
    {
        public static void setPlayerInfo(PlayerInfoResponse info)
        {
            for (int i = 0; i < info.levelUpRewards.Length; ++i)
            {
                var rewards = info.levelUpRewards[i];
                DataStore.getInstance.dataInfo.setLvupRewardData(rewards.kind, rewards.amount);
            }
            DataStore.getInstance.playerInfo.setLv(info.level);
            DataStore.getInstance.playerInfo.setLvUpExp(info.levelUpExp);
            DataStore.getInstance.playerInfo.setPlayerExpAndSubject(info.exp);
            DataStore.getInstance.playerInfo.setUserID(info.id);
            DataStore.getInstance.playerInfo.setIconIdx(info.iconIndex);
            DataStore.getInstance.playerInfo.setName(info.name);
            DataStore.getInstance.playerInfo.setFBImageUrl(info.photoUrl);
            DataStore.getInstance.playerInfo.setCoinExchangeRate(info.coinExchangeRate);
            DataStore.getInstance.playerInfo.setExpBoostEndTime(info.expBoostEndedAt);
            DataStore.getInstance.playerInfo.setLvupBoostEndTime(info.levelUpRewardBoostEndedAt);
            DataStore.getInstance.playerInfo.setCreateTime(info.createdAt);

            DataStore.getInstance.playerInfo.myWallet.commitAndPush(info.wallet);
            DataStore.getInstance.playerInfo.myVip.commitAndPush(info.vip);

            if (info.bindings != null)
            {
                DataStore.getInstance.playerInfo.setIsBindingFB(info.bindings.ContainsKey("facebook"));

                BindingInfo bindingDetail;
                bindingDetail = getBindingInfo("phoneNumber");
                if (null != bindingDetail)
                {
                    DataStore.getInstance.playerInfo.UpdateBindingPhoneInfo(bindingDetail.value);
                }

                bindingDetail = getBindingInfo("email");
                if (null != bindingDetail)
                {
                    DataStore.getInstance.playerInfo.UpdateBindingEmailInfo(bindingDetail.value);
                }
            }

            BindingInfo getBindingInfo(string key)
            {
                BindingInfo bindingDetail = null;
                info.bindings.TryGetValue(key, out bindingDetail);
                return bindingDetail;
            }
        }
    }

    public static class CommonUtil
    {
        public static string getChipExpireGreenText(string time)
        {
            return $"<color=#00FE2A>{time}</color>";
        }

        public static string getChipExpirePurpleText(string time)
        {
            return $"<color=#F607FF>{time}</color>";
        }

        public static void connectToCustomerService()
        {
            var helper = new LaunchFBHelper();
            Application.OpenURL(helper.getURL());
        }

        public static float medlaOpenTime { get { return 0.3f; } }
        public static Ease medlaOpenType { get { return Ease.OutBack; } }

        public static float medlaCloseTime { get { return 0.25f; } }
        public static Ease medlaCloseType { get { return Ease.Linear; } }
    }

    public static class GamePauseManager
    {
        static int pauseCount = 0;
        public static void gamePause()
        {
            pauseCount++;
            DataStore.getInstance.gameTimeManager.Pause();
        }

        public static void gameResume()
        {
            if (pauseCount <= 0)
            {
                return;
            }
            pauseCount--;
            if (pauseCount > 0)
            {
                return;
            }
            DataStore.getInstance.gameTimeManager.Resume();
        }

        public static void clearCount()
        {
            pauseCount = 0;
        }
    }

    public static class LanguageManager
    {
        public static void checkLanguage()
        {
            string languageFileName = PlayerPrefs.GetString(ApplicationConfig.LanguageSaveKey);
            ApplicationConfig.Language language = ApplicationConfig.Language.EN;
            if (!Enum.TryParse(languageFileName.ToUpper(), out language))
            {
                return;
            }
            if (ApplicationConfig.nowLanguage == language)
            {
                return;
            }
            ApplicationConfig.nowLanguage = language;
            reloadLanguageFile();
        }
        static async void reloadLanguageFile()
        {
            string languageFileName = Enum.GetName(typeof(ApplicationConfig.Language), ApplicationConfig.nowLanguage).ToLower();
            string jsonFile = await WebRequestText.instance.loadTextFromServer($"language_{languageFileName}");
            LanguageService.instance.initLanguageFile(jsonFile);
        }
    }
}
