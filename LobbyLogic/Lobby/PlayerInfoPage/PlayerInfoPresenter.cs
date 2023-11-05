using UnityEngine.UI;
using UnityEngine;
using UniRx;
using Service;
using Services;
using CommonService;
using System;
using Common;
using System.Collections.Generic;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using Binding;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using EasyUI.Toast;
using Lobby.VIP;
using CommonPresenter;
using Common.VIP;
using HighRoller;
using Debug = UnityLogUtility.Debug;

namespace Lobby.PlayerInfoPage
{
    enum BindInfoType
    {
        FB,
        Mail,
        Phone,
    }
    class PlayerInfoPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby/page_player_info";
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        #region BindingField
        Image headImg;
        Button openEditHeadBtn;
        Button openVipDetailBtn;
        Text nameTxt;
        Text idTxt;
        CustomTextSizeChange lvTxt;
        CustomTextSizeChange moneyTxt;
        Button closeBtn;
        BindingNode fbBindNode;
        BindingNode mailBindNode;
        BindingNode phoneBindNode;
        //GameObject bindSuccessMsg;

        BindingNode mailBindPageNode;
        BindingNode phoneBindPageNode;

        CustomBtn openShopBtn;
        CustomBtn idCopyBtn;
        Image vipIconImage;
        Image vipNameImage;

        GameObject blackCardObj;
        List<GameObject> crownObjs = new List<GameObject>();
        Text diamondDaysTxt;
        GameObject diamondPointObj;
        Text diamondPointTxt;
        Button openDiamondBtn;
        LvTipNodePresenter tipNodePresenter;
        #endregion

        BindMailMsgPresenter mailPage;
        BindPhoneMsgPresenter phonePage;

        Dictionary<BindInfoType, BindInfoPresenter> bindBtnClickEvents = new Dictionary<BindInfoType, BindInfoPresenter>();
        PlayerInfo playerInfo { get { return DataStore.getInstance.playerInfo; } }
        public override void initUIs()
        {
            base.initUIs();
            headImg = getImageData("head_img");
            openEditHeadBtn = getBtnData("open_editpage_btn");
            openVipDetailBtn = getBtnData("open_vip_btn");
            nameTxt = getTextData("name_txt");
            idTxt = getTextData("id_txt");
            lvTxt = getBindingData<CustomTextSizeChange>("lv_txt");
            moneyTxt = getBindingData<CustomTextSizeChange>("money_txt");
            closeBtn = getBtnData("close_btn");
            fbBindNode = getNodeData("fb_bind_node");
            mailBindNode = getNodeData("mail_bind_node");
            phoneBindNode = getNodeData("phone_bind_node");

            mailBindPageNode = getNodeData("mail_bindpage_node");
            phoneBindPageNode = getNodeData("phone_bindpage_node");

            openShopBtn = getCustomBtnData("money_plus_btn");
            idCopyBtn = getCustomBtnData("id_copy_btn");

            blackCardObj = getGameObjectData("black_card");
            vipIconImage = getImageData("vipIcon");
            vipNameImage = getImageData("vipName");

            for (int i = 1; i <= 2; ++i)
            {
                crownObjs.Add(getGameObjectData($"crown_obj_{i}"));
            }
            diamondDaysTxt = getTextData("diamond_days_txt");
            diamondPointObj = getGameObjectData("diamond_club_point");
            diamondPointTxt = getTextData("diamond_club_point_txt");
            openDiamondBtn = getBtnData("open_diamond_btn");
            tipNodePresenter = UiManager.bindNode<LvTipNodePresenter>(getNodeData("unlock_tip_node").cachedGameObject);
        }

        public override void init()
        {
            base.init();
            mailBindPageNode.cachedGameObject.setActiveWhenChange(false);
            phoneBindPageNode.cachedGameObject.setActiveWhenChange(false);

            initPlayerInfo();
            closeBtn.onClick.AddListener(closeBtnClick);
            openEditHeadBtn.onClick.AddListener(openEditPage);
            openVipDetailBtn.onClick.AddListener(openVipPage);
            openDiamondBtn.onClick.AddListener(openDiamondClubPage);
            idCopyBtn.clickHandler = copyID;
            openShopBtn.clickHandler = openShopPage;

            checkBindState();
            setDiamondData();
        }
        TimerService diamondExpireTimeServices;
        void setDiamondData()
        {
            AccessInfo accessInfo = HighRollerDataManager.instance.accessInfo;
            int detailsLength = accessInfo.details.Length;
            for (int i = 0; i < crownObjs.Count; ++i)
            {
                crownObjs[i].setActiveWhenChange(i < detailsLength);
            }
            diamondPointObj.setActiveWhenChange(detailsLength <= 0);
            diamondDaysTxt.gameObject.setActiveWhenChange(detailsLength > 0);
            if (detailsLength > 0)
            {
                DateTime expireTime = UtilServices.strConvertToDateTime(accessInfo.expiredAt, DateTime.MaxValue);
                TimeStruct expireTimeStruct = UtilServices.toTimeStruct(expireTime.Subtract(UtilServices.nowTime));
                if (expireTimeStruct.days >= 1)
                {
                    diamondDaysTxt.text = expireTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
                }
                else
                {
                    diamondExpireTimeServices = new TimerService();
                    diamondExpireTimeServices.setAddToGo(uiGameObject);
                    diamondExpireTimeServices.StartTimer(expireTime, updateExpireTimer);
                }
                return;
            }
            diamondPointTxt.text = HighRollerDataManager.instance.userRecord.passPoints.ToString();
        }

        void updateExpireTimer(TimeSpan updateTime)
        {
            if (updateTime <= TimeSpan.Zero)
            {
                diamondExpireTimeServices.ExecuteTimer();
                return;
            }
            diamondDaysTxt.text = UtilServices.formatCountTimeSpan(updateTime);
        }

        void copyID()
        {
            var successMsg = LanguageService.instance.getLanguageValue("Role_copy");
            GUIUtility.systemCopyBuffer = playerInfo.userID;
            Toast.Show(successMsg, 1.5f);
        }

        void checkBindState()
        {
            BindInfoData fbData = new BindInfoData()
            {
                infoType = BindInfoType.FB,
                isTypeBind = playerInfo.isBindFB,
                bindingMsgKey = "FBBindingSuccess",
                unBindingMsgKey = "FBBindingMsg",
                bindClickEvent = openFBBindMsg,
                bindCoin = DataStore.getInstance.dataInfo.settings["facebook"]
            };
            bindBtnClickEvents.Add(BindInfoType.FB, UiManager.bindNode<BindInfoPresenter>(fbBindNode.cachedGameObject).init(fbData));

            BindInfoData mailData = new BindInfoData()
            {
                infoType = BindInfoType.Mail,
                isTypeBind = playerInfo.mailVerifiedState != MailVerifiedState.None,
                bindingMsgKey = "PhoneEmailBindingSuccess",
                unBindingMsgKey = "EmailBindingMsg",
                bindClickEvent = openBindMailMPage,
                bindCoin = DataStore.getInstance.dataInfo.settings["email"]
            };
            bindBtnClickEvents.Add(BindInfoType.Mail, UiManager.bindNode<MailBindingNode>(mailBindNode.cachedGameObject).init(mailData));

            BindInfoData phoneData = new BindInfoData()
            {
                infoType = BindInfoType.Phone,
                isTypeBind = playerInfo.isBindPhone,
                bindingMsgKey = "PhoneEmailBindingSuccess",
                unBindingMsgKey = "PhoneBindingMsg",
                bindClickEvent = openBindPhonePage,
                bindCoin = DataStore.getInstance.dataInfo.settings["phoneNumber"]
            };
            bindBtnClickEvents.Add(BindInfoType.Phone, UiManager.bindNode<PhoneBindingNode>(phoneBindNode.cachedGameObject).init(phoneData));
        }

        void changeBindBtnState(BindInfoType infoType)
        {
            BindInfoPresenter infoPresenter;
            if (bindBtnClickEvents.TryGetValue(infoType, out infoPresenter))
            {
                infoPresenter.openStateObj(isBind: true);
                infoPresenter.setBindState();
            }
        }
        IDisposable bindingFBDis;
        void openFBBindMsg()
        {
            if (playerInfo.isBindFB)
            {
                return;
            }

            bindingFBDis = FirebaseService.TokenSubject.Subscribe(sendLinkFBSuccess).AddTo(uiGameObject);
            FirebaseService.linkFB();
        }

        async void sendLinkFBSuccess(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }
            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = string.Empty,
                contentKey = "FBBindingError",
            }, Result.FBBindingRepeat);
            var response = await AppManager.lobbyServer.linkToFB();
            if (null == response.reward)
            {
                return;
            }
            UtilServices.disposeSubscribes(bindingFBDis);
            bindSuccessProcess(BindInfoType.FB, response);
        }

        void openBindMailMPage()
        {
            if (mailPage == null)
            {
                mailPage = UiManager.bindNode<BindMailMsgPresenter>(mailBindPageNode.cachedGameObject);
            }

            mailPage.open();
            mailPage.MailObserve.Subscribe(ReceiveInputEmail);
            mailPage.BindingMailDataObserve.Subscribe(ReceiveBindingEmailData);
        }

        void openBindPhonePage()
        {
            if (phonePage == null)
            {
                phonePage = UiManager.bindNode<BindPhoneMsgPresenter>(phoneBindPageNode.cachedGameObject);
            }

            phonePage.open();
            phonePage.BindingPhoneDataObs.Subscribe(ReceiveBindingResult);
            phonePage.PhoneInputObs.Subscribe(ReceiveInputPhone);
        }

        private void bindSuccessProcess(BindInfoType bindType, BindingResponse response)
        {
            if (response.reward != null)
            {
                UiManager.getPresenter<BindingSuccessMsgPresenter>().openPage(response.reward.coin);
            }

            if (bindType == BindInfoType.FB)
            {
                playerInfo.setIsBindingFB(true);
            }
            changeBindBtnState(bindType);
        }

        private async void ReceiveInputPhone(string phoneNumber)
        {
            await AppManager.lobbyServer.askVerifyCode(phoneNumber);
        }

        private async void ReceiveBindingResult(BindingPhoneData data)
        {
            BindingResponse result = null;

            result = await AppManager.lobbyServer.bindingPhoneNumber(data.PhoneNumber, data.VerifyCode);

            if (result.result != Result.OK)
            {
                return;
            }
            playerInfo.UpdateBindingPhoneInfo(data.PhoneNumber);
            bindSuccessProcess(BindInfoType.Phone, result);
            phonePage.close();
        }

        private async void ReceiveInputEmail(string mail)
        {
            await AppManager.lobbyServer.askEmailVerifyCode(mail);
        }

        private async void ReceiveBindingEmailData(BindingMailData data)
        {
            BindingResponse result = null;

            result = await AppManager.lobbyServer.bindingEmail(data.Email, data.Code, data.isGetNew);

            if (result.result != Result.OK)
            {
                return;
            }
            playerInfo.UpdateBindingEmailInfo(data.Email);
            bindSuccessProcess(BindInfoType.Mail, result);
            mailPage.close();
        }

        void initPlayerInfo()
        {
            setName(playerInfo.playerName);
            setID(playerInfo.userID);
            setMoney(playerInfo.playerMoney.ToString("N0"));
            setLV();
            setVip();
            playerInfo.myWallet.subscribeCoinChange(setMoney).AddTo(uiGameObject);
            playerInfo.headImageSubject.Subscribe(setHeadImage).AddTo(uiGameObject);
            playerInfo.nameSubject.Subscribe(setName).AddTo(uiGameObject);
            playerInfo.callHeadChanged();
        }

        public override void animOut()
        {
            clear();
        }

        #region OpenPage
        void openShopPage()
        {
            UiManager.getPresenter<Shop.ShopMainPresenter>().open();
        }

        void openEditPage()
        {
            UiManager.getPresenter<PlayerInfoEditPresenter>().open();
        }

        void openVipPage()
        {
            UiManager.getPresenter<VipInfoBoardPresenter>().open();
        }

        //readonly int openHighRollerLv = 20;
        void openDiamondClubPage()
        {
            //if (DataStore.getInstance.playerInfo.level < openHighRollerLv)
            //{
            //    tipNodePresenter.openLvTip(LvTipArrowDirection.Right, openHighRollerLv);
            //    return;
            //}
            UiManager.getPresenter<HighRollerMainPresenter>().open();
        }

        #endregion

        void setName(string name)
        {
            nameTxt.text = name;
        }

        void setID(string id)
        {
            idTxt.text = id;
        }

        void setMoney(string money)
        {
            moneyTxt.text = money;
        }

        void setLV()
        {
            lvTxt.text = $"LEVEL {playerInfo.level}";
        }

        void setHeadImage(Sprite headSprite)
        {
            if (null == headImg)
            {
                return;
            }
            headImg.sprite = headSprite;
        }

        void setVip()
        {
            var vipLevel = playerInfo.myVip.info.level;
            vipIconImage.sprite = VipSpriteGetter.getIconSprite(vipLevel);
            vipNameImage.sprite = VipSpriteGetter.getNameSprite(vipLevel);
        }
    }

    class BindInfoPresenter : NodePresenter
    {
        #region UIs
        CustomBtn bindBtn;
        GameObject beforeObj;
        //Text beforeContent;
        Text bindCoin;
        GameObject afterObj;

        RectTransform bindCoinParent;
        GameObject tapHighLightObj;
        #endregion

        public BindInfoType bindInfoType { get; private set; }
        BindInfoData bindInfoData;
        public PlayerInfo playerInfo { get { return DataStore.getInstance.playerInfo; } }

        public override void initUIs()
        {
            bindBtn = getCustomBtnData("bind_btn");
            beforeObj = getGameObjectData("before_obj");
            bindCoin = getBindingData<Text>("bind_coin");
            afterObj = getGameObjectData("after_obj");
            tapHighLightObj = getGameObjectData("tap_light_obj");
            bindCoinParent = bindCoin.transform.parent.GetComponent<RectTransform>();
        }

        public virtual BindInfoPresenter init(BindInfoData infoData)
        {
            bindInfoType = infoData.infoType;
            bindInfoData = infoData;
            bindBtn.clickHandler = bindBtnClick;
            bindBtn.pointerDownHandler = () => { tapLightActive(true); };
            bindBtn.pointerUPHandler = () => { tapLightActive(false); };
            openStateObj(infoData.isTypeBind);
            setBindCoin(infoData.bindCoin * playerInfo.coinExchangeRate);
            if (infoData.isTypeBind)
            {
                setBindState();
            }
            return this;
        }

        void tapLightActive(bool active)
        {
            tapHighLightObj.gameObject.setActiveWhenChange(active);
        }

        public virtual void setBindState()
        {

        }

        public void openStateObj(bool isBind)
        {
            beforeObj.SetActive(!isBind);
            afterObj.SetActive(isBind);
        }

        public void setBindCoin(long coinVal)
        {
            bindCoin.text = coinVal.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(bindCoinParent);
        }

        void bindBtnClick()
        {
            if (null != bindInfoData.bindClickEvent)
            {
                bindInfoData.bindClickEvent();
            }
        }
    }
    class BindInfoWithDownText : BindInfoPresenter
    {
        public Text afterContentUpTxt;

        public override void initUIs()
        {
            base.initUIs();
            afterContentUpTxt = getTextData("after_content_up");
        }
    }

    class MailBindingNode : BindInfoWithDownText
    {
        public override BindInfoPresenter init(BindInfoData infoData)
        {
            if (bindInfoType == BindInfoType.Mail)
            {
                playerInfo.bindMailSubject.Subscribe(mailBindState).AddTo(uiGameObject);
            }
            return base.init(infoData);
        }

        public override void setBindState()
        {
            mailBindState(playerInfo.mailVerifiedState);
        }
        public void mailBindState(MailVerifiedState verifiedState)
        {
            openStateObj(true);
            afterContentUpTxt.text = playerInfo.Email;
        }
    }

    class PhoneBindingNode : BindInfoWithDownText
    {
        public override void setBindState()
        {
            afterContentUpTxt.text = playerInfo.PhoneNumber;
        }
    }
    class BindInfoData
    {
        public BindInfoType infoType;
        public string unBindingMsgKey;
        public string bindingMsgKey;
        public bool isTypeBind;
        public Action bindClickEvent;
        public int bindCoin;
    }
}
