using CommonILRuntime.BindingModule;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonILRuntime.Outcome;

namespace Lobby.Mail
{
    /// <summary>
    /// 用來包裝對於LobbyBottomBar的解藕，也提供由其他系統開啟信箱
    /// </summary>
    public class MailBoxProvider
    {
        public async Task<int> peekMails()
        {
            var response = await AppManager.lobbyServer.peekMailCount();
            if (Result.OK == response.result)
            {
                return response.count;
            }
            return 0;
        }

        public async Task<CommonReward[]> redeemMail(string mailId)
        {
            var response = await AppManager.lobbyServer.redeemMail(mailId);
            if (Result.OK == response.result)
            {
                return response.rewards;
            }
            return null;
        }

        public async void openMailBox(Action clearCallback)
        {
            HighRoller.HighRollerDataManager.instance.checkGetReturnToPayTime();
            var response = await AppManager.lobbyServer.getAllMail();
            var presenter = UiManager.getPresenter<MailBoxPresenter>();
            if (Result.OK == response.result)
            {
                if (null != response.messages)
                {
                    var mails = toSystemsMessages(response.messages);
                    //mails = MailTestDataCreator.make();
                    presenter.addMails(mails);
                }
            }

            var couponResponse = await AppManager.lobbyServer.getCoupons();
            if (Result.OK == couponResponse.result)
            {
                if (null != couponResponse.coupons)
                {
                    var messages = toCouponMessages(couponResponse.coupons);
                    //var messages = MailTestDataCreator.makeCouponMessages();
                    presenter.addMails(messages);

                }
            }
            presenter.clearCallback = clearCallback;
            presenter.open();
        }

        //TODO: beautify and refactor converters
        List<IMessage> toSystemsMessages(MailData[] source)
        {
            List<IMessage> outData = new List<IMessage>();
            for (int i = 0; i < source.Length; i++)
            {
                SystemMessage target = new SystemMessage();
                target.convertServerMailData(source[i]);
                outData.Add(target);
            }
            return outData;
        }

        //TODO: beautify and refactor converters
        List<IMessage> toCouponMessages(Coupon[] coupons)
        {
            List<IMessage> outData = new List<IMessage>();
            for (int i = 0; i < coupons.Length; i++)
            {
                CouponMessage target = new CouponMessage();
                target.convertServerCouponData(coupons[i]);
                outData.Add(target);
            }
            return outData;
        }

        //Dictionary<string, MailType> mailTypeFromServer = new Dictionary<string, MailType>()
        //{
        //    {"system",MailType.SYSTEM},
        //    {"coupon", MailType.COUPON},
        //    {"coin",MailType.SYSTEM},
        //};

       
    }
}
