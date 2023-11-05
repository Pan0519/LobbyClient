using System.Collections.Generic;

namespace Lobby.ActivityUI.Audio
{
    public static class PopupUISoundPathProvider
    {
        private static readonly IReadOnlyDictionary<ActivityUIAudio, string> audios = new Dictionary<ActivityUIAudio, string>()
        {
            { ActivityUIAudio.PopUp         ,"popup" },
            //{ ActivityUIAudio.ChangePage    ,"changePage" }   //尚無音效檔
        };

        public static string GetAudioPath(ActivityUIAudio audio)
        {
            return $"Basic/sound@{audios[audio]}";
        }
    }
    public enum ActivityUIAudio
    {
        PopUp,
        //ChangePage
    }
}
