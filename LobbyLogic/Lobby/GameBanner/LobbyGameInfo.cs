using UniRx;
using CommonService;
using System;
using Services;
using Randon = UnityEngine.Random;
using System.Threading.Tasks;
using Debug = UnityLogUtility.Debug;

public enum GameState : int
{
    None,
    FreeGame,
    Hot,
    New,
}

public class LobbyGameInfo
{

    public int unLockLv { get; private set; }

    public bool isLock { get { return DataStore.getInstance.playerInfo.level < unLockLv; } }

    public GameState gameState { get; private set; }
    public Subject<GameState> gameStateSubject = new Subject<GameState>();

    public bool isOpen
    {
        get
        {
            return gameInfo.open;
        }
    }
    public string gameID { get { return gameInfo.id; } }
    public string gameName { get { return gameInfo.name; } }
    public string languageName { get { return gameInfo.name_cht; } }
    public long jackpotMultiplier { get { return gameInfo.jackpotMultiplier; } }

    GameInfo gameInfo;

    public long getMaxJP(long initJP)
    {
        return initJP + (long)(initJP * 0.01f);
    }

    public async Task<long> getInitJP()
    {
        if (gameInfo.jackpotMultiplier <= 0)
        {
            return gameInfo.jackpotMultiplier;
        }

        var dataInfo = DataStore.getInstance.dataInfo;
        long maxJP = 0;
        if (!DataStore.getInstance.playerInfo.hasHighRollerPermission)
        {
            maxJP = await dataInfo.getRegularMaxJP(gameID);
        }
        else
        {
            maxJP = await dataInfo.getHighRollerMaxJP(gameID);
        }
        float rangeJP = (Randon.Range(1, 10) * 0.01f) + 1;
        var jpValue = maxJP * gameInfo.jackpotMultiplier;
        var miniJPValue = (long)(jpValue * 1.01f);
        var initJPValue = Math.Max((long)(jpValue * rangeJP), miniJPValue);
        return initJPValue;
    }
    public void setGameInfo(GameInfo info)
    {
        gameInfo = info;
        unLockLv = info.requiredLevel;
        GameState infoGameState = GameState.None;

        if (null != info.tags)
        {
            for (int i = 0; i < info.tags.Length; ++i)
            {
                if (UtilServices.enumParse(info.tags[i], out infoGameState))
                {
                    if (GameState.Hot == infoGameState)
                    {
                        break;
                    }
                }
            }
        }

        setGameState(infoGameState);
    }

    void setGameState(GameState gameState)
    {
        this.gameState = gameState;
        gameStateSubject.OnNext(this.gameState);
    }
}

