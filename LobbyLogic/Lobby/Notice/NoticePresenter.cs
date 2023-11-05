using UnityEngine.UI;
using UnityEngine;
using CommonILRuntime.Module;
using UniRx;

namespace Notice
{
    public class NoticePresenter : NodePresenter
    {
        private RectTransform noticeObj;

        public override void initUIs()
        {
            noticeObj = getRectData("notice_rect");
        }
        public void setSubject(Subject<int> noticeEvent)
        {
            noticeEvent.Subscribe(showNotice).AddTo(uiGameObject);
        }

        public void showNotice(int amount)
        {
            noticeObj.gameObject.setActiveWhenChange(amount > 0);
        }
    }
}
