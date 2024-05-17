using Rewired;
using UnityEngine;

public static class VTRewiredExtensions
{
	private static int zeroToOneBehaviourID = -1;

	public static float VTRWGetAxis(this Player p, int actionID)
	{
		bool flag = false;
		InputAction action = ReInput.mapping.GetAction(actionID);
		if (zeroToOneBehaviourID < 0)
		{
			zeroToOneBehaviourID = ReInput.mapping.GetInputBehaviorId("ZeroToOneAxis");
		}
		if (action.behaviorId == zeroToOneBehaviourID)
		{
			flag = true;
		}
		if (flag)
		{
			float axis = p.GetAxis(actionID);
			ActionElementMap firstElementMapWithAction = p.controllers.maps.GetFirstElementMapWithAction(actionID, skipDisabledMaps: true);
			if (firstElementMapWithAction != null && firstElementMapWithAction.invert)
			{
				return 1f - Mathf.Clamp01(Mathf.Abs(axis));
			}
			return Mathf.Clamp01(axis);
		}
		return p.GetAxis(actionID);
	}
}
