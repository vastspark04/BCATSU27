using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunetonsImprovedAtmosphere{

public class Model
{
	private const int READ = 0;

	private const int WRITE = 1;

	private const double kLambdaR = 680.0;

	private const double kLambdaG = 550.0;

	private const double kLambdaB = 440.0;

	private const int kLambdaMin = 360;

	private const int kLambdaMax = 830;

	public float planetScale = 1f;

	public float heightScale = 1f;

	public IList<double> Wavelengths { get; set; }

	public IList<double> SolarIrradiance { get; set; }

	public double SunAngularRadius { get; set; }

	public double BottomRadius { get; set; }

	public double TopRadius { get; set; }

	public DensityProfileLayer RayleighDensity { get; set; }

	public IList<double> RayleighScattering { get; set; }

	public DensityProfileLayer MieDensity { get; set; }

	public IList<double> MieScattering { get; set; }

	public IList<double> MieExtinction { get; set; }

	public double MiePhaseFunctionG { get; set; }

	public IList<DensityProfileLayer> AbsorptionDensity { get; set; }

	public IList<double> AbsorptionExtinction { get; set; }

	public IList<double> GroundAlbedo { get; set; }

	public double MaxSunZenithAngle { get; set; }

	public double LengthUnitInMeters { get; set; }

	public int NumPrecomputedWavelengths
	{
		get
		{
			if (UseLuminance != LUMINANCE.PRECOMPUTED)
			{
				return 3;
			}
			return 15;
		}
	}

	public bool CombineScatteringTextures { get; set; }

	public LUMINANCE UseLuminance { get; set; }

	public bool HalfPrecision { get; set; }

	public RenderTexture TransmittanceTexture { get; private set; }

	public RenderTexture ScatteringTexture { get; private set; }

	public RenderTexture IrradianceTexture { get; private set; }

	public RenderTexture OptionalSingleMieScatteringTexture { get; private set; }

	public void BindToMaterial(Material mat)
	{
		mat.SetTexture("transmittance_texture", TransmittanceTexture);
		mat.SetTexture("scattering_texture", ScatteringTexture);
		mat.SetTexture("irradiance_texture", IrradianceTexture);
		if (CombineScatteringTextures)
		{
			mat.SetTexture("single_mie_scattering_texture", Texture2D.blackTexture);
		}
		else
		{
			mat.SetTexture("single_mie_scattering_texture", OptionalSingleMieScatteringTexture);
		}
		mat.SetInt("TRANSMITTANCE_TEXTURE_WIDTH", CONSTANTS.TRANSMITTANCE_WIDTH);
		mat.SetInt("TRANSMITTANCE_TEXTURE_HEIGHT", CONSTANTS.TRANSMITTANCE_HEIGHT);
		mat.SetInt("SCATTERING_TEXTURE_R_SIZE", CONSTANTS.SCATTERING_R);
		mat.SetInt("SCATTERING_TEXTURE_MU_SIZE", CONSTANTS.SCATTERING_MU);
		mat.SetInt("SCATTERING_TEXTURE_MU_S_SIZE", CONSTANTS.SCATTERING_MU_S);
		mat.SetInt("SCATTERING_TEXTURE_NU_SIZE", CONSTANTS.SCATTERING_NU);
		mat.SetInt("SCATTERING_TEXTURE_WIDTH", CONSTANTS.SCATTERING_WIDTH);
		mat.SetInt("SCATTERING_TEXTURE_HEIGHT", CONSTANTS.SCATTERING_HEIGHT);
		mat.SetInt("SCATTERING_TEXTURE_DEPTH", CONSTANTS.SCATTERING_DEPTH);
		mat.SetInt("IRRADIANCE_TEXTURE_WIDTH", CONSTANTS.IRRADIANCE_WIDTH);
		mat.SetInt("IRRADIANCE_TEXTURE_HEIGHT", CONSTANTS.IRRADIANCE_HEIGHT);
		mat.SetFloat("sun_angular_radius", (float)SunAngularRadius);
		mat.SetFloat("bottom_radius", (float)(BottomRadius / LengthUnitInMeters));
		mat.SetFloat("top_radius", (float)(TopRadius / LengthUnitInMeters));
		mat.SetFloat("mie_phase_function_g", (float)MiePhaseFunctionG);
		mat.SetFloat("mu_s_min", (float)Math.Cos(MaxSunZenithAngle));
		SkySunRadianceToLuminance(out var skySpectralRadianceToLuminance, out var sunSpectralRadianceToLuminance);
		mat.SetVector("SKY_SPECTRAL_RADIANCE_TO_LUMINANCE", skySpectralRadianceToLuminance);
		mat.SetVector("SUN_SPECTRAL_RADIANCE_TO_LUMINANCE", sunSpectralRadianceToLuminance);
		double[] lambdas = new double[3] { 680.0, 550.0, 440.0 };
		Vector3 vector = ToVector(Wavelengths, SolarIrradiance, lambdas, 1.0);
		mat.SetVector("solar_irradiance", vector);
		Vector3 vector2 = ToVector(Wavelengths, RayleighScattering, lambdas, LengthUnitInMeters);
		mat.SetVector("rayleigh_scattering", vector2);
		Vector3 vector3 = ToVector(Wavelengths, MieScattering, lambdas, LengthUnitInMeters);
		mat.SetVector("mie_scattering", vector3);
	}

	public void BindGlobally()
	{
		Shader.SetGlobalTexture("transmittance_texture", TransmittanceTexture);
		Shader.SetGlobalTexture("scattering_texture", ScatteringTexture);
		Shader.SetGlobalTexture("irradiance_texture", IrradianceTexture);
		if (CombineScatteringTextures)
		{
			Shader.SetGlobalTexture("single_mie_scattering_texture", Texture2D.blackTexture);
		}
		else
		{
			Shader.SetGlobalTexture("single_mie_scattering_texture", OptionalSingleMieScatteringTexture);
		}
		Shader.SetGlobalInt("TRANSMITTANCE_TEXTURE_WIDTH", CONSTANTS.TRANSMITTANCE_WIDTH);
		Shader.SetGlobalInt("TRANSMITTANCE_TEXTURE_HEIGHT", CONSTANTS.TRANSMITTANCE_HEIGHT);
		Shader.SetGlobalInt("SCATTERING_TEXTURE_R_SIZE", CONSTANTS.SCATTERING_R);
		Shader.SetGlobalInt("SCATTERING_TEXTURE_MU_SIZE", CONSTANTS.SCATTERING_MU);
		Shader.SetGlobalInt("SCATTERING_TEXTURE_MU_S_SIZE", CONSTANTS.SCATTERING_MU_S);
		Shader.SetGlobalInt("SCATTERING_TEXTURE_NU_SIZE", CONSTANTS.SCATTERING_NU);
		Shader.SetGlobalInt("SCATTERING_TEXTURE_WIDTH", CONSTANTS.SCATTERING_WIDTH);
		Shader.SetGlobalInt("SCATTERING_TEXTURE_HEIGHT", CONSTANTS.SCATTERING_HEIGHT);
		Shader.SetGlobalInt("SCATTERING_TEXTURE_DEPTH", CONSTANTS.SCATTERING_DEPTH);
		Shader.SetGlobalInt("IRRADIANCE_TEXTURE_WIDTH", CONSTANTS.IRRADIANCE_WIDTH);
		Shader.SetGlobalInt("IRRADIANCE_TEXTURE_HEIGHT", CONSTANTS.IRRADIANCE_HEIGHT);
		BottomRadius = 6360000f * planetScale;
		TopRadius = BottomRadius + (double)(60000f * heightScale);
		Shader.SetGlobalFloat("sun_angular_radius", (float)SunAngularRadius);
		Shader.SetGlobalFloat("bottom_radius", (float)(BottomRadius / LengthUnitInMeters));
		Shader.SetGlobalFloat("_OC_EarthRadius", (float)(BottomRadius / LengthUnitInMeters));
		Shader.SetGlobalFloat("_OC_PlanetScale", planetScale);
		Shader.SetGlobalFloat("_OC_AtmHeightInv", 1f / (60000f * heightScale));
		Shader.SetGlobalFloat("top_radius", (float)(TopRadius / LengthUnitInMeters));
		Shader.SetGlobalFloat("mie_phase_function_g", (float)MiePhaseFunctionG);
		Shader.SetGlobalFloat("mu_s_min", (float)Math.Cos(MaxSunZenithAngle));
		SkySunRadianceToLuminance(out var skySpectralRadianceToLuminance, out var sunSpectralRadianceToLuminance);
		Shader.SetGlobalVector("SKY_SPECTRAL_RADIANCE_TO_LUMINANCE", skySpectralRadianceToLuminance);
		Shader.SetGlobalVector("SUN_SPECTRAL_RADIANCE_TO_LUMINANCE", sunSpectralRadianceToLuminance);
		double[] lambdas = new double[3] { 680.0, 550.0, 440.0 };
		Vector3 vector = ToVector(Wavelengths, SolarIrradiance, lambdas, 1.0);
		Shader.SetGlobalVector("solar_irradiance", vector);
		Vector3 vector2 = ToVector(Wavelengths, RayleighScattering, lambdas, LengthUnitInMeters);
		Shader.SetGlobalVector("rayleigh_scattering", vector2);
		Vector3 vector3 = ToVector(Wavelengths, MieScattering, lambdas, LengthUnitInMeters);
		Shader.SetGlobalVector("mie_scattering", vector3);
	}

	public void Release()
	{
		ReleaseTexture(TransmittanceTexture);
		ReleaseTexture(ScatteringTexture);
		ReleaseTexture(IrradianceTexture);
		ReleaseTexture(OptionalSingleMieScatteringTexture);
	}

	public void Init(ComputeShader compute, int num_scattering_orders)
	{
		TextureBuffer textureBuffer = new TextureBuffer(HalfPrecision);
		textureBuffer.Clear(compute);
		if (NumPrecomputedWavelengths <= 3)
		{
			Precompute(compute, textureBuffer, null, null, blend: false, num_scattering_orders);
		}
		else
		{
			int num = (NumPrecomputedWavelengths + 2) / 3;
			double num2 = 470.0 / (3.0 * (double)num);
			for (int i = 0; i < num; i++)
			{
				double[] array = new double[3]
				{
					360.0 + ((double)(3 * i) + 0.5) * num2,
					360.0 + ((double)(3 * i) + 1.5) * num2,
					360.0 + ((double)(3 * i) + 2.5) * num2
				};
				double[] luminance_from_radiance = new double[9]
				{
					Coeff(array[0], 0) * num2,
					Coeff(array[1], 0) * num2,
					Coeff(array[2], 0) * num2,
					Coeff(array[0], 1) * num2,
					Coeff(array[1], 1) * num2,
					Coeff(array[2], 1) * num2,
					Coeff(array[0], 2) * num2,
					Coeff(array[1], 2) * num2,
					Coeff(array[2], 2) * num2
				};
				bool blend = i > 0;
				Precompute(compute, textureBuffer, array, luminance_from_radiance, blend, num_scattering_orders);
			}
			int kernelIndex = compute.FindKernel("ComputeTransmittance");
			BindToCompute(compute, null, null);
			compute.SetTexture(kernelIndex, "transmittanceWrite", textureBuffer.TransmittanceArray[1]);
			compute.SetVector("blend", new Vector4(0f, 0f, 0f, 0f));
			int nUM_THREADS = CONSTANTS.NUM_THREADS;
			compute.Dispatch(kernelIndex, CONSTANTS.TRANSMITTANCE_WIDTH / nUM_THREADS, CONSTANTS.TRANSMITTANCE_HEIGHT / nUM_THREADS, 1);
			Swap(textureBuffer.TransmittanceArray);
		}
		TransmittanceTexture = textureBuffer.TransmittanceArray[0];
		textureBuffer.TransmittanceArray[0] = null;
		ScatteringTexture = textureBuffer.ScatteringArray[0];
		textureBuffer.ScatteringArray[0] = null;
		IrradianceTexture = textureBuffer.IrradianceArray[0];
		textureBuffer.IrradianceArray[0] = null;
		if (CombineScatteringTextures)
		{
			OptionalSingleMieScatteringTexture = null;
		}
		else
		{
			OptionalSingleMieScatteringTexture = textureBuffer.OptionalSingleMieScatteringArray[0];
			textureBuffer.OptionalSingleMieScatteringArray[0] = null;
		}
		textureBuffer.Release();
	}

	private double Coeff(double lambda, int component)
	{
		double num = CieColorMatchingFunctionTableValue(lambda, 1);
		double num2 = CieColorMatchingFunctionTableValue(lambda, 2);
		double num3 = CieColorMatchingFunctionTableValue(lambda, 3);
		return CONSTANTS.XYZ_TO_SRGB[component * 3] * num + CONSTANTS.XYZ_TO_SRGB[component * 3 + 1] * num2 + CONSTANTS.XYZ_TO_SRGB[component * 3 + 2] * num3;
	}

	private void BindToCompute(ComputeShader compute, double[] lambdas, double[] luminance_from_radiance)
	{
		if (lambdas == null)
		{
			lambdas = new double[3] { 680.0, 550.0, 440.0 };
		}
		if (luminance_from_radiance == null)
		{
			luminance_from_radiance = new double[9] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };
		}
		compute.SetInt("TRANSMITTANCE_TEXTURE_WIDTH", CONSTANTS.TRANSMITTANCE_WIDTH);
		compute.SetInt("TRANSMITTANCE_TEXTURE_HEIGHT", CONSTANTS.TRANSMITTANCE_HEIGHT);
		compute.SetInt("SCATTERING_TEXTURE_R_SIZE", CONSTANTS.SCATTERING_R);
		compute.SetInt("SCATTERING_TEXTURE_MU_SIZE", CONSTANTS.SCATTERING_MU);
		compute.SetInt("SCATTERING_TEXTURE_MU_S_SIZE", CONSTANTS.SCATTERING_MU_S);
		compute.SetInt("SCATTERING_TEXTURE_NU_SIZE", CONSTANTS.SCATTERING_NU);
		compute.SetInt("SCATTERING_TEXTURE_WIDTH", CONSTANTS.SCATTERING_WIDTH);
		compute.SetInt("SCATTERING_TEXTURE_HEIGHT", CONSTANTS.SCATTERING_HEIGHT);
		compute.SetInt("SCATTERING_TEXTURE_DEPTH", CONSTANTS.SCATTERING_DEPTH);
		compute.SetInt("IRRADIANCE_TEXTURE_WIDTH", CONSTANTS.IRRADIANCE_WIDTH);
		compute.SetInt("IRRADIANCE_TEXTURE_HEIGHT", CONSTANTS.IRRADIANCE_HEIGHT);
		SkySunRadianceToLuminance(out var skySpectralRadianceToLuminance, out var sunSpectralRadianceToLuminance);
		compute.SetVector("SKY_SPECTRAL_RADIANCE_TO_LUMINANCE", skySpectralRadianceToLuminance);
		compute.SetVector("SUN_SPECTRAL_RADIANCE_TO_LUMINANCE", sunSpectralRadianceToLuminance);
		Vector3 vector = ToVector(Wavelengths, SolarIrradiance, lambdas, 1.0);
		compute.SetVector("solar_irradiance", vector);
		Vector3 vector2 = ToVector(Wavelengths, RayleighScattering, lambdas, LengthUnitInMeters);
		BindDensityLayer(compute, RayleighDensity);
		compute.SetVector("rayleigh_scattering", vector2);
		Vector3 vector3 = ToVector(Wavelengths, MieScattering, lambdas, LengthUnitInMeters);
		Vector3 vector4 = ToVector(Wavelengths, MieExtinction, lambdas, LengthUnitInMeters);
		BindDensityLayer(compute, MieDensity);
		compute.SetVector("mie_scattering", vector3);
		compute.SetVector("mie_extinction", vector4);
		Vector3 vector5 = ToVector(Wavelengths, AbsorptionExtinction, lambdas, LengthUnitInMeters);
		BindDensityLayer(compute, AbsorptionDensity[0]);
		BindDensityLayer(compute, AbsorptionDensity[1]);
		compute.SetVector("absorption_extinction", vector5);
		Vector3 vector6 = ToVector(Wavelengths, GroundAlbedo, lambdas, 1.0);
		compute.SetVector("ground_albedo", vector6);
		compute.SetFloats("luminanceFromRadiance", ToMatrix(luminance_from_radiance));
		compute.SetFloat("sun_angular_radius", (float)SunAngularRadius);
		compute.SetFloat("bottom_radius", (float)(BottomRadius / LengthUnitInMeters));
		compute.SetFloat("top_radius", (float)(TopRadius / LengthUnitInMeters));
		compute.SetFloat("mie_phase_function_g", (float)MiePhaseFunctionG);
		compute.SetFloat("mu_s_min", (float)Math.Cos(MaxSunZenithAngle));
	}

	private void BindDensityLayer(ComputeShader compute, DensityProfileLayer layer)
	{
		compute.SetFloat(layer.Name + "_width", (float)(layer.Width / LengthUnitInMeters));
		compute.SetFloat(layer.Name + "_exp_term", (float)layer.ExpTerm);
		compute.SetFloat(layer.Name + "_exp_scale", (float)(layer.ExpScale * LengthUnitInMeters));
		compute.SetFloat(layer.Name + "_linear_term", (float)(layer.LinearTerm * LengthUnitInMeters));
		compute.SetFloat(layer.Name + "_constant_term", (float)layer.ConstantTerm);
	}

	private Vector3 ToVector(IList<double> wavelengths, IList<double> v, IList<double> lambdas, double scale)
	{
		double num = Interpolate(wavelengths, v, lambdas[0]) * scale;
		double num2 = Interpolate(wavelengths, v, lambdas[1]) * scale;
		double num3 = Interpolate(wavelengths, v, lambdas[2]) * scale;
		return new Vector3((float)num, (float)num2, (float)num3);
	}

	private static double CieColorMatchingFunctionTableValue(double wavelength, int column)
	{
		if (wavelength <= 360.0 || wavelength >= 830.0)
		{
			return 0.0;
		}
		double num = (wavelength - 360.0) / 5.0;
		int num2 = (int)Math.Floor(num);
		num -= (double)num2;
		return CONSTANTS.CIE_2_DEG_COLOR_MATCHING_FUNCTIONS[4 * num2 + column] * (1.0 - num) + CONSTANTS.CIE_2_DEG_COLOR_MATCHING_FUNCTIONS[4 * (num2 + 1) + column] * num;
	}

	private static double Interpolate(IList<double> wavelengths, IList<double> wavelength_function, double wavelength)
	{
		if (wavelength < wavelengths[0])
		{
			return wavelength_function[0];
		}
		for (int i = 0; i < wavelengths.Count - 1; i++)
		{
			if (wavelength < wavelengths[i + 1])
			{
				double num = (wavelength - wavelengths[i]) / (wavelengths[i + 1] - wavelengths[i]);
				return wavelength_function[i] * (1.0 - num) + wavelength_function[i + 1] * num;
			}
		}
		return wavelength_function[wavelength_function.Count - 1];
	}

	private void SkySunRadianceToLuminance(out Vector3 skySpectralRadianceToLuminance, out Vector3 sunSpectralRadianceToLuminance)
	{
		double k_r;
		double k_g;
		double k_b;
		if (NumPrecomputedWavelengths > 3)
		{
			k_r = (k_g = (k_b = CONSTANTS.MAX_LUMINOUS_EFFICACY));
		}
		else
		{
			ComputeSpectralRadianceToLuminanceFactors(Wavelengths, SolarIrradiance, -3.0, out k_r, out k_g, out k_b);
		}
		ComputeSpectralRadianceToLuminanceFactors(Wavelengths, SolarIrradiance, 0.0, out var k_r2, out var k_g2, out var k_b2);
		skySpectralRadianceToLuminance = new Vector3((float)k_r, (float)k_g, (float)k_b);
		sunSpectralRadianceToLuminance = new Vector3((float)k_r2, (float)k_g2, (float)k_b2);
	}

	private static void ComputeSpectralRadianceToLuminanceFactors(IList<double> wavelengths, IList<double> solar_irradiance, double lambda_power, out double k_r, out double k_g, out double k_b)
	{
		k_r = 0.0;
		k_g = 0.0;
		k_b = 0.0;
		double num = Interpolate(wavelengths, solar_irradiance, 680.0);
		double num2 = Interpolate(wavelengths, solar_irradiance, 550.0);
		double num3 = Interpolate(wavelengths, solar_irradiance, 440.0);
		int num4 = 1;
		for (int i = 360; i < 830; i += num4)
		{
			double num5 = CieColorMatchingFunctionTableValue(i, 1);
			double num6 = CieColorMatchingFunctionTableValue(i, 2);
			double num7 = CieColorMatchingFunctionTableValue(i, 3);
			double[] xYZ_TO_SRGB = CONSTANTS.XYZ_TO_SRGB;
			double num8 = xYZ_TO_SRGB[0] * num5 + xYZ_TO_SRGB[1] * num6 + xYZ_TO_SRGB[2] * num7;
			double num9 = xYZ_TO_SRGB[3] * num5 + xYZ_TO_SRGB[4] * num6 + xYZ_TO_SRGB[5] * num7;
			double num10 = xYZ_TO_SRGB[6] * num5 + xYZ_TO_SRGB[7] * num6 + xYZ_TO_SRGB[8] * num7;
			double num11 = Interpolate(wavelengths, solar_irradiance, i);
			k_r += num8 * num11 / num * Math.Pow((double)i / 680.0, lambda_power);
			k_g += num9 * num11 / num2 * Math.Pow((double)i / 550.0, lambda_power);
			k_b += num10 * num11 / num3 * Math.Pow((double)i / 440.0, lambda_power);
		}
		k_r *= CONSTANTS.MAX_LUMINOUS_EFFICACY * (double)num4;
		k_g *= CONSTANTS.MAX_LUMINOUS_EFFICACY * (double)num4;
		k_b *= CONSTANTS.MAX_LUMINOUS_EFFICACY * (double)num4;
	}

	public void ConvertSpectrumToLinearSrgb(out double r, out double g, out double b)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		for (int i = 360; i < 830; i++)
		{
			double num4 = Interpolate(Wavelengths, SolarIrradiance, i);
			num += CieColorMatchingFunctionTableValue(i, 1) * num4;
			num2 += CieColorMatchingFunctionTableValue(i, 2) * num4;
			num3 += CieColorMatchingFunctionTableValue(i, 3) * num4;
		}
		double[] xYZ_TO_SRGB = CONSTANTS.XYZ_TO_SRGB;
		r = CONSTANTS.MAX_LUMINOUS_EFFICACY * (xYZ_TO_SRGB[0] * num + xYZ_TO_SRGB[1] * num2 + xYZ_TO_SRGB[2] * num3) * 1.0;
		g = CONSTANTS.MAX_LUMINOUS_EFFICACY * (xYZ_TO_SRGB[3] * num + xYZ_TO_SRGB[4] * num2 + xYZ_TO_SRGB[5] * num3) * 1.0;
		b = CONSTANTS.MAX_LUMINOUS_EFFICACY * (xYZ_TO_SRGB[6] * num + xYZ_TO_SRGB[7] * num2 + xYZ_TO_SRGB[8] * num3) * 1.0;
	}

	private void Precompute(ComputeShader compute, TextureBuffer buffer, double[] lambdas, double[] luminance_from_radiance, bool blend, int num_scattering_orders)
	{
		int num = (blend ? 1 : 0);
		int nUM_THREADS = CONSTANTS.NUM_THREADS;
		BindToCompute(compute, lambdas, luminance_from_radiance);
		int kernelIndex = compute.FindKernel("ComputeTransmittance");
		int kernelIndex2 = compute.FindKernel("ComputeDirectIrradiance");
		int kernelIndex3 = compute.FindKernel("ComputeSingleScattering");
		int kernelIndex4 = compute.FindKernel("ComputeScatteringDensity");
		int kernelIndex5 = compute.FindKernel("ComputeIndirectIrradiance");
		int kernelIndex6 = compute.FindKernel("ComputeMultipleScattering");
		compute.SetTexture(kernelIndex, "transmittanceWrite", buffer.TransmittanceArray[1]);
		compute.SetVector("blend", new Vector4(0f, 0f, 0f, 0f));
		compute.Dispatch(kernelIndex, CONSTANTS.TRANSMITTANCE_WIDTH / nUM_THREADS, CONSTANTS.TRANSMITTANCE_HEIGHT / nUM_THREADS, 1);
		Swap(buffer.TransmittanceArray);
		compute.SetTexture(kernelIndex2, "deltaIrradianceWrite", buffer.DeltaIrradianceTexture);
		compute.SetTexture(kernelIndex2, "irradianceWrite", buffer.IrradianceArray[1]);
		compute.SetTexture(kernelIndex2, "irradianceRead", buffer.IrradianceArray[0]);
		compute.SetTexture(kernelIndex2, "transmittanceRead", buffer.TransmittanceArray[0]);
		compute.SetVector("blend", new Vector4(0f, num, 0f, 0f));
		compute.Dispatch(kernelIndex2, CONSTANTS.IRRADIANCE_WIDTH / nUM_THREADS, CONSTANTS.IRRADIANCE_HEIGHT / nUM_THREADS, 1);
		Swap(buffer.IrradianceArray);
		compute.SetTexture(kernelIndex3, "deltaRayleighScatteringWrite", buffer.DeltaRayleighScatteringTexture);
		compute.SetTexture(kernelIndex3, "deltaMieScatteringWrite", buffer.DeltaMieScatteringTexture);
		compute.SetTexture(kernelIndex3, "scatteringWrite", buffer.ScatteringArray[1]);
		compute.SetTexture(kernelIndex3, "scatteringRead", buffer.ScatteringArray[0]);
		compute.SetTexture(kernelIndex3, "singleMieScatteringWrite", buffer.OptionalSingleMieScatteringArray[1]);
		compute.SetTexture(kernelIndex3, "singleMieScatteringRead", buffer.OptionalSingleMieScatteringArray[0]);
		compute.SetTexture(kernelIndex3, "transmittanceRead", buffer.TransmittanceArray[0]);
		compute.SetVector("blend", new Vector4(0f, 0f, num, num));
		for (int i = 0; i < CONSTANTS.SCATTERING_DEPTH; i++)
		{
			compute.SetInt("layer", i);
			compute.Dispatch(kernelIndex3, CONSTANTS.SCATTERING_WIDTH / nUM_THREADS, CONSTANTS.SCATTERING_HEIGHT / nUM_THREADS, 1);
		}
		Swap(buffer.ScatteringArray);
		Swap(buffer.OptionalSingleMieScatteringArray);
		for (int j = 2; j <= num_scattering_orders; j++)
		{
			compute.SetTexture(kernelIndex4, "deltaScatteringDensityWrite", buffer.DeltaScatteringDensityTexture);
			compute.SetTexture(kernelIndex4, "transmittanceRead", buffer.TransmittanceArray[0]);
			compute.SetTexture(kernelIndex4, "singleRayleighScatteringRead", buffer.DeltaRayleighScatteringTexture);
			compute.SetTexture(kernelIndex4, "singleMieScatteringRead", buffer.DeltaMieScatteringTexture);
			compute.SetTexture(kernelIndex4, "multipleScatteringRead", buffer.DeltaMultipleScatteringTexture);
			compute.SetTexture(kernelIndex4, "irradianceRead", buffer.DeltaIrradianceTexture);
			compute.SetInt("scatteringOrder", j);
			compute.SetVector("blend", new Vector4(0f, 0f, 0f, 0f));
			for (int k = 0; k < CONSTANTS.SCATTERING_DEPTH; k++)
			{
				compute.SetInt("layer", k);
				compute.Dispatch(kernelIndex4, CONSTANTS.SCATTERING_WIDTH / nUM_THREADS, CONSTANTS.SCATTERING_HEIGHT / nUM_THREADS, 1);
			}
			compute.SetTexture(kernelIndex5, "deltaIrradianceWrite", buffer.DeltaIrradianceTexture);
			compute.SetTexture(kernelIndex5, "irradianceWrite", buffer.IrradianceArray[1]);
			compute.SetTexture(kernelIndex5, "irradianceRead", buffer.IrradianceArray[0]);
			compute.SetTexture(kernelIndex5, "singleRayleighScatteringRead", buffer.DeltaRayleighScatteringTexture);
			compute.SetTexture(kernelIndex5, "singleMieScatteringRead", buffer.DeltaMieScatteringTexture);
			compute.SetTexture(kernelIndex5, "multipleScatteringRead", buffer.DeltaMultipleScatteringTexture);
			compute.SetInt("scatteringOrder", j - 1);
			compute.SetVector("blend", new Vector4(0f, 1f, 0f, 0f));
			compute.Dispatch(kernelIndex5, CONSTANTS.IRRADIANCE_WIDTH / nUM_THREADS, CONSTANTS.IRRADIANCE_HEIGHT / nUM_THREADS, 1);
			Swap(buffer.IrradianceArray);
			compute.SetTexture(kernelIndex6, "deltaMultipleScatteringWrite", buffer.DeltaMultipleScatteringTexture);
			compute.SetTexture(kernelIndex6, "scatteringWrite", buffer.ScatteringArray[1]);
			compute.SetTexture(kernelIndex6, "scatteringRead", buffer.ScatteringArray[0]);
			compute.SetTexture(kernelIndex6, "transmittanceRead", buffer.TransmittanceArray[0]);
			compute.SetTexture(kernelIndex6, "deltaScatteringDensityRead", buffer.DeltaScatteringDensityTexture);
			compute.SetVector("blend", new Vector4(0f, 1f, 0f, 0f));
			for (int l = 0; l < CONSTANTS.SCATTERING_DEPTH; l++)
			{
				compute.SetInt("layer", l);
				compute.Dispatch(kernelIndex6, CONSTANTS.SCATTERING_WIDTH / nUM_THREADS, CONSTANTS.SCATTERING_HEIGHT / nUM_THREADS, 1);
			}
			Swap(buffer.ScatteringArray);
		}
	}

	private void Swap(RenderTexture[] arr)
	{
		RenderTexture renderTexture = arr[0];
		arr[0] = arr[1];
		arr[1] = renderTexture;
	}

	private void ReleaseTexture(RenderTexture tex)
	{
		if (!(tex == null))
		{
			UnityEngine.Object.DestroyImmediate(tex);
		}
	}

	private float[] ToMatrix(double[] arr)
	{
		return new float[16]
		{
			(float)arr[0],
			(float)arr[3],
			(float)arr[6],
			0f,
			(float)arr[1],
			(float)arr[4],
			(float)arr[7],
			0f,
			(float)arr[2],
			(float)arr[5],
			(float)arr[8],
			0f,
			0f,
			0f,
			0f,
			1f
		};
	}
}
}