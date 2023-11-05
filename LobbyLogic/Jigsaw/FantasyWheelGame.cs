using Common.Jigsaw;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Extension;
using CommonILRuntime.Module;
using CommonILRuntime.Outcome;
using CommonPresenter;
using LobbyLogic.NetWork.RequestStruce;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using Service;
using System;
using System.Collections.Generic;
using TweenModule;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonService;
using Services;
using static ApplicationConfig;
using Lobby.Common;

namespace Lobby.Jigsaw
{
    class WheelItem : NodePresenter
    {

    }

    class Wheel : NodePresenter
    {
        public Action rollOverListener = null;
        //private property
        protected RoulatteTurnTable wheel;

        public override void initUIs()
        {
            base.initUIs();
        }

        public void initWheel(int itemCount, int baseTurnCount, int baseTurnTime, int slowDownTurnCount, int slowDownTurnTime)
        {
            wheel = new RoulatteTurnTable();
            var wheelInfo = new RoulatteTurnTable.TurntableInfo()
            {
                gameObject = uiGameObject,
                totalChildCount = itemCount,
                firstTurns = baseTurnCount,
                firstTurnTime = baseTurnTime,
                lastTurns = slowDownTurnCount,
                lastTurnTime = slowDownTurnTime,
                overNotify = onRollOver,
                stopNotify = onStopping
            };
            wheel.setTurntableInfo(wheelInfo);
        }

        //外圈一般輪帶都是 coin
        public virtual void setTable(FantasyWheelItemData[] itemDatas)
        {
            int itemIdx = 0;
            while (true)
            {
                var itemName = $"wheelItem_{itemIdx}";
                var item = uiTransform.Find(itemName);
                if (null == item)
                {
                    break;
                }

                if (itemIdx >= itemDatas.Length)
                {
                    break;
                }

                var data = itemDatas[itemIdx];
                var coin = (ulong)data.type; //type is coin value
                var coinText = item.GetComponent<Text>();
                if (null != coinText)
                {
                    coinText.text = coin.convertToCurrencyUnit(showLong: 3, havePoint: true, pointDigits: 1);
                }
                itemIdx++;
            }
        }

        public virtual void startRoll(int itemIdx)
        {
            wheel.setTargetID(itemIdx);
            wheel.startRoll();
        }

        void onStopping()
        {
            //Do nothing currently
        }

        void onRollOver()
        {
            rollOverListener?.Invoke();
        }
    }

    class FantasyWheel : Wheel
    {
        GameObject wheelMask;
        public override void initUIs()
        {
            base.initUIs();
            wheelMask = getGameObjectData("innerWheelMask");
        }

        public override void init()
        {
            base.init();
        }

        public override void startRoll(int itemIdx)
        {
            wheel.setTargetID(itemIdx);
            wheel.startRoll();
        }

        public void showEnable(bool enable)
        {
            wheelMask.setActiveWhenChange(!enable);
        }

        public override void setTable(FantasyWheelItemData[] itemDatas)
        {
            int itemIdx = 0;
            while (true)
            {
                var itemName = $"wheelItem_{itemIdx}";
                var item = uiTransform.Find(itemName);
                if (null == item)
                {
                    break;
                }

                if (itemIdx >= itemDatas.Length)
                {
                    break;
                }

                var itemData = itemDatas[itemIdx];

                switch (itemData.kind)
                {
                    case 2:
                    case 3:
                        {
                            var image = item.GetComponent<Image>();
                            if (null != image)
                            {
                                var puzzlePackId = (PuzzlePackID)itemData.type;
                                image.sprite = JigsawPackSpriteProvider.getPackSprite(puzzlePackId);
                            }
                        }
                        break;
                    case 1:
                        {
                            var coinText = item.GetComponentInChildren<Text>();
                            coinText.text = $"x{itemData.type}";
                        }
                        break;
                }
                itemIdx++;
            }
        }
    }

    public class FantasyWheelGame : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/fantasy_wheel_main";
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        //UiBindings
        Button closeButton;
        Button spinButton;
        Text rewardText;
        GameObject normalRollOverEffect;
        GameObject fantasyRollOverEffect;

        GameObject enTitleObj;
        GameObject zhTitleObj;

        Image wheelRimgImage;

        //private property
        Wheel normalWheel;
        FantasyWheel fantasyWheel;
        int waitCount = 0;
        List<RecyclingPiece> recyclePieces = null;
        CommonReward[] rewards = null;
        Action onSpinSuccessListener = null;
        bool spinSuccess = false;
        List<GameObject> rollOverEffect = new List<GameObject>();
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle)};
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            base.initUIs();

            closeButton = getBtnData("closeButton");
            spinButton = getBtnData("spinButton");
            rewardText = getTextData("rewardText");

            normalRollOverEffect = getGameObjectData("normalRollOverEffect");
            fantasyRollOverEffect = getGameObjectData("fantasyRollOverEffect");

            zhTitleObj = getGameObjectData("zhTitleObj");
            enTitleObj = getGameObjectData("enTitleObj");

            wheelRimgImage = getImageData("wheelRimgImage");
        }

        public override void init()
        {
            base.init();
            normalWheel = UiManager.bindNode<Wheel>(getNodeData("normalWheel").cachedGameObject);
            fantasyWheel = UiManager.bindNode<FantasyWheel>(getNodeData("fantasyWheel").cachedGameObject);
            closeButton.onClick.AddListener(closeBtnClick);
            spinButton.onClick.AddListener(onSpinClick);

            normalWheel.initWheel(12, 5, 3, 2, 2);
            normalWheel.rollOverListener = onOneWheelFinish;
            //因為 RoletteTurnTable:initRoll 會重設 global rotation,同時影響轉輪自身的 local rotation,
            //這裡的轉輪比較特殊，中獎區在90度，已於prefab中父節點轉過，所以重設 local rotation為0, 讓父層決定中獎區就好
            normalWheel.uiTransform.localRotation = Quaternion.Euler(Vector3.zero);

            fantasyWheel.initWheel(8, 5, 4, 2, 5);
            fantasyWheel.rollOverListener = onOneWheelFinish;
            //因為 RoletteTurnTable:initRoll 會重設 global rotation,同時影響轉輪自身的 local rotation,
            //這裡的轉輪比較特殊，中獎區在90度，已於prefab中父節點轉過，所以重設 local rotation為0, 讓父層決定中獎區就好
            fantasyWheel.uiTransform.localRotation = Quaternion.Euler(Vector3.zero);

            normalRollOverEffect.setActiveWhenChange(false);
            fantasyRollOverEffect.setActiveWhenChange(false);

            rewardText.text = "0";

            zhTitleObj.setActiveWhenChange(Language.ZH == nowLanguage);
            enTitleObj.setActiveWhenChange(Language.ZH != nowLanguage); //不是ZH，預設EN

            spinSuccess = false;
        }

        public override void animOut()
        {
            if (spinSuccess)
            {
                onSpinSuccessListener?.Invoke();
            }
            clear();
        }

        public void setRecyclePieces(List<RecyclingPiece> pieces)
        {
            recyclePieces = pieces;
        }

        public void setWheelInfo(int wheelLevel, FantasyWheelItemData[] outerWheel, FantasyWheelItemData[] innerWheel, bool hasInnerWheel)
        {
            fantasyWheel.showEnable(hasInnerWheel);

            //設定盤面
            normalWheel.setTable(outerWheel);
            fantasyWheel.setTable(innerWheel);

            changeWheelRing(wheelLevel);
        }

        public void setMaxCoinReward(long reward)
        {
            rewardText.text = reward.ToString("N0");
        }

        public void setSpinSuccessListener(Action listener)
        {
            onSpinSuccessListener = listener;
        }

        AlbumRecycleRequestData getRequestStruct()
        {
            if (null != recyclePieces)
            {
                var requestData = new AlbumRecycleRequestData();
                var itemList = new List<AlbumRecycleItem>();
                for (int i = 0; i < recyclePieces.Count; i++)
                {
                    var piece = recyclePieces[i];
                    var item = new AlbumRecycleItem(piece.data.ID, piece.getSelectCount());
                    itemList.Add(item);
                }
                requestData.items = itemList.ToArray();
                return requestData;
            }
            return null;
        }

        void onSpinClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.SpinBtn));
            spinButton.interactable = false;
            closeButton.interactable = false;
            rollOverEffectActive(false);

            var requestStruct = getRequestStruct();
            if (null != requestStruct)
            {
                requestRecycle(requestStruct);
                return;
            }
            Debug.LogWarning("FantasyWheelGame getRequestStruct failed");
            closeButton.interactable = true;
        }

        async void onOneWheelFinish()
        {
            waitCount--;
            if (0 == waitCount)
            {
                rollOverEffectActive(true);
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(AlbumAudio.SpinEnd));
                await Task.Delay(TimeSpan.FromSeconds(2));
                closeButton.interactable = true;
                doReward();
            }
        }

        async void requestRecycle(AlbumRecycleRequestData requestData)
        {
            var response = await AppManager.lobbyServer.recycleAlbumItems(requestData);
            if (Result.OK == response.result)
            {
                NoticeManager.instance.getPuzzleStarAmount(false);
                spinSuccess = true;
                handleWheelResponse(response);
            }
            else
            {
                Debug.LogError("requestRecycle failed");
                closeBtnClick();
            }
        }

        void handleWheelResponse(AlbumRecycleResponse response)
        {
            JigsawDataHelper.updateRecycleTime(response.availableAfter);
            rollOverEffect.Clear();
            int outerWheelResult = response.outerWheelResult;
            int innerWheelResult = response.innerWheelResult;
            rewards = response.rewards;

            waitCount = innerWheelResult >= 0 ? 2 : 1;

            rollOverEffect.Add(normalRollOverEffect);
            normalWheel.startRoll(outerWheelResult + 1);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(AlbumAudio.Spin));
            if (innerWheelResult >= 0)
            {
                rollOverEffect.Add(fantasyRollOverEffect);
                fantasyWheel.startRoll(innerWheelResult + 1);
            }
        }

        void doReward()
        {
            if (null == rewards)
            {
                return;
            }

            var rewardList = new List<CommonReward>(rewards);
            var coinRewards = rewardList.FindAll(reward => reward.kind.Equals(UtilServices.outcomeCoinKey));

            ulong totalCoin = 0;
            for (int i = 0; i < coinRewards.Count; i++)
            {
                totalCoin = +coinRewards[i].getAmount();
            }

            var packReward = rewardList.Find(reward => reward.kind.Equals("puzzle-pack"));
            var packId = null == packReward ? string.Empty : packReward.type;

            var voucherReward = rewardList.Find(reward => reward.kind.Equals("puzzle-voucher"));
            if (string.IsNullOrEmpty(packId))
            {
                packId = null != voucherReward ? PuzzleTypeConverter.wildTypeToPuzzlePackID(voucherReward.type) : string.Empty;
            }

            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
            var rewardPresenter = UiManager.getPresenter<FantasyWheelResultPresenter>();
            rewardPresenter.setReward(totalCoin, packId);
            if (null != packReward)
            {
                rewardPresenter.closeListener = () =>
                JigsawPack.OpenPackRewards(rewardList, closePresenter);
                return;
            }

            if (null != voucherReward)
            {
                rewardPresenter.closeListener = () => WildPack.openWildPack(voucherReward, closePresenter);
                return;
            }
            rewardPresenter.closeListener = closePresenter;
        }

        void rollOverEffectActive(bool isActivte)
        {
            for (int i = 0; i < rollOverEffect.Count; ++i)
            {
                rollOverEffect[i].setActiveWhenChange(isActivte);
            }
        }
        void changeWheelRing(int wheelLevel)
        {
            var nameIdx = wheelLevel + 1;
            var spriteName = $"bg_fs_wheel_frame_{nameIdx}";
            var sprite = ResourceManager.instance.loadWithResOrder<Sprite>($"prefab/lobby_puzzle/pic/fantasy_wheel/{spriteName}",resOrder);
            wheelRimgImage.sprite = sprite;
        }
    }
}
