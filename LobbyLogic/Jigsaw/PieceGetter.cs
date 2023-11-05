using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;
using CommonService;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace Lobby.Jigsaw
{
    public static class PieceGetter
    {
        public static PieceGetterPresenter getPieces(string[] ids, Action showFinish = null)
        {
            List<JigsawPieceData> pieces = new List<JigsawPieceData>();
            var presenter = UiManager.getPresenter<PieceGetterPresenter>();
            for (int i = 0; i < ids.Length; i++)
            {
                var pieceId = ids[i];
                var data = new JigsawPieceData(pieceId, 1);
                pieces.Add(data);
            }
            presenter.show(pieces, showFinish);
            return presenter;
        }
    }
    /// <summary>
    /// 此介面有兩個 out state 故不適用 SystemUiBasePresenter
    /// </summary>
    public class PieceGetterPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/lobby_puzzle/puzzle_get_piece";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        const string OPEN_UI = "in";
        const string CLOSE_UI = "out";

        Animator uiAnimator;
        Button collectButton;
        RectTransform pieceRoot;
        RectTransform scaleGroup;

        List<JigsawPieceData> data;

        List<IDisposable> disposables = new List<IDisposable>();
        Action finishCallback = null;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            uiAnimator = getAnimatorData("uiAnimator");
            scaleGroup = getRectData("scaleRoot");
            collectButton = getBtnData("collectButton");
            pieceRoot = getBindingData<RectTransform>("pieceRoot");
        }

        public override void init()
        {
            uiAnimator.ResetTrigger(OPEN_UI);
            uiAnimator.ResetTrigger(CLOSE_UI);

            collectButton.onClick.AddListener(collectClick);

            var animTriggers = uiAnimator.GetBehaviours<ObservableStateMachineTrigger>();

            for (int i = 0; i < animTriggers.Length; i++)
            {
                var trigger = animTriggers[i];
                var disposable = trigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onEnterState).AddTo(uiGameObject);
                disposables.Add(disposable);
            }
        }

        async Task setOrientationScale()
        {
            var nowGameOrientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            float orientationScale = (GameOrientation.Landscape == nowGameOrientation) ? 1 : 0.7f;
            var scale = scaleGroup.localScale;
            scale.Set(orientationScale, orientationScale, orientationScale);
            scaleGroup.localScale = scale;
        }

        public async void show(List<JigsawPieceData> data, Action finishCallback)
        {
            await setOrientationScale();
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(AlbumAudio.WinCard));
            this.finishCallback = finishCallback;
            base.open();
            this.data = data;
            for (int i = 0; i < data.Count; i++)
            {
                var pieceData = data[i];
                PieceFactory.createPiece(pieceData, pieceRoot, true);
            }
            uiAnimator.SetTrigger(OPEN_UI);
        }

        void collectClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            collectButton.enabled = false;
            fadeoutUI();
        }

        public void fadeoutUI()
        {
            uiAnimator.SetTrigger(CLOSE_UI);
        }

        void onEnterState(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;

            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animTimerDis.Dispose();

                if (obj.StateInfo.IsName("puzzle_get_piece_out"))
                {
                    closeGetter();
                }
                else if (obj.StateInfo.IsName("puzzle_piece_duplicate_out"))
                {
                    closeGetter();
                }
            }).AddTo(uiGameObject);
        }

        void closeGetter()
        {
            if (null != finishCallback)
            {
                finishCallback();
            }
            for (int i = 0; i < disposables.Count; i++)
            {
                disposables[i].Dispose();
            }
            clear();
            JigsawReward.checkCollectionRewards();
        }
    }
}
