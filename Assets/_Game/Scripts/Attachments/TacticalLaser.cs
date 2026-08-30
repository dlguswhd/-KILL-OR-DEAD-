// [KILL OR DEAD] Attachments
using UnityEngine;

namespace KillOrDead.Attachments
{
    /// <summary>
    /// 레이저 사이트. 기획서 T키로 켜고 끈다.
    /// 로우폴리 팩의 Laser_*.prefab 루트에 붙이고, 렌즈 앞에 빈 오브젝트를 만들어 origin으로 지정한다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Attachments/Tactical Laser")]
    public class TacticalLaser : MonoBehaviour
    {
        [Header("발사 지점")]
        [Tooltip("비워두면 자기 트랜스폼. Z축이 총구 방향이어야 한다.")]
        [SerializeField] private Transform origin;

        [Header("빔")]
        [SerializeField] private LineRenderer beam;
        [SerializeField] private Color beamColor = new Color(1f, 0.1f, 0.1f);
        [SerializeField] private float beamWidth = 0.002f;
        [SerializeField] private float maxDistance = 100f;

        [Header("점")]
        [Tooltip("벽에 찍히는 레이저 점. 작은 Quad나 Sprite면 된다.")]
        [SerializeField] private Transform dot;
        [SerializeField] private float dotScale = 0.015f;

        [Header("충돌")]
        [SerializeField] private LayerMask hitMask = ~0;

        private bool _isOn;
        public bool IsOn => _isOn;

        private void Awake()
        {
            if (origin == null) origin = transform;

            if (beam != null)
            {
                beam.startWidth = beam.endWidth = beamWidth;
                beam.startColor = beam.endColor = beamColor;
                beam.positionCount = 2;
                beam.useWorldSpace = true;
            }
            SetOn(false);
        }

        public void Toggle() => SetOn(!_isOn);

        public void SetOn(bool on)
        {
            _isOn = on;
            if (beam != null) beam.enabled = on;
            if (dot != null) dot.gameObject.SetActive(on);
        }

        private void LateUpdate()
        {
            if (!_isOn) return;

            Vector3 start = origin.position;
            Vector3 direction = origin.forward;
            Vector3 end = start + direction * maxDistance;

            if (Physics.Raycast(start, direction, out var hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;

                if (dot != null)
                {
                    dot.position = hit.point + hit.normal * 0.004f;
                    dot.rotation = Quaternion.LookRotation(-hit.normal);
                    // 거리에 따라 점이 커지게 (실제 레이저처럼)
                    float scale = dotScale * Mathf.Max(1f, hit.distance * 0.35f);
                    dot.localScale = Vector3.one * scale;
                }
            }
            else if (dot != null)
            {
                dot.gameObject.SetActive(false);
            }

            if (beam != null)
            {
                beam.SetPosition(0, start);
                beam.SetPosition(1, end);
            }
        }
    }
}
