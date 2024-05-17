using UnityEngine;
using UnityEngine.Rendering;

namespace BrunetonsImprovedAtmosphere{

public class TextureBuffer
{
	public RenderTexture DeltaIrradianceTexture { get; private set; }

	public RenderTexture DeltaRayleighScatteringTexture { get; private set; }

	public RenderTexture DeltaMieScatteringTexture { get; private set; }

	public RenderTexture DeltaScatteringDensityTexture { get; private set; }

	public RenderTexture DeltaMultipleScatteringTexture { get; private set; }

	public RenderTexture[] TransmittanceArray { get; private set; }

	public RenderTexture[] IrradianceArray { get; private set; }

	public RenderTexture[] ScatteringArray { get; private set; }

	public RenderTexture[] OptionalSingleMieScatteringArray { get; private set; }

	public TextureBuffer(bool halfPrecision)
	{
		TransmittanceArray = NewTexture2DArray(CONSTANTS.TRANSMITTANCE_WIDTH, CONSTANTS.TRANSMITTANCE_HEIGHT, halfPrecision: false);
		IrradianceArray = NewTexture2DArray(CONSTANTS.IRRADIANCE_WIDTH, CONSTANTS.IRRADIANCE_HEIGHT, halfPrecision: false);
		ScatteringArray = NewTexture3DArray(CONSTANTS.SCATTERING_WIDTH, CONSTANTS.SCATTERING_HEIGHT, CONSTANTS.SCATTERING_DEPTH, halfPrecision);
		OptionalSingleMieScatteringArray = NewTexture3DArray(CONSTANTS.SCATTERING_WIDTH, CONSTANTS.SCATTERING_HEIGHT, CONSTANTS.SCATTERING_DEPTH, halfPrecision);
		DeltaIrradianceTexture = NewRenderTexture2D(CONSTANTS.IRRADIANCE_WIDTH, CONSTANTS.IRRADIANCE_HEIGHT, halfPrecision: false);
		DeltaRayleighScatteringTexture = NewRenderTexture3D(CONSTANTS.SCATTERING_WIDTH, CONSTANTS.SCATTERING_HEIGHT, CONSTANTS.SCATTERING_DEPTH, halfPrecision);
		DeltaMieScatteringTexture = NewRenderTexture3D(CONSTANTS.SCATTERING_WIDTH, CONSTANTS.SCATTERING_HEIGHT, CONSTANTS.SCATTERING_DEPTH, halfPrecision);
		DeltaScatteringDensityTexture = NewRenderTexture3D(CONSTANTS.SCATTERING_WIDTH, CONSTANTS.SCATTERING_HEIGHT, CONSTANTS.SCATTERING_DEPTH, halfPrecision);
		DeltaMultipleScatteringTexture = DeltaRayleighScatteringTexture;
	}

	public void Release()
	{
		ReleaseTexture(DeltaIrradianceTexture);
		ReleaseTexture(DeltaRayleighScatteringTexture);
		ReleaseTexture(DeltaMieScatteringTexture);
		ReleaseTexture(DeltaScatteringDensityTexture);
		ReleaseArray(TransmittanceArray);
		ReleaseArray(IrradianceArray);
		ReleaseArray(ScatteringArray);
		ReleaseArray(OptionalSingleMieScatteringArray);
	}

	public void Clear(ComputeShader compute)
	{
		ClearTexture(compute, DeltaIrradianceTexture);
		ClearTexture(compute, DeltaRayleighScatteringTexture);
		ClearTexture(compute, DeltaMieScatteringTexture);
		ClearTexture(compute, DeltaScatteringDensityTexture);
		ClearArray(compute, TransmittanceArray);
		ClearArray(compute, IrradianceArray);
		ClearArray(compute, ScatteringArray);
		ClearArray(compute, OptionalSingleMieScatteringArray);
	}

	private void ReleaseTexture(RenderTexture tex)
	{
		if (!(tex == null))
		{
			Object.DestroyImmediate(tex);
		}
	}

	private void ReleaseArray(RenderTexture[] arr)
	{
		if (arr == null)
		{
			return;
		}
		for (int i = 0; i < arr.Length; i++)
		{
			if (arr[i] != null)
			{
				Object.DestroyImmediate(arr[i]);
				arr[i] = null;
			}
		}
	}

	private RenderTexture[] NewTexture2DArray(int width, int height, bool halfPrecision)
	{
		return new RenderTexture[2]
		{
			NewRenderTexture2D(width, height, halfPrecision),
			NewRenderTexture2D(width, height, halfPrecision)
		};
	}

	private RenderTexture[] NewTexture3DArray(int width, int height, int depth, bool halfPrecision)
	{
		return new RenderTexture[2]
		{
			NewRenderTexture3D(width, height, depth, halfPrecision),
			NewRenderTexture3D(width, height, depth, halfPrecision)
		};
	}

	private void ClearArray(ComputeShader compute, RenderTexture[] arr)
	{
		if (arr != null)
		{
			foreach (RenderTexture tex in arr)
			{
				ClearTexture(compute, tex);
			}
		}
	}

	private void ClearTexture(ComputeShader compute, RenderTexture tex)
	{
		if (!(tex == null))
		{
			int nUM_THREADS = CONSTANTS.NUM_THREADS;
			if (tex.dimension == TextureDimension.Tex3D)
			{
				int width = tex.width;
				int height = tex.height;
				int volumeDepth = tex.volumeDepth;
				int kernelIndex = compute.FindKernel("ClearTex3D");
				compute.SetTexture(kernelIndex, "targetWrite3D", tex);
				compute.Dispatch(kernelIndex, width / nUM_THREADS, height / nUM_THREADS, volumeDepth / nUM_THREADS);
			}
			else
			{
				int width2 = tex.width;
				int height2 = tex.height;
				int kernelIndex2 = compute.FindKernel("ClearTex2D");
				compute.SetTexture(kernelIndex2, "targetWrite2D", tex);
				compute.Dispatch(kernelIndex2, width2 / nUM_THREADS, height2 / nUM_THREADS, 1);
			}
		}
	}

	public static RenderTexture NewRenderTexture2D(int width, int height, bool halfPrecision)
	{
		RenderTextureFormat format = RenderTextureFormat.ARGBFloat;
		if (halfPrecision && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
		{
			format = RenderTextureFormat.ARGBHalf;
		}
		RenderTexture renderTexture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
		renderTexture.filterMode = FilterMode.Bilinear;
		renderTexture.wrapMode = TextureWrapMode.Clamp;
		renderTexture.useMipMap = false;
		renderTexture.enableRandomWrite = true;
		renderTexture.Create();
		return renderTexture;
	}

	public static RenderTexture NewRenderTexture3D(int width, int height, int depth, bool halfPrecision)
	{
		RenderTextureFormat format = RenderTextureFormat.ARGBFloat;
		if (halfPrecision && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
		{
			format = RenderTextureFormat.ARGBHalf;
		}
		RenderTexture renderTexture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
		renderTexture.volumeDepth = depth;
		renderTexture.dimension = TextureDimension.Tex3D;
		renderTexture.filterMode = FilterMode.Bilinear;
		renderTexture.wrapMode = TextureWrapMode.Clamp;
		renderTexture.useMipMap = false;
		renderTexture.enableRandomWrite = true;
		renderTexture.Create();
		return renderTexture;
	}

	public static Texture2D NewTexture2D(int width, int height, bool halfPrecision)
	{
		TextureFormat textureFormat = TextureFormat.RGBAFloat;
		if (halfPrecision && SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
		{
			textureFormat = TextureFormat.RGBAHalf;
		}
		return new Texture2D(width, height, textureFormat, mipChain: false, linear: true)
		{
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};
	}

	public static Texture3D NewTexture3D(int width, int height, int depth, bool halfPrecision)
	{
		TextureFormat textureFormat = TextureFormat.RGBAFloat;
		if (halfPrecision && SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
		{
			textureFormat = TextureFormat.RGBAHalf;
		}
		return new Texture3D(width, height, depth, textureFormat, mipChain: false)
		{
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};
	}
}
}