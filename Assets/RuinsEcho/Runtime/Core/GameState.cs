namespace RuinsEcho.Core
{
    /// <summary>地牢数据层使用的敌人种类；表现层再映射到具体 Prefab。</summary>
    public enum EnemyKind
    {
        Melee = 0,
        Ranged = 1,
        ShadowHunter = 2
    }

    /// <summary>与规格文档 2.2 一致的状态机状态。</summary>
    public enum GameState
    {
        MainMenu = 0,
        RunPreparation = 1,
        InRoomIdle = 2,
        InCombat = 3,
        RoomReward = 4,
        EventDialog = 5,
        BossFight = 6,
        Victory = 7,
        Death = 8
    }
}
