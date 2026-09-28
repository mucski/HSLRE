using System;
using System.Collections.Generic;
using System.IO;
using HSUIKit;
using UnityEngine;
using UnityEngine.UI;

namespace HSIBL
{
	public partial class HSIBL
	{
		/// <summary>A detail page built directly with kit controls instead of through a routed section.</summary>
		private interface IDetailPage
		{
			void Build(RectTransform parent);
			void Tick();
			void Destroy();
		}

		/// <summary>
		/// Presets in the kit panel: a toolbar (save new, multi-select) above one row per preset. Clicking a row
		/// loads it; its icons overwrite it with the current settings or delete it. Same preset files and the same
		/// save / load / delete code as the original window.
		/// </summary>
		private sealed class PresetsPage : IDetailPage
		{
			private readonly HSIBL _o;
			private RectTransform _root, _list;
			private KitButton _saveNew, _select, _deleteSelected;
			private KitText _empty;
			private string[] _shown;
			private bool _selectMode;
			private readonly HashSet<string> _selected = new HashSet<string>();
			private int _shownSelectedCount = -1;
			private readonly ThumbnailCache _thumbnails = new ThumbnailCache(16f / 9f, 40f, Theme.RadiusSmall);
			private const float ThumbHeight = 40f, ThumbAspect = 16f / 9f;

			internal PresetsPage(HSIBL owner)
			{
				this._o = owner;
			}

			public void Build(RectTransform parent)
			{
				this._root = Kit.Rect(parent, "Presets");
				VerticalLayoutGroup v = this._root.gameObject.AddComponent<VerticalLayoutGroup>();
				v.spacing = 8f;
				v.childForceExpandWidth = true;
				v.childForceExpandHeight = false;
				this._root.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

				RectTransform toolbar = KitLayout.Row(this._root, 6f);
				Kit.Layout(toolbar.gameObject, -1f, 32f);
				this._saveNew = KitButton.Create(toolbar, "Save new…", ButtonStyle.Primary, this.SaveNew, Sprites.Plus);
				Kit.Layout(this._saveNew.gameObject, 124f, 30f);
				KitTooltip.Attach(this._saveNew.gameObject, "Save the current settings as a new preset");
				this._select = KitButton.Create(toolbar, "Select", ButtonStyle.Ghost, this.ToggleSelectMode);
				Kit.Layout(this._select.gameObject, 84f, 30f);
				KitTooltip.Attach(this._select.gameObject, "Select several presets to delete them together");
				KitLayout.Flexible(toolbar);
				this._deleteSelected = KitButton.Create(toolbar, "Delete", ButtonStyle.Danger, this.DeleteSelected, KitPanelIcons.Trash);
				Kit.Layout(this._deleteSelected.gameObject, 120f, 30f);
				this._deleteSelected.gameObject.SetActive(false);

				this._list = Kit.Rect(this._root, "List");
				VerticalLayoutGroup lv = this._list.gameObject.AddComponent<VerticalLayoutGroup>();
				lv.spacing = 2f;
				lv.childForceExpandWidth = true;
				lv.childForceExpandHeight = false;

				this._empty = KitText.Create(this._root, "No presets yet. Use \"Save new…\" to keep the current settings as one.", TextRole.Dim, true);

				if (_isStudio == false)
				{
					KitLayout.Space(this._root, 6f);
					KitButton maker = KitButton.Create(this._root, "Use the current cubemap as the chara maker default", ButtonStyle.Secondary, () =>
					{
						this._o._defaultCharaMakerCubemap = this._o._selectedCubemap == -1 ? "" : this._o._cubeMapFileNames[this._o._selectedCubemap];
						KitToast.Show("HSLRE", this._o._defaultCharaMakerCubemap.Length == 0 ? "The chara maker will start without a cubemap." : "The chara maker will start with \"" + this._o._defaultCharaMakerCubemap + "\".", ToastKind.Success, 2.5f);
					});
					Kit.Layout(maker.gameObject, -1f, 30f);
				}
				this.Rebuild();
			}

			public void Tick()
			{
				if (ReferenceEquals(this._o._presets, this._shown) == false)
					this.Rebuild();
				if (this._selected.Count != this._shownSelectedCount)
				{
					this._shownSelectedCount = this._selected.Count;
					this._deleteSelected.Label = this._selected.Count == 0 ? "Delete" : "Delete (" + this._selected.Count + ")";
					this._deleteSelected.Interactable = this._selected.Count != 0;
				}
			}

			public void Destroy()
			{
				if (this._root != null)
				{
					this._root.gameObject.SetActive(false);
					UnityEngine.Object.Destroy(this._root.gameObject);
				}
				this._thumbnails.Clear();
			}

			// ------------------------------------------------------------------ list

			private void Rebuild()
			{
				this._shown = this._o._presets;
				for (int i = this._list.childCount - 1; i >= 0; i--)
				{
					GameObject child = this._list.GetChild(i).gameObject;
					child.SetActive(false);
					UnityEngine.Object.Destroy(child);
				}
				this._selected.RemoveWhere(n => Array.IndexOf(this._shown, n) == -1);
				foreach (string preset in this._shown)
					this.BuildRow(preset);
				this._empty.gameObject.SetActive(this._shown.Length == 0);
				this._select.Interactable = this._shown.Length != 0;
				if (this._shown.Length == 0 && this._selectMode)
					this.ToggleSelectMode();
			}

			private void BuildRow(string preset)
			{
				RectTransform row = Kit.Rect(this._list, "Preset " + preset);
				Kit.Layout(row.gameObject, -1f, ThumbHeight + 8f);
				Image bg = Kit.Image(row, "Bg", Sprites.Rounded(Theme.RadiusSmall), Kit.Alpha(Theme.Surface, 0.6f), true);
				Kit.Fill(bg.rectTransform);
				Kit.IgnoreLayout(bg.gameObject);
				HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
				h.padding = new RectOffset(4, 6, 0, 0);
				h.spacing = 8f;
				h.childAlignment = TextAnchor.MiddleLeft;
				h.childForceExpandWidth = false;
				h.childForceExpandHeight = false;

				KitCheckbox check = null;
				if (this._selectMode)
				{
					check = KitCheckbox.Create(row, "", this._selected.Contains(preset), on =>
					{
						if (on)
							this._selected.Add(preset);
						else
							this._selected.Remove(preset);
					});
					Kit.Layout(check.gameObject, 26f, 26f);
				}

				// thumbnail (a dim placeholder for presets saved before thumbnails existed)
				// every frame is the same 16:9 rounded box; the cache crops the picture to it and rounds its corners
				// with a soft alpha edge (a stencil Mask clips hard, which looked pixelated)
				Texture2D thumb = this._thumbnails.Get(preset);
				RectTransform frame = Kit.Rect(row, "Thumb");
				Kit.Layout(frame.gameObject, Mathf.Round(ThumbHeight * ThumbAspect), ThumbHeight);
				Image frameBg = frame.gameObject.AddComponent<Image>();
				Kit.SetSprite(frameBg, Sprites.Rounded(Theme.RadiusSmall));
				frameBg.color = Kit.Alpha(Color.black, 0.35f);
				frameBg.raycastTarget = false;
				if (thumb != null)
				{
					RawImage img = new GameObject("Image", typeof(RectTransform)).AddComponent<RawImage>();
					img.transform.SetParent(frame, false);
					img.texture = thumb;
					img.raycastTarget = false;
					Kit.Fill(img.rectTransform);
				}

				Text name = Kit.MakeText(row, "Name", preset, Theme.FontSize, Theme.Text, TextAnchor.MiddleLeft);
				LayoutElement nl = Kit.Layout(name.gameObject, 0f, 22f);
				nl.flexibleWidth = 1f;
				name.horizontalOverflow = HorizontalWrapMode.Wrap;
				name.verticalOverflow = VerticalWrapMode.Truncate;

				if (this._selectMode == false)
				{
					KitIconButton overwrite = KitIconButton.Create(row, KitPanelIcons.Save, 26f, false, () => this.Overwrite(preset));
					KitTooltip.Attach(overwrite.gameObject, "Overwrite with the current settings");
					KitIconButton delete = KitIconButton.Create(row, KitPanelIcons.Trash, 26f, true, () => this.Delete(preset));
					KitTooltip.Attach(delete.gameObject, "Delete");
				}

				KitHoverClick hover = KitHoverClick.On(bg.gameObject);
				hover.OnEnter = () => Tween.Tint(bg, Theme.SurfaceHover, 0.1f, Ease.OutQuad);
				hover.OnExit = () => Tween.Tint(bg, Kit.Alpha(Theme.Surface, 0.6f), 0.18f, Ease.OutQuad);
				hover.OnClick = () =>
				{
					if (this._selectMode)
					{
						if (check != null)
							check.SetValue(!check.Value, true, true);
						return;
					}
					this._o.LoadPreset(preset + ".xml");
					KitToast.Show("HSLRE", "Loaded preset \"" + preset + "\".", ToastKind.Success, 2f);
				};
				KitTooltip.Attach(bg.gameObject, this._selectMode ? "Click to select" : "Click to load this preset");
			}

			// ------------------------------------------------------------------ actions

			private void SaveNew()
			{
				KitModal.Prompt("Save preset", "Saves every current HSLRE setting under this name.", "", "Preset name", "Save", text =>
				{
					if (text == null)
						return;
					string name = CleanName(text);
					if (name.Length == 0)
					{
						KitToast.Show("HSLRE", "A preset needs a name.", ToastKind.Warning, 2f);
						return;
					}
					if (Array.Exists(this._o._presets, p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase)))
					{
						KitModal.Show("Overwrite preset?", "A preset named \"" + name + "\" already exists. Replace it with the current settings?", new[] { "Cancel", "Overwrite" }, r =>
						{
							if (r == 1)
								this.Save(name);
						}, true);
						return;
					}
					this.Save(name);
				});
			}

			private void Overwrite(string preset)
			{
				KitModal.Show("Overwrite preset?", "Replace \"" + preset + "\" with the current settings?", new[] { "Cancel", "Overwrite" }, r =>
				{
					if (r == 1)
						this.Save(preset);
				}, true);
			}

			private void Save(string name)
			{
				this._o.SavePreset(name + ".xml");
				this._o.SavePresetThumbnail(name);
				this._o.RefreshPresetList();
				KitToast.Show("HSLRE", "Saved preset \"" + name + "\".", ToastKind.Success, 2f);
			}

			private void Delete(string preset)
			{
				KitModal.Show("Delete preset?", "Delete \"" + preset + "\"? This removes the file.", new[] { "Cancel", "Delete" }, r =>
				{
					if (r == 1)
						this._o.DeletePreset(preset + ".xml");
				}, true);
			}

			private void ToggleSelectMode()
			{
				this._selectMode = !this._selectMode;
				this._selected.Clear();
				this._select.Label = this._selectMode ? "Cancel" : "Select";
				this._select.Style = this._selectMode ? ButtonStyle.Secondary : ButtonStyle.Ghost;
				this._saveNew.gameObject.SetActive(this._selectMode == false);
				this._deleteSelected.gameObject.SetActive(this._selectMode);
				this._shownSelectedCount = -1;
				this._shown = null; // rebuild the rows with / without checkboxes
			}

			private void DeleteSelected()
			{
				if (this._selected.Count == 0)
					return;
				List<string> names = new List<string>(this._selected);
				string what = names.Count == 1 ? "\"" + names[0] + "\"" : names.Count + " presets";
				KitModal.Show("Delete presets?", "Delete " + what + "? This removes the files.", new[] { "Cancel", "Delete" }, r =>
				{
					if (r != 1)
						return;
					foreach (string n in names)
						this._o.DeletePreset(n + ".xml");
					this.ToggleSelectMode();
				}, true);
			}

			/// <summary>Same clean-up as the original window: trimmed, invalid file name characters become "_".</summary>
			private static string CleanName(string text)
			{
				string name = text.Trim();
				return string.Join("_", name.Split(Path.GetInvalidFileNameChars())).Trim();
			}
		}
	}
}
