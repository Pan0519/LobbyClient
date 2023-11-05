using CommonILRuntime.BindingModule;
using UnityEngine;

namespace Lobby.Mail
{
    public class MailFactory
    {
        //Templates
        GameObject couponTemplate;
        GameObject systemTemplate;

        public MailFactory(string[] resNames)
        {
            couponTemplate = ResourceManager.instance.getGameObjectWithResOrder("prefab/lobby_mail/mail_coupon", resNames);
            systemTemplate = ResourceManager.instance.getGameObjectWithResOrder("prefab/lobby_mail/mail_money", resNames);
        }

        public IMailPresenter createMail(IMessage data)
        {
            IMailPresenter mail = null;
            switch (data.getType())
            {
                case MailType.COUPON:
                    {
                        mail = CouponMail();
                    }
                    break;
                case MailType.SYSTEM:
                    {
                        mail = SystemMail();
                    }
                    break;
                default:
                    {
                        Debug.LogWarning($"createMail, mail type err: {data.getType()}");
                    }
                    break;
            }
            return mail;
        }

        IMailPresenter CouponMail()
        {
            var mail = GameObject.Instantiate(couponTemplate);
            var p = UiManager.bindNode<CouponMailPresenter>(mail);
            return p;
        }

        IMailPresenter SystemMail()
        {
            var mail = GameObject.Instantiate(systemTemplate);
            var p = UiManager.bindNode<SystemMailPresenter>(mail);
            return p;
        }
    }
}
