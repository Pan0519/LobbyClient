namespace Lobby.Popup
{
    public class PopUpRichman : PopUpBClassActivity   //功能相同，先使用繼承處理，若有差異需求再獨立出來
    {
        public override string objPath { get { return "prefab/activity/pop_richman"; } }    //TODO: 換成彈窗儲值的 prefab
    }
}