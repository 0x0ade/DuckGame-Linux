﻿using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Steamworks;

public class WorkshopItem : IDisposable {

    private static Dictionary<ulong, WorkshopItem> _items;

    internal static WorkshopItem GetItem(PublishedFileId_t id) {
        return GetItem(id.m_PublishedFileId);
    }

    public static WorkshopItem GetItem(ulong id) {
        if (id == 0) {
            return null;
        }

        if (_items == null) {
            _items = new Dictionary<ulong, WorkshopItem>();
        }
        using (Lock _lock = new Lock(_items)) {
            WorkshopItem item;
            if (!_items.TryGetValue(id, out item)) {
                item = new WorkshopItem(id);
                _items[id] = item;
            }
            return item;
        }

    }

    private PublishedFileId_t _id;
    public ulong id => _id.m_PublishedFileId;

    private UGCUpdateHandle_t _currentUpdateHandle;
    public ulong updateHandle => _currentUpdateHandle.m_UGCUpdateHandle;

    public string name { get; private set; }

    public WorkshopItemData data { get; private set; }

    public bool finishedProcessing { get; set; }
    public SteamResult result { get; private set; }
    public SteamResult downloadResult { get; private set; }

    public unsafe WorkshopItemState stateFlags => (WorkshopItemState) SteamUGC.GetItemState(_id);

    public bool needsLegal { get; private set; }

    public unsafe string path {
        get {
            // FIXME: What even is the WorkshopItem.path meant to point to?
            /*
            sbyte* ptr = < Module>.new[](256u);
            int num = *(<Module>.SteamInternal_ContextInit((void*)(&<Module>.?s_CallbackCounterAndContext@?1??SteamInternal_ModuleContext@@YAAAVCSteamAPIContext@@XZ@4PAPAXA)) + 56);
            ulong num2;
            uint num3;
            if (calli(System.Byte modopt(System.Runtime.CompilerServices.CompilerMarshalOverride) modopt(System.Runtime.CompilerServices.CallConvThiscall)(System.IntPtr,System.UInt64,System.UInt64*,System.SByte modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte)*,System.UInt32,System.UInt32*), num, _id, ref num2, ptr, 256, ref num3, *(*num + 240)))
            {
                return new string((sbyte*)ptr);
            }
            */

            return "";
        }
    }

    // private object _tags; // unused

    public WorkshopItem(ulong id)
        : this(new PublishedFileId_t(id)) {
    }

    internal WorkshopItem(PublishedFileId_t id) {
        _id = id;
        finishedProcessing = true;
        result = SteamResult.OK;
    }

    public WorkshopItem() {
    }

    public void ApplyResult(SteamResult r, bool legal, ulong id) {
        result = r;
        needsLegal = legal;
        _id = new PublishedFileId_t(id);
        finishedProcessing = true;
    }

    public void ApplyDownloadResult(SteamResult r) {
        downloadResult = r;
        finishedProcessing = true;
    }

    public unsafe bool ApplyWorkshopData(WorkshopItemData data) {
        UGCUpdateHandle_t handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), _id);
        if (handle.m_UGCUpdateHandle == 0) {
            return false;
        }
        this.data = data;
        if (data.name != null) {
            SteamUGC.SetItemTitle(handle, data.name);
            SteamUGC.SetItemVisibility(handle, (ERemoteStoragePublishedFileVisibility) data.visibility);
        }
        if (data.description != null) {
            SteamUGC.SetItemDescription(handle, data.description);
        }
        List<string> tags = data.tags;
        if (tags != null && tags.Count != 0) {
            SteamUGC.SetItemTags(handle, data.tags);
        }
        SteamUGC.SetItemPreview(handle, data.previewPath);
        SteamUGC.SetItemContent(handle, data.contentFolder);
        _currentUpdateHandle = handle;
        Steam.StartUpload(this);
        return true;
    }

    public unsafe TransferProgress GetUploadProgress() {
        ulong bytesDownloaded, bytesTotal;
        EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(_currentUpdateHandle, out bytesDownloaded, out bytesTotal);
        return new TransferProgress {
            status = (ItemUpdateStatus) (int) status,
            bytesDownloaded = bytesDownloaded,
            bytesTotal = bytesTotal
        };
    }

    public unsafe TransferProgress GetDownloadProgress() {
        ulong bytesDownloaded, bytesTotal;
        EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(_currentUpdateHandle, out bytesDownloaded, out bytesTotal);
        return new TransferProgress {
            status = ItemUpdateStatus.Invalid,
            bytesDownloaded = bytesDownloaded,
            bytesTotal = bytesTotal
        };
    }

    public void ResetProcessing() {
        finishedProcessing = false;
        needsLegal = false;
    }

    public void SkipProcessing() {
        finishedProcessing = true;
        needsLegal = false;
        result = SteamResult.OK;
    }

    public void SetDetails(string name, WorkshopItemData data) {
        this.name = name;
        this.data = data;
    }

    public unsafe void Subscribe() {
        SteamUGC.SubscribeItem(_id);
    }

    [HandleProcessCorruptedStateExceptions]
    protected virtual void Dispose(bool flag) {
    }

    public void Dispose() {
        Dispose(true);
    }

}
