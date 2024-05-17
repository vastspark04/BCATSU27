using UnityEngine;

[RequireComponent(typeof(Gun))]
public class GunTest : MonoBehaviour
{
	public KeyCode testKey;

	private Gun gun;

	private void Start()
	{
		gun = GetComponent<Gun>();
	}

	private void Update()
	{
		if (Input.GetKeyDown(testKey))
		{
			gun.SetFire(fire: true);
		}
		if (Input.GetKeyUp(testKey))
		{
			gun.SetFire(fire: false);
		}
	}
}
