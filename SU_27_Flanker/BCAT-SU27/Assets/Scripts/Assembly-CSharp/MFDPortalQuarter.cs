using UnityEngine;

public class MFDPortalQuarter : MonoBehaviour, ILocalizationUser
{
	public enum DisplayStates
	{
		ShowAll,
		HideSubs,
		Maximized
	}

	public MFDPortalHalf half;

	public RectTransform displayTransform;

	public RectTransform leftSubDisplayTransform;

	public RectTransform rightSubDisplayTransform;

	public MFDPortalPageSelectButton minLeftSubButton;

	public MFDPortalPageSelectButton minRightSubButton;

	public VRInteractable menuButton;

	public VRInteractable leftSubButton;

	public VRInteractable rightSubButton;

	public GameObject hideSubsButton;

	public GameObject showSubsButton;

	public GameObject maximizeButton;

	public GameObject unmaximizeButton;

	public GameObject soiDisplayObject;

	[Header("Runtime")]
	private MFDPortalPage _displayedPage;

	public MFDPortalPage subLeftPage;

	public MFDPortalPage subRightPage;

	public MFDPHome homepageInstance;

	public DisplayStates displayState;

	private float standardHeight;

	private float standardWidth;

	private float subsSize;

	private float maxHeight;

	private float maxWidth;

	private Vector3 origLP;

	private MFDPortalQuarter otherQuarter;

	private string s_mfdpOpenMenu;

	private string s_mfdpBLANK;

	public MFDPortalPage displayedPage
	{
		get
		{
			return _displayedPage;
		}
		set
		{
			if ((bool)_displayedPage && _displayedPage.quarter == this)
			{
				_displayedPage.quarter = null;
			}
			_displayedPage = value;
			_displayedPage.quarter = this;
		}
	}

	private bool hasPower => half.manager.hasPower;

	public void ApplyLocalization()
	{
		s_mfdpOpenMenu = VTLocalizationManager.GetString("mfdpOpenMenu", "Open Menu", "Tooltip for button that opens a menu.");
		s_mfdpBLANK = VTLocalizationManager.GetString("mfdpBLANK", "BLANK", "Label for a minimized MFD portal that is empty. Opens the portal menu.");
	}

	private void Awake()
	{
		if (half.quarterLeft == this)
		{
			otherQuarter = half.quarterRight;
		}
		else
		{
			otherQuarter = half.quarterLeft;
		}
		ApplyLocalization();
	}

	public void DisplayMenu()
	{
		if (!displayedPage || !displayedPage.locked)
		{
			MFDPHome page = homepageInstance;
			DisplayPage(page);
			menuButton.gameObject.SetActive(value: false);
		}
	}

	private void Start()
	{
		Rect rect = leftSubDisplayTransform.rect;
		Rect rect2 = displayTransform.rect;
		standardHeight = rect2.height;
		standardWidth = rect2.width;
		subsSize = rect.height;
		maxHeight = subsSize + standardHeight;
		maxWidth = standardWidth * 2f;
		origLP = base.transform.localPosition;
		showSubsButton.SetActive(value: false);
		hideSubsButton.SetActive(value: true);
		maximizeButton.SetActive(value: true);
		unmaximizeButton.SetActive(value: false);
		minLeftSubButton.gameObject.SetActive(value: false);
		minRightSubButton.gameObject.SetActive(value: false);
	}

	public void DisplayPage(MFDPortalPage page)
	{
		if (page == null)
		{
			DisplayMenu();
		}
		else
		{
			if (page.locked)
			{
				return;
			}
			menuButton.gameObject.SetActive(value: true);
			if ((bool)page.quarter)
			{
				if ((bool)page.minimizedButton && page.minimizedButton == page.quarter.half.hiddenMinButton)
				{
					page.quarter.SetHiddenQuarterMinButton(null);
				}
				if (page.quarter.displayedPage == page && page.quarter != this)
				{
					Debug.LogFormat("Taking page away from an existing display. ({0})", page.pageName);
					_ = page.quarter;
					page.quarter.DisplayMenu();
					if (half.hiddenMinButton.gameObject.activeSelf && half.hiddenMinButton.portalQtr == page.quarter)
					{
						Debug.Log("- Legacy clear hiddenMinButton");
						SetHiddenQuarterMinButton(null);
					}
				}
				else if (page.quarter.subLeftPage == page)
				{
					page.quarter.SetSubLeftPage(null);
				}
				else if (page.quarter.subRightPage == page)
				{
					page.quarter.SetSubRightPage(null);
				}
			}
			if (page == subLeftPage)
			{
				subLeftPage = null;
				if (displayedPage == homepageInstance)
				{
					SetSubLeftPage(null);
				}
				else
				{
					SetSubLeftPage(displayedPage);
				}
			}
			else if (page == subRightPage)
			{
				subRightPage = null;
				if (displayedPage == homepageInstance)
				{
					SetSubRightPage(null);
				}
				else
				{
					SetSubRightPage(displayedPage);
				}
			}
			if ((bool)displayedPage)
			{
				displayedPage.SetMinimized();
			}
			displayedPage = page;
			page.minimizedButton = null;
			page.gameObject.SetActive(value: true);
			page.transform.SetParent(displayTransform);
			page.transform.SetAsFirstSibling();
			page.transform.localPosition = Vector3.zero;
			page.transform.localScale = Vector3.one;
			page.transform.localRotation = Quaternion.identity;
			soiDisplayObject.SetActive(page.isSOI);
			switch (displayState)
			{
			case DisplayStates.ShowAll:
				page.SetDisplayAll();
				break;
			case DisplayStates.HideSubs:
				page.SetHideSubs();
				break;
			case DisplayStates.Maximized:
				page.SetMaximized();
				break;
			}
		}
	}

	public void SetSubLeftPage(MFDPortalPage page)
	{
		if ((bool)subLeftPage)
		{
			subLeftPage.quarter = null;
			subLeftPage.SetMinimized();
		}
		if ((bool)page)
		{
			if ((bool)page.quarter)
			{
				if (page.quarter.subRightPage == page)
				{
					page.quarter.SetSubRightPage(null);
				}
				else if (page.quarter.subLeftPage == page)
				{
					page.quarter.SetSubLeftPage(null);
				}
			}
			minLeftSubButton.page = page;
			minLeftSubButton.buttonText.text = page.pageLabel;
			minLeftSubButton.interactable.interactableName = page.pageName;
			subLeftPage = page;
			leftSubButton.interactableName = page.pageName;
			page.quarter = this;
			page.minimizedButton = minLeftSubButton;
			SetSubPagePosition(page, leftSubDisplayTransform);
			if (displayState == DisplayStates.ShowAll)
			{
				page.SetDisplayAsSub(leftSubDisplayTransform);
			}
			else
			{
				page.SetMinimized();
			}
		}
		else
		{
			subLeftPage = null;
			minLeftSubButton.page = homepageInstance;
			minLeftSubButton.buttonText.text = s_mfdpBLANK;
			minLeftSubButton.interactable.interactableName = s_mfdpOpenMenu;
			leftSubButton.interactableName = s_mfdpOpenMenu;
		}
	}

	public void SetSubRightPage(MFDPortalPage page)
	{
		if ((bool)subRightPage)
		{
			subRightPage.quarter = null;
			subRightPage.SetMinimized();
		}
		if ((bool)page)
		{
			if ((bool)page.quarter)
			{
				if (page.quarter.subRightPage == page)
				{
					page.quarter.SetSubRightPage(null);
				}
				if (page.quarter.subLeftPage == page)
				{
					page.quarter.SetSubLeftPage(null);
				}
			}
			page.quarter = this;
			minRightSubButton.page = page;
			minRightSubButton.buttonText.text = page.pageLabel;
			minRightSubButton.interactable.interactableName = page.pageName;
			subRightPage = page;
			rightSubButton.interactableName = page.pageName;
			page.minimizedButton = minRightSubButton;
			SetSubPagePosition(page, rightSubDisplayTransform);
			if (displayState == DisplayStates.ShowAll)
			{
				page.SetDisplayAsSub(rightSubDisplayTransform);
			}
			else
			{
				page.SetMinimized();
			}
		}
		else
		{
			subRightPage = null;
			minRightSubButton.page = homepageInstance;
			minRightSubButton.buttonText.text = s_mfdpBLANK;
			minRightSubButton.interactable.interactableName = s_mfdpOpenMenu;
			rightSubButton.interactableName = s_mfdpOpenMenu;
		}
	}

	private void SetSubPagePosition(MFDPortalPage page, RectTransform subTf)
	{
		page.transform.SetParent(subTf);
		page.transform.SetAsFirstSibling();
		page.transform.localPosition = Vector3.zero;
		page.transform.localRotation = Quaternion.identity;
		page.transform.localScale = Vector3.one;
	}

	public void HideSubs()
	{
		if (displayState != DisplayStates.HideSubs)
		{
			if (displayState == DisplayStates.Maximized)
			{
				otherQuarter.ShowQuarter();
			}
			displayState = DisplayStates.HideSubs;
			base.transform.localPosition = origLP;
			displayTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight);
			displayTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, standardWidth);
			if ((bool)displayedPage)
			{
				displayedPage.SetHideSubs();
			}
			else
			{
				homepageInstance.SetHideSubs();
			}
			if ((bool)subLeftPage)
			{
				subLeftPage.SetMinimized();
			}
			if ((bool)subRightPage)
			{
				subRightPage.SetMinimized();
			}
			leftSubDisplayTransform.gameObject.SetActive(value: false);
			rightSubDisplayTransform.gameObject.SetActive(value: false);
			minLeftSubButton.gameObject.SetActive(value: true);
			minRightSubButton.gameObject.SetActive(value: true);
			showSubsButton.SetActive(value: true);
			hideSubsButton.SetActive(value: false);
			maximizeButton.SetActive(value: true);
			unmaximizeButton.SetActive(value: false);
			half.hiddenMinButton.gameObject.SetActive(value: false);
		}
	}

	public void ShowSubs()
	{
		if (displayState == DisplayStates.HideSubs)
		{
			displayState = DisplayStates.ShowAll;
			base.transform.localPosition = origLP;
			displayTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, standardHeight);
			displayTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, standardWidth);
			if ((bool)displayedPage)
			{
				displayedPage.SetDisplayAll();
			}
			else
			{
				homepageInstance.SetDisplayAll();
			}
			if ((bool)subLeftPage)
			{
				subLeftPage.SetDisplayAsSub(leftSubDisplayTransform);
			}
			if ((bool)subRightPage)
			{
				subRightPage.SetDisplayAsSub(rightSubDisplayTransform);
			}
			leftSubDisplayTransform.gameObject.SetActive(value: true);
			rightSubDisplayTransform.gameObject.SetActive(value: true);
			minLeftSubButton.gameObject.SetActive(value: false);
			minRightSubButton.gameObject.SetActive(value: false);
			showSubsButton.SetActive(value: false);
			hideSubsButton.SetActive(value: true);
			maximizeButton.SetActive(value: true);
			unmaximizeButton.SetActive(value: false);
			half.hiddenMinButton.gameObject.SetActive(value: false);
		}
	}

	public void MaximizeDisplay()
	{
		if (displayState != DisplayStates.Maximized)
		{
			displayState = DisplayStates.Maximized;
			base.transform.localPosition = Vector3.zero;
			displayTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight);
			displayTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
			if ((bool)displayedPage)
			{
				displayedPage.SetMaximized();
			}
			else
			{
				homepageInstance.SetMaximized();
			}
			if ((bool)subLeftPage)
			{
				subLeftPage.SetDisplayAsSub(leftSubDisplayTransform);
			}
			if ((bool)subRightPage)
			{
				subRightPage.SetDisplayAsSub(rightSubDisplayTransform);
			}
			leftSubDisplayTransform.gameObject.SetActive(value: false);
			rightSubDisplayTransform.gameObject.SetActive(value: false);
			minLeftSubButton.gameObject.SetActive(value: true);
			minRightSubButton.gameObject.SetActive(value: true);
			otherQuarter.HideSubs();
			otherQuarter.HideQuarter();
			half.hiddenMinButton.gameObject.SetActive(value: true);
			SetHiddenQuarterMinButton(otherQuarter.displayedPage);
			showSubsButton.SetActive(value: false);
			hideSubsButton.SetActive(value: false);
			maximizeButton.SetActive(value: false);
			unmaximizeButton.SetActive(value: true);
		}
	}

	private void SetHiddenQuarterMinButton(MFDPortalPage page)
	{
		if ((bool)page)
		{
			half.hiddenMinButton.portalQtr = page.quarter;
		}
		if (page == null || page == page.quarter.homepageInstance)
		{
			half.hiddenMinButton.page = null;
			half.hiddenMinButton.buttonText.text = s_mfdpBLANK;
			half.hiddenMinButton.interactable.interactableName = s_mfdpOpenMenu;
		}
		else
		{
			page.minimizedButton = half.hiddenMinButton;
			half.hiddenMinButton.page = page;
			half.hiddenMinButton.buttonText.text = page.pageLabel;
			half.hiddenMinButton.interactable.interactableName = page.pageName;
		}
	}

	public void HideQuarter()
	{
		base.gameObject.SetActive(value: false);
		if ((bool)displayedPage)
		{
			displayedPage.SetMinimized();
		}
		if ((bool)subLeftPage)
		{
			subLeftPage.SetMinimized();
		}
		if ((bool)subRightPage)
		{
			subRightPage.SetMinimized();
		}
	}

	public void ShowQuarter()
	{
		base.gameObject.SetActive(value: true);
		switch (displayState)
		{
		case DisplayStates.ShowAll:
			if ((bool)displayedPage)
			{
				displayedPage.SetDisplayAll();
			}
			if ((bool)subLeftPage)
			{
				subLeftPage.SetDisplayAsSub(leftSubDisplayTransform);
			}
			if ((bool)subRightPage)
			{
				subRightPage.SetDisplayAsSub(rightSubDisplayTransform);
			}
			break;
		case DisplayStates.HideSubs:
			if ((bool)displayedPage)
			{
				displayedPage.SetHideSubs();
			}
			break;
		case DisplayStates.Maximized:
			if ((bool)displayedPage)
			{
				displayedPage.SetMaximized();
			}
			break;
		}
	}

	public void LeftSubPageButton()
	{
		if (!displayedPage || !displayedPage.locked)
		{
			if (!base.gameObject.activeSelf)
			{
				otherQuarter.HideSubs();
				MaximizeDisplay();
			}
			MFDPortalPage mFDPortalPage = subLeftPage;
			subLeftPage = null;
			MFDPortalPage mFDPortalPage2 = null;
			if ((bool)displayedPage && displayedPage != homepageInstance)
			{
				mFDPortalPage2 = displayedPage;
			}
			if ((bool)mFDPortalPage)
			{
				DisplayPage(mFDPortalPage);
			}
			else
			{
				DisplayMenu();
			}
			SetSubLeftPage(mFDPortalPage2);
			half.manager.PlayInputSound();
		}
	}

	public void RightSubPageButton()
	{
		if (!displayedPage || !displayedPage.locked)
		{
			if (!base.gameObject.activeSelf)
			{
				otherQuarter.HideSubs();
				MaximizeDisplay();
			}
			MFDPortalPage mFDPortalPage = subRightPage;
			subRightPage = null;
			MFDPortalPage mFDPortalPage2 = null;
			if ((bool)displayedPage && displayedPage != homepageInstance)
			{
				mFDPortalPage2 = displayedPage;
			}
			if ((bool)mFDPortalPage)
			{
				DisplayPage(mFDPortalPage);
			}
			else
			{
				DisplayMenu();
			}
			SetSubRightPage(mFDPortalPage2);
			half.manager.PlayInputSound();
		}
	}

	public void UpdateSOIUI()
	{
		if ((bool)displayedPage)
		{
			soiDisplayObject.SetActive(displayedPage.isSOI);
		}
		else
		{
			soiDisplayObject.SetActive(value: false);
		}
	}

	public void OnInputButtonDown()
	{
		if (hasPower && displayedPage.isSOI)
		{
			displayedPage.OnInputButtonDown.Invoke();
		}
	}

	public void OnInputButtonUp()
	{
		if (hasPower && displayedPage.isSOI)
		{
			displayedPage.OnInputButtonUp.Invoke();
		}
	}

	public void OnInputButton()
	{
		if (hasPower && displayedPage.isSOI)
		{
			displayedPage.OnInputButton.Invoke();
		}
	}

	public void OnInputAxis(Vector3 axis)
	{
		if (hasPower && displayedPage.isSOI)
		{
			displayedPage.OnInputAxis.Invoke(axis);
		}
	}

	public void OnInputAxisReleased()
	{
		if (hasPower && displayedPage.isSOI)
		{
			displayedPage.OnInputAxisReleased.Invoke();
		}
	}

	public void PlayInputSound()
	{
		half.PlayInputSound();
	}
}
