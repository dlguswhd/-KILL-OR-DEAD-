// [KILL OR DEAD] Combat Core
namespace KillOrDead.Combat
{
    /// <summary>
    /// 기획서 기준 신체 부위. 플레이어는 7부위 개별 HP, 적은 공유 HP + 부위별 배수.
    /// </summary>
    public enum BodyPartType
    {
        None = 0,
        Head = 1,
        Chest = 2,
        Abdomen = 3,
        LeftArm = 4,
        RightArm = 5,
        LeftLeg = 6,
        RightLeg = 7,
    }

    public static class BodyPartTypeExtensions
    {
        /// <summary> 팔 계열인가 (좌/우 통합 판정용) </summary>
        public static bool IsArm(this BodyPartType p) =>
            p == BodyPartType.LeftArm || p == BodyPartType.RightArm;

        /// <summary> 다리 계열인가 </summary>
        public static bool IsLeg(this BodyPartType p) =>
            p == BodyPartType.LeftLeg || p == BodyPartType.RightLeg;

        /// <summary> 플레이어 즉사 부위인가 (머리/흉부/복부) </summary>
        public static bool IsVital(this BodyPartType p) =>
            p == BodyPartType.Head || p == BodyPartType.Chest || p == BodyPartType.Abdomen;

        public static string ToKorean(this BodyPartType p) => p switch
        {
            BodyPartType.Head     => "머리",
            BodyPartType.Chest    => "흉부",
            BodyPartType.Abdomen  => "복부",
            BodyPartType.LeftArm  => "왼팔",
            BodyPartType.RightArm => "오른팔",
            BodyPartType.LeftLeg  => "왼다리",
            BodyPartType.RightLeg => "오른다리",
            _ => "없음",
        };
    }
}
