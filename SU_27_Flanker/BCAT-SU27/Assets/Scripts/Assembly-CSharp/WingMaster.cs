using System.Collections.Generic;
using UnityEngine;

public class WingMaster : MonoBehaviour
{
	private List<Wing> wings = new List<Wing>();

	private bool wingsEnabled;

	private Rigidbody rb;

	public bool alwaysEnabled;

	public void AddWing(Wing w)
	{
		if (!wings.Contains(w))
		{
			wings.Add(w);
		}
	}

	public void RemoveWing(Wing w)
	{
		wings.Remove(w);
	}

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		Wing[] componentsInChildren = GetComponentsInChildren<Wing>(includeInactive: true);
		foreach (Wing wing in componentsInChildren)
		{
			if ((bool)wing.rb && wing.rb == rb)
			{
				wings.Add(wing);
				wing.enabled = false;
			}
		}
		wingsEnabled = false;
	}

	private void Update()
	{
		if (!rb)
		{
			Object.Destroy(this);
		}
		else if (!alwaysEnabled && (rb.isKinematic || rb.velocity.sqrMagnitude < 100f))
		{
			if (!wingsEnabled)
			{
				return;
			}
			foreach (Wing wing in wings)
			{
				if ((bool)wing && wing.rb == rb)
				{
					wing.enabled = false;
				}
			}
			wingsEnabled = false;
		}
		else
		{
			if (wingsEnabled)
			{
				return;
			}
			foreach (Wing wing2 in wings)
			{
				if ((bool)wing2 && wing2.rb == rb)
				{
					wing2.enabled = true;
				}
			}
			wingsEnabled = true;
		}
	}
}
