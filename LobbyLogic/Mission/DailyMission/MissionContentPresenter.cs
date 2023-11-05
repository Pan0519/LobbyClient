using CommonILRuntime.Module;
using UnityEngine.UI;

namespace Mission
{
    public class MissionContentPresenter : NodePresenter
    {
        int specifyTxtIndex;
        int lowCindition;
        Text[] texts;

        const string textBindingID = "content_txt_{0}";

        public override void initUIs()
        {
            int count = uiTransform.childCount;
            string bindingID = string.Empty;

            texts = new Text[count];
            for (int i = 0; i < count; ++i)
            {
                bindingID = string.Format(textBindingID, (i + 1).ToString());
                texts[i] = getTextData(bindingID);
            }
        }

        public void setLowDisplayCondition(int specifyTxtIndex, int lowCindition)
        {
            this.specifyTxtIndex = specifyTxtIndex;
            this.lowCindition = lowCindition;
        }

        public void setContentTxt(string content)
        {
            clearAllText();
            setContentMsg(content);
        }

        void clearAllText()
        {
            int length = texts.Length;
            for (int i = 0; i < length; ++i)
            {
                texts[i].text = string.Empty;
            }
        }

        void setContentMsg(string content)
        {
            var msgSplit = content.Split(';');

            if (msgSplit.Length <= lowCindition)
            {
                setContentMsgStartWithDefaultIndex(msgSplit);
            }
            else
            {
                setContentMsgStartWithSpecifyIndex(msgSplit);
            }
        }

        void setContentMsgStartWithDefaultIndex(string[] msgs)
        {
            var textIndex = specifyTxtIndex;
            for (int i = 0; i < msgs.Length; ++i)
            {
                texts[textIndex].text = msgs[i];
                textIndex++;
            }
        }

        void setContentMsgStartWithSpecifyIndex(string[] msgs)
        {
            int textIndex = 0;
            for (int i = 0; i < msgs.Length; ++i)
            {
                textIndex = getNextDisplayTextIndex(i);
                texts[textIndex].text = getDisplayMsg(msgs, i);
            }
        }

        int getNextDisplayTextIndex(int displayIndex)
        {
            if (displayIndex >= texts.Length)
            {
                displayIndex = texts.Length - 1;
            }

            return displayIndex;
        }

        string getDisplayMsg(string[] msgSplit, int displayIndex)
        {
            var result = string.Empty;
            if (displayIndex >= texts.Length)
            {
                result = texts[texts.Length - 1].text;
                result = $"{result}{msgSplit[displayIndex]}";
            }
            else
            {
                result = msgSplit[displayIndex];
            }

            return result;
        }
    }
}
