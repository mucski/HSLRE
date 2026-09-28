using Harmony;
using Config;
using UnityStandardAssets.CinematicEffects;
using System.Reflection;
using UnityEngine;
using System;

namespace HSLRE.HSIBL
{
	internal static partial class IBLPatches
	{
		public static HSLRE.EffectData GetEffect(MonoBehaviour mb) => mb && HSLRE.self.effectsDictionary.TryGetValue(mb, out var effect) ? effect : null;

		/// <summary>
		/// Since a different SSR solution will be used, ensure the vanilla one is disabled and removed from the <see cref="ConfigEffector"/>.
		/// </summary>
		/// <param name="__instance">The config class that stores and updates postprocessing info</param>
		private static void RemoveOldSSR(ConfigEffector __instance)
		{
			if (__instance.ScreenSpaceReflection)
			{
				__instance.ScreenSpaceReflection.enabled = false;
				__instance.ScreenSpaceReflection = null;
			}
		}

		/// <summary>
		/// Patch to update the Enabled state of each LRE effect, when game refreshes the postprocessing effects.
		/// </summary>
		[HarmonyPatch(typeof(ConfigEffector), "Refresh")]
		private static class ConfigEffector_Refresh_Patch
		{
			// perf pass: DOF, Sun Shafts, Bloom and Flares and Antialiasing are rendered by HSLRE's chain, which keeps
			// their components disabled. Refresh (every frame in the main game) switched them back on to match the
			// game config and HSLRE switched them off again in LateUpdate: OnEnable + OnDisable, i.e. their
			// materials destroyed and re-made, every frame. Refresh now doesn't see them (fields hidden for the call).
			private static UnityStandardAssets.ImageEffects.DepthOfField _hiddenDof;
			private static UnityStandardAssets.ImageEffects.SunShafts _hiddenSunShafts;
			private static UnityStandardAssets.ImageEffects.BloomAndFlares _hiddenBloom;
			private static UnityStandardAssets.ImageEffects.Antialiasing _hiddenAntialiasing;

			// last game-config values seen, so a change made in the game's settings is passed on to HSLRE once
			private static bool _seen, _lastDof, _lastSunShafts, _lastBloom, _lastAntialiasing;

			private static void Prefix(ConfigEffector __instance)
			{
				HSLRE lre = HSLRE.self;
				if (lre == null)
					return;
				if (__instance.DepthOfField != null && ReferenceEquals(__instance.DepthOfField, lre.dof))
				{
					_hiddenDof = __instance.DepthOfField;
					__instance.DepthOfField = null;
				}
				if (__instance.SunShafts != null && ReferenceEquals(__instance.SunShafts, lre.sunShafts))
				{
					_hiddenSunShafts = __instance.SunShafts;
					__instance.SunShafts = null;
				}
				if (__instance.BloomAndFlares != null && ReferenceEquals(__instance.BloomAndFlares, lre.bloomAndFlares))
				{
					_hiddenBloom = __instance.BloomAndFlares;
					__instance.BloomAndFlares = null;
				}
				if (__instance.Antialiasing != null && ReferenceEquals(__instance.Antialiasing, lre.antialiasing))
				{
					_hiddenAntialiasing = __instance.Antialiasing;
					__instance.Antialiasing = null;
				}
			}

			/// <summary>A game-config switch that changed is applied to the HSLRE effect (only on the change).</summary>
			private static void SyncOnChange(ref bool last, bool now, MonoBehaviour effect)
			{
				if (_seen && now != last)
				{
					HSLRE.EffectData data = GetEffect(effect);
					if (data != null)
						data.enabled = now;
				}
				last = now;
			}

			/// <summary>
			/// Update the Enabled state of each LRE effect, when game refreshes the postprocessing effects.
			/// </summary>
			/// <param name="__instance">The config class that stores and updates postprocessing info</param>
			private static void Postfix(ConfigEffector __instance)
			{
				// perf pass: give the hidden components back
				if (_hiddenDof != null) { __instance.DepthOfField = _hiddenDof; _hiddenDof = null; }
				if (_hiddenSunShafts != null) { __instance.SunShafts = _hiddenSunShafts; _hiddenSunShafts = null; }
				if (_hiddenBloom != null) { __instance.BloomAndFlares = _hiddenBloom; _hiddenBloom = null; }
				if (_hiddenAntialiasing != null) { __instance.Antialiasing = _hiddenAntialiasing; _hiddenAntialiasing = null; }

				RemoveOldSSR(__instance);

				// perf pass: the old per-effect checks for these compared the game config with the component right
				// after Refresh had made them equal, so they never fired (the changelog's "toggles in the main game
				// update HSLRE" didn't work for them); now a change in the game config is passed on once
				HSLRE lre = HSLRE.self;
				if (lre != null)
				{
					SyncOnChange(ref _lastDof, Manager.Config.EtcData.DepthOfField, lre.dof);
					SyncOnChange(ref _lastSunShafts, Manager.Config.EtcData.SunShafts, lre.sunShafts);
					SyncOnChange(ref _lastBloom, Manager.Config.EtcData.Bloom, lre.bloomAndFlares);
					SyncOnChange(ref _lastAntialiasing, Manager.Config.EtcData.AntiAliasing, lre.antialiasing);
					_seen = true;
				}

				// get effect from main cam
				var e = GetEffect(HSLRE.self.ssao);
				if (e != null && Manager.Config.EtcData.SSAO != __instance.SSAOPro.enabled)
				{
					e.enabled = Manager.Config.EtcData.SSAO;
				}

				e = GetEffect(HSLRE.self.ssr);
				if (e != null && Manager.Config.EtcData.SSR != e.enabled) // use own SSR component here, since we removed the game's old ScreenSpaceReflection component
				{
					e.enabled = Manager.Config.EtcData.SSR;
				}

				e = GetEffect(HSLRE.self.vignette);
				if (e != null && Manager.Config.EtcData.Vignette != __instance.VignetteAndChromaticAberration.enabled)
				{
					e.enabled = Manager.Config.EtcData.Vignette;
				}
			}
		}

		/// <summary>
		/// Patch to ensure the old SSR effect remains disabled, when <see cref="ConfigEffector"/> is reset.
		/// </summary>
		[HarmonyPatch(typeof(ConfigEffector), "Reset")]
		private static class ConfigEffector_Reset_Patch
		{
			/// <summary>
			/// Ensure old SSR effect stays disabled when <see cref="ConfigEffector"/> is reset.
			/// </summary>
			/// <param name="__instance">The config class that stores and updates postprocessing info</param>
			private static void Postfix(ConfigEffector __instance) => RemoveOldSSR(__instance);
		}

		/// <summary>
		/// Patch to ensure old SSR effect stays disabled when toggling the SSR setting.
		/// </summary>
		[HarmonyPatch]
		private static class SystemButtonCtrl_SSRInfo_UpdateInfo
		{
			private static MethodInfo TargetMethod() => AccessTools.Method("Studio.SystemButtonCtrl+SSRInfo:UpdateInfo");

			/// <summary>
			/// Ensure old SSR effect stays disabled when toggling the SSR setting
			/// </summary>
			/// <param name="__instance">The <see cref="Studio.SystemButtonCtrl.ssrInfo"/> that toggles the old SSR component</param>
			private static void Postfix(object __instance)
			{
				var screenSpaceReflection = new Traverse(__instance).Property("screenSpaceReflection").GetValue<ScreenSpaceReflection>();
				if (screenSpaceReflection)
				{
					screenSpaceReflection.enabled = false;
				}
			}
		}


		#region Update LRE effect when changed in game
		/// <summary>
		/// Update a LRE effect's Enabled state to match, when changed in the game's default UI
		/// </summary>
		/// <param name="__instance">The effect info instance of the effect being updated</param>
		/// <param name="_value">The updated Enabled state of the effect</param>
		/// <param name="effect">The LRE effect to update</param>
		private static void Update_OnValueChangedEnable(object __instance, bool _value, HSLRE.EffectData effect)
		{
			//UnityEngine.Debug.Log($"Toggling effect: {effect?.simpleName}\r\n\tnew value:{_value}\r\n\tcurrent value: {effect?.enabled}");
			var isUpdateInfo = new Traverse(__instance).Field("isUpdateInfo").GetValue<bool>();
			if (!isUpdateInfo && effect != null) // toggle effect
			{
				effect.enabled = _value;
			}
		}

		[HarmonyPatch]
		private static class SystemButtonCtrl_SSAOInfo_OnValueChangedEnable
		{
			private static MethodInfo TargetMethod() => AccessTools.Method("Studio.SystemButtonCtrl+SSAOInfo:OnValueChangedEnable");

			private static void Postfix(object __instance, bool _value) => Update_OnValueChangedEnable(__instance, _value, GetEffect(HSLRE.self.ssao));

		}

		[HarmonyPatch]
		private static class SystemButtonCtrl_SunShaftsInfo_OnValueChangedEnable
		{
			private static MethodInfo TargetMethod() => AccessTools.Method("Studio.SystemButtonCtrl+SunShaftsInfo:OnValueChangedEnable");

			private static void Postfix(object __instance, bool _value) => Update_OnValueChangedEnable(__instance, _value, GetEffect(HSLRE.self.sunShafts));

		}

		[HarmonyPatch]
		private static class SystemButtonCtrl_DOFInfo_OnValueChangedEnable
		{
			private static MethodInfo TargetMethod() => AccessTools.Method("Studio.SystemButtonCtrl+DOFInfo:OnValueChangedEnable");

			private static void Postfix(object __instance, bool _value) => Update_OnValueChangedEnable(__instance, _value, GetEffect(HSLRE.self.dof));

		}

		[HarmonyPatch]
		private static class SystemButtonCtrl_SSRInfo_OnValueChangedEnable
		{
			private static MethodInfo TargetMethod() => AccessTools.Method("Studio.SystemButtonCtrl+SSRInfo:OnValueChangedEnable");

			private static void Postfix(object __instance, bool _value) => Update_OnValueChangedEnable(__instance, _value, GetEffect(HSLRE.self.ssr));

		}

		[HarmonyPatch]
		private static class SystemButtonCtrl_BloomInfo_OnValueChangedEnable
		{
			private static MethodInfo TargetMethod() => AccessTools.Method("Studio.SystemButtonCtrl+BloomInfo:OnValueChangedEnable");

			private static void Postfix(object __instance, bool _value) => Update_OnValueChangedEnable(__instance, _value, GetEffect(HSLRE.self.bloomAndFlares));

		}

		[HarmonyPatch]
		private static class SystemButtonCtrl_VignetteInfo_OnValueChangedEnable
		{
			private static MethodInfo TargetMethod() => AccessTools.Method("Studio.SystemButtonCtrl+VignetteInfo:OnValueChangedEnable");

			private static void Postfix(object __instance, bool _value) => Update_OnValueChangedEnable(__instance, _value, GetEffect(HSLRE.self.vignette));

		}
		#endregion
	}
}
