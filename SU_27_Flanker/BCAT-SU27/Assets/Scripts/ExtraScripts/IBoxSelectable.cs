using System;

namespace UnityEngine.UI.Extensions
{
	// Token: 0x02000609 RID: 1545
	public interface IBoxSelectable
	{
		// Token: 0x1700068C RID: 1676
		// (get) Token: 0x06003B25 RID: 15141
		// (set) Token: 0x06003B26 RID: 15142
		bool selected { get; set; }

		// Token: 0x1700068D RID: 1677
		// (get) Token: 0x06003B27 RID: 15143
		// (set) Token: 0x06003B28 RID: 15144
		bool preSelected { get; set; }

		// Token: 0x1700068E RID: 1678
		// (get) Token: 0x06003B29 RID: 15145
		Transform transform { get; }
	}
}
