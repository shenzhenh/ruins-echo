namespace RuinsEcho.Relics
{
    /// <summary>
    /// 遗物效果接口（规格 6.1）。效果只负责把修正写入 RelicManager.Stats，
    /// 新增遗物无需修改玩家 / 影子 / 敌人代码。
    /// </summary>
    public interface IRelicEffect
    {
        void OnApply(RelicManager manager);
        void OnRemove(RelicManager manager);
    }
}
