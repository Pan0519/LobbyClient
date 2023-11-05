using UniRx;

namespace CommonPresenter
{
    public class HighRollerVaultData
    {
        public Subject<VaultData> getVaultDataSub { get; private set; } = new Subject<VaultData>();
        public Subject<bool> isShowVault { get; private set; } = new Subject<bool>();
        public Subject<ulong> vaultReturnToPaySub { get; private set; } = new Subject<ulong>();

        public void setVaultData(VaultData data)
        {
            getVaultDataSub.OnNext(data);
        }

        public void openVault(bool isOpen)
        {
            isShowVault.OnNext(isOpen);
        }

        public void updateVaultReturnToPay(ulong pay)
        {
            vaultReturnToPaySub.OnNext(pay);
        }
    }

    public class VaultData
    {
        public string expireTime;
        public string lastBillingAt;
        public ulong returnToPay;
    }
}
