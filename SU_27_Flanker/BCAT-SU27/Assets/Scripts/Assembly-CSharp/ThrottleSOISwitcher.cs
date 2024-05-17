using System.Collections.Generic;
using UnityEngine;

public class ThrottleSOISwitcher : MonoBehaviour
{
	public MFDManager mfdManager;

	public MFDPortalManager mfdpManager;

	public AudioSource inputAudioSource;

	public AudioClip switchedClip;

	private bool awaitingRelease;

	private List<int> toggleSoiBuffer = new List<int>(3);

	private MFDPortalQuarter[] portalQuarters = new MFDPortalQuarter[4];

	public void OnSetThumbstick(Vector3 ts)
	{
		float num = Mathf.Abs(ts.x);
		if (num > 0.8f && !awaitingRelease)
		{
			if (ts.x > 0f)
			{
				if ((bool)mfdManager)
				{
					SOIRight();
				}
				else if ((bool)mfdpManager)
				{
					PortalSOIRight();
				}
			}
			else if ((bool)mfdManager)
			{
				SOILeft();
			}
			else if ((bool)mfdpManager)
			{
				PortalSOILeft();
			}
			awaitingRelease = true;
		}
		else if (awaitingRelease && num < 0.1f)
		{
			awaitingRelease = false;
		}
	}

	public void ToggleSOI()
	{
		if (!mfdManager)
		{
			return;
		}
		toggleSoiBuffer.Clear();
		int num = -1;
		for (int i = 0; i < mfdManager.mfds.Count; i++)
		{
			MFDPage activePage = mfdManager.mfds[i].activePage;
			if (activePage.canSOI)
			{
				toggleSoiBuffer.Add(i);
			}
			if (activePage.isSOI)
			{
				num = toggleSoiBuffer.Count - 1;
				activePage.ToggleInput();
			}
		}
		if (toggleSoiBuffer.Count <= 0)
		{
			return;
		}
		if (num == -1)
		{
			mfdManager.mfds[toggleSoiBuffer[0]].activePage.ToggleInput();
			if ((bool)inputAudioSource && (bool)switchedClip)
			{
				inputAudioSource.PlayOneShot(switchedClip);
			}
			return;
		}
		num = (num + 1) % toggleSoiBuffer.Count;
		mfdManager.mfds[toggleSoiBuffer[num]].activePage.ToggleInput();
		if (toggleSoiBuffer.Count > 1 && (bool)inputAudioSource && (bool)switchedClip)
		{
			inputAudioSource.PlayOneShot(switchedClip);
		}
	}

	private void PortalSOIRight()
	{
		SetPortalQuarters();
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < portalQuarters.Length; i++)
		{
			if (flag2)
			{
				break;
			}
			MFDPortalQuarter mFDPortalQuarter = portalQuarters[i];
			if (mFDPortalQuarter.displayedPage != null)
			{
				if (mFDPortalQuarter.displayedPage.isSOI)
				{
					flag = true;
				}
				else if (flag && mFDPortalQuarter.displayedPage.canSOI && mFDPortalQuarter.displayedPage.pageState != MFDPortalPage.PageStates.Minimized && mFDPortalQuarter.displayedPage.pageState != MFDPortalPage.PageStates.SubSized)
				{
					flag2 = true;
					mFDPortalQuarter.displayedPage.ToggleInput();
				}
			}
		}
		if (!flag)
		{
			int num = portalQuarters.Length - 1;
			while (num >= 0 && !flag2)
			{
				MFDPortalQuarter mFDPortalQuarter2 = portalQuarters[num];
				if (mFDPortalQuarter2.displayedPage != null && !mFDPortalQuarter2.displayedPage.isSOI && mFDPortalQuarter2.displayedPage.canSOI && mFDPortalQuarter2.displayedPage.pageState != MFDPortalPage.PageStates.Minimized && mFDPortalQuarter2.displayedPage.pageState != MFDPortalPage.PageStates.SubSized)
				{
					mFDPortalQuarter2.displayedPage.ToggleInput();
					flag2 = true;
				}
				num--;
			}
		}
		if (flag2 && (bool)inputAudioSource && (bool)switchedClip)
		{
			inputAudioSource.PlayOneShot(switchedClip);
		}
	}

	private void PortalSOILeft()
	{
		SetPortalQuarters();
		bool flag = false;
		bool flag2 = false;
		int num = portalQuarters.Length - 1;
		while (num >= 0 && !flag2)
		{
			MFDPortalQuarter mFDPortalQuarter = portalQuarters[num];
			if (mFDPortalQuarter.displayedPage != null)
			{
				if (mFDPortalQuarter.displayedPage.isSOI)
				{
					flag = true;
				}
				else if (flag && mFDPortalQuarter.displayedPage.canSOI && mFDPortalQuarter.displayedPage.pageState != MFDPortalPage.PageStates.Minimized && mFDPortalQuarter.displayedPage.pageState != MFDPortalPage.PageStates.SubSized)
				{
					flag2 = true;
					mFDPortalQuarter.displayedPage.ToggleInput();
				}
			}
			num--;
		}
		if (!flag)
		{
			for (int i = 0; i < portalQuarters.Length; i++)
			{
				if (flag2)
				{
					break;
				}
				MFDPortalQuarter mFDPortalQuarter2 = portalQuarters[i];
				if (mFDPortalQuarter2.displayedPage != null && !mFDPortalQuarter2.displayedPage.isSOI && mFDPortalQuarter2.displayedPage.canSOI && mFDPortalQuarter2.displayedPage.pageState != MFDPortalPage.PageStates.Minimized && mFDPortalQuarter2.displayedPage.pageState != MFDPortalPage.PageStates.SubSized)
				{
					mFDPortalQuarter2.displayedPage.ToggleInput();
					flag2 = true;
				}
			}
		}
		if (flag2 && (bool)inputAudioSource && (bool)switchedClip)
		{
			inputAudioSource.PlayOneShot(switchedClip);
		}
	}

	private void SetPortalQuarters()
	{
		portalQuarters[0] = mfdpManager.halfLeft.quarterLeft;
		portalQuarters[1] = mfdpManager.halfLeft.quarterRight;
		portalQuarters[2] = mfdpManager.halfRight.quarterLeft;
		portalQuarters[3] = mfdpManager.halfRight.quarterRight;
	}

	private void SOIRight()
	{
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < mfdManager.mfds.Count; i++)
		{
			if (flag2)
			{
				break;
			}
			MFD mFD = mfdManager.mfds[i];
			if (mFD.activePage.isSOI)
			{
				flag = true;
			}
			else if (flag && mFD.activePage.canSOI)
			{
				flag2 = true;
				mFD.activePage.ToggleInput();
			}
		}
		if (!flag)
		{
			int num = mfdManager.mfds.Count - 1;
			while (num >= 0 && !flag2)
			{
				MFD mFD2 = mfdManager.mfds[num];
				if (!mFD2.activePage.isSOI && mFD2.activePage.canSOI)
				{
					mFD2.activePage.ToggleInput();
					flag2 = true;
				}
				num--;
			}
		}
		if (flag2 && (bool)inputAudioSource && (bool)switchedClip)
		{
			inputAudioSource.PlayOneShot(switchedClip);
		}
	}

	private void SOILeft()
	{
		bool flag = false;
		bool flag2 = false;
		int num = mfdManager.mfds.Count - 1;
		while (num >= 0 && !flag2)
		{
			MFD mFD = mfdManager.mfds[num];
			if (mFD.activePage.isSOI)
			{
				flag = true;
			}
			else if (flag && mFD.activePage.canSOI)
			{
				flag2 = true;
				mFD.activePage.ToggleInput();
			}
			num--;
		}
		if (!flag)
		{
			for (int i = 0; i < mfdManager.mfds.Count; i++)
			{
				if (flag2)
				{
					break;
				}
				MFD mFD2 = mfdManager.mfds[i];
				if (!mFD2.activePage.isSOI && mFD2.activePage.canSOI)
				{
					mFD2.activePage.ToggleInput();
					flag2 = true;
				}
			}
		}
		if (flag2 && (bool)inputAudioSource && (bool)switchedClip)
		{
			inputAudioSource.PlayOneShot(switchedClip);
		}
	}
}
