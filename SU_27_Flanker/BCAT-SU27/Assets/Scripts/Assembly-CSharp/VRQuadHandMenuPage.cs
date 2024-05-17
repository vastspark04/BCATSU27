using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VRQuadHandMenuPage : MonoBehaviour
{
	public enum Buttons
	{
		Top,
		Right,
		Bottom,
		Left,
		Center,
		None
	}

	public string pageName;

	public UnityEvent OnPressTop;

	public UnityEvent OnPressRight;

	public UnityEvent OnPressBottom;

	public UnityEvent OnPressLeft;

	public UnityEvent OnPressCenter;

	public Image topImage;

	public Image rightImage;

	public Image bottomImage;

	public Image leftImage;

	public Image centerImage;

	private Buttons currButton = Buttons.None;

	private Image[] buttonImages;

	private UnityEvent[] events;

	private Color[] defaultColors;

	private float scale = 1f;

	private Buttons hButton = Buttons.None;

	private void Awake()
	{
		buttonImages = new Image[5];
		events = new UnityEvent[5];
		buttonImages[0] = topImage;
		buttonImages[1] = rightImage;
		buttonImages[2] = bottomImage;
		buttonImages[3] = leftImage;
		buttonImages[4] = centerImage;
		defaultColors = new Color[5];
		for (int i = 0; i < 5; i++)
		{
			if ((bool)buttonImages[i])
			{
				defaultColors[i] = buttonImages[i].color;
			}
		}
		events[0] = OnPressTop;
		events[1] = OnPressRight;
		events[2] = OnPressBottom;
		events[3] = OnPressLeft;
		events[4] = OnPressCenter;
	}

	public void ShowPage()
	{
		base.gameObject.SetActive(value: true);
		Highlight(Buttons.Center);
	}

	public void HidePage()
	{
		base.gameObject.SetActive(value: false);
	}

	public bool ButtonExists(Buttons button)
	{
		if (button < Buttons.None)
		{
			return buttonImages[(int)button] != null;
		}
		return false;
	}

	public bool Highlight(Buttons button)
	{
		if (currButton != button)
		{
			if (currButton != Buttons.None)
			{
				Image image = buttonImages[(int)currButton];
				if ((bool)image)
				{
					image.color = defaultColors[(int)currButton];
				}
			}
			Image image2 = buttonImages[(int)button];
			if ((bool)image2)
			{
				image2.color = defaultColors[(int)button] + 0.15f * Color.white;
				currButton = button;
				return true;
			}
		}
		return false;
	}

	public bool Press()
	{
		if (currButton != Buttons.None)
		{
			Image image = buttonImages[(int)currButton];
			if ((bool)image)
			{
				image.transform.localScale = Vector3.one;
			}
			UnityEvent unityEvent = events[(int)currButton];
			if (unityEvent != null)
			{
				unityEvent.Invoke();
				return true;
			}
		}
		return false;
	}

	public void HoldingButton()
	{
		if (currButton == Buttons.None)
		{
			return;
		}
		Image image = buttonImages[(int)currButton];
		if (!image)
		{
			return;
		}
		if (currButton != hButton)
		{
			if (hButton != Buttons.None)
			{
				Image image2 = buttonImages[(int)hButton];
				if ((bool)image2)
				{
					image2.transform.localScale = Vector3.one;
				}
			}
			scale = 1.05f;
		}
		hButton = currButton;
		scale += 0.15f * Time.deltaTime;
		image.transform.localScale = scale * Vector3.one;
	}

	public void ResetButton()
	{
		if (hButton != Buttons.None)
		{
			Image image = buttonImages[(int)hButton];
			if ((bool)image)
			{
				image.transform.localScale = Vector3.one;
			}
			hButton = Buttons.None;
		}
	}
}
