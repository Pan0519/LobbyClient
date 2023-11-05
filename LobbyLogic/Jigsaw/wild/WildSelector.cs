using CommonILRuntime.BindingModule;
using CommonILRuntime.Extension;
using CommonILRuntime.Module;
using CommonPresenter;
using LobbyLogic.NetWork.ResponseStruct;
using Services;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Outcome;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonService;

namespace Lobby.Jigsaw
{
    public static class WildPack
    {
        public static void openWildPack(CommonReward rewards, Action finishCallback = null)
        {
            var albumVoucher = toAlbumVoucher(rewards);
            if (null != albumVoucher)
            {
                UiManager.getPresenter<WildSelector>().setVoucher(albumVoucher, finishCallback);
                return;
            }
            Debug.LogError("openWildPack voucherReward parseError");
        }

        static AlbumVoucher toAlbumVoucher(CommonReward reward)
        {
            try
            {
                var outcome = reward.outcome;
                var album = outcome.album;
                var vouchers = album["vouchers"];
                var voucherData = vouchers[0];  //只會回傳一個 wild
                AlbumVoucher voucher = new AlbumVoucher();
                voucher.id = (string)voucherData["id"];
                voucher.type = (string)voucherData["type"];

                string expiry = (string)voucherData["expiredAt"];
                voucher.expiry = UtilServices.strConvertToDateTime(expiry, DateTime.UtcNow);

                return voucher;

            }
            catch (Exception e)
            {
                Debug.Log($"toAlbumVoucher {e} Exception caught.");
                //如果 parse失敗，回傳 null
                return null;
            }
        }
    }


    /// <summary>
    /// Wild拼圖選擇主頁
    /// </summary>
    public class WildSelector : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/puzzle_wild_selector";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        const string wildAlbumObjPath = "prefab/lobby_puzzle/wildSelector/ws_album_unit";
        GameObject wildAlbumTemplateObj;

        const string wildConfirmObjPath = "prefab/lobby_puzzle/puzzle_double_check";
        GameObject wildConfirmObj = null;

        Image packImage;

        Text expireTitleText;
        Text expireTimeText;

        Button closeButton;
        Button collectButton;

        Toggle filterToggle;
        RectTransform wildAlbumRoot;

        TimerService timerService = new TimerService();

        List<JigsawAlbumData> cachedAlbumsDetail = null;
        List<WildAlbumUnit> uiAlbums = new List<WildAlbumUnit>();

        Piece selectedPiece = null;

        AlbumVoucher voucher = null;
        RareLevel rareLevel = RareLevel.GREEN;
        Action pieceFinishCallback = null;

        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle)};
            base.initContainerPresenter();
        }

        public override void initUIs()
        {
            packImage = getImageData("packImage");
            expireTitleText = getTextData("expireTitleText");
            expireTimeText = getTextData("expireTimeText");
            closeButton = getBtnData("closeButton");
            collectButton = getBtnData("collectButton");

            filterToggle = getBindingData<Toggle>("filterToggle");
            wildAlbumRoot = getBindingData<RectTransform>("wildAlbumRoot");
        }

        public override void init()
        {
            base.init();
            closeButton.onClick.AddListener(closeClick);
            collectButton.onClick.AddListener(onCollectClick);
            collectButton.interactable = false;

            filterToggle.onValueChanged.AddListener(onIgnoreCollected);

            wildAlbumTemplateObj = ResourceManager.instance.getGameObjectWithResOrder(wildAlbumObjPath,resOrder);
            selectedPiece = null;

            expireTitleText.text = LanguageService.instance.getLanguageValue("PuzzleCrush_Wild_Time");
            expireTimeText.text = "";
        }

        void closeClick()
        {
            JigsawReward.isJigsawShowFinish.OnNext(true);
            closeBtnClick();
        }

        public void setVoucher(AlbumVoucher voucher, Action pieceCallback = null)
        {
            if (null == voucher)
            {
                clear();
                return;
            }
            this.pieceFinishCallback = pieceCallback;
            this.voucher = voucher;
            setRareType(voucher.type);
            timerService.StartTimer(voucher.expiry.ToUniversalTime(), updateRemainTime);
            fetchDataAndShow();
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(AlbumAudio.WinCard));
        }

        void setRareType(string type)
        {
            RareLevel rareLevel = RareLevel.GREEN;
            switch (type)
            {
                case "rarity-1":
                    {
                        rareLevel = RareLevel.BLUE;
                    }
                    break;
                case "rarity-2":
                    {
                        rareLevel = RareLevel.YELLOW;
                    }
                    break;
                case "rarity-all":
                    {
                        rareLevel = RareLevel.MAX;
                    }
                    break;
            }
            setRareLevel(rareLevel);
        }

        void setRareLevel(RareLevel level)
        {
            rareLevel = level;
            var provider = new WildPackSpriteProvider();
            var sprite = provider.getPackSprite(level);
            if (null != sprite)
            {
                packImage.sprite = sprite;
            }
        }

        public override void animOut()
        {
            timerService.disposable.Dispose();
            clear();
        }

        async void fetchDataAndShow()
        {
            filterToggle.enabled = false;
            cachedAlbumsDetail = await JigsawDataHelper.getInTimeAllAlbumDetail();
            changeContent(cachedAlbumsDetail);
            filterToggle.enabled = true;
            filterToggle.isOn = false;
        }

        void updateRemainTime(TimeSpan remainTime)
        {
            expireTimeText.text = UtilServices.formatCountTimeSpan(remainTime);
            if (remainTime.TotalSeconds <= 1)
            {
                closePresenter();
            }
        }

        void changeContent(List<JigsawAlbumData> content)
        {
            Extention.cleanRoot(wildAlbumRoot);

            uiAlbums.Clear();
            for (int i = 0; i < content.Count; i++)
            {
                var albumDetail = content[i];
                var uiAlbum = createWildAlbum(albumDetail);
                uiAlbums.Add(uiAlbum);
            }

            updateLayout();
        }

        void updateLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(wildAlbumRoot); //GridRoot
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)wildAlbumRoot.parent);   //RectTransform
        }

        WildAlbumUnit createWildAlbum(JigsawAlbumData data)
        {
            var obj = GameObject.Instantiate(wildAlbumTemplateObj);
            WildAlbumUnit albumUnit = UiManager.bindNode<WildAlbumUnit>(obj);
            List<JigsawPieceData> selectablePieces;
            if (RareLevel.MAX == rareLevel)
            {
                selectablePieces = data.pieces;
            }
            else
            {
                selectablePieces = data.pieces.FindAll(albumData =>
                {
                    return albumData.getRareLevel() == (int)rareLevel;
                });
            }

            var collectedPieces = data.pieces.FindAll(pieceData => { return pieceData.collectted; });
            albumUnit.setData(data.albumId, collectedPieces.Count, selectablePieces, onPieceSelected);
            obj.transform.SetParent(wildAlbumRoot, false);
            return albumUnit;
        }

        void onCollectClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            if (null != selectedPiece)
            {
                var data = new JigsawPieceData(selectedPiece.data.ID, 1);
                showWildConfirm(data);
            }
            else
            {
                Debug.LogWarning("selectedPiece == null");
            }
        }

        void showWildConfirm(JigsawPieceData data)
        {
            if (null == wildConfirmObj)
            {
                wildConfirmObj = ResourceManager.instance.getGameObjectWithResOrder(wildConfirmObjPath,resOrder);
            }

            var obj = GameObject.Instantiate(wildConfirmObj);
            obj.transform.SetParent(uiTransform, false);
            WildConfirm confirmPresenter = UiManager.bindNode<WildConfirm>(obj);
            confirmPresenter.setData(data, voucher.id, onDoubleConfirmed, pieceFinishCallback);
        }

        void onIgnoreCollected(bool ignore)
        {
            onPieceSelected(null);  //清空目前選擇的piece
            for (int i = 0; i < uiAlbums.Count; i++)
            {
                var uiAlbum = uiAlbums[i];
                uiAlbum.ignoreCollected(ignore);
            }
            updateLayout();
        }

        void onPieceSelected(Piece piece)
        {
            if (null != selectedPiece)
            {
                selectedPiece.setSelected(false);
            }

            if (null != piece)  //多此檢查，當input piece == null 時可以重設 selectedPiece
            {
                AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
                if (null != selectedPiece && selectedPiece.data.ID.Equals(piece.data.ID))    //選到自己，取消
                {
                    onPieceSelected(null);
                    return;
                }
                else
                {
                    piece.setSelected(true);
                }
            }
            selectedPiece = piece;

            collectButton.interactable = null != selectedPiece;
        }

        void onDoubleConfirmed(bool confirmed)
        {
            if (confirmed)
            {
                closePresenter();
            }
        }
    }

    class WildPackSpriteProvider
    {
        Dictionary<string, Sprite> sprites = null;

        public Sprite getPackSprite(RareLevel level)
        {
            string subName = "green";
            switch (level)
            {
                case RareLevel.GREEN:
                    {
                        subName = "green";
                    }
                    break;
                case RareLevel.BLUE:
                    {
                        subName = "blue";
                    }
                    break;
                case RareLevel.YELLOW:
                    {
                        subName = "gold";
                    }
                    break;
                case RareLevel.MAX:
                    {
                        subName = "color";
                    }
                    break;
            }

            var spriteName = $"puzzle_pack_{subName}_wild";
            Sprite outSprite = null;
            getSprites().TryGetValue(spriteName, out outSprite);
            return outSprite;
        }

        Dictionary<string, Sprite> getSprites()
        {
            if (null == sprites)
            {
                var iconSprites = ResourceManager.instance.loadAllWithResOrder("texture/board_common/pic/puzzle_pack_wild",AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
                sprites = UtilServices.spritesToDictionary(iconSprites);
            }
            return sprites;
        }
    }
}
