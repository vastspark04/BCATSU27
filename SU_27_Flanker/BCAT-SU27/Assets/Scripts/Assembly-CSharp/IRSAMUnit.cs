using UnityEngine;

public class IRSAMUnit : MonoBehaviour, IGroundColumnUnit
{
	public IRSamLauncher samLauncher;

	public GroundUnitMover mover;

	public Health health;

	public bool stopToEngage = true;

	public bool canMove = true;

	private GroundUnitColumn column;

	private bool alive = true;

	private void Awake()
	{
		health.OnDeath.AddListener(OnDeath);
	}

	private void OnDeath()
	{
		alive = false;
		mover.move = false;
	}

	private void Update()
	{
		if (!alive)
		{
			return;
		}
		if (!canMove)
		{
			mover.move = false;
			return;
		}
		if (stopToEngage)
		{
			mover.move = !samLauncher.isEngaging;
		}
		if ((bool)column)
		{
			mover.move = column.canMove;
		}
	}

	public bool GetIsAlive()
	{
		return alive;
	}

	public bool GetCanMove()
	{
		if (canMove && !samLauncher.isEngaging)
		{
			return !mover.railComplete;
		}
		return false;
	}

	public void SetColumn(GroundUnitColumn c)
	{
		column = c;
	}
}
