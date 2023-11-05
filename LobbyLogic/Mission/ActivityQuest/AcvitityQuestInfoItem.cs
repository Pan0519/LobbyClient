using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using System;
using Lobby.Common;
using UniRx;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using Services;

namespace Mission
{
    public class AcvitityQuestInfoItem : NodePresenter
    {
        #region UI Obj
        private Image bg;
        private Text info;
        #endregion

        public override void initUIs()
        {
            bg = getImageData("quest_info_img");
            info = getTextData("quest_info_txt");
        }

        public void setQuestContent(string questInfo, params string[] questCondition)
        {
            bg.sprite = ActivityQuestData.getQuestImage(questInfo);
            info.text = ActivityQuestData.getQuestInfoContent(questInfo, questCondition);
        }
    }
}
