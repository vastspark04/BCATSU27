using System;

namespace UnityEngine.UI.Extensions.Tweens
{
	// Token: 0x0200066B RID: 1643
	internal interface ITweenValue
	{
		// Token: 0x06003DD4 RID: 15828
		void TweenValue(float floatPercentage);

		// Token: 0x170006E0 RID: 1760
		// (get) Token: 0x06003DD5 RID: 15829
		bool ignoreTimeScale { get; }

		// Token: 0x170006E1 RID: 1761
		// (get) Token: 0x06003DD6 RID: 15830
		float duration { get; }

		// Token: 0x06003DD7 RID: 15831
		bool ValidTarget();

		// Token: 0x06003DD8 RID: 15832
		void Finished();
	}
}
