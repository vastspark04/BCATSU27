using UnityEngine;

public class ASMTestVehicle : MonoBehaviour
{
	public MissileLauncher ml;

	public Actor actor;

	public Transform[] waypoints;

	private Rigidbody rb;

	private float speed = 180f;

	private float altitude = 1000f;

	private int terminalMode;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void Update()
	{
	}

	private void LaunchMissile(float speed, float altitude, int tm)
	{
		Vector3 position = base.transform.position;
		position.y = WaterPhysics.instance.height + altitude;
		base.transform.position = position;
		rb.velocity = speed * base.transform.forward;
		actor.SetCustomVelocity(speed * base.transform.forward);
		ml.LoadMissile(0);
		AntiShipGuidance component = ml.GetNextMissile().GetComponent<AntiShipGuidance>();
		GPSTargetGroup gPSTargetGroup = new GPSTargetGroup("Foo", 1);
		for (int i = 0; i < waypoints.Length; i++)
		{
			gPSTargetGroup.AddTarget(new GPSTarget(waypoints[i].position, "WPT", 0));
		}
		gPSTargetGroup.isPath = true;
		component.SetTarget(gPSTargetGroup);
		component.terminalBehavior = (AntiShipGuidance.ASMTerminalBehaviors)tm;
		ml.parentActor = actor;
		ml.FireMissile();
	}

	private void OnGUI()
	{
		float num = 200f;
		if (float.TryParse(GUI.TextField(new Rect(10f, num + 10f, 200f, 25f), speed.ToString()), out var result))
		{
			speed = result;
		}
		if (float.TryParse(GUI.TextField(new Rect(10f, num + 40f, 200f, 25f), altitude.ToString()), out var result2))
		{
			altitude = result2;
		}
		if (int.TryParse(GUI.TextField(new Rect(10f, num + 70f, 50f, 25f), terminalMode.ToString()), out var result3))
		{
			terminalMode = result3;
		}
		Rect position = new Rect(60f, num + 70f, 200f, 25f);
		AntiShipGuidance.ASMTerminalBehaviors aSMTerminalBehaviors = (AntiShipGuidance.ASMTerminalBehaviors)terminalMode;
		GUI.Label(position, aSMTerminalBehaviors.ToString());
		if (GUI.Button(new Rect(10f, num + 100f, 100f, 25f), "Fire"))
		{
			LaunchMissile(speed, altitude, terminalMode);
		}
	}
}
