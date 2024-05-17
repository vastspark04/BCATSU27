using UnityEngine;

public class DisableOnPlay : MonoBehaviour
{
	private void Awake()
	{
		base.gameObject.SetActive(value: false);
	}
}
