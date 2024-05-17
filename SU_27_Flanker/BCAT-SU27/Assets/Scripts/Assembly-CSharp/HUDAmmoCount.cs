using UnityEngine;
using UnityEngine.UI;

public class HUDAmmoCount : MonoBehaviour
{
	public VTOLCannon gun;

	public Text text;

	public GameObject fireIndicator;

	private void Start()
	{
	}

	private void Update()
	{
		text.text = gun.ammo.ToString();
		fireIndicator.SetActive(gun.isSpinningUp);
	}
}
