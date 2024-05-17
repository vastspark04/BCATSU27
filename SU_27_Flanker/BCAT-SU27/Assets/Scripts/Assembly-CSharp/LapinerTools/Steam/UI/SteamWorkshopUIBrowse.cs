using UnityEngine;
using System;
using LapinerTools.Steam.Data;
using LapinerTools.uMyGUI;
using UnityEngine.UI;

namespace LapinerTools.Steam.UI
{
	public class SteamWorkshopUIBrowse : MonoBehaviour
	{
		[Serializable]
		public class SortingConfig
		{
			[Serializable]
			public class Option
			{
				[SerializeField]
				public WorkshopSortMode MODE;
				[SerializeField]
				public string DISPLAY_TEXT;
			}

			[SerializeField]
			public uMyGUI_Dropdown DROPDOWN;
			[SerializeField]
			public int DEFAULT_SORT_MODE;
			[SerializeField]
			public Option[] OPTIONS;
		}

		[SerializeField]
		protected uMyGUI_TreeBrowser ITEM_BROWSER;
		[SerializeField]
		protected uMyGUI_PageBox PAGE_SELCTOR;
		[SerializeField]
		public SortingConfig SORTING;
		[SerializeField]
		protected InputField SEARCH_INPUT;
		[SerializeField]
		protected Button SEARCH_BUTTON;
		[SerializeField]
		protected bool m_loadOnStart;
		[SerializeField]
		protected bool m_improveNavigationFocus;
		public bool showPopups;
	}
}
