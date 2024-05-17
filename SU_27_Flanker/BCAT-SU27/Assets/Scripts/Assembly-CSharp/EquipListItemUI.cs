using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EquipListItemUI : MonoBehaviour
{
	public float margin = 2f;

	public VRInteractable button;

	public Image buttonGraphic;

	public Text text;

	public Color defaultColor = new Color(0f, 0.25f, 0.15f, 1f);

	public Color selectedColor = Color.green;

	private HPConfiguratorFullInfo fullInfo;

	private int eqIdx;

	private string shortName;

	public void SetupItem(int groupIdx, string shortName, HPConfiguratorFullInfo hpConfig)
	{
		this.shortName = shortName;
		fullInfo = hpConfig;
		base.gameObject.SetActive(value: true);
		float height = ((RectTransform)base.transform).rect.height;
		base.transform.localPosition += new Vector3(0f, 0f - (height + margin), 0f) * groupIdx;
		text.text = shortName;
		button.interactableName = shortName;
		eqIdx = groupIdx;
		UnityAction call = delegate
		{
			fullInfo.ItemListButton(eqIdx);
		};
		button.OnInteract.AddListener(call);
	}

	private void Update()
	{
		if ((bool)fullInfo)
		{
			if (fullInfo.GetEquip(fullInfo.currIdx).shortName == shortName)
			{
				buttonGraphic.color = selectedColor;
			}
			else
			{
				buttonGraphic.color = defaultColor;
			}
			if (fullInfo.equippedIdx >= 0 && fullInfo.GetEquip(fullInfo.equippedIdx).shortName == shortName)
			{
				text.color = selectedColor;
			}
			else
			{
				text.color = Color.white;
			}
		}
	}
}
