using System;
using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
	// Token: 0x02000649 RID: 1609
	[Serializable]
	public class SaveGame
	{
		// Token: 0x06003CFE RID: 15614 RVA: 0x0016A5FB File Offset: 0x001687FB
		public SaveGame()
		{
		}

		// Token: 0x06003CFF RID: 15615 RVA: 0x0016A619 File Offset: 0x00168819
		public SaveGame(string s, List<SceneObject> list)
		{
			this.savegameName = s;
			this.sceneObjects = list;
		}

		// Token: 0x0400397B RID: 14715
		public string savegameName = "New SaveGame";

		// Token: 0x0400397C RID: 14716
		public List<SceneObject> sceneObjects = new List<SceneObject>();
	}
}
