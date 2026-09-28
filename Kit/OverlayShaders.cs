using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace HSIBL
{
	/// <summary>
	/// Shaders for the screen overlays (addition), from the hslreoverlays bundle embedded in the DLL
	/// (built by HSModernShadersUnity's BuildLREOverlays).
	/// </summary>
	internal static class OverlayShaders
	{
		private static bool _loaded;
		private static readonly Dictionary<string, Shader> _byName = new Dictionary<string, Shader>();

		/// <summary>The overlay shader "Hidden/HSLRE/Overlay/" + name, or null when missing or unsupported.</summary>
		internal static Shader Get(string name)
		{
			Load();
			Shader s;
			return _byName.TryGetValue(name, out s) ? s : null;
		}

		internal static void Load()
		{
			if (_loaded)
				return;
			_loaded = true;
			try
			{
				byte[] data;
				using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("HSLRE.Resources.hslreoverlays"))
				{
					if (s == null)
					{
						Debug.LogError("HSLRE overlays: the shader bundle is not embedded in the DLL.");
						return;
					}
					data = new byte[s.Length];
					s.Read(data, 0, data.Length);
				}
				AssetBundle bundle = AssetBundle.LoadFromMemory(data);
				if (bundle == null)
				{
					Debug.LogError("HSLRE overlays: could not load the shader bundle.");
					return;
				}
				const string prefix = "Hidden/HSLRE/Overlay/";
				foreach (Shader shader in bundle.LoadAllAssets<Shader>())
				{
					if (shader.isSupported == false)
					{
						Debug.LogWarning("HSLRE overlays: '" + shader.name + "' is not supported on this GPU, that overlay is disabled.");
						continue;
					}
					if (shader.name.StartsWith(prefix))
						_byName[shader.name.Substring(prefix.Length)] = shader;
				}
				bundle.Unload(false);
			}
			catch (Exception e)
			{
				Debug.LogError("HSLRE overlays: loading the shaders failed.\n" + e);
			}
		}
	}
}
