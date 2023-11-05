using CommonILRuntime.Module;
using UnityEngine.UI;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Network;
using UniRx;
using Debug = UnityLogUtility.Debug;

namespace Lobby.PlayerInfoPage
{
    class BindMailMsgPresenter : NodePresenter
    {
        #region BindingField
        InputField mailInput;
        Toggle getGameNewToggle;
        Button confirmBtn;
        Button closeBtn;
        InputField codeInput;
        Button acquireCodeBtn;
        #endregion

        public Subject<string> MailObserve { get; private set; }
        public Subject<BindingMailData> BindingMailDataObserve { get; private set; }

        public override void initUIs()
        {
            mailInput = getBindingData<InputField>("mail_input");
            getGameNewToggle = getBindingData<Toggle>("mail_toggle");
            confirmBtn = getBtnData("mail_confirm_btn");
            closeBtn = getBtnData("close_btn");
            codeInput = getBindingData<InputField>("code_input");
            acquireCodeBtn = getBtnData("acquire_code_btn");
        }

        public override void init()
        {
            mailInput.placeholder.GetComponent<Text>().text = LanguageService.instance.getLanguageValue("EmailHint");
            codeInput.placeholder.GetComponent<Text>().text = LanguageService.instance.getLanguageValue("VerificationCodeEnter");

            closeBtn.onClick.AddListener(close);
            acquireCodeBtn.onClick.AddListener(SendDataToVerfy);
            confirmBtn.onClick.AddListener(confirmClick);
            mailInput.onValueChanged.AddListener(mailInputValueChanged);
            codeInput.onValueChanged.AddListener(codeInputValueChanged);
            getGameNewToggle.isOn = true;

            acquireBtnEnableCheck(false);

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "OperationTitle",
                contentKey = "OperationError",
            }, Result.MailVerificationsError);

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "EmailIncorrectTitle",
                contentKey = "EmailIncorrect",
            }, Result.MailAskVerificationsFail, Result.MailIsNotValid, Result.MailWrongParameter);

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "EmailBindingErrorTitle",
                contentKey = "EmailBindingError",
            }, Result.MailRegistered);

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "VerificationCodeErrorTitle",
                contentKey = "VerificationCodeErrorContent",
            }, Result.MailVerificationProcessError, Result.MailVerificationFail);
        }

        public override void open()
        {
            MailObserve = new Subject<string>();
            BindingMailDataObserve = new Subject<BindingMailData>();

            confirmBtn.interactable = false;

            base.open();
        }

        public override void close()
        {
            Services.UtilServices.disposeSubscribes(MailObserve.Subscribe(), BindingMailDataObserve.Subscribe());
            base.close();
        }

        void confirmClick()
        {
            BindingMailData data = new BindingMailData()
            {
                Email = mailInput.text,
                Code = codeInput.text,
                isGetNew = getGameNewToggle.isOn
            };

            BindingMailDataObserve.OnNext(data);
        }

        private void mailInputValueChanged(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                acquireCodeBtn.interactable = false;
                return;
            }
            acquireBtnEnableCheck(isValidEmail(val));
        }

        private void codeInputValueChanged(string conetent)
        {
            confirmBtnEnableCheck(!string.IsNullOrEmpty(conetent));
        }

        private void confirmBtnEnableCheck(bool isInteractable)
        {
            confirmBtn.interactable = isInteractable;
        }

        private void acquireBtnEnableCheck(bool isON)
        {
            acquireCodeBtn.interactable = isON;
        }

        private void SendDataToVerfy()
        {
            MailObserve.OnNext(mailInput.text);
        }

        bool isValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();
                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);
                    return match.Groups[1].Value + domainName;
                }

                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException e)
            {
                Debug.LogError($"isValidEmail RegexMatchTimeoutException {e.Message}");
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"isValidEmail ArgumentException {e.Message}");
            }

            return false;
        }
    }

    class BindingMailData
    {
        public string Email;
        public string Code;
        public bool isGetNew;
    }
}
