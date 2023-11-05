using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using CommonILRuntime.Services;
using UnityEngine.UI;
using UnityEngine;
using LobbyLogic.Audio;
using Lobby.Audio;
using Game.Common;
using EventActivity;
using UniRx;
using System;
using System.Collections.Generic;

namespace Event.Common
{
    public class PrizeAward : ContainerPresenter, ILongValueTweenerHandler
    {
        Animator prizeBoosterAnim;
        GameObject prizeBoosterItem;
        GameObject prizeBoosterObj = null;
        RectTransform rootRect;
        public ulong prizeBoosterOriginalReward { get; private set; }
        public ulong awardValue { get; private set; }
        RectTransform coinRect;
        public virtual float YOffset { get; set; } = 5.0f;

        public override void initUIs()
        {
            prizeBoosterAnim = getAnimatorData("prize_anim");
            prizeBoosterObj = getGameObjectData("prize_booster_obj");
            coinRect = getBindingData<RectTransform>("money_group_trans");
            rootRect = getRectData("root_rect");
        }

        public virtual Button getCollectBtn()
        {
            return null;
        }
        public virtual Text getCoinTxt()
        {
            return null;
        }

        public void setPrizeItem(GameObject prizeItem)
        {
            prizeBoosterItem = prizeItem;
        }

        public void setAwardValue(ulong value)
        {
            awardValue = value;
            prizeBoosterOriginalReward = ActivityDataStore.isPrizeBooster ? (awardValue / 2) : awardValue;
        }

        public void onValueChanged(ulong value)
        {
            getCoinTxt().text = value.ToString("N0");
        }

        public GameObject getDisposableObj()
        {
            return uiGameObject;
        }

        public void checkIsShowPrizeBooster()
        {
            if (!ActivityDataStore.isPrizeBooster || null == prizeBoosterItem)
            {
                getCollectBtn().interactable = true;
                return;
            }

            prizeBoosterItem.transform.SetParent(rootRect);
            prizeBoosterItem.transform.localScale = Vector3.one;
            prizeBoosterItem.setActiveWhenChange(true);
            Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
            {
                movePrizeItem();
            }).AddTo(uiGameObject);
        }

        void movePrizeItem()
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.PrizeUpIconFly));
            Vector2 prizePos = prizeBoosterItem.transform.position;
            Vector2 middlePos = new Vector2(prizePos.x + (prizePos.x / 4), prizePos.y + YOffset);
            BezierPresenter bezierPresenter = UiManager.bind<BezierPresenter>(prizeBoosterItem);
            bezierPresenter.bezierPoints = new List<Vector2>() { prizePos, middlePos, getCoinTxt().gameObject.transform.position };
            bezierPresenter.moveBezierLine(0.5f, () =>
            {
                prizeBoosterAnim.SetTrigger("in");
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.PrizeUpIconIn));
                prizeBoosterObj.setActiveWhenChange(true);
                LongValueTweener prizeTween = new LongValueTweener(this, awardValue - prizeBoosterOriginalReward);
                prizeTween.onComplete = () =>
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(coinRect);
                    getCollectBtn().interactable = true;
                };
                prizeTween.setRange(prizeBoosterOriginalReward, awardValue);
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.PrizeRunCoin));
                prizeBoosterItem.setActiveWhenChange(false);
                //GameObject.DestroyImmediate(prizeBoosterItem);
            });
        }

    }
}
