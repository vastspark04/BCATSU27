using UnityEngine;

public class VroteinSwitcher : MonoBehaviour
{
	public GameObject[] showOnVrotein;

	public GameObject[] hideOnVrotein;

	private void Awake()
	{
		showOnVrotein.SetActive(active: false);
	}
}
