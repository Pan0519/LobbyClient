using UnityEngine.UI;
using UnityEngine;
using CommonPresenter;
using CommonILRuntime.Module;
using TMPro;
using Lobby.UI;
using System;
using Services;
using System.Collections.Generic;

namespace Lobby.Common
{
    class TermPresenter : SystemUIBasePresenter
    {
        public override string objPath => $"{UtilServices.getOrientationObjPath("prefab/lobby_login/page_terms")}";

        public override UiLayer uiLayer { get => UiLayer.System; }
        #region UIs
        ScrollRect termsScrollView;
        Button closeTermBtn;
        //Button agreeBtn;
        TextMeshProUGUI termTxtEn;
        Text termTxtZh;
        #endregion

        Action agreenCB;
        Action closeTermCB;

        Dictionary<TermContent, string> termContentTypeDict = new Dictionary<TermContent, string>()
        {
            { TermContent.Disclaimer,"disclaimer"},
            { TermContent.Privacy,"privacy_policy"},
            { TermContent.Terms,"terms_of_service"},
        };

        public override void initUIs()
        {
            termTxtEn = getBindingData<TextMeshProUGUI>("term_txt_en");
            termTxtZh = getTextData("term_txt_zh");
            //agreeBtn = getBtnData("btn_agree");
            termsScrollView = getBindingData<ScrollRect>("terms_scrollview");
            closeTermBtn = getBtnData("close_btn");
        }

        public override void init()
        {
            base.init();
            //agreeBtn.onClick.AddListener(agreenTerms);
            closeTermBtn.onClick.AddListener(closeTermClick);
        }

        public async void openTermWindow(TermContent termContentType, Action agreenCB = null, Action closeCB = null)
        {
            BindingLoadingPage.instance.open();
            string contentFileName;
            if (!termContentTypeDict.TryGetValue(termContentType, out contentFileName))
            {
                clear();
                BindingLoadingPage.instance.close();
                return;
            }

            string content = await WebRequestText.instance.loadTextFromServer($"{contentFileName}_{ApplicationConfig.nowLanguage.ToString().ToLower()}");
            setTermContent(content);
        }

        void setTermContent(string content)
        {
            content = content.Replace("\\n", "\n");
            termTxtEn.gameObject.setActiveWhenChange(ApplicationConfig.Language.EN == ApplicationConfig.nowLanguage);
            termTxtZh.gameObject.setActiveWhenChange(ApplicationConfig.Language.ZH == ApplicationConfig.nowLanguage);
            switch (ApplicationConfig.nowLanguage)
            {
                case ApplicationConfig.Language.EN:
                    termTxtEn.text = content;
                    break;

                case ApplicationConfig.Language.ZH:
                    termTxtZh.text = content;
                    break;
            }
            //this.agreenCB = agreenCB;
            //closeTermCB = closeCB;
            BindingLoadingPage.instance.close();
        }

        //void agreenTerms()
        //{
        //    termsScrollView.normalizedPosition = new Vector2(0, 1.0f);

        //    if (null != agreenCB)
        //    {
        //        agreenCB();
        //    }

        //    closePresenter();
        //}

        public override void animOut()
        {
            clear();
        }

        void closeTermClick()
        {
            closeBtnClick();
        }
    }
}
