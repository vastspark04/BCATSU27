using System.Collections.Generic;
using UnityEngine;

public class GroundUnitColumn : MonoBehaviour
{
	public List<GameObject> units;

	public bool canMove = true;

	public bool overrideCanMove = true;

	private List<IGroundColumnUnit> columnUnits = new List<IGroundColumnUnit>();

	public void AddColumnUnit(IGroundColumnUnit u)
	{
		columnUnits.Add(u);
		u.SetColumn(this);
	}

	private void Start()
	{
		foreach (GameObject unit in units)
		{
			IGroundColumnUnit componentInChildrenImplementing = unit.GetComponentInChildrenImplementing<IGroundColumnUnit>();
			if (componentInChildrenImplementing != null)
			{
				columnUnits.Add(componentInChildrenImplementing);
				componentInChildrenImplementing.SetColumn(this);
			}
		}
	}

	private void Update()
	{
		if (!overrideCanMove)
		{
			canMove = false;
			return;
		}
		canMove = true;
		for (int i = 0; i < columnUnits.Count; i++)
		{
			IGroundColumnUnit groundColumnUnit = columnUnits[i];
			if (groundColumnUnit.GetIsAlive() && !groundColumnUnit.GetCanMove())
			{
				canMove = false;
				break;
			}
		}
	}

	public void SetOverrideCanMove(bool move)
	{
		overrideCanMove = move;
	}
}
