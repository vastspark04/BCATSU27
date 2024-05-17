using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VehicleConfigScenarioUI : MonoBehaviour
{
	public RawImage scenarioImage;

	public Text scenarioTitle;

	public Text scenarioDescription;

	public AudioSource uiAudioSource;

	public AudioClip denyLaunchSound;

	public GameObject denyDisplayObject;

	public Text denyReasonText;

	public GameObject envSelectObj;

	public Text envSelectText;

	private Coroutine denyRoutine;

	private void Start()
	{
		if (PilotSaveManager.currentScenario != null)
		{
			CampaignScenario currentScenario = PilotSaveManager.currentScenario;
			if ((bool)currentScenario.scenarioImage)
			{
				scenarioImage.texture = currentScenario.scenarioImage;
			}
			scenarioTitle.text = currentScenario.scenarioName;
			scenarioDescription.text = currentScenario.description;
			if (currentScenario.envOptions != null && currentScenario.envOptions.Length != 0)
			{
				envSelectObj.SetActive(value: true);
				currentScenario.envIdx = 0;
				for (int i = 0; i < currentScenario.envOptions.Length; i++)
				{
					if (currentScenario.envOptions[i].envName == currentScenario.environmentName)
					{
						currentScenario.envIdx = i;
					}
				}
				UpdateEnvText();
			}
			else
			{
				envSelectObj.SetActive(value: false);
			}
		}
		denyDisplayObject.SetActive(value: false);
	}

	private void UpdateEnvText()
	{
		CampaignScenario currentScenario = PilotSaveManager.currentScenario;
		envSelectText.text = VTLocalizationManager.GetString($"env_{currentScenario.envOptions[currentScenario.envIdx].envLabel.ToLower()}", currentScenario.envOptions[currentScenario.envIdx].envLabel);
	}

	public void NextEnv()
	{
		CampaignScenario currentScenario = PilotSaveManager.currentScenario;
		currentScenario.envIdx = (currentScenario.envIdx + 1) % currentScenario.envOptions.Length;
		UpdateEnvText();
	}

	public void PrevEnv()
	{
		CampaignScenario currentScenario = PilotSaveManager.currentScenario;
		currentScenario.envIdx--;
		if (currentScenario.envIdx < 0)
		{
			currentScenario.envIdx = currentScenario.envOptions.Length - 1;
		}
		UpdateEnvText();
	}

	public void DenyLaunch(string reason)
	{
		uiAudioSource.PlayOneShot(denyLaunchSound);
		if (denyRoutine != null)
		{
			StopCoroutine(denyRoutine);
		}
		denyDisplayObject.SetActive(value: true);
		denyReasonText.text = reason;
		denyRoutine = StartCoroutine(DenyRoutine());
	}

	private IEnumerator DenyRoutine()
	{
		yield return new WaitForSeconds(5f);
		denyDisplayObject.SetActive(value: false);
	}
}
