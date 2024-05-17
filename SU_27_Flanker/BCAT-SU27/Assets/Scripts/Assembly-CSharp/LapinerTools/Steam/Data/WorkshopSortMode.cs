using System;
using Steamworks;
using UnityEngine;

namespace LapinerTools.Steam.Data
{
	[Serializable]
	public class WorkshopSortMode
	{
		[SerializeField]
		public EUGCQuery MODE;
		[SerializeField]
		public EWorkshopSource SOURCE;
	}
}
