using Network;
using System.Threading.Tasks;
using LobbyLogic.NetWork.ResponseStruct;
using LobbyLogic.NetWork.RequestStruce;
using CommonService;
using UnityEngine;
using SaveTheDog;

namespace LobbyLogic.NetWork
{
    public class LobbyServer
    {
        string apiME { get { return "/me"; } }
        string apiStroe { get { return "/store"; } }
        string userID { get { return DataStore.getInstance.playerInfo.userID; } }
        ServerProxy proxy;
        public LobbyServer setServerProxy(ServerProxy serverProxy)
        {
            proxy = serverProxy;
            return this;
        }

        #region Login && PlayerInfo
        public Task<LoginResponse> login(string token)
        {
            string method = PlayerPrefs.GetString("LoginType", string.Empty);
            return proxy.callApi<LoginResponse>("/sessions", new LoginRequest()
            {
                token = token,
                deviceId = ApplicationConfig.deviceID,
                method = method
            });
        }

        public Task<DailyRewardResponse> getDailyReward()
        {
            return proxy.callApi<DailyRewardResponse>($"{apiME}/daily-reward");
        }

        public Task<PlayerInfoResponse> modifyPlayerInfo(string name, int headIconId)
        {
            ModifyPlayerInfoRequest modifyPlayerInfo = new ModifyPlayerInfoRequest()
            {
                name = name,
                iconIndex = headIconId,
            };

            return proxy.callPatchApi<PlayerInfoResponse>(apiME, modifyPlayerInfo);
        }

        public Task<PlayerInfoResponse> getPlayerInfo(object data = null)
        {
            return proxy.callApi<PlayerInfoResponse>(apiME, data);
        }

        public Task<TickResponse> sendKeepAlive()
        {
            return proxy.callApi<TickResponse>("/tick");
        }

        public Task<BindingResponse> linkToFB()
        {
            return proxy.callApiWithEmptyData<BindingResponse>($"{apiME}/bindings/facebook");
        }

        public Task<GuestLoginResponse> guestLogin(string deviceID)
        {
            return proxy.callApi<GuestLoginResponse>($"/guests", new GuestLoginRequest() { deviceId = deviceID });
        }

        public Task<OnlyResultResponse> askVerifyCode(string phoneNumber)
        {
            string locale = ApplicationConfig.nowLanguage.ToString().ToLower();
            return proxy.callApi<OnlyResultResponse>($"{apiME}/bindings/phone-number/verifications", new AskVerifyCode() { phoneNumber = phoneNumber, locale = locale });
        }
        #endregion
        #region Account Binding
        public Task<BindingResponse> bindingPhoneNumber(string phoneNumber, string verifyCode)
        {
            return proxy.callApi<BindingResponse>($"{apiME}/bindings/phone-number", new BindingPhoneNumber() { phoneNumber = phoneNumber, code = verifyCode });
        }

        public Task<OnlyResultResponse> askEmailVerifyCode(string email)
        {
            string locale = ApplicationConfig.nowLanguage.ToString().ToLower();
            return proxy.callApi<OnlyResultResponse>($"{apiME}/bindings/email/verifications", new AskEmailVerifyCode() { email = email, locale = locale });
        }
        public Task<BindingResponse> bindingEmail(string email, string verifyCode, bool isReceiveNew)
        {
            return proxy.callApi<BindingResponse>($"{apiME}/bindings/email", new BindingEmail() { email = email, code = verifyCode, acceptPromotions = isReceiveNew });
        }
        #endregion
        #region Newbie-Tutorial

        int dogStageID { get { return SaveTheDogMapData.instance.nowOpenStageID; } }
        int dogClickID { get { return SaveTheDogMapData.instance.nowClickID; } }

        public Task<NewbieAdventure> getNewbieAdventure()
        {
            return proxy.callApi<NewbieAdventure>($"{apiME}/newbie-adventure", isShowMsgBox: false);
        }

        public Task<NewbieAdventureNotice> setNewbieAdventureNotice()
        {
            return proxy.callApi<NewbieAdventureNotice>($"/newbie-adventure/{dogStageID}/{dogClickID}/notice", new PostEmpty());
        }
        public Task<NewbieAdventureNotice> setNewbieAdventureComplete()
        {
            return proxy.callApi<NewbieAdventureNotice>($"/newbie-adventure/{dogStageID}/{dogClickID}/complete", new PostEmpty());
        }
        public Task<NewbieAdventureMissionProgress> getNewbieAdventureMissionProgress()
        {
            return proxy.callApi<NewbieAdventureMissionProgress>($"/newbie-adventure/{dogStageID}/{dogClickID}/progress");
        }
        public Task<NewbieAdventureRedeem> getNewbieAdventureRedeem()
        {
            return proxy.callApi<NewbieAdventureRedeem>($"/newbie-adventure/{dogStageID}/{dogClickID}/redeem", new PostEmpty(), isShowMsgBox: false);
        }

        public Task<NewbieTutorial> getNewbieTutorial()
        {
            return proxy.callApi<NewbieTutorial>($"{apiME}/newbie-tutorial");
        }
        public Task<NewbieMission> getNewbieCashReward()
        {
            return proxy.callApi<NewbieMission>("/newbie-tutorial/cash-reward");
        }

        public Task<NewbieMission> getNewbieMission()
        {
            return proxy.callApi<NewbieMission>("/newbie-tutorial/mission");
        }
        #endregion
        public Task<GameInfoResponse> getGamesInfo()
        {
            return proxy.callApi<GameInfoResponse>($"/lobby/games");
        }

        public Task<WagerResponse> getWagerInfos(string wagerID)
        {
            return proxy.callApi<WagerResponse>($"{apiME}/wagers/{wagerID}/attachments");
        }
        #region Activity
        public Task<GetBagResponse> getBag()
        {
            return proxy.callApi<GetBagResponse>($"{apiME}/bag");
        }

        public Task<GetBagItemResponse> getBagItem(string itemID)
        {
            return proxy.callApi<GetBagItemResponse>($"{apiME}/bag/{itemID}");
        }

        public Task<GetActivityResponse> getActivity()
        {
            return proxy.callApi<GetActivityResponse>("/lobby/activity");
        }
        public Task<ActivityStoreResponse> getActivityStore()
        {
            return proxy.callApi<ActivityStoreResponse>("/activity/store", new ActivityStoreData());
        }

        public Task<ActivityPropResponse> getActivityProp()
        {
            return proxy.callApi<ActivityPropResponse>($"{apiME}/activity/prop");
        }
        #endregion
        #region Store
        public Task<BuyProductResponse> sendStoreOrder(string sku)
        {
            return proxy.callApi<BuyProductResponse>($"{apiStroe}/orders", new ProductSKU()
            {
                sku = sku,
            });
        }
        public Task<OnlyResultResponse> patchReceipt(string orderId, string receipt)
        {
            return proxy.callPatchApi<OnlyResultResponse>($"{apiStroe}/orders/{orderId}/receipt", new StoreReceipt()
            {
                receipt = receipt
            });
        }
        public Task<OnlyResultResponse> patchReceiptWithCoupon(string orderId, string receipt, string couponId)
        {
            return proxy.callPatchApi<OnlyResultResponse>($"{apiStroe}/orders/{orderId}/receipt", new StoreReceiptWithCoupon()
            {
                receipt = receipt,
                couponId = couponId
            });
        }
        public Task<CommonRewardsResponse> sendStoreRedeem(string orderId)
        {
            return proxy.callApiWithEmptyData<CommonRewardsResponse>($"{apiStroe}/orders/{orderId}/redeem");
        }
        public Task<OnlyResultResponse> sendStoreCancel(string orderId)
        {
            return proxy.callApiWithEmptyData<OnlyResultResponse>($"{apiStroe}/orders/{orderId}/cancel", isShowMsgBox: false);
        }
        public Task<GetStoreResponse> getStore()
        {
            return proxy.callApi<GetStoreResponse>(apiStroe);
        }
        public Task<PatchBounsResponse> patchBouns()
        {
            return proxy.callPatchApi<PatchBounsResponse>($"{apiStroe}/bonus", null);
        }
        public Task<GetBounsResponse> getBouns()
        {
            return proxy.callApi<GetBounsResponse>($"{apiStroe}/bonus");
        }
        public Task<GetSpecialOfferResponse> getSpecialOffer()
        {
            return proxy.callApi<GetSpecialOfferResponse>($"{apiStroe}/special-offer");
        }
        #endregion

        public Task<CommonRewardsResponse> rewardPacksRedeem(string packId)
        {
            return proxy.callApi<CommonRewardsResponse>($"/reward-packs/{packId}/redeem", new LvUpRedeem() { packetID = packId }); ;
        }

        public Task<OnlyResultResponse> deleteSession()
        {
            return proxy.callDeleteApi<OnlyResultResponse>($"{apiME}/session");
        }

        public Task<PeekMailCountResponse> peekMailCount()
        {
            return proxy.callApi<PeekMailCountResponse>($"{apiME}/inbox/messages/count");
        }

        public Task<GetMailsResponse> getAllMail()
        {
            return proxy.callApi<GetMailsResponse>($"{apiME}/inbox/messages");
        }

        public Task<CommonRewardsResponse> redeemMail(string mailId)
        {
            return proxy.callApi<CommonRewardsResponse>($"{apiME}/inbox/messages/{mailId}/collect", new PostEmpty()); //force use POST
        }

        /// <summary>
        /// 金蛋滿滿API
        /// </summary>
        public Task<GoldEggResponse> getCoinBank()
        {
            return proxy.callApi<GoldEggResponse>($"{apiME}/coin-bank");
        }

        public Task<ProductResponse> getProductID(string sku)
        {
            return proxy.callApi<ProductResponse>($"/products/{sku}");
        }
        #region Jigsaw
        public Task<JigsawRecycle> getJigsawRecycle()
        {
            return proxy.callApi<JigsawRecycle>($"{apiME}/album/recycle");
        }

        /// <summary>
        /// 取得所有季度概要
        /// </summary>
        public Task<JigsawAllSeasonAbstractResponse> getJigsawAllSeasonAbstract()
        {
            return proxy.callApi<JigsawAllSeasonAbstractResponse>($"{apiME}/album/seasons");
        }

        /// <summary>
        /// 取得目前季度概要
        /// </summary>
        public Task<JigsawCurrentSeasonAbstractResponse> getJigsawCurrentSeasonAbstract()
        {
            return proxy.callApi<JigsawCurrentSeasonAbstractResponse>($"{apiME}/album/seasons/current");
        }

        /// <summary>
        /// 取得目前或特定某季的拼圖冊進度( seasonId: "current", "001"~"999" )
        /// </summary>
        public Task<JigsawAllAlbumProgressResponse> getJigsawAllAlbumProgress(string seasonId)
        {
            return proxy.callApi<JigsawAllAlbumProgressResponse>($"{apiME}/album/seasons/{seasonId}/summary");
        }
        public Task<JigsawAlbumDetailResponse> getJigsawAlbumDetail(string albumId)
        {
            return proxy.callApi<JigsawAlbumDetailResponse>($"{apiME}/albums/{albumId}");
        }
        public Task<JigsawAlbumRewardsResponse> getAlbmRewards()
        {
            return proxy.callApi<JigsawAlbumRewardsResponse>($"{apiME}/album/rewards");
        }
        public Task<CommonRewardsResponse> redeemJigsawAlbumReward(string id)
        {
            return proxy.callApi<CommonRewardsResponse>($"{apiME}/album/rewards/{id}/redeem", new PostEmpty());
        }
        public Task<AlbumVouchersResponse> getAllAlbumVouchers()
        {
            return proxy.callApi<AlbumVouchersResponse>($"{apiME}/album/vouchers");
        }
        public Task<AlbumVoucherRedeemResponse> redeemAlbumVoucher(string id, VoucherRedeemRequestData data)
        {
            return proxy.callApi<AlbumVoucherRedeemResponse>($"{apiME}/album/vouchers/{id}/redeem", data);
        }
        public Task<RecycleWheelTableResponse> getRecycleAlbumWheelTable(int level)
        {
            return proxy.callApi<RecycleWheelTableResponse>($"{apiME}/album/recycle-wheel-levels/{level}");
        }
        public Task<AlbumRecycleResponse> recycleAlbumItems(AlbumRecycleRequestData data)
        {
            return proxy.callApi<AlbumRecycleResponse>($"{apiME}/album/recycle", data);
        }
        #endregion
        public Task<PopupsResponse> getPopups()
        {
            return proxy.callApi<PopupsResponse>($"/lobby/popups");
        }
        public Task<StayGameBonus> getStayGameBonus()
        {
            return proxy.callApi<StayGameBonus>($"{apiME}/retention-bonus");
        }
        public Task<StayGameBonusRedeem> stayGameBonusRedeem(string boxType)
        {
            return proxy.callApi<StayGameBonusRedeem>($"{apiME}/retention-bonus/redeem", new StayGameRedeem() { type = boxType });
        }
        public Task<StayArcadeGameBonus> getStayArcadeBonus()
        {
            return proxy.callApi<StayArcadeGameBonus>($"{apiME}/arcade-bonus");
        }
        public Task<StayArcadeGameCommit> commitStayArcadeGame(string gameType)
        {
            return proxy.callApi<StayArcadeGameCommit>($"{apiME}/arcade-bonus/commit", new StayGameRedeem() { type = gameType });
        }
        public Task<StayArcadeGameRedeem> redeemStayArcadeGame(string gameType)
        {
            return proxy.callApi<StayArcadeGameRedeem>($"{apiME}/arcade-bonus/redeem", new StayGameRedeem() { type = gameType });
        }
        public Task<GetCouponsResponse> getCoupons()
        {
            return proxy.callApi<GetCouponsResponse>($"{apiME}/coupons");
        }
        public Task<RewardPacksResponse> getRewardPacks(string packId)
        {
            return proxy.callApi<RewardPacksResponse>($"/reward-packs/{packId}");
        }
        #region HighRoller
        public Task<HighRollerUserRecordResponse> getHighRollerUser()
        {
            return proxy.callApi<HighRollerUserRecordResponse>($"{apiME}/high-roller");
        }

        public Task<HighRollerCheckExpireResponse> checkExpire()
        {
            return proxy.callApi<HighRollerCheckExpireResponse>("/high-roller/access/expiry/check", new PostEmpty());
        }

        public Task<HighRollerCheckExpireResponse> checkExperienceUsed()
        {
            return proxy.callApi<HighRollerCheckExpireResponse>("/high-roller/access/experience/check-used", new PostEmpty());
        }

        public Task<HighRollerVaultResponse> getCurrentReturnToPay()
        {
            return proxy.callApi<HighRollerVaultResponse>("/high-roller/vault/rebate", new PostEmpty());
        }

        public Task<OnlyResultResponse> sendReturnToPay()
        {
            return proxy.callApi<OnlyResultResponse>("/high-roller/vault/rebate/check-send", new PostEmpty());
        }

        public Task<HighRollerStoreResponse> getHighRollerStore()
        {
            return proxy.callApi<HighRollerStoreResponse>("/high-roller/store");
        }
        #endregion

        #region DailyMission
        public Task<DailyMissionDataResponse> getDailyMissionData()
        {
            return proxy.callApi<DailyMissionDataResponse>($"{apiME}/daily-mission");
        }

        public Task<DailyMissionProgressResponse> getDailyMissionProgress()
        {
            return proxy.callApi<DailyMissionProgressResponse>("/daily-mission/progress/get");
        }

        public Task<DailyMissionRewardResponse> sendDailyMissionGeneralReward()
        {
            return proxy.callApi<DailyMissionRewardResponse>("/daily-mission/general/reward", new PostEmpty());
        }

        public Task<DailyMissionRewardResponse> sendDailyMissionSpecialReward()
        {
            return proxy.callApi<DailyMissionRewardResponse>("/daily-mission/special/reward", new PostEmpty());
        }

        public Task<DailyMissionRewardResponse> sendDailyMissionMedalReward(int stageIndex)
        {
            return proxy.callApi<DailyMissionRewardResponse>($"/daily-mission/medal/reward/{stageIndex}", new PostEmpty());
        }
        #endregion

        #region Chips Collect
        public Task<GetChipsCollect> getChipsCollect()
        {
            return proxy.callApi<GetChipsCollect>($"/store/chips-collect");
        }

        public Task<ChipsCollectRedeem> postChipsCollectRedeem()
        {
            return proxy.callApi<ChipsCollectRedeem>($"/store/chips-collect/redeem", new PostEmpty());
        }

        public Task<PatchChipsCollectRedeem> patchChipsCollectRedeem()
        {
            return proxy.callPatchApi<PatchChipsCollectRedeem>($"/store/chips-collect/redeem", null);
        }
        #endregion

        public void clientDisconnect()
        {
            proxy.httpClientDisconnect();
        }
    }
}
