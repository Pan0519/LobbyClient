using Debug = UnityLogUtility.Debug;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Lobby.UI;
using CommonILRuntime.BindingModule;
using CommonService;
using Binding;
using UniRx;
using System;
using System.Collections.Generic;
using Lobby.Common;
using Services;
using LobbyLogic.Audio;

namespace Mission
{
    public class ActivityQuestInfoPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/quest_mission/quest_info_board";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        #region UI Obj
        private Animator boardAni;
        private Image gameLogo;
        private RectTransform questLayout;
        private Button btnStart;
        private BindingNode questItem;
        #endregion

        #region Other
        private IDisposable closeAniCallBack;
        private List<PoolObject> poolObjList = new List<PoolObject>();
        #endregion

        public override void initUIs()
        {
            boardAni = getAnimatorData("board_ani");
            gameLogo = getImageData("game_logo_img");
            questLayout = getRectData("quest_layout_rect");
            btnStart = getBtnData("start_btn");
            questItem = getNodeData("quest_item_node");
        }

        public override void init()
        {
            btnStart.onClick.AddListener(onBtnStart);
            getQuestInfo();
            BindingLoadingPage.instance.close();
            boardAni.SetTrigger("in");
            questItem.gameObject.setActiveWhenChange(false);

            float scale = 1.0f;
            switch (UtilServices.getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    scale = 0.8f;
                    break;
            }

            var rootScale = boardAni.transform.localScale;
            rootScale.Set(scale, scale, scale);
            boardAni.transform.localScale = rootScale;

            if (DataStore.getInstance.dataInfo.getChooseBetClassType() == BetClass.Adventure && ApplicationConfig.environment < ApplicationConfig.Environment.Stage)
            {
                createTestBtn();
            }
        }

        private void createTestBtn()
        {
            GameObject go = new GameObject();
            RectTransform rect = go.AddComponent<RectTransform>();
            Image image = go.AddComponent<Image>();
            Button btn = go.AddComponent<Button>();

            rect.parent = gameMsgUiRoot;
            rect.anchoredPosition3D = new Vector3(Screen.width / 2, 0, 0);
            rect.localScale = Vector3.one;
            image.sprite = ResourceManager.instance.load<Sprite>(Services.UtilServices.getLocalizationAltasPath("btn_backtogame"));
            image.raycastTarget = true;
            btn.onClick.AddListener(forcePass);
        }

        private async void forcePass()
        {
            var completeNotice = await Service.AppManager.lobbyServer.setNewbieAdventureComplete();
            var redeem = await Service.AppManager.lobbyServer.getNewbieAdventureRedeem();
            SaveTheDog.SaveTheDogMapData.instance.updateAdventureRecord(redeem.adventureRecord);
            var rewardPack = await Service.AppManager.lobbyServer.getRewardPacks(redeem.rewardPackId);
            UiManager.getPresenter<ActivityQuestRewardPresenter>().getRewardInfo(rewardPack.rewards);
        }

        private void getQuestInfo()
        {
            BindingLoadingPage.instance.open();
            addQuestObj();
            setTitleInfo();
        }

        private void onBtnStart()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            boardAni.SetTrigger("out");
            closeAniCallBack = Observable.TimerFrame(33).Subscribe(_ =>
            {
                closeAniCallBack.Dispose();
                var itemPoolsEnum = poolObjList.GetEnumerator();
                while (itemPoolsEnum.MoveNext())
                {
                    ResourceManager.instance.returnObjectToPool(itemPoolsEnum.Current.cachedGameObject);
                }
                poolObjList.Clear();
                clear();
            }).AddTo(uiGameObject);
        }

        private void addQuestObj()
        {
            string questType = "";
            string[] questCondition = null;
            for (var i = 0; i < ActivityQuestData.missions.Length; i++)
            {
                PoolObject questObj = ResourceManager.instance.getObjectFromPool(questItem.cachedGameObject, questLayout.transform);
                if (!questObj)
                {
                    continue;
                }
                var mission = ActivityQuestData.missions[i];
                poolObjList.Add(questObj);
                var questInfoList = mission.progress;
                var questNode = UiManager.bindNode<AcvitityQuestInfoItem>(questObj.cachedGameObject);
                questType = questInfoList.type;
                questCondition = ActivityQuestData.convertToConditionsMsg(questInfoList.conditions);
                questNode.setQuestContent(questType, questCondition);
            }
        }

        private async void setTitleInfo()
        {
            var gameID = await DataStore.getInstance.dataInfo.getNowplayGameID();
            gameLogo.sprite = LobbySpriteProvider.instance.getSprite<ActivityQuestProvider>(LobbySpriteType.ActivityQuest, $"game_short_{gameID}");
        }
    }
}
