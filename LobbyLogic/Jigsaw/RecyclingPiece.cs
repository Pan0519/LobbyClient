using CommonILRuntime.BindingModule;
using System;

namespace Lobby.Jigsaw
{
    public class RecyclingPiece : Piece
    {
        const RareLevel FANTASY_STAR_RARE_LEVEL = RareLevel.BLUE;

        public Action<RecyclingPiece> selectCountChangeListener = null;

        PieceRecycleControl controller;

        public override void initUIs()
        {
            base.initUIs();
        }

        public override void init()
        {
            base.init();
            objRecycleGroup.setActiveWhenChange(true);
            controller = UiManager.bindNode<PieceRecycleControl>(objRecycleGroup);
            controller.onAmontChange = onSelectCountChange;
        }

        //單一片時的星星數
        public int singlePiecelStarCount
        {
            get
            {
                //公式: 稀有度 * 星級
                return data.getRareLevel() * data.getStarLevel();
            }
        }

        //可提供的星星總量
        public int avaliableStarCount { get { return singlePiecelStarCount * data.getCount(); } }

        //目前已選的星星總量
        public int selectedStarCount { get { return singlePiecelStarCount * getSelectCount(); } }

        //單一片時的fantasy星星數
        public bool isFantasy
        {
            get
            {
                return data.getRareLevel() >= (int)RareLevel.BLUE; //稀有度符合條件才算 fantasy star
            }
        }

        //可提供的fantasy星星總量
        public int avaliableFantasyCount { get { return isFantasy ? data.getCount() : 0; } }

        int maxCount;

        protected override void onChangeData(JigsawPieceData data)
        {
            maxCount = data.getCount();
            controller.showFantasyStar(isFantasy);
            controller.setMaxAmount(data.getCount());
        }

        public void setSelectedCount(int count)
        {
            controller.setSelectCount(count);
        }

        public int getSelectCount()
        {
            return controller.currentAmount;
        }

        public void registerSelected()
        {
            registerSelected(piece =>
            {
                controller.onAddAmountClick();
            });
        }

        void onSelectCountChange()
        {
            setSelfBtnInteractable(controller.currentAmount < maxCount);
            selectCountChangeListener?.Invoke(this);
        }
    }
}
