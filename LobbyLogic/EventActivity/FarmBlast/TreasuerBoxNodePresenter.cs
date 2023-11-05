using CommonILRuntime.Module;
using UnityEngine.UI;
using System;
using UnityEngine;
using UniRx;
using EventActivity;
using Services;
using LobbyLogic.NetWork.ResponseStruct;
using UniRx.Triggers;
using Lobby.Common;

namespace FarmBlast
{
    public class TreasuerBoxNodePresenter : NodePresenter
    {
        #region UIs
        Image boxIconImg;
        Text boxShowTxt;

        Animator boxAnim;
        Button boxBtn;
        GameObject boxTimeObj;
        Image boxTimeImg;
        public RectTransform packItemGroup { get; private set; }
        #endregion

        public Subject<TreasuerBoxNodePresenter> observeClick = new Subject<TreasuerBoxNodePresenter>();
        public int boxID { get; private set; }
        public TreasureBoxType boxType { get; private set; }

        TimerService treasureTimer = new TimerService();
        Action animFinishCB;
        public override void initUIs()
        {
            boxIconImg = getImageData("img_treasure");
            boxShowTxt = getTextData($"treasure_timer_txt");
            boxAnim = getAnimatorData($"anim_treasure");
            boxBtn = getBtnData("btn_treasure");
            boxTimeObj = getGameObjectData("treasure_time_obj");
            boxTimeImg = getImageData("treasure_time_img");
            packItemGroup = getRectData("packItem_dummy");
        }
        public override void init()
        {
            treasureTimer.setAddToGo(uiGameObject);
            boxBtn.onClick.AddListener(sendClick);
        }
        private void animEnterSubscribe(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                if (null != animFinishCB)
                {
                    animFinishCB();
                }
                animTimerDis.Dispose();
                animTriggerDis.Dispose();
            });
        }

        public void initBoxData(int id)
        {
            boxID = id;
            setTreasureboxType(TreasureBoxType.None);
        }

        public void initTreasureData(TreasureBox treasureBoxData)
        {
            long nowTime = UtilServices.nowUtcTimeSeconds;
            boxType = convertBoxType(treasureBoxData.Type);
            setTreasureboxType(boxType);

            if (TreasureBoxType.None == boxType)
            {
                return;
            }

            if (treasureBoxData.AvailableStartTime <= nowTime)
            {
                showOpenText();
                return;
            }
            startTimer(treasureBoxData.AvailableStartTime);
        }

        void setTreasureboxType(TreasureBoxType status)
        {
            boxType = status;
            boxIconImg.sprite = getSprite(getPicName(boxType));
            boxTimeObj.setActiveWhenChange(TreasureBoxType.None != status);
        }

        public void updateBoxType(string type, long countDownTime)
        {
            Debug.Log("updateBoxType closeBtnInteractable");
            closeBtnInteractable();
            TreasureBoxType status = convertBoxType(type);
            setTreasureboxType(status);
            if (countDownTime > 0)
            {
                long endTime = UtilServices.nowUtcTimeSeconds + countDownTime;
                startTimer(endTime);
            }
            else
            {
                showOpenText();
                openBtnInteractable();
            }

            if (TreasureBoxType.None == status)
            {
                openBtnInteractable();
                boxAnim.SetTrigger("back");
            }
        }
        Sprite getSprite(string name)
        {
            return LobbySpriteProvider.instance.getSprite<EventActivitySpriteProvider>(LobbySpriteType.EventActivity, name);
        }

        string getPicName(TreasureBoxType boxType)
        {
            if (TreasureBoxType.None == boxType)
            {
                return "activity_treasure_non_common";
            }
            return $"activity_treasure_lv{(int)boxType}";
        }

        public void setAnimFinishCB(Action finishCB)
        {
            animFinishCB = finishCB;
        }

        public void showOpenText()
        {
            openBtnInteractable();
            boxShowTxt.text = string.Empty;
            boxTimeImg.sprite = getSprite("bg_activity_box_open");
        }

        public void startTimer(long targetTime)
        {
            treasureTimer.ExecuteTimer();
            closeBtnInteractable();
            if (targetTime <= 0)
            {
                return;
            }
            boxTimeImg.sprite = getSprite("bg_activity_box_time");
            treasureTimer.StartTimeByTimestamp(targetTime, updateTime);
        }

        IDisposable animTriggerDis;

        public void playGetAnim()
        {
            var animTrigger = boxAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(animEnterSubscribe);
            boxAnim.SetTrigger("get");
        }

        public void updateTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero)
            {
                treasureTimer.ExecuteTimer();
                showOpenText();
                return;
            }
            TimeStruct timeStruct = UtilServices.toTimeStruct(time);
            boxShowTxt.text = string.Format("{0:00}:{1:00}", timeStruct.minutes, timeStruct.seconds);
        }

        private void sendClick()
        {
            if (TreasureBoxType.None == boxType)
            {
                return;
            }
            closeBtnInteractable();
            ActivityDataStore.playClickAudio();
            observeClick.OnNext(this);
        }

        public void openBtnInteractable()
        {
            Debug.Log("openBtnInteractable");
            boxBtn.interactable = true;
        }

        public void closeBtnInteractable()
        {
            Debug.Log("closeBtnInteractable");
            boxBtn.interactable = false;
        }

        TreasureBoxType convertBoxType(string boxType)
        {
            if (string.IsNullOrEmpty(boxType))
            {
                return TreasureBoxType.None;
            }

            TreasureBoxType result;
            if (UtilServices.enumParse(boxType, out result))
            {
                return result;
            }

            return TreasureBoxType.None;
        }
    }
}
