namespace FFXIVNetworkPacketAnalysisTool.PacketStructures
{
    /// <summary>
    /// Actor控制类型枚举
    /// 用于 ActorControl、ActorControlSelf、ActorControlTarget 包中的 Id 字段
    /// </summary>
    public enum ActorControlId : ushort
    {
        SetCombatState = 0x04,
        ChangeClassJob = 0x05,
        Death = 0x06,
        CancelCast = 0x0F,
        SetRecastGroupDuration = 0x11,
        AddStatus = 0x14,
        RemoveStatus = 0x15,
        SetStatusParam = 0x16,
        StatusEffect = 0x17,
        SetRestExp = 0x18,
        SetCharacterState = 0x1F,
        SetLockOn = 0x22,
        SetChanneling = 0x23,
        RemoveChanneling = 0x2F,
        SetModelScale = 0x30,
        SetModelAttr = 0x31,
        SetTargetable = 0x36,
        SetTimelineModelSkin = 0x3E,
        SetTimelineModelFlag = 0x3F,
        EventDirector = 0x6D,
        RejectEventFinish = 0x8C,
        SetMoveFlag2 = 0xEC,
        PlayActionTimeLine = 0x197,
        SetActorTimeLine = 0x19D,
        SetLimitBreak = 0x1F9,
        RejectSendAction = 0x2BC,
        InterruptCast = 0x5F1,
        FateState = 0x931,
        FateStart = 0x934,
        FateEnd = 0x935,
        FateProgress = 0x93C
    }
}
