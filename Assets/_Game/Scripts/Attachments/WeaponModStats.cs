// [KILL OR DEAD] Attachments
namespace KillOrDead.Attachments
{
    /// <summary>
    /// 장착된 부착물 전체를 합산한 결과. 매 장착/해제마다 다시 계산된다.
    /// </summary>
    public struct WeaponModStats
    {
        public float recoilMultiplier;
        public float aimSpeedMultiplier;
        public float hipSpreadMultiplier;
        public float aimSpreadMultiplier;

        /// <summary> 조준 시 FOV. 0이면 무기 기본값 사용 </summary>
        public float aimFovOverride;

        /// <summary> 타르코프식 인체공학 점수. 높을수록 다루기 편함 </summary>
        public int ergonomics;
        public float weightKg;

        public bool hasSuppressor;

        /// <summary> 격발 소음 반경 배율. 기획서 의심도 시스템에서 사용 </summary>
        public float noiseRadiusMultiplier;

        public static WeaponModStats Identity => new WeaponModStats
        {
            recoilMultiplier     = 1f,
            aimSpeedMultiplier   = 1f,
            hipSpreadMultiplier  = 1f,
            aimSpreadMultiplier  = 1f,
            aimFovOverride       = 0f,
            ergonomics           = 0,
            weightKg             = 0f,
            hasSuppressor        = false,
            noiseRadiusMultiplier = 1f,
        };
    }
}
