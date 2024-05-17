using UnityEngine;

public class AGMTest : MonoBehaviour
{
	public OpticalMissileLauncher ml;

	public Transform targetTransform;

	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Keypad0) && (bool)ml.GetNextMissile())
		{
			ml.GetNextMissile().SetOpticalTarget(targetTransform);
			ml.FireMissile();
		}
		if (Input.GetKeyDown(KeyCode.Keypad1))
		{
			ml.LoadAllMissiles();
		}
	}
}
