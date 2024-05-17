using UnityEngine;

public class TestOnDisable : MonoBehaviour
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
