using System.Collections;
using UnityEngine;

public class VTOLMPBriefingMap : MonoBehaviour
{
	public MeshRenderer mapRenderer;

	public Material waterEdgeMat;

	public Material hillsEdgeMat;

	public Material northCoastMat;

	public Material eastCoastMat;

	public Material westCoastMat;

	public Material southCoastMat;

	private IEnumerator Start()
	{
		while (!VTCustomMapManager.instance || !VTCustomMapManager.instance.mapGenerator.HasFinishedInitialGeneration())
		{
			yield return null;
		}
		Material material = waterEdgeMat;
		switch (VTCustomMapManager.instance.mapGenerator.edgeMode)
		{
		case VTMapGenerator.EdgeModes.Water:
			material = waterEdgeMat;
			break;
		case VTMapGenerator.EdgeModes.Hills:
			material = hillsEdgeMat;
			break;
		case VTMapGenerator.EdgeModes.Coast:
			switch (VTCustomMapManager.instance.mapGenerator.coastSide)
			{
			case CardinalDirections.North:
				material = northCoastMat;
				break;
			case CardinalDirections.East:
				material = eastCoastMat;
				break;
			case CardinalDirections.South:
				material = southCoastMat;
				break;
			case CardinalDirections.West:
				material = westCoastMat;
				break;
			}
			break;
		}
		mapRenderer.material = material;
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetTexture("_MainTex", VTCustomMapManager.instance.mapGenerator.heightMap);
		mapRenderer.SetPropertyBlock(materialPropertyBlock);
	}
}
