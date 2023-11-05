using UnityEngine.UI;
using UnityEngine;
using CommonILRuntime.Module;
using UniRx;

namespace Notice
{
    public class NumberNoticePresenter : NodePresenter
    {
        private RectTransform noticeObj;
        private CustomTextSizeChange noticeAmount;

        public override void initUIs()
        {
            noticeObj = getRectData("notice_rect");
            noticeAmount = getBindingData<CustomTextSizeChange>("notice_txt");
        }

        public override void init()
        {
            noticeObj.gameObject.setActiveWhenChange(false);
        }

        public void setSubject(Subject<int> noticeEvent)
        {
            noticeEvent.Subscribe(showNotice).AddTo(uiGameObject);
        }

        private void showNotice(int amount)
        {
            noticeObj.gameObject.setActiveWhenChange(amount > 0);
            noticeAmount.text = amount > 99 ? "99+" : amount.ToString();
        }
    }
}
