using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using EventActivity;
using Event.Common;
using System.Globalization;
using Services;

namespace FarmBlast
{
    class JpBoardNodePresenter : NodePresenter
    {
        Text text_Grand_Award;
        GameObject[] obj_Grand_gets = new GameObject[5];

        Text text_Major_Award;
        GameObject[] obj_Major_gets = new GameObject[5];

        Text text_Minor_Award;
        GameObject[] obj_Minor_gets = new GameObject[5];

        Text text_Mini_Award;
        GameObject[] obj_Mini_gets = new GameObject[4];

        Dictionary<string, GameObject[]> jpBoardObjs;

        GameObject getJPTargetObj;
        string rewardJpKey;

        Dictionary<string, ulong> rewardDict = new Dictionary<string, ulong>();
        public override void initUIs()
        {
            text_Grand_Award = getTextData("text_Grand_Award");
            for (int wCount = 0; wCount < obj_Grand_gets.Length; wCount++)
            {
                obj_Grand_gets[wCount] = getGameObjectData($"obj_Grand_get_{wCount}");
            }
            text_Major_Award = getTextData("text_Major_Award");
            for (int wCount = 0; wCount < obj_Major_gets.Length; wCount++)
            {
                obj_Major_gets[wCount] = getGameObjectData($"obj_Major_get_{wCount}");
            }

            text_Minor_Award = getTextData("text_Minor_Award");
            for (int wCount = 0; wCount < obj_Minor_gets.Length; wCount++)
            {
                obj_Minor_gets[wCount] = getGameObjectData($"obj_Minor_get_{wCount}");
            }

            text_Mini_Award = getTextData("text_Mini_Award");
            for (int wCount = 0; wCount < obj_Mini_gets.Length; wCount++)
            {
                obj_Mini_gets[wCount] = getGameObjectData($"obj_Mini_get_{wCount}");
            }

            jpBoardObjs = new Dictionary<string, GameObject[]>()
            {
                { ActivityDataStore.GrandKey,obj_Grand_gets},
                { ActivityDataStore.MajorKey,obj_Major_gets},
                { ActivityDataStore.MinorKey,obj_Minor_gets},
                { ActivityDataStore.MiniKey,obj_Mini_gets},
            };
        }

        public Transform getAwardJPObj(string key, long targetID)
        {
            GameObject[] jpObjs;
            rewardJpKey = key.toTitleCase();
            if (jpBoardObjs.TryGetValue(rewardJpKey, out jpObjs))
            {
                getJPTargetObj = jpObjs[targetID];
                Debug.Log($"getJPTargetObj {getJPTargetObj.name}");
                return getJPTargetObj.transform;
            }
            Debug.LogError($"get {key} award jp obj is null");
            return null;
        }

        public void addJPCollect()
        {
            getJPTargetObj.setActiveWhenChange(true);
            Debug.Log($"addJPCollect {getJPTargetObj.name}");
            getJPTargetObj = null;
            checkJpCollectFinish();
        }

        void checkJpCollectFinish()
        {
            GameObject[] jpObjs = jpBoardObjs[rewardJpKey];
            bool isCollectFinish = true;
            for (int i = 0; i < jpObjs.Length; ++i)
            {
                if (!jpObjs[i].activeSelf)
                {
                    isCollectFinish = false;
                    break;
                }
            }

            if (!isCollectFinish)
            {
                return;
            }
            ulong rewardValue = 0;
            if (rewardDict.TryGetValue(rewardJpKey, out rewardValue))
            {
                UiManager.getPresenter<ActivityJPRewardPresenter>().openAward(rewardJpKey, rewardValue);
            }
            resetStatus(jpObjs);
        }

        public override void init()
        {
            resetAllStatus();
        }

        public void setIniData(Dictionary<string, ulong> reward, Dictionary<string, bool[]> status)
        {
            setStatus(obj_Grand_gets, status[ActivityDataStore.GrandKey]);
            setStatus(obj_Major_gets, status[ActivityDataStore.MajorKey]);
            setStatus(obj_Minor_gets, status[ActivityDataStore.MinorKey]);
            setStatus(obj_Mini_gets, status[ActivityDataStore.MiniKey]);

            rewardDict = reward;

            text_Grand_Award.text = getAwardDictValueStr(ActivityDataStore.GrandKey);
            text_Major_Award.text = getAwardDictValueStr(ActivityDataStore.MajorKey);
            text_Minor_Award.text = getAwardDictValueStr(ActivityDataStore.MinorKey);
            text_Mini_Award.text = getAwardDictValueStr(ActivityDataStore.MiniKey);
        }

        string getAwardDictValueStr(string key)
        {
            ulong result = 0;
            if (!rewardDict.TryGetValue(key.toTitleCase(), out result))
            {
                Debug.LogError($"get jp reward {key} is empty");
                return string.Empty;
            }

            return result.ToString("N0");
        }
        private void setStatus(GameObject[] target, bool[] status)
        {
            if (target.Length != status.Length)
            {
                Debug.LogError("data count not match");
                return;
            }

            for (int tCount = 0; tCount < target.Length; tCount++)
            {
                target[tCount].setActiveWhenChange(status[tCount]);
            }
        }

        private void resetAllStatus()
        {
            resetStatus(obj_Grand_gets);
            resetStatus(obj_Major_gets);
            resetStatus(obj_Minor_gets);
            resetStatus(obj_Mini_gets);
        }

        private void resetStatus(GameObject[] target)
        {
            for (int i = 0; i < target.Length; ++i)
            {
                target[i].setActiveWhenChange(false);
            }
        }
    }
}
