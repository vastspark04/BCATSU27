using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VRInteractable))]
public class VRInteractableUIButton : MonoBehaviour
{
	public bool useButtonComponent;

	public Color normalColor;

	public Color hoverColor;

	public Color interactColor;

	public bool multiplyColor = true;

	private Graphic image;

	private Text txt;

	private Color origColor;

	private Button b;

	private bool didSetup;

	private bool interacted;

	private bool hovered;

	public void SetBaseColor(Color c)
	{
		Setup();
		origColor = c;
		if (interacted)
		{
			image.color = origColor * interactColor;
		}
		else if (hovered)
		{
			image.color = origColor * hoverColor;
		}
		else
		{
			image.color = origColor * normalColor;
		}
	}

	private void Start()
	{
		Setup();
	}

	private void Setup()
	{
		if (!didSetup)
		{
			didSetup = true;
			image = GetComponent<Graphic>();
			VRInteractable component = GetComponent<VRInteractable>();
			if (useButtonComponent)
			{
				b = GetComponent<Button>();
				image = b.targetGraphic;
				normalColor = b.colors.normalColor;
				hoverColor = b.colors.highlightedColor;
				interactColor = b.colors.pressedColor;
				b.transition = Selectable.Transition.None;
				b.enabled = false;
				b.enabled = true;
				origColor = image.color;
			}
			else if (!multiplyColor)
			{
				origColor = Color.white;
				origColor.a = image.color.a;
			}
			else
			{
				origColor = image.color;
			}
			component.OnHover += Vrint_OnHover;
			component.OnUnHover += Vrint_OnUnHover;
			component.OnStartInteraction += Vrint_OnStartInteraction;
			component.OnStopInteraction += Vrint_OnStopInteraction;
			image.color = normalColor * origColor;
		}
	}

	private void OnValidate()
	{
		if (!image)
		{
			image = GetComponent<Graphic>();
		}
		if ((bool)image && !useButtonComponent && !multiplyColor)
		{
			Color color = normalColor;
			color.a = image.color.a;
			image.color = color;
		}
	}

	private void Vrint_OnStopInteraction(VRHandController controller)
	{
		image.color = hoverColor * origColor;
		interacted = false;
	}

	private void Vrint_OnStartInteraction(VRHandController controller)
	{
		image.color = interactColor * origColor;
		if (useButtonComponent)
		{
			b.onClick.Invoke();
		}
		interacted = true;
	}

	private void Vrint_OnUnHover(VRHandController controller)
	{
		image.color = normalColor * origColor;
		hovered = false;
	}

	private void Vrint_OnHover(VRHandController controller)
	{
		image.color = hoverColor * origColor;
		hovered = true;
	}
}
