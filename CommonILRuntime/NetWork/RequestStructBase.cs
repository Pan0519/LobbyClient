using Network;

namespace Slot.Network.RequestStruct
{
    public class EnterGame : GameRequestBase
    {
        public string RoomID;
    }

    public class SpinBaseRequest : GameRequestBase
    {
        public int State;
    }

    public class NGSpin : SpinBaseRequest
    {
        public int BetIndex;
    }

    public class BGSpinRequest : SpinBaseRequest
    {
        public int Index;//bonus reel index
    }

    public class FGSpinRequest : SpinBaseRequest
    {
        public int Index;//FG Index
    }
}
