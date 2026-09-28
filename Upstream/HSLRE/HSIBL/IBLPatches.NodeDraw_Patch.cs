using Harmony;
using Config;
using ToolBox;
using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using static Config.EtceteraSystem;

namespace HSLRE.HSIBL
{
	internal static partial class IBLPatches
	{
		static NodeDraw nodeDraw = null;

		/// <summary>
		/// Update the in-game UI and Config Manager
		/// </summary>
		/// <param name="enabled">Enabled/Disabled state of the .</param>
		/// <param name="gameEffectKind">The Graphics Effect index in Manager.Config.EtcData.imageEffect[] array</param>
		public static void UpdateGameSettingsUI(bool enabled, int gameEffectKind)
		{
			if (Manager.Config.IsInstance()
				&& gameEffectKind > -1
				&& gameEffectKind < Manager.Config.EtcData.imageEffect.Length
				&& Manager.Config.EtcData.imageEffect[gameEffectKind] != enabled)
			{
				Manager.Config.EtcData.imageEffect[gameEffectKind] = enabled;
			}
			if (nodeDraw)
			{
				nodeDraw.SetParam();
			}
		}

		/// <summary>
		/// Patch to update IBL setting when editing a corresponding effect via the in-game settings menu
		/// </summary>
		[HarmonyPatch(typeof(NodeDraw), "OnValueChangeEffect")]
		private static class NodeDraw_OnValueChangeEffect_Patch
		{
			static bool Prepare() => HSLRE.self.binary == Binary.Game;

			private static void Postfix(NodeDraw __instance, bool _value, int _index)
			{
				// update ibl setting with for setting at corresponding in-game setting index
				EffectKind kind = (EffectKind)_index;
				HSLRE.EffectData effect = null;
				switch (kind)
				{
					case EffectKind.SSAO:			effect = GetEffect(HSLRE.self.ssao); break;
					case EffectKind.SSR:			effect = GetEffect(HSLRE.self.ssr); break;
					case EffectKind.DepthOfField:	effect = GetEffect(HSLRE.self.dof); break;
					case EffectKind.Vignette:		effect = GetEffect(HSLRE.self.vignette); break;
					case EffectKind.SunShafts:		effect = GetEffect(HSLRE.self.sunShafts); break;
					case EffectKind.Bloom:			effect = GetEffect(HSLRE.self.bloomAndFlares); break;
					case EffectKind.AntiAliasing:	effect = GetEffect(HSLRE.self.antialiasing); break;
				}
				if (effect != null)
				{
					effect.enabled = _value;
				}
			}
		}

		/// <summary>
		/// Patch to grab an intance of the NodeDraw instance, when the Settings menu is created; this will be used to update the in-game settings UI.
		/// </summary>
		[HarmonyPatch(typeof(NodeDraw), "Init")]
		private static class NodeDraw_Enable_Patch
		{
			static bool Prepare() => HSLRE.self.binary == Binary.Game;

			private static void Postfix(NodeDraw __instance)
			{
				nodeDraw = __instance;
			}
		}

		/// <summary>
		/// Patch to clear the tracked NodeDraw instance, when exiting the Settings menu; updating effects will no longer update in-game UI.
		/// </summary>
		[HarmonyPatch(typeof(ConfigScene))]
		private static class NodeDraw_Disable_Patch
		{
			static bool Prepare() => HSLRE.self.binary == Binary.Game;

			private static IEnumerable<MethodBase> TargetMethods()
			{
				yield return AccessTools.Method(typeof(ConfigScene), "Save");
				yield return AccessTools.Method(typeof(ConfigScene), "OnBack");
			}

			private static void Prefix()
			{
				nodeDraw = null;
			}
		}
	}
}
