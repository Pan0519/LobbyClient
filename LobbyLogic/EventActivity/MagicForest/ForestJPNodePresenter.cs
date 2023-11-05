using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EventActivity;
using Services;

namespace MagicForest
{
    public class JPBoardNode : NodePresenter
    {
        Dictionary<string, JPNode> jpNodeDicts = new Dictionary<string, JPNode>();

        Transform originalParent;
        public override void init()
        {
            jpNodeDicts.Add(ActivityDataStore.GrandKey, UiManager.bindNode<JPNode>(getNodeData("jp_grand_node").cachedGameObject));
            jpNodeDicts.Add(ActivityDataStore.MinorKey, UiManager.bindNode<JPNode>(getNodeData("jp_minor_node").cachedGameObject));
            jpNodeDicts.Add(ActivityDataStore.MiniKey, UiManager.bindNode<JPNode>(getNodeData("jp_mini_node").cachedGameObject));
            originalParent = uiTransform.parent;
        }

        public void returnToParent()
        {
            uiTransform.SetParent(originalParent);
            uiTransform.SetAsFirstSibling();
        }

        public void updateJpReward(Dictionary<string, long> jpRewards)
        {
            var rewardsEnum = jpRewards.GetEnumerator();
            while (rewardsEnum.MoveNext())
            {
                if (jpNodeDicts.ContainsKey(rewardsEnum.Current.Key))
                {
                    jpNodeDicts[rewardsEnum.Current.Key].setReward(rewardsEnum.Current.Value);
                }
            }
        }

        public void updateJpCount(Dictionary<string, int> jpCounts)
        {
            var countEnum = jpCounts.GetEnumerator();
            while (countEnum.MoveNext())
            {
                if (jpNodeDicts.ContainsKey(countEnum.Current.Key))
                {
                    jpNodeDicts[countEnum.Current.Key].setGetJPCount(countEnum.Current.Value);
                }
            }
        }

        public void updateJpCount(string jpName, int count)
        {
            jpName = jpName.toTitleCase();
            JPNode jPNode;
            if (jpNodeDicts.TryGetValue(jpName, out jPNode))
            {
                jPNode.setGetJPCount(count);
            }
        }

        public JPNode getJpNode(string jpName)
        {
            jpName = jpName.toTitleCase();
            JPNode jPNode;
            if (jpNodeDicts.TryGetValue(jpName, out jPNode))
            {
                return jPNode;
            }

            return null;
        }
    }

    public class JPNode : NodePresenter
    {
        Text rewardTxt;
        GameObject[] getJpObjs = new GameObject[3];
        int count;
        public override void initUIs()
        {
            rewardTxt = getTextData("text_reward");
            for (int i = 0; i < getJpObjs.Length; ++i)
            {
                getJpObjs[i] = getGameObjectData($"get_jp_{i + 1}");
            }
        }

        public override void init()
        {
            setGetJPCount(0);
        }

        public void setGetJPCount(int count)
        {
            if (count >= getJpObjs.Length)
            {
                count = 0;
            }
            this.count = count;
            for (int i = 0; i < getJpObjs.Length; ++i)
            {
                getJpObjs[i].setActiveWhenChange((i + 1) <= count);
            }
        }

        public GameObject getAddJPRect()
        {
            int getNextId = count + 1;
            if (getNextId <= getJpObjs.Length)
            {
                return getJpObjs[getNextId - 1];
            }
            return null;
        }

        public void setReward(long reward)
        {
            rewardTxt.text = reward.ToString("N0");
        }
    }
}
