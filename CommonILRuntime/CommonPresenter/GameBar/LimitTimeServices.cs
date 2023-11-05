using System;
using UnityEngine;
using UniRx;
using Services;
using CommonService;

namespace CommonPresenter
{
    public class LimitTimeServices
    {
        public Subject<DateTime> limitEndTimeSub = new Subject<DateTime>();
        string LimitEndTimeKey = "LimitEndTime";
        string LimitDataKey = "HaveLimitData";

        public void setHasLimitData(bool isHaveLimitData)
        {
            PlayerPrefs.SetInt(LimitDataKey, isHaveLimitData ? 1 : 0);
        }

        public DateTime getLimitEndTime()
        {
            bool haveLimitData = PlayerPrefs.GetInt(LimitDataKey) == 1;
            if (GuideStatus.Completed != DataStore.getInstance.guideServices.nowStatus || !haveLimitData)
            {
                return UtilServices.nowTime;
            }

            if (!PlayerPrefs.HasKey(LimitEndTimeKey))
            {
                saveLimitEndTime();
            }

            var timeStr = PlayerPrefs.GetString(LimitEndTimeKey);
            DateTime resultTime = UtilServices.strConvertToDateTime(timeStr, UtilServices.nowTime);
            if (resultTime <= UtilServices.nowTime)
            {
                saveLimitEndTime();
                return UtilServices.nowTime;
            }

            if (resultTime.Subtract(UtilServices.nowTime).TotalHours > 4)
            {
                saveLimitEndTime();
                return UtilServices.nowTime;
            }
            return resultTime;
        }

        void saveLimitEndTime()
        {
            DateTime endTime = UtilServices.nowTime.AddHours(4);
            limitEndTimeSub.OnNext(endTime);
            string saveTime = string.Format("{0:u}", endTime);
            PlayerPrefs.SetString(LimitEndTimeKey, saveTime);
        }

        public void limitSaleTimeFinish()
        {
            PlayerPrefs.DeleteKey(LimitEndTimeKey);
        }

        public void limitSaleFinish()
        {
            limitSaleTimeFinish();
            limitEndTimeSub.OnNext(UtilServices.nowTime);
        }
    }
}
