using UnityEngine;

public class MFDSwapper : MonoBehaviour
{
	public MFD a;

	public MFD b;

	public void Swap()
	{
		if (a.powerOn && b.powerOn)
		{
			string pageName = a.activePage.pageName;
			string pageName2 = b.activePage.pageName;
			a.SetPage(pageName2);
			b.SetPage(pageName);
		}
	}
}
