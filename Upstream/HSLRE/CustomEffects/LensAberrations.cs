using System;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;

namespace HSLRE.CustomEffects
{
	public class LensAberrations1
	{
		public Shader shader
		{
			get
			{
				if (m_Shader == null)
				{
					//m_Shader = HSLRE.self._resources.LoadAsset<Shader>("LensAberrations");
					m_Shader = HSIBL.Res.GetShader("Hidden/LensAberrations");
				}
				return m_Shader;
			}
		}

		public Material material
		{
			get
			{
				if (m_Material == null)
				{
					m_Material = ImageEffectHelper.CheckShaderAndCreateMaterial(shader);
				}
				return m_Material;
			}
		}

		public void OnEnable()
		{
			//if (!ImageEffectHelper.IsSupported(this.shader, false, false, this))
			//{
			//    base.enabled = false;
			//}
			//this.distortion = GraphicSetting.MyDistortionSettings;
			//this.vignette = GraphicSetting.MyVignetteSettings;
			//this.chromaticAberration = GraphicSetting.MyChromaticAberrationSettings;
			m_RTU = new RenderTextureUtility();
		}

		public void OnDisable()
		{
			if (m_Material != null)
			{
				UnityEngine.Object.DestroyImmediate(m_Material);
			}
			m_Material = null;
			m_RTU.ReleaseAllTemporaryRenderTextures();
		}

		public void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (!vignette.enabled && !chromaticAberration.enabled && !distortion.enabled)
			{
				Graphics.Blit(source, destination);
				return;
			}
			material.shaderKeywords = null;
			if (distortion.enabled)
			{
				float val = 1.6f * Math.Max(Mathf.Abs(distortion.amount), 1f);
				float num = 0.017453292f * Math.Min(160f, val);
				float y = 2f * Mathf.Tan(num * 0.5f);
				Vector4 vector = new Vector4(distortion.centerX, distortion.centerY, Mathf.Max(distortion.amountX, 0.0001f), Mathf.Max(distortion.amountY, 0.0001f));
				Vector3 v = new Vector3((distortion.amount >= 0f) ? num : (1f / num), y, 1f / distortion.scale);
				material.EnableKeyword((distortion.amount >= 0f) ? "DISTORT" : "UNDISTORT");
				material.SetVector("_DistCenterScale", vector);
				material.SetVector("_DistAmount", v);
			}
			if (chromaticAberration.enabled)
			{
				material.EnableKeyword("CHROMATIC_ABERRATION");
				Vector4 vector2 = new Vector4(chromaticAberration.color.r, chromaticAberration.color.g, chromaticAberration.color.b, chromaticAberration.amount * 0.001f);
				material.SetVector("_ChromaticAberration", vector2);
			}
			if (vignette.enabled)
			{
				material.SetColor("_VignetteColor", vignette.color);
				if (vignette.blur > 0f)
				{
					int num2 = source.width / 2;
					int num3 = source.height / 2;
					RenderTexture temporaryRenderTexture = m_RTU.GetTemporaryRenderTexture(num2, num3, 0, source.format, FilterMode.Bilinear);
					RenderTexture temporaryRenderTexture2 = m_RTU.GetTemporaryRenderTexture(num2, num3, 0, source.format, FilterMode.Bilinear);
					material.SetVector("_BlurPass", new Vector2(1f / (float)num2, 0f));
					Graphics.Blit(source, temporaryRenderTexture, material, 0);
					if (distortion.enabled)
					{
						material.DisableKeyword("DISTORT");
						material.DisableKeyword("UNDISTORT");
					}
					material.SetVector("_BlurPass", new Vector2(0f, 1f / (float)num3));
					Graphics.Blit(temporaryRenderTexture, temporaryRenderTexture2, material, 0);
					material.SetVector("_BlurPass", new Vector2(1f / (float)num2, 0f));
					Graphics.Blit(temporaryRenderTexture2, temporaryRenderTexture, material, 0);
					material.SetVector("_BlurPass", new Vector2(0f, 1f / (float)num3));
					Graphics.Blit(temporaryRenderTexture, temporaryRenderTexture2, material, 0);
					material.SetTexture("_BlurTex", temporaryRenderTexture2);
					material.SetFloat("_VignetteBlur", vignette.blur * 3f);
					material.EnableKeyword("VIGNETTE_BLUR");
					if (distortion.enabled)
					{
						material.EnableKeyword((distortion.amount >= 0f) ? "DISTORT" : "UNDISTORT");
					}
				}

				if (vignette.desaturate > 0f)
				{
					material.EnableKeyword("VIGNETTE_DESAT");
					material.SetFloat("_VignetteDesat", 1f - vignette.desaturate);
				}
				material.SetVector("_VignetteCenter", vignette.center);
				if (Mathf.Approximately(vignette.roundness, 1f))
				{
					material.EnableKeyword("VIGNETTE_CLASSIC");
					material.SetVector("_VignetteSettings", new Vector2(vignette.intensity, vignette.smoothness));
				}
				else
				{
					material.EnableKeyword("VIGNETTE_FILMIC");
					float z = (1f - vignette.roundness) * 6f + vignette.roundness;
					material.SetVector("_VignetteSettings", new Vector3(vignette.intensity, vignette.smoothness, z));
				}
			}
			int pass = 0;
			if (vignette.enabled && chromaticAberration.enabled && distortion.enabled)
			{
				pass = 7;
			}
			else if (vignette.enabled && chromaticAberration.enabled)
			{
				pass = 5;
			}
			else if (vignette.enabled && distortion.enabled)
			{
				pass = 6;
			}
			else if (chromaticAberration.enabled && distortion.enabled)
			{
				pass = 4;
			}
			else if (vignette.enabled)
			{
				pass = 3;
			}
			else if (chromaticAberration.enabled)
			{
				pass = 1;
			}
			else if (distortion.enabled)
			{
				pass = 2;
			}
			Graphics.Blit(source, destination, material, pass);
			m_RTU.ReleaseAllTemporaryRenderTextures();
		}

		[SettingsGroup]
		public DistortionSettings distortion = DistortionSettings.defaultSettings;

		[SettingsGroup]
		public VignetteSettings vignette = VignetteSettings.defaultSettings;

		[SettingsGroup]
		public ChromaticAberrationSettings chromaticAberration = ChromaticAberrationSettings.defaultSettings;

		[SerializeField]
		private Shader m_Shader;

		private Material m_Material;

		private RenderTextureUtility m_RTU;

		[AttributeUsage(AttributeTargets.Field)]
		public class SettingsGroup : Attribute
		{
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class AdvancedSetting : Attribute
		{
		}

		[Serializable]
		public struct DistortionSettings
		{
			public static DistortionSettings defaultSettings
			{
				get
				{
					return new DistortionSettings
					{
						enabled = false,
						amount = 0f,
						centerX = 0f,
						centerY = 0f,
						amountX = 1f,
						amountY = 1f,
						scale = 1f
					};
				}
			}

			public bool enabled;

			[Range(-100f, 100f)]
			[Tooltip("Distortion amount.")]
			public float amount;

			[Range(-1f, 1f)]
			[Tooltip("Distortion center point (X axis).")]
			public float centerX;

			[Range(-1f, 1f)]
			[Tooltip("Distortion center point (Y axis).")]
			public float centerY;

			[Range(0f, 1f)]
			[Tooltip("Amount multiplier on X axis. Set it to 0 to disable distortion on this axis.")]
			public float amountX;

			[Range(0f, 1f)]
			[Tooltip("Amount multiplier on Y axis. Set it to 0 to disable distortion on this axis.")]
			public float amountY;

			[Range(0.01f, 5f)]
			[Tooltip("Global screen scaling.")]
			public float scale;
		}

		[Serializable]
		public struct VignetteSettings
		{
			public static VignetteSettings defaultSettings
			{
				get
				{
					return new VignetteSettings
					{
						enabled = false,
						color = new Color(0f, 0f, 0f, 1f),
						center = new Vector2(0.5f, 0.5f),
						intensity = 1.4f,
						smoothness = 0.8f,
						roundness = 1f,
						blur = 0f,
						desaturate = 0f
					};
				}
			}

			public bool enabled;

			[ColorUsage(false)]
			[Tooltip("Vignette color. Use the alpha channel for transparency.")]
			public Color color;

			[Tooltip("Sets the vignette center point (screen center is [0.5,0.5]).")]
			public Vector2 center;

			[Range(0f, 3f)]
			[Tooltip("Amount of vignetting on screen.")]
			public float intensity;

			[Range(0.01f, 3f)]
			[Tooltip("Smoothness of the vignette borders.")]
			public float smoothness;

			[AdvancedSetting]
			[Range(0f, 1f)]
			[Tooltip("Lower values will make a square-ish vignette.")]
			public float roundness;

			[Range(0f, 1f)]
			[Tooltip("Blurs the corners of the screen. Leave this at 0 to disable it.")]
			public float blur;

			[Range(0f, 1f)]
			[Tooltip("Desaturate the corners of the screen. Leave this to 0 to disable it.")]
			public float desaturate;
		}

		[Serializable]
		public struct ChromaticAberrationSettings
		{
			public static ChromaticAberrationSettings defaultSettings
			{
				get
				{
					return new ChromaticAberrationSettings
					{
						enabled = false,
						color = Color.green,
						amount = 0f
					};
				}
			}

			public bool enabled;

			[ColorUsage(false)]
			[Tooltip("Channels to apply chromatic aberration to.")]
			public Color color;

			[Range(-50f, 50f)]
			[Tooltip("Amount of tangential distortion.")]
			public float amount;
		}

		private enum Pass
		{
			BlurPrePass,
			Chroma,
			Distort,
			Vignette,
			ChromaDistort,
			ChromaVignette,
			DistortVignette,
			ChromaDistortVignette
		}
	}
}
