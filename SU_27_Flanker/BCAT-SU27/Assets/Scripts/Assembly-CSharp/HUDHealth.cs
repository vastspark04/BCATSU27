using UnityEngine;
using UnityEngine.UI;

public class HUDHealth : MonoBehaviour
{
	public Text numberText;

	private Health health;

	private void Start()
	{
		health = GetComponentInParent<Health>();
	}

	private void Update()
	{
		numberText.text = Mathf.Round(health.normalizedHealth * 100f).ToString();
	}
}
