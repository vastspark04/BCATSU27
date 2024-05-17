using UnityEngine;

public class TutObjFaceNamedWaypoint : CustomTutorialObjective
{
	public string waypointName;

	public float threshold = 5f;

	private FlightInfo flightInfo;

	private Waypoint pt;

	public override void OnStartObjective()
	{
		base.OnStartObjective();
		flightInfo = GetComponentInParent<FlightInfo>();
		Waypoint[] waypoints = VTScenario.current.waypoints.GetWaypoints();
		foreach (Waypoint waypoint in waypoints)
		{
			if (waypoint.name == waypointName)
			{
				pt = waypoint;
				break;
			}
		}
		if (pt == null)
		{
			Debug.LogFormat("TutObjFaceNamedWaypoint: Failed to find waypoint for tutorial objective: {0}", waypointName);
			Debug.Log("Existing waypoints: ");
			waypoints = VTScenario.current.waypoints.GetWaypoints();
			foreach (Waypoint waypoint2 in waypoints)
			{
				Debug.LogFormat("- {0}", waypoint2.name);
			}
		}
	}

	public override bool GetIsCompleted()
	{
		float num = VectorUtils.Bearing(flightInfo.transform.position, pt.worldPosition);
		float num2 = flightInfo.heading + 360f;
		float num3 = num + 360f;
		if (!(Mathf.Abs(num2 - num3) < threshold))
		{
			return Mathf.Abs(num - num2) < threshold;
		}
		return true;
	}
}
