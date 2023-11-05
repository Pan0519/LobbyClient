using Binding;
using Common.VIP;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using CommonService;
using CommonPresenter;
using Lobby.UI;
using Lobby.VIP.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.VIP
{
    class VipInfoBoardPresenter : SystemUIBasePresenter
    {
        public override string objPath { get { return "prefab/vip_info/vip_main"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        Button closeButton;

        Text currentPoint;
        Text progressText;
        Text yearText;
        Image progressBarImg;
        GameObject progressBarGroup;

        Image currentLevel;
        Image nextLevel;
        GameObject nextLevelGroup;

        BindingNode dragableRoot;
        BindingNode vipTitleRoot;
        BindingNode profitTitleRoot;
        BindingNode lightFrameEffect;
        RectTransform lightFrameRoot;

        GameObject vipTitleUnitTemplate;
        GameObject profitTitleUnitTemplate;
        GameObject infoUnitTemplate;

        VipBoardSpriteProvider boardSpriteProvider;

        VipDashboardData data;
        VipUiInfo vipUiInfo;

        CancellationTokenSource setEffectPosCancelable = null;
        List<VipProfitTitlePresenter> titleHintPresenters = new List<VipProfitTitlePresenter>();
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Vip) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeButton = getBtnData("closeButton");
            currentPoint = getTextData("currentPoint");
            progressText = getTextData("progressText");
            yearText = getTextData("yearText");
            progressBarImg = getImageData("progressBar");
            progressBarGroup = getGameObjectData("progressBarGroup");
            currentLevel = getImageData("currentLevel");
            nextLevel = getImageData("nextLevel");
            nextLevelGroup = getGameObjectData("nextLevelGroup");

            lightFrameEffect = getNodeData("lightFrameEffect");

            //資料表相關root
            dragableRoot = getNodeData("infoObj");
            vipTitleRoot = getNodeData("titleObj");
            profitTitleRoot = getNodeData("profitObj");
            lightFrameRoot = getBindingData<RectTransform>("lightFrameRoot");
        }

        public override void init()
        {
            base.init();
            closeButton.onClick.AddListener(closeBtnClick);

            UiManager.bindNode<DragablePresenter>(dragableRoot.cachedGameObject);
            UiManager.bindNode<DragLinkerPresenter>(vipTitleRoot.cachedGameObject);
            UiManager.bindNode<DragLinkerPresenter>(profitTitleRoot.cachedGameObject);
            UiManager.bindNode<DragLinkerPresenter>(lightFrameEffect.cachedGameObject);
            
            vipTitleUnitTemplate = ResourceManager.instance.getGameObjectWithResOrder("prefab/vip_info/vipTitleUnit",resOrder);
            profitTitleUnitTemplate = ResourceManager.instance.getGameObjectWithResOrder("prefab/vip_info/profitTitleUnit",resOrder);
            infoUnitTemplate = ResourceManager.instance.getGameObjectWithResOrder("prefab/vip_info/infoUnit",resOrder);

            boardSpriteProvider = new VipBoardSpriteProvider();
        }

        //將 Server 結構轉換成 VIP UI 自己的結構
        VipUiInfo toUiInfo(VipInfo input)
        {
            var uiInfo = new VipUiInfo();
            uiInfo.level = input.level;
            uiInfo.points = input.points;
            uiInfo.levelUpPoints = input.levelUpPoints;
            return uiInfo;
        }

        void setPlayerVipInfo(VipInfo vipData)
        {
            vipUiInfo = toUiInfo(vipData);

            setLevel(vipUiInfo.level);
            setPoint(vipUiInfo.points);
        }

        public override async void open()
        {
            base.open();
            VipDashboardData data = new VipDashboardData();
            data.levels = await VipJsonData.getLevelInfos();
            setPlayerVipInfo(DataStore.getInstance.playerInfo.myVip.info);
            this.data = data;
            VipLevelData[] levels = data.levels;
            if (0 < levels.Length)
            {
                int levelCount = levels.Length - 1;
                dragableRoot.GetComponent<GridLayoutGroup>().constraintCount = levelCount;
                for (int i = 0; i < levelCount; i++)
                {
                    addTitleGrid(levels[i].title);  //填第一橫列 (Vip等級)
                    VipProfit[] profits = levels[i].profits;
                  
                    string spriteName = (i % 2) == 0 ? "bg_red" : "bg_darkred";
                    for (int j = 0; j < profits.Length - 1; j++)
                    {
                        addInfoGrid(profits[j], boardSpriteProvider.getPofitBgSprite(spriteName));
                    }
                }
                fillProfitGrid(levels[0].profits);  //填滿第一直行 (Vip加權)
            }

            yearText.text = DateTime.Now.Year.ToString();
        }

        public override void animOut()
        {
            if (null != setEffectPosCancelable)
            {
                setEffectPosCancelable.Cancel();
            }
            for (int i = 0; i < titleHintPresenters.Count; ++i)
            {
                titleHintPresenters[i].clearTips();
            }
            clear();
        }
        void addTitleGrid(VipTitle title)
        {
            var titleObj = GameObject.Instantiate(vipTitleUnitTemplate, vipTitleRoot.cachedTransform);
            var p = UiManager.bindNode<VipTitlePresenter>(titleObj);
            p.setSpriteProvider(boardSpriteProvider);
            p.setData(title);

        }

        void fillProfitGrid(VipProfit[] profits)
        {
            for (int i = 0; i < profits.Length - 1; i++)
            {
                VipProfit data = profits[i];
                var titleObj = GameObject.Instantiate(profitTitleUnitTemplate, profitTitleRoot.cachedTransform);
                var p = UiManager.bindNode<VipProfitTitlePresenter>(titleObj);
                p.openTipAction = openTips;
                p.setSpriteProvider(boardSpriteProvider);
                p.setData(data);
                titleHintPresenters.Add(p);
            }
        }

        void openTips()
        {
            for (int i = 0; i < titleHintPresenters.Count; ++i)
            {
                titleHintPresenters[i].clearTips();
            }
        }

        void addInfoGrid(VipProfit profit, Sprite sprite)
        {
            var infoObj = GameObject.Instantiate(infoUnitTemplate, dragableRoot.cachedTransform);
            var info = UiManager.bindNode<VipProfitValuePresenter>(infoObj);
            info.setData(profit);
            info.setBgSprite(sprite);
        }

        int getNextLevel()
        {
            if (vipUiInfo.levelUpPoints > 0)
            {
                return vipUiInfo.level + 1;
            }
            return -1;
        }

        async void setLevel(int level)
        {
            currentLevel.sprite = VipSpriteGetter.getNameSprite(level);
            nextLevelGroup.setActiveWhenChange(true);
            int nextLevelId = getNextLevel();
            if (nextLevelId > 0)
            {
                nextLevel.sprite = VipSpriteGetter.getNameSprite(nextLevelId);
            }
            else
            {
                nextLevelGroup.setActiveWhenChange(false);
            }

            //設定特效框位置
            lightFrameRoot.gameObject.setActiveWhenChange(false);
            setEffectPosCancelable = new CancellationTokenSource();
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            if (setEffectPosCancelable.Token.IsCancellationRequested)
            {
                return;
            }
            var childIdx = -1;
            for (int i = 0; i < data.levels.Length; i++)
            {
                var levelData = data.levels[i];
                if (levelData.title.level == level)
                {
                    childIdx = i;
                    break;
                }
            }
            //Debug.Log($"setLevel, childIdx: {childIdx}");
            if (childIdx >= 0)
            {
                lightFrameRoot.gameObject.setActiveWhenChange(true);
                lightFrameRoot.anchoredPosition = ((RectTransform)vipTitleRoot.transform.GetChild(childIdx)).anchoredPosition;
            }
        }

        void setPoint(int points)
        {
            currentPoint.text = points.ToString();
            progressBarGroup.setActiveWhenChange(true);
            int nextLevelId = getNextLevel();
            if (nextLevelId > 0)
            {
                int nextPoints = vipUiInfo.levelUpPoints;
                float progress = points / (float)nextPoints;
                progressBarImg.fillAmount = progress;
                progressText.text = $"{(int)(progress * 100)}%";
                return;
            }
            progressBarGroup.setActiveWhenChange(false);
        }
    }
}
