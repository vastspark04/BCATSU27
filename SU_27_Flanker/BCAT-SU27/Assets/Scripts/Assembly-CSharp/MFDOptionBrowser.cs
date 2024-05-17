using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MFDOptionBrowser : MonoBehaviour, ILocalizationUser
{
	public class MFDOptionPage
	{
		public string title;

		public List<MFDOption> options;

		public MFDOptionPage previousPage;

		public MFDOptionPage(string title, params MFDOption[] opts)
		{
			this.title = VTLocalizationManager.GetString("MFDOptionPage:" + title, title, "Title of an MFD option page");
			options = new List<MFDOption>();
			for (int i = 0; i < opts.Length; i++)
			{
				options.Add(opts[i]);
			}
		}
	}

	public class MFDOption
	{
		public string label;

		public UnityAction action;

		private MFDOptionPage targetPage;

		private MFDOptionBrowser browser;

		public MFDOption(string label, UnityAction action)
		{
			this.label = VTLocalizationManager.GetString("MFDOption:" + label, label, "Label of an MFD option");
			this.action = action;
		}

		public MFDOption(string label, MFDOptionPage targetPage, MFDOptionBrowser browser)
		{
			this.label = VTLocalizationManager.GetString("MFDOption:" + label, label, "Label of an MFD option");
			this.targetPage = targetPage;
			this.browser = browser;
			action = OpenTgtPage;
		}

		private void OpenTgtPage()
		{
			browser.OpenSubPage(targetPage);
		}
	}

	public MFDPage mfdPage;

	public MFDOptionPage currentPage;

	public MFDOptionPage homePage;

	private MFDPage.MFDButtonInfo backButton;

	private int subPageView;

	private MFDPage.MFDButtonInfo nextPageViewButton;

	private MFDPage.MFDButtonInfo prevPageViewButton;

	public Text titleText;

	public List<GameObject> labelObjects;

	public int maxOptionChars = 14;

	[Header("Non-MFD Objects")]
	public VRInteractable[] optionButtons;

	public VRInteractable backButtonTouch;

	public VRInteractable prevPageButtonTouch;

	public VRInteractable nextPageButtonTouch;

	private string s_cancel;

	private string s_next;

	private string s_prev;

	public void ApplyLocalization()
	{
		s_cancel = VTLocalizationManager.GetString("cancel", "Cancel");
		s_next = VTLocalizationManager.GetString("next", "Next");
		s_prev = VTLocalizationManager.GetString("prev", "Prev");
	}

	private void Awake()
	{
		ApplyLocalization();
		if ((bool)mfdPage)
		{
			backButton = new MFDPage.MFDButtonInfo();
			backButton.button = MFD.MFDButtons.B3;
			backButton.label = s_cancel;
			backButton.OnPress.AddListener(Back);
			backButton.toolTip = s_cancel;
			nextPageViewButton = new MFDPage.MFDButtonInfo();
			nextPageViewButton.button = MFD.MFDButtons.B5;
			nextPageViewButton.label = ">>";
			nextPageViewButton.OnPress.AddListener(NextPageView);
			nextPageViewButton.toolTip = s_next;
			prevPageViewButton = new MFDPage.MFDButtonInfo();
			prevPageViewButton.button = MFD.MFDButtons.B1;
			prevPageViewButton.label = "<<";
			prevPageViewButton.OnPress.AddListener(PrevPageView);
			prevPageViewButton.toolTip = s_prev;
		}
	}

	public void OpenSubPage(MFDOptionPage page)
	{
		page.previousPage = currentPage;
		SetupPage(page);
	}

	public void GoHomepage()
	{
		SetupPage(homePage);
	}

	private void SetupPage(MFDOptionPage page, int subPageIdx = 0)
	{
		currentPage = page;
		titleText.text = page.title;
		if ((bool)mfdPage)
		{
			mfdPage.mfd.ClearButtons();
		}
		else
		{
			VRInteractable[] array = optionButtons;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
			backButtonTouch.gameObject.SetActive(value: false);
			prevPageButtonTouch.gameObject.SetActive(value: false);
			nextPageButtonTouch.gameObject.SetActive(value: false);
		}
		subPageView = subPageIdx;
		if (page != homePage)
		{
			if ((bool)mfdPage)
			{
				mfdPage.SetPageButton(backButton);
			}
			else
			{
				backButtonTouch.gameObject.SetActive(value: true);
			}
		}
		foreach (GameObject labelObject in labelObjects)
		{
			labelObject.SetActive(value: false);
		}
		int num = subPageIdx * labelObjects.Count;
		int num2 = (subPageIdx + 1) * labelObjects.Count;
		for (int j = num; j < num2 && j < page.options.Count; j++)
		{
			int num3 = j % labelObjects.Count;
			if ((bool)mfdPage)
			{
				MFDPage.MFDButtonInfo mFDButtonInfo = new MFDPage.MFDButtonInfo();
				mFDButtonInfo.button = OptionIdxToButton(j);
				mFDButtonInfo.label = string.Empty;
				mFDButtonInfo.toolTip = page.options[j].label;
				if (page.options[j].action != null)
				{
					mFDButtonInfo.OnPress.AddListener(page.options[j].action);
				}
				mfdPage.SetPageButton(mFDButtonInfo);
			}
			else
			{
				VRInteractable vRInteractable = optionButtons[num3];
				vRInteractable.OnInteract.RemoveAllListeners();
				if (page.options[j].action != null)
				{
					vRInteractable.OnInteract.AddListener(page.options[j].action);
				}
				vRInteractable.interactableName = page.options[j].label;
			}
			labelObjects[num3].SetActive(value: true);
			string text = page.options[j].label;
			if (text.Length > maxOptionChars)
			{
				text = text.Substring(0, maxOptionChars);
			}
			labelObjects[num3].GetComponentInChildren<Text>().text = text;
			UIMaskedTextScroller componentInChildren = labelObjects[num3].GetComponentInChildren<UIMaskedTextScroller>();
			if ((bool)componentInChildren)
			{
				componentInChildren.Refresh();
			}
		}
		if (num > 0)
		{
			if ((bool)mfdPage)
			{
				mfdPage.SetPageButton(prevPageViewButton);
			}
			else
			{
				prevPageButtonTouch.gameObject.SetActive(value: true);
			}
		}
		if (page.options.Count > num2)
		{
			if ((bool)mfdPage)
			{
				mfdPage.SetPageButton(nextPageViewButton);
			}
			else
			{
				nextPageButtonTouch.gameObject.SetActive(value: true);
			}
		}
	}

	public void NextPageView()
	{
		subPageView++;
		SetupPage(currentPage, subPageView);
	}

	public void PrevPageView()
	{
		subPageView--;
		SetupPage(currentPage, subPageView);
	}

	private MFD.MFDButtons OptionIdxToButton(int idx)
	{
		idx %= 6;
		if (idx < 3)
		{
			return (MFD.MFDButtons)(idx + 2);
		}
		return (MFD.MFDButtons)(idx + 4);
	}

	public void Back()
	{
		SetupPage(currentPage.previousPage);
	}
}
