using UnityEngine;

public class SoldierAnimator : MonoBehaviour
{
	public Animator animator;

	public GroundUnitMover mover;

	public Soldier soldier;

	public float minAimAngle;

	public float maxAimAngle;

	public string seatedName = "seated";

	public string moveSpeedName = "moveSpeed";

	public string aimingName = "aiming";

	public int deathAnimCount = 2;

	private int seatedID;

	private int speedID;

	private int aimingID;

	private float moveSpeedSqr;

	private bool alive = true;

	private void Awake()
	{
		Health componentInParent = GetComponentInParent<Health>();
		if ((bool)componentInParent)
		{
			componentInParent.OnDeath.AddListener(OnDeath);
		}
	}

	private void Start()
	{
		seatedID = Animator.StringToHash(seatedName);
		speedID = Animator.StringToHash(moveSpeedName);
		aimingID = Animator.StringToHash(aimingName);
		moveSpeedSqr = mover.moveSpeed * mover.moveSpeed;
	}

	private void OnDeath()
	{
		alive = false;
		animator.SetInteger("death", Random.Range(0, deathAnimCount));
	}

	private void Update()
	{
		if (alive && animator.gameObject.activeInHierarchy)
		{
			float value = mover.velocity.sqrMagnitude / moveSpeedSqr;
			animator.SetFloat(speedID, value);
			bool isLoadedInBay = soldier.isLoadedInBay;
			animator.SetBool(seatedID, isLoadedInBay);
			animator.SetBool(aimingID, soldier.isAiming);
			if (soldier.isAiming)
			{
				float normalizedTime = Mathf.InverseLerp(minAimAngle, maxAimAngle, soldier.aimPitch);
				animator.Play(aimingID, 0, normalizedTime);
			}
		}
	}
}
