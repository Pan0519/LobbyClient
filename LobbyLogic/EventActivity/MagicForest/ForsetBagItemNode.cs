using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Services;
using UnityEngine.UI;
using UnityEngine;
using UniRx;
using LobbyLogic.NetWork.ResponseStruct;
using CommonILRuntime.Extension;
using CommonService;
using EventActivity;
using System;
using System.Threading.Tasks;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace MagicForest
{
    public class ForsetBagItemNode : NodePresenter
    {
        Animator openAnim;
        Button openBtn;
        GameObject moneyItem;
        GameObject hammerItem;
        Image hammerImg;
        GameObject bagItem;
        Image coinImg;
        Text coinAmountTxt;
        Animator moneyBoosterAnim;

        public Action shopSpinClick = null;
        public int selfID { get; private set; }
        public BagItemKind bagKind { get; private set; }
        public Subject<ForsetBagItemNode> showHammerFlySub = new Subject<ForsetBagItemNode>();
        public Subject<ForsetBagItemNode> openSub { get; private set; } = new Subject<ForsetBagItemNode>();
        ulong rewardAmount;
        ulong prizeOriginalAmount;
        Color grayColor;
        GameObject prizeItemObj;
        RectTransform bagItemGroup;
        public override void initUIs()
        {
            openAnim = getAnimatorData("bag_item_anim");
            openBtn = getBtnData("bag_item_btn");
            moneyItem = getGameObjectData("bag_item_money");
            hammerItem = getGameObjectData("bag_item_hammer");
            hammerImg = getImageData("bag_hammer_img");
            bagItem = getGameObjectData("bag_obj");
            coinImg = getImageData("coin_img");
            coinAmountTxt = getTextData("coin_amount_txt");
            moneyBoosterAnim = getAnimatorData("bag_item_money_anim");
        }

        public override void init()
        {
            openBtn.onClick.AddListener(openClick);
            float colorVal = 85.0f / 255.0f;
            grayColor = new Color(colorVal, colorVal, colorVal);
        }

        public void showBagHistory(ActivityReward historyData)
        {
            setBagKind(historyData);
            showItemKind();
            changeCoinToGray();
            changeHammerToGray();
        }

        void setBagKind(ActivityReward reward)
        {
            bagKind = BagItemKind.None;
            if (null != reward)
            {
                bagKind = ForestDataServices.getBagKind(reward.Kind);
                rewardAmount = reward.getAmount;
                prizeOriginalAmount = ActivityDataStore.isPrizeBooster ? rewardAmount / 2 : rewardAmount;
            }
        }
        void showItemKind()
        {
            updateCoinAmount(prizeOriginalAmount);
            moneyItem.setActiveWhenChange(BagItemKind.Coin == bagKind);
            hammerItem.setActiveWhenChange(BagItemKind.Hammer == bagKind);
            bagItem.setActiveWhenChange(BagItemKind.Bag == bagKind);
            openBtn.interactable = BagItemKind.Bag == bagKind;
        }

        //public void onValueChanged(long value)
        //{
        //    updateCoinAmount(value);
        //}

        void updateCoinAmount(ulong amount)
        {
            coinAmountTxt.text = amount.convertToCurrencyUnit(3, havePoint: true, pointDigits: 1);
        }

        public void showBagItem(ActivityReward reward)
        {
            setBagKind(reward);
            startCheckBagAnimPlaying();
        }

        public void setPrizeItem(GameObject prizeItem, RectTransform bagItemGroup)
        {
            prizeItemObj = prizeItem;
            this.bagItemGroup = bagItemGroup;
        }

        void startCheckBagAnimPlaying()
        {
            IDisposable checkUpdataDis = null;
            checkUpdataDis = Observable.TimerFrame(30).Subscribe(_ =>
            {
                checkUpdataDis.Dispose();
                showItemKind();
                switch (bagKind)
                {
                    case BagItemKind.Coin:
                        checkPrizeItem();
                        break;
                    case BagItemKind.Hammer:
                        flyHammer();
                        break;
                }
            }).AddTo(uiGameObject);
        }

        async void checkPrizeItem()
        {
            if (!ActivityDataStore.isPrizeBooster)
            {
                flyCoinItem();
                return;
            }
            Vector2 anchorsPos = new Vector2(0.5f, 0.5f);
            RectTransform prizeRect = prizeItemObj.GetComponent<RectTransform>();
            prizeRect.anchorMin = anchorsPos;
            prizeRect.anchorMax = anchorsPos;
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            calculatePrizeFinalPosAndMove();
        }

        //void runPrizeValue()
        //{
        //    GameObject.DestroyImmediate(prizeItemObj);
        //    LongValueTweener prizeTween = new LongValueTweener(this, rewardAmount - prizeOriginalAmount);
        //    prizeTween.onComplete = () =>
        //    {
        //        flyCoinItem();
        //    };
        //    prizeTween.setRange(prizeOriginalAmount, rewardAmount);
        //}

        async void flyCoinItem()
        {
            if (BagItemKind.Coin != bagKind)
            {
                return;
            }
            updateCoinAmount(rewardAmount);
            ulong sourceValue = DataStore.getInstance.playerInfo.playerMoney;
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            CoinFlyHelper.frontSFly(uiRectTransform, sourceValue, sourceValue + rewardAmount, onComplete: () =>
            {
                DataStore.getInstance.playerInfo.myWallet.unsafeAdd(rewardAmount);
                ForestDataServices.stopShowing();
            });
            changeCoinToGray();
        }

        async void flyHammer()
        {
            if (BagItemKind.Hammer != bagKind)
            {
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            showHammerFlySub.OnNext(this);
        }

        public void setBagID(int id)
        {
            selfID = id;
        }

        public void showFlyHammer()
        {
            bagItem.setActiveWhenChange(false);
            moneyItem.setActiveWhenChange(false);
            hammerItem.GetComponent<Animator>().enabled = false;
            hammerItem.setActiveWhenChange(true);
        }
        public void changeHammerToGray()
        {
            if (BagItemKind.Hammer != bagKind)
            {
                return;
            }
            hammerImg.color = grayColor;
        }

        void changeCoinToGray()
        {
            if (BagItemKind.Coin != bagKind)
            {
                return;
            }
            coinAmountTxt.color = grayColor;
            coinImg.color = grayColor;
        }

        void openClick()
        {
            if (ForestDataServices.isShowing)
            {
                return;
            }
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityMFAudio.OpenBag));
            if (ForestDataServices.totalTicketCount <= 0)
            {
                UiManager.getPresenter<ForestShopPresenter>().openShop(isShowSpinObj: true, shopSpinClick);
                return;
            }
            ForestDataServices.isShowing = true;
            openBtn.interactable = false;
            openAnim.SetTrigger("open");
            openSub.OnNext(this);
        }
        int[] bagFinalIDs = new int[] { 5, 10, 15, 20 };
        int commonOffsetNum = 10;
        void calculatePrizeFinalPosAndMove()
        {
            int groupID = selfID % 5;
            int bagID = 0;
            for (int i = 0; i <= bagFinalIDs.Length; ++i)
            {
                if (selfID < bagFinalIDs[i])
                {
                    bagID = i;
                    break;
                }
            }

            int halfNum = 2;
            int groupIDDifference = groupID - halfNum;
            float posX = bagItemGroup.anchoredPosition.x + (uiRectTransform.rect.width * groupIDDifference) + (commonOffsetNum * groupIDDifference);
            int bagIDRange = halfNum - bagID;
            float heightOffset;
            int extraY;
            switch (bagIDRange)
            {
                case 0:
                case 1:
                    heightOffset = 0.5f;
                    extraY = 1;
                    break;
                default:
                    heightOffset = 1.5f;
                    extraY = 2;
                    break;
            }
            float offsetY = (uiRectTransform.rect.height * heightOffset) + (extraY * commonOffsetNum);
            if (bagID <= 1)
            {
                offsetY = offsetY * -1;
            }
            float posY = bagItemGroup.anchoredPosition.y - offsetY;
            RectTransform prizeRect = prizeItemObj.GetComponent<RectTransform>();
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.PrizeUpIconFly));
            string moveTweenID = prizeRect.anchPosMove(new Vector2(posX, posY), 0.8f, onComplete: () =>
              {
                  moneyBoosterAnim.SetTrigger("money_booster");
                  prizeItemObj.setActiveWhenChange(false);
                  Observable.TimerFrame(11).Subscribe(_ =>
                  {
                      flyCoinItem();
                  }).AddTo(uiGameObject);
              }, easeType: DG.Tweening.Ease.InBack);

            string scaleTweenID = TweenManager.tweenToFloat(prizeItemObj.transform.localScale.x, 0.5f, durationTime: 0.3f, delayTime: 0.5f, onUpdate: (scale) =>
                 {
                     prizeItemObj.transform.localScale = new Vector3(scale, scale, scale);
                 });
            TweenManager.tweenPlayByID(moveTweenID, scaleTweenID);
        }
    }
}
