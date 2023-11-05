using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LocalNotification;

public class LocalNotificationManager
{
    static LocalNotificationManager _instance = null;

    public static LocalNotificationManager getInstance
    {
        get
        {
            if (null == _instance)
            {
                _instance = new LocalNotificationManager();
            }
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }
    public const string ChannelId = "game_channel0";
    public const string SchedulerChannelId = "scheduler_channel1";
    public const string NewsChannelId = "news_channel2";
    public const string LocalSaveKey = "Notification";
    public const string goldenSaveKey = "GoldenBoxNotifiaction";
    public const string NotificationTitle = "Notification_Title_";
    public const string NotificationBody = "Notification_Content_";
    bool isNotificationOn = true;
    GameNotificationsManager gameNotifictionMng = null;
    public void init()
    {
        if (null == gameNotifictionMng)
        {
            var notifition = new GameObject();
            notifition.name = "GameNotifictionManager";
            gameNotifictionMng = notifition.AddComponent<GameNotificationsManager>();
            StartInit();
            DontDestroyRoot.addChild(notifition.transform);

            if (PlayerPrefs.HasKey(LocalSaveKey))
            {
                isNotificationOn = PlayerPrefs.GetInt(LocalSaveKey) == 1 ? true : false;
            }
            else
            {
                PlayerPrefs.SetInt(LocalSaveKey, 1);
                isNotificationOn = true;
            }
        }
    }

    public void StartInit()
    {
        var c1 = new GameNotificationChannel(ChannelId, "Default Game Channel", "Generic notifications", GameNotificationChannel.NotificationStyle.Popup, highPriority:true);
        var c2 = new GameNotificationChannel(NewsChannelId, "News Channel", "News feed notifications");
        var c3 = new GameNotificationChannel(SchedulerChannelId, "Scheduler Channel", "Scheduler notifications");
        gameNotifictionMng.Initialize(c1, c2, c3);
    }

    public void setLocalSaveKey(bool isOn)
    {
        if (PlayerPrefs.HasKey(LocalSaveKey))
        {
            PlayerPrefs.SetInt(LocalSaveKey , isOn?0:1);
            isNotificationOn = !isOn;

            if (isNotificationOn)
            {
                reschedulerNotification();
            }
            else
            {
                clearAllNotifictions();
            }
        }
    }

    public bool getLocalKeyState()
    {
        return PlayerPrefs.GetInt(LocalSaveKey, 1) == 0;
    }

    public void reschedulerNotification()
    {
        clearAllNotifictions();

        if (PlayerPrefs.HasKey(goldenSaveKey))
        {
            addGoldenBoxNotifiction(PlayerPrefs.GetString(goldenSaveKey));
        }

        addEverdayScheduler();
    }

    public void addGoldenBoxNotifiction(string tmpDate = "")
    {
        List<string> hintKeys = new List<string>() {"1", "2", "3" };
        var rand = new System.Random();
        int rand_i = rand.Next(hintKeys.Count);
        string title = $"{NotificationTitle}{hintKeys[rand_i]}";
        string body = $"{NotificationBody}{hintKeys[rand_i]}";
        title = LanguageService.instance.getLanguageValue(title);
        body = LanguageService.instance.getLanguageValue(body);
        DateTime deliveryTime = (tmpDate == ""? DateTime.Now.ToLocalTime():DateTime.Parse(tmpDate)) + TimeSpan.FromHours(3);
        var iconSmall = "icon_small_256";
        var iconLarge = "icon_large_";

        SendNotification(title, body, deliveryTime, smallIcon: iconSmall, largeIcon: iconLarge , badgeNumber:0);

        PlayerPrefs.SetString(goldenSaveKey, deliveryTime.ToString());
    }

    public async void addEverdayScheduler()
    {
        /*
        for (int i = 17; i < 20; i++)
        {
            for (int j = 0; j < 11; j++)
            {
                await sendNotificationsEveryday(i, j*5, 0, new List<string>() { "9", "10", "11", "12", "13" }, 1);
            }   
        }*/

        //12:05
        await sendNotificationsEveryday(12,5,0, new List<string>() { "4", "5", "6", "7", "8" },7);
        //18:30
        await sendNotificationsEveryday(18,30,0, new List<string>() { "9", "10", "11", "12", "13" },7);
        //21:05
        await sendNotificationsEveryday(21,5,0, new List<string>() { "14", "15", "16", "17"},7);

    }

    public async Task sendNotificationsEveryday(int hour,int minute,int second,List<string> hintKeys,int countDay = 1,string iconSmall = "icon_small_256",string iconLarge = "icon_large_")
    {
        DateTime date = DateTime.Now.ToLocalTime();
        DateTime sDate = new DateTime(date.Year, date.Month, date.Day , hour,minute,second);
        string title = "";
        string body = "";
        int rand_i = 0;
        var rand = new System.Random();
        for (int i = 0; i < countDay; i++)
        {
            rand_i = rand.Next(hintKeys.Count);
            title = $"{NotificationTitle}{hintKeys[rand_i]}";
            body = $"{NotificationBody}{hintKeys[rand_i]}";
            title = LanguageService.instance.getLanguageValue(title);
            body = LanguageService.instance.getLanguageValue(body);
            SendNotification(title, body, sDate, smallIcon: iconSmall, largeIcon: iconLarge, badgeNumber: 0);
            sDate = sDate.AddDays(1);
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
        }

    }



    /// <summary>
    /// Queue a notification with the given parameters.
    /// </summary>
    /// <param name="title">The title for the notification.</param>
    /// <param name="body">The body text for the notification.</param>
    /// <param name="deliveryTime">The time to deliver the notification.</param>
    /// <param name="badgeNumber">The optional badge number to display on the application icon.</param>
    /// <param name="reschedule">
    /// Whether to reschedule the notification if foregrounding and the notification hasn't yet been shown.
    /// </param>
    /// <param name="channelId">Channel ID to use. If this is null/empty then it will use the default ID. For Android
    /// the channel must be registered in <see cref="GameNotificationsManager.Initialize"/>.</param>
    /// <param name="smallIcon">Notification small icon.</param>
    /// <param name="largeIcon">Notification large icon.</param>
    public void SendNotification(string title, string body, DateTime deliveryTime, int? badgeNumber = null,
        bool reschedule = false, string channelId = null,
        string smallIcon = null, string largeIcon = null)
    {
        if (!isNotificationOn) return;

        //Util.Log($"SendNotification_1:{deliveryTime}__{title}");
        if (deliveryTime < DateTime.Now)
        {
            //Util.Log($"SendNotification_2:{deliveryTime} < Date.now { DateTime.Now}");
            return;
        }
        IGameNotification notification = gameNotifictionMng.CreateNotification();
        //Util.Log($"SendNotification_2:{body}");
        if (notification == null)
        {
            return;
        }
        //Util.Log($"SendNotification_3:{deliveryTime}");
        notification.Title = title;
        //Util.Log($"SendNotification_3_1:{title}");
        notification.Body = body;
        //Util.Log($"SendNotification_3_2:{body}");
        notification.Group = !string.IsNullOrEmpty(channelId) ? channelId : ChannelId;
        //Util.Log($"SendNotification_3_3:{ notification.Group}");
        notification.DeliveryTime = deliveryTime;
        //Util.Log($"SendNotification_3_4:{ notification.DeliveryTime}");
        notification.SmallIcon = smallIcon;
        //Util.Log($"SendNotification_3_5:{ smallIcon}");
        notification.LargeIcon = largeIcon;
        //Util.Log($"SendNotification_3_6:{ largeIcon}");
        //notification.BadgeNumber = 0;
        
        if (badgeNumber != null)
        {
            //Util.Log($"SendNotification_3_7:{ badgeNumber}");
            notification.BadgeNumber = badgeNumber;
            //Util.Log($"SendNotification_3_8:{ notification.BadgeNumber}");
        }

        int id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode());
        notification.Id = id;

        gameNotifictionMng.Platform.ScheduleNotification(notification);

        //Util.Log($"SendNotification_4:{smallIcon}");
        //PendingNotification notificationToDisplay = gameNotifictionMng.ScheduleNotification(notification);
        //notificationToDisplay.Reschedule = reschedule;
        //Util.Log($"SendNotification_5:{largeIcon}");
        //updatePendingNotifications = true;

        //QueueEvent($"Queued event with ID \"{notification.Id}\" at time {deliveryTime:HH:mm}");


    }

    public void clearAllNotifictions()
    {
        gameNotifictionMng.CancelAllNotifications();
    }

}
