using Common.Jigsaw;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using CommonPresenter;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Lobby.Audio;
using LobbyLogic.Audio;
using Services;
using System;
using UniRx;
using Lobby.UI;
using Binding;
using Notice;
using Lobby.Common;

namespace Lobby.Jigsaw
{
    public static class Museum
    {
        public static async void openMuseum()
        {
            //規格，如果有拼圖兌換券，開啟兌換券的介面
            var vouchers = await JigsawDataHelper.getAvaliableVouchers();
            if (vouchers.Count > 0)
            {
                var wildSelector = UiManager.getPresenter<WildSelector>();
                var voucher = vouchers[0];   //一次顯示一張
                wildSelector.setVoucher(voucher);
                return;
            }
            else
            {
                UiManager.getPresenter<MuseumPresenter>().open();
            }
        }
    }
    /// <summary>
    /// 博物館拼圖冊(主頁)
    /// </summary>
    class MuseumPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/puzzle_main";
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        const string albumObjPath = "prefab/lobby_puzzle/puzzle_book";

        GameObject albumObjTemplate = null;

        Button infoButton;
        Button closeButton;
        Button fantasyWheelButton;

        Text totalRewardText;
        Text progressText;

        GameObject allCompleteObj;
        GameObject puzzleCompleteObj;

        RectTransform rewardGroupRect;
        RectTransform albumRoot;

        RectTransform albumTempRoot;

        GameObject titleEn;
        GameObject titleZh;
        GameObject timerObjOpen;
        Image timerOffImg;
        Text timerTxt;
        CanvasGroup albumCanvasGroup;
        //Album unfoldAlbumPresenter;
        List<string> albumIds = new List<string>();
        string currentAlbumId;

        List<JigsawAlbumData> allInTimeAlbumDetail;
        Dictionary<string, AlbumFold> allAlbumFold;

        ScrollRect albumScrollRoot;
        private BindingNode noticeNode;

        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            infoButton = getBtnData("infoButton");
            closeButton = getBtnData("closeButton");
            fantasyWheelButton = getBtnData("fantasyWheelButton");

            totalRewardText = getTextData("totalRewardText");
            progressText = getTextData("progressText");

            rewardGroupRect = getBindingData<RectTransform>("rewardGroupRect");
            albumRoot = getBindingData<RectTransform>("albumRoot");

            allCompleteObj = getGameObjectData("allCompleteObj");
            puzzleCompleteObj = getGameObjectData("puzzle_complete_img");

            titleEn = getGameObjectData("title_en");
            titleZh = getGameObjectData("title_zh");
            timerObjOpen = getGameObjectData("fantasyWheel_open");
            timerOffImg = getImageData("fantasyWheel_off_img");
            timerTxt = getTextData("fantasyWheeloff_time");

            albumTempRoot = getRectData("temp_group_rect");
            albumCanvasGroup = getBindingData<CanvasGroup>("album_canvas_group");
            albumScrollRoot = uiRectTransform.GetComponentInChildren<ScrollRect>();
            albumScrollRoot.enabled = false;

            noticeNode = getNodeData("notice_node");
        }

        public override void init()
        {
            base.init();
            closeButton.onClick.AddListener(closeBtnClick);
            infoButton.onClick.AddListener(showTips);
            fantasyWheelButton.onClick.AddListener(onFantasyWheelClick);
            JigsawDataHelper.recycleTimeSub.Subscribe(checkRecyleTime).AddTo(uiGameObject);
            albumObjTemplate = ResourceManager.instance.getGameObjectWithResOrder(albumObjPath, resOrder);
            titleEn.setActiveWhenChange(ApplicationConfig.nowLanguage == ApplicationConfig.Language.EN);
            titleZh.setActiveWhenChange(ApplicationConfig.nowLanguage == ApplicationConfig.Language.ZH);
            totalRewardText.text = string.Empty;
            progressText.text = string.Empty;
            changeOffImg();
            UiManager.bindNode<NoticePresenter>(noticeNode.cachedGameObject).setSubject(NoticeManager.instance.puzzleNoticeEvent);
            NoticeManager.instance.getPuzzleStarAmount(false);
        }

        void changeOffImg()
        {
            var languageSprite = ResourceManager.instance.loadAll(UtilServices.getLocalizationAltasPath("puzzle_ui"));
            timerOffImg.sprite = Array.Find(languageSprite, sprite => sprite.name.Equals("tex_wheel_entry"));
        }

        void openAllCompleteObj(bool isAllComplete)
        {
            allCompleteObj.setActiveWhenChange(isAllComplete);
            puzzleCompleteObj.setActiveWhenChange(!isAllComplete);
            rewardGroupRect.gameObject.setActiveWhenChange(!isAllComplete);
        }

        /// <summary>
        /// 調整版面
        /// </summary>
        void rebuildLayout()
        {
            //迫使總獎勵的金幣&文字重新排版對齊
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardGroupRect);
        }
        TimerService recycleTimeServices;
        TimeStruct openTimeStruct;
        async Task syncRecycleTime()
        {
            var recycleTime = await JigsawDataHelper.getJigsawRecycleTime();
            checkRecyleTime(recycleTime);
        }

        void checkRecyleTime(string recycleTimeStr)
        {
            DateTime recycleTime = UtilServices.strConvertToDateTime(recycleTimeStr, DateTime.MinValue);
            CompareTimeResult compareResult = UtilServices.compareTimeWithNow(recycleTime);
            timerOffImg.gameObject.setActiveWhenChange(compareResult == CompareTimeResult.Later);
            timerObjOpen.setActiveWhenChange(compareResult == CompareTimeResult.Earlier);
            fantasyWheelButton.interactable = compareResult == CompareTimeResult.Earlier;
            if (compareResult == CompareTimeResult.Later)
            {
                recycleTimeServices = new TimerService();
                recycleTimeServices.setAddToGo(uiGameObject);
                recycleTimeServices.StartTimer(recycleTime, updateRecycleTimeTxt);
            }
        }

        void updateRecycleTimeTxt(TimeSpan recycleTime)
        {
            if (recycleTime <= TimeSpan.Zero)
            {
                recycleTimeServices.ExecuteTimer();
                timerObjOpen.setActiveWhenChange(true);
                timerOffImg.gameObject.setActiveWhenChange(false);
                fantasyWheelButton.interactable = true;
                return;
            }
            if (null != openTimeStruct)
            {
                openTimeStruct = UtilServices.updateTimeStruct(recycleTime, openTimeStruct);
            }
            else
            {
                openTimeStruct = UtilServices.toTimeStruct(recycleTime);
            }
            timerTxt.text = openTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
        }
        List<float> albumsOriginPosX = new List<float>();

        async Task syncDataAndShow()
        {
            albumCanvasGroup.alpha = 0;
            allAlbumFold = new Dictionary<string, AlbumFold>();
            var progressList = await JigsawDataHelper.getCurrentSeasonAllAlbumProgress();
            int totalCollected = 0;
            for (int i = 0; i < progressList.Count; i++)
            {
                var progress = progressList[i];
                createFoldAlbum(progress.albumId, progress.numCollected);
                totalCollected += progress.numCollected;
            }
            var totalReward = await JigsawDataHelper.getCurrentSeasonTotalReward();
            totalRewardText.text = totalReward.ToString("N0");
            rebuildLayout();
            allInTimeAlbumDetail = await JigsawDataHelper.getInTimeAllAlbumDetail();
            bool isAllComplete = true;
            var allAlbumFoldEnum = allAlbumFold.GetEnumerator();
            while (allAlbumFoldEnum.MoveNext())
            {
                allAlbumFoldEnum.Current.Value.setIsOpen(false);
                if (isAllComplete)
                {
                    isAllComplete = allAlbumFoldEnum.Current.Value.isComplete;
                }
            }

            for (int i = 0; i < allInTimeAlbumDetail.Count; i++)
            {
                string openAlbumID = allInTimeAlbumDetail[i].albumId;
                albumIds.Add(openAlbumID);    //作為之後看詳情使用
                AlbumFold albumFold;
                if (allAlbumFold.TryGetValue(openAlbumID, out albumFold))
                {
                    albumFold.setIsOpen(isOpen: true);
                }
            }

            openAllCompleteObj(isAllComplete);
            setProgress(totalCollected, JigsawDataHelper.getCurrentSeasonTargetPieces());
        }

        public override async void open()
        {
            BindingLoadingPage.instance.open();
            AudioManager.instance.playBGM(AudioPathProvider.getAudioPath(AlbumAudio.BGM));
            await syncRecycleTime();
            base.open();
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            getUiAnimator().enabled = false;
            await syncDataAndShow();
            readyMoveAlbumChild();
            BindingLoadingPage.instance.close();
            startMoveAlbumChild();
        }

        void readyMoveAlbumChild()
        {
            albumsOriginPosX.Clear();
            var albumsEnum = allAlbumFold.GetEnumerator();
            while (albumsEnum.MoveNext())
            {
                var albumRect = albumsEnum.Current.Value.uiRectTransform;
                albumRect.SetParent(albumTempRoot);
                var hidePos = albumRect.anchoredPosition;
                albumsOriginPosX.Add(hidePos.x);
                hidePos.Set(1800, hidePos.y);
                albumRect.anchoredPosition = hidePos;
            }
        }
        IDisposable moveAlbumDis = null;
        List<string> childMoveTween = new List<string>();
        void startMoveAlbumChild()
        {
            childMoveTween.Clear();
            albumCanvasGroup.alpha = 1;
            getUiAnimator().enabled = true;
            moveAlbumDis = Observable.Timer(TimeSpan.FromSeconds(0.3f), TimeSpan.FromSeconds(0.1)).Subscribe(repeatCount =>
            {
                var childID = (int)repeatCount;
                var child = albumTempRoot.transform.GetChild(childID).GetComponent<RectTransform>();
                childMoveTween.Add(child.anchPosMoveX(albumsOriginPosX[childID], 0.6f, easeType: DG.Tweening.Ease.OutExpo, onComplete: () =>
                 {
                     if (null == moveAlbumDis)
                     {
                         var albums = allAlbumFold.GetEnumerator();
                         while (albums.MoveNext())
                         {
                             albums.Current.Value.uiRectTransform.SetParent(albumRoot);
                         }
                         albumScrollRoot.enabled = true;
                     }
                 }));

                if (childID >= albumTempRoot.childCount - 1)
                {
                    moveAlbumDis.Dispose();
                    moveAlbumDis = null;
                    childMoveTween.Clear();
                }
            });
        }

        void setProgress(int collectedCount, int totalCount)
        {
            progressText.text = $"{collectedCount}/{totalCount}";
        }

        void createFoldAlbum(string albumId, int collectedCount)
        {
            GameObject obj = GameObject.Instantiate(albumObjTemplate, albumRoot, false);
            AlbumFold album = UiManager.bindNode<AlbumFold>(obj);
            album.setId(albumId);

            Sprite coverSprite = JigsawCoverSpriteProvider.getAlbumCover(albumId);
            if (null != coverSprite)
            {
                album.setCoverSprite(coverSprite);
            }
            else
            {
                Debug.LogWarning($"Museum Album Sprite null, albumId: {albumId}");
            }

            album.setProgress(collectedCount, JigsawConfig.MAX_JIGSAW_COUNT);
            album.onClick = unfoldAlbum;
            allAlbumFold.Add(albumId, album);
        }

        public override void animOut()
        {
            AudioManager.instance.playBGM(AudioPathProvider.getAudioPath(LobbyMainAudio.Main_BGM));
            UtilServices.disposeSubscribes(moveAlbumDis);
            for (int i = 0; i < childMoveTween.Count; ++i)
            {
                TweenManager.tweenKill(childMoveTween[i]);
            }
            clear();
        }

        void showTips()
        {
            UiManager.getPresenter<Hint>();
        }

        List<JigsawPieceData> getAlbumPieces(string albumId)
        {
            List<JigsawPieceData> pieces = new List<JigsawPieceData>();
            for (int i = 0; i < allInTimeAlbumDetail.Count; i++)
            {
                var data = allInTimeAlbumDetail[i];
                if (data.albumId.Equals(albumId))
                {
                    pieces = data.pieces;
                    break;
                }
            }
            return pieces;
        }

        /// <summary>
        /// 打開圖冊
        /// </summary>
        /// <param name="albumId"></param>
        void unfoldAlbum(string albumId)
        {
            List<JigsawPieceData> pieces = getAlbumPieces(albumId);
            if (null != pieces)
            {
                long completeReward = JigsawDataHelper.getCurrentSeasonAlbumReward(albumId);
                var album = UiManager.getPresenter<Album>();
                album.onNextButtonHandler = nextAlbum;
                album.onPreviousButtonHandler = previousAlbum;
                album.setData(albumId, pieces, completeReward);

                album.enableSwitchAlbum(albumIds.Count > 1);
                currentAlbumId = albumId;
                album.open();
            }
            else
            {
                Debug.LogWarning($"unfoldAlbum failed, albumId: {albumId}");
            }
        }

        void nextAlbum()
        {
            var idx = albumIds.IndexOf(currentAlbumId);
            //不做循環撥放
            if (idx == albumIds.Count - 1)
            {
                return;
            }
            idx = (idx + 1) % albumIds.Count;   //Avoid overflow
            unfoldAlbum(albumIds[idx]);
        }

        void previousAlbum()
        {
            var idx = albumIds.IndexOf(currentAlbumId);
            //不做循環撥放
            if (0 == idx)
            {
                return;
            }
            idx = (idx - 1 + albumIds.Count) % albumIds.Count;  //Avoid overflow
            unfoldAlbum(albumIds[idx]);
        }

        void onFantasyWheelClick()
        {
            UiManager.getPresenter<FantasyWheelSelector>().open();
        }
    }
}
