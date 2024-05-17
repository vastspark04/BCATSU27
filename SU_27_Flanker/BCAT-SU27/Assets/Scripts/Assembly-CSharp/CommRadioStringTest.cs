using UnityEngine;

public class CommRadioStringTest : MonoBehaviour
{
	public AudioClip[] clips;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			CommRadioManager.instance.PlayMessageString(clips);
		}
	}
}
