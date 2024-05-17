using UnityEngine;
using System;

public class OvrAvatarMaterialManager : MonoBehaviour
{
	[Serializable]
	public struct AvatarComponentMaterialProperties
	{
		public ovrAvatarBodyPartType TypeIndex;
		public Color Color;
		public Texture2D[] Textures;
		public float DiffuseIntensity;
		public float RimIntensity;
		public float ReflectionIntensity;
	}

	[Serializable]
	public struct AvatarMaterialPropertyBlock
	{
		public Vector4[] Colors;
		public float[] DiffuseIntensities;
		public float[] RimIntensities;
		public float[] ReflectionIntensities;
	}

	[Serializable]
	public class AvatarMaterialConfig
	{
		public OvrAvatarMaterialManager.AvatarComponentMaterialProperties[] ComponentMaterialProperties;
		public OvrAvatarMaterialManager.AvatarMaterialPropertyBlock MaterialPropertyBlock;
	}

	public Texture2D[] DiffuseFallbacks;
	public Texture2D[] NormalFallbacks;
	public AvatarMaterialConfig LocalAvatarConfig;
	public AvatarMaterialConfig DefaultAvatarConfig;
}
