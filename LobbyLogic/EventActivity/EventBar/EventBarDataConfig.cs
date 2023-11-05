using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventActivity
{
    public static class EventBarDataConfig
    {
        public static Dictionary<ActivityID, ActivityIconData> activityObjData = new Dictionary<ActivityID, ActivityIconData>()
        {
            {ActivityID.Rookie, new ActivityIconData(){
                spriteName = "casino_crush",
                prefabPath = $"{ActivityDataStore.CommonPrefabPath}activity_item_ticket",
                rewardSpriteName = "ticket"}},
            {ActivityID.FarmBlast,new ActivityIconData(){
                spriteName = "farm_blast",
                prefabPath = $"{ActivityDataStore.CommonPrefabPath}activity_item_ticket",
                rewardSpriteName = "ticket",
                boosterSpriteName = "ticket_booster",
            }},
            {ActivityID.FrenzyJourney,new ActivityIconData(){
                spriteName = "frenzy_journey",
                prefabPath = $"{ActivityDataStore.CommonPrefabPath}activity_item_dice",
                rewardSpriteName = "dice",
                boosterSpriteName = "dice_booster"
            }},
            {ActivityID.MagicForest,new ActivityIconData(){
                spriteName = "magic_forest",
                prefabPath = $"{ActivityDataStore.CommonPrefabPath}activity_item_magnifier",
                rewardSpriteName = "magnifier",
                boosterSpriteName = "golden_mallet"
            }},
        };
    }
    public class ActivityIconData
    {
        public ActivityID activityID;
        public string spriteName;
        public string prefabPath;
        public string rewardSpriteName = string.Empty;
        public string boosterSpriteName = string.Empty;
    }
}
