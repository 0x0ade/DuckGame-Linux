using System;
using System.Collections.Generic;
using Steamworks;

public class Steam : IDisposable {

    private static Dictionary<Type, object> _callResults = new Dictionary<Type, object>();

    private static CallResult<T> GetCallResult<T>() => (CallResult<T>) _callResults[typeof(T)];
    private static void SetCallResult<T>(CallResult<T>.APIDispatchDelegate func) => _callResults.Add(typeof(T), new CallResult<T>(func));
    private static void SetCallResult<T>(SteamAPICall_t call) => ((CallResult<T>) _callResults[typeof(T)]).Set(call);

    private static bool _initialized;
    private static bool _offline;

    private static WorkshopItem _pendingItem;

    private static WorkshopItem _pendingItemDownload;

    // private static bool _waitingForCurrentStats; // unused

    private readonly static int kPacketBufferSize = 2048; // originally not readonly; could be const because it's inlined

    private unsafe static byte[] _packetData;

    public delegate void ConnectionRequestedDelegate(User remote);
    public static event ConnectionRequestedDelegate ConnectionRequested;

    public delegate void ConnectionFailedDelegate(User remote);
    public static event ConnectionFailedDelegate ConnectionFailed;

    public delegate void InviteReceivedDelegate(User friend, Lobby lobby);
    public static event InviteReceivedDelegate InviteReceived;

    public delegate void LobbySearchCompleteDelegate(Lobby lobby);
    public static event LobbySearchCompleteDelegate LobbySearchComplete;

    public delegate void RequestCurrentStatsDelegate();
    public static event RequestCurrentStatsDelegate RequestCurrentStatsComplete;

    public static Lobby lobbySearchResult { get; private set; }

    public static bool lobbySearchComplete { get; private set; }

    public static bool waitingForGlobalStats { get; private set; }

    public static int lobbiesFound { get; private set; }

    public static Lobby lobby { get; private set; }

    public static User user { get; private set; }

    // private static List<User> _friends; // unused
    public unsafe static List<User> friends => _.GetList(SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagAll), i => User.GetUser(SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagAll)));

    public Steam() {
        _initialized = false;
    }

    public static bool NeedsRestartForSteam() {
        return SteamAPI.RestartAppIfNecessary(SteamUtils.GetAppID());
    }

    public unsafe static bool Authorize() {
        if (!_initialized)
            return false;
        // FIXME: SteamApps.RequestAppProofOfPurchaseKey? SteamApps.BIsAppInstalled? SteamApps.BIsSubscribedApp?
        return SteamApps.BIsSubscribedApp(SteamUtils.GetAppID());
    }

    public static bool InitializeCore() {
        return _initialized = SteamAPI.Init();
    }

    public unsafe static void Initialize() {
        Callback<LobbyChatUpdate_t>.Create(OnLobbyMemberStatus);
        Callback<P2PSessionRequest_t>.Create(OnConnectionRequest);
        Callback<P2PSessionConnectFail_t>.Create(OnConnectionFail);
        Callback<GameLobbyJoinRequested_t>.Create(OnInviteReceive);
        Callback<DownloadItemResult_t>.Create(OnDownloadItemResult);
        Callback<UserStatsReceived_t>.Create(OnRequestStats);

        // Manually added CallResults which otherwise would be added, then removed behind the scenes:
        SetCallResult<GlobalStatsReceived_t>(OnRequestGlobalStats);
        SetCallResult<CreateItemResult_t>(OnCreateItem);
        SetCallResult<SubmitItemUpdateResult_t>(OnSubmitItemUpdate);
        SetCallResult<SteamUGCQueryCompleted_t>(OnSendQueryUGCRequest);
        SetCallResult<LobbyCreated_t>(OnCreateLobby);
        SetCallResult<LobbyEnter_t>(OnJoinLobby);
        SetCallResult<LobbyMatchList_t>(OnSearchForLobby);

        _packetData = new byte[kPacketBufferSize];

        // THIS IS A HORRIBLE HACK to get this to comply when using a stubbed Steamworks.NET.dll.
        if (_initialized)
            _initialized = SteamUser.GetSteamID().m_SteamID != 0;

        if (_initialized) {
            user = User.GetUser(SteamUser.GetSteamID());
            // FIXME: The original Steam.dll would call something now, but I can't identify what.
        } else {
            // THIS IS A HORRIBLE HACK TO GET DUCK GAME TO SHUT UP WHEN "OFFLINE".
            _offline = true;
            user = new OfflineSelfUser();
        }

        lobbySearchComplete = true;
        lobbySearchResult = null;
    }

    public static bool IsInitialized() {
        return _initialized || _offline;
    }

    public unsafe static void Terminate() {
        if (!_initialized)
            return;
        SteamAPI.Shutdown();
    }

    public static void Update() {
        if (!_initialized)
            return;
        SteamAPI.RunCallbacks();
    }

    public unsafe static void OverlayOpenURL(string url) {
        if (!_initialized)
            return;
        SteamFriends.ActivateGameOverlayToWebPage(url);
    }

    public unsafe static void SetAchievement(string id) {
        if (!_initialized)
            return;
        SteamUserStats.SetAchievement(id);
    }

    public unsafe static float GetStat(string id) {
        if (!_initialized)
            return 0f;
        float val;
        SteamUserStats.GetStat(id, out val);
        return val;
    }

    public unsafe static void SetStat(string id, float val) {
        if (!_initialized)
            return;
        SteamUserStats.SetStat(id, val);
    }

    public unsafe static void SetStat(string id, int val) {
        if (!_initialized)
            return;
        SteamUserStats.SetStat(id, val);
    }

    public unsafe static void StoreStats() {
        if (!_initialized)
            return;
        SteamUserStats.StoreStats();
    }

    public static void RequestGlobalStats() {
        if (!_initialized)
            return;
        waitingForGlobalStats = true;
        SetCallResult<GlobalStatsReceived_t>(SteamUserStats.RequestGlobalStats(1)); // FIXME: How many days for RequestGlobalStats?
    }

    public unsafe static double GetGlobalStat(string id) {
        if (!_initialized)
            return 0D;
        double val;
        SteamUserStats.GetGlobalStat(id, out val);
        return val;
    }

    public unsafe static double GetDailyGlobalStat(string id) {
        if (!_initialized)
            return 0D;
        // FIXME: This feels wrong for GetDailyGlobalStat.
        double val;
        SteamUserStats.GetGlobalStat(id, out val);
        return val;
    }

    public static WorkshopItem CreateItem() {
        if (!_initialized)
            return null;
        _pendingItem = new WorkshopItem();
        SetCallResult<CreateItemResult_t>(SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeFirst));
        return _pendingItem;
    }

    public unsafe static void ShowWorkshopLegalAgreement(string id) {
        if (_initialized)
            SteamFriends.ActivateGameOverlayToWebPage("steam://url/CommunityFilePage/" + id);
    }

    public unsafe static void StartUpload(WorkshopItem item) {
        if (!_initialized)
            return;
        _pendingItem = item;
        SetCallResult<SubmitItemUpdateResult_t>(SteamUGC.SubmitItemUpdate(new UGCUpdateHandle_t(item.updateHandle), item.data.changeNotes));
    }

    public unsafe static List<WorkshopItem> GetAllWorkshopItems() {
        if (!_initialized)
            return new List<WorkshopItem>();
        // FIXME: This seems wrong, but the original basically just does this.
        PublishedFileId_t[] tmp = new PublishedFileId_t[GetNumWorkshopItems()];
        SteamUGC.GetSubscribedItems(tmp, (uint) tmp.Length);
        List<WorkshopItem> list = _.GetList(tmp.Length, i => WorkshopItem.GetItem(tmp[i].m_PublishedFileId));
        RequestWorkshopInfo(list);
        return list;
    }

    public unsafe static int GetNumWorkshopItems() {
        if (!_initialized)
            return 0;
        // FIXME: This seems wrong, but the original just calls a single function... this is the closest match.
        return (int) SteamUGC.GetNumSubscribedItems();
    }

    public unsafe static void RequestWorkshopInfo(List<WorkshopItem> items) {
        if (!_initialized)
            return;
        UGCQueryHandle_t query = SteamUGC.CreateQueryUGCDetailsRequest(_.GetArray(items, item => new PublishedFileId_t(item.id)), (uint) items.Count);
        SetCallResult<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(query));
    }

    public unsafe static void WorkshopUnsubscribe(ulong id) {
        if (!_initialized)
            return;
        SteamUGC.UnsubscribeItem(new PublishedFileId_t(id));
    }

    public unsafe static void WorkshopSubscribe(ulong id) {
        if (!_initialized)
            return;
        SteamUGC.SubscribeItem(new PublishedFileId_t(id));
    }

    public unsafe static bool DownloadWorkshopItem(WorkshopItem item) {
        bool result = SteamUGC.DownloadItem(new PublishedFileId_t(item.id), true);
        item.ResetProcessing();
        _pendingItemDownload = item;
        return result;
    }

    public static WorkshopQueryAll CreateQueryAll(WorkshopQueryFilterOrder queryType, WorkshopType type) {
        return new WorkshopQueryAll((EUGCQuery) queryType, (EUGCMatchingUGCType) type);
    }

    public static WorkshopQueryUser CreateQueryUser(ulong accountID, WorkshopList listType, WorkshopType type, WorkshopSortOrder order) {
        return new WorkshopQueryUser((uint) accountID, (EUserUGCList) listType, (EUGCMatchingUGCType) type, (EUserUGCListSortOrder) order);
    }

    public static WorkshopQueryFileDetails CreateQueryFileDetails() {
        return new WorkshopQueryFileDetails();
    }

    public unsafe static byte[] FileRead(string name) {
        if (!_initialized)
            return null;
        int size = SteamRemoteStorage.GetFileSize(name);
        byte[] data = new byte[size];
        int rv = SteamRemoteStorage.FileRead(name, data, size);
        return data;
    }

    public unsafe static bool FileExists(string name) {
        if (!_initialized)
            return false;
        return SteamRemoteStorage.FileExists(name);
    }

    public unsafe static bool FileWrite(string name, byte[] data, int length) {
        if (!_initialized)
            return false;
        return data != null && data.Length != 0 && SteamRemoteStorage.FileWrite(name, data, length);
    }

    public unsafe static bool FileDelete(string name) {
        if (!_initialized)
            return false;
        return SteamRemoteStorage.FileDelete(name);
    }

    public unsafe static int FileGetCount() {
        if (!_initialized)
            return 0;
        return SteamRemoteStorage.GetFileCount();
    }

    public unsafe static string FileGetName(int file) {
        if (!_initialized)
            return null;
        int size;
        return SteamRemoteStorage.GetFileNameAndSize(file, out size);
    }

    public unsafe static int FileGetSize(int file) {
        if (!_initialized)
            return 0;
        int size;
        SteamRemoteStorage.GetFileNameAndSize(file, out size);
        return size;
    }

    public static Lobby CreateLobby(SteamLobbyType lobbyType, int maxMembers) {
        if (!_initialized)
            return null;
        if (lobby != null)
            LeaveLobby(lobby);
        lobby = new Lobby(lobbyType, maxMembers);
        SetCallResult<LobbyCreated_t>(SteamMatchmaking.CreateLobby((ELobbyType) lobbyType, maxMembers));
        return lobby;
    }

    public static Lobby JoinLobby(ulong lobbyID) {
        if (!_initialized)
            return null;
        if (lobby == null || lobbyID != lobby.id) {
            if (lobby != null)
                LeaveLobby(lobby);
            lobby = new Lobby(lobbyID);
        }
        SetCallResult<LobbyEnter_t>(SteamMatchmaking.JoinLobby(new CSteamID(lobbyID)));
        return lobby;
    }

    public unsafe static void LeaveLobby(Lobby which) {
        if (!_initialized)
            return;
        if (which != null)
            SteamMatchmaking.LeaveLobby(new CSteamID(which.id));
        if (lobby == which)
            lobby = null;
    }

    public unsafe static void SearchForLobby(User who) {
        if (!_initialized)
            return;
        lobbySearchResult = null;
        lobbySearchComplete = false;
        lobbiesFound = 0;
        if (who != null) {
            // FIXME: What does the original Steam.dll do? Filter by user?
        }
        SteamMatchmaking.RequestLobbyList();
    }

    public unsafe static void SearchForLobbyWorldwide() {
        if (!_initialized)
            return;
        lobbySearchResult = null;
        lobbySearchComplete = false;
        lobbiesFound = 0;
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SetCallResult<LobbyMatchList_t>(SteamMatchmaking.RequestLobbyList());
    }

    public unsafe static int GetNumLobbyMembers(Lobby which) {
        if (!_initialized)
            return 0;
        return which != null ? 0 :
            SteamMatchmaking.GetNumLobbyMembers(new CSteamID(which.id));
    }

    public unsafe static User GetLobbyMemberAtIndex(Lobby which, int member) {
        if (!_initialized)
            return null;
        return which != null && member < GetNumLobbyMembers(which) ? null :
            User.GetUser(SteamMatchmaking.GetLobbyMemberByIndex(new CSteamID(which.id), member));
    }

    public unsafe static void AddLobbyStringFilter(string key, string value, SteamLobbyComparison compareType) {
        if (!_initialized)
            return;
        SteamMatchmaking.AddRequestLobbyListStringFilter(key, value, (ELobbyComparison) compareType);
    }

    public unsafe static void AddLobbyNumericalFilter(string key, int value, SteamLobbyComparison compareType) {
        if (!_initialized)
            return;
        SteamMatchmaking.AddRequestLobbyListNumericalFilter(key, value, (ELobbyComparison) compareType);
    }

    public unsafe static void AddLobbySlotsAvailableFilter(int slots) {
        if (!_initialized)
            return;
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(slots);
    }

    public unsafe static void AddLobbyMaxResultsFilter(int max) {
        if (!_initialized)
            return;
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(max);
    }

    public unsafe static void AddLobbyNearFilter(string key, int filt) {
        if (!_initialized)
            return;
        SteamMatchmaking.AddRequestLobbyListNearValueFilter(key, filt);
    }

    public unsafe static Lobby GetSearchLobbyAtIndex(int index) {
        if (!_initialized)
            return null;
        return new Lobby(SteamMatchmaking.GetLobbyByIndex(index));
    }

    public unsafe static bool AcceptConnection(User who) {
        if (!_initialized)
            return false;
        return SteamNetworking.AcceptP2PSessionWithUser(new CSteamID(who.id));
    }

    public unsafe static void SendPacket(User who, byte[] data, uint size, P2PDataSendType type) {
        if (!_initialized)
            return;
        SteamNetworking.SendP2PPacket(new CSteamID(who.id), data, size, (EP2PSend) type);
    }

    public unsafe static void CloseConnection(User who) {
        if (!_initialized)
            return;
        SteamNetworking.CloseP2PSessionWithUser(new CSteamID(who.id));
    }

    public unsafe static SteamPacket ReadPacket() {
        if (!_initialized)
            return null;
        uint size;
        if (SteamNetworking.IsP2PPacketAvailable(out size)) {
            byte[] data;
            if (size > kPacketBufferSize)
                data = new byte[size];
            else
                data = _packetData;
            CSteamID user;
            if (SteamNetworking.ReadP2PPacket(data, size, out size, out user)) {
                if (data == _packetData) {
                    data = new byte[size];
                    Array.Copy(_packetData, data, size);
                }
                return new SteamPacket {
                    data = data,
                    connection = User.GetUser(user)
                };
            }
        }
        return null;
    }

    public unsafe static bool InviteUser(User userVal, Lobby lobbyVal) {
        if (!_initialized)
            return false;
        if (lobbyVal == null) {
            lobbyVal = lobby;
            if (lobbyVal == null)
                return false;
        }
        return userVal != null && SteamMatchmaking.InviteUserToLobby(new CSteamID(lobbyVal.id), new CSteamID(userVal.id));
    }

    public unsafe static void OpenInviteDialogue() {
        if (!_initialized)
            return;
        if (lobby == null)
            return;
        SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(lobby.id));
    }

    public static void LogException(string message, string error) {
    }

    private unsafe static void OnCreateLobby(LobbyCreated_t result, bool ioFailure) {
        if (!_initialized)
            return;
        if (lobby == null)
            return;
        if (result.m_eResult == EResult.k_EResultOK)
            lobby.OnProcessingComplete(result.m_ulSteamIDLobby, SteamLobbyJoinResult.Success);
        else
            lobby.OnProcessingComplete(0, SteamLobbyJoinResult.Error);
    }

    private unsafe static void OnJoinLobby(LobbyEnter_t result, bool ioFailure) {
        if (!_initialized)
            return;
        lobby?.OnProcessingComplete(result.m_ulSteamIDLobby, (SteamLobbyJoinResult) result.m_rgfChatPermissions);
    }

    private unsafe static void OnSearchForLobby(LobbyMatchList_t result, bool ioFailure) {
        if (!_initialized)
            return;
        if (result.m_nLobbiesMatching != 0) {
            lobbySearchResult = new Lobby(SteamMatchmaking.GetLobbyByIndex(0));
            lobbiesFound = (int) result.m_nLobbiesMatching;
        } else {
            lobbySearchResult = null;
            lobbiesFound = 0;
        }
        lobbySearchComplete = true;
        LobbySearchComplete?.Invoke(lobbySearchResult);
    }

    private unsafe static void OnRequestGlobalStats(GlobalStatsReceived_t result, bool ioFailure) {
        if (!_initialized)
            return;
        waitingForGlobalStats = false;
    }

    private unsafe static void OnCreateItem(CreateItemResult_t result, bool ioFailure) {
        if (!_initialized)
            return;
        _pendingItem?.ApplyResult((SteamResult) result.m_eResult, result.m_bUserNeedsToAcceptWorkshopLegalAgreement, result.m_nPublishedFileId.m_PublishedFileId);
    }

    private unsafe static void OnSubmitItemUpdate(SubmitItemUpdateResult_t result, bool ioFailure) {
        if (!_initialized)
            return;
        _pendingItem?.ApplyResult((SteamResult) result.m_eResult, result.m_bUserNeedsToAcceptWorkshopLegalAgreement, _pendingItem.id);
    }

    private unsafe static void OnSendQueryUGCRequest(SteamUGCQueryCompleted_t result, bool ioFailure) {
        Console.WriteLine($"Got result in Steam.cs: {result.m_handle.m_UGCQueryHandle}");
        if (!_initialized)
            return;
        for (uint i = 0; i < result.m_unNumResultsReturned; i++) {
            SteamUGCDetails_t details;
            if (SteamUGC.GetQueryUGCResult(result.m_handle, i, out details)) {
                WorkshopItem item = WorkshopItem.GetItem(details.m_nPublishedFileId.m_PublishedFileId);
                if (item != null) {
                    WorkshopItemData workshopItemData = new WorkshopItemData();
                    SteamUGC.GetQueryUGCPreviewURL(result.m_handle, i, out workshopItemData.previewPath, 256);
                    workshopItemData.description = details.m_rgchDescription;
                    workshopItemData.votesUp = (int) details.m_unVotesUp;
                    item.SetDetails(details.m_pchFileName, workshopItemData);
                }
            }
        }
        SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
    }

    // This is exactly what is going on in the original Steam.dll.
    // FIXME: Replace hardcoded offsets in OnLobbyMemberStatus.
    private unsafe static void OnLobbyMemberStatus(LobbyChatUpdate_t result) {
        if (lobby == null)
            return;
        User user = User.GetUser((ulong) (*(long*) (&result + 8 / sizeof(LobbyChatUpdate_t))));
        ulong num = (ulong) (*(long*) (&result + 16 / sizeof(LobbyChatUpdate_t)));
        if (*(long*) (&result + 8 / sizeof(LobbyChatUpdate_t)) != (long) num)
            user = User.GetUser(num);
        lobby.OnUserStatusChange(user, (SteamLobbyUserStatusFlags) (*(int*) (&result + 24 / sizeof(LobbyChatUpdate_t))), user);
    }

    private unsafe static void OnConnectionRequest(P2PSessionRequest_t result) {
        ConnectionRequested?.Invoke(User.GetUser(result.m_steamIDRemote));
    }

    private unsafe static void OnConnectionFail(P2PSessionConnectFail_t result) {
        ConnectionFailed?.Invoke(User.GetUser(result.m_steamIDRemote));
    }

    private unsafe static void OnInviteReceive(GameLobbyJoinRequested_t result) {
        InviteReceived?.Invoke(User.GetUser(result.m_steamIDFriend), new Lobby(result.m_steamIDLobby));
    }

    private unsafe static void OnDownloadItemResult(DownloadItemResult_t result) {
        _pendingItemDownload?.ApplyDownloadResult((SteamResult) result.m_eResult);
    }

    private unsafe static void OnRequestStats(UserStatsReceived_t result) {
        RequestCurrentStatsComplete?.Invoke();
    }

    protected virtual void Dispose(bool flag) {
    }

    public void Dispose() {
        Dispose(true);
    }

}
