using UnityEngine;
using UnityEngine.UI;

public class HUDMaskToggler : MonoBehaviour
{
	public Mask[] masks;

	public Image[] images;

	public GameObject canvasObject;

	private Transform[] displayObjects;

	private const int MASKED_LAYER = 5;

	private const int UNMASKED_LAYER = 28;

	private const int HMD_ONLY_LAYER = 28;

	public bool alwaysFPVOnly;

	public bool isMasked { get; private set; }

	private void Awake()
	{
		displayObjects = canvasObject.GetComponentsInChildren<Transform>(includeInactive: true);
	}

	private void Start()
	{
		isMasked = true;
		if (alwaysFPVOnly)
		{
			SetMask(maskEnabled: false);
		}
	}

	public void SetMask(bool maskEnabled)
	{
		if (alwaysFPVOnly && maskEnabled)
		{
			return;
		}
		if (displayObjects != null)
		{
			for (int i = 0; i < displayObjects.Length; i++)
			{
				if ((bool)displayObjects[i])
				{
					if (alwaysFPVOnly)
					{
						displayObjects[i].gameObject.layer = 28;
					}
					else
					{
						displayObjects[i].gameObject.layer = (maskEnabled ? 5 : 28);
					}
				}
			}
		}
		for (int j = 0; j < masks.Length; j++)
		{
			if ((bool)masks[j])
			{
				masks[j].enabled = maskEnabled;
			}
		}
		for (int k = 0; k < images.Length; k++)
		{
			if ((bool)images[k])
			{
				images[k].enabled = maskEnabled;
			}
		}
		if (alwaysFPVOnly)
		{
			canvasObject.layer = 28;
		}
		else
		{
			canvasObject.layer = (maskEnabled ? 5 : 28);
		}
		isMasked = maskEnabled;
	}
}
