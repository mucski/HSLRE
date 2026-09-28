using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IllusionInjector;
using IllusionPlugin;
using UnityEngine;

namespace HSLRE.CustomEffects
{
	public class AfterImage
	{
		public const int maxFrames = 128;
		private readonly Material _motionBlurMat;
		// perf pass: newest first; a List with fixed capacity instead of a LinkedList (a node allocated every frame)
		private readonly List<RenderTexture> _frames = new List<RenderTexture>(maxFrames + 1);
		private static readonly int _mixTexId = Shader.PropertyToID("_MixTex");
		private static readonly int _weightId = Shader.PropertyToID("_Weight");
		private readonly float[] _weights = new float[maxFrames];
		private int _framesToMix = 8;
		private float _falloffPower = 10f / 7f;
		private readonly Func<bool> _videoExportIsRecording;

		public int framesToMix
		{
			get { return this._framesToMix; }
			set
			{
				this._framesToMix = Mathf.Clamp(value, 2, maxFrames);
				this.UpdateWeights();
			}
		}

		public float falloffPower
		{
			get { return this._falloffPower; }
			set
			{
				this._falloffPower = value;
				this.UpdateWeights();
			}
		}

		public AfterImage()
		{
			Shader motionBlurShader = HSLRE.self._resources.LoadAsset<Shader>("AfterImage");
			this._motionBlurMat = new Material(motionBlurShader);
			this.UpdateWeights();

			Type t = Type.GetType("VideoExport.VideoExport,VideoExport");
			if (t != null)
			{
				IPlugin videoExport = PluginManager.Plugins.FirstOrDefault(p => p.GetType() == t);
				if (videoExport != null)
				{
					PropertyInfo pi = t.GetProperty("isRecording", BindingFlags.Public | BindingFlags.Instance);
					if (pi != null)
						this._videoExportIsRecording = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), videoExport, pi.GetGetMethod());
				}
			}
		}

		/// <summary>Perf pass: level loads make a new AfterImage; this frees the old one's frames and material.</summary>
		internal void Dispose()
		{
			this.OnDisable();
			if (this._motionBlurMat != null)
				UnityEngine.Object.Destroy(this._motionBlurMat);
		}

		private void OnDisable()
		{
			for (int i = 0; i < this._frames.Count; i++)
				RenderTexture.ReleaseTemporary(this._frames[i]);
			this._frames.Clear();
		}

		private void UpdateWeights()
		{
			float total = 0f;
			for (int i = 0; i < this._framesToMix; i++)
			{
				this._weights[i] = 1f / Mathf.Pow(Mathf.Pow(128, this._falloffPower), i / ((float)this._framesToMix));
				total += this._weights[i];
			}
			for (int i = 0; i < this._framesToMix; i++)
				this._weights[i] = this._weights[i] / total;
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (this._videoExportIsRecording != null && this._videoExportIsRecording() && Camera.main.targetTexture == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			RenderTexture newFrame = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
			Graphics.Blit(source, newFrame);
			this._frames.Insert(0, newFrame);

			while (this._frames.Count > this._framesToMix)
			{
				RenderTexture.ReleaseTemporary(this._frames[this._frames.Count - 1]);
				this._frames.RemoveAt(this._frames.Count - 1);
			}

			RenderTexture mixTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
			RenderTexture mixTex2 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);

			// perf pass: the first frame mixes onto the black texture directly (was a full-screen clear blit first)
			for (int i = 0; i < this._frames.Count; i++)
			{
				this._motionBlurMat.SetTexture(_mixTexId, i == 0 ? (Texture)Texture2D.blackTexture : mixTex);
				this._motionBlurMat.SetFloat(_weightId, this._weights[i]);
				Graphics.Blit(this._frames[i], mixTex2, this._motionBlurMat, 0);

				RenderTexture temp = mixTex;
				mixTex = mixTex2;
				mixTex2 = temp;
			}
			Graphics.Blit(mixTex, destination);

			RenderTexture.ReleaseTemporary(mixTex);
			RenderTexture.ReleaseTemporary(mixTex2);
		}
	}
}
