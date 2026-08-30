// [KILL OR DEAD] Attachments
using UnityEngine;

namespace KillOrDead.Attachments
{
    /// <summary>
    /// 전술 후레쉬. 기획서 Y키로 켜고 끈다.
    /// 로우폴리 팩의 Light_*.prefab 루트에 붙이고 Spot Light를 연결한다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Attachments/Tactical Flashlight")]
    public class TacticalFlashlight : MonoBehaviour
    {
        [Tooltip("비워두면 자식에서 Light를 찾는다.")]
        [SerializeField] private Light spotLight;

        [Tooltip("렌즈에 붙은 발광 메쉬 같은 것. 없으면 비워둬도 된다.")]
        [SerializeField] private GameObject glowObject;

        [Header("설정")]
        [SerializeField] private float intensity = 8f;
        [SerializeField] private float range = 40f;
        [SerializeField] private float spotAngle = 35f;

        private bool _isOn;
        public bool IsOn => _isOn;

        private void Awake()
        {
            if (spotLight == null) spotLight = GetComponentInChildren<Light>(true);

            if (spotLight != null)
            {
                spotLight.type = LightType.Spot;
                spotLight.intensity = intensity;
                spotLight.range = range;
                spotLight.spotAngle = spotAngle;
            }
            SetOn(false);
        }

        public void Toggle() => SetOn(!_isOn);

        public void SetOn(bool on)
        {
            _isOn = on;
            if (spotLight != null) spotLight.enabled = on;
            if (glowObject != null) glowObject.SetActive(on);
        }
    }
}
