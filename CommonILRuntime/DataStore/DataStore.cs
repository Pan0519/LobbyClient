using Services;
using GameBar;
using CommonPresenter;
using CommonILRuntime.Game.GameTime;
using CommonILRuntime.Services;

namespace CommonService
{
    public class DataStore
    {
        static DataStore _instance = null;

        public static DataStore getInstance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new DataStore();
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public DataInfo dataInfo
        {
            get
            {
                if (null == _dataInfo)
                {
                    _dataInfo = new DataInfo();
                }
                return _dataInfo;
            }
            set { _dataInfo = value; }
        }

        DataInfo _dataInfo = null;

        public PlayerInfo playerInfo
        {
            get
            {
                if (null == _playerInfo)
                {
                    _playerInfo = new PlayerInfo();
                }
                return _playerInfo;
            }
            set { _playerInfo = value; }
        }

        PlayerInfo _playerInfo;

        public GameToLobbyServices gameToLobbyService
        {
            get
            {
                if (null == _toLobbyService)
                {
                    _toLobbyService = new GameToLobbyServices();
                }
                return _toLobbyService;
            }
            set { _toLobbyService = value; }
        }

        GameToLobbyServices _toLobbyService;

        public FuncInGameToLobbyService eventInGameToLobbyService
        {
            get
            {
                if (null == _eventInGameToLobbyService)
                {
                    _eventInGameToLobbyService = new FuncInGameToLobbyService();
                }
                return _eventInGameToLobbyService;
            }
            set { _eventInGameToLobbyService = value; }
        }

        FuncInGameToLobbyService _eventInGameToLobbyService;

        public PlayerMoneyServices playerMoneyPresenter
        {
            get
            {
                if (null == _moneyServices)
                {
                    _moneyServices = new PlayerMoneyServices();
                }
                return _moneyServices;
            }
        }

        PlayerMoneyServices _moneyServices;

        MiniGameConfig _miniGameData;

        public MiniGameConfig miniGameData
        {
            get
            {
                if (null == _miniGameData)
                {
                    _miniGameData = MiniGameConfig.instance;
                }

                return _miniGameData;
            }
        }

        HighRollerVaultData _vaultData;
        public HighRollerVaultData highVaultData
        {
            get
            {
                if (null == _vaultData)
                {
                    _vaultData = new HighRollerVaultData();
                }
                return _vaultData;
            }
        }

        GameTimeManager _gameTimeManager;

        public GameTimeManager gameTimeManager
        {
            get
            {
                if (null == _gameTimeManager)
                {
                    _gameTimeManager = new GameTimeManager();
                }

                return _gameTimeManager;
            }
            set { _gameTimeManager = value; }
        }

        GuideServices _guideServices;

        public GuideServices guideServices
        {
            get
            {
                if (null == _guideServices)
                {
                    _guideServices = new GuideServices();
                }

                return _guideServices;
            }
            set { _guideServices = value; }
        }

        DailyMissionServices _dailyMissionServices;

        public DailyMissionServices dailyMissionServices
        {
            get
            {
                if (null == _dailyMissionServices)
                {
                    _dailyMissionServices = new DailyMissionServices();
                }

                return _dailyMissionServices;
            }
            set { _dailyMissionServices = value; }
        }

        LimitTimeServices _limitTimeServices;
        public LimitTimeServices limitTimeServices
        {
            get
            {
                if (null == _limitTimeServices)
                {
                    _limitTimeServices = new LimitTimeServices();
                }
                return _limitTimeServices;
            }
        }

        ExtraGameServices _extraGameServices;
        public ExtraGameServices extraGameServices
        {
            get
            {
                if (null == _extraGameServices)
                {
                    _extraGameServices = new ExtraGameServices();
                }

                return _extraGameServices;
            }
            set { _extraGameServices = value; }
        }

        LobbyToGameServices _lobbyToGameServices;
        public LobbyToGameServices lobbyToGameServices
        {
            get
            {
                if (null == _lobbyToGameServices)
                {
                    _lobbyToGameServices = new LobbyToGameServices();
                }

                return _lobbyToGameServices;
            }
            set { _lobbyToGameServices = value; }
        }
    }
}
