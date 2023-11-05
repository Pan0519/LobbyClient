using CommonILRuntime.Extension;
using CommonILRuntime.Module;
using CommonILRuntime.Services;
using CommonService;
using System;
using UniRx;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CommonPresenter
{
    public class TopMiniPricePresenter : NodePresenter
    {
        #region UIs
        Text priceTxt;
        RectTransform flyCoinPos;
        #endregion

        ulong bonusCoin = 0;
        Action onHideComplete;

        const float priceHidePosY = 450.0f;
        const float showPosY = 280.0f;

        public override void initUIs()
        {
            priceTxt = getTextData("price");
            flyCoinPos = getBindingData<RectTransform>("fly_coin_pos");
        }

        public override void init()
        {
            uiRectTransform.anchoredPosition = new Vector2(uiRectTransform.anchoredPosition.x, priceHidePosY);
        }

        public void showPrice(ulong price, Action callback = null)
        {
            onHideComplete = callback;
            bonusCoin = price;
            priceTxt.text = price.convertToCurrencyUnit(showLong: 4, havePoint: true, pointDigits: 3);
            uiGameObject.setActiveWhenChange(true);
            movePrice();
            //TweenManager.tweenToFloat(MINI_PRICE_HIDE_POS_Y, MINI_PRICE_SHOW_POS_Y, 0.5f, onUpdate: setPosY, onComplete: playCoinFly);
        }

        public void playCoinFly()
        {
            IDisposable flyTimer = null;
            flyTimer = Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
            {
                CoinFlyHelper.reverseCurveFly(flyCoinPos, DataStore.getInstance.playerInfo.myWallet.coin, DataStore.getInstance.playerInfo.myWallet.coin + bonusCoin, 0.5f, false, coinFlyComplete);
                flyTimer.Dispose();
            });
        }

        void coinFlyComplete()
        {
            DataStore.getInstance.playerInfo.myWallet.unsafeAdd(bonusCoin);
            hidePrice();
        }

        public virtual void movePrice()
        {
            uiRectTransform.anchPosMoveY(showPosY, 0.35f, onComplete: playCoinFly);
        }

        public virtual void hidePrice()
        {
            IDisposable hideTimer = null;
            hideTimer = Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                uiRectTransform.anchPosMoveY(priceHidePosY, 0.35f, onComplete: endPrice);
                hideTimer.Dispose();
            });
        }

        void endPrice()
        {
            uiGameObject.setActiveWhenChange(false);
            if (null != onHideComplete)
            {
                onHideComplete();
            }
        }

    }
}
