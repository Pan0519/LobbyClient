using UniRx;
using UniRx.Triggers;
using CommonILRuntime.Module;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;
using CommonILRuntime.BindingModule;
using Services;
using CommonILRuntime.Services;
using CommonService;
using LoginReward;
using CommonILRuntime.Outcome;
using EventActivity;
using Lobby.Common;
using System.Threading.Tasks;

namespace Mission
{
    public class MissionBaseRewardPresenter : ContainerPresenter
    {
        public override UiLayer uiLayer { get => UiLayer.System; }

        Animator showAnim;
        Button collectBtn;
        RectTransform itemGroupRect;
        GridLayoutGroup layoutGroup;
        Action animOutCallbackAction;
        ulong bonusCoin;
        protected ulong playerFinalCoin;

        const string REWARD_ITEM_PACK = "prefab/reward_item/reward_item_pack";
        const string REWARD_ITEM = "prefab/reward_item/reward_item";

        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.DailyMission) };
            base.initContainerPresenter();
        }

        public override void initUIs()
        {
            showAnim = getAnimatorData("show_anim");
            collectBtn = getBtnData("collect_btn");
            itemGroupRect = getRectData("item_group");
            layoutGroup = itemGroupRect.GetComponent<GridLayoutGroup>();
        }

        public override void init()
        {
            collectBtn.onClick.AddListener(flyCoinAni);
        }

        public void addItemDatas(List<CommonReward> items)
        {
            for (int i = 0; i < items.Count; ++i)
            {
                updateBonusCoin(items[i]);
            }

            setRewardItems(items);
        }

        void updateBonusCoin(CommonReward itemData)
        {
            var rewardKind = ActivityDataStore.getAwardKind(itemData.kind);
            if (AwardKind.Coin == rewardKind)
            {
                bonusCoin += itemData.getAmount();
            }
        }

        void setRewardItems(List<CommonReward> items)
        {
            if (4 >= items.Count)
            {
                updateRewardInfos(items, 25f, 0.7f);
            }
            else
            {
                updateRewardInfos(items, 0f, 0.6f);
            }
        }

        async void updateRewardInfos(List<CommonReward> items, float spacingXValue, float itemScale)
        {
            int count = items.Count;

            updateLauyout(spacingXValue);
            for (int i = 0; i < count; ++i)
            {
                obtainRewardItem(items[i], itemScale);
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
            }
        }

        void updateLauyout(float spacingXValue)
        {
            var spacing = layoutGroup.spacing;

            spacing.x = spacingXValue;
            layoutGroup.spacing = spacing;
        }

        void obtainRewardItem(CommonReward commonReward, float itemScale)
        {
            AwardKind awardKind = ActivityDataStore.getAwardKind(commonReward.kind);

            switch (awardKind)
            {
                case AwardKind.PuzzlePack:
                case AwardKind.PuzzleVoucher:
                    obtainPuzzleItem(commonReward, itemScale);
                    break;
                case AwardKind.Coin:
                case AwardKind.HighRollerPassPoint:
                    obtainNormalRewardItem(commonReward, itemScale);
                    break;
            }
        }

        void obtainPuzzleItem(CommonReward commonReward, float itemScale)
        {
            PoolObject poolObject = ResourceManager.instance.getObjectFromPool(REWARD_ITEM_PACK, itemGroupRect);

            poolObject.cachedTransform.localScale = new Vector3(itemScale, itemScale, itemScale);
            var packPresenter = UiManager.bindNode<RewardPackItemNode>(poolObject.gameObject);
            packPresenter.setPuzzlePack(commonReward.type);
        }

        void obtainNormalRewardItem(CommonReward commonReward, float itemScale)
        {
            PoolObject poolObject = ResourceManager.instance.getObjectFromPool(REWARD_ITEM, itemGroupRect);
            poolObject.cachedTransform.localScale = new Vector3(itemScale, itemScale, itemScale);
            var rewardPresenter = UiManager.bindNode<RewardItemNode>(poolObject.gameObject);
            rewardPresenter.setRewardData(commonReward);
        }

        public void animPlayTrigger(string triggerName)
        {
            showAnim.SetTrigger(triggerName);
        }

        void flyCoinAni()
        {
            collectBtn.interactable = false;
            CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), (playerFinalCoin - bonusCoin), playerFinalCoin , 0.7f, false, animOut);
        }

        void animOut()
        {
            var triggers = showAnim.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < triggers.Length; ++i)
            {
                triggers[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAnimOut).AddTo(uiGameObject);
            }
            showAnim.SetTrigger("out");
        }

        private void onAnimOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                bonusCoin = 0;
                animOutCallback();
                animTimerDis.Dispose();
            });
        }

        public void setAnimoutCallback(Action animOutCallback)
        {
            animOutCallbackAction = animOutCallback;
        }


        void animOutCallback()
        {
            if (null != animOutCallbackAction)
            {
                animOutCallbackAction();
            }
            clear();
        }
    }

    public abstract class MissionRewardPresenter : MissionBaseRewardPresenter
    {
        protected const float delayShowItemTime = 1f;
        public virtual void openReward(List<CommonReward> items, ulong playerFinalCoin)
        {
            this.playerFinalCoin = playerFinalCoin;
            delayShowItem(items);
            open();
        }

        async void delayShowItem(List<CommonReward> items)
        {
            await Task.Delay(TimeSpan.FromSeconds(delayShowItemTime));
            addItemDatas(items);
        }
    }

    public class MissionNormalRewardPresenter : MissionRewardPresenter
    {
        private GameObject[] numObj = null;
        public override string objPath => UtilServices.getOrientationObjPath("prefab/daily_mission/daily_normal_mission_reward");

        public override void initUIs()
        {
            base.initUIs();
            initNumObj();
            initUiItem();
        }

        void initNumObj()
        {
            numObj = new GameObject[3];
            for (int i = 0; i < 3; ++i)
            {
                numObj[i] = getGameObjectData($"mission_num_{(i + 1)}");
            }
        }

        void initUiItem()
        {
            Image titleImage = getImageData("title_image");
            titleImage.sprite = LobbySpriteProvider.instance.getSprite<DailyMissionProvider>(LobbySpriteType.Mission, "tex_normal_mission_rewards");
        }

        public override void openReward(List<CommonReward> items, ulong playerFinalCoin)
        {
            if (MissionData.normalRound <= 0)
            {
                return;
            }
            base.openReward(items, playerFinalCoin);
            updateMissionNum();
            animPlayTrigger("in");
        }

        private void updateMissionNum()
        {
            int count = numObj.Length;
            bool isActive = false;

            for (int i = 0; i < count; ++i)
            {
                isActive = ((i + 1) == MissionData.normalRound);
                numObj[i].setActiveWhenChange(isActive);
            }
        }
    }

    public class MissionSpecialRewardPresenter : MissionRewardPresenter
    {
        public override string objPath => UtilServices.getOrientationObjPath("prefab/daily_mission/daily_special_mission_reward");

        public override void initUIs()
        {
            base.initUIs();
            Image titleImage = getImageData("title_image");
            titleImage.sprite = LobbySpriteProvider.instance.getSprite<DailyMissionProvider>(LobbySpriteType.Mission, "tex_special_mission_rewards");
        }

        public override void openReward(List<CommonReward> items, ulong playerFinalCoin)
        {
            base.openReward(items, playerFinalCoin);
            animPlayTrigger("in");
        }
    }

    public class MissionPrizePresenter : MissionRewardPresenter
    {
        public override string objPath => UtilServices.getOrientationObjPath("prefab/daily_mission/daily_prize");

        public override void initUIs()
        {
            base.initUIs();
            Image titleImage = getImageData("title_image");
            Image titleImageEffect = getImageData("title_image_effect");
            Image systemImage = getImageData("system_image");

            titleImage.sprite = LobbySpriteProvider.instance.getSprite<DailyMissionProvider>(LobbySpriteType.Mission, "title_congratulation_daily_mission");
            titleImageEffect.sprite = LobbySpriteProvider.instance.getSprite<DailyMissionProvider>(LobbySpriteType.Mission, "title_congratulation_flow");
            systemImage.sprite = LobbySpriteProvider.instance.getSprite<DailyMissionProvider>(LobbySpriteType.Mission, "title_daily_mission");
        }
    }
}
