using Network;
using System.Collections.Generic;

namespace Services
{
    public static class ErrorCodeMsgService
    {
        static List<Result> LoginAgain = new List<Result>() { Result.EnterGameError, Result.FinPlayerError, Result.SessionError, Result.PlayerStateError };
        static List<Result> GameError = new List<Result>
        { Result.NGSpinError , Result.NGEndError, Result.FGSpinError, Result.FGEndError,Result.BGSpinError,Result.BGSpinIdxError,
          Result.BGEndErrir,Result.SFGSpinError ,Result.SFGEndError,Result.JPSpinError,Result.JPEndError,Result.MiniSpinError,
          Result.MiniEndError,Result.HighRollerAccessExpiredError};

        static List<Result> GameLoginAgain = new List<Result> { Result.MsgpackEncodeError, Result.MsgpackDecodeError, Result.MsgpackEncodeForHexError, Result.MsgpackDecodeFromHexError };
        static List<Result> SystemError = new List<Result> { Result.TwoPacketAtOnce, Result.ServerError, Result.SystemSessionError };
        static List<Result> SystemMaintenance = new List<Result>() { Result.SystemMaintenance };
        public static void registerCommonErrorMSg()
        {
            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "Err_Player",
                contentKey = "Err_LoginAgain",
                confirmCB = UtilServices.reloadLobbyScene,
            }, LoginAgain.ToArray());

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "Err_Game",
                contentKey = "Err_ToLobby",
                confirmCB = UtilServices.backToLobby,
            }, GameError.ToArray());

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "Err_Msgpack",
                contentKey = "Err_LoginAgain",
                confirmCB = UtilServices.reloadLobbyScene,
            }, GameLoginAgain.ToArray());


            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "Err_System",
                contentKey = "Err_LoginAgain",
                confirmCB = UtilServices.reloadLobbyScene,
            }, SystemError.ToArray());

            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "Err_SystemMaintenance",
                contentKey = "Err_ErrCodeOnly",
                confirmCB = UtilServices.reloadLobbyScene,
            }, SystemMaintenance.ToArray());
        }
    }
}
