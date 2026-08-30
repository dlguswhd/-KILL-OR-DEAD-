// [KILL OR DEAD] Combat Core
using UnityEngine;

namespace KillOrDead.Combat
{
    /// <summary>
    /// 한 발의 피격 정보. 배수가 적용되기 "전"의 기본 데미지를 담아 넘긴다.
    /// 배수 적용은 받는 쪽(EnemyHealth 등)의 책임.
    /// </summary>
    public struct DamageInfo
    {
        /// <summary> 무기 프로필의 기본 데미지 (거리 감쇠까지만 적용된 값) </summary>
        public float baseDamage;

        /// <summary> 맞은 부위. Hitbox가 채워준다. </summary>
        public BodyPartType bodyPart;

        public Vector3 hitPoint;
        public Vector3 hitNormal;
        /// <summary> 탄이 날아간 방향(정규화) </summary>
        public Vector3 direction;

        /// <summary> 쏜 주체 (플레이어 or 적 GameObject) </summary>
        public GameObject instigator;

        /// <summary> 발사지점 ~ 피격지점 거리(m) </summary>
        public float distance;

        /// <summary> 물리 반응용 충격량 </summary>
        public float impactForce;

        public static DamageInfo Simple(float damage, BodyPartType part = BodyPartType.Chest)
        {
            return new DamageInfo
            {
                baseDamage = damage,
                bodyPart = part,
                hitNormal = Vector3.up,
                direction = Vector3.forward,
            };
        }
    }
}
