using UnityEngine;

namespace TerrainComposer2{

public class Octree
{
	public class MaxCell
	{
	}

	public class Cell
	{
		public Cell mainParent;

		public Cell parent;

		public Cell[] cells;

		public bool[] cellsUsed;

		public Bounds bounds;

		public int cellIndex;

		public int cellCount;

		public int level;

		public Cell(Cell parent, int cellIndex, Bounds bounds)
		{
			mainParent = parent.mainParent;
			this.parent = parent;
			this.cellIndex = cellIndex;
			this.bounds = bounds;
			level = parent.level + 1;
		}

		private byte AddCell(Vector3 position)
		{
			Vector3 vector = position - this.bounds.min;
			int num = (int)(vector.x / this.bounds.extents.x);
			int num2 = (int)(vector.y / this.bounds.extents.y);
			int num3 = (int)(vector.z / this.bounds.extents.z);
			byte b = (byte)(num + num2 * 4 + num3 * 2);
			if (cells == null)
			{
				cells = new Cell[8];
				cellsUsed = new bool[8];
			}
			if (!cellsUsed[b])
			{
				Bounds bounds = new Bounds(new Vector3(this.bounds.min.x + this.bounds.extents.x * ((float)num + 0.5f), this.bounds.min.y + this.bounds.extents.y * ((float)num2 + 0.5f), this.bounds.min.z + this.bounds.extents.z * ((float)num3 + 0.5f)), this.bounds.extents);
				cells[b] = new Cell(this, b, bounds);
				cellsUsed[b] = true;
				cellCount++;
			}
			return b;
		}
	}

	public Cell cell;

	public int maxLevels;
}}
