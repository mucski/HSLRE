using HSLRE.HSIBL;
using System;
using System.Reflection;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;

namespace HSLRE
{
	public partial class HSLRE
	{
		public class EffectData
		{
			internal bool _enabled;

			public bool enabled
			{
				get { return this._enabled; }
				set
				{
					if (this._enabled == value)
						return;
					this._enabled = value;
					if (this._enabled)
					{
						this.onEnable?.Invoke();
					}
					else
					{
						this.onDisable?.Invoke();
					}
				}
			}

			public object effect;

			public Action onEnable;
			public Action<AntiAliasing> onEnableAA;
			public Action onDisable;

			public Action<RenderTexture, RenderTexture> onRenderImage;
			public Action<Camera, RenderTexture, RenderTexture> onRenderImageWithCamera;

			public EffectType effectType;
			public int originalPosition;
			public string simpleName;

			public void OnRenderImage(RenderTexture src, RenderTexture dest, Camera cam = null)
			{
				onRenderImage?.Invoke(src, dest);
				if (onRenderImageWithCamera != null)
				{
					if (cam)
						onRenderImageWithCamera.Invoke(cam, src, dest);
					else
						Graphics.Blit(src, dest); // perf pass: no camera -> pass the image through, dest was left stale
				}
			}


			public EffectData(object effect, bool enabled, EffectType effectType, bool treatAsMonobehaviour, int originalPosition, string simpleName, int gameEffectKind)
			{
				this.effect = effect;
				this._enabled = enabled;
				this.effectType = effectType;
				this.originalPosition = originalPosition;
				this.simpleName = simpleName;

				MethodInfo methodInfo;
				MonoBehaviour mono = effect as MonoBehaviour;
				if (treatAsMonobehaviour && mono != null)
				{
					this.onEnable = () => { mono.enabled = true; IBLPatches.UpdateGameSettingsUI(true, gameEffectKind); };
					this.onDisable = () => { mono.enabled = false; IBLPatches.UpdateGameSettingsUI(false, gameEffectKind); };
				}
				else
				{
					methodInfo = this.effect.GetType().GetMethod("OnEnable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
					if (methodInfo != null)
					{
						if (methodInfo.GetParameters().Length == 0)
						{
							var action = (Action)Delegate.CreateDelegate(typeof(Action), this.effect, methodInfo);
							this.onEnable = () => { action.Invoke(); IBLPatches.UpdateGameSettingsUI(true, gameEffectKind); };
							//this.onEnable = (Action)Delegate.CreateDelegate(typeof(Action), this.effect, methodInfo);
						}
						else
						{
							var action = (Action<AntiAliasing>)Delegate.CreateDelegate(typeof(Action<AntiAliasing>), this.effect, methodInfo);
							this.onEnableAA = (aa) => { action.Invoke(aa); IBLPatches.UpdateGameSettingsUI(true, gameEffectKind); };
							//this.onEnableAA = (Action<AntiAliasing>)Delegate.CreateDelegate(typeof(Action<AntiAliasing>), this.effect, methodInfo);
						}
					}
					else
					{
						this.onEnable = () => IBLPatches.UpdateGameSettingsUI(true, gameEffectKind);
					}

					methodInfo = this.effect.GetType().GetMethod("OnDisable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
					if (methodInfo != null)
					{
						var action = (Action)Delegate.CreateDelegate(typeof(Action), this.effect, methodInfo);
						this.onDisable = () => { action.Invoke(); IBLPatches.UpdateGameSettingsUI(false, gameEffectKind); };
						//this.onDisable = (Action)Delegate.CreateDelegate(typeof(Action), this.effect, methodInfo);
					}
					else
					{
						this.onDisable = () =>  IBLPatches.UpdateGameSettingsUI(false, gameEffectKind);
					}
				}

				methodInfo = this.effect.GetType().GetMethod("OnRenderImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				if (methodInfo != null)
				{
					if (methodInfo.GetParameters().Length == 2)
					{
						this.onRenderImage = (Action<RenderTexture, RenderTexture>)Delegate.CreateDelegate(typeof(Action<RenderTexture, RenderTexture>), this.effect, methodInfo);
					}
					else
					{
						this.onRenderImageWithCamera = (Action<Camera, RenderTexture, RenderTexture>)Delegate.CreateDelegate(typeof(Action<Camera, RenderTexture, RenderTexture>), this.effect, methodInfo);
					}
				}

				this.onEnable?.Invoke();
				if (!this._enabled && this.onDisable != null)
					this.onDisable();
			}
		}
	}
}
