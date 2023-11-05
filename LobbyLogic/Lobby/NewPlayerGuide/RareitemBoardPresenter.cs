using CommonPresenter;
using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using LobbyLogic.NetWork.ResponseStruct;
using Service;
using EventActivity;

namespace NewPlayerGuide
{
    class RareitemBoardPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/quest_mission/quest_rareitem_board";
        public override UiLayer uiLayer { get => UiLayer.System; }
        Animator statusAnim;
        Text ticketCount;
        Button openActivityBtn;
        public override void initUIs()
        {
            statusAnim = getAnimatorData("status_anim");
            ticketCount = getTextData("ticket_amount_txt");
            openActivityBtn = getBtnData("open_activty_btn");
        }

        public override void init()
        {
            openActivityBtn.onClick.AddListener(openActivityClick);
        }

        public override async void open()
        {
            GetActivityResponse actRes = await AppManager.lobbyServer.getActivity();
            ActivityDataStore.nowActivityInfo = actRes.activity;
            var getItemResponse = await AppManager.lobbyServer.getBagItem(ActivityDataStore.getNowActivityTicketID());
            ticketCount.text = getItemResponse.amount.ToString();
            base.open();
        }

        void openActivityClick()
        {
            statusAnim.SetTrigger("out");
            Observable.TimerFrame(40).Subscribe(_ =>
            {
                ActivityPageData.instance.openActivityPage(ActivityDataStore.getNowActivityID());
            }).AddTo(uiGameObject);

            Observable.TimerFrame(50).Subscribe(_ =>
            {
                clear();
            }).AddTo(uiGameObject);
        }
    }
}
