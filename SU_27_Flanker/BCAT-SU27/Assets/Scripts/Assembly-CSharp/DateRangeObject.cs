using System;
using UnityEngine;

public class DateRangeObject : MonoBehaviour
{
	public string startDate;

	public string endDate;

	private void Awake()
	{
		DateTime dateTime = DateTime.Parse(startDate);
		DateTime dateTime2 = DateTime.Parse(endDate);
		DateTime utcNow = DateTime.UtcNow;
		if (utcNow < dateTime || utcNow > dateTime2)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}
}
