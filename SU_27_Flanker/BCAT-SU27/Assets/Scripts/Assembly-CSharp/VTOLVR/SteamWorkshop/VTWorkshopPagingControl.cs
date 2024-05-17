using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.SteamWorkshop{

public class VTWorkshopPagingControl : MonoBehaviour
{
	public GameObject nextButtonObj;

	public GameObject prevButtonObj;

	public GameObject pageNumberTemplate;

	public Text currentPageText;

	public VTSteamWorkshopBrowser browser;

	public int maxPageNumbers;

	private List<GameObject> objs = new List<GameObject>();

	private int currPage;

	private int numPages;

	private void Awake()
	{
		pageNumberTemplate.SetActive(value: false);
	}

	public void SetPage(int currentPage, int numPages)
	{
		currPage = currentPage;
		this.numPages = numPages;
		foreach (GameObject obj in objs)
		{
			Object.Destroy(obj);
		}
		objs.Clear();
		float num = pageNumberTemplate.transform.localPosition.x;
		float num2 = ((RectTransform)pageNumberTemplate.transform).rect.width * pageNumberTemplate.transform.localScale.x;
		int num3 = maxPageNumbers / 2;
		int num4 = Mathf.Max(1, currentPage - num3);
		for (int num5 = Mathf.Min(numPages, num4 + num3 + 1 + num3); num5 >= num4; num5--)
		{
			GameObject gameObject;
			if (num5 == currentPage)
			{
				gameObject = currentPageText.gameObject;
				currentPageText.text = currentPage.ToString();
			}
			else
			{
				gameObject = Object.Instantiate(pageNumberTemplate, pageNumberTemplate.transform.parent);
				gameObject.GetComponentInChildren<Text>().text = num5.ToString();
				int p = num5;
				gameObject.GetComponent<Button>().onClick.AddListener(delegate
				{
					browser.DisplayPage(p);
				});
				objs.Add(gameObject);
			}
			gameObject.SetActive(value: true);
			Vector3 localPosition = gameObject.transform.localPosition;
			localPosition.x = num;
			gameObject.transform.localPosition = localPosition;
			num -= num2;
		}
		Vector3 localPosition2 = prevButtonObj.transform.localPosition;
		localPosition2.x = num;
		prevButtonObj.transform.localPosition = localPosition2;
		prevButtonObj.SetActive(currentPage > 1);
		nextButtonObj.SetActive(currentPage < numPages);
	}

	public void NextPage()
	{
		browser.DisplayPage(currPage + 1);
	}

	public void PrevPage()
	{
		browser.DisplayPage(currPage - 1);
	}
}

}