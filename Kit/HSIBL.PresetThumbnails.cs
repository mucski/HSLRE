using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HSIBL
{
	public partial class HSIBL
	{
		// Preset thumbnails (addition): a small picture saved next to each preset file as "<name>.png", taken
		// from the main camera (so with every effect, but without any UI) when the preset is saved.
		private const int ThumbnailWidth = 320;

		internal static string ThumbnailPath(string presetName)
		{
			return Path.Combine(_presetFolder, presetName + ".png");
		}

		/// <summary>Renders the main camera once and saves a small PNG for the preset. Never throws.</summary>
		internal void SavePresetThumbnail(string presetName)
		{
			Camera c = this.cam;
			if (c == null)
				return;
			RenderTexture big = null, half = null, small = null;
			RenderTexture previousTarget = c.targetTexture, previousActive = RenderTexture.active;
			Texture2D tex = null;
			try
			{
				float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
				int w = ThumbnailWidth * 3, h = Mathf.Max(1, Mathf.RoundToInt(w / aspect));
				big = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
				c.targetTexture = big;
				c.Render();
				c.targetTexture = previousTarget;

				// two halving-ish steps keep the downscale from aliasing
				half = RenderTexture.GetTemporary(w / 2, h / 2, 0, RenderTextureFormat.ARGB32);
				Graphics.Blit(big, half);
				int tw = ThumbnailWidth, th = Mathf.Max(1, Mathf.RoundToInt(tw / aspect));
				small = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32);
				Graphics.Blit(half, small);

				RenderTexture.active = small;
				tex = new Texture2D(tw, th, TextureFormat.RGB24, false);
				tex.ReadPixels(new Rect(0, 0, tw, th), 0, 0);
				tex.Apply(false);
				RenderTexture.active = previousActive;

				if (Directory.Exists(_presetFolder) == false)
					Directory.CreateDirectory(_presetFolder);
				File.WriteAllBytes(ThumbnailPath(presetName), tex.EncodeToPNG());
			}
			catch (Exception e)
			{
				Debug.LogError("HSLRE: could not save the preset thumbnail.\n" + e);
			}
			finally
			{
				c.targetTexture = previousTarget;
				RenderTexture.active = previousActive;
				if (big != null)
					RenderTexture.ReleaseTemporary(big);
				if (half != null)
					RenderTexture.ReleaseTemporary(half);
				if (small != null)
					RenderTexture.ReleaseTemporary(small);
				if (tex != null)
					Destroy(tex);
			}
		}

		/// <summary>
		/// Loaded thumbnails, reloaded when the file changes. Owned by the presets page. Each is cropped to the
		/// shown box's aspect and gets rounded corners with a one-pixel soft edge baked into its alpha (the kit's
		/// own shapes are anti-aliased the same way), plus mipmaps so the small display stays smooth.
		/// </summary>
		private sealed class ThumbnailCache
		{
			private readonly Dictionary<string, KeyValuePair<DateTime, Texture2D>> _loaded = new Dictionary<string, KeyValuePair<DateTime, Texture2D>>(StringComparer.OrdinalIgnoreCase);
			private readonly float _boxAspect, _boxHeight, _radius;

			/// <param name="boxHeight">Shown height in UI units; with <paramref name="radius"/> it sets the corner size.</param>
			internal ThumbnailCache(float boxAspect, float boxHeight, float radius)
			{
				this._boxAspect = boxAspect;
				this._boxHeight = boxHeight;
				this._radius = radius;
			}

			/// <summary>The preset's thumbnail, or null when it has none.</summary>
			internal Texture2D Get(string presetName)
			{
				string path = ThumbnailPath(presetName);
				KeyValuePair<DateTime, Texture2D> entry;
				bool cached = this._loaded.TryGetValue(presetName, out entry);
				if (File.Exists(path) == false)
				{
					if (cached)
						this.Forget(presetName);
					return null;
				}
				DateTime written = File.GetLastWriteTimeUtc(path);
				if (cached && entry.Key == written)
					return entry.Value;
				if (cached)
					this.Forget(presetName);
				Texture2D file = new Texture2D(2, 2, TextureFormat.RGB24, false);
				Texture2D tex;
				try
				{
					if (file.LoadImage(File.ReadAllBytes(path)) == false)
						return null;
					tex = this.Bake(file);
				}
				catch (Exception)
				{
					return null;
				}
				finally
				{
					UnityEngine.Object.Destroy(file);
				}
				this._loaded[presetName] = new KeyValuePair<DateTime, Texture2D>(written, tex);
				return tex;
			}

			/// <summary>Centre crop to the box aspect, rounded-corner alpha, mipmaps.</summary>
			private Texture2D Bake(Texture2D file)
			{
				int sw = file.width, sh = file.height;
				int w = sw, h = sh;
				if ((float)sw / sh > this._boxAspect)
					w = Mathf.Max(1, Mathf.RoundToInt(sh * this._boxAspect));
				else
					h = Mathf.Max(1, Mathf.RoundToInt(sw / this._boxAspect));
				int x0 = (sw - w) / 2, y0 = (sh - h) / 2;
				Color32[] src = file.GetPixels32();
				Color32[] dst = new Color32[w * h];
				float texelsPerUnit = h / this._boxHeight;
				float r = this._radius * texelsPerUnit;
				float edge = Mathf.Max(1f, texelsPerUnit); // one shown pixel of soft edge
				float hx = w * 0.5f, hy = h * 0.5f;
				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++)
					{
						Color32 c = src[(y + y0) * sw + x + x0];
						// signed distance to the rounded rectangle, sampled at the texel centre
						float qx = Mathf.Abs(x + 0.5f - hx) - (hx - r), qy = Mathf.Abs(y + 0.5f - hy) - (hy - r);
						float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
						c.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - outside / edge) * 255f);
						dst[y * w + x] = c;
					}
				Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
				tex.wrapMode = TextureWrapMode.Clamp;
				tex.filterMode = FilterMode.Trilinear;
				tex.SetPixels32(dst);
				tex.Apply(true, true);
				return tex;
			}

			private void Forget(string presetName)
			{
				KeyValuePair<DateTime, Texture2D> entry;
				if (this._loaded.TryGetValue(presetName, out entry))
				{
					if (entry.Value != null)
						UnityEngine.Object.Destroy(entry.Value);
					this._loaded.Remove(presetName);
				}
			}

			internal void Clear()
			{
				foreach (KeyValuePair<DateTime, Texture2D> entry in this._loaded.Values)
					if (entry.Value != null)
						UnityEngine.Object.Destroy(entry.Value);
				this._loaded.Clear();
			}
		}
	}
}
