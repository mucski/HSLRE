using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ToolBox.Extensions;
using UnityEngine;

namespace HSLRE.HSIBL
{
	internal static class Res
	{
		static readonly Dictionary<string, Texture2D> texDic = new Dictionary<string, Texture2D>();
		static readonly Dictionary<string, Shader> shadersDic = GetShaders();

		/*public static void Init()
		{
			UnityEngine.Debug.Log($"Loaded shaders:\r\n{string.Join("\r\n", shadersDic.Keys.ToArray())}");
			UnityEngine.Debug.Log($"Loaded textures:\r\n{string.Join("\r\n", texDic.Keys.ToArray())}");
		}*/

		static Dictionary<string, Shader> GetShaders()
		{
			var ab = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("HSLRE.Resources.postprocessingeffects_resource"));
			var dic = new Dictionary<string, Shader>();

			foreach (var t in ab.LoadAllAssets<Texture2D>())
			{
				texDic.Add(t.name, t);
			}
			foreach (var mat in ab.LoadAllAssets<Material>())
			{
				dic.Add(mat.shader.name, mat.shader);
			}
			ab.Unload(false);

			return dic;
		}

		public static Shader GetShader(string name) => shadersDic[name];

		public static Texture2D GetTexture(string name) => texDic[name];
	}
}
