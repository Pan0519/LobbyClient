using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using CommonPresenter.PackItem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lobby.Common;
using LobbyLogic.NetWork.ResponseStruct;
using UniRx;
using System.Threading.Tasks;
using Services;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace MagicForest
{
    public class ForestStageNode : NodePresenter
    {
        #region StageUI
        Text stageNumTxt;
        RectTransform stageItemLayout;
        #endregion

        #region StageInfo
        Text stageRewardTxt;
        RectTransform stageInfoRect;
        RectTransform stageInfoLayout;
        RectTransform stagePackRect;
        Text buffText;
        GameObject buffObj;
        ScrollRect stageItemScroll;
        Animator buffMoreAnim;
        #endregion

        MagicForestStageReward[] rewards;
        const int moveCount = 5;
        float maxMoveY;
        float moveUnit;
        int initStageID;
        int maxStage;
        int nowStage;
        bool isInfoOpening;

        Dictionary<int, StageItemNode> stageItemNodes = new Dictionary<int, StageItemNode>();
        public Subject<StageItemNode> openInfoSub { get; private set; } = new Subject<StageItemNode>();
        public Action closeInfoCB;
        public bool isRunningReward { get; private set; }
        GameObject stageTempObj;

        float endY { get { return Mathf.Max((nowStage - 1) * moveUnit * -1, maxMoveY); } }
        public override void initUIs()
        {
            stageItemLayout = getRectData("stage_item_layout");
            stageItemScroll = getBindingData<ScrollRect>("stage_scroll");
            stageNumTxt = getTextData("stage_num_txt");
            stageInfoRect = getRectData("stage_info_rect");
            stageRewardTxt = getTextData("stage_reward_txt");
            stageInfoLayout = getRectData("stage_info_layout");
            stagePackRect = getRectData("stage_pack_group");

            buffObj = getGameObjectData("buff_more_obj");
            buffText = getTextData("more_buff_txt");
            buffMoreAnim = getAnimatorData("anim_more");
        }

        public override void init()
        {
            stageInfoRect.gameObject.setActiveWhenChange(false);
            stageTempObj = ResourceManager.instance.getGameObjectWithResOrder("prefab/activity/magic_forest/stage_level_item",AssetBundleData.getBundleName(BundleType.MagicForest));
            moveUnit = stageTempObj.GetComponent<RectTransform>().rect.height;
        }

        public void updateStageNum(int nowStage)
        {
            this.nowStage = nowStage;
            stageNumTxt.text = $"{nowStage}/{maxStage}";
            stageItemNodes[nowStage].setEffectActive(true);
            if (nowStage > 1)
            {
                StageItemNode lastItem;
                if (stageItemNodes.TryGetValue(nowStage - 1, out lastItem))
                {
                    lastItem.setEffectActive(false);
                }
            }
            var itemNode = stageItemNodes.GetEnumerator();
            while (itemNode.MoveNext())
            {
                itemNode.Current.Value.isCheck(itemNode.Current.Value.id < nowStage);
            }

            if (nowStage - initStageID >= moveCount)
            {
                Debug.Log($"updateStageNum nowStage {nowStage} - initStageID {initStageID} >= moveCount {moveCount}");
                moveStageItemLayout(endY);
            }
        }

        void checkStageScrollPos(Action moveEndCB)
        {
            bool isPosInRange = true;
            float totalMove = moveUnit * (nowStage - initStageID);
            float checkMiniPosY = endY + totalMove;
            float checkMaxPosY = Mathf.Max(endY - totalMove, maxMoveY);
            if (stageItemLayout.anchoredPosition.y > checkMiniPosY || stageItemLayout.anchoredPosition.y < checkMaxPosY)
            {
                isPosInRange = false;
            }

            if (!isPosInRange)
            {
                moveStageItemLayout(endY, moveEndCB);
            }
            else if (null != moveEndCB)
            {
                moveEndCB();
            }
        }
        public void moveStageItemLayout(float endY, Action endCB = null)
        {
            string twID = TweenManager.tweenToFloat(stageItemLayout.anchoredPosition.y, endY, durationTime: 0.6f, onUpdate: changeItemLayout, onComplete: () =>
              {
                  initStageID = nowStage;
                  Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
                  {
                      if (null != endCB)
                      {
                          endCB();
                      }
                  }).AddTo(uiGameObject);
              });
            TweenManager.tweenPlay(twID);
        }

        void changeItemLayout(float posY)
        {
            var newPos = stageItemLayout.anchoredPosition;
            newPos.Set(newPos.x, posY);
            stageItemLayout.anchoredPosition = newPos;
        }

        public async void initStageRewards(MagicForestStageReward[] rewards, int nowStage)
        {
            this.rewards = rewards;
            initStageID = nowStage;
            maxStage = rewards[rewards.Length - 1].StageNum;
            maxMoveY = (maxStage - moveCount - 1) * moveUnit * -1;
            showStageData(nowStage);
            updateStageNum(nowStage);
            await Task.Delay(TimeSpan.FromSeconds(0.2f));
            changeItemLayout(endY);
        }

        public int upStageLv()
        {
            if (nowStage >= maxStage)
            {
                return nowStage;
            }
            nowStage++;

            checkStageScrollPos(() =>
            {
                updateStageNum(nowStage);
            });

            return nowStage;
        }

        public StageItemNode getNowStageRewardData()
        {
            StageItemNode result = null;
            var itemNodes = stageItemNodes.GetEnumerator();
            while (itemNodes.MoveNext())
            {
                if (itemNodes.Current.Value.id >= nowStage && null != itemNodes.Current.Value.reward)
                {
                    result = itemNodes.Current.Value;
                    break;
                }
            }
            return result;
        }

        public StageItemNode getLastStageReward()
        {
            return stageItemNodes[maxStage];
        }

        public void addStageRewardAmount(ulong addAmount, long addBonus)
        {
            isRunningReward = true;
            StageItemNode nowStageItem = getNowStageRewardData();
            if (nowStageItem.id - initStageID > moveCount)
            {
                moveStageItemLayout(endY, () =>
                {
                    runStageReward(nowStageItem, addAmount, addBonus);
                });
                return;
            }
            checkStageScrollPos(() =>
            {
                runStageReward(nowStageItem, addAmount, addBonus);
            });
        }
        List<IDisposable> waitRunMoreBonus = new List<IDisposable>();
        string runRewardTWID;
        void runStageReward(StageItemNode nowStageItem, ulong addAmount, long addBonus)
        {
            UtilServices.disposeSubscribes(waitRunMoreBonus.ToArray());
            waitRunMoreBonus = new List<IDisposable>();

            stageItemScroll.enabled = false;
            openInfo(nowStageItem);

            buffObj.setActiveWhenChange(true);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.PrizeUpIconIn));
            buffMoreAnim.SetTrigger("get");

            waitRunMoreBonus.Add(Observable.TimerFrame(30).Subscribe(_ =>
            {
                buffText.text = $"{nowStageItem.reward.Bonus + addBonus}%";
            }).AddTo(uiGameObject));

            waitRunMoreBonus.Add(Observable.TimerFrame(45).Subscribe(_ =>
            {
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.PrizeRunCoin));
                runRewardTWID = TweenManager.tweenToLong((long)nowStageItem.reward.CompleteReward, (long)(nowStageItem.reward.CompleteReward + addAmount), durationTime: 0.8f, onUpdate: val =>
                {
                    stageRewardTxt.text = val.ToString("N0");
                }, onComplete: () =>
                {
                    stageRewardTxt.text = (nowStageItem.reward.CompleteReward + addAmount).ToString("N0");
                    isRunningReward = false;
                    nowStageItem.addStageRewardAmount(addAmount, addBonus);
                    bonusCloseInfo();
                    runRewardTWID = string.Empty;
                });

                TweenManager.tweenPlay(runRewardTWID);
            }).AddTo(uiGameObject));
        }

        void breakRunReward()
        {
            UtilServices.disposeSubscribes(waitRunMoreBonus.ToArray());
            if (string.IsNullOrEmpty(runRewardTWID))
            {
                TweenManager.tweenComplete(runRewardTWID);
            }
        }

        async void bonusCloseInfo()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            stageItemScroll.enabled = true;
            closeInfo();
        }

        public bool isLastStageNode(StageItemNode checkStage)
        {
            return checkStage.id == rewards[rewards.Length - 1].StageNum;
        }

        void showStageData(int nowLv)
        {
            if (stageItemNodes.Count < maxStage)
            {
                initStageItemData();
            }

            for (int i = 0; i < rewards.Length; ++i)
            {
                var reward = rewards[i];
                StageItemNode itemNode;
                if (!stageItemNodes.TryGetValue(reward.StageNum, out itemNode))
                {
                    continue;
                }
                itemNode.setStageReward(reward);
                if (reward.StageNum < maxStage)
                {
                    itemNode.setIconImage(StageStatus.End);
                    continue;
                }

                itemNode.setIconImage(StageStatus.Final);
            }

            var stageItemEnum = stageItemNodes.GetEnumerator();
            while (stageItemEnum.MoveNext())
            {
                stageItemEnum.Current.Value.isCheck(stageItemEnum.Current.Key < nowLv);
            }
        }
        void initStageItemData()
        {
            for (int i = 0; i < maxStage; ++i)
            {
                StageItemNode itemNode = UiManager.bindNode<StageItemNode>(GameObject.Instantiate(stageTempObj, stageItemLayout));
                int id = i + 1;
                itemNode.setID(id);
                itemNode.setIconImage(StageStatus.Normal);
                itemNode.uiGameObject.name = $"StageItem-{id}";
                itemNode.openInfoCB = openInfo;
                stageItemNodes.Add(id, itemNode);
            }
        }

        IDisposable infoCloseDis;
        void openInfo(StageItemNode openStageItem)
        {
            if (isInfoOpening)
            {
                closeInfo();
                return;
            }
            var stageReward = openStageItem.reward;
            if (null == stageReward)
            {
                return;
            }
            openInfoSub.OnNext(openStageItem);
            if (isLastStageNode(openStageItem))
            {
                return;
            }
            isInfoOpening = true;
            infoCloseDis = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ =>
            {
                closeInfo();
            }).AddTo(uiGameObject);
            stageItemScroll.enabled = false;
            stageInfoRect.transform.SetParent(openStageItem.uiTransform);
            var pos = stageInfoRect.anchoredPosition;
            pos.Set(pos.x, 0);
            stageInfoRect.anchoredPosition = pos;
            stageInfoRect.gameObject.setActiveWhenChange(true);
            stageRewardTxt.text = stageReward.CompleteReward.ToString("N0");
            buffObj.setActiveWhenChange(stageReward.Bonus > 0);
            buffText.text = $"{stageReward.Bonus}%";
            for (int i = 0; i < stageReward.CompleteItem.Length; ++i)
            {
                PackItemPresenterServices.getSinglePackItem(stageReward.CompleteItem[i].Type, stagePackRect);
            }

            stageInfoRect.transform.SetParent(uiTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(stageInfoLayout);
        }

        public void closeInfo()
        {
            if (null != closeInfoCB)
            {
                closeInfoCB();
            }
            isInfoOpening = false;
            stageItemScroll.enabled = true;
            stageInfoRect.gameObject.setActiveWhenChange(false);
            infoCloseDis.Dispose();
        }
    }

    public class StageItemNode : NodePresenter
    {
        Image iconImage;
        GameObject isCheckObj;
        Button stageBtn;
        GameObject effectObj;
        public int id { get; private set; }
        public MagicForestStageReward reward { get; private set; }
        public bool isChecking { get; private set; }
        public Action<StageItemNode> openInfoCB;

        public override void initUIs()
        {
            iconImage = getImageData("stage_icon");
            isCheckObj = getGameObjectData("check_item");
            stageBtn = getBtnData("stage_btn");
            effectObj = getGameObjectData("stage_effect_obj");
        }

        public override void init()
        {
            setEffectActive(false);
            stageBtn.onClick.AddListener(openStageInfo);
        }

        public void setID(int id)
        {
            this.id = id;
        }

        public void setIconImage(StageStatus stageName)
        {
            iconImage.sprite = getStageSprite(stageName);
        }

        public void setEffectActive(bool isActive)
        {
            effectObj.setActiveWhenChange(isActive);
        }

        public void isCheck(bool isCheck)
        {
            isChecking = isCheck;
            isCheckObj.setActiveWhenChange(isCheck);
        }

        public void setStageReward(MagicForestStageReward reward)
        {
            this.reward = reward;
        }

        public void addStageRewardAmount(ulong addAmount, long addBonus)
        {
            reward.CompleteReward += addAmount;
            reward.Bonus += addBonus;
        }

        void openStageInfo()
        {
            if (null != openInfoCB)
            {
                openInfoCB(this);
            }
        }

        Sprite getStageSprite(StageStatus stageName)
        {
            string stageSpriteName = $"icon_stage_{stageName}";
            return LobbySpriteProvider.instance.getSprite<ForestSpriteProvider>(LobbySpriteType.MagicForest, stageSpriteName.ToLower());
        }
    }

    public enum StageStatus
    {
        Normal,
        Final,
        End,
    }
}
