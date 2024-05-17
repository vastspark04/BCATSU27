using LapinerTools.Steam.Data;
using UnityEngine;
using System.Collections.Generic;

namespace LapinerTools.Steam
{
	public class SteamWorkshopMain : SteamMainBase<SteamWorkshopMain>
	{
		[SerializeField]
		private WorkshopSortMode m_sorting;
		[SerializeField]
		private string m_searchText;
		[SerializeField]
		private List<string> m_searchTags;
		[SerializeField]
		private bool m_searchMatchAnyTag;
		[SerializeField]
		private bool m_isSteamCacheEnabled;
	}
}
