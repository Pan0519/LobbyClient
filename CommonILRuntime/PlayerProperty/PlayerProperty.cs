using CommonService;
using System;
using UniRx;

namespace CommonILRuntime.PlayerProp
{
    /// <summary>
    /// 泛型Template的 function 使用 protect 包裝，因不同Domain的IL呼叫會無法正確找到泛型對應的function name)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PlayerProperty<T> where T : IPropertyStruct
    {
        protected T currentValue { get; private set; }
        protected T commitedValue { get; private set; }

        protected PlayerProperty(T t)
        {
            currentValue = t;
            commitedValue = t;
        }

        /// <summary>
        /// 確保此commit是最新版
        /// </summary>
        /// <param name="value"></param>
        protected bool commit(T value)
        {
            if (value.getRevision() > commitedValue.getRevision())
            {
                commitedValue = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 將目前最新commit的值套用
        /// </summary>
        protected bool apply()
        {
            if (commitedValue.getRevision() != currentValue.getRevision())
            {
                currentValue = commitedValue;
                return true;
            }
            return false;
        }
    }

    public class PlayerWallet// : PlayerProperty<Wallet>
    {
        protected Wallet currentValue { get; private set; }
        protected Wallet commitedValue { get; private set; }

        private Subject<string> subject { get; } = new Subject<string>();

        //public PlayerWallet(Wallet value) : base(value) { }
        public PlayerWallet(Wallet value)
        {
            currentValue = value;
            commitedValue = value;
        }

        public ulong coin { get { return commitedValue.getCoin(); } }
        public ulong deprecatedCoin { get { return currentValue.getCoin(); } }

        //不同IL間互相調用無法使用泛型，故多包一層使其可以正確對應function
        public bool commit(Wallet value)
        {
            if (value.revision > commitedValue.revision)
            {
                commitedValue = value;
                return true;
            }
            return false;
        }

        bool apply()
        {
            if (commitedValue.revision != currentValue.revision)
            {
                currentValue = commitedValue;
                return true;
            }
            return false;
        }

        public void commitAndPush(Wallet value)
        {
            commit(value);
            refresh();
        }

        public void refresh()
        {
            bool valueChanged = apply();
            if (valueChanged)
            {
                notifyCoinChange();
            }
        }

        void notifyCoinChange()
        {
            subject.OnNext(commitedValue.coin.ToString("N0"));
        }

        public IDisposable subscribeCoinChange(Action<string> handler)
        {
            var disposable = subject.Subscribe(handler);
            return disposable;
        }

        /// <summary>
        /// 未經過 revision check 直接加值
        /// </summary>
        /// <param name="value"></param>
        public void unsafeAdd(ulong value)
        {
            commitedValue.coin += value;
            currentValue.coin = commitedValue.coin;
            notifyCoinChange();
        }

        public void unsafeAddWithoutNotifyChange(ulong value)
        {
            commitedValue.coin += value;
            currentValue.coin = commitedValue.coin;
        }

        /// <summary>
        /// 未經過 revision check 直接給最終值
        /// </summary>
        /// <param name="value"></param>
        public void unsafeSet(ulong value)
        {
            commitedValue.coin = value;
            currentValue.coin = commitedValue.coin;
            notifyCoinChange();
        }
        public void unsafeSetWithoutNotifyChange(ulong value)
        {
            commitedValue.coin = value;
            currentValue.coin = commitedValue.coin;
        }

        public void forceNotify()
        {
            notifyCoinChange();
        }
    }

    public class PlayerVip// : PlayerProperty<VipInfo>
    {
        protected VipInfo currentValue { get; private set; }
        protected VipInfo commitedValue { get; private set; }

        //public PlayerVip(VipInfo value) : base(value) { }
        public PlayerVip(VipInfo value)
        {
            currentValue = value;
            commitedValue = value;
        }
        public Subject<int> subject { get; private set; } = new Subject<int>();

        public VipInfo info { get { return commitedValue; } }
        public VipInfo deprecatedInfo { get { return currentValue; } }

        //不同IL間互相調用無法使用泛型，故多包一層使其可以正確對應function
        public new bool commit(VipInfo value)
        {
            //return base.commit(value);
            if (value.revision > commitedValue.revision)
            {
                commitedValue = value;
                return true;
            }
            return false;
        }

        bool apply()
        {
            if (commitedValue.revision != currentValue.revision)
            {
                currentValue = commitedValue;
                return true;
            }
            return false;
        }

        public void commitAndPush(VipInfo value)
        {
            //base.commit(value);
            commit(value);
            refresh();
        }

        public void refresh()
        {
            bool levelChanged = currentValue.level != commitedValue.level;
            bool valueChanged = apply();
            if (valueChanged)
            {
                if (levelChanged)
                {
                    subject.OnNext(commitedValue.level);
                }
            }
        }

        public IDisposable subscribeLevelChange(Action<int> handler)
        {
            var disposable = subject.Subscribe(handler);
            return disposable;
        }
    }
}
