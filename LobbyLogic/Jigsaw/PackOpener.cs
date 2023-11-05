using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using System;
using System.Collections.Generic;
using Common.Jigsaw;
using UniRx;
using UniRx.Triggers;
using CommonService;
using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Outcome;
using Lobby.Audio;
using LobbyLogic.Audio;
using Services;

namespace Lobby.Jigsaw
{
    public static class JigsawPack
    {
        public static void OpenPackRewards(List<CommonReward> rewards, Action onFinishCallback = null)
        {
            var packRewards = rewards.FindAll(reward => reward.kind.Equals("puzzle-pack"));
            var scheduler = new OpenPackScheduler(packRewards, onFinishCallback);
            scheduler.start();
        }
    }

    public class OpenPackScheduler
    {
        Queue<CommonReward> packRewards;
        Action onAllFinish = null;

        public OpenPackScheduler(List<CommonReward> packRewards, Action onAllFinish = null)
        {
            this.packRewards = new Queue<CommonReward>(packRewards);
            this.onAllFinish = onAllFinish;
        }

        public void start()
        {
            doNextPack();
        }

        void doNextPack()
        {
            if (packRewards.Count > 0)
            {
                //retrive pack pieces from outcome
                var reward = packRewards.Dequeue();
                var packId = reward.type;

                var album = reward.outcome.album;
                var items = album["items"];
                var pieces = new List<JigsawPieceData>();
                for (int itemIdx = 0; itemIdx < items.Length; itemIdx++)
                {
                    var item = items[itemIdx];
                    var pieceId = (string)item["id"];
                    var amount = (int)item["amount"];
                    var piece = new JigsawPieceData(pieceId, amount);
                    pieces.Add(piece);
                }

                var opener = UiManager.getPresenter<PackOpener>();
                opener.openPack(long.Parse(packId), pieces, doNextPack);

            }
            else
            {
                Mission.MissionData.updateProgress();
                onAllFinish?.Invoke();
            }
        }
    }

    public class PackOpener : ContainerPresenter
    {
        public override string objPath => "prefab/lobby_puzzle/puzzle_open_pack";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        Action onFinishListener = null;

        const string OPEN_PACK_TRIGGER = "open";
        const string CLOSE_UI_TRIGGER = "out";

        Animator uiAnimator;
        Button openPackButton;
        Image packUpImg;
        Image packUpBackImg;
        Image packDownImg;
        Button closeBtn;
        GameObject dummyRootGameObject;
        GameObject titleEnObj;
        GameObject titleZhObj;
        Image flyPackImg;
        List<Transform> dummyList = null;
        List<IDisposable> disposables = new List<IDisposable>();

        List<JigsawPieceData> resultPiecesData = null;
        List<Piece> resultPieceItems = new List<Piece>();
        int puzzleCount = 1;
        const int puzzleMaxCount = 5;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            uiAnimator = getAnimatorData("uiAnimator");
            openPackButton = getBtnData("openPackButton");
            packUpImg = getImageData("pack_up_img");
            packUpBackImg = getImageData("pack_up_back_img");
            packDownImg = getImageData("pack_down_img");
            closeBtn = getBtnData("btn_Close");
            titleEnObj = getGameObjectData("title_en_obj");
            titleZhObj = getGameObjectData("title_zh_obj");
            flyPackImg = getImageData("pack_fly_img");

            dummyRootGameObject = getGameObjectData("dummyRootGameObject");

            dummyList = new List<Transform>();
            int dummyIdx = 0;
            while (true)
            {
                var dummyName = $"puzzle_piece_dummy_{dummyIdx}";
                var dymmyTransform = dummyRootGameObject.transform.Find(dummyName);
                if (null == dymmyTransform)
                {
                    break;
                }
                dummyList.Add(dymmyTransform);
                dummyIdx++;
            }
        }

        public override void init()
        {
            base.init();
            setRootScale();
            for (int i = 1; i <= puzzleMaxCount; ++i)
            {
                uiAnimator.ResetTrigger($"{OPEN_PACK_TRIGGER}_{i}");    //開卡包
            }

            uiAnimator.ResetTrigger(CLOSE_UI_TRIGGER);    //關閉視窗
            closeBtn.onClick.AddListener(onPackClose);
            openPackButton.onClick.AddListener(onOpenPack);
            titleEnObj.setActiveWhenChange(ApplicationConfig.nowLanguage == ApplicationConfig.Language.EN);
            titleZhObj.setActiveWhenChange(ApplicationConfig.nowLanguage == ApplicationConfig.Language.ZH);

            var animTriggers = uiAnimator.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animTriggers.Length; i++)
            {
                var trigger = animTriggers[i];
                var disposable = trigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onEnterState);
                disposables.Add(disposable);
            }
        }

        void setRootScale()
        {
            float scale = 1.0f;
            switch (UtilServices.getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    scale = 0.65f;
                    break;
            }
            var scaleRoot = getRectData("scale_root_rect");
            var originalScale = scaleRoot.localScale;
            originalScale.Set(scale, scale, scale);
            scaleRoot.localScale = originalScale;
        }

        void onEnterState(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animTimerDis.Dispose();
                onClose();
            });
        }

        public void openPackOpener(long packID)
        {
            setPackImage(packID);
            open();
        }

        public void openPack(long packID, List<JigsawPieceData> pieces, Action onFinish = null)
        {
            setPackImage(packID);
            resultPiecesData = pieces;
            onFinishListener = onFinish;
            open();
        }

        void setPackImage(long packID)
        {
            PuzzlePackID puzzlePackID = (PuzzlePackID)packID;
            OpenPackSprites packSprites = JigsawPackSpriteProvider.getOpenPackSprites(puzzlePackID);
            packUpImg.sprite = packSprites.upSprite;
            packUpBackImg.sprite = packSprites.upSprite;
            packDownImg.sprite = packSprites.downSprite;
            flyPackImg.sprite = JigsawPackSpriteProvider.getPackSprite(puzzlePackID);
        }

        public override void open()
        {
            closeBtn.interactable = true;
            openPackButton.interactable = true;
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(AlbumAudio.WinCard));
            base.open();
        }

        void onOpenPack()
        {
            resultPieceItems.Clear();
            openPackButton.interactable = false;
            if (null != resultPiecesData)
            {
                puzzleCount = resultPiecesData.Count;
                for (int i = 0; i < resultPiecesData.Count; i++)
                {
                    var data = resultPiecesData[i];
                    var dummy = i <= dummyList.Count - 1 ? dummyList[i] : null;
                    if (null != dummy)
                    {
                        var piece = PieceFactory.createPiece(data, dummy, true);
                        if (data.getCount() == 1)
                        {
                            piece.isOpenNewObj(true);
                            PieceNewData.saveData(data.ID);
                        }
                        resultPieceItems.Add(piece);
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot find dummy: {i}");
                    }
                }
            }
            uiAnimator.SetTrigger($"{OPEN_PACK_TRIGGER}_{puzzleCount}");
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(AlbumAudio.WinCardOpen));
        }

        void onPackClose()
        {
            closeBtn.interactable = false;
            for (int i = 0; i < resultPieceItems.Count; ++i)
            {
                resultPieceItems[i].playCardRareAnim();
            }
            uiAnimator.SetTrigger(CLOSE_UI_TRIGGER);
        }

        void onClose()
        {
            UtilServices.disposeSubscribes(disposables);
            clear();
            onFinishListener?.Invoke();
        }
    }
}
