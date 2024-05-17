using UnityEngine;
using UnityEngine.UI;

public class HPConfiguratorNode : MonoBehaviour, ILocalizationUser
{
	public VRInteractable interactable;

	public Text titleText;

	public Text massText;

	public Text countText;

	[HideInInspector]
	public LoadoutConfigurator configurator;

	private int hpIdx;

	private string s_mass;

	private string s_hardpoint;

	private string s_none;

	public void ApplyLocalization()
	{
		s_mass = VTLocalizationManager.GetString("vehicleConfig_equipMass", "Mass:").ToUpper();
		s_hardpoint = VTLocalizationManager.GetString("vehicleConfig_hardpoint", "Hardpoint");
		s_none = VTLocalizationManager.GetString("vehicleConfig_none", "None");
	}

	private void Awake()
	{
		ApplyLocalization();
	}

	private void Start()
	{
		interactable.OnInteract.AddListener(OnPressButton);
	}

	public void UpdateInfo(HPEquippable eq, int idx)
	{
		hpIdx = idx;
		interactable.interactableName = $"{s_hardpoint} {idx.ToString()}";
		string shortName = s_none;
		if ((bool)eq)
		{
			titleText.color = Color.white;
			IMassObject componentImplementing = eq.gameObject.GetComponentImplementing<IMassObject>();
			shortName = eq.shortName;
			if (componentImplementing != null)
			{
				massText.gameObject.SetActive(value: true);
				float num = Mathf.Round(componentImplementing.GetMass() * 1000f);
				massText.text = $"{s_mass} {num} kg";
			}
			else
			{
				massText.gameObject.SetActive(value: false);
			}
			if (eq.GetCount() < eq.GetMaxCount())
			{
				countText.gameObject.SetActive(value: true);
				countText.text = $"{eq.GetCount()}/{eq.GetMaxCount()}";
			}
			else
			{
				countText.gameObject.SetActive(value: false);
			}
		}
		else
		{
			titleText.color = Color.gray;
			massText.gameObject.SetActive(value: false);
			countText.gameObject.SetActive(value: false);
		}
		if (idx == configurator.activeHardpoint)
		{
			titleText.color = Color.green;
		}
		string text = $"E{idx.ToString()}: {shortName}";
		titleText.text = text;
	}

	private void OnPressButton()
	{
		configurator.SetActiveHardpoint(hpIdx);
	}
}
