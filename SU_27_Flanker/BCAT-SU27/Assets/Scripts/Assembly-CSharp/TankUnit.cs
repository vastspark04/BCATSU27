using UnityEngine;

public class TankUnit : MonoBehaviour, IGroundColumnUnit, IOptionalStopToEngage
{
	private VehicleMover mover;

	public GunTurretAI cannonAI;

	private bool alive = true;

	public bool stopToEngage = true;

	public bool canMove = true;

	private GroundUnitColumn column;

	private void Awake()
	{
		mover = GetComponent<VehicleMover>();
		GetComponent<Health>().OnDeath.AddListener(H_OnDeath);
	}

	private void H_OnDeath()
	{
		cannonAI.targetFinder.enabled = false;
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
			mover.move = !cannonAI.engagingTarget;
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
		if (canMove && !cannonAI.engagingTarget)
		{
			return !mover.railComplete;
		}
		return false;
	}

	public void SetColumn(GroundUnitColumn c)
	{
		column = c;
	}

	public void SetStopToEngage(bool stopToEngage)
	{
		this.stopToEngage = stopToEngage;
	}
}
