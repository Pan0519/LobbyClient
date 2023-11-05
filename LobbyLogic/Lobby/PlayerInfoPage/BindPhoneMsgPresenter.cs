using CommonILRuntime.Module;
using UnityEngine.UI;
using UniRx;
using System;
using System.Linq;
using System.Collections.Generic;
using CommonService;
using Network;

namespace Lobby.PlayerInfoPage
{
    class BindPhoneMsgPresenter : NodePresenter
    {
        #region BindingField
        InputField codeInput;
        InputField phoneInput;
        Toggle getMsgToggle;
        Dropdown areaCodeDropdown;
        Button verificationCodeBtn;
        Button confirmBtn;
        Button closeBtn;
        #endregion

        public Subject<string> PhoneInputObs { get; private set; }
        public Subject<BindingPhoneData> BindingPhoneDataObs { get; private set; }
        //string[] areacodesBackup = new string[] { "+93", "+355", "+213", "+1684", "+376", "+1264", "+6721", "+1268", "+54", "+374" };
        string[] areacodesBackup = new string[] { "+93", "+355", "+213", "+1684", "+376", };
        List<string> areacodes = new List<string>();

        bool isToggleOn;
        public override void initUIs()
        {
            codeInput = getBindingData<InputField>("code_input");
            phoneInput = getBindingData<InputField>("phone_input");
            getMsgToggle = getBindingData<Toggle>("get_msg_toggle");
            areaCodeDropdown = getBindingData<Dropdown>("areacode_dropdown");
            verificationCodeBtn = getBtnData("verification_code_btn");
            confirmBtn = getBtnData("confirm_btn");
            closeBtn = getBtnData("close_btn");
        }

        public override void init()
        {
            codeInput.placeholder.GetComponent<Text>().text = LanguageService.instance.getLanguageValue("VerificationCodeEnter");
            phoneInput.placeholder.GetComponent<Text>().text = LanguageService.instance.getLanguageValue("VerificationCodePhoneNumber");
            setAreacodeData();

            closeBtn.onClick.AddListener(close);
            confirmBtn.onClick.AddListener(confirmClick);
            verificationCodeBtn.onClick.AddListener(verificationCodeClick);
            getMsgToggle.isOn = true;
            getMsgToggle.onValueChanged.AddListener(confirmBtnActive);

            areaCodeDropdown.options.Clear();
            areaCodeDropdown.AddOptions(areacodes);

            phoneInput.onValueChanged.AddListener(phoneInputValueChanged);
            codeInput.onValueChanged.AddListener(codeInputValueChanged);

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "PhoneBindingErrorTitle",
                contentKey = "PhoneBindingError",
            }, Result.PhoneNumberRegistered);

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "PhoneIncorrectTitle",
                contentKey = "PhoneIncorrect",
            }, Result.PhoneAskVerificationsFail, Result.PhoneNumberIsNotValid, Result.PhoneWrongParameter);

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "OperationTitle",
                contentKey = "OperationError",
            }, Result.PhoneVerificationsError);

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "VerificationCodeErrorTitle",
                contentKey = "VerificationCodeErrorContent",
            }, Result.PhoneVerificationFail, Result.PhoneVerificationProcessError);
        }

        public override void open()
        {
            verificationCodeBtn.interactable = false;
            confirmBtn.interactable = false;
            phoneInput.text = string.Empty;
            codeInput.text = string.Empty;

            PhoneInputObs = new Subject<string>();
            BindingPhoneDataObs = new Subject<BindingPhoneData>();

            base.open();
        }

        void phoneInputValueChanged(string val)
        {
            verificationCodeBtn.interactable = !string.IsNullOrEmpty(val);
        }

        void codeInputValueChanged(string val)
        {
            confirmBtn.interactable = !string.IsNullOrEmpty(val) && !string.IsNullOrEmpty(phoneInput.text);
        }

        void confirmClick()
        {
            BindingPhoneData data = new BindingPhoneData()
            {
                PhoneNumber = areacodes[areaCodeDropdown.value] + phoneInput.text,
                VerifyCode = codeInput.text
            };

            BindingPhoneDataObs.OnNext(data);
        }

        void confirmBtnActive(bool isToggleOn)
        {
            this.isToggleOn = isToggleOn;
        }

        IDisposable verificationBtnCountDown;

        private void verificationCodeClick()
        {
            string phoneNumber = areacodes[areaCodeDropdown.value] + phoneInput.text;

            verificationCodeBtn.interactable = false;
            verificationBtnCountDown = Observable.Timer(TimeSpan.FromMinutes(5)).Subscribe(_ =>
            {
                verificationCodeBtn.interactable = true;
            });

            PhoneInputObs.OnNext(phoneNumber);
        }

        public override void close()
        {
            Services.UtilServices.disposeSubscribes(BindingPhoneDataObs.Subscribe(), PhoneInputObs.Subscribe(), verificationBtnCountDown);
            base.close();
        }

        private void setAreacodeData()
        {
            areacodes.Clear();
            areacodes.Add("+886");
            if (DataStore.getInstance.dataInfo.areaCode != null && DataStore.getInstance.dataInfo.areaCode.Count > 0)
            {
                var areaCodeEnum = DataStore.getInstance.dataInfo.areaCode.GetEnumerator();
                while (areaCodeEnum.MoveNext())
                {
                    areacodes.Add($"+{areaCodeEnum.Current.Value}");
                }
                return;
            }
            areacodes.AddRange(areacodesBackup);
        }
    }

    class BindingPhoneData
    {
        public string PhoneNumber;
        public string VerifyCode;
    }
}
