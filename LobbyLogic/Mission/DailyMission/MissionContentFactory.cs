
using CommonILRuntime.Extension;
using Game.Common;

namespace Mission
{
    public class MissionContentFactory
    {
        const string winWindowsTypeMission = "cumulative-award-board-times";
        const string winWindowsFormat = "{0} WIN";
        const decimal bigWinCondition = 5;
        const decimal megaWinCondition = 10;
        const decimal epicWinCondition = 30;
        const decimal massiveWinCondition = 50;
        const decimal ultimateWinCondition = 100;

        public string createContentMsg(MissionProgressData progressData)
        {
            string format = LanguageService.instance.getLanguageValue(progressData.contentKey);

            if (checkIsWinWindowsTypeCondition(progressData.contentKey))
            {
                return convertWinWindowsTypeCondition(format, progressData.condition);
            }

            return convertCondition(format, progressData.condition);
        }

        bool checkIsWinWindowsTypeCondition(string contentKey)
        {
            contentKey = contentKey.ToLower();
            return contentKey.Contains(winWindowsTypeMission);
        }

        string convertWinWindowsTypeCondition(string msgFormat, decimal[] conditions)
        {
            string winTypeMsg = getWinTypeMsg(conditions[1]);
            ulong reward = (ulong)conditions[0];
            return string.Format(msgFormat, winTypeMsg, reward.convertToCurrencyUnit(3, false));
        }

        string getWinTypeMsg(decimal condition)
        {
            string result = string.Empty;

            switch (condition)
            {
                case bigWinCondition:
                    result = WinWindowPresenter.WinLevels.big.ToString();
                    break;
                case megaWinCondition:
                    result = WinWindowPresenter.WinLevels.mega.ToString();
                    break;
                case epicWinCondition:
                    result = WinWindowPresenter.WinLevels.epic.ToString();
                    break;
                case massiveWinCondition:
                    result = WinWindowPresenter.WinLevels.massive.ToString();
                    break;
                case ultimateWinCondition:
                    result = WinWindowPresenter.WinLevels.ultimate.ToString();
                    break;
            }

            return string.Format(winWindowsFormat, result.ToUpper());
        }

        string convertCondition(string msgFormat, decimal[] conditions)
        {
            var conditionMsg = convertToConditionsMsg(conditions);
            return string.Format(msgFormat, conditionMsg);
        }

        string[] convertToConditionsMsg(decimal[] condition)
        {
            int count = condition.Length;
            string[] result = new string[count];
            ulong reward = 0;

            for (int i = 0; i < count; ++i)
            {
                reward = (ulong)condition[i];
                result[i] = reward.convertToCurrencyUnit(3, false);
            }

            return result;
        }
    }
}
