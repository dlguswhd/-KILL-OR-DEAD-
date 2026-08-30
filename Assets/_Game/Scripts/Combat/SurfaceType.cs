// [KILL OR DEAD] Combat Core
namespace KillOrDead.Combat
{
    /// <summary>
    /// 총알이 맞은 표면 재질. WarFX Bullet Impact 프리팹 종류와 1:1로 대응한다.
    /// </summary>
    public enum SurfaceType
    {
        Concrete = 0,   // WFX_BImpact Concrete
        Metal    = 1,   // WFX_BImpact Metal
        Wood     = 2,   // WFX_BImpact Wood
        Dirt     = 3,   // WFX_BImpact Dirt
        Sand     = 4,   // WFX_BImpact Sand
        Flesh    = 5,   // WFX_BImpact SoftBody
    }
}
