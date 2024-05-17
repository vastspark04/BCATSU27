using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDGMeter : MonoBehaviour
{
	public FlightInfo flightInfo;

	public Text gText;

	private void OnEnable()
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		while (base.enabled)
		{
			yield return new WaitForSeconds(0.15f);
			float playerGs = flightInfo.playerGs;
			playerGs = Mathf.Round(playerGs * 10f) / 10f;
			gText.text = ((playerGs > 0f) ? "+" : string.Empty) + playerGs.ToString("0.0");
		}
	}
}
