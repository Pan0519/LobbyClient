using CommonILRuntime.BindingModule;
using CommonILRuntime.Extension;
using CommonILRuntime.Module;
using CommonILRuntime.Tooltip;
using CommonService;
using CommonPresenter;
using Lobby.Jigsaw.FantasySelectorMethod;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Lobby.UI;
using Services;
using LobbyLogic.Audio;

namespace Lobby.Jigsaw
{
    /// <summary>
    /// 用來控制:自動選&全部取消
    /// </summary>
    class BinaryButtonController
    {
        public Action<bool> stateChangeAction = null;
        Button positiveButton;
        Button negativeButton;

        public BinaryButtonController(Button positiveButton, Button negativeButton)
        {
            this.positiveButton = positiveButton;
            this.negativeButton = negativeButton;

            positiveButton.onClick.AddListener(onPositive);
            negativeButton.onClick.AddListener(onNegative);

            this.positiveButton.gameObject.setActiveWhenChange(true);
            this.negativeButton.gameObject.setActiveWhenChange(false);
        }

        public void changeStateWithoutNotify(bool positive)
        {
            positiveButton.gameObject.setActiveWhenChange(positive);
            negativeButton.gameObject.setActiveWhenChange(!positive);
        }

        void onPositive()
        {
            positiveButton.gameObject.setActiveWhenChange(false);
            negativeButton.gameObject.setActiveWhenChange(true);
            stateChangeAction?.Invoke(true);
        }

        void onNegative()
        {
            positiveButton.gameObject.setActiveWhenChange(true);
            negativeButton.gameObject.setActiveWhenChange(false);
            stateChangeAction?.Invoke(false);
        }
    }

    class WheelTable
    {
        public FantasyWheelItemData[] outerWheelData;
        public FantasyWheelItemData[] innerWheelData;
        public long maxCoinReward;
    }

    public class FantasyWheelSelector : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/fantasy_wheel_choose_page";
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        //CONFIG
        const int PIECE_AVALIABLE_BASE_COUNT = 2;   //一張拼圖的擁有數量需超過此數量才會顯示於grid中

        const string TRIGGER_CLICK = "click";
        const string BOOL_AVALIABLE = "avaliable";

        //UI Bindings
        Button closeButton;
        Button autoSelectButton;
        Button cancelAllButton;
        Button goSpinButton;
        Button hintInfoButton;
        Button fantasyInfoButton;
        Button extendSpinButton;

        Text avaliableStarsText;
        Text fantasyStarCountText;

        Toggle showChosenPuzzleToggle;

        RectTransform piecesRoot;

        Animator goSpinButtonAnimator;
        GameObject enoughTipObj;

        //Private Datas
        List<JigsawAlbumData> cachedAlbumsDetail = new List<JigsawAlbumData>();
        List<RecyclingPiece> selectablePieces = new List<RecyclingPiece>();
        Dictionary<string, RecyclingPiece> selectedPieces;
        FantasyWheelDataProvider dataProvider;

        FantasyProgressBar progressBar;
        BinaryButtonController smartSelectButton;

        //Tooltip
        TooltipController fantasyTooltip;

        int avaliableStarsCount = 0;
        int targetFantasyCount = 0;
        int currentFantasyCount = 0;
        int avaliableWheelIdx = -1;

        WheelTable[] tables = new WheelTable[3];
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeButton = getBtnData("closeButton");
            autoSelectButton = getBtnData("autoSelectButton");
            cancelAllButton = getBtnData("cancelAllButton");
            goSpinButton = getBtnData("goSpinButton");
            hintInfoButton = getBtnData("hintInfoButton");
            fantasyInfoButton = getBtnData("fantasyInfoButton");
            extendSpinButton = getBtnData("extendSpinButton");

            goSpinButtonAnimator = getAnimatorData("goSpinButtonAnimator");

            avaliableStarsText = getTextData("avaliableStarsText");
            fantasyStarCountText = getTextData("fantasyStarCountText");

            showChosenPuzzleToggle = getBindingData<Toggle>("showChosenPuzzleToggle");
            enoughTipObj = getGameObjectData("enough_tip_obj");

            piecesRoot = getBindingData<RectTransform>("piecesRoot");

            progressBar = UiManager.bindNode<FantasyProgressBar>(getNodeData("progressBar").cachedGameObject);

            //Tooltips
            fantasyTooltip = UiManager.bindNode<TooltipController>(getNodeData("fantasyTooltip").cachedGameObject);
            fantasyTooltip.close();
        }

        public override void init()
        {
            base.init();
            dataProvider = new FantasyWheelDataProvider();
            enoughTipObj.setActiveWhenChange(false);
            closeButton.onClick.AddListener(closeBtnClick);
            goSpinButton.onClick.AddListener(onSpinFantasyWheel);
            extendSpinButton.onClick.AddListener(onExtendSpinBttonClick);
            hintInfoButton.onClick.AddListener(onHintClick);
            showChosenPuzzleToggle.onValueChanged.AddListener(ignoreUnSelected);
            selectedPieces = new Dictionary<string, RecyclingPiece>();
            //Auto Select, Cancel
            smartSelectButton = new BinaryButtonController(autoSelectButton, cancelAllButton);
            smartSelectButton.stateChangeAction = onSmartSelect;
            fantasyInfoButton.onClick.AddListener(onFantasyInfoClick);
            fetchTableInfo(0);
            fetchTableInfo(1);
            fetchTableInfo(2);
            reset();
        }

        void reset()
        {
            selectedPieces.Clear();

            //Avaliable Star
            avaliableStarsText.text = $"{0}";

            //Smart SelectButton
            smartSelectButton.changeStateWithoutNotify(true);

            //Fantasy Star Related
            fantasyStarCountText.text = "";
            targetFantasyCount = dataProvider.getFantasyTargetCount(DataStore.getInstance.playerInfo.level);
            updateFantasyStarUI(0);

            //ProgressBar Related
            var targetStars = dataProvider.getNormarStarConditions(DataStore.getInstance.playerInfo.level);
            progressBar.initTargetStars(targetStars);

            //Grid Content
            fetchDataAndShow();

            updateProgress();
        }

        public override void animOut()
        {
            clear();
        }

        async void fetchDataAndShow()
        {
            BindingLoadingPage.instance.open();
            autoSelectButton.interactable = false;
            showChosenPuzzleToggle.enabled = false;
            cachedAlbumsDetail = await JigsawDataHelper.getInTimeAllAlbumDetail();
            changeContent(cachedAlbumsDetail);
            showChosenPuzzleToggle.enabled = true;
            showChosenPuzzleToggle.isOn = false;
            BindingLoadingPage.instance.close();
        }

        void changeContent(List<JigsawAlbumData> content)
        {
            for (int i = 0; i < selectablePieces.Count; i++)
            {
                var piece = selectablePieces[i];
                piece.selectCountChangeListener = null;
            }
            selectablePieces.Clear();

            Extention.cleanRoot(piecesRoot);
            List<JigsawPieceData> pieces = new List<JigsawPieceData>();
            for (int i = 0; i < content.Count; i++)
            {
                var albumDetail = content[i];
                pieces.AddRange(albumDetail.pieces);
            }

            //拆選出符合規格條件者(擁有數量大於等於N張)
            var filteredPieces = pieces.FindAll(pieceData =>
            {
                return pieceData.getCount() >= PIECE_AVALIABLE_BASE_COUNT;
            });

            bool havePieces = filteredPieces.Count > 0;
            enoughTipObj.setActiveWhenChange(!havePieces);
            autoSelectButton.interactable = havePieces;
            //稀有度高>>低, 星等低>>高
            filteredPieces.Sort((x, y) =>
             {
                 if (x.getRareLevel() == y.getRareLevel())
                 {
                     return x.getStarLevel() < y.getStarLevel() ? -1 : 1;
                 }
                 return x.getRareLevel() < y.getRareLevel() ? 1 : -1;
             });

            //setCount調整資料為可選擇最大數量
            for (int i = 0; i < filteredPieces.Count; i++)
            {
                var filteredPiece = filteredPieces[i];
                filteredPiece.setCount(filteredPiece.getCount() - (PIECE_AVALIABLE_BASE_COUNT - 1));
                var piece = PieceFactory.createReclcyingPiece(filteredPiece, piecesRoot);
                piece.selectCountChangeListener = onPuzzleCountChange;
                piece.setSelectedCount(0);
                piece.registerSelected();
                selectablePieces.Add(piece);
            }

            computeAvaliableStars();
            updateAvaliableStars();
            updateLayout();
        }

        void computeAvaliableStars()
        {
            avaliableStarsCount = 0;
            for (int i = 0; i < selectablePieces.Count; i++)
            {
                var piece = selectablePieces[i];
                avaliableStarsCount += piece.avaliableStarCount;
            }
        }

        void updateAvaliableStars()
        {
            int selectedStarCount = 0;
            var pieceValues = selectedPieces.GetEnumerator();
            while (pieceValues.MoveNext())
            {
                selectedStarCount += pieceValues.Current.Value.selectedStarCount;
            }
          
            var remainCount = avaliableStarsCount - selectedStarCount;
            avaliableStarsText.text = $"{remainCount}";
        }

        void updateLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(piecesRoot); //GridRoot
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)piecesRoot.parent);   //RectTransform
        }

        void setCurrentStarCount(int count)
        {
            fantasyStarCountText.text = $"{count}";
        }

        void onSmartSelect(bool isSelect)
        {
            if (isSelect)
            {
                var selector = new FantasyFirstSelector();
                var targetStars = dataProvider.getNormarStarConditions(DataStore.getInstance.playerInfo.level);
                selector.select(selectablePieces, targetStars, targetFantasyCount);
                AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.MaxbetBtn));
                return;
            }
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            //不操作 selectedPieces, 避免因數值改變連動造成 selectedPieces iterator race Condition
            for (int i = 0; i < selectablePieces.Count; i++)
            {
                var piece = selectablePieces[i];
                piece.setSelectedCount(0);
            }
        }

        void ignoreUnSelected(bool ignore)
        {
            for (int i = 0; i < selectablePieces.Count; i++)
            {
                var piece = selectablePieces[i];
                bool show = piece.getSelectCount() > 0 || !ignore;
                piece.uiGameObject.setActiveWhenChange(show);
            }
        }

        void onHintClick()
        {
            UiManager.getPresenter<FantasyHint>();
        }

        void onSpinFantasyWheel()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            var presenter = UiManager.getPresenter<FantasyWheelGame>();
            presenter.close();
            presenter.setSpinSuccessListener(onSpinSuccess);
            presenter.setRecyclePieces(new List<RecyclingPiece>(selectedPieces.Values));
            var table = tables[avaliableWheelIdx];
            presenter.setWheelInfo(avaliableWheelIdx, table.outerWheelData, table.innerWheelData, currentFantasyCount >= targetFantasyCount);
            presenter.setMaxCoinReward(table.maxCoinReward);
            presenter.open();
        }

        void onPuzzleCountChange(RecyclingPiece changedPiece)
        {
            var pieceId = changedPiece.data.ID;
            if (0 == changedPiece.getSelectCount())
            {
                selectedPieces.Remove(pieceId);
                if (0 == selectedPieces.Count)
                {
                    smartSelectButton.changeStateWithoutNotify(true);
                }
            }
            else if (1 == changedPiece.getSelectCount() && !selectedPieces.ContainsKey(pieceId))
            {
                selectedPieces.Add(pieceId, changedPiece);
                smartSelectButton.changeStateWithoutNotify(false);
            }

            updateProgress();
            updateAvaliableStars();
        }

        void updateProgress()
        {
            int starCount = 0;
            currentFantasyCount = 0;
            var piecesEnum = selectedPieces.GetEnumerator();
            while (piecesEnum.MoveNext())
            {
                var piece = piecesEnum.Current.Value;
                starCount += piece.selectedStarCount;
                if (piece.isFantasy)
                {
                    currentFantasyCount += piece.getSelectCount();
                }
            }

            updateProgressBar(starCount);
            updateFantasyStarUI(currentFantasyCount);
        }

        async void fetchTableInfo(int wheelLevel)
        {
            await syncTableInfo(wheelLevel);
        }

        void updateProgressBar(int starCount)
        {
            avaliableWheelIdx = progressBar.setCurrentStar(starCount);
            if (avaliableWheelIdx >= 0)
            {
                if (avaliableWheelIdx == 0)
                {
                    goSpinButtonAnimator.SetBool(BOOL_AVALIABLE, true);
                }

                if (!extendSpinButton.interactable && 0 == avaliableWheelIdx)
                {
                    onExtendSpinBttonClick();
                }

                extendSpinButton.interactable = true;
            }
            else
            {
                goSpinButtonAnimator.SetBool(BOOL_AVALIABLE, false);
                extendSpinButton.interactable = false;
            }
        }

        void onExtendSpinBttonClick()
        {
            goSpinButtonAnimator.SetTrigger(TRIGGER_CLICK);
        }

        void updateFantasyStarUI(int starCount)
        {
            string progressMessage;
            if (starCount > targetFantasyCount)
            {
                progressMessage = $"<color=#ff5757ff>{starCount}</color> / {targetFantasyCount}";
            }
            else
            {
                progressMessage = $"{starCount} / {targetFantasyCount}";
            }
            fantasyStarCountText.text = progressMessage;
        }

        void onFantasyInfoClick()
        {
            fantasyTooltip.open();
        }

        async Task syncTableInfo(int wheelLevel)
        {
            if (wheelLevel < 0)
            {
                return;
            }
            var response = await AppManager.lobbyServer.getRecycleAlbumWheelTable(wheelLevel);
            if (Result.OK != response.result)
            {
                return;
            }

            var table = new WheelTable() { outerWheelData = response.outerWheel, innerWheelData = response.innerWheel };
            tables[wheelLevel] = table;
            updateMaxCoinReward(response.outerWheel, response.innerWheel, wheelLevel);
        }

        void updateMaxCoinReward(FantasyWheelItemData[] outerWheelData, FantasyWheelItemData[] innerWheelData, int wheelLevel)
        {
            //找出外圈最大獎項金額
            List<FantasyWheelItemData> outerWheelItemList = new List<FantasyWheelItemData>(outerWheelData);
            var coinItems = outerWheelItemList.FindAll(itemData => itemData.kind.Equals(UtilServices.outcomeCoinKey));
            long maxOuterCoin = 0;
            if (coinItems.Count > 0)
            {
                coinItems.Sort((x, y) =>
                {
                    var xCoin = (long)x.type;
                    var yCoin = (long)y.type;
                    return xCoin < yCoin ? 1 : -1;
                });
                maxOuterCoin = (long)coinItems[0].type;
            }

            //找出內圈最大獎倍率
            int maxInnerBoost = 1;
            if (currentFantasyCount >= targetFantasyCount)
            {
                List<FantasyWheelItemData> innerWheelItemList = new List<FantasyWheelItemData>(innerWheelData);
                var boostItems = innerWheelItemList.FindAll(itemData => itemData.kind.Equals(1));
                if (boostItems.Count > 0)
                {
                    boostItems.Sort((x, y) =>
                    {
                        var xCoin = (long)x.type;
                        var yCoin = (long)y.type;
                        return xCoin < yCoin ? 1 : -1;
                    });
                    maxInnerBoost = (int)boostItems[0].type;
                }
            }
            var maxCoinReward = maxOuterCoin * maxInnerBoost;
            tables[wheelLevel].maxCoinReward = maxCoinReward;
            progressBar.setMaxCoinReward(maxCoinReward, wheelLevel);
        }

        void onSpinSuccess()
        {
            closePresenter();
        }
    }
}
