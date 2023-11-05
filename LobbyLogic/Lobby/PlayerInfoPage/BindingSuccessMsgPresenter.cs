using CommonILRuntime.Module;
using CommonILRuntime.Services;
using CommonService;
using UnityEngine;
using UnityEngine.UI;
using System;
using Service;
using UniRx;
using UniRx.Triggers;
using LobbyLogic.Common;

namespace Lobby.PlayerInfoPage
{
    class BindingSuccessMsgPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/lobby/page_bind_success";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        Button bindSuccessMsgConfirmBtn;
        Text bindSuccessMsgCoin;
        Animator successMsgAnim;
        ulong bindingRewardCoin;
        public override void initUIs()
        {
            bindSuccessMsgConfirmBtn = getBtnData("bind_success_confirm");
            bindSuccessMsgCoin = getTextData("bind_success_coin");
            successMsgAnim = getAnimatorData("bind_success_anim");
        }

        public override void init()
        {
            bindSuccessMsgConfirmBtn.onClick.AddListener(bindingConfirmClick);
        }

        public void openPage(ulong bindingRewardCoin)
        {
            this.bindingRewardCoin = bindingRewardCoin;
            bindSuccessMsgCoin.text = bindingRewardCoin.ToString("N0");
        }

        void bindingConfirmClick()
        {
            var playCoin = DataStore.getInstance.playerInfo.myWallet.coin;
            CoinFlyHelper.frontSFly(bindSuccessMsgConfirmBtn.GetComponent<RectTransform>(), playCoin, playCoin + bindingRewardCoin, onComplete: playSuccessOut);
        }
        void playSuccessOut()
        {
            updatePlayerInfo();
            var animTrigger = successMsgAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(successAniOut);
            successMsgAnim.SetTrigger("success_out");
        }

        void successAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animTimerDis.Dispose();
                clear();
            });
        }

        async void updatePlayerInfo()
        {
            var playerInfoResponse = await AppManager.lobbyServer.getPlayerInfo();
            LobbyPlayerInfo.setPlayerInfo(playerInfoResponse);
        }
    }
}
