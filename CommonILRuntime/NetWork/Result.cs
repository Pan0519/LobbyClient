
namespace Network
{
    public enum Result : int
    {
        NetError = -1,
        OK = 0,

        EnterGameError = 1010,
        FinPlayerError = 1020,
        SessionError = 1021, //遊戲Session Error
        PlayerStateError = 1030,

        NGSpinError = 2010,
        NGEndError = 2020,
        FGSpinError = 2030,
        FGEndError = 2040,
        BGSpinError = 2050,
        BGSpinIdxError = 2051,
        BGEndErrir = 2060,
        SFGSpinError = 2070,        //招財貓用
        SFGEndError = 2080,         //招財貓用
        JPSpinError = 2090,
        BetError = 2011,
        JPEndError = 2100,
        MiniSpinError = 2110,
        MiniEndError = 2120,
        HighRollerAccessExpiredError = 2500,

        MsgpackEncodeError = 5010,
        MsgpackDecodeError = 5011,
        MsgpackEncodeForHexError = 5012,
        MsgpackDecodeFromHexError = 5013,

        #region 平台活動錯誤碼
        ActivityIDError = 6010, //錯誤的活動ID
        ActivityNotAvailableError = 6011,
        ActivitySerialError = 6012,
        ActivityTicketNotEnough = 6013,
        ActivityIsEnd = 6020,   //活動已結束
        ActivityClickRepeat = 6030, //重複點擊
        ActivityOutRange = 6040,  //超出範圍
        ActivityEmptyTreasure = 6050, //空寶箱
        ActivityOpenTreasureTimeError = 6060, //寶箱未到開啟時間
        ActivityBossError = 6070, //Boss關卡已結束
        ActivityStatueError = 6071, //狀態錯誤
        #endregion
        #region 活動server 錯誤碼
        ActivityIDPromotedError = 6021,
        ActivityServerIsNull = 22300, //沒有活動
        ActivityServerSerialError = 22301, //活動期號錯誤
        ActivityServerIDError = 22302, //活動ID錯誤
        #endregion

        TwoPacketAtOnce = 8888,
        ServerError = 9999,

        SystemSessionError = 11000,//系統SessionError
        SystemMaintenance = 91000,//系統維護中

        #region Mail驗證錯誤碼
        MailAskVerificationsFail = 11220,    //要求驗證碼失敗
        MailIsNotValid = 11221,   //不合法的Mail
        MailVerificationsError = 11222, //驗證碼要求太頻繁
        MailWrongParameter = 11223,          //參數錯誤
        MailRegistered = 11224,         //已驗證過
        MailVerificationProcessError = 11225,//驗證過程有誤
        MailVerificationFail = 11226,    //驗證失敗
        #endregion

        #region FB綁定錯誤
        FBBindingRepeat = 11210,//重複綁定
        #endregion

        #region 手機號驗證錯誤碼
        PhoneAskVerificationsFail = 11230,    //要求驗證碼失敗
        PhoneNumberIsNotValid = 11231,   //不合法的手機號
        PhoneVerificationsError = 11232, //驗證碼要求太頻繁
        PhoneWrongParameter = 11233,          //參數錯誤
        PhoneNumberRegistered = 11234,   //已驗證過
        PhoneVerificationProcessError = 11235,//驗證過程有誤
        PhoneVerificationFail = 11236,         //驗證失敗
        #endregion

        GetWagerEmpty = 13106,
        GetStoreBounsError = 22411, //商城取得獎勵時間錯誤

        NewbieAdventureSkipCode = 18471,
        NewbieDogAlreadyComplete = 18468,//救狗關卡已過關

        #region AlbumSeason
        SeasonNotExits = 24001,
        AlbumRedeemRepeat = 24060,
        #endregion

        ExpError = 90200, //經驗值計算錯誤


    }
}
