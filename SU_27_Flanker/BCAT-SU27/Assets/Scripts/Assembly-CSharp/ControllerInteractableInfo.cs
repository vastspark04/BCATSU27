using UnityEngine;
using UnityEngine.UI;

public class ControllerInteractableInfo : MonoBehaviour, ILocalizationUser
{
	private VRHandController ctrl;

	public MeshRenderer sphereRenderer;

	public Text uiText;

	public Color hoverColor;

	public Color idleColor;

	public Color activeColor;

	private bool active;

	private Color currColor;

	private Color tgtColor;

	private MaterialPropertyBlock sphereProps;

	public string sphereColorProperty;

	private int sphereColorIdx;

	private bool showToolTips;

	private void Awake()
	{
		GameSettings.OnAppliedSettings += OnAppliedSettings;
	}

	private void OnDestroy()
	{
		GameSettings.OnAppliedSettings -= OnAppliedSettings;
	}

	private void Start()
	{
		ctrl = GetComponentInParent<VRHandController>();
		ControllerEventHandler.fetch.OnHover += ControllerEventHandler_fetch_OnHover;
		ControllerEventHandler.fetch.OnUnHover += ControllerEventHandler_fetch_OnUnHover;
		uiText.gameObject.SetActive(value: false);
		if ((bool)sphereRenderer)
		{
			sphereColorIdx = Shader.PropertyToID(sphereColorProperty);
			sphereProps = new MaterialPropertyBlock();
			sphereRenderer.GetPropertyBlock(sphereProps);
			sphereProps.SetColor(sphereColorIdx, idleColor);
			sphereRenderer.SetPropertyBlock(sphereProps);
		}
		currColor = idleColor;
		tgtColor = idleColor;
		showToolTips = GameSettings.CurrentSettings.GetBoolSetting("TOOLTIPS");
	}

	private void OnAppliedSettings(GameSettings settings)
	{
		showToolTips = settings.GetBoolSetting("TOOLTIPS");
	}

	private void ControllerEventHandler_fetch_OnHover(VRHandController controller, VRInteractable interactable)
	{
		if (!controller || !ctrl || !(controller.gameObject == ctrl.gameObject))
		{
			return;
		}
		if ((bool)sphereRenderer)
		{
			sphereRenderer.transform.localScale = 2f * Vector3.one;
		}
		tgtColor = hoverColor;
		currColor = Color.white;
		if (showToolTips)
		{
			uiText.text = $"{interactable.interactableName} ({GetLocalizedButtonString(interactable.button)})";
			uiText.gameObject.SetActive(value: true);
			RectTransform rectTransform = uiText.rectTransform;
			if (Vector3.Dot(rectTransform.position - VRHead.position, VRHead.instance.transform.right) < 0f)
			{
				rectTransform.pivot = Vector2.zero;
				uiText.alignment = TextAnchor.LowerLeft;
			}
			else
			{
				rectTransform.pivot = new Vector2(1f, 0f);
				uiText.alignment = TextAnchor.LowerRight;
			}
			rectTransform.localPosition = new Vector3(0f, rectTransform.localPosition.y, rectTransform.localPosition.z);
		}
		else
		{
			uiText.gameObject.SetActive(value: false);
		}
	}

	public void ApplyLocalization()
	{
		VTLocalizationManager.GetString("interactable_button_grip", "Grip");
		VTLocalizationManager.GetString("interactable_button_trigger", "Trigger");
	}

	private string GetLocalizedButtonString(VRInteractable.Buttons button)
	{
		switch (button)
		{
		case VRInteractable.Buttons.Grip:
		case VRInteractable.Buttons.GripPlus:
			return VTLocalizationManager.GetString("interactable_button_grip", "Grip");
		case VRInteractable.Buttons.Trigger:
			return VTLocalizationManager.GetString("interactable_button_trigger", "Trigger");
		default:
			return button.ToString();
		}
	}

	private void ControllerEventHandler_fetch_OnUnHover(VRHandController controller, VRInteractable interactable)
	{
		if ((bool)controller && (bool)ctrl && controller.gameObject == ctrl.gameObject)
		{
			tgtColor = idleColor;
			uiText.gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (ControllerEventHandler.eventsPaused)
		{
			tgtColor = idleColor;
			uiText.gameObject.SetActive(value: false);
		}
		else
		{
			if ((!active || !uiText.gameObject.activeInHierarchy) && ctrl.activeInteractable != null)
			{
				active = true;
				if ((bool)sphereRenderer)
				{
					sphereRenderer.transform.localScale = 2f * Vector3.one;
				}
				tgtColor = activeColor;
				currColor = Color.white;
				uiText.gameObject.SetActive(value: false);
			}
			else if (active && ctrl.activeInteractable == null)
			{
				active = false;
				if ((bool)ctrl.hoverInteractable)
				{
					ControllerEventHandler_fetch_OnHover(ctrl, ctrl.hoverInteractable);
				}
				else
				{
					tgtColor = idleColor;
					uiText.gameObject.SetActive(value: false);
				}
			}
			if (uiText.gameObject.activeInHierarchy)
			{
				uiText.transform.rotation = VRHead.instance.transform.rotation;
			}
		}
		currColor = Color.Lerp(currColor, tgtColor, 8f * Time.deltaTime);
		if ((bool)sphereRenderer)
		{
			sphereProps.SetColor(sphereColorIdx, currColor);
			sphereRenderer.SetPropertyBlock(sphereProps);
			sphereRenderer.transform.localScale = Vector3.Lerp(sphereRenderer.transform.localScale, Vector3.one, 8f * Time.deltaTime);
		}
	}
}
