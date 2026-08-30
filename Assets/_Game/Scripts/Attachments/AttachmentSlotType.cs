// [KILL OR DEAD] Attachments
namespace KillOrDead.Attachments
{
    /// <summary>
    /// 부착물의 종류. 소켓은 여러 종류를 동시에 받을 수 있다
    /// (예: 상부 레일 = Optic + Laser + Light).
    /// </summary>
    public enum AttachmentSlotType
    {
        Optic       = 0,   // 스코프 / 도트 / 아이언사이트
        Muzzle      = 1,   // 소음기 / 소염기 / 브레이크
        UnderBarrel = 2,   // 전방그립 / 총검 / 바이포드
        Laser       = 3,   // 레이저 사이트 (기획서 T키)
        Light       = 4,   // 전술 후레쉬 (기획서 Y키)
        Rail        = 5,   // 레일 - 달면 그 위에 새 슬롯이 생긴다
        Magazine    = 6,
        Stock       = 7,
        PistolGrip  = 8,
        Handguard   = 9,
        Other       = 10,
    }

    public static class AttachmentSlotTypeExtensions
    {
        public static string ToKorean(this AttachmentSlotType t) => t switch
        {
            AttachmentSlotType.Optic       => "조준경",
            AttachmentSlotType.Muzzle      => "총구",
            AttachmentSlotType.UnderBarrel => "총열하부",
            AttachmentSlotType.Laser       => "레이저",
            AttachmentSlotType.Light       => "전술등",
            AttachmentSlotType.Rail        => "레일",
            AttachmentSlotType.Magazine    => "탄창",
            AttachmentSlotType.Stock       => "개머리판",
            AttachmentSlotType.PistolGrip  => "권총손잡이",
            AttachmentSlotType.Handguard   => "총열덮개",
            _ => "기타",
        };
    }
}
