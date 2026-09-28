using System;
using System.Collections.Generic;
using Utility.Xml;
using UnityEngine;
using HSLRE.CustomEffects;
using System.IO;
using ToolBox;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CinematicEffects;
using UnityStandardAssets.ImageEffects;
using Bloom = HSLRE.CustomEffects.Bloom;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using TonemappingColorGrading = HSLRE.CustomEffects.TonemappingColorGrading;
using HSLRE.SaveLoad;
using LensAberrations = UnityStandardAssets.CinematicEffects.LensAberrations;
//using LensAberrations = HSLRE.CustomEffects.LensAberrations;
using SMAA = UnityStandardAssets.CinematicEffects.SMAA;
using static UnityStandardAssets.CinematicEffects.ScreenSpaceReflection2;
using System.Linq;

namespace HSLRE
{
	public static partial class Settings
	{

		public static BasicSetting basicSettings;
		public static Preset studioPresets;
		public static Preset gamePresets;

		public static SMAASetting smaaSettings;
		public static BloomSetting bloomSettings;
		public static ToneMapSetting tonemappingSettings;
		public static LensAberrationSetting lensAberrationSettings;

		public static BloomAndFlaresSetting bloomAndFlaresSettings;
		public static CurveSetting curveSettings;
		public static AntialiasingSetting antialiasingSettings;
		public static VignetteSetting vignetteSettings;

		public static SSAOSetting ssaoSettings;
		public static SSRSetting ssrSettings;
		public static ShadowSetting shadowSettings;
		public static DepthOfFieldSetting depthOfFieldSettings;
		public static SunShaftsSetting sunShaftsSettings;
		public static NoiseAndGrainSetting noiseAndGrainSettings;
		public static SEGISetting segiSettings;
		public static AmplifyBloomSetting amplifyBloomSettings;

		private static Control _xmlGraphicsSetting;
		private static Control _xmlImageEffect;
		private static Control _xmlStyle;
		private static Control _xmlShadow;
		private static Control _xmlCommon;

		public static void Load()
		{
			basicSettings = new BasicSetting("BasicConfig");
			gamePresets = new Preset("GamePresets");
			studioPresets = new Preset("StudioPresets");
			_xmlGraphicsSetting = new Control("GraphicSetting", "Config.xml", "HoneySelect", new List<Data>
			{
				basicSettings,
				gamePresets,
				studioPresets
			});
			_xmlGraphicsSetting.Read();

			if ((CameraClearFlags)basicSettings.CharaMakerBackgroundType != CameraClearFlags.Skybox && (CameraClearFlags)basicSettings.CharaMakerBackgroundType != CameraClearFlags.Color)
				basicSettings.CharaMakerBackgroundType = (int)CameraClearFlags.Skybox;
			QualitySettings.antiAliasing = basicSettings.msaa;
			if (HSLRE.self.binary == Binary.Game &&
				(SceneManager.GetActiveScene().buildIndex == 21 || SceneManager.GetActiveScene().buildIndex == 22) &&
				!basicSettings.CharaMakerReform &&
				QualitySettings.antiAliasing == 0)
				QualitySettings.antiAliasing = 2;
			string imageEffectPreset;
			string shadowPreset;
			string stylePreset;
			string commonPreset;
			switch (Application.productName)
			{
				case "HoneySelect":
				case "Honey Select Unlimited":
					imageEffectPreset = gamePresets.ImageEffectPreset;
					shadowPreset = gamePresets.ShadowPreset;
					stylePreset = gamePresets.StylePreset;
					commonPreset = gamePresets.CommonPreset;
					break;
				case "StudioNEO":
					imageEffectPreset = studioPresets.ImageEffectPreset;
					shadowPreset = studioPresets.ShadowPreset;
					stylePreset = studioPresets.StylePreset;
					commonPreset = studioPresets.CommonPreset;
					break;
				default:
					Console.WriteLine("Product Name is " + Application.productName);
					return;
			}



			//UnityEngine.Debug.Log("config:\r\n" + string.Join("\r\n", HSLRE.self.effectsDictionary.Keys.Select(x => x.ToString()).ToArray())); // todo: delete

			//LRE only settings
			{
				lensAberrationSettings = new LensAberrationSetting("LensAberration");
				bloomSettings = new BloomSetting("Bloom");
				tonemappingSettings = new ToneMapSetting("ToneMapping");
				smaaSettings = new SMAASetting("SMAA");
				_xmlImageEffect = new Control("GraphicSetting/ImageEffect", imageEffectPreset + ".xml", "Config", new List<Data>
				{
					lensAberrationSettings,
					bloomSettings,
					tonemappingSettings,
					smaaSettings
				});
				_xmlImageEffect.Read();

				if (HSLRE.self.tonemapping != null)
				{
					TonemappingColorGrading.TonemappingSettings localTonemappingSettings = new TonemappingColorGrading.TonemappingSettings
					{
						enabled = true,
						tonemapper = TonemappingColorGrading.Tonemapper.ACES,
						exposure = tonemappingSettings.exposure,
						curve = TonemappingColorGrading.CurvesSettings.defaultCurve,
						neutralBlackIn = 0.02f,
						neutralWhiteIn = 10f,
						neutralBlackOut = 0f,
						neutralWhiteOut = 10f,
						neutralWhiteLevel = 5.3f,
						neutralWhiteClip = 10f
					};
					switch (tonemappingSettings.tonemapper)
					{
						case 0:
							localTonemappingSettings.enabled = false;
							break;
						case 1:
							localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.ACES;
							break;
						case 2:
							localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.Hable;
							break;
						case 3:
							localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.HejlDawson;
							break;
						case 4:
							localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.Photographic;
							break;
						case 5:
							localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.Reinhard;
							break;
					}
					HSLRE.self.tonemapping.tonemapping = localTonemappingSettings;
					TonemappingColorGrading.ColorGradingSettings localColorGradingSettings = TonemappingColorGrading.ColorGradingSettings.defaultSettings;
					localColorGradingSettings.useDithering = tonemappingSettings.useDithering;
					localColorGradingSettings.basics = new TonemappingColorGrading.BasicsSettings
					{
						temperatureShift = tonemappingSettings.temperatureShift,
						tint = tonemappingSettings.tint,
						contrast = tonemappingSettings.contrast,
						hue = tonemappingSettings.hue,
						saturation = tonemappingSettings.saturation,
						value = tonemappingSettings.value,
						vibrance = tonemappingSettings.vibrance,
						gain = tonemappingSettings.gain,
						gamma = tonemappingSettings.gamma,
					};
					HSLRE.self.tonemapping.colorGrading = localColorGradingSettings;

					Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false, true);
					string path = Path.Combine(Application.dataPath, "../UserData/LUT/" + tonemappingSettings.LutName + ".png");
					if (File.Exists(path))
					{
						byte[] data = File.ReadAllBytes(path);
						texture2D.LoadImage(data);
						texture2D.filterMode = FilterMode.Trilinear;
						texture2D.anisoLevel = 0;
						texture2D.wrapMode = TextureWrapMode.Repeat;
					}
					else
					{
						tonemappingSettings.Lut = false;
					}
					HSLRE.self.tonemapping.lut = new TonemappingColorGrading.LUTSettings
					{
						enabled = tonemappingSettings.Lut,
						texture = texture2D,
						contribution = tonemappingSettings.LutContribution
					};
				}

				if (HSLRE.self.cinematicBloom != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.cinematicBloom].enabled = bloomSettings.enabled;

					HSLRE.self.cinematicBloom.settings = new Bloom.Settings()
					{
						antiFlicker = bloomSettings.antiFlicker,
						highQuality = true,
						intensity = bloomSettings.intensity,
						radius = bloomSettings.raduis,
						softKnee = bloomSettings.softKnee,
						threshold = bloomSettings.threshold
					};

				}

				if (HSLRE.self.lensAberrations != null)
				{
					HSLRE.self.lensAberrations.distortion = new LensAberrations.DistortionSettings
					{
						enabled = lensAberrationSettings.DistortionEnabled,
						amount = lensAberrationSettings.DistortionAmount,
						centerX = 0f,
						centerY = 0f,
						amountX = lensAberrationSettings.DistortionAmountX,
						amountY = lensAberrationSettings.DistortionAmountY,
						scale = lensAberrationSettings.DistortionScale
					};
					HSLRE.self.lensAberrations.vignette = new LensAberrations.VignetteSettings
					{
						enabled = lensAberrationSettings.VignetteEnabled,
						color = lensAberrationSettings.VignetteColor,
						center = new Vector2(0.5f, 0.5f),
						intensity = lensAberrationSettings.VignetteIntensity,
						smoothness = lensAberrationSettings.VignetteSmoothness,
						roundness = lensAberrationSettings.VignetteRoundness,
						blur = lensAberrationSettings.VignetteBlur,
						desaturate = lensAberrationSettings.VignetteDesaturate
					};
					HSLRE.self.lensAberrations.chromaticAberration = new LensAberrations.ChromaticAberrationSettings
					{
						enabled = lensAberrationSettings.ChromaticAberrationEnabled,
						color = lensAberrationSettings.ChromaticColor,
						amount = lensAberrationSettings.ChromaticAberrationAmount
					};
				}

				if (HSLRE.self.smaa != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.smaa].enabled = smaaSettings.enabled;
					HSLRE.self.smaa.settings = new SMAA.GlobalSettings()
					{
						debugPass = SMAA.DebugPass.Off,
						edgeDetectionMethod = (SMAA.EdgeDetectionMethod)smaaSettings.edgeDetectionMethod,
						quality = (SMAA.QualityPreset)smaaSettings.quality
					};
					HSLRE.self.smaa.predication = new SMAA.PredicationSettings()
					{
						enabled = smaaSettings.predication,
						scale = smaaSettings.predicationScale,
						strength = smaaSettings.predicationStrength,
						threshold = smaaSettings.threshold
					};
					HSLRE.self.smaa.temporal = new SMAA.TemporalSettings()
					{
						enabled = smaaSettings.temporal,
						fuzzSize = smaaSettings.temporalFuzzSize
					};
					HSLRE.self.smaa.quality = new SMAA.QualitySettings()
					{
						cornerDetection = smaaSettings.cornerDetection,
						cornerRounding = smaaSettings.cornerRounding,
						depthThreshold = smaaSettings.depthThreshold,
						diagonalDetection = smaaSettings.diagonalDetection,
						localContrastAdaptationFactor = smaaSettings.localContrastAdaptationFactor,
						maxDiagonalSearchSteps = smaaSettings.maxDiagonalSearchSteps,
						maxSearchSteps = smaaSettings.maxSearchSteps,
						threshold = smaaSettings.threshold
					};
				}

			}

			//4K only settings
			{
				bloomAndFlaresSettings = new BloomAndFlaresSetting("Bloom");
				curveSettings = new CurveSetting("ColorCorrectionCurve");
				antialiasingSettings = new AntialiasingSetting("Antialiasing");
				vignetteSettings = new VignetteSetting("VignetteAndChromaticAberration");
				_xmlStyle = new Control("GraphicSetting/Style", stylePreset + ".xml", "Config", new List<Data>
				{
					bloomAndFlaresSettings,
					curveSettings,
					antialiasingSettings,
					vignetteSettings
				});
				_xmlStyle.Read();

				if (HSLRE.self.bloomAndFlares != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.bloomAndFlares].enabled = bloomAndFlaresSettings.BloomEnabled;
					HSLRE.self.bloomAndFlares.bloomIntensity = bloomAndFlaresSettings.Bloomintensity;
					if (Studio.Studio.Instance != null)
						Studio.Studio.Instance.sceneInfo.bloomIntensity = HSLRE.self.bloomAndFlares.bloomIntensity;
					HSLRE.self.bloomAndFlares.sepBlurSpread = bloomAndFlaresSettings.BloomBlurSpread;
					if (Studio.Studio.Instance != null)
						Studio.Studio.Instance.sceneInfo.bloomBlur = HSLRE.self.bloomAndFlares.sepBlurSpread;
					HSLRE.self.bloomAndFlares.bloomThreshold = bloomAndFlaresSettings.BloomThreshold;

					switch (bloomAndFlaresSettings.BloomScreenBlendMode)
					{
						case "Screen":
							HSLRE.self.bloomAndFlares.screenBlendMode = BloomScreenBlendMode.Screen;
							break;
						case "Add":
							HSLRE.self.bloomAndFlares.screenBlendMode = BloomScreenBlendMode.Add;
							break;
						default:
							HSLRE.self.bloomAndFlares.screenBlendMode = BloomScreenBlendMode.Screen;
							bloomAndFlaresSettings.BloomScreenBlendMode = "Screen";
							break;
					}
					switch (bloomAndFlaresSettings.BloomHdr)
					{
						case "Auto":
							HSLRE.self.bloomAndFlares.hdr = HDRBloomMode.Auto;
							break;
						case "On":
							HSLRE.self.bloomAndFlares.hdr = HDRBloomMode.On;
							break;
						case "Off":
							HSLRE.self.bloomAndFlares.hdr = HDRBloomMode.Off;
							break;
						default:
							HSLRE.self.bloomAndFlares.hdr = HDRBloomMode.Auto;
							bloomAndFlaresSettings.BloomHdr = "Auto";
							break;
					}

					HSLRE.self.bloomAndFlares.bloomBlurIterations = bloomAndFlaresSettings.BloomBlurIterations;
					HSLRE.self.bloomAndFlares.useSrcAlphaAsMask = bloomAndFlaresSettings.BloomUseSrcAlphaAsMask;

					HSLRE.self.bloomAndFlares.lensflares = bloomAndFlaresSettings.BloomLensflares;
					switch (bloomAndFlaresSettings.BloomLensflareMode)
					{
						case "Ghosting":
							HSLRE.self.bloomAndFlares.lensflareMode = LensflareStyle34.Ghosting;
							break;
						case "Anamorphic":
							HSLRE.self.bloomAndFlares.lensflareMode = LensflareStyle34.Anamorphic;
							break;
						case "Combined":
							HSLRE.self.bloomAndFlares.lensflareMode = LensflareStyle34.Combined;
							break;
						default:
							HSLRE.self.bloomAndFlares.lensflareMode = LensflareStyle34.Anamorphic;
							bloomAndFlaresSettings.BloomLensflareMode = "Anamorphic";
							break;
					}
					HSLRE.self.bloomAndFlares.lensflareIntensity = bloomAndFlaresSettings.BloomLensflareIntensity;
					HSLRE.self.bloomAndFlares.lensflareThreshold = bloomAndFlaresSettings.BloomLensflareThreshold;
					HSLRE.self.bloomAndFlares.hollyStretchWidth = bloomAndFlaresSettings.BloomLensFlareStretchWidth;
					HSLRE.self.bloomAndFlares.hollywoodFlareBlurIterations = bloomAndFlaresSettings.BloomLensFlareBlurIterations;
					HSLRE.self.bloomAndFlares.flareColorA = bloomAndFlaresSettings.BloomFlareColorA;
					HSLRE.self.bloomAndFlares.flareColorB = bloomAndFlaresSettings.BloomFlareColorB;
					HSLRE.self.bloomAndFlares.flareColorC = bloomAndFlaresSettings.BloomFlareColorC;
					HSLRE.self.bloomAndFlares.flareColorD = bloomAndFlaresSettings.BloomFlareColorD;
				}

				if (HSLRE.self.ccc != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.ccc].enabled = curveSettings.Enabled;
					HSLRE.self.ccc.saturation = curveSettings.CurveSaturation;
					HSLRE.self.ccc.UpdateParameters();
				}

				if (HSLRE.self.antialiasing != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.antialiasing].enabled = antialiasingSettings.enabled;
					HSLRE.self.antialiasing.mode = antialiasingSettings.mode;
					HSLRE.self.antialiasing.showGeneratedNormals = antialiasingSettings.showGeneratedNormals;
					HSLRE.self.antialiasing.offsetScale = antialiasingSettings.offsetScale;
					HSLRE.self.antialiasing.blurRadius = antialiasingSettings.blurRadius;
					HSLRE.self.antialiasing.edgeThresholdMin = antialiasingSettings.edgeThresholdMin;
					HSLRE.self.antialiasing.edgeThreshold = antialiasingSettings.edgeThreshold;
					HSLRE.self.antialiasing.edgeSharpness = antialiasingSettings.edgeSharpness;
					HSLRE.self.antialiasing.dlaaSharp = antialiasingSettings.dlaaSharp;
				}

				if (HSLRE.self.vignette != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.vignette].enabled = vignetteSettings.enabled;
					HSLRE.self.vignette.mode = vignetteSettings.mode;
					HSLRE.self.vignette.intensity = vignetteSettings.intensity;
					if (Studio.Studio.Instance != null)
						Studio.Studio.Instance.sceneInfo.vignetteVignetting = HSLRE.self.vignette.intensity;
					HSLRE.self.vignette.chromaticAberration = vignetteSettings.chromaticAberration;
					HSLRE.self.vignette.axialAberration = vignetteSettings.axialAberration;
					HSLRE.self.vignette.blur = vignetteSettings.blur;
					HSLRE.self.vignette.blurSpread = vignetteSettings.blurSpread;
					HSLRE.self.vignette.luminanceDependency = vignetteSettings.luminanceDependency;
					HSLRE.self.vignette.blurDistance = vignetteSettings.blurDistance;
				}
			}

			//Common settings
			{
				ssaoSettings = new SSAOSetting("SSAO");
				ssrSettings = new SSRSetting("SSR");
				depthOfFieldSettings = new DepthOfFieldSetting("DepthOfField");
				sunShaftsSettings = new SunShaftsSetting("SunShafts");
				noiseAndGrainSettings = new NoiseAndGrainSetting("NoiseAndGrain");
				segiSettings = new SEGISetting("SEGI");
				amplifyBloomSettings = new AmplifyBloomSetting("AmplifyBloom");
				_xmlCommon = new Control("GraphicSetting/Common", commonPreset + ".xml", "Config", new List<Data>
				{
					ssaoSettings,
					ssrSettings,
					depthOfFieldSettings,
					sunShaftsSettings,
					noiseAndGrainSettings,
					segiSettings,
					amplifyBloomSettings
				});
				_xmlCommon.Read();

				if (HSLRE.self.ssao != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.ssao].enabled = ssaoSettings.SSAOEnabled;
					HSLRE.self.ssao.Bias = ssaoSettings.SSAOBias;
					HSLRE.self.ssao.BlurBilateralThreshold = ssaoSettings.SSAOBlurBilateralThreshold;
					HSLRE.self.ssao.BlurPasses = ssaoSettings.SSAOBlurPasses;
					HSLRE.self.ssao.CutoffDistance = ssaoSettings.SSAOCutoffDistance;
					HSLRE.self.ssao.CutoffFalloff = ssaoSettings.SSAOfalloffDistance;
					HSLRE.self.ssao.LumContribution = ssaoSettings.SSAOLumContribution;
					HSLRE.self.ssao.Distance = ssaoSettings.SSAODistance;
					HSLRE.self.ssao.Radius = ssaoSettings.SSAORadius;
					switch (ssaoSettings.SSAOSamples)
					{
						case "VeryLow":
							HSLRE.self.ssao.Samples = SSAOPro.SampleCount.VeryLow;
							break;
						case "Low":
							HSLRE.self.ssao.Samples = SSAOPro.SampleCount.Low;
							break;
						case "Medium":
							HSLRE.self.ssao.Samples = SSAOPro.SampleCount.Medium;
							break;
						case "High":
							HSLRE.self.ssao.Samples = SSAOPro.SampleCount.High;
							break;
						case "Ultra":
							HSLRE.self.ssao.Samples = SSAOPro.SampleCount.Ultra;
							break;
						default:
							HSLRE.self.ssao.Samples = SSAOPro.SampleCount.Ultra;
							ssaoSettings.SSAOSamples = "Ultra";
							break;
					}
					HSLRE.self.ssao.Intensity = ssaoSettings.SSAOIntensity;
					if (Studio.Studio.Instance != null)
						Studio.Studio.Instance.sceneInfo.ssaoIntensity = HSLRE.self.ssao.Intensity;
					HSLRE.self.ssao.UseHighPrecisionDepthMap = ssaoSettings.SSAOUseHighPrecisionDepthMap;
					HSLRE.self.ssao.Downsampling = ssaoSettings.SSAODownsampling;
					switch (ssaoSettings.SSAOBlur)
					{
						case "None":
							HSLRE.self.ssao.Blur = SSAOPro.BlurMode.None;
							break;
						case "Gaussian":
							HSLRE.self.ssao.Blur = SSAOPro.BlurMode.Gaussian;
							break;
						case "Bilateral":
							HSLRE.self.ssao.Blur = SSAOPro.BlurMode.Bilateral;
							break;
						case "HighQualityBilateral":
							HSLRE.self.ssao.Blur = SSAOPro.BlurMode.HighQualityBilateral;
							break;
						default:
							HSLRE.self.ssao.Blur = SSAOPro.BlurMode.Bilateral;
							ssaoSettings.SSAOBlur = "Bilateral";
							break;
					}
					HSLRE.self.ssao.BlurDownsampling = ssaoSettings.SSAOBlurDownsampling;
					HSLRE.self.ssao.OcclusionColor = ssaoSettings.SSAOOcclusionColor;
					Studio.Studio.Instance?.sceneInfo.ssaoColor.SetDiffuseRGBA(HSLRE.self.ssao.OcclusionColor);
				}

				if (HSLRE.self.ssr != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.ssr].enabled = true;
					HSLRE.self.effectsDictionary[HSLRE.self.ssr].enabled = ssrSettings.SSRenabled;
					HSLRE.self.ssr.settings = new SSRSettings
					{
						/*basicSettings = new ScreenSpaceReflection.BasicSettings
						{
							screenEdgeFading = ssrSettings.SSRscreenEdgeFading,
							maxDistance = ssrSettings.SSRmaxDistance,
							fadeDistance = ssrSettings.SSRfadeDistance,
							reflectionMultiplier = ssrSettings.SSRreflectionMultiplier,
							enableHDR = ssrSettings.SSRenableHDR,
							additiveReflection = ssrSettings.SSRadditiveReflection
						},
						reflectionSettings = new ScreenSpaceReflection.ReflectionSettings
						{
							maxSteps = ssrSettings.SSRmaxSteps,
							rayStepSize = ssrSettings.SSRrayStepSize,
							widthModifier = ssrSettings.SSRwidthModifier,
							smoothFallbackThreshold = ssrSettings.SSRsmoothFallbackThreshold,
							distanceBlur = ssrSettings.SSRdistanceBlur,
							fresnelFade = ssrSettings.SSRfresnelFade,
							fresnelFadePower = ssrSettings.SSRfresnelFadePower,
							smoothFallbackDistance = ssrSettings.SSRsmoothFallbackDistance
						},
						advancedSettings = new ScreenSpaceReflection.AdvancedSettings
						{
							useTemporalConfidence = ssrSettings.SSRuseTemporalConfidence,
							temporalFilterStrength = ssrSettings.SSRtemporalFilterStrength,
							treatBackfaceHitAsMiss = ssrSettings.SSRtreatBackfaceHitAsMiss,
							allowBackwardsRays = ssrSettings.SSRallowBackwardsRays,
							traceBehindObjects = ssrSettings.SSRtraceBehindObjects,
							highQualitySharpReflections = ssrSettings.SSRhighQualitySharpReflections,
							traceEverywhere = ssrSettings.SSRtraceEverywhere,
							resolution = (ScreenSpaceReflection.SSRResolution)ssrSettings.SSRresolution,
							bilateralUpsample = ssrSettings.SSRbilateralUpsample,
							improveCorners = ssrSettings.SSRimproveCorners,
							reduceBanding = ssrSettings.SSRreduceBanding,
							highlightSuppression = ssrSettings.SSRhighlightSuppression
						},
						debugSettings = new ScreenSpaceReflection.DebugSettings
						{
							debugMode = (ScreenSpaceReflection.SSRDebugMode)ssrSettings.SSRdebugMode
						}*/

						reflectionSettings = new ReflectionSettings
						{
							blendType = SSRReflectionBlendType.PhysicallyBased,
							reflectionQuality = SSRResolution.High,
							maxDistance = 100.0f,
							iterationCount = 256,
							stepSize = 3,
							widthModifier = 0.5f,
							reflectionBlur = 1.0f,
							reflectBackfaces = true
						},

						intensitySettings = new IntensitySettings
						{
							reflectionMultiplier = 1.0f,
							fadeDistance = 100.0f,

							fresnelFade = 1.0f,
							fresnelFadePower = 1.0f,
						},

						screenEdgeMask = new ScreenEdgeMask
						{
							intensity = 0.03f
						}

					};
				}

				if (HSLRE.self.dof != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.dof].enabled = depthOfFieldSettings.enabled;
					HSLRE.self.dof.focalLength = depthOfFieldSettings.focalLength;
					HSLRE.self.dof.focalSize = depthOfFieldSettings.focalSize;
					if (Studio.Studio.Instance != null)
						Studio.Studio.Instance.sceneInfo.depthFocalSize = HSLRE.self.dof.focalSize;
					HSLRE.self.dof.aperture = depthOfFieldSettings.aperture;
					if (Studio.Studio.Instance != null)
						Studio.Studio.Instance.sceneInfo.depthAperture = HSLRE.self.dof.aperture;
					HSLRE.self.dof.maxBlurSize = depthOfFieldSettings.maxBlurSize;
					HSLRE.self.dof.highResolution = depthOfFieldSettings.highResolution;
					switch (depthOfFieldSettings.blurType)
					{
						case "DiscBlur":
							HSLRE.self.dof.blurType = DepthOfField.BlurType.DiscBlur;
							break;
						case "DX11":
							HSLRE.self.dof.blurType = DepthOfField.BlurType.DX11;
							break;
						default:
							HSLRE.self.dof.blurType = DepthOfField.BlurType.DiscBlur;
							depthOfFieldSettings.blurType = "DiscBlur";
							break;
					}
					switch (depthOfFieldSettings.blurSampleCount)
					{
						case "Low":
							HSLRE.self.dof.blurSampleCount = DepthOfField.BlurSampleCount.Low;
							break;
						case "Medium":
							HSLRE.self.dof.blurSampleCount = DepthOfField.BlurSampleCount.Medium;
							break;
						case "High":
							HSLRE.self.dof.blurSampleCount = DepthOfField.BlurSampleCount.High;
							break;
						default:
							HSLRE.self.dof.blurSampleCount = DepthOfField.BlurSampleCount.High;
							depthOfFieldSettings.blurSampleCount = "High";
							break;
					}
					HSLRE.self.dof.nearBlur = depthOfFieldSettings.nearBlur;
					HSLRE.self.dof.foregroundOverlap = depthOfFieldSettings.foregroundOverlap;
					HSLRE.self.dof.dx11BokehThreshold = depthOfFieldSettings.dx11BokehThreshold;
					HSLRE.self.dof.dx11SpawnHeuristic = depthOfFieldSettings.dx11SpawnHeuristic;
					HSLRE.self.dof.dx11BokehScale = depthOfFieldSettings.dx11BokehScale;
					HSLRE.self.dof.dx11BokehIntensity = depthOfFieldSettings.dx11BokehIntensity;
					HSLRE.self.fixDofForUpscaledScreenshots = depthOfFieldSettings.fixUpscaledScreenshots;
				}

				if (HSLRE.self.sunShafts != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.sunShafts].enabled = sunShaftsSettings.enabled;
					switch (sunShaftsSettings.resolution)
					{
						case "Low":
							HSLRE.self.sunShafts.resolution = SunShafts.SunShaftsResolution.Low;
							break;
						case "Normal":
							HSLRE.self.sunShafts.resolution = SunShafts.SunShaftsResolution.Normal;
							break;
						case "High":
							HSLRE.self.sunShafts.resolution = SunShafts.SunShaftsResolution.High;
							break;
						default:
							HSLRE.self.sunShafts.resolution = SunShafts.SunShaftsResolution.Normal;
							sunShaftsSettings.resolution = "Normal";
							break;
					}
					switch (sunShaftsSettings.screenBlendMode)
					{
						case "Screen":
							HSLRE.self.sunShafts.screenBlendMode = SunShafts.ShaftsScreenBlendMode.Screen;
							break;
						case "Add":
							HSLRE.self.sunShafts.screenBlendMode = SunShafts.ShaftsScreenBlendMode.Add;
							break;
						default:
							HSLRE.self.sunShafts.screenBlendMode = SunShafts.ShaftsScreenBlendMode.Screen;
							sunShaftsSettings.screenBlendMode = "Screen";
							break;
					}
					HSLRE.self.sunShafts.radialBlurIterations = sunShaftsSettings.radialBlurIterations;
					HSLRE.self.sunShafts.sunColor = sunShaftsSettings.sunColor;
					HSLRE.self.sunShafts.sunThreshold = sunShaftsSettings.sunThreshold;
					HSLRE.self.sunShafts.sunShaftBlurRadius = sunShaftsSettings.sunShaftBlurRadius;
					HSLRE.self.sunShafts.sunShaftIntensity = sunShaftsSettings.sunShaftIntensity;
					HSLRE.self.sunShafts.maxRadius = sunShaftsSettings.sunShaftBlurRadius;
					HSLRE.self.sunShafts.useDepthTexture = sunShaftsSettings.useDepthTexture;
				}

				if (HSLRE.self.noiseAndGrain != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.noiseAndGrain].enabled = noiseAndGrainSettings.enabled;
					HSLRE.self.noiseAndGrain.dx11Grain = noiseAndGrainSettings.dx11Grain;
					HSLRE.self.noiseAndGrain.monochrome = noiseAndGrainSettings.monochrome;
					HSLRE.self.noiseAndGrain.intensityMultiplier = noiseAndGrainSettings.intensityMultiplier;
					HSLRE.self.noiseAndGrain.generalIntensity = noiseAndGrainSettings.generalIntensity;
					HSLRE.self.noiseAndGrain.blackIntensity = noiseAndGrainSettings.blackIntensity;
					HSLRE.self.noiseAndGrain.whiteIntensity = noiseAndGrainSettings.whiteIntensity;
					HSLRE.self.noiseAndGrain.midGrey = noiseAndGrainSettings.midGrey;
					HSLRE.self.noiseAndGrain.intensities = noiseAndGrainSettings.intensities;
					switch (noiseAndGrainSettings.filterMode)
					{
						case "Point":
							HSLRE.self.noiseAndGrain.filterMode = FilterMode.Point;
							break;
						case "Bilinear":
							HSLRE.self.noiseAndGrain.filterMode = FilterMode.Bilinear;
							break;
						case "Trilinear":
							HSLRE.self.noiseAndGrain.filterMode = FilterMode.Trilinear;
							break;
						default:
							HSLRE.self.noiseAndGrain.filterMode = FilterMode.Bilinear;
							noiseAndGrainSettings.filterMode = "Bilinear";
							break;
					}
					HSLRE.self.noiseAndGrain.softness = noiseAndGrainSettings.softness;
					HSLRE.self.noiseAndGrain.monochromeTiling = noiseAndGrainSettings.monochromeTiling;
					HSLRE.self.noiseAndGrain.tiling = noiseAndGrainSettings.tiling;
					HSLRE.self.fixNoiseAndGrainForUpscaledScreenshots = noiseAndGrainSettings.fixUpscaledScreenshots;
				}

				if (HSLRE.self.segi != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.segi].enabled = segiSettings.enabled;
					switch (segiSettings.voxelResolution)
					{
						case "low":
							HSLRE.self.segi.voxelResolution = SEGI.VoxelResolution.low;
							break;
						case "high":
							HSLRE.self.segi.voxelResolution = SEGI.VoxelResolution.high;
							break;
						default:
							HSLRE.self.segi.voxelResolution = SEGI.VoxelResolution.high;
							segiSettings.voxelResolution = "high";
							break;
					}
					HSLRE.self.segi.voxelAA = segiSettings.voxelAA;
					HSLRE.self.segi.innerOcclusionLayers = segiSettings.innerOcclusionLayers;
					HSLRE.self.segi.gaussianMipFilter = segiSettings.gaussianMipFilter;
					HSLRE.self.segi.voxelSpaceSize = segiSettings.voxelSpaceSize;
					HSLRE.self.segi.shadowSpaceSize = segiSettings.shadowSpaceSize;
					HSLRE.self.segi.updateGI = segiSettings.updateGI;
					HSLRE.self.segi.infiniteBounces = segiSettings.infiniteBounces;
					HSLRE.self.segi.hsStandardCustomShadersCompatibility = segiSettings.hsStandardCustomShadersCompatibility;

					HSLRE.self.segi.softSunlight = segiSettings.softSunlight;
					HSLRE.self.segi.skyColor = segiSettings.skyColor;
					HSLRE.self.segi.skyIntensity = segiSettings.skyIntensity;
					HSLRE.self.segi.sphericalSkylight = segiSettings.sphericalSkylight;

					HSLRE.self.segi.temporalBlendWeight = segiSettings.temporalBlendWeight;
					HSLRE.self.segi.useBilateralFiltering = segiSettings.useBilateralFiltering;
					HSLRE.self.segi.halfResolution = segiSettings.halfResolution;
					HSLRE.self.segi.stochasticSampling = segiSettings.stochasticSampling;

					HSLRE.self.segi.cones = segiSettings.cones;
					HSLRE.self.segi.coneTraceSteps = segiSettings.coneTraceSteps;
					HSLRE.self.segi.coneLength = segiSettings.coneLength;
					HSLRE.self.segi.coneWidth = segiSettings.coneWidth;
					HSLRE.self.segi.coneTraceBias = segiSettings.coneTraceBias;
					HSLRE.self.segi.occlusionStrength = segiSettings.farOcclusionStrength;
					HSLRE.self.segi.nearOcclusionStrength = segiSettings.nearOcclusionStrength;
					HSLRE.self.segi.farOcclusionStrength = segiSettings.farOcclusionStrength;
					HSLRE.self.segi.farthestOcclusionStrength = segiSettings.farthestOcclusionStrength;
					HSLRE.self.segi.occlusionPower = segiSettings.occlusionPower;
					HSLRE.self.segi.nearLightGain = segiSettings.nearLightGain;
					HSLRE.self.segi.giGain = segiSettings.giGain;

					HSLRE.self.segi.secondaryBounceGain = segiSettings.secondaryBounceGain;
					HSLRE.self.segi.secondaryCones = segiSettings.secondaryCones;
					HSLRE.self.segi.secondaryOcclusionStrength = segiSettings.secondaryOcclusionStrength;

					HSLRE.self.segi.doReflections = segiSettings.doReflections;
					HSLRE.self.segi.reflectionSteps = segiSettings.reflectionSteps;
					HSLRE.self.segi.reflectionOcclusionPower = segiSettings.reflectionOcclusionPower;
					HSLRE.self.segi.skyReflectionIntensity = segiSettings.skyReflectionIntensity;
				}

				if (HSLRE.self.amplifyBloom != null)
				{
					HSLRE.self.effectsDictionary[HSLRE.self.amplifyBloom].enabled = amplifyBloomSettings.enabled;
					if (EnumTryParse(amplifyBloomSettings.UpscaleQuality, out UpscaleQualityEnum newUpscaleQualityEnum))
						HSLRE.self.amplifyBloom.UpscaleQuality = newUpscaleQualityEnum;
					if (EnumTryParse(amplifyBloomSettings.MainThresholdSize, out MainThresholdSizeEnum newMainThresholdSizeEnum))
						HSLRE.self.amplifyBloom.MainThresholdSize = newMainThresholdSizeEnum;
					if (EnumTryParse(amplifyBloomSettings.CurrentPrecisionMode, out PrecisionModes newPrecisionModes))
						HSLRE.self.amplifyBloom.CurrentPrecisionMode = newPrecisionModes;
					HSLRE.self.amplifyBloom.BloomRange = amplifyBloomSettings.BloomRange;
					HSLRE.self.amplifyBloom.OverallIntensity = amplifyBloomSettings.OverallIntensity;
					HSLRE.self.amplifyBloom.OverallThreshold = amplifyBloomSettings.OverallThreshold;
					HSLRE.self.amplifyBloom.UpscaleBlurRadius = amplifyBloomSettings.UpscaleBlurRadius;
					HSLRE.self.amplifyBloom.TemporalFilteringActive = amplifyBloomSettings.TemporalFilteringActive;
					HSLRE.self.amplifyBloom.TemporalFilteringValue = amplifyBloomSettings.TemporalFilteringValue;
					HSLRE.self.amplifyBloom.SeparateFeaturesThreshold = amplifyBloomSettings.SeparateFeaturesThreshold;
					HSLRE.self.amplifyBloom.FeaturesThreshold = amplifyBloomSettings.FeaturesThreshold;
					HSLRE.self.amplifyBloom.ApplyLensDirt = amplifyBloomSettings.ApplyLensDirt;
					HSLRE.self.amplifyBloom.LensDirtStrength = amplifyBloomSettings.LensDirtStrength;
					if (EnumTryParse(amplifyBloomSettings.LensDirtTexture, out LensDirtTextureEnum newLensDirtTextureEnum))
						HSLRE.self.amplifyBloom.LensDirtTexture = newLensDirtTextureEnum;
					HSLRE.self.amplifyBloom.ApplyLensStardurst = amplifyBloomSettings.ApplyLensStardurst;
					HSLRE.self.amplifyBloom.LensStarburstStrength = amplifyBloomSettings.LensStarburstStrength;
					if (EnumTryParse(amplifyBloomSettings.LensStardurstTex, out LensStarburstTextureEnum newLensStarburstTextureEnum))
						HSLRE.self.amplifyBloom.LensStardurstTex = newLensStarburstTextureEnum;
					HSLRE.self.amplifyBloom.BokehFilterInstance.ApplyBokeh = amplifyBloomSettings.BokehFilterInstanceApplyBokeh;
					HSLRE.self.amplifyBloom.BokehFilterInstance.ApplyOnBloomSource = amplifyBloomSettings.BokehFilterInstanceApplyOnBloomSource;
					if (EnumTryParse(amplifyBloomSettings.BokehFilterInstanceApertureShape, out ApertureShape newApertureShape))
						HSLRE.self.amplifyBloom.BokehFilterInstance.ApertureShape = newApertureShape;
					HSLRE.self.amplifyBloom.BokehFilterInstance.OffsetRotation = amplifyBloomSettings.BokehFilterInstanceOffsetRotation;
					HSLRE.self.amplifyBloom.BokehFilterInstance.BokehSampleRadius = amplifyBloomSettings.BokehFilterInstanceBokehSampleRadius;
					HSLRE.self.amplifyBloom.BokehFilterInstance.Aperture = amplifyBloomSettings.BokehFilterInstanceAperture;
					HSLRE.self.amplifyBloom.BokehFilterInstance.FocalLength = amplifyBloomSettings.BokehFilterInstanceFocalLength;
					HSLRE.self.amplifyBloom.BokehFilterInstance.FocalDistance = amplifyBloomSettings.BokehFilterInstanceFocalDistance;
					HSLRE.self.amplifyBloom.BokehFilterInstance.MaxCoCDiameter = amplifyBloomSettings.BokehFilterInstanceMaxCoCDiameter;
					HSLRE.self.amplifyBloom.LensFlareInstance.ApplyLensFlare = amplifyBloomSettings.LensFlareInstanceApplyLensFlare;
					HSLRE.self.amplifyBloom.LensFlareInstance.OverallIntensity = amplifyBloomSettings.LensFlareInstanceOverallIntensity;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount = amplifyBloomSettings.LensFlareInstanceLensFlareGaussianBlurAmount;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity = amplifyBloomSettings.LensFlareInstanceLensFlareNormalizedGhostsIntensity;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostAmount = amplifyBloomSettings.LensFlareInstanceLensFlareGhostAmount;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal = amplifyBloomSettings.LensFlareInstanceLensFlareGhostsDispersal;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion = amplifyBloomSettings.LensFlareInstanceLensFlareGhostChrDistortion;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor = amplifyBloomSettings.LensFlareInstanceLensFlareGhostsPowerFactor;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff = amplifyBloomSettings.LensFlareInstanceLensFlareGhostsPowerFalloff;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity = amplifyBloomSettings.LensFlareInstanceLensFlareNormalizedHaloIntensity;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloWidth = amplifyBloomSettings.LensFlareInstanceLensFlareHaloWidth;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion = amplifyBloomSettings.LensFlareInstanceLensFlareHaloChrDistortion;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor = amplifyBloomSettings.LensFlareInstanceLensFlareHaloPowerFactor;
					HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff = amplifyBloomSettings.LensFlareInstanceLensFlareHaloPowerFalloff;
					HSLRE.self.amplifyBloom.LensGlareInstance.ApplyLensGlare = amplifyBloomSettings.LensGlareInstanceApplyLensGlare;
					HSLRE.self.amplifyBloom.LensGlareInstance.Intensity = amplifyBloomSettings.LensGlareInstanceIntensity;
					HSLRE.self.amplifyBloom.LensGlareInstance.OverallStreakScale = amplifyBloomSettings.LensGlareInstanceOverallStreakScale;
					HSLRE.self.amplifyBloom.LensGlareInstance.OverallTint = amplifyBloomSettings.LensGlareInstanceOverallTint;
					if (EnumTryParse(amplifyBloomSettings.LensGlareInstanceCurrentGlare, out GlareLibType newGlareLibType))
						HSLRE.self.amplifyBloom.LensGlareInstance.CurrentGlare = newGlareLibType;
					HSLRE.self.amplifyBloom.LensGlareInstance.PerPassDisplacement = amplifyBloomSettings.LensGlareInstancePerPassDisplacement;
					HSLRE.self.amplifyBloom.LensGlareInstance.GlareMaxPassCount = amplifyBloomSettings.LensGlareInstanceGlareMaxPassCount;
					HSLRE.self.fixAmplifyBloomForUpscaledScreenshots = amplifyBloomSettings.fixUpscaledScreenshots;
				}
			}

			{
				shadowSettings = new ShadowSetting("Shadow");
				_xmlShadow = new Control("GraphicSetting/Shadow", $"{shadowPreset}.xml", "Config", new List<Data>
				{
					shadowSettings
				});
				_xmlShadow.Read();
				if (shadowSettings.ShadowProjection == 0)
					QualitySettings.shadowProjection = ShadowProjection.CloseFit;
				else if (shadowSettings.ShadowProjection == 1)
					QualitySettings.shadowProjection = ShadowProjection.StableFit;
				QualitySettings.shadowNearPlaneOffset = shadowSettings.ShadowNearPlaneOffset;
				QualitySettings.shadowDistance = shadowSettings.ShadowDistance;
				QualitySettings.shadowCascades = shadowSettings.ShadowCascades;
				QualitySettings.shadowCascade2Split = shadowSettings.ShadowCascade2Split;
				QualitySettings.shadowCascade4Split = new Vector3(shadowSettings.ShadowCascade4Split_x, shadowSettings.ShadowCascade4Split_y, shadowSettings.ShadowCascade4Split_z);
			}
		}

		public static void Save()
		{
			_xmlGraphicsSetting?.Write();
			_xmlImageEffect?.Write();
			_xmlShadow?.Write();
			_xmlStyle?.Write();
			_xmlCommon?.Write();
		}

		private static bool EnumTryParse<T>(string value, out T result) where T : Enum
		{
			try
			{
				result = (T)Enum.Parse(typeof(T), value);
				return true;
			}
			catch (Exception)
			{
				result = default;
				return false;
			}
		}
	}
}
