using System;

namespace RuinsEcho.Relics
{
    /// <summary>便于新增遗物的空实现基类。</summary>
    [Serializable]
    public abstract class RelicEffectBase : IRelicEffect
    {
        public abstract void OnApply(RelicManager manager);

        public virtual void OnRemove(RelicManager manager)
        {
            // 聚合值每次都会从零重算，因此移除时无需手工回滚。
        }
    }
}
