using CommonILRuntime.Module;
using LobbyLogic.NetWork.RequestStruce;
using Service;
using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;
using CommonService;
using LobbyLogic.Audio;
using Debug = UnityLogUtility.Debug;

namespace Lobby.Jigsaw
{
    public class WildConfirm : NodePresenter
    {
        Button confirmButton;
        Button cancelButton;

        RectTransform pieceRootTrans;

        Action<bool> confirmCallback = null;
        Action getPieceFinishCallback = null;

        Animator closeAnim = null;
        IDisposable animTriggerDis;

        string voucherId = null;
        JigsawPieceData pieceData = null;

        public override void initUIs()
        {
            confirmButton = getBtnData("confirmButton");
            cancelButton = getBtnData("cancelButton");

            pieceRootTrans = getBindingData<RectTransform>("pieceRootTrans");
        }

        public override void init()
        {
            base.init();
            initAnimator();
            confirmButton.onClick.AddListener(onConfirmClick);
            cancelButton.onClick.AddListener(onCancelClick);
        }

        void initAnimator()
        {
            closeAnim = uiGameObject.GetComponent<Animator>();

            if (null == closeAnim)
            {
                return;
            }
            closeAnim.ResetTrigger("out");
            var animTriggers = closeAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut);
        }

        void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animTriggerDis.Dispose();
                animTimerDis.Dispose();
                animOut();
            });
        }
        public void setData(JigsawPieceData data, string voucherId, Action<bool> confirmCallback, Action getPieceFinishCallback)
        {
            this.voucherId = voucherId;
            this.confirmCallback = confirmCallback;
            this.getPieceFinishCallback = getPieceFinishCallback;
            pieceData = data;
            var p = PieceFactory.createPiece(data, pieceRootTrans, true);
        }

        public void animOut()
        {
            clear();
        }

        async void onConfirmClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            var response = await AppManager.lobbyServer.redeemAlbumVoucher(voucherId, new VoucherRedeemRequestData(pieceData.ID));

            if (Network.Result.OK == response.result)
            {
                var items = response.items;
                string[] ids = new string[response.items.Length];
                for (int i = 0; i < response.items.Length; i++)
                {
                    var itemInfo = items[i];
                    var pieceId = (string)itemInfo["id"];
                    ids[i] = pieceId;
                }

                closeConfirm(true);

                PieceGetter.getPieces(ids, getPieceFinishCallback);
                Mission.MissionData.updateProgress();
            }
            else
            {
                Debug.LogWarning("redeemAlbumVoucher failed");
                closeConfirm(false);
            }
        }

        void onCancelClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            closeConfirm(false);
        }

        void closeConfirm(bool confirm)
        {
            closeBtnClick();
            confirmCallback?.Invoke(confirm);
        }

        void closeBtnClick()
        {
            if (null == closeAnim)
            {
                Debug.LogWarning("closeAnim is Null");
                return;
            }

            closeAnim.SetTrigger("out");
        }
    }
}
