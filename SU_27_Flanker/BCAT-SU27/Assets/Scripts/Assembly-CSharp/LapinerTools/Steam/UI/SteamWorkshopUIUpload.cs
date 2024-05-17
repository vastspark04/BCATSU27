using UnityEngine;
using UnityEngine.UI;

namespace LapinerTools.Steam.UI
{
	public class SteamWorkshopUIUpload : MonoBehaviour
	{
		[SerializeField]
		protected int ICON_WIDTH;
		[SerializeField]
		protected int ICON_HEIGHT;
		[SerializeField]
		protected InputField NAME_INPUT;
		[SerializeField]
		protected InputField DESCRIPTION_INPUT;
		[SerializeField]
		protected RawImage ICON;
		[SerializeField]
		protected Button SCREENSHOT_BUTTON;
		[SerializeField]
		protected Button UPLOAD_BUTTON;
		[SerializeField]
		protected bool m_improveNavigationFocus;
	}
}
