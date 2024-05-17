using System.Collections.Generic;
using UnityEngine;

public class LevelBuilderCreator : MonoBehaviour
{
	public LevelBuilder levelBuilder;

	public IntVector2 editCenterPos;

	public int editRange;

	private List<GameObject> activeChunks = new List<GameObject>();

	[HideInInspector]
	public IntVector2 lastEditingCenterPos;

	[HideInInspector]
	public int currentPrefabIdx;

	[HideInInspector]
	public int currentPrefabRot;

	private GameObject creatorObject;

	private GameObject selectedChunkObject;

	private GridPlatoon[] gridPlatoons;

	[HideInInspector]
	public int seaWallIdx;

	[HideInInspector]
	public int innerCornerIdx;

	[HideInInspector]
	public int outerCornerIdx;

	[HideInInspector]
	public int landStartIdx;

	public int prevPrefabCount;

	[HideInInspector]
	public bool prefabCountDirty;

	private void OnDrawGizmosSelected()
	{
		if ((bool)levelBuilder)
		{
			Gizmos.DrawWireCube(Vector3.zero + levelBuilder.tileSize / 4f * Vector3.up, new Vector3(levelBuilder.tileSize, levelBuilder.tileSize / 2f, levelBuilder.tileSize));
		}
	}

	public void RefreshMap()
	{
		ClearMap();
		if (levelBuilder.legacyMapMode)
		{
			int num = levelBuilder.chunkPrefabs.Length;
			if (num != prevPrefabCount)
			{
				float factor = ((float)prevPrefabCount + 1f) / (float)num;
				RefactorTexture(factor);
				prevPrefabCount = num;
				prefabCountDirty = true;
			}
		}
		levelBuilder.RefreshMapInfo();
		GridPlatoon[] array = gridPlatoons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
		if (!creatorObject)
		{
			creatorObject = new GameObject("LevelCreator");
			creatorObject.tag = "LevelCreator";
		}
		creatorObject.transform.position = Vector3.zero;
		for (int j = editCenterPos.x - editRange; j <= editCenterPos.x + editRange; j++)
		{
			for (int k = editCenterPos.y - editRange; k <= editCenterPos.y + editRange; k++)
			{
				IntVector2 intVector = new IntVector2(j, k);
				GameObject gameObject = levelBuilder.GenerateGridEditor(intVector, editCenterPos, creatorObject);
				activeChunks.Add(gameObject);
				if (intVector.Equals(editCenterPos))
				{
					selectedChunkObject = gameObject;
				}
				array = gridPlatoons;
				foreach (GridPlatoon gridPlatoon in array)
				{
					if (gridPlatoon.spawnInGrid.Equals(intVector))
					{
						gridPlatoon.gameObject.SetActive(value: true);
						gridPlatoon.transform.position = gameObject.transform.position;
					}
				}
			}
		}
		lastEditingCenterPos = editCenterPos;
		if (EditPosIsInRange())
		{
			LevelBuilder.ChunkMapInfo mapInfo = levelBuilder.GetMapInfo(editCenterPos);
			currentPrefabIdx = mapInfo.prefabIdx;
			currentPrefabRot = mapInfo.rotation;
		}
	}

	public void ClearMap()
	{
		activeChunks.RemoveAll((GameObject x) => x == null);
		foreach (GameObject activeChunk in activeChunks)
		{
			Object.DestroyImmediate(activeChunk);
		}
		activeChunks.Clear();
		if (!creatorObject)
		{
			creatorObject = GameObject.FindWithTag("LevelCreator");
		}
		if ((bool)creatorObject)
		{
			Object.DestroyImmediate(creatorObject);
			creatorObject = null;
		}
		GameObject gameObject = GameObject.FindWithTag("Platoons");
		if ((bool)gameObject)
		{
			gridPlatoons = gameObject.GetComponentsInChildren<GridPlatoon>(includeInactive: true);
			GridPlatoon[] array = gridPlatoons;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: true);
			}
		}
	}

	public void RefactorTexture(float factor)
	{
		for (int i = 0; i < levelBuilder.width; i++)
		{
			for (int j = 0; j < levelBuilder.height; j++)
			{
				Color pixel = levelBuilder.chunkMap.GetPixel(i, j);
				pixel.r = factor * pixel.r;
				pixel.g = pixel.r;
				levelBuilder.chunkMap.SetPixel(i, j, pixel);
				levelBuilder.chunkMap.Apply();
			}
		}
	}

	public void RotateAll180()
	{
		for (int i = 0; i < levelBuilder.width; i++)
		{
			for (int j = 0; j < levelBuilder.height; j++)
			{
				Color pixel = levelBuilder.chunkMap.GetPixel(i, j);
				int num = Mathf.RoundToInt(pixel.a * 3f);
				num = (num + 2) % 4;
				pixel.a = (float)num / 3f;
				levelBuilder.chunkMap.SetPixel(i, j, pixel);
			}
		}
		levelBuilder.chunkMap.Apply();
		RefreshMap();
	}

	private void WritePixel(LevelBuilder.ChunkMapInfo info)
	{
		Color color = levelBuilder.MapInfoToPixel(info);
		levelBuilder.chunkMap.SetPixel(info.grid.x, info.grid.y, color);
		levelBuilder.RefreshMapInfo();
		levelBuilder.chunkMap.Apply();
	}

	public void ApplyChunk(int prefabIdx, int rotation)
	{
		if (levelBuilder.GetMapInfo(editCenterPos).special)
		{
			ClearSpecial();
		}
		LevelBuilder.ChunkMapInfo info = new LevelBuilder.ChunkMapInfo(editCenterPos, prefabIdx, rotation);
		WritePixel(info);
		RefreshMap();
	}

	public void PaintChunk(LevelBuilder.ChunkMapInfo info, IntVector2 grid)
	{
		LevelBuilder.ChunkMapInfo info2 = new LevelBuilder.ChunkMapInfo(grid, info.prefabIdx, info.rotation);
		if (levelBuilder.GetMapInfo(grid).special)
		{
			ClearSpecial(grid);
		}
		WritePixel(info2);
		RefreshMap();
	}

	public void ApplyRotationOnly(int rotation)
	{
		LevelBuilder.ChunkMapInfo mapInfo = levelBuilder.GetMapInfo(editCenterPos);
		if (mapInfo.special)
		{
			for (int i = 0; i < levelBuilder.specialChunks.Count; i++)
			{
				if (levelBuilder.specialChunks[i].grid.Equals(editCenterPos))
				{
					LevelBuilder.SpecialChunk value = new LevelBuilder.SpecialChunk(levelBuilder.specialChunks[i]);
					value.rotation = rotation;
					levelBuilder.specialChunks[i] = value;
					break;
				}
			}
		}
		LevelBuilder.ChunkMapInfo info = new LevelBuilder.ChunkMapInfo(editCenterPos, mapInfo.prefabIdx, rotation, mapInfo.special);
		WritePixel(info);
		RefreshMap();
	}

	public void ApplyAsSpecial()
	{
		GameObject gameObject = selectedChunkObject;
		GameObject gameObject2 = GameObject.FindWithTag("LevelCreatorPrefabs");
		if (!gameObject2)
		{
			gameObject2 = new GameObject("LevelCreator Prefabs");
			gameObject2.tag = "LevelCreatorPrefabs";
		}
		gameObject.transform.parent = gameObject2.transform;
		gameObject.SetActive(value: false);
		activeChunks.Remove(selectedChunkObject);
		ClearSpecial();
		levelBuilder.specialChunkPrefabs.Add(gameObject);
		LevelBuilder.SpecialChunk item = default(LevelBuilder.SpecialChunk);
		item.chunkIndex = levelBuilder.specialChunkPrefabs.Count - 1;
		item.grid = editCenterPos;
		item.rotation = currentPrefabRot;
		levelBuilder.specialChunks.Add(item);
		LevelBuilder.ChunkMapInfo info = new LevelBuilder.ChunkMapInfo(editCenterPos, item.chunkIndex, item.rotation, special: true);
		WritePixel(info);
		RefreshMap();
	}

	public void ClearSpecial()
	{
		ClearSpecial(editCenterPos);
	}

	public void ClearSpecial(IntVector2 clearGrid)
	{
		LevelBuilder.SpecialChunk item = default(LevelBuilder.SpecialChunk);
		bool flag = false;
		int num = -1;
		foreach (LevelBuilder.SpecialChunk specialChunk in levelBuilder.specialChunks)
		{
			IntVector2 grid = specialChunk.grid;
			if (grid.Equals(clearGrid))
			{
				item = specialChunk;
				flag = true;
				num = specialChunk.chunkIndex;
				break;
			}
		}
		if (flag)
		{
			levelBuilder.specialChunks.Remove(item);
			bool flag2 = true;
			foreach (LevelBuilder.SpecialChunk specialChunk2 in levelBuilder.specialChunks)
			{
				if (specialChunk2.chunkIndex == num)
				{
					flag2 = false;
				}
			}
			if (flag2)
			{
				GameObject gameObject = levelBuilder.specialChunkPrefabs[num];
				levelBuilder.specialChunkPrefabs.RemoveAt(num);
				for (int i = 0; i < levelBuilder.specialChunks.Count; i++)
				{
					if (levelBuilder.specialChunks[i].chunkIndex > num)
					{
						LevelBuilder.SpecialChunk value = new LevelBuilder.SpecialChunk(levelBuilder.specialChunks[i]);
						value.chunkIndex--;
						levelBuilder.specialChunks[i] = value;
					}
				}
				GameObject gameObject2 = GameObject.FindWithTag("LevelCreatorTrash");
				if (!gameObject2)
				{
					gameObject2 = new GameObject("LevelCreator Trash");
					gameObject2.tag = "LevelCreatorTrash";
				}
				gameObject.transform.parent = gameObject2.transform;
				gameObject.SetActive(value: false);
			}
			LevelBuilder.ChunkMapInfo info = new LevelBuilder.ChunkMapInfo(clearGrid, 0, 0);
			WritePixel(info);
		}
		RefreshMap();
	}

	public bool EditPosIsInRange()
	{
		if (editCenterPos.x >= 0 && editCenterPos.x < levelBuilder.chunkMap.width && editCenterPos.y >= 0)
		{
			return editCenterPos.y < levelBuilder.chunkMap.height;
		}
		return false;
	}

	public void GenerateSeaWalls()
	{
		for (int i = 0; i < levelBuilder.width; i++)
		{
			for (int j = 0; j < levelBuilder.height; j++)
			{
				IntVector2 grid = new IntVector2(i, j);
				if (levelBuilder.GetMapInfo(grid).prefabIdx >= landStartIdx)
				{
					continue;
				}
				IntVector2 grid2 = new IntVector2(i, j + 1);
				IntVector2 grid3 = new IntVector2(i - 1, j + 1);
				IntVector2 grid4 = new IntVector2(i + 1, j + 1);
				IntVector2 grid5 = new IntVector2(i - 1, j);
				IntVector2 grid6 = new IntVector2(i + 1, j);
				IntVector2 grid7 = new IntVector2(i, j - 1);
				IntVector2 grid8 = new IntVector2(i - 1, j - 1);
				IntVector2 grid9 = new IntVector2(i + 1, j - 1);
				bool flag = CheckIsLand(grid2);
				bool flag2 = CheckIsLand(grid3);
				bool flag3 = CheckIsLand(grid4);
				bool flag4 = CheckIsLand(grid5);
				bool flag5 = CheckIsLand(grid6);
				bool flag6 = CheckIsLand(grid7);
				bool flag7 = CheckIsLand(grid8);
				bool flag8 = CheckIsLand(grid9);
				if (!(flag || flag2 || flag3 || flag4 || flag5 || flag6 || flag8 || flag7))
				{
					continue;
				}
				int prefabIdx = 0;
				int rotation = 0;
				if (flag4)
				{
					if (!flag && !flag6)
					{
						prefabIdx = seaWallIdx;
						rotation = 3;
					}
					else if (flag)
					{
						prefabIdx = innerCornerIdx;
						rotation = 3;
					}
					else if (flag6)
					{
						prefabIdx = innerCornerIdx;
						rotation = 2;
					}
				}
				else if (flag5)
				{
					if (!flag && !flag6)
					{
						prefabIdx = seaWallIdx;
						rotation = 1;
					}
					else if (flag)
					{
						prefabIdx = innerCornerIdx;
						rotation = 0;
					}
					else if (flag6)
					{
						prefabIdx = innerCornerIdx;
						rotation = 1;
					}
				}
				else if (flag6)
				{
					prefabIdx = seaWallIdx;
					rotation = 2;
				}
				else if (flag)
				{
					prefabIdx = seaWallIdx;
					rotation = 0;
				}
				else if (flag3)
				{
					prefabIdx = outerCornerIdx;
					rotation = 0;
				}
				else if (flag8)
				{
					prefabIdx = outerCornerIdx;
					rotation = 1;
				}
				else if (flag7)
				{
					prefabIdx = outerCornerIdx;
					rotation = 2;
				}
				else if (flag2)
				{
					prefabIdx = outerCornerIdx;
					rotation = 3;
				}
				PaintChunk(new LevelBuilder.ChunkMapInfo(grid, prefabIdx, rotation), grid);
			}
		}
	}

	private bool CheckIsLand(IntVector2 grid)
	{
		return levelBuilder.GetMapInfo(grid).prefabIdx >= landStartIdx;
	}

	public void ClearAllMapData()
	{
		Texture2D chunkMap = levelBuilder.chunkMap;
		for (int i = 0; i < chunkMap.width; i++)
		{
			for (int j = 0; j < chunkMap.height; j++)
			{
				chunkMap.SetPixel(i, j, Color.black);
			}
		}
		levelBuilder.RefreshMapInfo();
		levelBuilder.chunkMap.Apply();
	}
}
