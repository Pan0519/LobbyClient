using CommonILRuntime.Module;
using CommonPresenter;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Mail
{
    public class MailBoxPresenter : SystemUIBasePresenter
    {
        public override string objPath { get { return "prefab/lobby_mail/mail_main"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        public Action clearCallback = null;

        //UI Components
        Button closeBtn;
        ScrollRect scrollRect;

        //UI Root
        Transform mailRoot;

        MailFactory factory;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyMail)};
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeBtn = getBtnData("closeButton");
            scrollRect = getBindingData<ScrollRect>("scrollRect");
            mailRoot = getBindingData<Transform>("mailRoot");
        }

        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);
            factory = new MailFactory(resOrder);
        }

        //TODO: change Message to message
        public void addMails(List<IMessage> mails)
        {
            List<IMessage> sortedMails = classifyAndSort(mails);
            //var sortedMails = mails;
            for (int i = 0; i<sortedMails.Count; i++)
            {
                var data = sortedMails[i];
                IMailPresenter mail = factory.createMail(data);
                if (null != mail)
                {
                    mail.getObj().transform.SetParent(mailRoot, false);
                    mail.setData(data);
                    if (MailType.COUPON == mail.mailType)
                    {
                        mail.setReadedListener(closeBtnClick);
                    }
                }
                else
                {
                    Debug.Log("IMailPresenter null");
                }
            }

            scrollRect.normalizedPosition = new Vector2(0, 1);
        }

        public override void animOut()
        {
            clearCallback?.Invoke();
            clear();
        }

        List<IMessage> classifyAndSort(List<IMessage> mails)
        {
            MailType[] sortOrder = new MailType[] { MailType.COUPON, MailType.SYSTEM };

            //Allocate classified buckets
            Dictionary<MailType, List<IMessage>> buckets = new Dictionary<MailType, List<IMessage>>();
            for (int i = 0; i < sortOrder.Length; i++)
            {
                buckets.Add(sortOrder[i], new List<IMessage>());
            }
            buckets.Add(MailType.OTHER, new List<IMessage>());

            //Classify to buckets
            for (int i = 0; i < mails.Count; i++)
            {
                var mail = mails[i];
                List<IMessage> bucket;
                if (buckets.TryGetValue(mail.getType(), out bucket))
                {
                    bucket.Add(mail);
                }
                else
                {
                    buckets[MailType.OTHER].Add(mail);
                }
            }

            List<IMessage> result = new List<IMessage>();
            //Combine buckets
            for (int i = 0; i < sortOrder.Length; i++)
            {
                var bucket = buckets[sortOrder[i]];
                result.AddRange(sortByEndTime(bucket));
            }
            result.AddRange(buckets[MailType.OTHER]);

            return result;
        }

        List<IMessage> sortByEndTime(List<IMessage> mails)
        {
            mails.Sort((IMessage x, IMessage y) =>
            {
                return x.getExpiredTime() < y.getExpiredTime()? -1:1;
            });

            return mails;
        }
    }
}
