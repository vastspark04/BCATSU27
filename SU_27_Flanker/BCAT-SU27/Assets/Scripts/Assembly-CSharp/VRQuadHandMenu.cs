using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRQuadHandMenu : MonoBehaviour
{
	public GameObject displayObj;

	public Transform selectorTf;

	public float centerRadius;

	public float maxRadius;

	public AudioSource audioSource;

	public AudioClip highlightSound;

	public AudioClip pressSound;

	public AudioClip errorSound;

	public Image errorImage;

	public List<VRQuadHandMenuPage> pages;

	public string defaultPage;

	public VRHandController controller;

	private Dictionary<string, VRQuadHandMenuPage> pageDict;

	private bool isOpen;

	private VRQuadHandMenuPage currentPage;

	private const float PRESS_HOLD_TIME = 0.4f;

	public Material overrideMaterial;

	private float pressedTime;

	private bool waitingForUp;

	private bool pressing;

	private VRQuadHandMenuPage.Buttons pressingButton = VRQuadHandMenuPage.Buttons.None;

	private Coroutine errorRoutine;

	private void Awake()
	{
		if ((bool)overrideMaterial)
		{
			Graphic[] componentsInChildren = GetComponentsInChildren<Graphic>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material = overrideMaterial;
			}
		}
		pageDict = new Dictionary<string, VRQuadHandMenuPage>();
		foreach (VRQuadHandMenuPage page in pages)
		{
			if (page != null)
			{
				pageDict.Add(page.pageName, page);
			}
		}
	}

	private void Update()
	{
		if (!controller)
		{
			return;
		}
		if ((bool)controller.activeInteractable)
		{
			if (isOpen)
			{
				Close();
			}
		}
		else if (isOpen)
		{
			UpdateMenuInput();
		}
		else if (!ControllerEventHandler.eventsPaused && controller.gripPressed && controller.triggerClicked && controller.GetThumbButtonDown())
		{
			Open();
		}
	}

	private void UpdateMenuInput()
	{
		Vector2 stickAxis = controller.stickAxis;
		stickAxis.x = Mathf.Sign(stickAxis.x) * (stickAxis.x * stickAxis.x);
		stickAxis.y = Mathf.Sign(stickAxis.y) * (stickAxis.y * stickAxis.y);
		Vector3 vector = stickAxis * maxRadius;
		selectorTf.localPosition = vector;
		VRQuadHandMenuPage.Buttons buttons = VRQuadHandMenuPage.Buttons.None;
		if (vector.sqrMagnitude < centerRadius * centerRadius)
		{
			buttons = VRQuadHandMenuPage.Buttons.Center;
		}
		else
		{
			float num = VectorUtils.SignedAngle(Vector3.up, vector, Vector3.right);
			buttons = ((!(num > -45f) || !(num < 45f)) ? ((num > 45f && num < 135f) ? VRQuadHandMenuPage.Buttons.Right : ((!(num > 135f) && !(num < -135f)) ? VRQuadHandMenuPage.Buttons.Left : VRQuadHandMenuPage.Buttons.Bottom)) : VRQuadHandMenuPage.Buttons.Top);
		}
		if (buttons != VRQuadHandMenuPage.Buttons.None && currentPage.Highlight(buttons))
		{
			audioSource.PlayOneShot(highlightSound);
		}
		else if (!currentPage.ButtonExists(buttons))
		{
			buttons = VRQuadHandMenuPage.Buttons.None;
		}
		if (controller.stickPressDown || (!pressing && stickAxis.magnitude > 0.9f))
		{
			if (buttons != VRQuadHandMenuPage.Buttons.None)
			{
				pressedTime = Time.time;
				pressingButton = buttons;
			}
			pressing = true;
		}
		else if (controller.stickPressed || stickAxis.magnitude > 0.9f)
		{
			if (waitingForUp)
			{
				return;
			}
			if (pressingButton != buttons && pressingButton != VRQuadHandMenuPage.Buttons.None)
			{
				currentPage.ResetButton();
			}
			if (buttons != VRQuadHandMenuPage.Buttons.None)
			{
				if (Time.time - pressedTime > 0.4f)
				{
					if (currentPage.Press())
					{
						audioSource.PlayOneShot(pressSound);
						waitingForUp = true;
					}
				}
				else
				{
					currentPage.HoldingButton();
				}
			}
			else
			{
				pressedTime = Time.time;
			}
		}
		else if (controller.stickPressUp || (pressing && !controller.stickPressed && stickAxis.magnitude < 0.7f))
		{
			if (pressingButton != VRQuadHandMenuPage.Buttons.None)
			{
				currentPage.ResetButton();
			}
			pressingButton = VRQuadHandMenuPage.Buttons.None;
			waitingForUp = false;
			pressing = false;
		}
	}

	public void Open()
	{
		displayObj.SetActive(value: true);
		OpenPage(defaultPage);
		waitingForUp = false;
	}

	public void Close()
	{
		displayObj.SetActive(value: false);
		isOpen = false;
		waitingForUp = false;
		if ((bool)currentPage)
		{
			currentPage.ResetButton();
		}
		pressingButton = VRQuadHandMenuPage.Buttons.None;
	}

	public void OpenPage(string pageName)
	{
		if ((bool)currentPage)
		{
			currentPage.HidePage();
		}
		currentPage = pageDict[pageName];
		currentPage.ShowPage();
		isOpen = true;
	}

	public void ExitMission()
	{
		FlightSceneManager.instance.ReturnToBriefingOrExitScene();
		Close();
	}

	public void RestartMission()
	{
		FlightSceneManager.instance.ReloadScene();
		Close();
	}

	public void RecenterSeat()
	{
		VRHead.ReCenter();
	}

	public void Quicksave()
	{
		if (QuicksaveManager.instance.CheckQsEligibility() && QuicksaveManager.instance.CheckScenarioQsLimits())
		{
			QuicksaveManager.instance.Quicksave();
			Close();
		}
		else
		{
			ShowError();
		}
	}

	public void Quickload()
	{
		if (QuicksaveManager.instance.canQuickload)
		{
			QuicksaveManager.instance.Quickload();
			Close();
		}
		else
		{
			ShowError();
		}
	}

	public void ShowError()
	{
		if (errorRoutine != null)
		{
			StopCoroutine(errorRoutine);
		}
		errorRoutine = StartCoroutine(ErrorRoutine());
	}

	private IEnumerator ErrorRoutine()
	{
		audioSource.PlayOneShot(errorSound);
		errorImage.enabled = true;
		float t = 1f;
		while (t > 0f)
		{
			Color color = errorImage.color;
			color.a = t;
			errorImage.color = color;
			t = Mathf.MoveTowards(t, 0f, Time.deltaTime);
			yield return null;
		}
		errorImage.enabled = false;
	}
}
