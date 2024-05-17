using System;
using UnityEngine;

public class EngageEnemiesAnimation : MonoBehaviour, IEngageEnemies
{
	public GroundUnitMover stopMoverWhileEngaging;

	public RotationToggle rotationToggle;

	public event Action<bool> OnSetEngage;

	public void SetEngageEnemies(bool engage)
	{
		if ((bool)rotationToggle)
		{
			rotationToggle.SetState(engage ? 1 : 0);
		}
		if ((bool)stopMoverWhileEngaging)
		{
			stopMoverWhileEngaging.move = !engage;
		}
		this.OnSetEngage?.Invoke(engage);
	}
}
