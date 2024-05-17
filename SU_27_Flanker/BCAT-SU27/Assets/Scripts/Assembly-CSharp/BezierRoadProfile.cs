using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BezierRoadProfile : ScriptableObject
{
	[Serializable]
	public class BridgeSupportProfile
	{
		public BridgeHeightProfile[] heightProfiles;

		public float minLength;
	}

	[Serializable]
	public class BridgeHeightProfile
	{
		public Mesh mesh;

		public float minHeight;

		public float minSegmentLength;
	}

	private class VTBridgeSupportProfile
	{
		public VTBridgeHeightProfile[] heightProfiles;

		public float minLength;

		public VTBridgeSupportProfile(BridgeSupportProfile bp)
		{
			heightProfiles = new VTBridgeHeightProfile[bp.heightProfiles.Length];
			for (int i = 0; i < heightProfiles.Length; i++)
			{
				heightProfiles[i] = new VTBridgeHeightProfile
				{
					mesh = new VTTerrainMesh(bp.heightProfiles[i].mesh),
					minHeight = bp.heightProfiles[i].minHeight,
					minSegmentLength = bp.heightProfiles[i].minSegmentLength
				};
			}
			minLength = bp.minLength;
		}
	}

	private class VTBridgeHeightProfile
	{
		public VTTerrainMesh mesh;

		public float minHeight;

		public float minSegmentLength;
	}

	public string profileName;

	[TextArea]
	public string profileDescription;

	public Texture2D thumbnail;

	public float radius;

	public Material roadMaterial;

	public Mesh[] segmentMeshes;

	public float lengthToIndexFactor;

	public Mesh segmentEndMesh;

	[Header("Intersection")]
	public Mesh intersectionMesh;

	public List<int> intersectionVertIdx;

	public int[] rightIntersectionVerts;

	public int[] leftIntersectionVerts;

	public Mesh bridgeAdapterMesh;

	public Mesh[] bridgeMeshes;

	public float lengthToBridgeIndexFactor;

	public Mesh bridgeIntersectionMesh;

	public List<int> bridgeIntersectionVertIdx;

	public int[] rightBridgeIntersectionVerts;

	public int[] leftBridgeIntersectionVerts;

	public Mesh bridgeEndMesh;

	public List<BridgeSupportProfile> bridgeSupportProfiles;

	private List<VTBridgeSupportProfile> vtBridgeSupports;

	private VTTerrainMesh[] segmentVTMeshes;

	public VTTerrainMesh intersectionVTMesh;

	public VTTerrainMesh bridgeAdapterVTMesh;

	private VTTerrainMesh[] bridgeVTMeshes;

	public VTTerrainMesh bridgeIntersectionVTMesh;

	public VTTerrainMesh segmentEndVTMesh;

	public VTTerrainMesh bridgeEndVTMesh;

	public void EnsureVTMeshes()
	{
		if (segmentVTMeshes == null || segmentVTMeshes.Length != segmentMeshes.Length)
		{
			CreateVTMeshes();
		}
	}

	private void CreateVTMeshes()
	{
		segmentVTMeshes = new VTTerrainMesh[segmentMeshes.Length];
		for (int i = 0; i < segmentVTMeshes.Length; i++)
		{
			segmentVTMeshes[i] = new VTTerrainMesh(segmentMeshes[i]);
		}
		segmentEndVTMesh = new VTTerrainMesh(segmentEndMesh);
		intersectionVTMesh = new VTTerrainMesh(intersectionMesh);
		bridgeAdapterVTMesh = new VTTerrainMesh(bridgeAdapterMesh);
		bridgeVTMeshes = new VTTerrainMesh[bridgeMeshes.Length];
		for (int j = 0; j < bridgeVTMeshes.Length; j++)
		{
			bridgeVTMeshes[j] = new VTTerrainMesh(bridgeMeshes[j]);
		}
		bridgeIntersectionVTMesh = new VTTerrainMesh(bridgeIntersectionMesh);
		bridgeEndVTMesh = new VTTerrainMesh(bridgeEndMesh);
		SetupBridgeSupports();
	}

	private void SetupBridgeSupports()
	{
		vtBridgeSupports = new List<VTBridgeSupportProfile>(bridgeSupportProfiles.Count);
		foreach (BridgeSupportProfile bridgeSupportProfile in bridgeSupportProfiles)
		{
			vtBridgeSupports.Add(new VTBridgeSupportProfile(bridgeSupportProfile));
		}
	}

	public VTTerrainMesh GetBridgeSupportMesh(float bridgeLength, float segmentLength, float height)
	{
		for (int num = vtBridgeSupports.Count - 1; num >= 0; num--)
		{
			if (bridgeLength > vtBridgeSupports[num].minLength)
			{
				for (int num2 = vtBridgeSupports[num].heightProfiles.Length - 1; num2 >= 0; num2--)
				{
					VTBridgeHeightProfile vTBridgeHeightProfile = vtBridgeSupports[num].heightProfiles[num2];
					if (height > vTBridgeHeightProfile.minHeight && segmentLength > vTBridgeHeightProfile.minSegmentLength)
					{
						return vTBridgeHeightProfile.mesh;
					}
				}
				return null;
			}
		}
		return null;
	}

	public VTTerrainMesh GetSegmentMesh(float segmentLength)
	{
		int value = Mathf.RoundToInt(segmentLength * lengthToIndexFactor);
		value = Mathf.Clamp(value, 0, segmentVTMeshes.Length - 1);
		return segmentVTMeshes[value];
	}

	public VTTerrainMesh GetBridgeMesh(float bridgeLength)
	{
		int value = Mathf.RoundToInt(bridgeLength * lengthToBridgeIndexFactor);
		value = Mathf.Clamp(value, 0, bridgeVTMeshes.Length - 1);
		return bridgeVTMeshes[value];
	}
}
