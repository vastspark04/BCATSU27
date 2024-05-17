using System.Collections.Generic;

public static class LevelBuilderExtensions
{
	public static bool ContainsGrid(this List<LevelBuilder.SpecialChunk> list, IntVector2 grid, out int index)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].grid.Equals(grid))
			{
				index = i;
				return true;
			}
		}
		index = -1;
		return false;
	}
}
