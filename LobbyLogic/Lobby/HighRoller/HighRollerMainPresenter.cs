using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using CommonPresenter;
using Service;
using System.Threading.Tasks;
using LobbyLogic.NetWork.ResponseStruct;
using UnityEngine;
using UnityEngine.UI;
using Common;
using CommonService;
using System;
using System.Collections.Generic;
using UniRx;
using Services;
using LobbyLogic.Common;

namespace HighRoller
{
    class HighRollerMainPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/diamond_club/dc_main";
        public override UiLayer uiLayer { get => UiLayer.System; }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;
        const int maxPoint = 20000;
        #region UIs
        Button closeBtn;
        Button infoBtn;
        Image progressBar;
        Text progressTxt;
        GameObject leftCrownObj;
        GameObject rightCrownObj;
        Text crownPointTxt;
        Text transCoinTxt;
        Button tapBtn;
        Button pointInfoBtn;
        Text daysTxt;
        RectTransform formulaLayout;
        RectTransform mainGroupRect;
        GameObject mainTopObj;
        GameObject daysPointObj;
        #endregion

        InfoBaseNode infoNode;
        GuideStepNode guideStepNode;
        AuthorityGroupNode authorityGroupNode;
        int nowStepID;
        bool isGuide = false;
        HighRollerUserRecordResponse userRecord;
        HighRollerCheckExpireResponse checkExpireResponse;
        long passPoint;
        TimerService diamondExpireTimeServices;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Crown) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            infoBtn = getBtnData("info_btn");
            progressBar = getImageData("progressBar");
            progressTxt = getTextData("progress_txt");
            leftCrownObj = getGameObjectData("crown_left_obj");
            rightCrownObj = getGameObjectData("crown_right_obj");
            crownPointTxt = getTextData("crown_point_txt");
            transCoinTxt = getTextData("trans_coin_txt");
            tapBtn = getBtnData("tap_btn");
            pointInfoBtn = getBtnData("point_info_btn");
            daysTxt = getTextData("days_txt");
            formulaLayout = getRectData("formula_layout");
            mainGroupRect = getRectData("main_group");
            mainTopObj = getGameObjectData("main_top_obj");
            daysPointObj = getGameObjectData("club_point_obj");
        }

        public override void init()
        {
            base.init();
            authorityGroupNode = UiManager.bindNode<AuthorityGroupNode>(getNodeData("authority_group_node").cachedGameObject);
            guideStepNode = UiManager.bindNode<GuideStepNode>(getNodeData("guide_step_node").cachedGameObject);
            guideStepNode.close();
            infoNode = UiManager.bindNode<InfoBaseNode>(getNodeData("info_node").cachedGameObject);
            closeBtn.onClick.AddListener(closeBtnClick);
            infoBtn.onClick.AddListener(openInfo);
            tapBtn.onClick.AddListener(closeAuthority);
            pointInfoBtn.onClick.AddListener(openPointInfoPage);
            authorityGroupNode.openVaultSub.Subscribe(openVaultSub).AddTo(uiGameObject);
            authorityGroupNode.isOpenAuthoritySub.Subscribe(openAuthority).AddTo(uiGameObject);
            DataStore.getInstance.playerInfo.addPassPointSub.Subscribe(addPassPoint).AddTo(uiGameObject);
            HighRollerDataManager.instance.userRecordSub.Subscribe(updateUserRecord).AddTo(uiGameObject);
        }

        public override async void open()
        {
            await initData();
            base.open();
            if (isGuide)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
                guideStepNode.stepIDSub.Subscribe(nowGuideStep).AddTo(uiGameObject);
                guideStepNode.stepObjSub.Subscribe(addObjToGuide).AddTo(uiGameObject);
                guideStepNode.startGuide();
            }
        }

        async Task initData()
        {
            userRecord = await AppManager.lobbyServer.getHighRollerUser();
            updateUserData();
        }

        void updateUserRecord(HighRollerUserRecordResponse recordResponse)
        {
            userRecord = recordResponse;
            updateUserData();
        }

        void updateUserData()
        {
            DateTime accessInfoExpireAtTime = UtilServices.strConvertToDateTime(userRecord.accessInfo.expiredAt, DateTime.MaxValue);
            CompareTimeResult compareResult = UtilServices.compareTimeWithNow(accessInfoExpireAtTime);
            authorityGroupNode.setAuthorityItemsLock(CompareTimeResult.Earlier == compareResult);
            setTransCoinTxt();
            passPoint = userRecord.passPoints;
            updatePassPointProgress();
            HighRollerAccessDetail[] details = userRecord.accessInfo.details;
            int detailsLength = details.Length;
            daysPointObj.setActiveWhenChange(detailsLength > 0);
            leftCrownObj.setActiveWhenChange(detailsLength >= 1);
            rightCrownObj.setActiveWhenChange(detailsLength >= 2);

            AccessInfo accessInfo = HighRollerDataManager.instance.accessInfo;
            DateTime expireTime = UtilServices.strConvertToDateTime(accessInfo.expiredAt, DateTime.MaxValue);
            TimeStruct expireTimeStruct = UtilServices.toTimeStruct(expireTime.Subtract(UtilServices.nowTime));
            if (expireTimeStruct.days >= 1)
            {
                daysTxt.text = expireTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
            }
            else
            {
                countdownExpireTime();
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(formulaLayout);
            //isGuide = !userRecord.accessExperienceUsed;
            formulaLayout.gameObject.setActiveWhenChange(userRecord.accessInfo.details.Length >= 2);
        }
        void countdownExpireTime()
        {
            DateTime expireTime = UtilServices.strConvertToDateTime(userRecord.accessInfo.expiredAt, DateTime.MaxValue);
            TimeStruct expireTimeStruct = UtilServices.toTimeStruct(expireTime.Subtract(UtilServices.nowTime));
            if (expireTimeStruct.days >= 1)
            {
                daysTxt.text = expireTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
            }
            else
            {
                diamondExpireTimeServices = new TimerService();
                diamondExpireTimeServices.setAddToGo(uiGameObject);
                diamondExpireTimeServices.StartTimer(expireTime, updateExpireTimer);
            }
        }

        void updateExpireTimer(TimeSpan updateTime)
        {
            if (updateTime <= TimeSpan.Zero)
            {
                diamondExpireTimeServices.ExecuteTimer();
                HighRollerDataManager.instance.getHighUserRecordAndCheck();
                return;
            }
            daysTxt.text = UtilServices.formatCountTimeSpan(updateTime);
        }

        void setTransCoinTxt()
        {
            var transToCoin = (userRecord.passPoints * HighRollerDataManager.instance.getHighRollerCoinExchangeRate) / 300;
            transCoinTxt.text = transToCoin.ToString("N0");
        }

        void updatePassPointProgress()
        {
            if (userRecord.accessInfo.details.Length < 2)
            {
                if (passPoint < maxPoint)
                {
                    progressTxt.text = $"{passPoint}/{maxPoint}";
                }
                else
                {
                    progressTxt.text = $"{passPoint}";
                }
                progressBar.fillAmount = (float)passPoint / (float)maxPoint;
            }
            else
            {
                progressTxt.text = passPoint.ToString();
                progressBar.fillAmount = 1;
            }

            crownPointTxt.text = $"{passPoint} =";
        }

        void addPassPoint(long addPoint)
        {
            passPoint += addPoint;
            updatePassPointProgress();
        }

        void openVaultSub(HighRollerVaultPresenter vaultPresenter)
        {
            vaultPresenter.setUserRecord(userRecord);
        }
        void openAuthority(bool isOpen)
        {
            tapBtn.gameObject.setActiveWhenChange(isOpen);
        }

        void closeAuthority()
        {
            authorityGroupNode.closeTip();
        }

        void openPointInfoPage()
        {
            UiManager.getPresenter<HighRollerEarningPoints>().openPage(userRecord.cumulation.spinTimes, closePresenter);
        }

        void nowGuideStep(int id)
        {
            nowStepID = id;
        }

        void addObjToGuide(GameObject stepObj)
        {
            switch (nowStepID)
            {
                case 1:
                    mainTopObj.transform.SetParent(stepObj.transform);
                    break;

                case 2:
                    mainTopObj.transform.SetParent(mainGroupRect);
                    authorityGroupNode.enabelBtns(false);
                    authorityGroupNode.uiTransform.SetParent(stepObj.transform);
                    checkExperienceUsed();
                    break;

                case 3:
                case 4:
                    authorityGroupNode.enabelBtns(true);
                    authorityGroupNode.uiTransform.SetParent(mainGroupRect);
                    break;

                default:
                    if (null == stepObj && null != checkExpireResponse)
                    {
                        HighRollerDataManager.instance.userRecordSub.Subscribe(record =>
                        {
                            userRecord = record;
                            updateUserData();
                        }).AddTo(uiGameObject);
                        HighRollerRewardManager.openReward(checkExpireResponse.highRoller);
                    }
                    break;
            }
        }

        async void checkExperienceUsed()
        {
            checkExpireResponse = await AppManager.lobbyServer.checkExperienceUsed();
            if (string.IsNullOrEmpty(checkExpireResponse.highRoller.awardBoardType) || checkExpireResponse.highRoller.awardBoardType.Equals("pass-transfer-coin"))
            {
                guideStepNode.specifyGuideStep(nowStepID + 1);
            }
        }

        void openInfo()
        {
            infoNode.open();
        }

        void closeInfo()
        {
            infoNode.closeBtnClick();
        }

        public override async void animOut()
        {
            clear();
            if (GameOrientation.Portrait == await DataStore.getInstance.dataInfo.getNowGameOrientation())
            {
                await UIRootChangeScreenServices.Instance.justChangeScreenToProp();
            }
            GamePauseManager.gameResume();
        }
    }

    class GuideStepNode : NodePresenter
    {
        Button guideBtn;
        Text guideClubNum;
        GameObject[] guideObjs = new GameObject[5];
        int guideStepID = 0;
        public Subject<int> stepIDSub = new Subject<int>();
        public Subject<GameObject> stepObjSub = new Subject<GameObject>();
        public override void initUIs()
        {
            guideBtn = getBtnData("tutorial_step_btn");
            guideClubNum = getTextData("guide_club_num");
            for (int i = 0; i < guideObjs.Length; ++i)
            {
                guideObjs[i] = getGameObjectData($"step_{i + 1}_obj");
            }
        }

        public override void init()
        {
            guideBtn.onClick.AddListener(toNextStep);
            guideClubNum.text = $"1 {LanguageService.instance.getLanguageValue("Time_Days")}";
        }

        public void startGuide()
        {
            showNowStep();
            open();
        }

        public void specifyGuideStep(int stepID)
        {
            guideStepID = stepID;
        }

        void toNextStep()
        {
            guideStepID++;
            showNowStep();
        }

        void showNowStep()
        {
            stepIDSub.OnNext(guideStepID);
            for (int i = 0; i < guideObjs.Length; ++i)
            {
                bool isShow = i == guideStepID;
                GameObject showObj = guideObjs[i];
                showObj.setActiveWhenChange(isShow);
                if (isShow)
                {
                    stepObjSub.OnNext(showObj);
                }
            }

            if (guideStepID >= guideObjs.Length)
            {
                stepObjSub.OnNext(null);
                close();
            }
        }
    }

    class AuthorityGroupNode : NodePresenter
    {
        Animator tipAnim;
        Animator tipPetAnim;
        RectTransform tipRect;
        Text tipMsg;

        AuthorityItemNode waitOpenNode;
        AuthorityItemNode selectOpenNode;
        IDisposable waitTipDis;
        IDisposable closeTipDis;
        Dictionary<AuthorityItemKind, AuthorityItemNode> authorityItemNodes = new Dictionary<AuthorityItemKind, AuthorityItemNode>();
        //AuthorityPetMasterNode petMasterNode;
        string[] bindingNames = new string[] { "club_vault", "higher_xp", "card_pack", "huge_bonus", "boosted_coin", "high_roller" };

        public Subject<HighRollerVaultPresenter> openVaultSub = new Subject<HighRollerVaultPresenter>();
        public Subject<bool> isOpenAuthoritySub = new Subject<bool>();
        public override void initUIs()
        {
            tipAnim = getAnimatorData("tip_anim");
            tipPetAnim = getAnimatorData("tip_pet_anim");
            tipMsg = getTextData("tip_msg");
        }

        public override void init()
        {
            tipRect = tipAnim.gameObject.GetComponent<RectTransform>();
            tipRect.gameObject.setActiveWhenChange(false);
            tipPetAnim.gameObject.setActiveWhenChange(false);
            for (int i = 0; i < bindingNames.Length; ++i)
            {
                bindingItemNode((AuthorityItemKind)i);
            }

            //petMasterNode = UiManager.bindNode<AuthorityPetMasterNode>(getNodeData("pet_master_node").cachedGameObject);
            //petMasterNode.onClickEvent.Subscribe(petMasterBtnClick).AddTo(petMasterNode.uiGameObject);
            //petMasterNode.setSelfKind(AuthorityItemKind.PetMaster);
        }

        public void enabelBtns(bool enable)
        {
            var itemNodes = authorityItemNodes.GetEnumerator();
            while (itemNodes.MoveNext())
            {
                itemNodes.Current.Value.btnEnable(enable);
            }
            //petMasterNode.btnEnable(enable);
        }

        public void setAuthorityItemsLock(bool isLocking)
        {
            var itemNodes = authorityItemNodes.GetEnumerator();
            while (itemNodes.MoveNext())
            {
                itemNodes.Current.Value.setLockObj(isLocking);
            }
            //petMasterNode.setLockObj(isLocking);
        }

        public void setVaultIsLock(bool isLocking)
        {
            authorityItemNodes[AuthorityItemKind.Vault].setLockObj(isLocking);
        }

        void bindingItemNode(AuthorityItemKind itemKind)
        {
            int kindID = (int)itemKind;
            var itemNode = UiManager.bindNode<AuthorityItemNode>(getNodeData($"{bindingNames[kindID]}_node").cachedGameObject).setMsgKey($"DiamondClub_Benefits_Text_{kindID + 1}");
            itemNode.setSelfKind(itemKind);
            itemNode.onClickEvent.Subscribe(authorityBtnClick).AddTo(itemNode.uiGameObject);
            authorityItemNodes.Add(itemKind, itemNode);
        }

        void authorityBtnClick(AuthorityItemNode vaultData)
        {
            waitOpenNode = null;
            if (AuthorityItemKind.Vault == vaultData.selfKind && !vaultData.isLocking)
            {
                if (null != selectOpenNode)
                {
                    closeTip();
                }
                var vaultPresenter = UiManager.getPresenter<HighRollerVaultPresenter>();
                vaultPresenter.open();
                openVaultSub.OnNext(vaultPresenter);
                return;
            }

            if (null != selectOpenNode)
            {
                if (selectOpenNode.selfKind != vaultData.selfKind)
                {
                    waitOpenNode = vaultData;
                }

                closeTip();
                return;
            }
            selectOpenNode = vaultData;
            setTipMsg(vaultData.msgKey);
            tipRect.SetParent(vaultData.uiTransform);
            var vaultPos = tipRect.anchoredPosition;
            vaultPos.Set(vaultData.objWidth * -1, 13);
            tipRect.anchoredPosition = vaultPos;
            tipRect.gameObject.setActiveWhenChange(true);
            isOpenAuthoritySub.OnNext(true);
            countDownCloseTip();
        }

        //IDisposable closePetMasterDis = null;
        //void petMasterBtnClick(AuthorityItemNode petMasterNode)
        //{
        //    if (petMasterNode.isLocking)
        //    {
        //        if (tipPetAnim.gameObject.activeSelf)
        //        {
        //            closePetMasterTip();
        //            return;
        //        }
        //        tipPetAnim.gameObject.setActiveWhenChange(true);
        //        closePetMasterDis = Observable.Timer(TimeSpan.FromSeconds(5.0f)).Subscribe(_ =>
        //        {
        //            closePetMasterTip();
        //        }).AddTo(uiGameObject);
        //        return;
        //    }
        //}
        //void closePetMasterTip()
        //{
        //    petMasterNode.btnEnable(false);
        //    UtilServices.disposeSubscribes(closePetMasterDis);
        //    tipPetAnim.SetTrigger("close");
        //    closePetMasterDis = null;
        //    Observable.TimerFrame(20).Subscribe(_ =>
        //    {
        //        petMasterNode.btnEnable(true);
        //        tipPetAnim.gameObject.setActiveWhenChange(false);
        //    }).AddTo(uiGameObject);
        //}
        void countDownCloseTip()
        {
            closeTipDis = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ =>
            {
                closeTip();
            }).AddTo(uiGameObject);
        }

        public void closeTip()
        {
            isOpenAuthoritySub.OnNext(false);
            UtilServices.disposeSubscribes(waitTipDis, closeTipDis);
            tipAnim.SetTrigger("close");
            waitTipDis = Observable.TimerFrame(23).Subscribe(_ =>
             {
                 selectOpenNode = null;
                 tipRect.gameObject.setActiveWhenChange(false);
                 if (null != waitOpenNode)
                 {
                     authorityBtnClick(waitOpenNode);
                     waitOpenNode = null;
                 }
             }).AddTo(tipAnim.gameObject);
        }

        void setTipMsg(string key)
        {
            tipMsg.text = LanguageService.instance.getLanguageValue(key);
        }
    }

    class AuthorityItemNode : NodePresenter
    {
        Button clickBtn;
        GameObject lockObj;
        public string msgKey { get; private set; }
        public float objWidth { get { return uiRectTransform.rect.width; } }
        public Subject<AuthorityItemNode> onClickEvent { get; private set; } = new Subject<AuthorityItemNode>();
        public AuthorityItemKind selfKind { get; private set; }
        public bool isLocking { get; private set; }

        public AuthorityItemNode setMsgKey(string key)
        {
            msgKey = key;
            return this;
        }

        public void setSelfKind(AuthorityItemKind selfKind)
        {
            this.selfKind = selfKind;
        }

        public override void initUIs()
        {
            clickBtn = getBtnData("tip_btn");
            lockObj = getGameObjectData("lock_obj");
        }

        public override void init()
        {
            clickBtn.onClick.AddListener(() =>
            {
                onClickEvent.OnNext(this);
            });
        }

        public void setLockObj(bool isLocking)
        {
            this.isLocking = isLocking;
            lockObj.setActiveWhenChange(isLocking);
        }

        public void btnEnable(bool enable)
        {
            clickBtn.enabled = enable;
        }
    }

    class AuthorityPetMasterNode : AuthorityItemNode
    {
        Text seasonNumTxt;
        Text tipNumTxt;
        RectTransform seasonRect;

        public override void initUIs()
        {
            base.initUIs();
            seasonNumTxt = getTextData("season_num_txt");
            tipNumTxt = getTextData("tip_num_txt");
            seasonRect = getRectData("season_group_rect");
        }

        public override void init()
        {
            base.init();
            tipNumTxt.text = "0";
        }

        public void setSeasonNum(int season)
        {
            seasonNumTxt.text = season.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(seasonRect);
        }
    }

    enum AuthorityItemKind
    {
        Vault,
        XP,
        Pack,
        Bonus,
        CoinStore,
        Roller,
        PetMaster,
    }
}
