// [KILL OR DEAD] Attachments
using System;
using System.Collections.Generic;
using KillOrDead.Weapons;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.TacticalShooterPack.Scripts.Weapon;
using UnityEngine;

namespace KillOrDead.Attachments
{
    /// <summary>
    /// 무기 한 정의 부착물을 총괄한다. 무기 프리팹의 TacticalShooterWeapon과 같은 오브젝트에 붙인다.
    ///
    /// 하는 일:
    ///   - 슬롯(AttachmentSocket) 탐색 - 레일 위의 슬롯까지 계층적으로 전부 찾는다
    ///   - 부착물 장착/해제
    ///   - 조준경을 달면 조준점을 그쪽으로 넘긴다 (TSP가 매 프레임 다시 읽으므로 즉시 반영됨)
    ///   - 전방그립을 달면 왼손 IK 위치를 바꾼다
    ///   - 소음기를 달면 TSP의 소음 발사음으로 전환
    ///   - 반동/조준속도/탄퍼짐/FOV 합산해서 반영
    ///
    /// 중요: TacticalWeaponSettings는 여러 무기가 공유하는 에셋이라
    /// 런타임에 그냥 고치면 에디터의 원본까지 영구 변경된다.
    /// 그래서 Awake에서 복제본을 만들어 그 복제본만 건드린다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Attachments/Weapon Attachment System")]
    [DefaultExecutionOrder(-500)]
    public class WeaponAttachmentSystem : MonoBehaviour
    {
        [Header("디버그")]
        [SerializeField] private bool logChanges = true;

        [Tooltip("끄면 설정 에셋을 복제하지 않는다. 조준경 FOV와 그립 왼손 보정이 동작하지 않게 된다.")]
        [SerializeField] private bool cloneSettingsAtRuntime = true;

        private TacticalShooterWeapon _weapon;
        private WeaponBallistics _ballistics;

        private TacticalWeaponSettings _runtimeSettings;
        private Transform _defaultAimPoint;
        private KTransform _defaultLeftHandOffset;
        private float _defaultAimingSpeed;
        private float _defaultAimFov;

        private readonly List<AttachmentSocket> _sockets = new List<AttachmentSocket>();
        private readonly List<AttachmentInstance> _instances = new List<AttachmentInstance>();

        public WeaponModStats Stats { get; private set; } = WeaponModStats.Identity;

        /// <summary> Awake를 지났는가. 에디터에서 실수로 호출됐을 때 원본을 망가뜨리지 않기 위한 안전장치. </summary>
        private bool _initialized;

        /// <summary> 부착물 구성이 바뀔 때마다 발생. 모딩 UI가 구독하면 된다. </summary>
        public event Action OnLoadoutChanged;

        private void Awake()
        {
            _weapon = GetComponent<TacticalShooterWeapon>();
            if (_weapon == null) _weapon = GetComponentInChildren<TacticalShooterWeapon>(true);

            if (_weapon == null)
            {
                Debug.LogError($"[부착물] '{name}' 아래에 TacticalShooterWeapon이 없습니다. " +
                               $"이 컴포넌트를 끕니다.", this);
                enabled = false;
                return;
            }

            _ballistics = GetComponent<WeaponBallistics>();
            if (_ballistics == null) _ballistics = GetComponentInChildren<WeaponBallistics>(true);

            _defaultAimPoint = _weapon.GetAimPoint();

            if (cloneSettingsAtRuntime && _weapon.tacWeaponSettings != null)
            {
                // 원본 에셋 보호용 복제. 씬을 나가면 사라지는 임시 사본이다.
                _runtimeSettings = Instantiate(_weapon.tacWeaponSettings);
                _runtimeSettings.name = _weapon.tacWeaponSettings.name + " (런타임)";
                _weapon.tacWeaponSettings = _runtimeSettings;
            }
            else
            {
                _runtimeSettings = _weapon.tacWeaponSettings;
            }

            if (_runtimeSettings != null)
            {
                _defaultLeftHandOffset = _runtimeSettings.leftHandOffset;
                _defaultAimingSpeed = _runtimeSettings.aimingSpeed;
                _defaultAimFov = _runtimeSettings.aimFov;
            }

            _initialized = true;

            RefreshSockets();
            InstallDefaults();
        }

        // ── 슬롯 ──────────────────────────────────────────────────────
        /// <summary>
        /// 슬롯 목록을 다시 훑는다. 부착물이 붙거나 떨어질 때마다 호출된다.
        /// 부착물은 슬롯의 자식으로 들어가므로 GetComponentsInChildren 하나로
        /// 레일 위의 슬롯까지 전부 잡힌다.
        /// </summary>
        public void RefreshSockets()
        {
            _sockets.Clear();
            GetComponentsInChildren(true, _sockets);

            _instances.Clear();
            GetComponentsInChildren(true, _instances);
        }

        public IReadOnlyList<AttachmentSocket> Sockets => _sockets;
        public IReadOnlyList<AttachmentInstance> Attachments => _instances;

        public AttachmentSocket FindSocket(string socketKey)
        {
            foreach (var socket in _sockets)
                if (socket.SocketKey == socketKey) return socket;
            return null;
        }

        private void InstallDefaults()
        {
            // 레일을 달면 그 위에 새 슬롯이 생기고, 그 슬롯에도 기본 부착물이 있을 수 있다.
            // 그래서 새로 생긴 슬롯이 없을 때까지 반복한다.
            for (int pass = 0; pass < 5; pass++)
            {
                bool attachedAny = false;
                var snapshot = new List<AttachmentSocket>(_sockets);

                foreach (var socket in snapshot)
                {
                    if (socket == null || socket.DefaultAttachment == null || socket.IsOccupied) continue;
                    if (Attach(socket, socket.DefaultAttachment, rebuild: false) != null) attachedAny = true;
                }

                RefreshSockets();
                if (!attachedAny) break;
            }

            Rebuild();
        }

        // ── 장착 / 해제 ───────────────────────────────────────────────
        public bool CanAttach(AttachmentSocket socket, AttachmentDefinition definition)
        {
            return socket != null && definition != null
                && definition.prefab != null && socket.Accepts(definition);
        }

        /// <summary> 슬롯에 부착물을 단다. 이미 뭔가 달려 있으면 교체한다. </summary>
        public AttachmentInstance Attach(AttachmentSocket socket, AttachmentDefinition definition, bool rebuild = true)
        {
            if (!CanAttach(socket, definition))
            {
                if (logChanges)
                    Debug.LogWarning($"[부착물] '{socket?.DisplayName}' 슬롯에 " +
                                     $"'{definition?.displayName}'을 달 수 없습니다.", this);
                return null;
            }

            if (socket.IsOccupied) Detach(socket, rebuild: false);

            var go = Instantiate(definition.prefab, socket.transform);
            go.name = $"ATT_{definition.id}";

            go.transform.localPosition = definition.localPositionOffset;
            go.transform.localRotation = Quaternion.Euler(definition.localRotationOffset);
            go.transform.localScale = Vector3.one * definition.scaleMultiplier;

            var instance = go.GetComponent<AttachmentInstance>();
            if (instance == null) instance = go.AddComponent<AttachmentInstance>();
            instance.Initialize(definition, socket);

            socket.Current = instance;

            if (logChanges)
                Debug.Log($"[부착물] {socket.DisplayName} <- {definition.displayName}", this);

            if (rebuild)
            {
                RefreshSockets();
                Rebuild();
            }
            return instance;
        }

        /// <summary> 슬롯에서 부착물을 뗀다. 그 위에 얹혀 있던 것들도 같이 사라진다. </summary>
        public void Detach(AttachmentSocket socket, bool rebuild = true)
        {
            if (socket == null || !socket.IsOccupied) return;

            if (logChanges)
                Debug.Log($"[부착물] {socket.DisplayName} 에서 " +
                          $"{socket.Current.Definition?.displayName} 제거", this);

            var go = socket.Current.gameObject;
            socket.Current = null;

            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);

            if (rebuild)
            {
                RefreshSockets();
                Rebuild();
            }
        }

        [ContextMenu("전부 해제")]
        public void DetachAll()
        {
            var snapshot = new List<AttachmentSocket>(_sockets);
            foreach (var socket in snapshot)
                if (socket != null && socket.IsOccupied) Detach(socket, rebuild: false);

            RefreshSockets();
            Rebuild();
        }

        // ── 재계산 ────────────────────────────────────────────────────
        /// <summary> 달려 있는 부착물 전부를 다시 합산하고 무기에 반영한다. </summary>
        public void Rebuild()
        {
            // Awake 전(에디터 우클릭 메뉴 등)에 돌면 기본값을 캐싱하지 못한 상태라
            // 무기 프리팹의 조준점을 null로 덮어쓸 수 있다. 막는다.
            if (!_initialized || _weapon == null) return;

            var stats = WeaponModStats.Identity;

            Transform bestAimPoint = null;
            int bestPriority = int.MinValue;
            bool overrideLeftHand = false;
            Vector3 leftHandPos = Vector3.zero, leftHandRot = Vector3.zero;

            foreach (var instance in _instances)
            {
                var def = instance.Definition;
                if (def == null) continue;

                stats.recoilMultiplier    *= def.recoilMultiplier;
                stats.aimSpeedMultiplier  *= def.aimSpeedMultiplier;
                stats.hipSpreadMultiplier *= def.hipSpreadMultiplier;
                stats.aimSpreadMultiplier *= def.aimSpreadMultiplier;
                stats.ergonomics          += def.ergonomicsDelta;
                stats.weightKg            += def.weightKg;

                if (def.isSuppressor)
                {
                    stats.hasSuppressor = true;
                    stats.noiseRadiusMultiplier *= def.noiseRadiusMultiplier;
                }

                if (def.overrideAimPoint && instance.AimPoint != null && def.aimPriority > bestPriority)
                {
                    bestPriority = def.aimPriority;
                    bestAimPoint = instance.AimPoint;
                    if (def.aimFovOverride > 0f) stats.aimFovOverride = def.aimFovOverride;
                }

                if (def.overrideLeftHandOffset)
                {
                    overrideLeftHand = true;
                    leftHandPos = def.leftHandPosition;
                    leftHandRot = def.leftHandRotation;
                }
            }

            Stats = stats;

            ApplyAimPoint(bestAimPoint);
            ApplyLeftHand(overrideLeftHand, leftHandPos, leftHandRot);
            ApplySettings(stats);
            ApplySuppressor(stats.hasSuppressor);

            if (_ballistics != null) _ballistics.ApplyModStats(stats);

            OnLoadoutChanged?.Invoke();
        }

        private void ApplyAimPoint(Transform attachmentAimPoint)
        {
            // TacticalShooterPlayer.Update()가 GetAimPoint()를 매 프레임 읽으므로
            // 여기서 바꿔주면 다음 프레임부터 바로 새 조준경에 정렬된다.
            _weapon.SetAimPoint(attachmentAimPoint != null ? attachmentAimPoint : _defaultAimPoint);
        }

        private void ApplyLeftHand(bool doOverride, Vector3 position, Vector3 rotationEuler)
        {
            if (_runtimeSettings == null) return;

            if (!doOverride)
            {
                _runtimeSettings.leftHandOffset = _defaultLeftHandOffset;
                return;
            }

            // KTransform은 struct라서 꺼내서 고치고 다시 넣어야 한다.
            var offset = _defaultLeftHandOffset;
            offset.position = position;
            offset.rotation = Quaternion.Euler(rotationEuler);
            _runtimeSettings.leftHandOffset = offset;
        }

        private void ApplySettings(WeaponModStats stats)
        {
            if (_runtimeSettings == null) return;

            _runtimeSettings.aimingSpeed = _defaultAimingSpeed * stats.aimSpeedMultiplier;
            _runtimeSettings.aimFov = stats.aimFovOverride > 0f ? stats.aimFovOverride : _defaultAimFov;
        }

        private void ApplySuppressor(bool suppressed)
        {
            if (suppressed) _weapon.AttachSuppressor();
            else _weapon.DetachSuppressor();
        }

        // ── 레이저 / 전술등 (기획서 T키 / Y키) ─────────────────────────
        [ContextMenu("레이저 토글")]
        public void ToggleLaser()
        {
            foreach (var instance in _instances)
                if (instance.Laser != null) instance.Laser.Toggle();
        }

        [ContextMenu("전술등 토글")]
        public void ToggleFlashlight()
        {
            foreach (var instance in _instances)
                if (instance.Flashlight != null) instance.Flashlight.Toggle();
        }

        public bool HasLaser()
        {
            foreach (var instance in _instances)
                if (instance.Laser != null) return true;
            return false;
        }

        public bool HasFlashlight()
        {
            foreach (var instance in _instances)
                if (instance.Flashlight != null) return true;
            return false;
        }

        // ── 저장 / 불러오기 ───────────────────────────────────────────
        [Serializable]
        public struct SavedLoadout
        {
            public string[] socketKeys;
            public string[] attachmentIds;
        }

        /// <summary> 현재 구성을 저장 가능한 형태로 뽑는다. </summary>
        public SavedLoadout Save()
        {
            var keys = new List<string>();
            var ids = new List<string>();

            foreach (var socket in _sockets)
            {
                if (!socket.IsOccupied || socket.Current.Definition == null) continue;
                keys.Add(socket.SocketKey);
                ids.Add(socket.Current.Definition.id);
            }

            return new SavedLoadout { socketKeys = keys.ToArray(), attachmentIds = ids.ToArray() };
        }

        /// <summary>
        /// 저장된 구성을 되살린다. 레일 위에 얹힌 부착물이 있을 수 있으므로
        /// 슬롯이 새로 생길 때까지 여러 번 훑는다.
        /// </summary>
        public void Load(SavedLoadout loadout, Func<string, AttachmentDefinition> resolver)
        {
            if (loadout.socketKeys == null || resolver == null) return;

            DetachAll();

            var pending = new List<int>();
            for (int i = 0; i < loadout.socketKeys.Length; i++) pending.Add(i);

            // 레일 -> 레일 위 옵틱 순서로 붙어야 하므로 반복해서 시도한다.
            for (int pass = 0; pass < 5 && pending.Count > 0; pass++)
            {
                RefreshSockets();

                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    int index = pending[i];
                    var socket = FindSocket(loadout.socketKeys[index]);
                    if (socket == null) continue;

                    var definition = resolver(loadout.attachmentIds[index]);
                    if (definition == null) { pending.RemoveAt(i); continue; }

                    if (Attach(socket, definition, rebuild: false) != null)
                        pending.RemoveAt(i);
                }
            }

            RefreshSockets();
            Rebuild();

            if (pending.Count > 0 && logChanges)
                Debug.LogWarning($"[부착물] 구성 복원 중 {pending.Count}개를 붙이지 못했습니다.", this);
        }
    }
}
