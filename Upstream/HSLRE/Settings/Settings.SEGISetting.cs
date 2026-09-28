using Config;
using UnityEngine;

namespace HSLRE
{
	public static partial class Settings
	{
		public class SEGISetting : BaseSystem
		{
			public SEGISetting(string elementName) : base(elementName)
			{
			}

			public override void Init()
			{
				this.enabled = false;
				this.voxelResolution = "high";
				this.voxelAA = true;
				this.innerOcclusionLayers = 1;
				this.gaussianMipFilter = true;
				this.voxelSpaceSize = 12f;
				this.shadowSpaceSize = 12f;
				this.updateGI = true;
				this.infiniteBounces = true;
				this.hsStandardCustomShadersCompatibility = false;

				this.softSunlight = 0f;
				this.skyColor = Color.white;
				this.skyIntensity = 1f;
				this.sphericalSkylight = false;

				this.temporalBlendWeight = 1f;
				this.useBilateralFiltering = false;
				this.halfResolution = true;
				this.stochasticSampling = false;

				this.cones = 15;
				this.coneTraceSteps = 11;
				this.coneLength = 1f;
				this.coneWidth = 6f;
				this.coneTraceBias = 0.640f;
				this.occlusionStrength = 0.460f;
				this.nearOcclusionStrength = 0.240f;
				this.farOcclusionStrength = 1f;
				this.farthestOcclusionStrength = 0.620f;
				this.occlusionPower = 0.960f;
				this.nearLightGain = 0f;
				this.giGain = 1.360f;

				this.secondaryBounceGain = 1.640f;
				this.secondaryCones = 6;
				this.secondaryOcclusionStrength = 1;

				this.doReflections = true;
				this.reflectionSteps = 73;
				this.reflectionOcclusionPower = 1;
				this.skyReflectionIntensity = 1;
			}

			public bool enabled;
			public string voxelResolution;
			public bool voxelAA;
			public int innerOcclusionLayers;
			public bool gaussianMipFilter;
			public float voxelSpaceSize;
			public float shadowSpaceSize;
			public bool updateGI;
			public bool infiniteBounces;
			public bool hsStandardCustomShadersCompatibility;
			public float softSunlight;
			public Color skyColor;
			public float skyIntensity;
			public bool sphericalSkylight;
			public float temporalBlendWeight;
			public bool useBilateralFiltering;
			public bool halfResolution;
			public bool stochasticSampling;
			public int cones;
			public int coneTraceSteps;
			public float coneLength;
			public float coneWidth;
			public float coneTraceBias;
			public float occlusionStrength;
			public float nearOcclusionStrength;
			public float farOcclusionStrength;
			public float farthestOcclusionStrength;
			public float occlusionPower;
			public float nearLightGain;
			public float giGain;
			public float secondaryBounceGain;
			public int secondaryCones;
			public float secondaryOcclusionStrength;
			public bool doReflections;
			public int reflectionSteps;
			public float reflectionOcclusionPower;
			public float skyReflectionIntensity;
		}
	}
}
