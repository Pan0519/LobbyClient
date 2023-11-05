using Common.VIP;
using Lobby.VIP.UI;
using UnityEngine.UI;
using System;
using CommonILRuntime.BindingModule;
using UniRx;

namespace Lobby.VIP
{
    public class VipProfitTitlePresenter : VipSubjectUnit
    {
        Image titleImg;
        Button tipButton;

        public Action openTipAction = null;

        VipProfit data;
        VipProfitHint hintPresenter = null;

        public override void initUIs()
        {
            base.initUIs();
            titleImg = getImageData("titleImg");
            tipButton = getBtnData("tipButton");
        }

        public override void init()
        {
            base.init();
            tipButton.onClick.AddListener(onTipButtonClick);
        }

        public void setData(VipProfit data)
        {
            this.data = data;
            titleImg.sprite = spriteProvider.getProfitSprite(data.id);
        }

        public void clearTips()
        {
            if (null == hintPresenter)
            {
                return;
            }
            hintPresenter.clear();
            hintPresenter = null;
        }

        void onTipButtonClick()
        {
            if (null != openTipAction)
            {
                openTipAction();
            }
            openTip();
        }

        void openTip()
        {
            hintPresenter = UiManager.getPresenter<VipProfitHint>();
            hintPresenter.setInfoAndPos(getProfitHintMessage(), tipButton.gameObject, () =>
            {
                hintPresenter = null;
            });
        }

        string getProfitHintMessage()
        {
            var key = string.Empty;
            switch (data.id)
            {
                case VipProfitDef.COIN_DEAL:
                    {
                        key = "VIP_CoinDeals_Description";
                    }
                    break;
                case VipProfitDef.VIP_POINTS:
                    {
                        key = "VIP_VIPPoints_Description";
                    }
                    break;
                case VipProfitDef.SILVER_BOX:
                    {
                        key = "VIP_SilverBox_Description";
                    }
                    break;
                case VipProfitDef.GOLDEN_BOX:
                    {
                        key = "VIP_GlodenBox_Description";
                    }
                    break;
                case VipProfitDef.STORE_BONUS:
                    {
                        key = "VIP_StoreGift_Description";
                    }
                    break;
            }
            return LanguageService.instance.getLanguageValue(key);
        }
    }
}
