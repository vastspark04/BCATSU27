using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ARMTest : MonoBehaviour
{
	public Transform testVehicleTf;

	public MissileLauncher testML;

	public Actor enemyActor;

	private float altitude = 1000f;

	private float speed = 200f;

	private float distance = 4000f;

	public InputField altText;

	public InputField speedText;

	public InputField distText;

	private Coroutine fireRoutine;

	private void Start()
	{
		altText.text = altitude.ToString();
		speedText.text = speed.ToString();
		distText.text = distance.ToString();
		altText.onValueChanged.AddListener(EditAlt);
		speedText.onValueChanged.AddListener(EditSpeed);
		distText.onValueChanged.AddListener(EditDist);
	}

	private void EditAlt(string t)
	{
		altitude = float.Parse(t);
	}

	private void EditSpeed(string t)
	{
		speed = float.Parse(t);
	}

	private void EditDist(string t)
	{
		distance = float.Parse(t);
	}

	public void LaunchTest()
	{
		testML.LoadAllMissiles();
		Missile nextMissile = testML.GetNextMissile();
		nextMissile.antiRadTargetActor = enemyActor;
		Vector3 position;
		Vector3 position2 = (position = testVehicleTf.position);
		position2.y = WaterPhysics.instance.height + 5f;
		position2 += new Vector3(0f, 0f, distance);
		enemyActor.transform.position = position2;
		position.y = WaterPhysics.instance.height + altitude;
		testVehicleTf.position = position;
		if (fireRoutine != null)
		{
			StopCoroutine(fireRoutine);
		}
		fireRoutine = StartCoroutine(LaunchWhenReady(nextMissile));
	}

	private IEnumerator LaunchWhenReady(Missile m)
	{
		m.antiRadRWR.enabled = true;
		bool ready = false;
		while (!ready)
		{
			ModuleRWR.RWRContact[] contacts = m.antiRadRWR.contacts;
			foreach (ModuleRWR.RWRContact rWRContact in contacts)
			{
				if (rWRContact.active && rWRContact.radarActor == enemyActor)
				{
					ready = true;
					break;
				}
			}
			yield return null;
		}
		testML.FireMissile();
		yield return new WaitForFixedUpdate();
		m.rb.velocity += speed * Vector3.forward;
		while ((bool)m)
		{
			testVehicleTf.position += new Vector3(0f, 0f, speed * Time.deltaTime);
			yield return null;
		}
	}
}
