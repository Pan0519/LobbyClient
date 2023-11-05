using Network;
using EventActivity;
using System;
using System.Collections.Generic;

namespace Event.Common
{
    public static class ActivityErrorMsgServices
    {
        static List<Result> ActivityError = new List<Result>()
         { Result.ActivityIDError , Result.ActivityNotAvailableError, Result.ActivitySerialError, Result.ActivityTicketNotEnough,
           Result.ActivityIsEnd,Result.ActivityClickRepeat,Result.ActivityOutRange,Result.ActivityEmptyTreasure ,Result.ActivityOpenTreasureTimeError,
           Result.ActivityBossError,Result.ActivityStatueError,Result.ActivityServerIsNull,Result.ActivityServerIDError,Result.ActivityServerSerialError};

        public static void registerErrorMSg()
        {
            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = "Err_Activity",
                contentKey = "Err_ToGame",
                confirmCB = callActivityError,
            }, ActivityError.ToArray());
        }

        static void callActivityError()
        {
            ActivityDataStore.activityCallErrorComplete(true);
        }
    }
}
