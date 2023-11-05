using CommonPresenter;
using CommonILRuntime.BindingModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using CommonService;

namespace Services
{
    public class PlayerMoneyServices
    {
        PlayerMoneyPresenter _moneyPresneter;
        PlayerMoneyPresenter moneyPresenter
        {
            get
            {
                if (null == _moneyPresneter)
                {
                    _moneyPresneter = UiManager.getPresenter<PlayerMoneyPresenter>();
                }
                return _moneyPresneter;
            }
        }

        public void addTo(Transform parentTrans)
        {
            moneyPresenter.addTo(parentTrans);
        }
        public void returnToLastParent()
        {
            moneyPresenter.returnToLastParent();
        }

        public void setMoney(string moneyFormat)
        {
            moneyPresenter.updateMoney(moneyFormat);
        }

        public void clearMoneyPresenter()
        {
            _moneyPresneter = null;
        }
    }
}
