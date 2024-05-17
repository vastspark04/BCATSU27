using UnityEngine;

namespace Oculus.Platform
{
	public class PlatformSettings : ScriptableObject
	{
		[SerializeField]
		private string ovrAppID;
		[SerializeField]
		private string ovrMobileAppID;
		[SerializeField]
		private bool ovrUseStandalonePlatform;
		[SerializeField]
		private bool ovrEnableARM64Support;
	}
}
