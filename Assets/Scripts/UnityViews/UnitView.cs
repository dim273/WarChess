using System;
using VContainer;
using WarChess.Configuration;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WarChess.Domain;
using WarChess.Presentation;
using WarChess.UnityViews;

namespace WarChess.UnityViews
{
    /// <summary>
    /// 角色的 Unity View，持有 Inspector 引用并执行移动、攻击、受击和防御表现。
    /// 业务状态与合法性仍由 UnitModel 和 Service 决定。
    /// </summary>
    public sealed class UnitView : MonoBehaviour, IUnitView
    {
        [Header("Identity")]
        [SerializeField] private string unitId = "unit-1";
        [SerializeField] private Team team;
        [SerializeField] private int initialX;
        [SerializeField] private int initialZ;

        [Tooltip("勾选后从现有场景位置推导初始格；否则使用 Initial X/Z。")]
        [SerializeField] private bool useScenePosition = true;
        [SerializeField] private UnitDefinitionAsset definitionAsset;

        [Header("Rules")]
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(1)] private int maxActionPoints = 2;
        [SerializeField, Min(0)] private int moveRange = 4;
        [SerializeField, Min(0)] private int attackRange = 3;
        [SerializeField, Min(0)] private int attackDamage = 40;
        [SerializeField]
        private UnitActionType[] actions =
        {
            UnitActionType.Move,
            UnitActionType.Attack,
            UnitActionType.Defend
        };

        [Header("Presentation")]
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject selectedVisual;
        [SerializeField] private Image healthFill;
        [SerializeField] private TextMeshProUGUI actionPointsText;
        [SerializeField] private Collider selectionCollider;
        [SerializeField, Min(0.01f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float attackHitDelay = 0.35f;
        [SerializeField, Min(0f)] private float attackDuration = 0.7f;
        [SerializeField, Min(0f)] private float defendDuration = 0.5f;

        [Tooltip("旧 Role_A/Role_B 模型正面为局部 -Z 时勾选；Enemy 不勾选。")]
        [SerializeField] private bool invertForward;

        [Header("Animator parameters")]
        [SerializeField] private string walkingParameter = "Walking";
        [SerializeField] private string attackParameter = "Attacking";
        [SerializeField] private string hitParameter = "Hit";
        [SerializeField] private string defendParameter = "Defending";
        [SerializeField] private string deathParameter = "Dead";

        private BoardView _boardView;
        private Coroutine _animationRoutine;
        private int _walkingHash;
        private int _attackHash;
        private int _hitHash;
        private int _defendHash;
        private int _deathHash;
        private Action _finish;
        private Action _impact;
        private Vector3? _finalPosition;
        private readonly HashSet<int> _boolParameters = new HashSet<int>();
        private readonly HashSet<int> _triggerParameters = new HashSet<int>();

        public UnitId UnitId => new UnitId(unitId);

        /// <summary>只读取配置；必须先注入 BoardView，才能按场景坐标转换。</summary>
        public UnitDefinition Definition
        {
            get
            {
                GridCoord position = useScenePosition
                    ? _boardView.WorldToGrid(transform.position) : new GridCoord(initialX, initialZ);
                return definitionAsset != null
                    ? definitionAsset.CreateRuntimeDefinition(UnitId, team, position)
                    : new UnitDefinition(UnitId, team, position, maxHealth, maxActionPoints,
                        moveRange, attackRange, attackDamage, actions);
            }
        }

        private void Awake()
        {
            // Animator 参数在初始化时转为哈希，避免播放过程中重复计算字符串哈希。
            _walkingHash = Animator.StringToHash(walkingParameter);
            _attackHash = Animator.StringToHash(attackParameter);
            _hitHash = Animator.StringToHash(hitParameter);
            _defendHash = Animator.StringToHash(defendParameter);
            _deathHash = Animator.StringToHash(deathParameter);
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.type == AnimatorControllerParameterType.Bool) _boolParameters.Add(parameter.nameHash);
                    if (parameter.type == AnimatorControllerParameterType.Trigger) _triggerParameters.Add(parameter.nameHash);
                }
            }
        }

        private void OnDisable() => CancelAnimation();

        /// <summary>中断时对齐已结算结果，并且只完成一次，避免 Presenter 永远停留在 Animating。</summary>
        public void CancelAnimation()
        {
            if (this == null) return;
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _animationRoutine = null;
            FinishAnimation();
        }

        [Inject]
        public void BindBoard(BoardView boardView)
        {
            // 由 Composition Root 显式注入坐标转换依赖，不在运行时搜索场景对象。
            _boardView = boardView ?? throw new ArgumentNullException(nameof(boardView));
        }

        public void Initialize(GridCoord position)
        {
            if (_boardView == null) throw new InvalidOperationException("UnitView is not bound to a BoardView.");
            transform.position = _boardView.GridToWorld(position);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedVisual != null)
            {
                selectedVisual.SetActive(selected);
            }
        }

        public void SetHealth(int current, int maximum)
        {
            if (healthFill != null)
            {
                healthFill.fillAmount = maximum <= 0 ? 0f : (float)current / maximum;
            }
        }

        public void SetActionPoints(int current, int maximum)
        {
            if (actionPointsText != null)
            {
                actionPointsText.text = current.ToString();
            }
        }

        public void PlayMove(IReadOnlyList<GridCoord> path, Action completed)
        {
            PrepareAnimation(completed);
            if (path != null && path.Count > 0) _finalPosition = _boardView.GridToWorld(path[path.Count - 1]);
            StartAnimation(MoveRoutine(path, FinishAnimation));
        }

        public void PlayAttack(GridCoord target, Action hitMoment, Action completed)
        {
            PrepareAnimation(completed);
            _impact = hitMoment;
            StartAnimation(AttackRoutine(target, InvokeImpact, FinishAnimation));
        }

        public void PlayDefend(Action completed)
        {
            PrepareAnimation(completed);
            StartAnimation(DefendRoutine(FinishAnimation));
        }

        public void PlayHit(int damage, bool died)
        {
            // 场景卸载中其他单位可能先销毁；迟到的纯视觉回调直接忽略。
            if (this == null) return;
            Trigger(died ? _deathHash : _hitHash);

            if (died)
            {
                // 保留 GameObject 以便死亡动画继续播放，但禁止它再接受选择射线。
                SetSelected(false);
                if (selectionCollider != null)
                {
                    selectionCollider.enabled = false;
                }
                // 兼容现有预制体的多个角色碰撞体，防止尸体继续拦截点击。
                foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
                if (!_triggerParameters.Contains(_deathHash))
                {
                    // 当前控制器没有死亡动画时隐藏尸体显示，避免零血量单位看起来仍然存活。
                    foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                    foreach (Canvas canvas in GetComponentsInChildren<Canvas>()) canvas.enabled = false;
                }
            }
        }

        private void PrepareAnimation(Action completed)
        {
            CancelAnimation();
            _finish = completed;
        }

        private void InvokeImpact()
        {
            Action impact = _impact;
            _impact = null;
            impact?.Invoke();
        }

        private void FinishAnimation()
        {
            if (_finalPosition.HasValue) transform.position = _finalPosition.Value;
            _finalPosition = null;
            SetWalking(false);
            InvokeImpact();
            Action finish = _finish;
            _finish = null;
            _animationRoutine = null;
            finish?.Invoke();
        }

        private void Trigger(int hash)
        {
            // 现有 Animator 只有 Walking/Attacking，缺少可选触发器时安全跳过。
            if (animator != null && _triggerParameters.Contains(hash)) animator.SetTrigger(hash);
        }

        private void SetWalking(bool walking)
        {
            if (animator != null && _boolParameters.Contains(_walkingHash)) animator.SetBool(_walkingHash, walking);
        }

        private void StartAnimation(IEnumerator routine)
        {
            // 同一角色同一时间只允许存在一条主表现协程。
            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
            }

            if (!isActiveAndEnabled) { FinishAnimation(); return; }
            _animationRoutine = StartCoroutine(routine);
        }

        private IEnumerator MoveRoutine(IReadOnlyList<GridCoord> path, Action completed)
        {
            SetWalking(true);

            if (_boardView != null && path != null)
            {
                // 路径第一个节点是当前位置，因此从索引 1 开始移动。
                for (int i = 1; i < path.Count; i++)
                {
                    Vector3 target = _boardView.GridToWorld(path[i]);
                    while ((transform.position - target).sqrMagnitude > 0.0001f)
                    {
                        Vector3 direction = target - transform.position;
                        if (direction.sqrMagnitude > 0.0001f)
                        {
                            transform.forward = Vector3.Slerp(
                                transform.forward,
                                direction.normalized * (invertForward ? -1f : 1f),
                                Time.deltaTime * 12f);
                        }

                        transform.position = Vector3.MoveTowards(
                            transform.position,
                            target,
                            Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);
                        yield return null;
                    }

                    transform.position = target;
                }
            }

            SetWalking(false);
            _animationRoutine = null;
            completed?.Invoke();
        }

        private IEnumerator AttackRoutine(GridCoord target, Action hitMoment, Action completed)
        {
            if (_boardView != null)
            {
                Vector3 direction = _boardView.GridToWorld(target) - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    transform.forward = direction.normalized * (invertForward ? -1f : 1f);
                }
            }

            Trigger(_attackHash);

            // hitMoment 只决定视觉反馈时机，业务伤害在 Service 执行阶段已经确定。
            if (attackHitDelay > 0f) yield return new WaitForSeconds(attackHitDelay);
            hitMoment?.Invoke();

            float remaining = Mathf.Max(0f, attackDuration - attackHitDelay);
            if (remaining > 0f) yield return new WaitForSeconds(remaining);

            _animationRoutine = null;
            completed?.Invoke();
        }

        private IEnumerator DefendRoutine(Action completed)
        {
            Trigger(_defendHash);
            if (defendDuration > 0f) yield return new WaitForSeconds(defendDuration);
            _animationRoutine = null;
            completed?.Invoke();
        }
    }
}
