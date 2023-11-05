namespace Lobby.Jigsaw.wild
{
    public class WildPiece : Piece
    {
        protected override void onChangeData(JigsawPieceData data)
        {
            base.onChangeData(data);
            notGetObj.setActiveWhenChange(!data.collectted);    //只有Wild選擇器中的 Piece 會出現位獲取的物件提示
        }
    }
}
