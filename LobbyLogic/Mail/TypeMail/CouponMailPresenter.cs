using CommonILRuntime.BindingModule;
using UnityEngine;
using Debug = UnityLogUtility.Debug;

namespace Lobby.Mail
{
    public class CouponMailPresenter : MailPresenter
    {
        CouponMessage couponData;
        public CouponMailPresenter()
        {
            onGet = onGetClickHandler;
        }

        public override void setData(IMessage data)
        {
            this.data = data;
            couponData = (CouponMessage)data;
            setRemainTime(data.getExpiredTime());
        }

        void onGetClickHandler()
        {
            getButton.enabled = false;
            var helper = new MailBoxProvider();
            readed();
            onReaded?.Invoke();
            openShop();
        }

        void openShop()
        {
            Debug.Log($"Coupon Go To Shop, couponId: {couponData.Id}, bonus: {couponData.bonus}, expiredAt:{couponData.expiredAt}");
            UiManager.getPresenter<Shop.ShopMainPresenter>().openWithCoupon(couponData.Id, couponData.bonus, couponData.expiredAt);
        }
    }
}
