using System;
namespace Game.Slot
{
    public interface ISlotGameTable
    {
        int listenScatterScrollIdx { get; set; }    //已出現兩個 Scatter 的 scroll column idx
        bool isJPListen { get; set; }               //jp是否聽牌
        void onDestroy();
        void preSpin();
        void spin();
        void stop();
        void spinEnd();
        void showTable(ulong[][] symbolDatas);
        void closeTable();
        void applyForAllShowingItem(Action<IGameSlotItem> customAction);
        void registerTableRollEnd(Action onRollEndCallBack);
        void unRegisterTableRollEnd(Action onRollEndCallBack);
        void registerTableSpinEnd(Action handler);
        void unRegisterTableSpinEnd(Action handler);
        void addJPScrollAnimatedSymbol(int rowIdx, string trigger);
        void addScrollAnimatedSymbol(int columnIdx, int rowIdx, string trigger);

    }
}
