using UnityEngine;

public class TestOnEnable : MonoBehaviour
{
	private void OnEnable()
	{
		Debug.Log("OnEnable");
	}

	private void OnDisable()
	{
		Debug.Log("OnDisable");
	}
}
