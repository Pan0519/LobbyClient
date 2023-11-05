using CommonILRuntime.Game.Slot;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Game.Slot.SlotDefine;

namespace Game.Slot.Interface
{
    public interface ISlotScroll
    {
        bool show { get; set; }     ///開關輪帶
        //bool isReceiveSpinResult;//是否收到spin data，收到才能將盤面停下開獎
        int getScrollIdx();         ///取得此輪帶索引值
        //List<T> getShowItems();     ///取得目前輪帶顯示的所有 ISlotItem 物件 (由上而下)

        /// 註冊輪帶轉動停止事件，使用完記得反註冊
        void registerScrollEnd(Action<ISlotScroll> handler);
        void unRegisterScrollEnd(Action<ISlotScroll> handler);

        /// 註冊輪帶完整轉動且停止事件 ( 未被介入中斷 )
        void registerScrollComplete(Action<ISlotScroll> handler);
        void unRegisterScrollComplete(Action<ISlotScroll> handler);

        /// <summary>
        /// 初始輪帶
        /// </summary>
        /// <param name="showCount"></param> 顯示幾個
        /// <param name="defaultReels"></param> 預設輪帶
        /// <param name="scrollIdx"></param> 輪帶Idx (會影響啟動停輪時間差)
        /// <param name="itemPrefab"></param> SlotItem 的生成 Prefab
        /// <param name="bindingDelegate"></param>  綁定 SlotItem 的 delagate
        /// <param name="initShowReelIdx"></param>  預設輪帶起始位置
        void initScroll(int showCount, int[] defaultReels, int scrollIdx, GameObject itemPrefab, Func<GameObject, ISlotItem> bindingDelegate, int initShowReelIdx = 0);

        void roll(ulong[][] inputRollResult, AutoMode stopMode = AutoMode.INFINITY_AUTO,bool isPreScroll = true);    /// 啟動轉輪
        void manualStop();                          ///手動停輪
        void addConstantTime(float addTime);        ///延長轉動時間   (ex: 聽牌表演)
        void setSpeedMultiple(float value);         ///輪帶加速 (ex: 聽牌表演)
        void setReels(List<ReelStrip> reelStrips);  ///直接設定結果盤面 (可用於轉換遊戲設定輪帶起始盤面)
        
        void changeDefaultReels(int[] defaultReels);   ///Change Default Reel
        List<int> getShowItemsIdx();                ///取得顯示中的Slotems Idx

        ISlotConfigProvider config { get; set; }
    }
}
