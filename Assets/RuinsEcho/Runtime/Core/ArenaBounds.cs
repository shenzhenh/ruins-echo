using UnityEngine;

namespace RuinsEcho.Core
{
    /// <summary>
    /// 灰色盒技术切片的场地边界。正式房间系统接入后由房间边界替换。
    /// </summary>
    public static class ArenaBounds
    {
        public static Rect Bounds = new Rect(
            -GameConfig.ArenaHalfWidth,
            -GameConfig.ArenaHalfHeight,
            GameConfig.ArenaHalfWidth * 2f,
            GameConfig.ArenaHalfHeight * 2f);

        public static Vector2 Clamp(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, Bounds.xMin, Bounds.xMax),
                Mathf.Clamp(position.y, Bounds.yMin, Bounds.yMax));
        }

        public static void Reset()
        {
            Bounds = new Rect(
                -GameConfig.ArenaHalfWidth,
                -GameConfig.ArenaHalfHeight,
                GameConfig.ArenaHalfWidth * 2f,
                GameConfig.ArenaHalfHeight * 2f);
        }
    }
}
