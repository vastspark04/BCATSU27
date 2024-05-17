using UnityEngine;
using UnityEngine.UI;

namespace LapinerTools.Steam.UI
{
	public class SteamWorkshopItemNode : MonoBehaviour
	{
		[SerializeField]
		protected Text m_textName;
		[SerializeField]
		protected Text m_textDescription;
		[SerializeField]
		protected Text m_textVotes;
		[SerializeField]
		protected Button m_btnVotesUp;
		[SerializeField]
		protected Button m_btnVotesUpActive;
		[SerializeField]
		protected Button m_btnVotesDown;
		[SerializeField]
		protected Button m_btnVotesDownActive;
		[SerializeField]
		protected Text m_textFavorites;
		[SerializeField]
		protected Button m_btnFavorites;
		[SerializeField]
		protected Button m_btnFavoritesActive;
		[SerializeField]
		protected Text m_textSubscriptions;
		[SerializeField]
		protected Text m_textDownloadProgress;
		[SerializeField]
		protected Button m_btnSubscriptions;
		[SerializeField]
		protected Button m_btnSubscriptionsActive;
		[SerializeField]
		protected RawImage m_image;
		[SerializeField]
		protected Image m_selectionImage;
		[SerializeField]
		protected Button m_btnDownload;
		[SerializeField]
		protected Button m_btnPlay;
		[SerializeField]
		protected Button m_btnDelete;
		[SerializeField]
		protected bool m_useExplicitNavigation;
		[SerializeField]
		protected bool m_improveNavigationFocus;
	}
}
