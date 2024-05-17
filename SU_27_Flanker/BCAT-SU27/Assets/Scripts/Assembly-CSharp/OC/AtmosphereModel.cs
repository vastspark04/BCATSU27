using System;
using System.Collections.Generic;
using BrunetonsImprovedAtmosphere;
using UnityEngine;

namespace OC{

public class AtmosphereModel
{
	public const float EARTH_RADIUS = 6360000f;

	public const float EARTH_ATMOSPHERE_HEIGHT = 60000f;

	public float planetScale = 1f;

	public float heightScale = 1f;

	private static readonly float kSunAngularRadius = 0.004675f;

	private static readonly float kBottomRadius = 6360000f;

	private static readonly float kLengthUnitInMeters = 1f;

	public Light Sun;

	public bool UseConstantSolarSpectrum = true;

	public bool UseOzone = true;

	public bool UseCombinedTextures = true;

	public bool UseHalfPrecision = true;

	public bool DoWhiteBalance;

	public LUMINANCE UseLuminance = LUMINANCE.PRECOMPUTED;

	public float Exposure = 10f;

	public ComputeShader m_compute;

	private Model m_model;

	private bool m_Initialized;

	public bool initialized => m_Initialized;

	public void Initialize(OverCloud.Atmosphere.Precomputation settings)
	{
		int num = 360;
		int num2 = 830;
		double[] array = new double[48]
		{
			1.11776, 1.14259, 1.01249, 1.14716, 1.72765, 1.73054, 1.6887, 1.61253, 1.91198, 2.03474,
			2.02042, 2.02212, 1.93377, 1.95809, 1.91686, 1.8298, 1.8685, 1.8931, 1.85149, 1.8504,
			1.8341, 1.8345, 1.8147, 1.78158, 1.7533, 1.6965, 1.68194, 1.64654, 1.6048, 1.52143,
			1.55622, 1.5113, 1.474, 1.4482, 1.41018, 1.36775, 1.34188, 1.31429, 1.28303, 1.26758,
			1.2367, 1.2082, 1.18737, 1.14683, 1.12362, 1.1058, 1.07124, 1.04992
		};
		double[] array2 = new double[48]
		{
			1.18E-27, 2.182E-28, 2.818E-28, 6.636E-28, 1.527E-27, 2.763E-27, 5.52E-27, 8.451E-27, 1.582E-26, 2.316E-26,
			3.669E-26, 4.924E-26, 7.752E-26, 9.016E-26, 1.48E-25, 1.602E-25, 2.139E-25, 2.755E-25, 3.091E-25, 3.5E-25,
			4.266E-25, 4.672E-25, 4.398E-25, 4.701E-25, 5.019E-25, 4.305E-25, 3.74E-25, 3.215E-25, 2.662E-25, 2.238E-25,
			1.852E-25, 1.473E-25, 1.209E-25, 9.423E-26, 7.455E-26, 6.566E-26, 5.105E-26, 4.15E-26, 4.228E-26, 3.237E-26,
			2.451E-26, 2.801E-26, 2.534E-26, 1.624E-26, 1.465E-26, 2.078E-26, 1.383E-26, 7.105E-27
		};
		double num3 = 2.687E+20;
		double num4 = 300.0 * num3 / 15000.0;
		double item = 1.5;
		double topRadius = 6420000.0;
		double num5 = 1.24062E-06;
		double num6 = 8000.0;
		double num7 = 1200.0;
		double num8 = 0.0;
		double num9 = 0.005328;
		double num10 = 0.9;
		double item2 = 0.1;
		double maxSunZenithAngle = (UseHalfPrecision ? 102.0 : 120.0) / 180.0 * 3.1415927410125732;
		DensityProfileLayer rayleighDensity = new DensityProfileLayer("rayleigh", 0.0, 1.0, -1.0 / num6, 0.0, 0.0);
		DensityProfileLayer mieDensity = new DensityProfileLayer("mie", 0.0, 1.0, -1.0 / num7, 0.0, 0.0);
		List<DensityProfileLayer> list = new List<DensityProfileLayer>();
		list.Add(new DensityProfileLayer("absorption0", 25000.0, 0.0, 0.0, 6.666666666666667E-05, -2.0 / 3.0));
		list.Add(new DensityProfileLayer("absorption1", 0.0, 0.0, 0.0, -6.666666666666667E-05, 2.6666666666666665));
		List<double> list2 = new List<double>();
		List<double> list3 = new List<double>();
		List<double> list4 = new List<double>();
		List<double> list5 = new List<double>();
		List<double> list6 = new List<double>();
		List<double> list7 = new List<double>();
		List<double> list8 = new List<double>();
		for (int i = num; i <= num2; i += 10)
		{
			double x = (double)i * 0.001;
			double num11 = num9 / num7 * Math.Pow(x, 0.0 - num8) * (double)settings.mie * 9.999999747378752E-06;
			list2.Add(i);
			if (UseConstantSolarSpectrum)
			{
				list3.Add(item);
			}
			else
			{
				list3.Add(array[(i - num) / 10]);
			}
			list4.Add(num5 * Math.Pow(x, -4.0) * (double)settings.rayleigh);
			list5.Add(num11 * num10);
			list6.Add(num11);
			list7.Add(UseOzone ? (num4 * array2[(i - num) / 10]) : 0.0);
			list8.Add(item2);
		}
		m_model = new Model();
		m_model.planetScale = planetScale;
		m_model.heightScale = heightScale;
		m_model.HalfPrecision = UseHalfPrecision;
		m_model.CombineScatteringTextures = UseCombinedTextures;
		m_model.UseLuminance = UseLuminance;
		m_model.Wavelengths = list2;
		m_model.SolarIrradiance = list3;
		m_model.SunAngularRadius = kSunAngularRadius;
		m_model.BottomRadius = kBottomRadius;
		m_model.TopRadius = topRadius;
		m_model.RayleighDensity = rayleighDensity;
		m_model.RayleighScattering = list4;
		m_model.MieDensity = mieDensity;
		m_model.MieScattering = list5;
		m_model.MieExtinction = list6;
		m_model.MiePhaseFunctionG = settings.phase;
		m_model.AbsorptionDensity = list;
		m_model.AbsorptionExtinction = list7;
		m_model.GroundAlbedo = list8;
		m_model.MaxSunZenithAngle = maxSunZenithAngle;
		m_model.LengthUnitInMeters = kLengthUnitInMeters;
		int num_scattering_orders = 4;
		m_model.Init(m_compute, num_scattering_orders);
		m_model.BindGlobally();
		UpdateShaderVariables();
	}

	public void Bind()
	{
		m_model.BindGlobally();
		UpdateShaderVariables();
	}

	public void Release()
	{
		if (m_model != null)
		{
			m_model.Release();
		}
	}

	public void UpdateShaderVariables()
	{
		Shader.SetGlobalVector("_ScattEarthCenter", new Vector3(0f, (0f - 6360000f * planetScale) / kLengthUnitInMeters, 0f));
	}
}
}