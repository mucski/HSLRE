using System;
using System.Collections.Generic;
using HSUIKit;
using IllusionPlugin;
using ToolBox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static HSLRE.HSLRE;
using Settings = HSLRE.Settings;

namespace HSIBL
{
	public partial class HSIBL
	{
		private bool _useKitUI;
		private KitPanel _kitPanel;
		private string[] _cubeMapNamesWithNone;
		private ColorLooks _colorLooks;
		private ScreenOverlays _screenOverlays;
		private AnimatedSky _animatedSky;
		private AnimatedSky _skyBackdrop;

		/// <summary>
		/// The HSUIKit interface: a list of everything HSLRE can set (scene lighting, camera, every effect in
		/// render order, presets) on the left, the selected entry's settings on the right. The settings are the
		/// same section code the original window runs, drawn through KitBackend.
		/// </summary>
		private sealed class KitPanel
		{
			private const float ListWidth = 290f;
			private static readonly Color OpaqueColor = new Color(0.35f, 0.85f, 0.95f);
			private static readonly Color HdrColor = new Color(0.95f, 0.85f, 0.3f);
			private static readonly Color LdrColor = new Color(0.95f, 0.55f, 0.25f);

			private readonly HSIBL _o;
			private readonly KitWindow _win;
			private readonly KitScroll _listScroll, _detailScroll;
			private readonly KitBackend _backend;
			private readonly HeadlessBackend _headless = new HeadlessBackend();
			private readonly KitText _detailTitle, _detailSubtitle;
			private readonly KitSegmented _filter;
			private readonly KitIconButton _measure;
			private static KitPanel _current; // for the Studio shortcut patch
			private bool _trackPress;
			private int _recordUndoIn;
			private bool _measureShown;
			// Bottom filters: All / On / Off by switch state, and the legend dots show or hide a render stage.
			// An effect switched while a state filter is active stays listed until a filter changes, so its row
			// (and open page) doesn't vanish under the mouse.
			private const int StageOpaque = 1, StageHdr = 2, StageLdr = 4;
			private int _stateFilter; // 0 all, 1 on, 2 off
			private int _hiddenStages;
			private readonly HashSet<string> _keepListed = new HashSet<string>();
			private readonly List<KeyValuePair<int, CanvasGroup>> _legendItems = new List<KeyValuePair<int, CanvasGroup>>();
			private readonly KitButton _compare, _snapshot;
			private readonly CanvasGroup _listGroup, _detailGroup;
			// per-effect actions strip at the top of the settings pane (only for effects)
			private readonly RectTransform _actions, _detailArea;
			private readonly KitButton _resetEffect;
			private readonly KitIconButton _pin;
			// favourites: pinned entry ids, in the order they were pinned (kept in modprefs)
			private readonly List<string> _pinned = new List<string>();
			private readonly List<Entry> _entries = new List<Entry>();
			private readonly List<RowView> _rows = new List<RowView>();
			private PresetsPage _presetsPage;
			private IDetailPage _shownPage;
			private string _search = "";
			private Entry _selected;
			private string _selectedId = "skybox";
			private int _listSignature;
			private bool _opening;

			private sealed class Entry
			{
				internal string Id, Name, Group, Number, Stage;
				internal EffectData Data;
				internal Action Draw;
				internal IDetailPage Page;
				internal Action HeadlessDraw;
				internal bool HasSwitch, Reorderable;
				internal int GeneralIndex = -1;
				internal Color Accent = Color.clear;
			}

			internal KitPanel(HSIBL owner)
			{
				this._o = owner;
				_current = this;
				foreach (string id in ModPrefs.GetString("HSIBL", "Favourites", "", true).Split('|'))
					if (id.Length != 0 && this._pinned.Contains(id) == false)
						this._pinned.Add(id);
				this._win = KitWindow.Create("HSLRE", 960f, 660f)
					.Resizable(new Vector2(720f, 420f), new Vector2(3000f, 2400f))
					.Remember(_isStudio ? "HSLRE" : "HSLRE.Game");
				this._win.Rect.gameObject.SetActive(false);
				this._win.VisibilityChanged += v =>
				{
					if (v == false)
					{
						_showUI = false;
						this.SetCompare(false); // never leave the snapshot applied with the window closed
					}
					else
					{
						this._o.TakeSnapshot(); // "before" = how it looked when the window was opened
						if (this._o.HasUndoHistory == false)
							this._o.ResetUndo();
					}
					KitToolbarButton.SetOn(v);
				};

				RectTransform content = this._win.Content;

				// ---------------------------------------------------------------- left: the list
				RectTransform left = Kit.Rect(content, "List");
				left.anchorMin = new Vector2(0f, 0f);
				left.anchorMax = new Vector2(0f, 1f);
				left.pivot = new Vector2(0f, 0.5f);
				left.offsetMin = new Vector2(0f, 0f);
				left.offsetMax = new Vector2(ListWidth, 0f);
				Image leftBg = Kit.Image(left, "Bg", null, new Color(0f, 0f, 0f, 0.16f));
				Kit.Fill(leftBg.rectTransform);

				RectTransform top = Kit.Rect(left, "Top");
				top.anchorMin = new Vector2(0f, 1f);
				top.anchorMax = new Vector2(1f, 1f);
				top.pivot = new Vector2(0.5f, 1f);
				top.sizeDelta = new Vector2(0f, 48f);
				top.anchoredPosition = Vector2.zero;
				VerticalLayoutGroup tv = top.gameObject.AddComponent<VerticalLayoutGroup>();
				tv.padding = new RectOffset(12, 12, 10, 4);
				tv.spacing = 6f;
				tv.childForceExpandWidth = true;
				tv.childForceExpandHeight = false;
				RectTransform searchRow = KitLayout.Row(top, 6f);
				Kit.Layout(searchRow.gameObject, -1f, 30f);
				KitInput search = KitInput.Create(searchRow, "Search settings…", "", s =>
				{
					this._search = s ?? "";
					this._listSignature = 0;
				});
				Kit.Layout(search.gameObject, -1f, 30f).flexibleWidth = 1f;
				this._measure = KitIconButton.Create(searchRow, KitPanelIcons.Stopwatch, 30f, false, this.MeasureCosts);
				KitTooltip.Attach(this._measure.gameObject, "Measure how many milliseconds each effect costs. Takes about a second; the frame rate dips while it measures. SSR and Volumetric Lights run outside the effect chain and can't be timed.");

				RectTransform listArea = Kit.Rect(left, "ListArea");
				listArea.anchorMin = Vector2.zero;
				listArea.anchorMax = Vector2.one;
				listArea.offsetMin = new Vector2(0f, BottomHeight);
				listArea.offsetMax = new Vector2(0f, -48f);
				this._listScroll = KitScroll.Create(listArea, 6, 4, 2f);
				Kit.Fill((RectTransform)this._listScroll.transform);
				// the effect filters sit in the list, right under the Effects heading (placed by RebuildList)
				this._filter = KitSegmented.Create(this._listScroll.Content, new[] { "4K", "LRE", "Both" }, Mathf.Clamp((int)this._o._effectTypeDisplay - 1, 0, 2), i =>
				{
					this._o._effectTypeDisplay = (EffectType)(i + 1);
					this._listSignature = 0;
				});
				KitTooltip.Attach(this._filter.gameObject, "Which effects to list: the 4K ones (Studio's own), the LRE ones, or both");

				RectTransform bottom = Kit.Rect(left, "Legend");
				bottom.anchorMin = new Vector2(0f, 0f);
				bottom.anchorMax = new Vector2(1f, 0f);
				bottom.pivot = new Vector2(0.5f, 0f);
				bottom.sizeDelta = new Vector2(0f, BottomHeight);
				bottom.anchoredPosition = Vector2.zero;
				VerticalLayoutGroup bv = bottom.gameObject.AddComponent<VerticalLayoutGroup>();
				bv.padding = new RectOffset(12, 10, 8, 6);
				bv.spacing = 4f;
				bv.childForceExpandWidth = true;
				bv.childForceExpandHeight = false;

				KitSegmented stateFilter = KitSegmented.Create(bottom, new[] { "All", "On", "Off" }, 0, i =>
				{
					this._stateFilter = i;
					this.FiltersChanged();
				});
				Kit.Layout(stateFilter.gameObject, -1f, 28f);
				KitTooltip.Attach(stateFilter.gameObject, "List all effects, only the ones switched on, or only the ones switched off");

				RectTransform legendRow = KitLayout.Row(bottom, 8f);
				Kit.Layout(legendRow.gameObject, -1f, 30f);
				this.Legend(legendRow, StageOpaque, OpaqueColor, "Opaque", "Applied to opaque objects only, before transparent ones are drawn");
				this.Legend(legendRow, StageHdr, HdrColor, "HDR", "Applied to everything, before tonemapping (HDR)");
				this.Legend(legendRow, StageLdr, LdrColor, "LDR", "Applied to everything, after tonemapping (LDR)");
				KitLayout.Flexible(legendRow);
				KitButton resetOrder = KitButton.Create(legendRow, "Reset order", ButtonStyle.Ghost, () =>
				{
					self.ResetOrder();
					this._listSignature = 0;
				});
				Kit.Layout(resetOrder.gameObject, 92f, 28f);
				KitTooltip.Attach(resetOrder.gameObject, "Put the effects back in their default render order");

				// ---------------------------------------------------------------- right: the selected entry
				RectTransform right = Kit.Rect(content, "Detail");
				right.anchorMin = Vector2.zero;
				right.anchorMax = Vector2.one;
				right.offsetMin = new Vector2(ListWidth, 0f);
				right.offsetMax = Vector2.zero;

				RectTransform header = Kit.Rect(right, "Header");
				header.anchorMin = new Vector2(0f, 1f);
				header.anchorMax = new Vector2(1f, 1f);
				header.pivot = new Vector2(0.5f, 1f);
				header.sizeDelta = new Vector2(0f, 62f);
				header.anchoredPosition = Vector2.zero;
				VerticalLayoutGroup hv = header.gameObject.AddComponent<VerticalLayoutGroup>();
				hv.padding = new RectOffset(20, 300, 12, 4); // room for the snapshot / compare buttons on the right
				hv.spacing = 2f;
				hv.childForceExpandWidth = true;
				hv.childForceExpandHeight = false;
				this._detailTitle = KitText.Create(header, "", TextRole.Header, false);
				this._detailSubtitle = KitText.Create(header, "", TextRole.Dim, false);

				RectTransform detailArea = Kit.Rect(right, "DetailArea");
				detailArea.anchorMin = Vector2.zero;
				detailArea.anchorMax = Vector2.one;
				detailArea.offsetMin = Vector2.zero;
				detailArea.offsetMax = new Vector2(0f, -HeaderHeight);
				this._detailArea = detailArea;

				this._actions = Kit.Rect(right, "Actions");
				this._actions.anchorMin = new Vector2(0f, 1f);
				this._actions.anchorMax = new Vector2(1f, 1f);
				this._actions.pivot = new Vector2(0.5f, 1f);
				this._actions.sizeDelta = new Vector2(0f, ActionsHeight);
				this._actions.anchoredPosition = new Vector2(0f, -HeaderHeight);
				HorizontalLayoutGroup ah = this._actions.gameObject.AddComponent<HorizontalLayoutGroup>();
				ah.padding = new RectOffset(20, 20, 2, 4);
				ah.spacing = 8f;
				ah.childAlignment = TextAnchor.MiddleRight;
				ah.childForceExpandWidth = false;
				ah.childForceExpandHeight = false;
				this._pin = KitIconButton.Create(this._actions, KitPanelIcons.Star, 28f, false, this.TogglePin);
				this._resetEffect = KitButton.Create(this._actions, "Reset to defaults", ButtonStyle.Ghost, this.ConfirmResetEffect, Sprites.Reset);
				Kit.Layout(this._resetEffect.gameObject, -1f, 28f);
				KitTooltip.Attach(this._resetEffect.gameObject, "Puts this effect's settings back to how they were when the game started. Whether it is on stays as it is.");
				this._actions.gameObject.SetActive(false);
				this._detailScroll = KitScroll.Create(detailArea, 20, 10, 8f);
				Kit.Fill((RectTransform)this._detailScroll.transform);
				this._backend = new KitBackend(this._detailScroll.Content);

				// before/after compare: "before" is a snapshot of every setting (taken when the window opens, or
				// with the Snapshot button); Compare swaps it in, clicking again brings the current settings back
				this._compare = KitButton.Create(right, "Compare", ButtonStyle.Secondary, () => this.SetCompare(!this._o.ShowingSnapshot));
				PlaceTopRight(this._compare.Rect, 160f, -20f);
				KitTooltip.Attach(this._compare.gameObject, "Switches between your current settings and the snapshot (how things looked when you opened this window, or when you last clicked Snapshot). Click again to go back.");
				this._snapshot = KitButton.Create(right, "Snapshot", ButtonStyle.Ghost, () =>
				{
					this._o.TakeSnapshot();
					this._snapshot.Label = "Snapshot saved";
					this._snapshotLabelUntil = Time.unscaledTime + 1.2f;
				});
				PlaceTopRight(this._snapshot.Rect, 110f, -188f);
				KitTooltip.Attach(this._snapshot.gameObject, "Remembers the current settings as the 'before' that Compare switches to.");

				// while the snapshot is shown the settings are locked: edits there would be lost on switching back
				this._listGroup = left.gameObject.AddComponent<CanvasGroup>();
				this._detailGroup = detailArea.gameObject.AddComponent<CanvasGroup>();

				if (_isStudio)
					KitToolbarButton.Install(() => _showUI = !_showUI);
			}

			private const float BottomHeight = 80f;
			private const float HeaderHeight = 62f, ActionsHeight = 34f;

			// ------------------------------------------------------------------ favourites

			private bool IsPinned(Entry e) => e != null && this._pinned.Contains(e.Id);

			private void TogglePin()
			{
				Entry e = this._selected;
				if (e == null)
					return;
				if (this._pinned.Remove(e.Id) == false)
					this._pinned.Add(e.Id);
				ModPrefs.SetString("HSIBL", "Favourites", string.Join("|", this._pinned.ToArray()));
				this._listSignature = 0;
				this.StylePin();
			}

			private void StylePin()
			{
				if (this._pin == null)
					return;
				bool pinned = this.IsPinned(this._selected);
				this._pin.Icon.color = pinned ? Theme.Accent : Kit.Alpha(Theme.TextDim, 0.7f);
				KitTooltip tip = this._pin.GetComponent<KitTooltip>() ?? KitTooltip.Attach(this._pin.gameObject, "");
				tip.Text = pinned ? "Unpin: move it back to its usual place in the list" : "Pin to the top of the list";
			}

			/// <summary>Pinned entries leave their group and go, in pin order, into a "Pinned" group at the very top.</summary>
			private void MovePinnedToTop()
			{
				if (this._pinned.Count == 0)
					return;
				List<Entry> pinned = new List<Entry>();
				foreach (string id in this._pinned)
				{
					Entry e = this._entries.Find(x => x.Id == id);
					if (e == null)
						continue;
					this._entries.Remove(e);
					if (e.Stage == null)
						e.Stage = e.Group; // the detail header keeps saying where it usually lives
					e.Group = "Pinned";
					e.Reorderable = false; // render order is changed in the Effects list
					pinned.Add(e);
				}
				this._entries.InsertRange(0, pinned);
			}

			private void ConfirmResetEffect()
			{
				Entry e = this._selected;
				if (e == null || e.Data == null)
					return;
				KitModal.Show("Reset " + e.Name, "Put all of " + e.Name + "'s settings back to their defaults?", new[] { "Reset", "Cancel" }, result =>
				{
					if (result == 0 && this._selected == e)
						this._o.ResetEffect(e.Data);
				});
			}

			/// <summary>The actions strip is only there for effects that have defaults to go back to.</summary>
			private void UpdateActions(Entry e)
			{
				bool show = e != null;
				this._actions.gameObject.SetActive(show);
				this._resetEffect.gameObject.SetActive(e != null && e.Data != null && this._o.CanResetEffect(e.Data));
				this.StylePin();
				this._detailArea.offsetMax = new Vector2(0f, -(HeaderHeight + (show ? ActionsHeight : 0f)));
			}

			/// <summary>A legend dot + name; clicking it hides or shows that render stage's effects in the list.</summary>
			private void Legend(RectTransform parent, int stage, Color color, string text, string tooltip)
			{
				RectTransform item = KitLayout.Row(parent, 4f);
				Image dot = Kit.Image(item, "Dot", Sprites.Circle, color);
				Kit.Layout(dot.gameObject, 8f, 8f);
				Text t = Kit.MakeText(item, "Text", text, Theme.SmallSize, Theme.TextDim, TextAnchor.MiddleLeft);
				Kit.Layout(t.gameObject, t.preferredWidth + 2f, 20f);
				Kit.HitArea(item.gameObject);
				KitTooltip.Attach(item.gameObject, tooltip + ". Click to hide or show these effects.");
				this._legendItems.Add(new KeyValuePair<int, CanvasGroup>(stage, item.gameObject.AddComponent<CanvasGroup>()));
				item.gameObject.AddComponent<ClickRelay>().Clicked = () =>
				{
					this._hiddenStages ^= stage;
					foreach (KeyValuePair<int, CanvasGroup> pair in this._legendItems)
						pair.Value.alpha = (this._hiddenStages & pair.Key) != 0 ? 0.35f : 1f;
					this.FiltersChanged();
				};
			}

			private void MeasureCosts()
			{
				if (global::HSLRE.EffectCost.Measuring)
					return;
				global::HSLRE.EffectCost.Begin();
				KitToast.Show("HSLRE", "Measuring effect costs…", ToastKind.Info, 1.2f);
			}

			/// <summary>Stopwatch tint while measuring, and a toast with the total when it is done.</summary>
			private void RefreshMeasure()
			{
				bool measuring = global::HSLRE.EffectCost.Measuring;
				if (measuring == this._measureShown)
					return;
				this._measureShown = measuring;
				this._measure.Icon.color = measuring ? Theme.Accent : Theme.Text;
				if (measuring == false)
					KitToast.Show("HSLRE", "Effects cost " + global::HSLRE.EffectCost.Total.ToString("0.0") + " ms per frame in total.", ToastKind.Success, 3f);
			}

			// ------------------------------------------------------------------ undo / redo

			private bool MouseOverWindow()
			{
				return RectTransformUtility.RectangleContainsScreenPoint(this._win.Rect, Input.mousePosition, null);
			}

			private static bool Typing()
			{
				GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
				InputField field = selected != null ? selected.GetComponent<InputField>() : null;
				return field != null && field.isFocused;
			}

			/// <summary>Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z) pressed this frame with the mouse over the open window.</summary>
			internal static bool UndoShortcutPressed()
			{
				KitPanel p = _current;
				if (p == null || p._win == null || p._win.Visible == false)
					return false;
				if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) == false)
					return false;
				if ((Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Y)) == false)
					return false;
				return Typing() == false && p.MouseOverWindow();
			}

			/// <summary>Records a step after each finished interaction with the window, and handles the shortcuts.</summary>
			private void TickUndo()
			{
				if (UndoShortcutPressed())
				{
					bool redo = Input.GetKeyDown(KeyCode.Y) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
					bool done = redo ? this._o.Redo() : this._o.Undo();
					string text = this._o.ShowingSnapshot ? "Switch Compare off first." : done ? (redo ? "Redo" : "Undo") : (redo ? "Nothing to redo" : "Nothing to undo");
					KitToast.Show("HSLRE", text, done ? ToastKind.Info : ToastKind.Warning, 1f);
					this._recordUndoIn = 0;
					return;
				}
				if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
					this._trackPress = this.MouseOverWindow() || KitModal.AnyOpen;
				// one frame later, so the click has been handled by the UI first
				if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) && this._trackPress)
				{
					this._trackPress = false;
					this._recordUndoIn = 2;
				}
				if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
					this._recordUndoIn = 2;
				if (this._recordUndoIn > 0 && --this._recordUndoIn == 0)
					this._o.RecordUndo();
			}

			private void FiltersChanged()
			{
				this._keepListed.Clear();
				this._listSignature = 0;
			}

			/// <summary>
			/// Click receiver for plain (non-button) items. It takes the pointer-down too: otherwise an ancestor
			/// that handles pointer-down would receive the click instead.
			/// </summary>
			private sealed class ClickRelay : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
			{
				internal Action Clicked;

				public void OnPointerDown(PointerEventData e)
				{
				}

				public void OnPointerClick(PointerEventData e)
				{
					if (e.button == PointerEventData.InputButton.Left)
						this.Clicked?.Invoke();
				}
			}

			private float _snapshotLabelUntil = -1f;

			private static void PlaceTopRight(RectTransform rect, float width, float x)
			{
				rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
				rect.pivot = new Vector2(1f, 1f);
				rect.sizeDelta = new Vector2(width, 30f);
				rect.anchoredPosition = new Vector2(x, -16f);
			}

			private void SetCompare(bool before)
			{
				this._o.ShowSnapshot(before);
				this.RefreshCompare();
			}

			/// <summary>Brings the buttons and the settings lock in line with the compare state.</summary>
			private void RefreshCompare()
			{
				bool before = this._o.ShowingSnapshot;
				if (this._compare == null)
					return;
				string label = before ? "Showing: snapshot" : "Compare";
				if (this._compare.Label != label)
				{
					this._compare.Label = label;
					this._compare.Style = before ? ButtonStyle.Primary : ButtonStyle.Secondary;
					this._snapshot.Interactable = before == false;
					this._resetEffect.Interactable = before == false;
					this._listGroup.interactable = this._detailGroup.interactable = before == false;
					this._listGroup.blocksRaycasts = this._detailGroup.blocksRaycasts = before == false;
					this._listGroup.alpha = this._detailGroup.alpha = before ? 0.45f : 1f;
				}
				if (this._snapshotLabelUntil > 0f && Time.unscaledTime > this._snapshotLabelUntil)
				{
					this._snapshotLabelUntil = -1f;
					this._snapshot.Label = "Snapshot";
				}
			}

			/// <summary>Escape belongs to an open dialog or menu first, not to the window.</summary>
			internal static bool EscapeTaken => KitModal.AnyOpen || KitContextMenu.IsOpen;

			internal void Destroy()
			{
				// the shown page (preset thumbnails) and the static _current kept the dead panel alive
				if (this._shownPage != null)
				{
					this._shownPage.Destroy();
					this._shownPage = null;
				}
				if (_current == this)
					_current = null;
				if (this._win != null)
					UnityEngine.Object.Destroy(this._win.Rect.gameObject);
			}

			// ------------------------------------------------------------------ per frame

			internal void Tick()
			{
				if (_showUI != this._win.Visible)
				{
					if (_showUI)
						this.Open();
					else
						this._win.Hide();
				}
				if (this._win.Visible == false)
					return;
				this.RefreshCompare(); // a scene / preset load can end the compare on its own
				this.RefreshMeasure();
				this.TickUndo();

				int signature = this.ListSignature();
				if (signature != this._listSignature)
				{
					this._listSignature = signature;
					this.RebuildList();
				}
				for (int i = 0; i < this._rows.Count; i++)
					this._rows[i].Refresh(this._rows[i].Entry == this._selected);

				if (this._selected != null)
				{
					if (this._selected.Page != null)
						this._selected.Page.Tick();
					else
					{
						this._backend.BeginFrame();
						IblUi.Run(this._backend, this._selected.Draw);
						this._backend.EndFrame();
					}
				}
			}

			/// <summary>Opens the window, first asking about HDR / deferred rendering like the original window does.</summary>
			private void Open()
			{
				if (this._opening)
					return;
				Camera cam = this._o.cam;
				if (cam.hdr == false)
				{
					if (cam.actualRenderingPath != RenderingPath.DeferredShading)
					{
						this._opening = true;
						KitModal.Show("HSLRE",
							"Using image based lighting in the chara maker requires deferred rendering path and HDR, meanwhile you can't use a background image. Do you want to continue?",
							new[] { "Just this time", "Always", "No" }, result =>
							{
								this._opening = false;
								if (result == 0 || result == 1)
								{
									if (result == 1)
										Settings.basicSettings.CharaMakerReform = true;
									Console.WriteLine("HSIBL Info: Changing rendering path to deferred shading.");
									cam.renderingPath = RenderingPath.DeferredShading;
									cam.clearFlags = (CameraClearFlags)Settings.basicSettings.CharaMakerBackgroundType;
									cam.hdr = true;
									this._win.Show();
								}
								else
									_showUI = false;
							});
						return;
					}
					Console.WriteLine("HSIBL Warning: HDR has somehow been disabled! Trying to re-enable it...");
					cam.hdr = true;
					if (cam.hdr == false)
					{
						Console.WriteLine("HSIBL Error: Failed to enable HDR");
						_showUI = false;
						return;
					}
					Console.WriteLine("HSIBL Info: Done!");
				}
				if (_errorcode > 1)
				{
					this._opening = true;
					KitModal.Show("HSLRE", "Error" + _errorcode + ": Please make sure you have installed HS linear rendering experiment (Version ≥ 3).", new[] { "OK" }, result =>
					{
						this._opening = false;
						_showUI = false;
					});
					return;
				}
				this._listSignature = 0;
				this._win.Show();
			}

			// ------------------------------------------------------------------ list

			private int ListSignature()
			{
				unchecked
				{
					int h = 17;
					h = h * 31 + (int)this._o._effectTypeDisplay;
					h = h * 31 + this._search.GetHashCode();
					h = h * 31 + this._stateFilter;
					foreach (string id in this._pinned)
						h = h * 31 + id.GetHashCode();
					h = h * 31 + this._hiddenStages;
					h = h * 31 + (this._o._cMfemale != null ? 1 : 0);
					h = h * 31 + this._o._effectsModules.Count;
					foreach (EffectData d in self.opaqueEffects)
						h = h * 31 + d.effect.GetHashCode();
					h = h * 31 + 7;
					foreach (EffectData d in self.generalEffects)
						h = h * 31 + d.effect.GetHashCode();
					return h == 0 ? 1 : h;
				}
			}

			private void BuildEntries()
			{
				this._entries.Clear();
				HSIBL o = this._o;

				this.Add(new Entry { Id = "presets", Name = "Presets", Group = "Other", Page = this._presetsPage ?? (this._presetsPage = new PresetsPage(o)) });
				if (o._colorLooks != null)
					this.Add(new Entry { Id = "looks", Name = "Color Looks", Group = "Other", Draw = o.ColorLooksModule });
				if (o._screenOverlays != null)
					this.Add(new Entry { Id = "overlays", Name = "Screen Overlays", Group = "Other", Draw = o.ScreenOverlaysModule });
				if (o._cMfemale != null)
					this.Add(new Entry { Id = "rotate", Name = "Character Rotation", Group = "Other", Draw = o.CharaRotateModule });

				this.Add(new Entry
				{
					Id = "skybox", Name = "Skybox & Cubemap", Group = "Scene",
					Draw = () =>
					{
						IblUi.Enabled = !o._isLoading;
						o.CubeMapModule();
						IblUi.Enabled = true;
						IblUi.HorizontalLine();
						if (o._selectedCubemap == 0)
							o.ProceduralSkyboxModule();
						else
							o.SkyboxModule();
					}
				});
				if (o._animatedSky != null)
					this.Add(new Entry { Id = "sky", Name = "Animated Sky", Group = "Scene", Draw = o.AnimatedSkyModule });
				if (o._skyBackdrop != null)
					this.Add(new Entry { Id = "skyBackdrop", Name = "Sky Backdrop", Group = "Scene", Draw = o.SkyBackdropModule });
				this.Add(new Entry { Id = "reflection", Name = "Reflection Probe", Group = "Scene", Draw = o.ReflectionModule });
				this.Add(new Entry { Id = "lights", Name = "Directional Lights", Group = "Scene", Draw = o.DefaultLightModule });
				this.Add(new Entry
				{
					Id = "camera", Name = "Field of View & Lens Presets", Group = "Camera",
					Draw = () =>
					{
						o.FieldOfViewModule();
						if (o._lensManager != null)
						{
							IblUi.HorizontalLine();
							o.LensPresetsModule();
						}
					}
				});

				int tonemappingIndex = int.MaxValue;
				if (o._toneMappingManager != null)
					tonemappingIndex = self.generalEffects.FindIndex(e => ReferenceEquals(e.effect, o._toneMappingManager));
				int opaqueCount = self.opaqueEffects.Count;
				for (int i = 0; i < opaqueCount; i++)
					this.AddEffect(self.opaqueEffects[i], i, -1, OpaqueColor, "Applied to opaque objects");
				for (int i = 0; i < self.generalEffects.Count; i++)
				{
					bool hdr = i < tonemappingIndex;
					this.AddEffect(self.generalEffects[i], i + opaqueCount, i, hdr ? HdrColor : LdrColor, hdr ? "Applied to everything, HDR (before tonemapping)" : "Applied to everything, LDR (after tonemapping)");
				}
				this.MovePinnedToTop();

			}

			private void AddEffect(EffectData data, int number, int generalIndex, Color accent, string stage)
			{
				if ((data.effectType & this._o._effectTypeDisplay) == 0)
					return;
				string id = "fx:" + data.effect.GetType().FullName;
				int stageBit = accent == OpaqueColor ? StageOpaque : accent == HdrColor ? StageHdr : StageLdr;
				if ((this._hiddenStages & stageBit) != 0)
					return;
				bool stateMatches = this._stateFilter == 0 || (this._stateFilter == 1) == data.enabled;
				if (stateMatches == false && this._keepListed.Contains(id) == false)
					return;
				if (this._o._effectsModules.TryGetValue(data.effect, out Action<EffectData, int> module) == false)
					return;
				bool composite = ReferenceEquals(data.effect, this._o._toneMappingManager) || ReferenceEquals(data.effect, this._o._lensManager);
				this.Add(new Entry
				{
					Id = id,
					Name = PrettyName(data),
					Group = "Effects",
					Number = number.ToString(),
					Stage = stage,
					Data = data,
					Draw = () => module(data, number),
					HeadlessDraw = () => module(data, number),
					HasSwitch = composite == false,
					Reorderable = generalIndex >= 0,
					GeneralIndex = generalIndex,
					Accent = accent
				});
			}

			private void Add(Entry e)
			{
				if (this._search.Length != 0 && e.Name.IndexOf(this._search, StringComparison.OrdinalIgnoreCase) == -1)
					return;
				this._entries.Add(e);
			}

			private static readonly Dictionary<string, string> _names = new Dictionary<string, string>
			{
				{ "segi", "SEGI" },
				{ "ssao", "SSAO" },
				{ "ssr", "Screen Space Reflections" },
				{ "volumetricLights", "Volumetric Lights" },
				{ "depthOfField", "Depth of Field" },
				{ "vignetteAndChromaticAberration", "Vignette & Chromatic Aberration" },
				{ "sunShafts", "Sun Shafts" },
				{ "smaa", "SMAA" },
				{ "bloom", "Cinematic Bloom" },
				{ "amplifyBloom", "Amplify Bloom" },
				{ "tonemapping", "Tonemapping & Color Grading" },
				{ "colorCorrectionCurves", "Color Correction Curves" },
				{ "bloomAndFlares", "Bloom and Flares" },
				{ "motionBlur", "Camera Motion Blur" },
				{ "afterImage", "After Image" },
				{ "noiseAndGrain", "Noise and Grain" },
				{ "lens", "Lens Aberrations" },
				{ "blur", "Blur" },
				{ "antialiasing", "Antialiasing" }
			};

			private string PrettyName(EffectData data)
			{
				if (this._o._effectsPrettyNames.TryGetValue(data.effect, out string pretty) && string.IsNullOrEmpty(pretty) == false)
					return pretty;
				string simple = data.simpleName;
				if (string.IsNullOrEmpty(simple))
					return data.effect.GetType().Name;
				if (_names.TryGetValue(simple, out pretty))
					return pretty;
				// an effect added by another plugin: "someEffectName" -> "Some Effect Name"
				System.Text.StringBuilder sb = new System.Text.StringBuilder(simple.Length + 4);
				for (int i = 0; i < simple.Length; i++)
				{
					char c = simple[i];
					if (i == 0)
						c = char.ToUpperInvariant(c);
					else if (char.IsUpper(c) && char.IsLower(simple[i - 1]))
						sb.Append(' ');
					sb.Append(c);
				}
				return sb.ToString();
			}

			private readonly Dictionary<string, KitText> _groupHeaders = new Dictionary<string, KitText>();

			/// <summary>
			/// Brings the list in line with the entries. Rows are reused (matched by id), so when the order
			/// changes each row glides from where it was drawn to its new place.
			/// </summary>
			private void RebuildList()
			{
				RectTransform content = this._listScroll.Content;
				Dictionary<string, RowView> previous = new Dictionary<string, RowView>();
				Dictionary<string, float> previousY = new Dictionary<string, float>();
				foreach (RowView r in this._rows)
				{
					previous[r.Entry.Id] = r;
					previousY[r.Entry.Id] = content.InverseTransformPoint(r.Body.position).y;
				}
				this._rows.Clear();

				this.BuildEntries();
				HashSet<string> usedGroups = new HashSet<string>();
				int sibling = 0;
				Entry keep = null;
				foreach (Entry e in this._entries)
				{
					if (usedGroups.Add(e.Group))
					{
						if (this._groupHeaders.TryGetValue(e.Group, out KitText header) == false)
						{
							header = KitText.Create(content, e.Group.ToUpperInvariant(), TextRole.Small, false);
							Kit.Layout(header.gameObject, -1f, 26f);
							header.Text.alignment = TextAnchor.LowerLeft;
							header.Text.color = Theme.TextDim;
							this._groupHeaders[e.Group] = header;
						}
						header.transform.SetSiblingIndex(sibling++);
						if (e.Group == "Effects")
							this._filter.transform.SetSiblingIndex(sibling++);
					}
					if (previous.TryGetValue(e.Id, out RowView row))
					{
						previous.Remove(e.Id);
						row.Bind(e);
					}
					else
						row = RowView.Create(this, e);
					row.transform.SetSiblingIndex(sibling++);
					this._rows.Add(row);
					if (e.Id == this._selectedId)
						keep = e;
				}
				// keep the filter reachable even when it filters out every effect (only a search may hide it)
				bool effectsListed = usedGroups.Contains("Effects");
				this._filter.gameObject.SetActive(effectsListed || this._search.Length == 0);
				if (effectsListed == false)
					this._filter.transform.SetSiblingIndex(sibling++);
				foreach (RowView r in previous.Values)
				{
					r.gameObject.SetActive(false);
					UnityEngine.Object.Destroy(r.gameObject);
				}
				List<string> unusedGroups = new List<string>();
				foreach (KeyValuePair<string, KitText> pair in this._groupHeaders)
					if (usedGroups.Contains(pair.Key) == false)
						unusedGroups.Add(pair.Key);
				foreach (string g in unusedGroups)
				{
					this._groupHeaders[g].gameObject.SetActive(false);
					UnityEngine.Object.Destroy(this._groupHeaders[g].gameObject);
					this._groupHeaders.Remove(g);
				}

				LayoutRebuilder.ForceRebuildLayoutImmediate(content);
				foreach (RowView r in this._rows)
				{
					if (previousY.TryGetValue(r.Entry.Id, out float oldY))
						r.GlideFrom(oldY - content.InverseTransformPoint(r.transform.position).y);
					else
						r.SetOffset(0f, false);
				}
				this.Select(keep ?? (this._entries.Count != 0 ? this._entries[0] : null));
			}

			private void Select(Entry e)
			{
				bool changed = this._selected == null || e == null || this._selected.Id != e.Id;
				this._selected = e;
				if (e == null)
				{
					if (this._shownPage != null)
					{
						this._shownPage.Destroy();
						this._shownPage = null;
					}
					this.UpdateActions(null);
					this._detailTitle.Value = "Nothing matches the search";
					this._detailSubtitle.Value = "";
					this._backend.Clear();
					return;
				}
				this._selectedId = e.Id;
				this.UpdateActions(e);
				this._detailTitle.Value = e.Number != null ? e.Number + "   " + e.Name : e.Name;
				this._detailSubtitle.Value = e.Stage ?? e.Group;
				this._detailSubtitle.Text.color = e.Accent.a > 0f ? e.Accent : Theme.TextDim;
				if (changed)
				{
					this._backend.Clear();
					if (this._shownPage != null)
					{
						this._shownPage.Destroy();
						this._shownPage = null;
					}
					if (e.Page != null)
					{
						this._shownPage = e.Page;
						e.Page.Build(this._detailScroll.Content);
					}
					this._detailScroll.ScrollToTop();
				}
			}

			private void SetEnabled(Entry e, bool on)
			{
				if (e.Data == null)
					return;
				e.Data.enabled = on;
				if (this._stateFilter != 0)
					this._keepListed.Add(e.Id); // stays listed until a filter changes
				// the section also mirrors some switches into Studio's own scene settings; run it once without drawing
				if (e.HeadlessDraw != null)
					IblUi.Run(this._headless, e.HeadlessDraw);
			}

			// ------------------------------------------------------------------ drag to reorder

			// While a row is dragged it follows the mouse and the rows it passes slide out of its way; the
			// render order only changes when it is dropped.
			private readonly List<RowView> _dragRows = new List<RowView>();
			private int _dragFrom = -1, _dragTo = -1;
			private float _dragStartY, _dragPitch;

			private float PointerY(Vector2 screen)
			{
				return Kit.ScreenToLocal(this._listScroll.Content, screen).y;
			}

			private void DragBegin(RowView dragged, Vector2 screen)
			{
				this._dragRows.Clear();
				foreach (RowView r in this._rows)
					if (r.Entry.Reorderable)
						this._dragRows.Add(r);
				this._dragFrom = this._dragTo = this._dragRows.IndexOf(dragged);
				if (this._dragFrom == -1)
					return;
				RectTransform content = this._listScroll.Content;
				this._dragPitch = 36f;
				if (this._dragRows.Count > 1)
					this._dragPitch = Mathf.Abs(content.InverseTransformPoint(this._dragRows[0].transform.position).y - content.InverseTransformPoint(this._dragRows[1].transform.position).y);
				this._dragStartY = this.PointerY(screen);
				dragged.Lift(true, this.DragLayer());
			}

			private RectTransform _dragLayer;

			private RectTransform DragLayer()
			{
				if (this._dragLayer == null)
				{
					this._dragLayer = Kit.Rect(this._listScroll.Content, "DragLayer");
					Kit.IgnoreLayout(this._dragLayer.gameObject);
					Kit.Fill(this._dragLayer);
				}
				return this._dragLayer;
			}

			private void DragUpdate(RowView dragged, Vector2 screen)
			{
				if (this._dragFrom == -1)
					return;
				float delta = this.PointerY(screen) - this._dragStartY;
				float maxUp = this._dragFrom * this._dragPitch, maxDown = (this._dragRows.Count - 1 - this._dragFrom) * this._dragPitch;
				delta = Mathf.Clamp(delta, -maxDown, maxUp);
				dragged.SetOffset(delta, false);
				int to = Mathf.Clamp(this._dragFrom - Mathf.RoundToInt(delta / this._dragPitch), 0, this._dragRows.Count - 1);
				if (to == this._dragTo)
					return;
				this._dragTo = to;
				for (int i = 0; i < this._dragRows.Count; i++)
				{
					if (i == this._dragFrom)
						continue;
					float shift = 0f;
					if (this._dragFrom < to && i > this._dragFrom && i <= to)
						shift = this._dragPitch;
					else if (to < this._dragFrom && i >= to && i < this._dragFrom)
						shift = -this._dragPitch;
					this._dragRows[i].SetOffset(shift, true);
				}
			}

			private void DragEnd(RowView dragged)
			{
				if (this._dragFrom == -1)
					return;
				dragged.Lift(false, this.DragLayer());
				int from = this._dragFrom, to = this._dragTo;
				this._dragFrom = this._dragTo = -1;
				if (to != from && dragged.Entry.Data != null)
				{
					self.MoveAtIndex(dragged.Entry.Data.effect, this._dragRows[to].Entry.GeneralIndex);
					this._listSignature = this.ListSignature();
					this.RebuildList();
				}
				else
					foreach (RowView r in this._dragRows)
						r.SetOffset(0f, true);
				this._dragRows.Clear();
			}

			// ------------------------------------------------------------------ list row

			private sealed class RowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
			{
				internal Entry Entry;
				internal RectTransform Body;
				private KitPanel _panel;
				private Image _bg, _bar;
				private KitSwitch _switch;
				private Text _name, _number;
				private KitTooltip _nameTip;
				private RectTransform _home;
				private bool _hover, _selected, _dragging, _lifted, _shownSelected = true;

				internal static RowView Create(KitPanel panel, Entry e)
				{
					RectTransform rt = Kit.Rect(panel._listScroll.Content, "Row " + e.Name);
					Kit.Layout(rt.gameObject, -1f, 34f);
					RowView row = rt.gameObject.AddComponent<RowView>();
					row._panel = panel;
					row.Entry = e;
					Kit.HitArea(rt.gameObject);

					// everything visible sits in Body, which slides while the layout keeps the row's slot
					row.Body = Kit.Rect(rt, "Body");
					Kit.Fill(row.Body);
					row._bg = Kit.Image(row.Body, "Bg", Sprites.Rounded(Theme.RadiusSmall), Color.clear);
					Kit.Fill(row._bg.rectTransform);
					Kit.IgnoreLayout(row._bg.gameObject);
					HorizontalLayoutGroup h = row.Body.gameObject.AddComponent<HorizontalLayoutGroup>();
					h.padding = new RectOffset(4, 8, 0, 0);
					h.spacing = 6f;
					h.childAlignment = TextAnchor.MiddleLeft;
					h.childForceExpandWidth = false;
					h.childForceExpandHeight = false;

					row._bar = Kit.Image(row.Body, "Accent", Sprites.Pill(4f), Color.clear);
					Kit.Layout(row._bar.gameObject, 4f, 22f);

					if (e.Reorderable)
					{
						Image grip = Kit.Image(row.Body, "Grip", KitPanelIcons.Grip, Theme.TextDim);
						Kit.Layout(grip.gameObject, 12f, 16f);
						Kit.HitArea(grip.gameObject);
						KitTooltip.Attach(grip.gameObject, "Drag to change the render order");
					}
					if (e.Number != null)
					{
						row._number = Kit.MakeText(row.Body, "Number", "", Theme.SmallSize, Theme.TextDim, TextAnchor.MiddleRight);
						Kit.Layout(row._number.gameObject, 18f, 20f);
					}
					row._name = Kit.MakeText(row.Body, "Name", "", Theme.FontSize, Theme.Text, TextAnchor.MiddleLeft);
					LayoutElement nl = Kit.Layout(row._name.gameObject, -1f, 22f);
					nl.flexibleWidth = 1f;
					nl.minWidth = 40f;
					nl.preferredWidth = 0f;
					// long names stop before the switch instead of running under it; the full name is in the tooltip
					row._name.horizontalOverflow = HorizontalWrapMode.Wrap;
					row._name.verticalOverflow = VerticalWrapMode.Truncate;
					row._nameTip = KitTooltip.Attach(row._name.gameObject, e.Name);

					if (e.Data != null)
					{
						// measured cost, filled in after a Measure (empty before, and for effects that are off)
						row._cost = Kit.MakeText(row.Body, "Cost", "", Theme.SmallSize, Theme.TextDim, TextAnchor.MiddleRight);
						Kit.Layout(row._cost.gameObject, 50f, 20f);
						row._costTip = KitTooltip.Attach(row._cost.gameObject, "");
					}
					if (e.HasSwitch)
					{
						row._switch = KitSwitch.Create(row.Body, "", e.Data.enabled, v => panel.SetEnabled(row.Entry, v));
						Kit.Layout(row._switch.gameObject, 44f, 30f);
					}
					row.Bind(e);
					return row;
				}

				/// <summary>Points a reused row at the current entry (its number and stage can change with the order).</summary>
				internal void Bind(Entry e)
				{
					this.Entry = e;
					this._bar.color = e.Accent.a > 0f ? e.Accent : Color.clear;
					if (this._number != null)
						this._number.text = e.Number ?? "";
					this._name.text = e.Name;
					this._costVersion = -1; // a reused row may now show another effect
					if (this._nameTip != null)
						this._nameTip.Text = e.Name;
					this.name = "Row " + e.Name;
				}

				internal void Refresh(bool selected)
				{
					this._selected = selected;
					if (this._switch != null && this._switch.Value != this.Entry.Data.enabled)
						this._switch.SetValue(this.Entry.Data.enabled, true, false);
					this.RefreshCost();
					if (selected != this._shownSelected)
					{
						this._shownSelected = selected;
						this.Paint();
					}
				}

				private Text _cost;
				private KitTooltip _costTip;
				private int _costVersion = -1;
				private bool _costEnabled;

				private void RefreshCost()
				{
					if (this._cost == null || this.Entry.Data == null)
						return;
					bool enabled = this.Entry.Data.enabled;
					if (this._costVersion == global::HSLRE.EffectCost.Version && this._costEnabled == enabled)
						return;
					this._costVersion = global::HSLRE.EffectCost.Version;
					this._costEnabled = enabled;
					float ms;
					if (enabled && global::HSLRE.EffectCost.TryGet(this.Entry.Data.effect, out ms))
					{
						this._cost.text = ms < 0.05f ? "<0.1 ms" : ms.ToString(ms < 10f ? "0.0" : "0") + " ms";
						this._cost.color = ms >= 4f ? new Color(1f, 0.45f, 0.4f) : ms >= 1.5f ? LdrColor : Theme.TextDim;
						this._costTip.Text = "Measured cost per frame (GPU + CPU). Measure again after changing settings.";
					}
					else
					{
						this._cost.text = "";
						this._costTip.Text = "";
					}
				}

				internal void SetOffset(float y, bool animate)
				{
					Tween.Kill(this, "offset");
					if (this._lifted)
					{
						// lifted into the drag layer: place it relative to its own slot
						RectTransform slot = (RectTransform)this.transform;
						this.Body.position = slot.TransformPoint(slot.rect.center + new Vector2(0f, y));
						return;
					}
					if (animate == false)
					{
						this.Body.anchoredPosition = new Vector2(0f, y);
						return;
					}
					float from = this.Body.anchoredPosition.y;
					Tween.Run(this, "offset", 0.22f, Ease.OutCubic, t => this.Body.anchoredPosition = new Vector2(0f, Tween.Lerp(from, y, t)));
				}

				/// <summary>After a relayout: start drawn where the row was, then slide into its new slot.</summary>
				internal void GlideFrom(float offset)
				{
					Tween.Kill(this, "offset");
					if (Mathf.Abs(offset) < 0.5f)
					{
						this.Body.anchoredPosition = Vector2.zero;
						return;
					}
					this.Body.anchoredPosition = new Vector2(0f, offset);
					Tween.Run(this, "offset", 0.3f, Ease.OutCubic, t => this.Body.anchoredPosition = new Vector2(0f, Tween.Lerp(offset, 0f, t)));
				}

				/// <summary>
				/// The dragged row is drawn above the others and slightly raised: its body moves into the list's
				/// drag layer (the last child of the list, so it draws on top, still inside the list's mask).
				/// </summary>
				internal void Lift(bool on, RectTransform dragLayer)
				{
					if (on == this._lifted)
						return;
					Tween.Kill(this, "offset");
					Vector3 world = this.Body.position;
					if (on)
					{
						this._home = (RectTransform)this.transform;
						Vector2 size = this._home.rect.size;
						dragLayer.SetAsLastSibling();
						this.Body.SetParent(dragLayer, false);
						this.Body.anchorMin = this.Body.anchorMax = new Vector2(0.5f, 0.5f);
						this.Body.pivot = new Vector2(0.5f, 0.5f);
						this.Body.sizeDelta = size;
						this.Body.position = world;
					}
					else
					{
						this.Body.SetParent(this._home, false);
						this.Body.pivot = new Vector2(0.5f, 0.5f);
						Kit.Fill(this.Body);
						this.Body.position = world;
					}
					this._lifted = on;
					Tween.Scale(this.Body, on ? 1.03f : 1f, 0.18f, Ease.OutCubic);
					this.Paint();
				}

				private void Paint()
				{
					Color c = this._lifted ? Kit.Shade(Theme.Surface, 0.12f)
						: this._selected ? Kit.Alpha(Theme.Accent, 0.28f)
						: this._hover ? Theme.SurfaceHover : Color.clear;
					Tween.Tint(this._bg, c, 0.12f, Ease.OutQuad);
					this._name.fontStyle = this._selected ? FontStyle.Bold : FontStyle.Normal;
				}

				public void OnPointerEnter(PointerEventData e) { this._hover = true; this.Paint(); }

				public void OnPointerExit(PointerEventData e) { this._hover = false; this.Paint(); }

				public void OnPointerClick(PointerEventData e)
				{
					if (e.button == PointerEventData.InputButton.Left && e.dragging == false)
						this._panel.Select(this.Entry);
				}

				public void OnBeginDrag(PointerEventData e)
				{
					if (this.Entry.Reorderable == false || e.button != PointerEventData.InputButton.Left)
					{
						// not reorderable: let the list scroll instead
						ExecuteEvents.ExecuteHierarchy(this.transform.parent.gameObject, e, ExecuteEvents.beginDragHandler);
						return;
					}
					this._dragging = true;
					this._panel.DragBegin(this, e.position);
				}

				public void OnDrag(PointerEventData e)
				{
					if (this._dragging)
						this._panel.DragUpdate(this, e.position);
					else
						ExecuteEvents.ExecuteHierarchy(this.transform.parent.gameObject, e, ExecuteEvents.dragHandler);
				}

				public void OnEndDrag(PointerEventData e)
				{
					if (this._dragging)
					{
						this._dragging = false;
						this._panel.DragEnd(this);
					}
					else
						ExecuteEvents.ExecuteHierarchy(this.transform.parent.gameObject, e, ExecuteEvents.endDragHandler);
				}
			}
		}

		/// <summary>Runs a section without drawing anything: every control keeps the value it is given.</summary>
		private sealed class HeadlessBackend : IIblBackend
		{
			public bool Enabled { get; set; } = true;
			public Color Color { get; set; } = Color.white;
			public void Label(string text, string tooltip, LabelKind kind) { }
			public bool Button(string text, string tooltip) => false;
			public bool ToggleButton(bool value, string text, string tooltip) => value;
			public float Slider(float value, float min, float max, float reset, string label, string tooltip, int decimals, bool hasMinMaxLimit, bool hasMinLimit) => value;
			public float CompactSlider(float value, float min, float max) => value;
			public bool Toggle(bool value, string label, string tooltip, bool header) => value;
			public bool ColorField(Color value, Color reset, string label, string tooltip, bool alpha, out Color result)
			{
				result = value;
				return false;
			}
			public int Select(int selected, string[] options, int columns, string tooltip) => selected;
			public int Dropdown(int selected, string[] options, string tooltip) => selected;
			public string TextField(string text) => text;
			public void Separator() { }
			public void Space(float height) { }
			public void BeginRow() { }
			public void EndRow() { }
		}
	}
}
