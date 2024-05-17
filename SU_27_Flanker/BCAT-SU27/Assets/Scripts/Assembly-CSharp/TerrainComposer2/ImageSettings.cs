using System;
using UnityEngine;

namespace TerrainComposer2
{
	[Serializable]
	public class ImageSettings
	{
		[Serializable]
		public class ColChannel
		{
			public bool active;
			public Vector2 range;
		}

		public ColorSelectMode colSelectMode;
		public ColChannel[] colChannels;
	}
}
