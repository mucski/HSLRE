using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Config;
using Harmony;
using HSLRE.CustomEffects;
using HSLRE.HSIBL;
using IllusionPlugin;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;
using UnityStandardAssets.ImageEffects;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using LensAberrations = UnityStandardAssets.CinematicEffects.LensAberrations;
//using LensAberrations = HSLRE.CustomEffects.LensAberrations;
using SMAA = UnityStandardAssets.CinematicEffects.SMAA;

/*
HSLRE/HSIBL changelog:
v5.2.0.1:
- Merged into LRE plugin
- IBL Window now scales based on game's window
- Big sliders and scrollbars
v5.2.0.2:
- New SSR (fixes visual corruption)
- Fixed default light rotation not being updated on scene load
- IBL settings synced with in-game equivalent graphics options
- Sliders clamped where required
- More tooltips
v5.2.0.6:
- The SSR feature no longer degrades quality or becomes incorrectly enabled, when taking screenshots
- The following settings should update correctly when toggled in the main game: Bloom (IBL equivalent: 'Bloom and Flares'), SSR, Depth of Field, SSAO, Sun Shafts, Vignette (IBL equivalent: Vignette and Chromatic Aberration), and Antialiasing
- Distortion/Chromatic Aberration/Vignette effects no longer distorted
- Changing Ambient Intensity now works even when no cubemap is selected
- SEGI: The 'main light' selection used by SEGI should now be updated as expected when loading scenes and when changing light intensity (SEGI uses the strongest directional light as 'the sun' in its calculations, but this wasn't always updated correctly)
- Volumetric lights effect is no longer distorted when taking upscaled screenshots
*/
namespace HSLRE
{
	public partial class HSLRE : GenericPlugin, IEnhancedPlugin
	{
		public override string Name => "HSLRE";
		//public override string Version { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
		public override string[] Filter => new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" };

		#region Types
		[Flags]
		public enum EffectType
		{
			FourKDiffuse = 1 << 0,
			LRE = 1 << 1
		}
		#endregion

		public static HSLRE self;

		#region Private Variables
		private readonly List<MonoBehaviour> _effectsToIgnore = new List<MonoBehaviour>();
		private int _currentEffectIndex;
		private RenderTexture _tempSource;
		private RenderTexture _tempDestination;
		private RenderTexture _originalDestination;
		internal AssetBundle _resources;
		#endregion

		#region Public Variables
		public readonly Dictionary<object, EffectData> effectsDictionary = new Dictionary<object, EffectData>();
		public bool init = false;

		public readonly List<EffectData> opaqueEffects = new List<EffectData>();
		public SEGI segi;
		public SSAOPro ssao;
		public ScreenSpaceReflection2 ssr;
		public VolumetricLightRenderer volumetricLights;
		//private GlobalFog _fog;

		public readonly List<EffectData> generalEffects = new List<EffectData>();
		public DepthOfField dof;
		public VignetteAndChromaticAberration vignette;
		public SunShafts sunShafts;
		public SMAA smaa;
		public CustomEffects.Bloom cinematicBloom;
		public CustomEffects.TonemappingColorGrading tonemapping;
		public LensAberrations lensAberrations;
		public ColorCorrectionCurves ccc;
		public BloomAndFlares bloomAndFlares;
		public Antialiasing antialiasing;
		public NoiseAndGrain noiseAndGrain;
		public CameraMotionBlur motionBlur;
		public AfterImage afterImage;
		public AmplifyBloom amplifyBloom;
		public BlurOptimized blur;

		public bool fixDofForUpscaledScreenshots = true;
		public bool fixNoiseAndGrainForUpscaledScreenshots = true;
		public bool fixBlurForUpscaledScreenshots = true;
		public bool fixAmplifyBloomForUpscaledScreenshots = true;
		public bool fixSsrForUpscaledScreenshots = true;
		#endregion

		static Camera cam;
		static Camera Cam
		{
			get
			{
				if (!cam)
				{
					cam = Camera.main;
				}
				return cam;
			}
		}

		#region Unity Methods
		protected override void Awake()
		{
			base.Awake();
			self = this;
			this._resources = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("HSLRE.Resources.LREResources.unity3d"));
			//Res.Init();

			var harmonyInstance = HarmonyExtensions.CreateInstance("com.joan6694.illusionplugins.hslre");
			harmonyInstance.PatchAllSafe();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Settings.Save();
		}

		/// <summary>
		/// Perf pass: the effects HSLRE creates itself (plain classes, not components) are made anew on every level
		/// load. The old ones were never shut down and stayed referenced from effectsDictionary, which was never
		/// cleared - their materials, textures and buffers leaked on each load. Components die with their scene.
		/// </summary>
		private void DisposeOldEffects()
		{
			foreach (EffectData data in this.effectsDictionary.Values)
			{
				if (data.effect == null || data.effect is MonoBehaviour)
					continue;
				try
				{
					if (data.effect is AmplifyBloom)
						((AmplifyBloom)data.effect).Dispose();
					else if (data.effect is AfterImage)
						((AfterImage)data.effect).Dispose();
					else if (data._enabled && data.onDisable != null)
						data.onDisable(); // an effect that is off already cleaned up when it was switched off
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogWarning("[LRE] Could not dispose " + data.simpleName + ": " + e.Message);
				}
			}
			this.effectsDictionary.Clear();
		}

		protected override void LevelLoaded(int level)
		{
			this.DisposeOldEffects();
			this._effectsToIgnore.Clear();
			this.opaqueEffects.Clear();
			this.generalEffects.Clear();
			this.init = false;
			Camera mainCamera = /*this.*/GetMainCamera();
			cam = GetMainCamera();

			if (mainCamera == null)
				return;
			this.ExecuteDelayed(() =>
			{
				// try adding new SSR script before other effects
				this.ssao = mainCamera.GetComponent<SSAOPro>();
				if (this.ssao != null)
				{
					// disable old SSR
					var oldSSR = (MonoBehaviour)mainCamera.GetComponent("ScreenSpaceReflection");
					if (oldSSR)
					{
						oldSSR.enabled = false;
					}
					if (mainCamera.GetComponent<ScreenSpaceReflection2>() == null)
					{
						mainCamera.gameObject.AddComponent<ScreenSpaceReflection2>();
					}
				}

				// perf pass: a camera that survives a level load already has one; adding another ran SMAA / SEGI hooks twice
				CameraEventsDispatcher dispatcher = mainCamera.gameObject.GetComponent<CameraEventsDispatcher>() ?? mainCamera.gameObject.AddComponent<CameraEventsDispatcher>();
				dispatcher.onPreCull -= this.OnPreCull;
				dispatcher.onPreRender -= this.OnPreRender;
				dispatcher.onPostRender -= this.OnPostRender;
				dispatcher.onPreCull += this.OnPreCull;
				dispatcher.onPreRender += this.OnPreRender;
				dispatcher.onPostRender += this.OnPostRender;

				if (this.ssao != null)
				{
					try
					{
						this.segi = new SEGI(mainCamera);
						this.segi.ApplyPreset(10);
						this.AddPostProcessingToList(true, false, this.segi, false, EffectType.FourKDiffuse | EffectType.LRE, "segi");
					}
					catch
					{
						this.segi = null;
					}

					this.AddPostProcessingToList(true, false, this.ssao, true, EffectType.FourKDiffuse | EffectType.LRE, "ssao", (int)EtceteraSystem.EffectKind.SSAO);


					// disable old SSR
					/*var oldSSR = (MonoBehaviour)mainCamera.GetComponent("ScreenSpaceReflection");
					if (oldSSR)
					{
						oldSSR.enabled = false;
					}*/

					this.ssr = mainCamera.GetComponent<ScreenSpaceReflection2>();
					if (ssr == null)
					{
						ssr = mainCamera.gameObject.AddComponent<ScreenSpaceReflection2>();
					}
					this.AddPostProcessingToList(true, true, this.ssr, this.ssr != null && this.ssr.enabled, EffectType.FourKDiffuse | EffectType.LRE, "ssr", (int)EtceteraSystem.EffectKind.SSR);
					//this.IgnoreEffect(this.ssr);

					if (this.binary == Binary.Studio)
					{
						this.volumetricLights = mainCamera.gameObject.AddComponent<VolumetricLightRenderer>();
						this.volumetricLights.enabled = false;
						this.AddPostProcessingToList(true, true, this.volumetricLights, this.volumetricLights.enabled, EffectType.FourKDiffuse | EffectType.LRE, "volumetricLights");
					}
				}

				this.vignette = mainCamera.GetComponent<VignetteAndChromaticAberration>();
				if (this.vignette != null)
				{
					this.dof = mainCamera.GetComponent<DepthOfField>();
					this.AddPostProcessingToList(false, false, this.dof, this.dof != null && this.dof.enabled, EffectType.FourKDiffuse | EffectType.LRE, "depthOfField", (int)EtceteraSystem.EffectKind.DepthOfField);
					this.IgnoreEffect(this.dof);

					this.AddPostProcessingToList(false, false, this.vignette, this.vignette.enabled, EffectType.FourKDiffuse, "vignetteAndChromaticAberration", (int)EtceteraSystem.EffectKind.Vignette);

					this.sunShafts = mainCamera.GetComponent<SunShafts>();
					this.AddPostProcessingToList(false, false, this.sunShafts, this.sunShafts != null && this.sunShafts.enabled, EffectType.FourKDiffuse | EffectType.LRE, "sunShafts", (int)EtceteraSystem.EffectKind.SunShafts);
					this.IgnoreEffect(this.sunShafts);

					this.smaa = new SMAA();
					this.AddPostProcessingToList(false, false, this.smaa, true, EffectType.LRE, "smaa");

					this.cinematicBloom = new CustomEffects.Bloom();
					this.AddPostProcessingToList(false, false, this.cinematicBloom, true, EffectType.LRE, "bloom");

					this.amplifyBloom = new AmplifyBloom(mainCamera);
					this.AddPostProcessingToList(false, false, this.amplifyBloom, false, EffectType.FourKDiffuse | EffectType.LRE, "amplifyBloom");

					this.tonemapping = new CustomEffects.TonemappingColorGrading();
					this.AddPostProcessingToList(false, false, this.tonemapping, true, EffectType.LRE, "tonemapping");

					this.ccc = mainCamera.GetComponent<ColorCorrectionCurves>();
					this.AddPostProcessingToList(false, false, this.ccc, this.ccc != null && this.ccc.enabled, EffectType.FourKDiffuse, "colorCorrectionCurves");
					this.IgnoreEffect(this.ccc);

					this.bloomAndFlares = mainCamera.GetComponent<BloomAndFlares>();
					this.AddPostProcessingToList(false, false, this.bloomAndFlares, this.bloomAndFlares != null && this.bloomAndFlares.enabled, EffectType.FourKDiffuse, "bloomAndFlares", (int)EtceteraSystem.EffectKind.Bloom);
					this.IgnoreEffect(this.bloomAndFlares);

					this.motionBlur = mainCamera.gameObject.AddComponent<CameraMotionBlur>();
					this.AddPostProcessingToList(false, false, this.motionBlur, false, EffectType.FourKDiffuse | EffectType.LRE, "motionBlur");
					this.IgnoreEffect(this.motionBlur);

					this.afterImage = new AfterImage();
					this.AddPostProcessingToList(false, false, this.afterImage, false, EffectType.FourKDiffuse | EffectType.LRE, "afterImage");

					this.noiseAndGrain = mainCamera.gameObject.AddComponent<NoiseAndGrain>();
					this.AddPostProcessingToList(false, false, this.noiseAndGrain, false, EffectType.FourKDiffuse | EffectType.LRE, "noiseAndGrain");
					this.IgnoreEffect(this.noiseAndGrain);

					// note: null
					//this.lensAberrations = mainCamera.gameObject.AddComponent<LensAberrations>();
					this.lensAberrations = new LensAberrations();
					this.AddPostProcessingToList(false, false, this.lensAberrations, true, EffectType.LRE, "lens");

					this.blur = mainCamera.gameObject.AddComponent<BlurOptimized>();
					this.AddPostProcessingToList(false, false, this.blur, false, EffectType.FourKDiffuse | EffectType.LRE, "blur");
					this.IgnoreEffect(this.blur);

					this.antialiasing = mainCamera.GetComponent<Antialiasing>();
					this.AddPostProcessingToList(false, false, this.antialiasing, this.antialiasing != null && this.antialiasing.enabled, EffectType.FourKDiffuse, "antialiasing", (int)EtceteraSystem.EffectKind.AntiAliasing);
					this.IgnoreEffect(this.antialiasing);
				}

				Settings.Load();

				if (this.binary == Binary.Game && (level == 21 || level == 22))
				{
					if (Settings.basicSettings.CharaMakerReform)
					{
						Camera.main.clearFlags = (CameraClearFlags)Settings.basicSettings.CharaMakerBackgroundType;
						Camera.main.renderingPath = RenderingPath.DeferredShading;
						Camera.main.hdr = true;
						Camera.main.backgroundColor = Settings.basicSettings.CharaMakerBackground;
						UnityEngine.Debug.Log("Chara maker reformed");
					}
				}
				else
				{
					mainCamera.hdr = true;
					mainCamera.renderingPath = RenderingPath.DeferredShading;
					if (Camera.main != null) //Because sometimes they're not the same, idk m8
					{
						Camera.main.hdr = true;
						Camera.main.renderingPath = RenderingPath.DeferredShading;
					}
				}

				this.init = true;
			}, 1);
		}

		protected override void Update()
		{
			if (this.segi != null && this.effectsDictionary[this.segi].enabled)
				this.segi.Update();
			//if (this.volumetricLights != null && this.effectsDictionary[this.volumetricLights].enabled)
			// this.volumetricLights.Update();
		}

		protected override void LateUpdate()
		{
			if (this.ssao != null)
				this.ssao.enabled = true;
			if (this.vignette != null)
				this.vignette.enabled = true;
			foreach (MonoBehaviour effect in this._effectsToIgnore)
				effect.enabled = false;
		}
		#endregion

		#region Public Methods
		public void MoveUp(object effect)
		{
			int index = this.generalEffects.FindIndex(e => e.effect == effect);
			if (index <= 0)
				return;
			EffectData temp = this.generalEffects[index - 1];
			this.generalEffects[index - 1] = this.generalEffects[index];
			this.generalEffects[index] = temp;
		}

		public void MoveDown(object effect)
		{
			int index = this.generalEffects.FindIndex(e => e.effect == effect);
			if (index == -1 || index == this.generalEffects.Count - 1)
				return;
			EffectData temp = this.generalEffects[index + 1];
			this.generalEffects[index + 1] = this.generalEffects[index];
			this.generalEffects[index] = temp;
		}

		public void ResetOrder()
		{
			this.generalEffects.Sort((x, y) => x.originalPosition.CompareTo(y.originalPosition));
		}

		public void MoveAtIndex(object effect, int index)
		{
			EffectData data = this.effectsDictionary[effect];
			this.generalEffects.Remove(data);
			if (index >= this.generalEffects.Count)
				this.generalEffects.Add(data);
			else
				this.generalEffects.Insert(index, data);
		}
		#endregion

		#region Private Methods
		public static Camera GetMainCamera()
		{
			Camera GetCamAtPath(string path, bool getMainCamIfFailed = false)
			{
				GameObject go = GameObject.Find(path);
				return go ? go.GetComponent<Camera>() : (getMainCamIfFailed ? Camera.main : null);
			}

			switch (self.binary)
			{
				case Binary.Studio:
					return GetCamAtPath("/StudioScene/Camera/Main Camera");
				case Binary.Game:
					var level = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
					if (level == 3)
					{
						return GetCamAtPath("/TitleScene/Camera Map Chara", true);
					}
					if (level == 21)
					{
						return GetCamAtPath("/CustomScene/Camera", true);
					}
					return Camera.main;
			}
			return null;
		}

		private void AddPostProcessingToList(bool opaqueEffect, bool treatAsMonobehaviour, object effect, bool enabledByDefault, EffectType effectType, string simpleName, int gameEffectKind = -1)
		{
			//UnityEngine.Debug.Log("[LRE] Adding effect: +" + effect);
			if (effect == null)
				return;

			try
			{
				EffectData data = new EffectData(effect, enabledByDefault, effectType, treatAsMonobehaviour, opaqueEffect ? this.opaqueEffects.Count : this.generalEffects.Count, simpleName, gameEffectKind);

				// note: adding special case for SSR, since the new version doesn't have an OnRenderImage method
				if (data.onRenderImage != null || data.onRenderImageWithCamera != null || effect.GetType().Equals(typeof(ScreenSpaceReflection2)))
				{
					if (!opaqueEffect)
						this.generalEffects.Add(data);
					else
						this.opaqueEffects.Add(data);
					this.effectsDictionary.Add(effect, data);
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogWarning($"[LRE] Failed to add effect `{effect}`\r\nError:\r\n{ex}");
			}
		}

		private void IgnoreEffect(MonoBehaviour effect)
		{
			if (effect != null)
				this._effectsToIgnore.Add(effect);
		}

		private void OnPreCull(Camera camera)
		{
			if (this.smaa != null && this.effectsDictionary[this.smaa].enabled)
				this.smaa.OnPreCull(camera);
		}
		private void OnPreRender(Camera camera)
		{
			if (this.segi != null && this.effectsDictionary[this.segi].enabled)
				this.segi.OnPreRender();
			//if (this.volumetricLights != null && this.effectsDictionary[this.volumetricLights].enabled)
			//    this.volumetricLights.OnPreRender();
		}

		private void OnPostRender(Camera camera)
		{
			if (this.smaa != null && this.effectsDictionary[this.smaa].enabled)
				this.smaa.OnPostRender(camera);
		}

		/// <summary>
		/// Perf pass: effects that are switched on but would only copy the image through. Skipping one also skips
		/// its ping-pong swap, so the picture is unchanged - minus a full-screen HDR copy per frame.
		/// </summary>
		private bool NothingToDo(object effect)
		{
			if (ReferenceEquals(effect, this.lensAberrations))
				return this.lensAberrations.vignette.enabled == false && this.lensAberrations.chromaticAberration.enabled == false && this.lensAberrations.distortion.enabled == false;
			if (ReferenceEquals(effect, this.cinematicBloom))
				return this.cinematicBloom.settings.intensity <= 0f;
			return false;
		}

		private bool RenderEffectListPre(object hostInstance, ref RenderTexture source, ref RenderTexture destination, List<EffectData> effectList)
		{
			this._originalDestination = destination;
			// The chain works on a copy of the camera's image, as in the original. Starting on the camera's own image
			// (a perf-pass shortcut, one copy less) turned the picture upside down whenever the camera rendered with
			// MSAA - the main game before HSLRE is switched on: on DirectX that image is stored flipped and this plain
			// copy is what puts it right. The temporaries have no depth buffer / MSAA; no effect in the chain uses them.
			this._tempSource = RenderTexture.GetTemporary(source.width, source.height, 0, source.format, RenderTextureReadWrite.Default, 1);
			this._tempDestination = RenderTexture.GetTemporary(source.width, source.height, 0, source.format, RenderTextureReadWrite.Default, 1);
			Graphics.Blit(source, this._tempSource);

			this._currentEffectIndex = 0;
			bool hostEffectEnabled = true;
			for (; this._currentEffectIndex < effectList.Count; ++this._currentEffectIndex)
			{
				EffectData effect = effectList[this._currentEffectIndex];
				if (ReferenceEquals(effect.effect, hostInstance))
				{
					hostEffectEnabled = effect._enabled;
					++this._currentEffectIndex;
					break;
				}
				if (effect._enabled == false || this.NothingToDo(effect.effect))
					continue;
				//effect.onRenderImage(this._tempSource, this._tempDestination);
				EffectCost.Start(effect.effect); // addition: cost per effect (only while measuring)
				effect.OnRenderImage(this._tempSource, this._tempDestination, Cam);
				EffectCost.Stop(effect.effect);

				RenderTexture temp = this._tempSource;
				this._tempSource = this._tempDestination;
				this._tempDestination = temp;
			}

			source = this._tempSource;
			destination = this._tempDestination;

			if (hostEffectEnabled)
			{
				EffectCost.Start(hostInstance); // addition: the host effect renders right after this prefix
				RenderTexture temp = this._tempSource;
				this._tempSource = this._tempDestination;
				this._tempDestination = temp;
			}

			return hostEffectEnabled;
		}

		private void RenderEffectListPost(object hostInstance, RenderTexture source, RenderTexture destination, List<EffectData> effectList)
		{
			EffectCost.Stop(hostInstance); // addition: ignored when the host didn't render
			for (; this._currentEffectIndex < effectList.Count; ++this._currentEffectIndex)
			{
				EffectData effect = effectList[this._currentEffectIndex];
				if (effect._enabled == false || this.NothingToDo(effect.effect))
					continue;
				//effect.onRenderImage(this._tempSource, this._tempDestination);
				EffectCost.Start(effect.effect); // addition: cost per effect (only while measuring)
				effect.OnRenderImage(this._tempSource, this._tempDestination, Cam);
				EffectCost.Stop(effect.effect);

				RenderTexture temp = this._tempSource;
				this._tempSource = this._tempDestination;
				this._tempDestination = temp;
			}
			Graphics.Blit(this._tempSource, this._originalDestination);
			RenderTexture.ReleaseTemporary(this._tempSource);
			RenderTexture.ReleaseTemporary(this._tempDestination);

		}
		#endregion

		#region Patches
		[HarmonyPatch(typeof(SSAOPro), "OnRenderImage", typeof(RenderTexture), typeof(RenderTexture))]
		private static class SSAOPro_OnRenderImage_Patches
		{
			private static bool Prefix(SSAOPro __instance, RenderTexture source, RenderTexture destination)
			{
				if (self.ssao != __instance)
					return true;

				if (self.segi != null && self.effectsDictionary.TryGetValue(self.segi, out EffectData data) && data.enabled)
				{
					EffectCost.Start(self.segi); // addition
					RenderTexture temporary = RenderTexture.GetTemporary(destination.width, destination.height, destination.depth, destination.format, RenderTextureReadWrite.Default, destination.antiAliasing);
					//data.onRenderImage(source, temporary);
					data.OnRenderImage(source, temporary, Cam);
					Graphics.Blit(temporary, source);
					RenderTexture.ReleaseTemporary(temporary);
					EffectCost.Stop(self.segi);
				}
				if (self.effectsDictionary[__instance].enabled == false)
				{
					Graphics.Blit(source, destination);
					return false;
				}
				EffectCost.Start(__instance); // addition: SSAO itself renders after this prefix
				return true;
			}

			private static void Postfix(SSAOPro __instance)
			{
				EffectCost.Stop(__instance); // addition
			}
		}

		[HarmonyPatch(typeof(VignetteAndChromaticAberration), "OnRenderImage", typeof(RenderTexture), typeof(RenderTexture))]
		private static class VignetteAndChromaticAberration_OnRenderImage_Patches
		{
			[HarmonyAfter("com.joan6694.illusionplugins.fogeditor")]
			private static bool Prefix(VignetteAndChromaticAberration __instance, ref RenderTexture source, ref RenderTexture destination)
			{
				if (self.vignette != __instance)
					return true;
				return self.RenderEffectListPre(__instance, ref source, ref destination, self.generalEffects);
			}

			[HarmonyBefore("com.joan6694.illusionplugins.fogeditor")]
			private static void Postfix(VignetteAndChromaticAberration __instance, RenderTexture source, RenderTexture destination)
			{
				if (self.vignette != __instance)
					return;
				self.RenderEffectListPost(__instance, source, destination, self.generalEffects);
			}
		}
		#endregion
	}


}
