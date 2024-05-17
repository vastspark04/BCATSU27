using System;
using UnityEngine;

public class MFDSpecificObjects : MonoBehaviour
{
	[Serializable]
	public class MFDObject
	{
		public int[] mfds;

		public GameObject[] objects;
	}

	public MFDPage page;

	public MFDObject[] objects;

	private void Awake()
	{
		page.OnActivatePage.AddListener(OnActivate);
	}

	private void OnActivate()
	{
		int s = page.manager.mfds.IndexOf(page.mfd);
		MFDObject[] array = objects;
		foreach (MFDObject mFDObject in array)
		{
			mFDObject.objects.SetActive(mFDObject.mfds.Contains(s));
		}
	}
}
