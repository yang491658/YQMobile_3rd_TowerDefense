using TMPro;
using UnityEngine;

[RequireComponent(typeof(MonsterDebuff))]
public class Monster : Pooling
{
	private static int sorting = 0;

	[Header("UI")]
	[SerializeField][Min(0f)] protected float scale = 0.7f;
	[SerializeField] private Canvas canvas;
	[SerializeField] private TextMeshProUGUI healthText;
	[Space]
	[SerializeField][Min(0f)] private float damageSpeed = 150f;
	[SerializeField][Min(0f)] private float damageDuration = 1.5f;

	[Header("Move")]
	[SerializeField] private Transform[] paths;
	[SerializeField][Min(0)] private int pathIndex;
	[SerializeField][Min(0f)] protected float moveSpeed = 3f;
	[SerializeField] private Vector3 moveDirection;

	public bool IsForward { private set; get; } = true;

	[Header("Battle")]
	[SerializeField][Min(0)] private int reserve = 0;
	[SerializeField][Min(0)] private int health;
	[SerializeField][Min(0)] protected int maxHealth;
	[Space]
	[SerializeField][Min(0)] private int gold;
	[Space]
	[SerializeField] private MonsterDebuff debuff;

	public bool IsDead { private set; get; } = false;

#if UNITY_EDITOR
	protected virtual void OnValidate()
	{
		if (canvas == null)
			canvas = GetComponentInChildren<Canvas>();
		if (healthText == null)
			healthText = canvas?.GetComponentInChildren<TextMeshProUGUI>();
		if (debuff == null)
			debuff = GetComponent<MonsterDebuff>();
	}
#endif

	protected override void Update()
	{
		base.Update();

		if (IsDead) return;

		float dt = Time.deltaTime;
		UpdateMove(dt);
	}

	#region 이동
	private void UpdateMove(float _deltaTime)
	{
		if (MonsterWave.Instance.IsPause)
		{ Stop(); return; }

		if (IsForward)
			MoveForward(_deltaTime);
		else
			MoveBackward(_deltaTime);
	}

	private void MoveForward(float _deltaTime)
	{
		if (pathIndex >= paths.Length)
		{ OnGoal(); return; }

		Vector3 delta = paths[pathIndex].position - transform.position;

		float arrive = Mathf.Max(moveSpeed * _deltaTime, 0.01f);
		if (delta.sqrMagnitude <= arrive * arrive)
		{
			transform.position = paths[pathIndex].position;

			if (++pathIndex >= paths.Length)
			{ Stop(); return; }

			delta = paths[pathIndex].position - transform.position;
		}

		moveDirection = delta.normalized;
		Move(moveSpeed, moveDirection);
	}

	private void MoveBackward(float _deltaTime)
	{
		if (pathIndex <= 1)
		{ Stop(); return; }

		Vector3 delta = paths[pathIndex - 1].position - transform.position;

		float arrive = Mathf.Max(moveSpeed * _deltaTime, 0.01f);
		if (delta.sqrMagnitude <= arrive * arrive)
		{
			transform.position = paths[pathIndex - 1].position;

			if (--pathIndex <= 1)
			{ Stop(); return; }

			delta = paths[pathIndex - 1].position - transform.position;
		}

		moveDirection = delta.normalized;
		Move(moveSpeed, moveDirection);
	}
	#endregion

	#region 타겟
	public bool IsExclude()
	{
		Bounds bounds = SR.bounds;
		Rect rect = AutoCamera.WorldRect;

		bool onScreen = bounds.max.x >= rect.xMin
			&& bounds.min.x <= rect.xMax
			&& bounds.max.y >= rect.yMin
			&& bounds.min.y <= rect.yMax;

		return pathIndex <= 1 || health * 1.5f < reserve || IsDead || IsDespawn || !onScreen;
	}

	public bool IsInvalid(int _index = -1) => IsDead || IsDespawn || (_index >= 0 && Index != _index);
	#endregion

	#region 전투
	public bool TakeDamage(int _damage, DamageType _type = DamageType.Normal, bool _direct = false)
	{
		if (IsDead) return false;

		if (!_direct) ReserveDown(_damage);

		int damage = debuff.CalcAmplified(_damage);

		SetHealth(health - damage);
		CreateDamage(damage, _type);

		if (health <= 0) Die();

		return true;
	}

	private void CreateDamage(int _damage, DamageType _type = DamageType.Normal)
	{
		if (_damage <= 0) return;

		DamageData data = DataManager.Instance.GetTowerDamage(_type);

		Vector3 from = transform.position + Vector3.up * 0.5f;
		Vector3 to = new Vector3(0f, AutoCamera.WorldRect.yMax, 0f);
		Vector3 dir = (to - from).normalized;

		TextEffect text = EntityManager.Instance?.MakeText(from);
		if (text == null) return;

		text.SetText(_damage.ToString(), data.font, data.color);
		text.SetMove(damageSpeed, dir);
		text.SetDuration(damageDuration);
	}

	public void ReserveUp(int _damage) => reserve += _damage;
	public void ReserveDown(int _damage) => reserve = Mathf.Max(reserve - _damage, 0);

	public void Die()
	{
		if (IsDead) return;
		IsDead = true;

		OnDeath();

		EntityManager.Instance?.DespawnMonster(this);
	}

	protected virtual void OnDeath()
	{
		GameManager.Instance?.ScoreUp();
		GameManager.Instance?.GoldUp(gold);
	}

	protected virtual void OnGoal()
	{
		GameManager.Instance?.LifeDown();
		GameManager.Instance?.GoldDown(gold / 10, true);
		EntityManager.Instance?.DespawnMonster(this);
	}
	#endregion

	#region SET
	public void SetMonster()
	{
		int score = Mathf.Max(GameManager.Instance.Score / 50, 1);

		maxHealth = 50 * score;
		gold = 10 * score;

		SetHealth(maxHealth);
	}

	public void SetPath(Transform[] _paths)
	{
		paths = _paths;
		pathIndex = 0;
	}
	public void SetSpeed(float _speed) => moveSpeed = Mathf.Max(_speed, 0f);
	public void SetForward(bool _on) => IsForward = _on;

	public void SetHealth(int _health)
	{
		health = Mathf.Max(_health, 0);
		if (healthText != null)
			healthText.text = UIManager.Instance?.FormatNumber(health);
	}
	#endregion

	#region 프로퍼티
	public float Scale => scale;
	public float Speed => moveSpeed;
	public Vector3 Direction => moveDirection;
	public float PathProgress
	{
		get
		{
			int index = pathIndex;
			if (index >= paths.Length) return -paths.Length * 10000f;

			Vector3 target = paths[index].position;
			float dist = (target - transform.position).sqrMagnitude;
			return -index * 10000f + dist;
		}
	}

	public int Health => health;
	public int MaxHealth => maxHealth;

	public MonsterDebuff Debuff => debuff;
	#endregion

	#region 풀링
	public int Index { private set; get; }

	public override void OnSpawnPool()
	{
		base.OnSpawnPool();

		transform.localScale = Vector3.one * scale;

		int order = ++sorting * 10;
		SR.sortingOrder = order;
		if (canvas != null)
			canvas.sortingOrder = order;

		IsForward = true;
		IsDead = false;
		Index = order;

		SetMonster();
	}

	public override void OnDespawnPool()
	{
		Pooling[] poolings = GetComponentsInChildren<Pooling>(true);
		for (int i = 0; i < poolings.Length; i++)
		{
			Pooling pooling = poolings[i];
			if (pooling == this) continue;
			if (pooling.IsDespawn) continue;

			EntityManager.Instance?.DespawnPool(pooling);
		}

		base.OnDespawnPool();
	}

	public override void ResetPool()
	{
		base.ResetPool();

		paths = null;
		pathIndex = 0;
		moveSpeed = 3f;
		moveDirection = Vector3.zero;

		reserve = 0;
		health = 0;
		maxHealth = 0;
		gold = 0;

		debuff.Clear();

		Stop();
	}
	#endregion
}
