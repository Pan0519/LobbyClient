using CommonILRuntime.Services;
using CommonService;
using LobbyLogic.NetWork.ResponseStruct;
using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Outcome;
using Lobby.Jigsaw;
using EventActivity;

namespace Lobby.Mail
{
    public class SystemMailPresenter : MailPresenter
    {
        Text title;
        Text context;

        RectTransform coinRectTrans;
        SystemMessage systemMessageData;
        AwardKind awardKind;
        public SystemMailPresenter()
        {
            onGet = null;
        }

        public override void initUIs()
        {
            base.initUIs();
            title = getTextData("title");
            context = getTextData("context");
            coinRectTrans = getBindingData<RectTransform>("coinRectTrans");
        }

        public override void setData(IMessage data)
        {
            this.data = data;
            systemMessageData = (SystemMessage)data;
            title.text = LanguageService.instance.getLanguageValue(systemMessageData.title);
            setRemainTime(systemMessageData.endTime);
            if (null != systemMessageData.rewards)
            {
                setRewards(systemMessageData.rewards);
            }
        }

        void setRewards(Reward[] rewards)
        {
            onGet = onGetClickHandler;
            Reward reward = rewards[0];
            awardKind = ActivityDataStore.getAwardKind(reward.kind);
            string languageValue = LanguageService.instance.getLanguageValue(systemMessageData.context);
            switch (awardKind)
            {
                case AwardKind.Coin:
                    context.text = $"{languageValue} {reward.amount.ToString("N0")}";
                    break;

                case AwardKind.PuzzlePack:
                case AwardKind.PuzzleVoucher:
                    context.text = languageValue;
                    break;

                default:
                    context.text = languageValue;
                    break;
            }
        }

        async void onGetClickHandler()
        {
            getButton.enabled = false;
            var helper = new MailBoxProvider();
            var rewards = await helper.redeemMail(systemMessageData.Id);
            if (null == rewards)
            {
                readed();
                return;
            }
            awardKind = ActivityDataStore.getAwardKind(rewards[0].kind);
            switch (awardKind)
            {
                case AwardKind.Coin:
                    var outcome = Outcome.process(rewards);
                    var sourceValue = DataStore.getInstance.playerInfo.myWallet.deprecatedCoin;
                    var targetValue = DataStore.getInstance.playerInfo.playerMoney;

                    CoinFlyHelper.frontSFly(coinRectTrans, sourceValue, targetValue,
                        onComplete: () =>
                        {
                            outcome.apply();
                            readed();
                        });
                    break;

                case AwardKind.PuzzlePack:
                case AwardKind.PuzzleVoucher:
                    OpenPackWildProcess.openPackWild(rewards, readed);
                    break;

                default:
                    readed();
                    break;
            }
        }
    }
}
