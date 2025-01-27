﻿using ForkCommon.Model.Entity.Enums.Player;
using ForkCommon.Model.Entity.Pocos.Player;
using ForkCommon.Model.Privileges;
using ForkCommon.Model.Privileges.Entity.ReadEntity.ReadConsoleTab;

namespace ForkCommon.Model.Notifications.EntityNotifications.PlayerNotifications;

/// <summary>
///     Updates a Player on the Banlist (add, remove or update)
/// </summary>
[Privileges(typeof(ReadBanlistConsoleTabPrivilege))]
public class UpdateBanlistPlayerNotification : AbstractEntityNotification
{
    public UpdateBanlistPlayerNotification(ulong entityId, PlayerlistUpdateType updateType, Player player) :
        base(entityId)
    {
        UpdateType = updateType;
        Player = player;
    }

    public PlayerlistUpdateType UpdateType { get; set; }
    public Player Player { get; set; }
}