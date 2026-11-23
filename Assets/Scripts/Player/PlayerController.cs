using UnityEngine;
using AesikHaiman.Systems;
using AesikHaiman.Game;
using AesikHaiman.Combat;
using AesikHaiman.Audio;

namespace AesikHaiman.Player
{
    /// <summary>
    /// 좌우 이동 / 점프(2단 점프) / 기본 공격 콤보를 처리하는 캐릭터 컨트롤러.
    ///
    /// Rigidbody2D를 선택한 이유:
    /// - 이 게임은 2D 횡스크롤 액션이므로 3D용 CharacterController보다 2D 물리(Rigidbody2D + Collider2D)가 적합.
    /// - 넉백(AddForce/velocity 조작), 중력, 플랫폼 충돌을 엔진의 2D 물리 엔진이 그대로 처리해줘서
    ///   직접 충돌/중력 계산을 구현하는 커스텀 키네마틱 컨트롤러보다 구현 비용이 낮음.
    /// - 이후 넉백형 스킬, 강제 이동기(대시/블링크성 스킬) 등을 추가할 때도 AddForce/velocity로 자연스럽게 확장 가능.
    /// - 다만 정밀한 픽셀 단위 판정이 필요해지면(프레임 데이터 기반 격투 로직 등) 추후 커스텀 키네마틱 이동으로
    ///   교체할 수 있도록 이동 로직을 FixedUpdate 한 곳에 모아둠.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float moveSpeed = 6f;
        [Tooltip("키보드 화살표와 숫자패드(Num Lock 꺼졌을 때 화살표 신호를 보내는 키패드 4/6)를 모두 인식합니다.")]
        [SerializeField] private KeyCode[] leftKeys = { KeyCode.LeftArrow, KeyCode.Keypad4 };
        [SerializeField] private KeyCode[] rightKeys = { KeyCode.RightArrow, KeyCode.Keypad6 };

        [Header("점프")]
        [SerializeField] private float jumpForce = 12f;
        [Tooltip("위쪽 화살표/숫자패드 8 외에 Space도 함께 인식합니다.")]
        [SerializeField] private KeyCode[] jumpKeys = { KeyCode.UpArrow, KeyCode.Keypad8, KeyCode.Space };
        [Tooltip("엘소드 스타일 액션 캐릭터는 대부분 2단 점프를 보유하므로 기본값 true로 설정. " +
                 "지상 전용 캐릭터로 만들고 싶다면 false로 변경.")]
        [SerializeField] private bool allowDoubleJump = true;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        [Header("기본 공격 콤보")]
        [SerializeField] private KeyCode attackKey = KeyCode.Z;
        [Tooltip("콤보 각 타격 사이에 다음 입력을 받아줄 수 있는 시간(초). " +
                 "이 시간 안에 다시 공격 입력이 들어오면 다음 타로 이어지고, 넘어가면 1타로 초기화됩니다.")]
        [SerializeField] private float comboResetTime = 0.8f;
        [Tooltip("콤보 단수 및 임시 데미지값. 이후 스킬로 콤보 마무리 연계를 추가할 때 이 배열 길이를 늘리면 됩니다.")]
        [SerializeField] private float[] comboDamages = { 10f, 12f, 18f };
        [Tooltip("공격 판정용 히트박스. 공격 입력이 들어올 때만 잠깐 켜졌다가 꺼집니다.")]
        [SerializeField] private AttackHitbox attackHitbox;
        [SerializeField] private float attackHitboxActiveDuration = 0.15f;

        [Header("특수 공격 - 우산 콤보 (X)")]
        [Tooltip("X 입력을 우산으로 처리: 1타(찌르기) → 콤보 유지시간 안에 재입력하면 2타(펼치기) → " +
                 "펼치기 상태에서 키를 계속 누르고 있으면 우산을 돌려 물방울을 연속 발사합니다.")]
        [SerializeField] private KeyCode specialAttackKey = KeyCode.X;
        [Tooltip("찌르기/펼치기 각 타 사이에 다음 입력을 받아줄 수 있는 시간(초). 기본 공격의 comboResetTime과 같은 역할.")]
        [SerializeField] private float specialComboWindow = 0.5f;
        [Tooltip("펼치기 상태에서 이 시간(초) 이상 키를 계속 누르고 있으면 회전(물방울 발사)으로 넘어갑니다.")]
        [SerializeField] private float spinHoldThreshold = 0.12f;
        [SerializeField] private float maxSpinDuration = 1.5f;
        [SerializeField] private float spinDropletInterval = 0.15f;
        [SerializeField] private float manaCostPerDroplet = 1f;
        [SerializeField] private float stabDamage = 8f;
        [SerializeField] private float openDamage = 10f;
        [SerializeField] private float dropletDamage = 4f;
        [Tooltip("물방울 발사는 물 관련 행동이라 발사할 때마다 강수량 게이지가 소량 축적됩니다.")]
        [SerializeField] private float dropletGaugeGain = 1f;
        [Tooltip("9주차 스프린트: 실제로 날아가는 물방울 발사체 프리팹. 비워두면 로그만 찍히고 투사체는 안 나갑니다.")]
        [SerializeField] private GameObject dropletProjectilePrefab;

        [Header("스테이지 경계 (11주차 스프린트: 화면 밖 이탈/벽 뚫기 방지)")]
        [Tooltip("물리적인 벽 콜라이더 대신 Rigidbody2D 위치를 직접 clamp해서 항상 이 범위 밖으로 못 나가게 막습니다.")]
        [SerializeField] private float stageMinX = -14f;
        [SerializeField] private float stageMaxX = 14f;
        [SerializeField] private float stageMinY = -5f;
        [SerializeField] private float stageMaxY = 10f;

        private enum SpecialStage { None, Stab, Open, Spin }

        private Rigidbody2D _rb;
        private Animator _animator;
        private PlayerHealth _health;
        private BuffSystem _buffSystem; // 이동속도/공격력에 버프 배율을 곱하기 위해 참조 (없어도 동작)
        private RainGaugeSystem _gaugeSystem; // 우산 콤보가 강수량 배율을 받고 게이지를 축적하기 위해 참조

        private int _jumpsUsed;
        private bool _isGrounded;
        private float _moveInput;

        private int _comboIndex;
        private float _comboTimer;
        private bool _isAttacking;

        private SpecialStage _specialStage = SpecialStage.None;
        private float _specialComboTimer;
        private float _specialHoldTimer;
        private float _specialSpinTimer;
        private float _specialDropletTimer;

        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int AnimJumpTrigger = Animator.StringToHash("JumpTrigger");
        private static readonly int AnimAttackTrigger = Animator.StringToHash("AttackTrigger");
        private static readonly int AnimComboIndex = Animator.StringToHash("ComboIndex");
        private static readonly int AnimHitTrigger = Animator.StringToHash("HitTrigger");
        private static readonly int AnimSpecialStabTrigger = Animator.StringToHash("SpecialStabTrigger");
        private static readonly int AnimSpecialOpenTrigger = Animator.StringToHash("SpecialOpenTrigger");
        private static readonly int AnimSpecialSpinTrigger = Animator.StringToHash("SpecialSpinTrigger");

        public bool IsAttacking => _isAttacking;
        public bool IsGrounded => _isGrounded;
        public int CurrentComboIndex => _comboIndex;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _health = GetComponent<PlayerHealth>();
            _buffSystem = GetComponent<BuffSystem>();
            _gaugeSystem = GetComponent<RainGaugeSystem>();
        }

        private void Update()
        {
            // ===== 7/10주차 스프린트: 게임오버/게임클리어 중에는 조작 입력을 완전히 무시 (Time.timeScale=0만으로는
            //        Input 폴링 자체는 안 멈추기 때문에 명시적으로 막아줌) =====
            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsGameClear)) return;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

            if (_health != null && _health.IsKnockedBack)
            {
                // 넉백 중에는 이동/공격 입력을 받지 않음
                UpdateAnimator();
                return;
            }

            ReadMoveInput();
            HandleJumpInput();
            HandleAttackInput();
            HandleSpecialAttackInput();
            UpdateComboTimer();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            CheckGrounded();

            if (_health != null && _health.IsKnockedBack)
                return; // 넉백 중에는 넉백 힘을 이동 로직이 덮어쓰지 않도록 함

            // 공격 중에는 이동 속도를 줄여서 처리 (완전 정지가 아닌 딜레이 부여, 필요 시 조정)
            float speedFactor = _isAttacking ? 0.2f : 1f;
            // ===== 스파 가디언 이동속도 버프/디버프(예: 온천수)는 BuffSystem.GetStatMultiplier(StatType.MoveSpeed)로 자동 반영됨 =====
            float speedBuffMultiplier = _buffSystem != null ? _buffSystem.GetStatMultiplier(StatType.MoveSpeed) : 1f;
            _rb.linearVelocity = new Vector2(_moveInput * moveSpeed * speedFactor * speedBuffMultiplier, _rb.linearVelocity.y);

            ClampToStageBounds();
        }

        // ===== 11주차 스프린트: 화면 밖 이탈(벽 뚫기) 방지 - 물리 벽 대신 위치를 직접 clamp =====
        private void ClampToStageBounds()
        {
            Vector2 pos = _rb.position;
            Vector2 clamped = new Vector2(
                Mathf.Clamp(pos.x, stageMinX, stageMaxX),
                Mathf.Clamp(pos.y, stageMinY, stageMaxY));

            if (clamped != pos)
                _rb.position = clamped;
        }

        private void ReadMoveInput()
        {
            _moveInput = 0f;
            if (IsAnyKeyHeld(leftKeys)) _moveInput -= 1f;
            if (IsAnyKeyHeld(rightKeys)) _moveInput += 1f;

            if (_moveInput != 0f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Sign(_moveInput) * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        private void CheckGrounded()
        {
            bool wasGrounded = _isGrounded;

            if (groundCheck != null)
                _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            else
                _isGrounded = Mathf.Abs(_rb.linearVelocity.y) < 0.01f;

            if (_isGrounded && !wasGrounded)
                _jumpsUsed = 0;
        }

        private void HandleJumpInput()
        {
            if (!IsAnyKeyDown(jumpKeys)) return;

            int maxJumps = allowDoubleJump ? 2 : 1;
            if (_jumpsUsed >= maxJumps) return;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
            _jumpsUsed++;
            _animator?.SetTrigger(AnimJumpTrigger);
        }

        private void HandleAttackInput()
        {
            if (!Input.GetKeyDown(attackKey)) return;

            // 콤보 판정: comboResetTime 안에 재입력하면 다음 타로 진행, 시간이 지났으면 1타부터 다시 시작
            _comboIndex = _comboTimer <= 0f ? 0 : (_comboIndex + 1) % comboDamages.Length;

            _comboTimer = comboResetTime;
            _isAttacking = true;

            _animator?.SetInteger(AnimComboIndex, _comboIndex);
            _animator?.SetTrigger(AnimAttackTrigger);

            // ===== 스파 가디언 공격력 버프/디버프(예: 첨벙첨벙)는 BuffSystem.GetStatMultiplier(StatType.AttackPower)로 자동 반영됨 =====
            float attackBuffMultiplier = _buffSystem != null ? _buffSystem.GetStatMultiplier(StatType.AttackPower) : 1f;
            float finalDamage = comboDamages[_comboIndex] * attackBuffMultiplier;

            attackHitbox?.Activate(finalDamage, attackHitboxActiveDuration);

            Debug.Log($"[PlayerController] 공격 {_comboIndex + 1}타 (데미지 {finalDamage:0.0})");

            // TODO: 스킬 15종 중 "콤보 마무리 연계형" 스킬은 여기서 _comboIndex == comboDamages.Length - 1 조건을 참고해 구현
        }

        /// <summary>
        /// 우산 콤보 - X. 1타(찌르기) → 콤보 유지시간 안에 재입력하면 2타(펼치기) →
        /// 펼치기 상태에서 키를 떼지 않고 spinHoldThreshold초 이상 누르고 있으면 회전(물방울 연속 발사)으로 전환.
        /// 회전은 최대 maxSpinDuration초까지 지속되며, 물방울을 쏠 때마다 마나가 manaCostPerDroplet만큼 소모됩니다.
        /// ===== 우산이 애식과 정령의 힘을 함께 다루는 무기라는 설정에 맞춰, 회전 중 발사되는
        ///        물방울만 "물 관련 행동"으로 취급해 강수량 게이지를 소량 축적함 =====
        /// </summary>
        private void HandleSpecialAttackInput()
        {
            bool keyDown = Input.GetKeyDown(specialAttackKey);
            bool keyHeld = Input.GetKey(specialAttackKey);

            switch (_specialStage)
            {
                case SpecialStage.None:
                    if (keyDown) EnterStab();
                    break;

                case SpecialStage.Stab:
                    _specialComboTimer -= Time.deltaTime;
                    if (keyDown)
                        EnterOpen();
                    else if (_specialComboTimer <= 0f)
                        EndSpecialCombo();
                    break;

                case SpecialStage.Open:
                    if (keyHeld)
                    {
                        _specialHoldTimer += Time.deltaTime;
                        if (_specialHoldTimer >= spinHoldThreshold)
                            EnterSpin();
                    }
                    else
                    {
                        _specialComboTimer -= Time.deltaTime;
                        if (_specialComboTimer <= 0f)
                            EndSpecialCombo();
                    }
                    break;

                case SpecialStage.Spin:
                    _specialSpinTimer += Time.deltaTime;
                    _specialDropletTimer -= Time.deltaTime;

                    bool hasMana = _health == null || _health.CurrentMana >= manaCostPerDroplet;

                    if (!keyHeld || _specialSpinTimer >= maxSpinDuration || !hasMana)
                    {
                        EndSpecialCombo();
                        break;
                    }

                    if (_specialDropletTimer <= 0f)
                    {
                        FireDroplet();
                        _specialDropletTimer = spinDropletInterval;
                    }
                    break;
            }

            _isAttacking = _comboTimer > 0f || _specialStage != SpecialStage.None;
        }

        private void EnterStab()
        {
            _specialStage = SpecialStage.Stab;
            _specialComboTimer = specialComboWindow;
            _specialHoldTimer = 0f;

            _animator?.SetTrigger(AnimSpecialStabTrigger);

            float finalDamage = stabDamage * CurrentAttackMultiplier();
            Debug.Log($"[PlayerController] 우산 찌르기 (데미지 {finalDamage:0.0})");
            // TODO: 히트박스/판정 시스템을 붙일 때 finalDamage를 실제 데미지 계산에 사용
        }

        private void EnterOpen()
        {
            _specialStage = SpecialStage.Open;
            _specialComboTimer = specialComboWindow;
            _specialHoldTimer = 0f;

            _animator?.SetTrigger(AnimSpecialOpenTrigger);

            float finalDamage = openDamage * CurrentAttackMultiplier();
            Debug.Log($"[PlayerController] 우산 펼치기 (데미지 {finalDamage:0.0})");
        }

        private void EnterSpin()
        {
            _specialStage = SpecialStage.Spin;
            _specialSpinTimer = 0f;
            _specialDropletTimer = 0f; // 회전 시작 즉시 첫 물방울 발사

            _animator?.SetTrigger(AnimSpecialSpinTrigger);
            Debug.Log("[PlayerController] 우산 회전 시작 - 물방울 연속 발사");
        }

        private void FireDroplet()
        {
            _health?.ConsumeMana(manaCostPerDroplet);
            _gaugeSystem?.AddGauge(dropletGaugeGain);

            float finalDamage = dropletDamage * CurrentAttackMultiplier();

            // ===== 9주차 스프린트: 원거리 공격/발사체 시스템 - 실제로 날아가는 물방울 =====
            if (dropletProjectilePrefab != null)
            {
                Vector2 facing = new Vector2(Mathf.Sign(transform.localScale.x), 0f);
                Vector3 spawnPos = transform.position + (Vector3)(facing * 0.6f);
                GameObject projectileGO = Instantiate(dropletProjectilePrefab, spawnPos, Quaternion.identity);
                if (projectileGO.TryGetComponent(out Projectile projectile))
                    projectile.Launch(facing, finalDamage);
            }

            AudioManager.Instance?.PlayFireSfx();

            Debug.Log($"[PlayerController] 물방울 발사! 마나 -{manaCostPerDroplet:0} (남은 마나 {(_health != null ? _health.CurrentMana : 0):0}), 데미지 {finalDamage:0.0}");
        }

        private void EndSpecialCombo()
        {
            _specialStage = SpecialStage.None;
            _specialComboTimer = 0f;
            _specialHoldTimer = 0f;
            _specialSpinTimer = 0f;
            _specialDropletTimer = 0f;
        }

        // 강수량 게이지 배율 x 공격력 버프 배율. 콤보/우산 공격의 데미지 계산에서 공통으로 사용.
        private float CurrentAttackMultiplier()
        {
            float gaugeMultiplier = _gaugeSystem != null ? _gaugeSystem.GetGaugeMultiplier() : 1f;
            float attackBuffMultiplier = _buffSystem != null ? _buffSystem.GetStatMultiplier(StatType.AttackPower) : 1f;
            return gaugeMultiplier * attackBuffMultiplier;
        }

        private void UpdateComboTimer()
        {
            if (_comboTimer <= 0f) return;

            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f)
                _comboIndex = 0;
        }

        /// <summary>공격 애니메이션 이벤트 등에서 호출해 공격 상태를 즉시 종료시킬 때 사용.</summary>
        public void EndAttack()
        {
            _comboTimer = 0f;
            EndSpecialCombo();
            _isAttacking = false;
        }

        public void TriggerHitAnimation()
        {
            _animator?.SetTrigger(AnimHitTrigger);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;
            _animator.SetFloat(AnimSpeed, Mathf.Abs(_rb.linearVelocity.x));
            _animator.SetBool(AnimIsGrounded, _isGrounded);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // 여러 키 중 하나라도 눌려있는지/눌리는 순간인지 확인하는 헬퍼.
        // 화살표 키 고장 등에 대비해 숫자패드 등 대체 키를 함께 등록해두는 용도.
        private static bool IsAnyKeyHeld(KeyCode[] keys)
        {
            foreach (KeyCode key in keys)
                if (Input.GetKey(key)) return true;
            return false;
        }

        private static bool IsAnyKeyDown(KeyCode[] keys)
        {
            foreach (KeyCode key in keys)
                if (Input.GetKeyDown(key)) return true;
            return false;
        }
    }
}
