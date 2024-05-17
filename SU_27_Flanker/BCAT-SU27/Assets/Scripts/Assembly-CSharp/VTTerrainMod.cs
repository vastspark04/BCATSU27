public abstract class VTTerrainMod
{
	public int prefabID;

	public virtual void ApplyHeightMod(VTTerrainJob job, VTTerrainMesh mesh)
	{
	}

	public virtual void ApplyColorMod(VTTerrainJob job, VTTerrainMesh mesh)
	{
	}

	public virtual bool AppliesToChunk(VTMapGenerator.VTTerrainChunk chunk)
	{
		return false;
	}
}
