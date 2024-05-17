using System.Collections.Generic;
using UnityEngine;

public class MFDManager : MonoBehaviour
{
	public List<MFD> mfds;

	public GameObject homepagePrefab;

	private Dictionary<string, MFDPage> pagesDic;

	[HideInInspector]
	public List<MFDPage> mfdPages;

	private void Awake()
	{
		SetupDict();
	}

	public void EnsureReady()
	{
		SetupDict();
	}

	private void SetupDict()
	{
		if (pagesDic != null)
		{
			return;
		}
		pagesDic = new Dictionary<string, MFDPage>();
		mfdPages = new List<MFDPage>();
		MFDPage[] componentsInChildren = GetComponentsInChildren<MFDPage>(includeInactive: true);
		foreach (MFDPage mFDPage in componentsInChildren)
		{
			if (pagesDic.ContainsKey(mFDPage.pageName))
			{
				Debug.LogError($"Duplicate MFD page name: {mFDPage.pageName}");
			}
			else
			{
				pagesDic.Add(mFDPage.pageName, mFDPage);
				mfdPages.Add(mFDPage);
				mFDPage.manager = this;
			}
			mFDPage.gameObject.SetActive(value: false);
		}
	}

	private void Start()
	{
		foreach (MFD mfd in mfds)
		{
			mfd.Initialize(this, Object.Instantiate(homepagePrefab).GetComponent<MFDPage>());
		}
		homepagePrefab.gameObject.SetActive(value: false);
	}

	public MFDPage GetPage(string pageName)
	{
		if (pagesDic == null)
		{
			SetupDict();
		}
		return pagesDic[pageName];
	}

	public void OnInputAxis(Vector3 axis)
	{
		for (int i = 0; i < mfds.Count; i++)
		{
			mfds[i].OnInputAxis(axis);
		}
	}

	public void OnInputButtonDown()
	{
		for (int i = 0; i < mfds.Count; i++)
		{
			mfds[i].OnInputButtonDown();
		}
	}

	public void OnInputAxisReleased()
	{
		for (int i = 0; i < mfds.Count; i++)
		{
			mfds[i].OnInputAxisReleased();
		}
	}

	public void OnInputButtonUp()
	{
		for (int i = 0; i < mfds.Count; i++)
		{
			mfds[i].OnInputButtonUp();
		}
	}

	public void OnInputButton()
	{
		for (int i = 0; i < mfds.Count; i++)
		{
			mfds[i].OnInputButton();
		}
	}
}
