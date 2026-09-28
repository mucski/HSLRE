using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.CinematicEffects;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using IllusionPlugin;
using IllusionUtility.GetUtility;
using Studio;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine.Rendering;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using Object = UnityEngine.Object;
using HSLRE;
using HSLRE.CustomEffects;
using static HSLRE.HSLRE;
using TonemappingColorGrading = HSLRE.CustomEffects.TonemappingColorGrading;
using ToolBox_Shared;

using LensAberrations = UnityStandardAssets.CinematicEffects.LensAberrations;
//using LensAberrations = HSLRE.CustomEffects.LensAberrations;
using SMAA = UnityStandardAssets.CinematicEffects.SMAA;
using static UnityStandardAssets.CinematicEffects.ScreenSpaceReflection2;
using Harmony;

namespace HSIBL
{
	public partial class HSIBL : MonoBehaviour
	{
		#region Types
		private enum DOFFocusType
		{
			Crosshair,
			Object,
			Manual
		}
		#endregion

		#region Fields
		private readonly ProceduralSkyboxManager _proceduralSkybox = new ProceduralSkyboxManager();
		private readonly SkyboxManager _skybox = new SkyboxManager();
		public GameObject probeGameObject = new GameObject("RealtimeReflectionProbe");
		private ProceduralSkyboxParams _tempProceduralSkyboxParams;
		private SkyboxParams _tempSkyboxParams;
		private GameObject _lightsObj;
		private Quaternion _lightsObjDefaultRotation;
		private ReflectionProbe _probeComponent;
		private readonly int[] _possibleReflectionProbeResolutions = { 64, 128, 256, 512, 1024, 2048 };
		private string[] _possibleReflectionProbeResolutionsNames;
		private string[] _cubeMapFileNames;
		private int _reflectionProbeResolution;
		private SortedList<int, string> _layers;
		private CharFemale _cMfemale;
		public bool cameraCtrlOff;
		private Camera _subCamera;
		private FolderAssist _cubemapFolder;
		private const string _presetFolder = "Plugins\\HSIBL\\Presets\\";
		private readonly Dictionary<object, Action<EffectData, int>> _effectsModules = new Dictionary<object, Action<EffectData, int>>();
		private readonly Dictionary<object, string> _effectsPrettyNames = new Dictionary<object, string>();
		private EffectType _effectTypeDisplay = EffectType.FourKDiffuse | EffectType.LRE;
		private readonly string[] _effectTypeDisplayNames = { "4K", "LRE", "Both" };
		private Shortcut shortcutKey = new Shortcut(ModPrefs.GetString("HSIBL", "shortcut", "F5", true));
		private RectTransform _imguiBackground;

		private static ushort _errorcode = 0;
		private Light _backDirectionalLight;
		private Light _frontDirectionalLight;
		private Light _frontDirectionalMapLight;
		private bool _cubemapLoaded = false;
		private bool _hideSkybox = false;

		private bool _tonemappingEnabled = true;
		private TonemappingColorGrading.Tonemapper _toneMapper = TonemappingColorGrading.Tonemapper.ACES;
		private float _ev;
		private float _eyeSpeed;
		private bool _eyeEnabled = false;
		private float _eyeMiddleGrey;
		private float _eyeMin;
		private float _eyeMax;

		private TonemappingColorGrading _toneMappingManager;
		private HSLRE.CustomEffects.Bloom _cinematicBloom;
		private LensAberrations _lensManager;

		private float _previousAmbientIntensity = 1f;

		private bool _environmentUpdateFlag = false;

		private static bool _showUI = false;

		private float _charaRotate = 0f;
		private Vector3 _rotateValue;
		private bool _autoRotate = false;
		private float _autoRotateSpeed = 0.2f;
		private int _selectedCubemap;
		private int _previousSelectedCubeMap;
		private const int _uniqueId = ('H' << 24) | ('I' << 16) | ('B' << 8) | 'L';
		private int _reflectionProbeRefreshRate;

		private bool _windowdragflag = false;
		private int _tabMenu;
		private bool _frontLightAnchor = true;
		private bool _backLightAnchor = true;
		private Vector3 _frontRotate;
		private Vector3 _backRotate;
		private bool _isLoading = false;

		private SSAOPro _ssao;
		private string[] _possibleSSAOSampleCountNames;
		private string[] _possibleSSAOBlurModeNames;

		private SunShafts _sunShafts;
		private string[] _possibleSunShaftsResolutionNames;
		private string[] _possibleShaftsScreenBlendModeNames;

		private DepthOfField _depthOfField;
		private string[] _possibleBlurSampleCountNames;
		private string[] _possibleDOFBlurTypeNames;
		private Transform _depthOfFieldOriginalFocusPoint;
		private DOFFocusType _focusType = DOFFocusType.Crosshair;
		private string[] _possibleDOFFocusTypeNames;
		private ObjectCtrlInfo _focussedObject;

		private ScreenSpaceReflection2 _ssr;
		private string[] _possibleSSRResolutionNames;
		private string[] _possibleSSRReflectionBlendTypeNames;
		//private string[] _possibleSSRDebugModeNames;

		private VolumetricLightRenderer _volumetricLights;
		private string[] _possibleVolumetricLightsResolutionNames;

		private SMAA _smaa;
		private string[] _possibleSMAADebugPassNames;
		private string[] _possibleSMAAQualityPresetNames;
		private string[] _possibleSMAAEdgeDetectionMethodNames;

		private BloomAndFlares _bloomAndFlares;
		private string[] _possibleScreenBlendModeNames;
		private string[] _possibleHDRBloomModeNames;
		private string[] _possibleLensFlareStyle34Names;

		private ColorCorrectionCurves _colorCorrectionCurves;

		private VignetteAndChromaticAberration _vignette;
		private string[] _possibleAberrationModeNames;

		private Antialiasing _antialiasing;
		private string[] _possibleAAModeNames;

		private NoiseAndGrain _noiseAndGrain;
		private string[] _possibleFilterModeNames;

		private CameraMotionBlur _motionBlur;
		private string[] _possibleMotionBlurFilterNames;

		private AfterImage _afterImage;

		private SEGI _segi;
		private string[] _possibleVoxelResolutionNames;
		private string[] _possiblePresetNames;
		private int _selectedSegiPreset = 10;

		private AmplifyBloom _amplifyBloom;
		private string[] _possibleUpscaleQualityEnumNames;
		private string[] _possibleMainThresholdSizeEnumNames;
		private string[] _possiblePrecisionModesNames;
		private string[] _possibleDebugToScreenEnumNames;
		private string[] _possibleApertureShapeNames;
		private string[] _possibleGlareLibTypeNames;
		private string[] _possibleLensDirtTextureEnumNames;
		private string[] _possibleLensStarburstTextureEnumNames;

		private BlurOptimized _blur;
		private string[] _possibleBlurTypeNames;

		private Quaternion _frontLightDefaultRotation;
		private Quaternion _backLightDefaultRotation;
		private Material _originalSkybox;
		private AmbientMode _originalAmbientMode;
		private DefaultReflectionMode _originalDefaultReflectionMode;
		private string _currentTooltip = "";
		private Vector2 _presetsScroll;
		private string _presetName = "";
		private bool _removePresetMode;
		private string[] _presets = new string[0];
		internal static bool _isStudio = false;
		private AmbientMode _oldAmbientMode;

		Camera cam = Camera.main;
		float prevFrameAmbientIntensity = 0f;
		float savedWindowHeight = ModPrefs.GetFloat("HSIBL", "Window.height", 600);
		float savedWindowWidth = ModPrefs.GetFloat("HSIBL", "Window.width", 600);
		#endregion

		#region Accessors
		private readonly Func<float> _getWindowHeight = () => ModPrefs.GetFloat("HSIBL", "Window.height", 600);
		private readonly Func<float> _getWindowWidth = () => ModPrefs.GetFloat("HSIBL", "Window.width", 600);
		private string _defaultCharaMakerCubemap;
		private int _lastObjectCount = -1;
		private Object _lastSelectedNode;
		private VolumetricLight _currentVolumetricLight;
		#endregion

		#region Unity Methods
		private void Awake()
		{
			cam = Camera.main;
			cam.hdr = true;

			Console.WriteLine("HSIBL is Loaded");
			Console.WriteLine("----------------");

			_lightsObj = GameObject.Find("Lights");
			if (_lightsObj != null)
				_lightsObjDefaultRotation = _lightsObj.transform.localRotation;
			_probeComponent = probeGameObject.AddComponent<ReflectionProbe>();
			_probeComponent.mode = ReflectionProbeMode.Realtime;
			_probeComponent.resolution = 512;
			_probeComponent.hdr = true;
			_probeComponent.intensity = 1f;
			_probeComponent.type = ReflectionProbeType.Cube;
			_probeComponent.clearFlags = ReflectionProbeClearFlags.Skybox;
			_probeComponent.size = new Vector3(1000, 1000, 1000);
			probeGameObject.transform.position = new Vector3(0, 2, 0);
			_probeComponent.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
			_probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
			_probeComponent.nearClipPlane = 1.01f;
			_probeComponent.cullingMask &= ~(1 << 27); //SEGI GI Blockers
			_probeComponent.cullingMask &= ~(1 << 26); //Suimono FX
			_probeComponent.cullingMask &= ~(1 << 25); //Suimono Depth
			_probeComponent.cullingMask &= ~(1 << 24); //Suimono Water

			_layers = new SortedList<int, string>(32);
			for (int i = 0; i < 32; i++)
			{
				string name = LayerMask.LayerToName(i);
				if (string.IsNullOrEmpty(name) == false)
					_layers.Add(i, name);
			}
			_layers.Add(24, "Suimono");

			_possibleReflectionProbeResolutionsNames = _possibleReflectionProbeResolutions.Select(e => e.ToString()).ToArray();
			_possibleSSAOSampleCountNames = Enum.GetNames(typeof(SSAOPro.SampleCount));
			_possibleSSAOBlurModeNames = Enum.GetNames(typeof(SSAOPro.BlurMode));
			_possibleSunShaftsResolutionNames = Enum.GetNames(typeof(SunShafts.SunShaftsResolution));
			_possibleShaftsScreenBlendModeNames = Enum.GetNames(typeof(SunShafts.ShaftsScreenBlendMode));
			_possibleBlurSampleCountNames = Enum.GetNames(typeof(DepthOfField.BlurSampleCount));
			_possibleDOFFocusTypeNames = Enum.GetNames(typeof(DOFFocusType));
			_possibleDOFBlurTypeNames = Enum.GetNames(typeof(DepthOfField.BlurType));
			_possibleSSRResolutionNames = Enum.GetNames(typeof(SSRResolution));
			//_possibleSSRDebugModeNames = Enum.GetNames(typeof(ScreenSpaceReflection.SSRDebugMode));
			_possibleVolumetricLightsResolutionNames = Enum.GetNames(typeof(VolumetricLightRenderer.VolumetricResolution));
			_possibleSMAADebugPassNames = Enum.GetNames(typeof(SMAA.DebugPass));
			_possibleSMAAQualityPresetNames = Enum.GetNames(typeof(SMAA.QualityPreset));
			_possibleSMAAEdgeDetectionMethodNames = Enum.GetNames(typeof(SMAA.EdgeDetectionMethod));
			_possibleScreenBlendModeNames = Enum.GetNames(typeof(BloomScreenBlendMode));
			_possibleHDRBloomModeNames = Enum.GetNames(typeof(HDRBloomMode));
			_possibleLensFlareStyle34Names = Enum.GetNames(typeof(LensflareStyle34));
			_possibleAberrationModeNames = Enum.GetNames(typeof(VignetteAndChromaticAberration.AberrationMode));
			_possibleAAModeNames = Enum.GetNames(typeof(AAMode));
			_possibleFilterModeNames = Enum.GetNames(typeof(FilterMode));
			_possibleMotionBlurFilterNames = Enum.GetNames(typeof(CameraMotionBlur.MotionBlurFilter));
			_possibleVoxelResolutionNames = Enum.GetNames(typeof(SEGI.VoxelResolution));
			_possibleUpscaleQualityEnumNames = Enum.GetNames(typeof(UpscaleQualityEnum));
			_possibleMainThresholdSizeEnumNames = Enum.GetNames(typeof(MainThresholdSizeEnum));
			_possiblePrecisionModesNames = Enum.GetNames(typeof(PrecisionModes));
			_possibleDebugToScreenEnumNames = Enum.GetNames(typeof(DebugToScreenEnum));
			_possibleApertureShapeNames = Enum.GetNames(typeof(ApertureShape));
			_possibleGlareLibTypeNames = Enum.GetNames(typeof(GlareLibType)).ToList().Take((int)(GlareLibType.Custom - 1)).ToArray();
			_possibleLensDirtTextureEnumNames = Enum.GetNames(typeof(LensDirtTextureEnum));
			_possibleLensStarburstTextureEnumNames = Enum.GetNames(typeof(LensStarburstTextureEnum));
			_possibleBlurTypeNames = Enum.GetNames(typeof(BlurOptimized.BlurType));


			_possibleSSRReflectionBlendTypeNames = Enum.GetNames(typeof(SSRReflectionBlendType));



			if (HSIBLPlugin._self.binary == Binary.Studio)
			{
				_isStudio = true;
				HSExtSave.HSExtSave.RegisterHandler("hsibl", null, null, OnSceneLoad, null, OnSceneSave, null, null);
			}

			_effectTypeDisplay = (EffectType)ModPrefs.GetInt("HSIBL", "effectTypeFilter", (int)_effectTypeDisplay, true);
			_defaultCharaMakerCubemap = ModPrefs.GetString("HSIBL", "defaultCharaMakerCubemap", "", true);
			// Addition: the HSUIKit panel, or the original window when UseKitUI is off. The original window's
			// OnGUI lives on its own component so it costs nothing while the kit panel is used.
			_useKitUI = ModPrefs.GetBool("HSIBL", "UseKitUI", true, true);
			if (_useKitUI == false)
			{
				_imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI(HSIBLPlugin._self.binary == Binary.Game);
				gameObject.AddComponent<ClassicGuiHost>().owner = this;
			}
		}

		private IEnumerator Start()
		{
			yield return null;
			yield return null;
			yield return null;
			_originalSkybox = RenderSettings.skybox;
			_originalAmbientMode = RenderSettings.ambientMode;
			_originalDefaultReflectionMode = RenderSettings.defaultReflectionMode;
			yield return new WaitUntil(() => self.init);

			_ssao = self.ssao;
			if (_ssao != null)
				_effectsModules.Add(_ssao, SSAOModule);

			_sunShafts = self.sunShafts;
			if (_sunShafts != null)
				_effectsModules.Add(_sunShafts, SunShaftsModule);

			_depthOfField = self.dof;
			if (_depthOfField != null)
			{
				_effectsModules.Add(_depthOfField, DepthOfFieldModule);
				_depthOfFieldOriginalFocusPoint = _depthOfField.focalTransform;
			}
			_ssr = self.ssr;
			if (_ssr != null)
				_effectsModules.Add(_ssr, SSRModule);

			_volumetricLights = self.volumetricLights;
			if (_volumetricLights != null)
				_effectsModules.Add(_volumetricLights, VolumetricLightsModule);

			_smaa = self.smaa;
			if (_smaa != null)
				_effectsModules.Add(_smaa, SMAAModule);

			_cinematicBloom = self.cinematicBloom;
			if (_cinematicBloom != null)
				_effectsModules.Add(_cinematicBloom, BloomModule);

			_toneMappingManager = self.tonemapping;
			if (_toneMappingManager != null)
				_effectsModules.Add(_toneMappingManager, ToneMappingColorGradingModule);

			_lensManager = self.lensAberrations;
			if (_lensManager != null)
				_effectsModules.Add(_lensManager, LensModule);

			_bloomAndFlares = self.bloomAndFlares;
			if (_bloomAndFlares != null)
				_effectsModules.Add(_bloomAndFlares, BloomAndFlaresModule);

			_colorCorrectionCurves = self.ccc;
			if (_colorCorrectionCurves != null)
				_effectsModules.Add(_colorCorrectionCurves, ColorCorrectionCurvesModule);

			_vignette = self.vignette;
			if (_vignette != null)
				_effectsModules.Add(_vignette, VignetteAndChromaticAberrationModule);

			_antialiasing = self.antialiasing;
			if (_antialiasing != null)
				_effectsModules.Add(_antialiasing, AntialiasingModule);

			_noiseAndGrain = self.noiseAndGrain;
			if (_noiseAndGrain != null)
				_effectsModules.Add(_noiseAndGrain, NoiseAndGrainModule);

			_motionBlur = self.motionBlur;
			if (_motionBlur != null)
				_effectsModules.Add(_motionBlur, MotionBlurModule);

			_afterImage = self.afterImage;
			if (_afterImage != null)
				_effectsModules.Add(_afterImage, AfterImageModule);

			_segi = self.segi;
			if (_segi != null)
				_effectsModules.Add(_segi, SEGIModule);

			_amplifyBloom = self.amplifyBloom;
			if (_amplifyBloom != null)
				_effectsModules.Add(_amplifyBloom, AmplifyBloomModule);

			_blur = self.blur;
			if (_blur != null)
				_effectsModules.Add(_blur, BlurModule);

			if (_isStudio == false && Settings.basicSettings.CharaMakerReform && (HSIBLPlugin._self.level == 21 || HSIBLPlugin._self.level == 22))
				_selectedCubemap = LoadCubemap(_defaultCharaMakerCubemap);

			if (_segi != null)
			{
				var src = _isStudio
					? (from oci in Studio.Studio.Instance.dicObjectCtrl.Values where oci.kind == 2 && (oci as OCILight).lightType == LightType.Directional select (oci as OCILight).light)
					: FindObjectsOfType<Light>().Where(l => l.type == LightType.Directional);
				_segi.sun = (src == null || !src.Any()) ? null : src.Aggregate((max, x) => x.intensity > max.intensity ? x : max);
				_possiblePresetNames = _segi._presets.Select(e => e.name).ToArray();
			}
			_ev = Mathf.Log(Settings.tonemappingSettings.exposure, 2);
			_colorLooks = new ColorLooks(); // addition
			OverlayShaders.Load(); // addition: screen overlay shaders
			_screenOverlays = new ScreenOverlays(); // addition
			_animatedSky = new AnimatedSky(true, "animatedSky"); // addition
			_skyBackdrop = new AnimatedSky(false, "skyBackdrop"); // addition: same sky, background only

			if (_useKitUI)
			{
				CaptureDefaults(); // addition: start-up settings for the per-effect reset
				try
				{
					_kitPanel = new KitPanel(this);
				}
				catch (Exception e)
				{
					Debug.LogError("HSLRE: could not create the kit panel, using the original window instead.\n" + e);
					_kitPanel = null;
					_useKitUI = false;
					_imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI(HSIBLPlugin._self.binary == Binary.Game);
					gameObject.AddComponent<ClassicGuiHost>().owner = this;
				}
			}
		}

		private void OnEnable()
		{
			if (!_proceduralSkybox.Proceduralsky)
			{
				_proceduralSkybox.Init();
			}
			_tempProceduralSkyboxParams = _proceduralSkybox.skyboxparams;
			StopAllCoroutines();
			StartCoroutine(EndOfFrame());
			if (_isStudio)
			{
				_subCamera = GameObject.Find("Camera").GetComponent<Camera>();
				_subCamera.fieldOfView = cam.fieldOfView;
				UIUtils.windowRect = new Rect(Screen.width * 0.7f, Screen.height * 0.65f, Screen.width * 0.34f, Screen.height * 0.45f);
			}
			else if (HSIBLPlugin._self.level == 21 || HSIBLPlugin._self.level == 22)
			{

				UIUtils.windowRect = new Rect(Screen.width * 0.22f, Screen.height * 0.64f, Screen.width * 0.34f, Screen.height * 0.45f);
				_cMfemale = Singleton<Character>.Instance.GetFemale(0);
				if (_cMfemale)
				{
					_rotateValue = _cMfemale.GetRotation();
					StartCoroutine(RotateCharater());
				}
			}
			else
			{
				UIUtils.windowRect = new Rect(0f, Screen.height * 0.3f, Screen.width * 0.35f, Screen.height * 0.45f);
			}

			_frontDirectionalLight = cam.transform.FindLoop("DirectionalFront").GetComponent<Light>();
			_backDirectionalLight = cam.transform.FindLoop("DirectionalBack").GetComponent<Light>();
			_frontLightDefaultRotation = _frontDirectionalLight.transform.localRotation;
			_backLightDefaultRotation = _backDirectionalLight.transform.localRotation;
			GameObject mapLight;
			if ((mapLight = GameObject.Find("DirectionalFrontMap")) != null)
				_frontDirectionalMapLight = mapLight.GetComponent<Light>();

			_cubemapFolder = new FolderAssist();
			_cubemapFolder.CreateFolderInfo(Application.dataPath + "/../abdata/plastic/cubemaps/", "*.unity3d", true, true);
			_selectedCubemap = -1;
			_previousSelectedCubeMap = -1;
			_cubeMapFileNames = new string[_cubemapFolder.lstFile.Count + 1];
			_cubeMapFileNames[0] = "Procedural";
			int count = 1;
			foreach (FolderAssist.FileInfo fileInfo in _cubemapFolder.lstFile)
			{
				_cubeMapFileNames[count] = fileInfo.FileName;
				count++;
			}
			_cubeMapNamesWithNone = null;

			RefreshPresetList();
			_environmentUpdateFlag = true;
		}

		private void Update()
		{
			if (_oldAmbientMode != RenderSettings.ambientMode)
				_environmentUpdateFlag = true;
			_oldAmbientMode = RenderSettings.ambientMode;

			if (shortcutKey.ShortcutPressed())
				_showUI = !_showUI;

			if (_showUI && Input.GetKeyDown(KeyCode.Escape) && (_useKitUI == false || KitPanel.EscapeTaken == false))
				_showUI = false;
			_colorLooks?.Apply(_toneMappingManager); // addition: bakes / hands over the look LUT only when it changed
			_screenOverlays?.Tick(cam); // addition: the overlay component only runs while an overlay is on
			CaptureDefaultsFallback(); // addition: only acts if capturing in Start failed
			global::HSLRE.EffectCost.Tick(); // addition: counts a cost measurement down
			_kitPanel?.Tick();
			if (_selectedCubemap == 0)
			{
				if (!Mathf.Approximately(_previousAmbientIntensity, RenderSettings.ambientIntensity) || !(_tempProceduralSkyboxParams).Equals(_proceduralSkybox.skyboxparams))
					_environmentUpdateFlag = true;
				_proceduralSkybox.ApplySkyboxParams();
				_tempProceduralSkyboxParams = _proceduralSkybox.skyboxparams;
				_previousAmbientIntensity = RenderSettings.ambientIntensity;
			}
			else if (_selectedCubemap > 0)
			{
				if (!Mathf.Approximately(_previousAmbientIntensity, RenderSettings.ambientIntensity) || !(_tempSkyboxParams).Equals(_skybox.skyboxparams))
					_environmentUpdateFlag = true;
				_skybox.ApplySkyboxParams();
				_tempSkyboxParams = _skybox.skyboxparams;
				_previousAmbientIntensity = RenderSettings.ambientIntensity;
			}

			if (_hideSkybox)
			{
				cam.clearFlags = CameraClearFlags.SolidColor;
			}
			else
			{
				if (!_isStudio && (HSIBLPlugin._self.level == 21 || HSIBLPlugin._self.level == 22))
				{
					if (Settings.basicSettings.CharaMakerReform)
						cam.clearFlags = (CameraClearFlags)Settings.basicSettings.CharaMakerBackgroundType;
				}
				else
					cam.clearFlags = _cubemapLoaded ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
			}
			// addition: after the clear flags above, so an animated sky is shown even with no cubemap picked
			if (_animatedSky != null && _animatedSky.Tick(cam, _hideSkybox))
				_environmentUpdateFlag = true;
			_skyBackdrop?.Tick(cam, _hideSkybox); // addition: after the animated sky, so it sets the clear flags last

			if (_isStudio && _segi != null && Studio.Studio.Instance != null && _lastObjectCount != Studio.Studio.Instance.dicObjectCtrl.Count)
			{
				var src = from oci in Studio.Studio.Instance.dicObjectCtrl.Values where oci.kind == 2 && (oci as OCILight).lightType == LightType.Directional select (oci as OCILight).light;
				_segi.sun = (src == null || !src.Any()) ? null : src.Aggregate((max, x) => x.intensity > max.intensity ? x : max);

				_lastObjectCount = Studio.Studio.Instance.dicObjectCtrl.Count;
			}

			if (_isStudio && Studio.Studio.Instance != null)
			{
				TreeNodeObject node = Studio.Studio.Instance.treeNodeCtrl.selectNode;
				if (_lastSelectedNode != node)
				{
					_lastSelectedNode = node;
					if (node != null && Studio.Studio.Instance.dicInfo.TryGetValue(node, out ObjectCtrlInfo objectCtrlInfo))
					{
						_currentVolumetricLight = objectCtrlInfo is OCILight light ? light.light.GetComponent<VolumetricLight>() : null;
					}
					else
					{
						_currentVolumetricLight = null;
					}
				}
			}
		}

		internal void DrawClassicGUI()
		{
			if (!_showUI)
			{
				_imguiBackground.gameObject.SetActive(false);
				return;
			}
			if (!UIUtils.styleInitialized)
				UIUtils.InitStyle();


			if (!cam.hdr)
			{
				if (cam.actualRenderingPath != RenderingPath.DeferredShading)
				{
					_imguiBackground.gameObject.SetActive(true);
					IMGUIExtensions.FitRectTransformToRect(_imguiBackground, UIUtils.cmWarningRect);
					UIUtils.cmWarningRect = GUILayout.Window(_uniqueId + 1, UIUtils.cmWarningRect, CharaMakerWarningWindow, "Warning", UIUtils.windowStyle);
					return;
				}
				Console.WriteLine("HSIBL Warning: HDR has somehow been disabled! Trying to re-enable it...");
				cam.hdr = true;
				if (!cam.hdr)
				{
					Console.WriteLine("HSIBL Error: Failed to enable HDR");
					_showUI = false;
					return;
				}
				Console.WriteLine("HSIBL Info: Done!");
			}
			UIUtils.UpdateScale();
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, UIUtils.scale);

			if (_errorcode > 1)
			{
				_imguiBackground.gameObject.SetActive(true);
				IMGUIExtensions.FitRectTransformToRect(_imguiBackground, UIUtils.errorWindowRect);
				UIUtils.errorWindowRect = GUILayout.Window(_uniqueId + 2, UIUtils.errorWindowRect, ErrorWindow, "", UIUtils.windowStyle);
				return;
			}
			if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp)
			{
				cameraCtrlOff = false;
				_windowdragflag = false;
			}
			UIUtils.windowRect = UIUtils.LimitWindowRect(UIUtils.windowRect);
			_imguiBackground.gameObject.SetActive(true);
			IMGUIExtensions.FitRectTransformToRect(_imguiBackground, new Rect(UIUtils.windowRect.x * UIUtils.scale.x, UIUtils.windowRect.y * UIUtils.scale.y, UIUtils.windowRect.width * UIUtils.scale.x, UIUtils.windowRect.height * UIUtils.scale.y));
			IMGUIExtensions.DrawBackground(UIUtils.windowRect);
			UIUtils.windowRect = GUILayout.Window(_uniqueId + 3, UIUtils.windowRect, HSIBLWindow, "", UIUtils.windowStyle);
			if (_currentTooltip.Length != 0)
			{
				Rect tooltipRect = new Rect(new Vector2(UIUtils.windowRect.xMin, UIUtils.windowRect.yMax), new Vector2(UIUtils.windowRect.width, UIUtils.labelStyle.CalcHeight(new GUIContent(_currentTooltip), UIUtils.windowRect.width) + 10));
				GUI.Box(tooltipRect, "");
				GUI.Box(tooltipRect, "");
				GUI.Label(tooltipRect, _currentTooltip, UIUtils.labelStyle);
			}
			//GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, UIUtils.scale);
		}

		private void OnDestroy()
		{
			ModPrefs.SetInt("HSIBL", "effectTypeFilter", (int)_effectTypeDisplay);
			ModPrefs.SetString("HSIBL", "defaultCharaMakerCubemap", _defaultCharaMakerCubemap);
			if (_imguiBackground != null)
				Destroy(_imguiBackground);
			_kitPanel?.Destroy();
		}
		#endregion

		#region UI
		private void HSIBLWindow(int id)
		{
			CameraControlOffOnGUI();
			if (Event.current.type == EventType.MouseDown)
			{
				GUI.FocusWindow(id);
				_windowdragflag = true;
				cameraCtrlOff = true;
			}
			else if (Event.current.type == EventType.MouseUp)
			{
				_windowdragflag = false;
				cameraCtrlOff = false;
			}
			if (_windowdragflag && Event.current.type == EventType.MouseDrag)
			{
				cameraCtrlOff = true;
			}

			GUILayout.BeginHorizontal();
			using (var verticalScope = new GUILayout.VerticalScope("box", GUILayout.MaxWidth(UIUtils.windowRect.width * 0.33f)))
			{
				//////////////////Load cubemaps/////////////// 
				GUI.enabled = !_isLoading;
				CubeMapModule();
				GUI.enabled = true;

			}
			GUILayout.Space(1f);
			GUILayout.BeginVertical();
			_tabMenu = GUILayout.Toolbar(_tabMenu, GUIStrings.titlebar, UIUtils.titleStyle);
			if (_tabMenu == 1 || _tabMenu == 2)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Filter effects", UIUtils.labelStyleNoStretch);

				_effectTypeDisplay = (EffectType)GUILayout.SelectionGrid((int)_effectTypeDisplay - 1, _effectTypeDisplayNames, 3, UIUtils.buttonStyleStrechWidth) + 1;
				GUILayout.EndHorizontal();
			}

			var thumbW = GUI.skin.verticalScrollbarThumb.fixedWidth;
			GUI.skin.verticalScrollbarThumb.fixedWidth = 40;
			UIUtils.scrollPosition[_tabMenu + 1] = GUILayout.BeginScrollView(UIUtils.scrollPosition[_tabMenu + 1], UIUtils.horizontalScrollbar, UIUtils.verticalScrollbar);
			using (GUILayout.VerticalScope verticalScope = new GUILayout.VerticalScope("box"/*, GUILayout.MaxHeight(Screen.height * 0.8f)*/))
			{
				GUILayout.Space(UIUtils.space);
				switch (_tabMenu)
				{
					case 0:
						////////////////////Lighting tweak/////////////////////

						if (_selectedCubemap == 0)
							ProceduralSkyboxModule();
						else
							SkyboxModule();
						UIUtils.HorizontalLine();
						ReflectionModule();
						UIUtils.HorizontalLine();
						DefaultLightModule();
						break;
					case 1:
						int tonemappingIndex = Int32.MaxValue;
						if (_toneMappingManager != null)
							tonemappingIndex = self.generalEffects.FindIndex(e => ReferenceEquals(e.effect, _toneMappingManager));

						FieldOfViewModule();
						if (_vignette != null && (_effectTypeDisplay & EffectType.FourKDiffuse) != 0)
						{
							int index = self.generalEffects.FindIndex(e => ReferenceEquals(e.effect, _vignette));

							GUILayout.BeginHorizontal();

							GUILayout.BeginVertical();
							GUILayout.Label("", GUILayout.Width(4));
							GUILayout.EndVertical();

							Rect lastRect = GUILayoutUtility.GetLastRect();

							GUILayout.BeginVertical();
							UIUtils.HorizontalLine();
							VignetteAndChromaticAberrationModule(self.effectsDictionary[_vignette], index + self.opaqueEffects.Count);
							GUILayout.EndVertical();

							Color c = GUI.color;
							GUI.color = index < tonemappingIndex ? Color.yellow : ColorPresets.Changed;
							GUI.DrawTexture(new Rect(lastRect.x, lastRect.y, lastRect.width, GUILayoutUtility.GetLastRect().height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
							GUI.color = c;

							GUILayout.EndHorizontal();
						}

						if (_lensManager != null && (_effectTypeDisplay & EffectType.LRE) != 0)
						{
							int index = self.generalEffects.FindIndex(e => e.effect == _lensManager);
							UIUtils.HorizontalLine();
							LensPresetsModule();

							GUILayout.BeginHorizontal();

							GUILayout.BeginVertical();
							GUILayout.Label("", GUILayout.Width(4));
							GUILayout.EndVertical();

							Rect lastRect = GUILayoutUtility.GetLastRect();

							GUILayout.BeginVertical();
							LensModule(null, index + self.opaqueEffects.Count);
							GUILayout.EndVertical();

							Color c = GUI.color;
							GUI.color = index < tonemappingIndex ? Color.yellow : ColorPresets.Changed;
							GUI.DrawTexture(new Rect(lastRect.x, lastRect.y, lastRect.width, GUILayoutUtility.GetLastRect().height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
							GUI.color = c;

							GUILayout.EndHorizontal();

						}
						break;
					case 2:
						GUILayout.BeginHorizontal();
						GUILayout.Label("Effects are displayed in the same order they are applied.", UIUtils.labelStyle);
						if (GUILayout.Button("Reset order", UIUtils.buttonStyleNoStretch, GUILayout.ExpandWidth(false)))
							self.ResetOrder();
						GUILayout.EndHorizontal();

						for (int i = 0; i < self.opaqueEffects.Count; i++)
						{
							EffectData data = self.opaqueEffects[i];
							if ((data.effectType & _effectTypeDisplay) == 0)
								continue;
							if (!_effectsModules.TryGetValue(data.effect, out Action<EffectData, int> moduleFunction))
								continue;
							GUILayout.BeginHorizontal();

							GUILayout.BeginVertical();
							GUILayout.Label("", GUILayout.Width(4));
							GUILayout.EndVertical();

							Rect lastRect = GUILayoutUtility.GetLastRect();

							GUILayout.BeginVertical();
							UIUtils.HorizontalLine();
							moduleFunction(data, i);
							GUILayout.EndVertical();

							Color c = GUI.color;
							GUI.color = Color.cyan;
							GUI.DrawTexture(new Rect(lastRect.x, lastRect.y, lastRect.width, GUILayoutUtility.GetLastRect().height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
							GUI.color = c;

							GUILayout.EndHorizontal();
						}
						tonemappingIndex = Int32.MaxValue;
						if (_toneMappingManager != null)
							tonemappingIndex = self.generalEffects.FindIndex(e => ReferenceEquals(e.effect, _toneMappingManager));
						for (int i = 0; i < self.generalEffects.Count; i++)
						{
							EffectData data = self.generalEffects[i];
							if ((data.effectType & _effectTypeDisplay) == 0)
								continue;
							if (ReferenceEquals(data.effect, _lensManager) || ReferenceEquals(data.effect, _vignette))
								continue;
							if (!_effectsModules.TryGetValue(data.effect, out Action<EffectData, int> moduleFunction))
								continue;
							GUILayout.BeginHorizontal();

							GUILayout.BeginVertical();
							GUILayout.Label("", GUILayout.Width(4));
							GUILayout.EndVertical();

							Rect lastRect = GUILayoutUtility.GetLastRect();

							GUILayout.BeginVertical();
							UIUtils.HorizontalLine();
							moduleFunction(data, i + self.opaqueEffects.Count);
							GUILayout.EndVertical();

							Color c = GUI.color;
							GUI.color = i < tonemappingIndex ? Color.yellow : ColorPresets.Changed;
							GUI.DrawTexture(new Rect(lastRect.x, lastRect.y, lastRect.width, GUILayoutUtility.GetLastRect().height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
							GUI.color = c;

							GUILayout.EndHorizontal();
						}
						break;
					case 3:

						GUILayout.BeginHorizontal();

						//GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();


						CharaRotateModule();
						UserCustomModule();
						break;
				}
			}
			GUILayout.EndScrollView();
			GUI.skin.verticalScrollbarThumb.fixedWidth = thumbW;

			GUILayout.FlexibleSpace();

			if (_tabMenu == 1 || _tabMenu == 2)
			{
				GUILayout.BeginHorizontal(GUI.skin.box);
				Color c = GUI.color;
				GUI.color = Color.cyan;
				GUILayout.FlexibleSpace();
				GUILayout.Label("Opaque objects", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
				GUI.color = Color.yellow;
				GUILayout.FlexibleSpace();
				GUILayout.Label("Everything HDR", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
				GUI.color = ColorPresets.Changed;
				GUILayout.FlexibleSpace();
				GUILayout.Label("Everything LDR", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
				GUILayout.FlexibleSpace();
				GUI.color = c;
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUI.DragWindow();
			if (Event.current.type == EventType.repaint)
				_currentTooltip = GUI.tooltip;
		}

		private void AnimatedSkyModule()
		{
			// Addition: kit panel only (listed under Scene). See AnimatedSky.
			AnimatedSky sky = _animatedSky;
			if (sky == null)
				return;
			bool on = IblUi.ToggleGUI(sky.On, new GUIContent("Animated Sky", "A generated sky with drifting clouds. Picking a cubemap replaces it."), IblUi.TitleStyle);
			if (on && sky.On == false && _skyBackdrop != null)
				_skyBackdrop.On = false; // one generated sky at a time
			sky.On = on;
			if (sky.On == false)
			{
				IblUi.Label("Turn it on to replace the skybox. Ambient light and reflections follow the sky's settings; the moving clouds never trigger a lighting update, so it doesn't cost frame rate.", UIUtils.labelStyle);
				return;
			}
			AnimatedSkySettings(sky);
		}

		private void SkyBackdropModule()
		{
			// Addition: kit panel only (listed under Scene). The animated sky as a background only, no lighting.
			AnimatedSky sky = _skyBackdrop;
			if (sky == null)
				return;
			bool on = IblUi.ToggleGUI(sky.On, new GUIContent("Sky Backdrop", "The animated sky as a flat background: lighting and reflections are left alone."), IblUi.TitleStyle);
			if (on && sky.On == false && _animatedSky != null)
				_animatedSky.On = false; // one generated sky at a time
			sky.On = on;
			if (sky.On == false)
			{
				IblUi.Label("Turn it on to show the animated sky behind the scene without changing its lighting: the picked cubemap (or the map's own lighting) still lights the characters and reflections.", UIUtils.labelStyle);
				return;
			}
			AnimatedSkySettings(sky);
		}

		private static void AnimatedSkySettings(AnimatedSky sky)
		{
			int preset = IblUi.SelectionGrid(sky.Preset, AnimatedSky.Presets, 4, UIUtils.buttonStyleStrechWidth);
			if (preset != sky.Preset)
				sky.SetPreset(preset);
			sky.Coverage = IblUi.SliderGUI(sky.Coverage, 0f, 1f, 0.45f, "Cloud Coverage", "How much of the sky is covered by clouds.", 2, true);
			sky.CloudSpeed = IblUi.SliderGUI(sky.CloudSpeed, 0f, 1f, 0.2f, "Cloud Speed", "How fast the clouds drift (0 = still).", 2, true);
			sky.WindAngle = IblUi.SliderGUI(sky.WindAngle, 0f, 360f, 30f, "Wind Direction", "Direction the clouds drift in.", 0, true);
			sky.SunHeight = IblUi.SliderGUI(sky.SunHeight, -10f, 90f, 55f, sky.Preset == 3 ? "Moon Height" : "Sun Height", "Height above the horizon, in degrees.", 0, true);
			sky.SunAzimuth = IblUi.SliderGUI(sky.SunAzimuth, 0f, 360f, 140f, sky.Preset == 3 ? "Moon Direction" : "Sun Direction", "Compass direction, in degrees.", 0, true);
			sky.Exposure = IblUi.SliderGUI(sky.Exposure, 0f, 2f, 1f, "Brightness", "Overall brightness of the sky.", 2, true);
		}

		private void ScreenOverlaysModule()
		{
			// Addition: kit panel only (listed under Other). See ScreenOverlays.
			ScreenOverlays o = _screenOverlays;
			if (o == null)
				return;
			o.Freeze = IblUi.ToggleGUI(o.Freeze, new GUIContent("Freeze animation", "Holds grain, VHS, glitch and light leaks still, e.g. for screenshots."));

			IblUi.HorizontalLine();
			o.GrainOn = IblUi.ToggleGUI(o.GrainOn, new GUIContent("Film Grain & Dust", "Moving film grain with dust specks and scratches."), IblUi.TitleStyle);
			if (o.GrainOn)
			{
				o.GrainStrength = IblUi.SliderGUI(o.GrainStrength, 0f, 1f, 0.25f, "Grain", "Strength of the grain.", 2, true);
				o.GrainSize = IblUi.SliderGUI(o.GrainSize, 1f, 4f, 1.5f, "Grain Size", "Size of a grain, in pixels at 1080p.", 2, true);
				o.GrainColored = IblUi.ToggleGUI(o.GrainColored, new GUIContent("Colored Grain", "Separate grain per color channel instead of gray grain."));
				o.GrainDust = IblUi.SliderGUI(o.GrainDust, 0f, 1f, 0.3f, "Dust", "How many dust specks appear.", 2, true);
				o.GrainScratches = IblUi.SliderGUI(o.GrainScratches, 0f, 1f, 0.2f, "Scratches", "How often thin vertical scratches appear.", 2, true);
			}

			IblUi.HorizontalLine();
			o.LeaksOn = IblUi.ToggleGUI(o.LeaksOn, new GUIContent("Light Leaks", "Soft colored glows bleeding in from the edges of the frame."), IblUi.TitleStyle);
			if (o.LeaksOn)
			{
				o.LeaksStrength = IblUi.SliderGUI(o.LeaksStrength, 0f, 1.5f, 0.5f, "Strength", "How bright the leaks are.", 2, true);
				o.LeaksSpeed = IblUi.SliderGUI(o.LeaksSpeed, 0f, 1f, 0.2f, "Drift Speed", "How fast the leaks move along the edges.", 2, true);
				IblUi.ColorPickerGUI(o.LeaksColor, new Color(1f, 0.55f, 0.25f), "Color", "Base color of the leaks (each leans a little warmer).", c => o.LeaksColor = c);
				if (IblUi.Button(new GUIContent("Shuffle placement", "Moves the leaks to a new random arrangement."), UIUtils.buttonStyleStrechWidth))
					o.LeaksSeed = UnityEngine.Random.Range(1f, 999f);
			}

			IblUi.HorizontalLine();
			o.CrtOn = IblUi.ToggleGUI(o.CrtOn, new GUIContent("CRT / Scanlines", "Scanlines, an RGB pixel mask and screen curvature, like an old monitor."), IblUi.TitleStyle);
			if (o.CrtOn)
			{
				o.CrtScanlines = IblUi.SliderGUI(o.CrtScanlines, 0f, 1f, 0.5f, "Scanlines", "Strength of the scanlines.", 2, true);
				o.CrtDensity = IblUi.SliderGUI(o.CrtDensity, 120f, 1080f, 360f, "Line Count", "Scanlines per screen height.", 0, true);
				o.CrtMask = IblUi.SliderGUI(o.CrtMask, 0f, 1f, 0.3f, "RGB Mask", "Strength of the red / green / blue pixel columns.", 2, true);
				o.CrtCurvature = IblUi.SliderGUI(o.CrtCurvature, 0f, 1f, 0.2f, "Curvature", "How much the screen bulges.", 2, true);
			}

			IblUi.HorizontalLine();
			o.HalftoneOn = IblUi.ToggleGUI(o.HalftoneOn, new GUIContent("Comic Halftone", "The image drawn as printed dots."), IblUi.TitleStyle);
			if (o.HalftoneOn)
			{
				o.HalftoneColored = IblUi.ToggleGUI(o.HalftoneColored, new GUIContent("Color Print", "Cyan / magenta / yellow dots instead of black ink."));
				o.HalftoneSize = IblUi.SliderGUI(o.HalftoneSize, 3f, 24f, 8f, "Dot Size", "Distance between dots, in pixels at 1080p.", 1, true);
				o.HalftoneAngle = IblUi.SliderGUI(o.HalftoneAngle, 0f, 90f, 45f, "Angle", "Angle of the dot grid.", 0, true);
				o.HalftoneMix = IblUi.SliderGUI(o.HalftoneMix, 0f, 1f, 1f, "Blend", "Mix between the original image and the print.", 2, true);
			}

			IblUi.HorizontalLine();
			o.PaperOn = IblUi.ToggleGUI(o.PaperOn, new GUIContent("Paper / Canvas", "A paper or canvas texture over the image."), IblUi.TitleStyle);
			if (o.PaperOn)
			{
				o.PaperType = IblUi.SelectionGrid(o.PaperType, ScreenOverlays.PaperTypes, 2, UIUtils.buttonStyleStrechWidth);
				o.PaperStrength = IblUi.SliderGUI(o.PaperStrength, 0f, 1f, 0.5f, "Strength", "How strongly the texture shows.", 2, true);
				o.PaperScale = IblUi.SliderGUI(o.PaperScale, 0.25f, 4f, 1f, "Scale", "Size of the texture.", 2, true);
			}

			IblUi.HorizontalLine();
			o.GlitchOn = IblUi.ToggleGUI(o.GlitchOn, new GUIContent("Glitch / RGB Split", "Color channels pulled apart, with short blocky glitch bursts."), IblUi.TitleStyle);
			if (o.GlitchOn)
			{
				o.GlitchSplit = IblUi.SliderGUI(o.GlitchSplit, 0f, 30f, 4f, "RGB Split", "Distance between the color channels, in pixels at 1080p.", 1, true);
				o.GlitchFrequency = IblUi.SliderGUI(o.GlitchFrequency, 0f, 1f, 0.2f, "Burst Frequency", "How often glitch bursts happen.", 2, true);
				o.GlitchBurst = IblUi.SliderGUI(o.GlitchBurst, 0f, 1f, 0.5f, "Burst Strength", "How strong a burst is.", 2, true);
			}

			IblUi.HorizontalLine();
			o.VhsOn = IblUi.ToggleGUI(o.VhsOn, new GUIContent("VHS", "Color bleeding, noise lines and tape wobble."), IblUi.TitleStyle);
			if (o.VhsOn)
			{
				o.VhsBleed = IblUi.SliderGUI(o.VhsBleed, 0f, 1f, 0.5f, "Color Bleed", "How far colors smear sideways.", 2, true);
				o.VhsNoise = IblUi.SliderGUI(o.VhsNoise, 0f, 1f, 0.3f, "Noise Lines", "Amount of noise and bright streaks.", 2, true);
				o.VhsWobble = IblUi.SliderGUI(o.VhsWobble, 0f, 1f, 0.3f, "Wobble", "How much the picture sways.", 2, true);
			}

			IblUi.HorizontalLine();
			o.LetterboxOn = IblUi.ToggleGUI(o.LetterboxOn, new GUIContent("Letterbox", "Black bars at a chosen aspect ratio (pillarbox when narrower than the screen)."), IblUi.TitleStyle);
			if (o.LetterboxOn)
			{
				o.LetterboxPreset = IblUi.SelectionGrid(o.LetterboxPreset, ScreenOverlays.LetterboxPresets, 5, UIUtils.buttonStyleStrechWidth);
				if (o.LetterboxPreset == ScreenOverlays.LetterboxPresets.Length - 1)
					o.LetterboxCustom = IblUi.SliderGUI(o.LetterboxCustom, 0.5f, 4f, 2.39f, "Aspect Ratio", "Width divided by height.", 2, true);
				o.LetterboxOpacity = IblUi.SliderGUI(o.LetterboxOpacity, 0f, 1f, 1f, "Opacity", "How opaque the bars are.", 2, true);
				IblUi.ColorPickerGUI(o.LetterboxColor, Color.black, "Bar Color", "Color of the bars.", c => o.LetterboxColor = c);
			}
		}

		private void ColorLooksModule()
		{
			// Addition: kit panel only (listed under Other). Two looks baked into one LUT, see ColorLooks.
			if (_colorLooks == null)
				return;
			if (_toneMappingManager == null)
			{
				IblUi.Label("Color Looks need the Tonemapping & Color Grading effect, which is not available here.", UIUtils.labelStyle);
				return;
			}
			IblUi.Label("Look", IblUi.TitleStyle);
			_colorLooks.Look1 = IblUi.Dropdown(_colorLooks.Look1, _colorLooks.Names, "Color grade applied to the whole image");
			_colorLooks.Strength1 = IblUi.SliderGUI(_colorLooks.Strength1, 0f, 1f, 1f, "Strength", "How strongly the look is applied.", 2, true);
			IblUi.HorizontalLine();
			IblUi.Label("Second Look", IblUi.TitleStyle);
			_colorLooks.Look2 = IblUi.Dropdown(_colorLooks.Look2, _colorLooks.Names, "Applied on top of the first look");
			_colorLooks.Strength2 = IblUi.SliderGUI(_colorLooks.Strength2, 0f, 1f, 1f, "Strength", "How strongly the second look is applied.", 2, true);
			IblUi.HorizontalLine();
			IblUi.Label("Your own LUTs: strip images (width = height x height, e.g. 1024x32) in UserData\\LUT\\ are listed after the built-in looks.", UIUtils.labelStyle);
			IblUi.BeginHorizontal();
			if (IblUi.Button("Reload LUT folder", UIUtils.buttonStyleStrechWidth))
				_colorLooks.Reload();
			if (IblUi.Button("Open LUT folder", UIUtils.buttonStyleStrechWidth))
			{
				try
				{
					Directory.CreateDirectory(ColorLooks.LutFolder);
					System.Diagnostics.Process.Start(ColorLooks.LutFolder);
				}
				catch (Exception e)
				{
					Debug.LogError("HSLRE: could not open the LUT folder.\n" + e);
				}
			}
			IblUi.EndHorizontal();
		}

		private void FieldOfViewModule()
		{
			// note: must avoid competing with other logic that can also set the camera FOV to float values that have higher precision than 1 decimal
			if (_isStudio)
			{
				float currentFov = Studio.Studio.Instance.cameraCtrl.fieldOfView;
				var newFov = IblUi.SliderGUI(currentFov, 1f, 179f, 23f, "Field of View", "The vertical field of view of the Camera, in degrees.\r\nThis is the vertical field of view; horizontal field of view depends on the viewport's aspect ratio.", 5);

				if (Mathf.Abs(newFov - currentFov) >= 0.01f)
				{
					Studio.Studio.Instance.cameraCtrl.fieldOfView = newFov;
				}
			}
			else
			{
				float currentFov = cam.fieldOfView;
				var newFov = IblUi.SliderGUI(currentFov, 1f, 179f, 23f, "Field of View", "The vertical field of view of the Camera, in degrees.\r\nThis is the vertical field of view; horizontal field of view depends on the viewport's aspect ratio.", 5);
				if (Mathf.Abs(newFov - currentFov) >= 0.01f)
				{
					cam.fieldOfView = newFov;
					if (_subCamera != null)
					{
						_subCamera.fieldOfView = cam.fieldOfView;
					}
				}
			}
		}

		private void CubeMapModule()
		{
			IblUi.BeginHorizontal();
			IblUi.Label("Load Cubemaps:", UIUtils.labelStyle);
			IblUi.FlexibleSpace();
			_hideSkybox = IblUi.ToggleButton(_hideSkybox, new GUIContent("Hide", "Hide skybox in the background"));
			IblUi.EndHorizontal();


			float thumbW = 0f, thumbH = 0f;
			if (IblUi.Classic)
			{
				thumbW = GUI.skin.verticalScrollbarThumb.fixedWidth;
				thumbH = GUI.skin.horizontalScrollbarThumb.fixedHeight;
				GUI.skin.verticalScrollbarThumb.fixedWidth = 40;
				GUI.skin.horizontalScrollbarThumb.fixedHeight = 40;

				UIUtils.scrollPosition[0] = GUILayout.BeginScrollView(UIUtils.scrollPosition[0], UIUtils.horizontalScrollbar, UIUtils.verticalScrollbar);
			}

			if (IblUi.Classic)
			{
				if (IblUi.Button("None", UIUtils.buttonStyleStrechWidthAlignLeft))
					_selectedCubemap = -1;

				_selectedCubemap = IblUi.SelectionGrid(_selectedCubemap, _cubeMapFileNames, 1, UIUtils.buttonStyleStrechWidthAlignLeft);
			}
			else
			{
				// Addition: the kit panel shows one dropdown with "None" as its first option (index shifted by one)
				if (_cubeMapNamesWithNone == null || _cubeMapNamesWithNone.Length != _cubeMapFileNames.Length + 1)
				{
					_cubeMapNamesWithNone = new string[_cubeMapFileNames.Length + 1];
					_cubeMapNamesWithNone[0] = "None";
					Array.Copy(_cubeMapFileNames, 0, _cubeMapNamesWithNone, 1, _cubeMapFileNames.Length);
				}
				_selectedCubemap = IblUi.Dropdown(_selectedCubemap + 1, _cubeMapNamesWithNone, "Cubemap used for the skybox and the image based lighting") - 1;
			}

			_selectedCubemap = LoadCubemap(_selectedCubemap);

			if (IblUi.Classic)
			{
				GUILayout.EndScrollView();

				GUI.skin.verticalScrollbarThumb.fixedWidth = thumbW;
				GUI.skin.horizontalScrollbarThumb.fixedHeight = thumbH;
			}
		}

		private void ProceduralSkyboxModule()
		{
			IblUi.Label("Procedural Skybox", IblUi.TitleStyle);
			IblUi.Space(UIUtils.space);
			_proceduralSkybox.skyboxparams.exposure = IblUi.SliderGUI(_proceduralSkybox.skyboxparams.exposure, 0f, 8f, 1f, "Skybox Exposure:", "Adjusts the sky’s exposure. This allows you to change tonal values in the skybox this Material generates. Larger values produce a more exposed, seemingly brighter, skybox. Smaller values produce a less exposed, seemingly darker, skybox.", 3);
			_proceduralSkybox.skyboxparams.sunsize = IblUi.SliderGUI(_proceduralSkybox.skyboxparams.sunsize, 0f, 1f, 0.1f, "Sun Size :", "The size modifier for the sun disk. Larger values make the sun disk appear larger and setting this value to 0 makes the sun disk disappear.", 3);
			// addition: the sun's position (HS never passed the stock shader a sun, so it had none)
			_proceduralSkybox.skyboxparams.sunheight = IblUi.SliderGUI(_proceduralSkybox.skyboxparams.sunheight, -10f, 90f, 35f, "Sun Height", "Height of the sun above the horizon, in degrees. Low values give a sunset sky.", 0, true);
			_proceduralSkybox.skyboxparams.sunazimuth = IblUi.SliderGUI(_proceduralSkybox.skyboxparams.sunazimuth, 0f, 360f, 140f, "Sun Direction", "Compass direction of the sun, in degrees.", 0, true);
			_proceduralSkybox.skyboxparams.atmospherethickness = IblUi.SliderGUI(_proceduralSkybox.skyboxparams.atmospherethickness, 0f, 5f, 1f, "Atmosphere Thickness:", "The density of the atmosphere. An atmosphere of higher density absorbs more light. Unity uses the Rayleigh scattering method to absorb light.", 3);
			IblUi.ColorPickerGUI(_proceduralSkybox.skyboxparams.skytint, Color.gray, "Sky Tint:", "The color to tint the sky to.", (c) =>
			  {
				  _proceduralSkybox.skyboxparams.skytint = c;
			  });
			IblUi.ColorPickerGUI(_proceduralSkybox.skyboxparams.groundcolor, Color.gray, "Gound Color:", "The color of the ground (the area below the horizon).", (c) =>
			  {
				  _proceduralSkybox.skyboxparams.groundcolor = c;
			  });
			RenderSettings.ambientIntensity = IblUi.SliderGUI(RenderSettings.ambientIntensity, 0f, 2f, 1f, "Ambient Intensity:", "How much the light from the Ambient Source affects the Scene.", 3);
		}

		private void SkyboxModule()
		{
			IblUi.Label("Skybox", IblUi.TitleStyle);
			IblUi.Space(UIUtils.space);
			_skybox.skyboxparams.rotation = IblUi.SliderGUI(_skybox.skyboxparams.rotation, 0f, 360f, 0f, "Skybox Rotation:", "Rotation of the skybox around the Y (up) axis.", 2);
			_skybox.skyboxparams.exposure = IblUi.SliderGUI(_skybox.skyboxparams.exposure, 0f, 8f, 1f, "Skybox Exposure:", "Adjusts the sky’s exposure. This allows you to change tonal values in the skybox this Material generates. Larger values produce a more exposed, seemingly brighter, skybox. Smaller values produce a less exposed, seemingly darker, skybox.", 3);
			IblUi.ColorPickerGUI(_skybox.skyboxparams.tint, Color.gray, "Skybox Tint:", "The color to tint the sky to.", c =>
			  {
				  _skybox.skyboxparams.tint = c;
			  });
			RenderSettings.ambientIntensity = IblUi.SliderGUI(RenderSettings.ambientIntensity, 0f, 2f, 1f, "Ambient Intensity:", "How much the light from the Ambient Source affects the Scene.", 3);
		}

		private void ReflectionModule()
		{
			_probeComponent.enabled = IblUi.ToggleGUI(_probeComponent.enabled, new GUIContent(GUIStrings.reflection), IblUi.TitleStyle);

			if (_probeComponent.enabled)
			{
				IblUi.Space(UIUtils.space);

				if (_probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting && IblUi.Button(GUIStrings.reflectionProbeRefresh, UIUtils.buttonStyleNoStretch))
				{
					_probeComponent.RenderProbe();
				}

				if (_probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
				{
					_reflectionProbeRefreshRate = 0;
				}
				else if (_probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.AllFacesAtOnce)
				{
					_reflectionProbeRefreshRate = 2;
				}
				else if (_probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.IndividualFaces)
				{
					_reflectionProbeRefreshRate = 1;
				}
				else if (_probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.NoTimeSlicing)
				{
					_reflectionProbeRefreshRate = 3;
				}
				_reflectionProbeRefreshRate = IblUi.SelectionGrid(_reflectionProbeRefreshRate, GUIStrings.reflectionProbeRefreshRateArray, 4, UIUtils.selectStyle);

				switch (_reflectionProbeRefreshRate)
				{
					default:
					case 0:
						_probeComponent.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
						break;
					case 1:
						_probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
						_probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
						break;
					case 2:
						_probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
						_probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
						break;
					case 3:
						_probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
						_probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
						break;
				}
				IblUi.Label(GUIStrings.reflectionProbeResolution, UIUtils.labelStyle);
				_reflectionProbeResolution = 0;
				for (int i = 0; i < _possibleReflectionProbeResolutions.Length; i++)
				{
					if (_possibleReflectionProbeResolutions[i] == _probeComponent.resolution)
					{
						_reflectionProbeResolution = i;
						break;
					}
				}

				_reflectionProbeResolution = IblUi.SelectionGrid(_reflectionProbeResolution, _possibleReflectionProbeResolutionsNames, _possibleReflectionProbeResolutions.Length, UIUtils.selectStyle);

				_probeComponent.resolution = _possibleReflectionProbeResolutions[_reflectionProbeResolution];
				if (_isStudio)
				{
					IblUi.BeginHorizontal();
					if (IblUi.Button("Move Reflection Probe to target", UIUtils.buttonStyleNoStretch))
					{
						probeGameObject.transform.position = Studio.Studio.Instance.cameraCtrl.targetTex.position;
						if (_probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
							_probeComponent.RenderProbe();
					}
					IblUi.FlexibleSpace();
					IblUi.Label(probeGameObject.transform.position.ToString(), UIUtils.labelStyle);
					IblUi.EndHorizontal();
				}
				IblUi.Space(UIUtils.space);
				_probeComponent.boxProjection = IblUi.ToggleGUI(_probeComponent.boxProjection, new GUIContent("Box Projection", "Box Projection is useful for reflections in enclosed spaces  where some parrallax and movement in the reflection is wanted. If not endbled then cubemap reflection will be treated as coming infinite far away. And within this zone objects with the Standard shader will receive this probe's cubemap."));

				_probeComponent.intensity = IblUi.SliderGUI(_probeComponent.intensity, 0f, 2f, 1f, GUIStrings.reflectionIntensity, "The intensity modifier that is applied to the texture of this probe in its shader.", 3);

				_probeComponent.shadowDistance = IblUi.SliderGUI(_probeComponent.shadowDistance, 1f, 100f, 100f, "Shadow Distance", "Distance at which shadows are drawn when rendering the probe.", 1);

				_probeComponent.nearClipPlane = IblUi.SliderGUI(_probeComponent.nearClipPlane, 0f, 25f, 1.01f, "Near Clip Plane", "Distances from the camera to start and stop rendering.\r\nThe closest point relative to the camera that drawing will occur.", 3);

				RenderSettings.reflectionBounces = Mathf.RoundToInt(IblUi.SliderGUI(RenderSettings.reflectionBounces, 1, 5, 1, "Reflection Bounces",
					"The number of times a reflection includes other reflections. " +
					"If set to 1, the scene will be rendered once, which means that a reflection will not be able to reflect another reflection and reflective objects will show up black, when seen in other reflective surfaces. " +
					"If set to 2, the scene will be rendered twice and reflective objects will show reflections from the first pass, when seen in other reflective surfaces.\r\n\r\n" +
					"Generig rendering setting; not specific to the reflection probe.", 0, true));

				_probeComponent.cullingMask = IblUi.LayerMaskValue(_probeComponent.cullingMask, new GUIContent("Culling Mask", "Allows objects on specified layers to be included or excluded in the reflection."), _layers, "D", "E");
			}
		}

		private void DefaultLightModule()
		{
			IblUi.Label("Directional Light", IblUi.TitleStyle);
			IblUi.Space(UIUtils.space);
			IblUi.BeginHorizontal();
			IblUi.Label("Front Light", UIUtils.labelStyle);
			IblUi.FlexibleSpace();
			bool lastFrontLightAnchor = _frontDirectionalLight.transform.parent != null;
			_frontLightAnchor = IblUi.ToggleButton(lastFrontLightAnchor, new GUIContent("Rotate with camera"));
			IblUi.EndHorizontal();

			if (_frontLightAnchor)
			{
				_lightsObj.transform.parent = cam.transform;
				_frontDirectionalLight.transform.parent = _lightsObj.transform;
				if (_frontLightAnchor != lastFrontLightAnchor)
				{
					_lightsObj.transform.localRotation = _isStudio && Studio.Studio.Instance != null ? Quaternion.Euler(Studio.Studio.Instance.sceneInfo.cameraLightRot[0], Studio.Studio.Instance.sceneInfo.cameraLightRot[1], 0f) : _lightsObjDefaultRotation;
					_frontDirectionalLight.transform.localRotation = _frontLightDefaultRotation;
				}
			}
			else
			{
				_lightsObj.transform.parent = null;
				_frontDirectionalLight.transform.parent = null;
				_frontRotate.x = IblUi.SliderGUI(_frontRotate.x, 0f, 360f, 0f, "Vertical rotation", "Vertical rotation", 1);
				_frontRotate.y = IblUi.SliderGUI(_frontRotate.y, 0f, 360f, 0f, "Horizontal rotation", "Horizontal rotation", 1);
				_frontDirectionalLight.transform.eulerAngles = _frontRotate;
			}
			_frontDirectionalLight.intensity = IblUi.SliderGUI(_frontDirectionalLight.intensity, 0f, 8f, 1f, "Intensity:", "The Intensity of a light is multiplied with the Light color.\r\nThe value can be between 0 and 8. This allows you to create over bright lights.", 3, true);
			IblUi.ColorPickerGUI(_frontDirectionalLight.color, new Color(0.863f, 0.823f, 0.802f, 1f), "Color:", "The color of the light", c =>
			 {
				 _frontDirectionalLight.color = c;
			 });

			IblUi.BeginHorizontal();
			IblUi.Label("Back Light", UIUtils.labelStyle);
			IblUi.FlexibleSpace();
			bool lastBackLightAnchor = _backDirectionalLight.transform.parent != null;
			_backLightAnchor = IblUi.ToggleButton(lastBackLightAnchor, new GUIContent("Rotate with camera"));
			IblUi.EndHorizontal();
			if (_backLightAnchor)
			{
				_backDirectionalLight.transform.parent = cam.transform;
				if (_backLightAnchor != lastBackLightAnchor)
					_backDirectionalLight.transform.localRotation = _backLightDefaultRotation;
			}
			else
			{
				_backDirectionalLight.transform.parent = null;
				_backRotate.x = IblUi.SliderGUI(_backRotate.x, 0f, 360f, 0f, "Vertical rotation", "", 1);
				_backRotate.y = IblUi.SliderGUI(_backRotate.y, 0f, 360f, 0f, "Horizontal rotation", "", 1);
				_backDirectionalLight.transform.eulerAngles = _backRotate;
			}
			_backDirectionalLight.intensity = IblUi.SliderGUI(_backDirectionalLight.intensity, 0f, 8f, 1f, "Intensity:", "The Intensity of a light is multiplied with the Light color.\r\nThe value can be between 0 and 8. This allows you to create over bright lights.", 3, true);
			IblUi.ColorPickerGUI(_backDirectionalLight.color, new Color(0.824f, 0.824f, 0.824f, 1f), GUIStrings.color, "The color of the light.", c =>
			 {
				 _backDirectionalLight.color = c;
			 });
		}

		private void LensPresetsModule()
		{
			IblUi.Label("Lens Presets", UIUtils.labelStyle);
			IblUi.Space(UIUtils.space);
			IblUi.BeginHorizontal();
			if (IblUi.Button("85mm", UIUtils.buttonStyleStrechWidth))
			{
				cam.fieldOfView = 23.9f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = cam.fieldOfView;
				if (_subCamera != null)
				{
					_subCamera.fieldOfView = cam.fieldOfView;
				}
				_lensManager.distortion.enabled = false;
				_lensManager.vignette.enabled = true;
				_lensManager.vignette.intensity = 0.7f;
				_lensManager.vignette.color = Color.black;
				_lensManager.vignette.blur = 0f;
				_lensManager.vignette.desaturate = 0f;
				_lensManager.vignette.roundness = 0.5625f;
				_lensManager.vignette.smoothness = 2f;
			}
			if (IblUi.Button("50mm", UIUtils.buttonStyleStrechWidth))
			{
				cam.fieldOfView = 39.6f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = cam.fieldOfView;
				if (_subCamera != null)
				{
					_subCamera.fieldOfView = cam.fieldOfView;
				}
				_lensManager.distortion.enabled = false;
				_lensManager.vignette.enabled = true;
				_lensManager.vignette.intensity = 1f;
				_lensManager.vignette.color = Color.black;
				_lensManager.vignette.blur = 0f;
				_lensManager.vignette.desaturate = 0f;
				_lensManager.vignette.roundness = 0.5625f;
				_lensManager.vignette.smoothness = 2f;
			}
			if (IblUi.Button("35mm", UIUtils.buttonStyleStrechWidth))
			{
				cam.fieldOfView = 57.9f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = cam.fieldOfView;
				if (_subCamera != null)
				{
					_subCamera.fieldOfView = cam.fieldOfView;
				}
				_lensManager.distortion.enabled = false;
				_lensManager.vignette.enabled = true;
				_lensManager.vignette.intensity = 1.6f;
				_lensManager.vignette.color = Color.black;
				_lensManager.vignette.blur = 0f;
				_lensManager.vignette.desaturate = 0f;
				_lensManager.vignette.roundness = 0.5625f;
				_lensManager.vignette.smoothness = 1.6f;
			}
			if (IblUi.Button("24mm", UIUtils.buttonStyleStrechWidth))
			{
				cam.fieldOfView = 85.5f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = cam.fieldOfView;
				if (_subCamera != null)
				{
					_subCamera.fieldOfView = cam.fieldOfView;
				}
				_lensManager.distortion.enabled = true;
				_lensManager.distortion.amount = 25f;
				_lensManager.distortion.amountX = 1f;
				_lensManager.distortion.amountY = 1f;
				_lensManager.distortion.scale = 1.025f;
				_lensManager.vignette.enabled = true;
				_lensManager.vignette.intensity = 1.8f;
				_lensManager.vignette.color = Color.black;
				_lensManager.vignette.blur = 0.1f;
				_lensManager.vignette.desaturate = 0f;
				_lensManager.vignette.roundness = 0.187f;
				_lensManager.vignette.smoothness = 1.4f;
			}
			if (IblUi.Button("16mm", UIUtils.buttonStyleStrechWidth))
			{
				cam.fieldOfView = 132.6f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = cam.fieldOfView;
				if (_subCamera != null)
				{
					_subCamera.fieldOfView = cam.fieldOfView;
				}
				_lensManager.distortion.enabled = true;
				_lensManager.distortion.amount = 69f;
				_lensManager.distortion.amountX = 1f;
				_lensManager.distortion.amountY = 1f;
				_lensManager.distortion.scale = 1.05f;
				_lensManager.vignette.enabled = true;
				_lensManager.vignette.intensity = 1.95f;
				_lensManager.vignette.color = Color.black;
				_lensManager.vignette.blur = 0.14f;
				_lensManager.vignette.desaturate = 0.14f;
				_lensManager.vignette.roundness = 0.814f;
				_lensManager.vignette.smoothness = 1.143f;
			}
			IblUi.EndHorizontal();
		}

		private void LensModule(EffectData effectData, int index)
		{
			//IblUi.Space(UIUtils.space);
			//////////////////////Lens Aberration/////////////////

			IblUi.HorizontalLine();
			_lensManager.distortion.enabled = IblUi.ToggleGUITitle(_lensManager.distortion.enabled, new GUIContent(index + " Distortion"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(_lensManager), () => self.MoveDown(_lensManager));
			if (_lensManager.distortion.enabled)
			{
				_lensManager.distortion.amount = IblUi.SliderGUI(_lensManager.distortion.amount, -100f, 100f, 0f, "Distortion amount", "Distortion amount.", 2, true);
				_lensManager.distortion.amountX = IblUi.SliderGUI(_lensManager.distortion.amountX, 0f, 1f, 1f, "Amount multiplier on X axis", "Amount multiplier on X axis. Set it to 0 to disable distortion on this axis.", 3, true);
				_lensManager.distortion.amountY = IblUi.SliderGUI(_lensManager.distortion.amountY, 0f, 1f, 1f, "Amount multiplier on Y axis", "Amount multiplier on Y axis. Set it to 0 to disable distortion on this axis.", 3, true);
				_lensManager.distortion.scale = IblUi.SliderGUI(_lensManager.distortion.scale, 0.01f, 5f, 1f, "Global screen scale", "Global screen scaling.", 3, true);
			}
			//IblUi.Space(UIUtils.space);
			IblUi.HorizontalLine();

			_lensManager.chromaticAberration.enabled = IblUi.ToggleGUITitle(_lensManager.chromaticAberration.enabled, new GUIContent((index + 0.1f).ToString("0.0") + " " + GUIStrings.chromaticAberration), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(_lensManager), () => self.MoveDown(_lensManager));
			if (_lensManager.chromaticAberration.enabled)
			{
				_lensManager.chromaticAberration.amount = IblUi.SliderGUI(_lensManager.chromaticAberration.amount, -50f, 50f, 0f, "Tangential distortion Amount", "Amount of tangential distortion.", 3, true);
				IblUi.ColorPickerGUI(_lensManager.chromaticAberration.color, Color.green, "Color", "Channels to apply chromatic aberration to.", (c) =>
				{
					_lensManager.chromaticAberration.color = c;
				});
			}
			//IblUi.Space(UIUtils.space);
			IblUi.HorizontalLine();
			_lensManager.vignette.enabled = IblUi.ToggleGUITitle(_lensManager.vignette.enabled, new GUIContent((index + 0.2f).ToString("0.0") + " " + GUIStrings.vignette), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(_lensManager), () => self.MoveDown(_lensManager));
			if (_lensManager.vignette.enabled)
			{
				_lensManager.vignette.intensity = IblUi.SliderGUI(_lensManager.vignette.intensity, 0f, 3f, 1.4f, "Intensity", "Amount of vignetting on screen.", 3, true);
				_lensManager.vignette.smoothness = IblUi.SliderGUI(_lensManager.vignette.smoothness, 0.01f, 3f, 0.8f, "Smothness", "Smoothness of the vignette borders.", 3, true);
				_lensManager.vignette.roundness = IblUi.SliderGUI(_lensManager.vignette.roundness, 0f, 1f, 1f, "Roundness", "Lower values will make a square-ish vignette.", 3, true);
				_lensManager.vignette.desaturate = IblUi.SliderGUI(_lensManager.vignette.desaturate, 0f, 1f, 0f, "Desaturate", "Desaturate the corners of the screen. Leave this to 0 to disable it.", 3, true);
				_lensManager.vignette.blur = IblUi.SliderGUI(_lensManager.vignette.blur, 0f, 1f, 0f, "Blur Corner", "Blurs the corners of the screen. Leave this at 0 to disable it.", 3, true);

				IblUi.ColorPickerGUI(_lensManager.vignette.color, Color.black, GUIStrings.vignetteColor, "Vignette color. Use the alpha channel for transparency.", (c) =>
				{
					_lensManager.vignette.color = c;
				});
			}
		}

		private void ToneMappingColorGradingModule(EffectData effectData, int index)
		{
			EyeAdaptationModule(effectData, index);
			IblUi.HorizontalLine();
			ToneMappingModule(effectData, index + 0.1f);
			IblUi.HorizontalLine();
			ColorGradingModule(effectData, index + 0.2f);
		}

		private void EyeAdaptationModule(EffectData effectData, int index)
		{
			_eyeEnabled = IblUi.ToggleGUITitle(_toneMappingManager.eyeAdaptation.enabled, new GUIContent(index + " " + GUIStrings.eyeAdaptation.text, GUIStrings.eyeAdaptation.tooltip), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));
			if (_eyeEnabled)
			{
				IblUi.Space(UIUtils.space);
				//_eyeMiddleGrey = Mathf.Max(0, UIUtils.SliderGUI2(_toneMappingManager.eyeAdaptation.middleGrey, 0f, 0.5f, 0.1f, "Middle Grey", "Midpoint Adjustment.\r\n\r\n[Min: 0]", "N3"));
				_eyeMiddleGrey = IblUi.SliderGUI(_toneMappingManager.eyeAdaptation.middleGrey, 0f, 0.5f, 0.1f, "Middle Grey", "Midpoint Adjustment.", 3, false, true);
				_eyeMin = IblUi.SliderGUI(_toneMappingManager.eyeAdaptation.min, -8f, 0f, -4f, "Lowest Exposure Value", "The lowest possible exposure value; adjust this value to modify the brightest areas of your level.", 3);
				_eyeMax = IblUi.SliderGUI(_toneMappingManager.eyeAdaptation.max, 0f, 8f, 4f, "Highest Exposure Value", "The highest possible exposure value; adjust this value to modify the darkest areas of your level.", 3);
				//_eyeSpeed = Mathf.Max(0, UIUtils.SliderGUI2(_toneMappingManager.eyeAdaptation.speed, 0f, 8f, 0f, "Adaptation Speed", "Speed of linear adaptation. Higher is faster.\r\n\r\n[Min: 0]", "N3"));
				_eyeSpeed = IblUi.SliderGUI(_toneMappingManager.eyeAdaptation.speed, 0f, 8f, 0f, "Adaptation Speed", "Speed of linear adaptation. Higher is faster.", 3, false, true);
			}
			_toneMappingManager.eyeAdaptation = new TonemappingColorGrading.EyeAdaptationSettings
			{
				enabled = _eyeEnabled,
				showDebug = false,
				middleGrey = _eyeMiddleGrey,
				max = _eyeMax,
				min = _eyeMin,
				speed = _eyeSpeed
			};
		}

		private void ToneMappingModule(EffectData effectData, float i)
		{
			_tonemappingEnabled = IblUi.ToggleGUITitle(_toneMappingManager.tonemapping.enabled, new GUIContent(i.ToString("0.0") + " " + GUIStrings.tonemapping.text, GUIStrings.tonemapping.tooltip), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (_tonemappingEnabled)
			{
				IblUi.Space(UIUtils.space);
				int index = 0;
				switch (_toneMappingManager.tonemapping.tonemapper)
				{
					default:
					case TonemappingColorGrading.Tonemapper.ACES:
						index = 0;
						break;
					case TonemappingColorGrading.Tonemapper.Hable:
						index = 1;
						break;
					case TonemappingColorGrading.Tonemapper.HejlDawson:
						index = 2;
						break;
					case TonemappingColorGrading.Tonemapper.Photographic:
						index = 3;
						break;
					case TonemappingColorGrading.Tonemapper.Reinhard:
						index = 4;
						break;
				}
				IblUi.Space(5f);
				index = IblUi.SelectionGrid(index, new string[]
				{
					"ACES",
					"Hable",
					"HejlDawson",
					"Photographic",
					"Reinhard"
				}, 3, UIUtils.selectStyle);

				switch (index)
				{
					default:
					case 0:
						_toneMapper = TonemappingColorGrading.Tonemapper.ACES;
						break;
					case 1:
						_toneMapper = TonemappingColorGrading.Tonemapper.Hable;
						break;
					case 2:
						_toneMapper = TonemappingColorGrading.Tonemapper.HejlDawson;
						break;
					case 3:
						_toneMapper = TonemappingColorGrading.Tonemapper.Photographic;
						break;
					case 4:
						_toneMapper = TonemappingColorGrading.Tonemapper.Reinhard;
						break;
				}
				_ev = IblUi.SliderGUI(_ev, -5f, 5f, 0f,GUIStrings.exposureValue, "Adjusts the overall exposure of the scene.", 3);
			}

			_toneMappingManager.tonemapping = new TonemappingColorGrading.TonemappingSettings
			{
				tonemapper = _toneMapper,
				exposure = Mathf.Pow(2f, _ev),
				enabled = _tonemappingEnabled,
				neutralBlackIn = _toneMappingManager.tonemapping.neutralBlackIn,
				neutralBlackOut = _toneMappingManager.tonemapping.neutralBlackOut,
				neutralWhiteClip = _toneMappingManager.tonemapping.neutralWhiteClip,
				neutralWhiteIn = _toneMappingManager.tonemapping.neutralWhiteIn,
				neutralWhiteLevel = _toneMappingManager.tonemapping.neutralWhiteLevel,
				neutralWhiteOut = _toneMappingManager.tonemapping.neutralWhiteOut,
				curve = _toneMappingManager.tonemapping.curve
			};

		}

		private void ColorGradingModule(EffectData effectData, float index)
		{
			TonemappingColorGrading.ColorGradingSettings colorGrading = _toneMappingManager.colorGrading;
			colorGrading.enabled = IblUi.ToggleGUITitle(colorGrading.enabled, new GUIContent(index.ToString("0.0") + " Color Grading", "Color Grading is the process of altering or correcting the color and luminance of the final image."), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (colorGrading.enabled)
			{
				TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;

				settings.temperatureShift = IblUi.SliderGUI(settings.temperatureShift, -2f, 2f, Settings.tonemappingSettings.temperatureShift, GUIStrings.tonemappingTemperatureShift, "Sets the white balance to a custom color temperature.", 2, true);
				settings.tint = IblUi.SliderGUI(settings.tint, -2f, 2f, Settings.tonemappingSettings.tint, GUIStrings.tonemappingTint, "Sets the white balance to compensate for a green or magenta tint.", 2, true);
				settings.contrast = IblUi.SliderGUI(settings.contrast, 0f, 5f, Settings.tonemappingSettings.contrast, GUIStrings.tonemappingContrast, "Expands or shrinks the overall range of tonal values.", 2, true);
				settings.hue = IblUi.SliderGUI(settings.hue, -0.5f, 0.5f, Settings.tonemappingSettings.hue, GUIStrings.tonemappingHue, "Shift the hue of all colors.", 2, true);
				settings.saturation = IblUi.SliderGUI(settings.saturation, 0f, 3f, Settings.tonemappingSettings.saturation, GUIStrings.tonemappingSaturation, "Pushes the intensity of all colors.", 2, true);
				settings.value = IblUi.SliderGUI(settings.value, 0f, 10f, Settings.tonemappingSettings.value, GUIStrings.tonemappingValue, "Brightens or darkens all colors.", 2, true);
				settings.vibrance = IblUi.SliderGUI(settings.vibrance, -1f, 1f, Settings.tonemappingSettings.vibrance, GUIStrings.tonemappingVibrance, "Adjusts the saturation so that clipping is minimized as colors approach full saturation.", 2, true);
				settings.gain = IblUi.SliderGUI(settings.gain, 0f, 5f, Settings.tonemappingSettings.gain, GUIStrings.tonemappingGain, "Contrast gain curve. Controls the steepness of the curve.", 2, true);
				settings.gamma = IblUi.SliderGUI(settings.gamma, 0f, 5f, Settings.tonemappingSettings.gamma, GUIStrings.tonemappingGamma, "Applies a pow function to the source.", 2, true);
				colorGrading.basics = settings;
			}

			// perf pass: assigning marks the LUT dirty, which re-bakes it (curve texture + LUT blit + garbage); this
			// page runs every frame while shown, so only assign when something on it actually changed
			TonemappingColorGrading.ColorGradingSettings current = _toneMappingManager.colorGrading;
			if (colorGrading.enabled != current.enabled || SameBasics(colorGrading.basics, current.basics) == false)
				_toneMappingManager.colorGrading = colorGrading;
		}

		private static bool SameBasics(TonemappingColorGrading.BasicsSettings a, TonemappingColorGrading.BasicsSettings b)
		{
			return a.temperatureShift == b.temperatureShift && a.tint == b.tint && a.hue == b.hue && a.saturation == b.saturation
				&& a.vibrance == b.vibrance && a.value == b.value && a.contrast == b.contrast && a.gain == b.gain && a.gamma == b.gamma;
		}

		private void SMAAModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " SMAA"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				IblUi.Space(UIUtils.space);
				SMAA.GlobalSettings settings = _smaa.settings;
				SMAA.PredicationSettings predication = _smaa.predication;
				SMAA.TemporalSettings temporal = _smaa.temporal;

				IblUi.Label(new GUIContent("Debug Pass", "Use this to fine tune your settings when working in Custom quality mode. \"Accumulation\" only works when \"Temporal Filtering\" is enabled."), UIUtils.labelStyle);
				settings.debugPass = (SMAA.DebugPass)IblUi.SelectionGrid((int)settings.debugPass, _possibleSMAADebugPassNames, 3, UIUtils.buttonStyleStrechWidth);
				IblUi.Label(new GUIContent("Quality", "Low: 60% of the quality.\nMedium: 80% of the quality.\nHigh: 95% of the quality.\nUltra: 99% of the quality (overkill)."), UIUtils.labelStyle);
				settings.quality = (SMAA.QualityPreset)IblUi.SelectionGrid((int)settings.quality, _possibleSMAAQualityPresetNames, 5, UIUtils.buttonStyleStrechWidth);
				IblUi.Label(new GUIContent("Edge Detection Method", "You have three edge detection methods to choose from: luma, color or depth.\nThey represent different quality/performance and anti-aliasing/sharpness tradeoffs, so our recommendation is for you to choose the one that best suits your particular scenario:\n\n- Depth edge detection is usually the fastest but it may miss some edges.\n- Luma edge detection is usually more expensive than depth edge detection, but catches visible edges that depth edge detection can miss.\n- Color edge detection is usually the most expensive one but catches chroma-only edges."), UIUtils.labelStyle);
				settings.edgeDetectionMethod = (SMAA.EdgeDetectionMethod)IblUi.SelectionGrid((int)settings.edgeDetectionMethod, _possibleSMAAEdgeDetectionMethodNames, 3, UIUtils.buttonStyleStrechWidth);
				if (settings.quality == SMAA.QualityPreset.Custom)
				{
					SMAA.QualitySettings quality = _smaa.quality;
					quality.diagonalDetection = IblUi.ToggleGUI(quality.diagonalDetection, new GUIContent("Diagonal Detection", "Enables/Disables diagonal processing."));
					quality.cornerDetection = IblUi.ToggleGUI(quality.cornerDetection, new GUIContent("Corner Detection", "Enables/Disables corner detection. Leave this on to avoid blurry corners."));
					quality.threshold = IblUi.SliderGUI(quality.threshold, 0f, 0.5f, 0.01f, "Threshold", "Filters out pixels under this level of brightness.", 3, true);
					//quality.depthThreshold = Mathf.Max(0.0001f, UIUtils.SliderGUI2(quality.depthThreshold, 0.0001f, 10f, 0.01f, "Depth Threshold", "Specifies the threshold for depth edge detection. Lowering this value you will be able to detect more edges at the expense of performance.\r\n[Min: 0.0001]", "N4"));
					quality.depthThreshold = IblUi.SliderGUI(quality.depthThreshold, 0.0001f, 10f, 0.01f, "Depth Threshold", "Specifies the threshold for depth edge detection. Lowering this value you will be able to detect more edges at the expense of performance.", 4, false, true);
					quality.maxSearchSteps = (int)IblUi.SliderGUI(quality.maxSearchSteps, 0f, 112, 16, "Max Search Steps", "Specifies the maximum steps performed in the horizontal/vertical pattern searches, at each side of the pixel.\nIn number of pixels, it's actually the double. So the maximum line length perfectly handled by, for example 16, is 64 (by perfectly, we meant that longer lines won't look as good, but still antialiased).", 0, true);
					quality.maxDiagonalSearchSteps = (int)IblUi.SliderGUI(quality.maxDiagonalSearchSteps, 0f, 20f, 8, "Max Diagonal Search Steps", "Specifies the maximum steps performed in the diagonal pattern searches, at each side of the pixel. In this case we jump one pixel at time, instead of two.\nOn high-end machines it is cheap (between a 0.8x and 0.9x slower for 16 steps), but it can have a significant impact on older machines.", 0, true);
					quality.cornerRounding = (int)IblUi.SliderGUI(quality.cornerRounding, 0f, 100f, 25f, "Corner Rounding", "Specifies how much sharp corners will be rounded.", 0, true);
					//quality.localContrastAdaptationFactor = Mathf.Max(0, UIUtils.SliderGUI2(quality.localContrastAdaptationFactor, 0f, 10f, 2f, "Local Contrast Adaptation Factor", "If there is a neighbor edge that has a local contrast factor times bigger contrast than current edge, current edge will be discarded.\nThis allows to eliminate spurious crossing edges, and is based on the fact that, if there is too much contrast in a direction, that will hide perceptually contrast in the other neighbors.\r\n[Min: 0]", "N3"));
					quality.localContrastAdaptationFactor = IblUi.SliderGUI(quality.localContrastAdaptationFactor, 0f, 10f, 2f, "Local Contrast Adaptation Factor", "If there is a neighbor edge that has a local contrast factor times bigger contrast than current edge, current edge will be discarded.\nThis allows to eliminate spurious crossing edges, and is based on the fact that, if there is too much contrast in a direction, that will hide perceptually contrast in the other neighbors.", 3, false, true);
					_smaa.quality = quality;
				}

				predication.enabled = IblUi.ToggleGUI(predication.enabled, new GUIContent("Predication", "Predicated thresholding allows to better preserve texture details and to improve performance, by decreasing the number of detected edges using an additional buffer (the detph buffer).\nIt locally decreases the luma or color threshold if an edge is found in an additional buffer (so the global threshold can be higher)."));
				if (predication.enabled)
				{
					//predication.threshold = Mathf.Max(0.0001f, UIUtils.SliderGUI2(predication.threshold, 0.0001f, 10f, 0.01f, "Threshold", "Threshold to be used in the additional predication buffer.\r\n\r\n[Min: 0.0001]", "N4"));
					predication.threshold = IblUi.SliderGUI(predication.threshold, 0.0001f, 10f, 0.01f, "Threshold", "Threshold to be used in the additional predication buffer.", 4, false, true);
					predication.scale = IblUi.SliderGUI(predication.scale, 1f, 5f, 2, "Scale", "How much to scale the global threshold used for luma or color edge detection when using predication.", 3, true);
					predication.strength = IblUi.SliderGUI(predication.strength, 0f, 1f, 0.4f, "Strength", "How much to locally decrease the threshold.", 4, true);
				}

				temporal.enabled = IblUi.ToggleGUI(temporal.enabled, new GUIContent("Temporal", "Temporal filtering makes it possible for the SMAA algorithm to benefit from minute subpixel information available that has been accumulated over many frames."));
				if (temporal.enabled)
				{
					temporal.fuzzSize = IblUi.SliderGUI(temporal.fuzzSize, 0.5f, 10f, 2f, "Fuzz Size", "The size of the fuzz-displacement (jitter) in pixels applied to the camera's perspective projection matrix.\nUsed for 2x temporal anti-aliasing.", 3, true);
				}

				_smaa.predication = predication;
				_smaa.temporal = temporal;
				_smaa.settings = settings;
			}
		}

		private void BloomModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " " + GUIStrings.bloom), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				IblUi.Space(UIUtils.space);
				_cinematicBloom.settings.intensity = IblUi.SliderGUI(_cinematicBloom.settings.intensity, 0f, 1f, 0.1f, "Intensity", "Blend factor of the result image.", 3);
				_cinematicBloom.settings.threshold = IblUi.SliderGUI(_cinematicBloom.settings.threshold, 0f, 8f, 1f, "Threshold", "Filters out pixels under this level of brightness.", 3);
				_cinematicBloom.settings.softKnee = IblUi.SliderGUI(_cinematicBloom.settings.softKnee, 0f, 1f, 0.2f, "Softknee", "Makes transition between under/over-threshold gradual.", 3, true);
				_cinematicBloom.settings.radius = IblUi.SliderGUI(_cinematicBloom.settings.radius, 0f, 16f, 3f, "Radius", "Changes extent of veiling effects in a screen resolution-independent fashion.", 3, true);
				_cinematicBloom.settings.antiFlicker = IblUi.ToggleGUI(_cinematicBloom.settings.antiFlicker, GUIStrings.bloomAntiflicker);
			}
		}

		private void SSAOModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUI(effectData.enabled, new GUIContent(index + " SSAO"), IblUi.TitleStyle);

			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			{
				bool shouldUpdateStudioUI = Studio.Studio.Instance.sceneInfo.enableSSAO != effectData.enabled;
				Studio.Studio.Instance.sceneInfo.enableSSAO = effectData.enabled;

				if (shouldUpdateStudioUI)
				{
					new Traverse(Studio.Studio.Instance).Field("m_SystemButtonCtrl").Field("ssaoInfo").Method("UpdateInfo").GetValue();
				}
			}

			if (effectData.enabled)
			{
				//IblUi.Label("SSAO", UIUtils.titlestyle2);
				IblUi.Space(UIUtils.space);
				_ssao.DebugAO = IblUi.ToggleGUI(_ssao.DebugAO, new GUIContent("Debug AO"));
				_ssao.UseHighPrecisionDepthMap = IblUi.ToggleGUI(_ssao.UseHighPrecisionDepthMap, new GUIContent("Use high precision depth map"));

				IblUi.Label(new GUIContent("Sample Count", "The number of ambient occlusion samples for each pixel on screen. More samples means slower but smoother rendering. Five presets are available"), UIUtils.labelStyle);
				_ssao.Samples = (SSAOPro.SampleCount)IblUi.SelectionGrid((int)_ssao.Samples, _possibleSSAOSampleCountNames, 3, UIUtils.buttonStyleStrechWidth);
				_ssao.Downsampling = Mathf.RoundToInt(IblUi.SliderGUI(_ssao.Downsampling, 1f, 4f, 1f, "Downsampling", "Lets you change resolution at which calculations should be performed (for example, a downsampling value of 2 will work at half the screen resolution). Using downsampling increases rendering speed at the cost of quality.", 0, true));
				_ssao.Intensity = IblUi.SliderGUI(_ssao.Intensity, 0.0f, 16f, 2f, "Intensity", "The occlusion multiplier (degree of darkness added by ambient occlusion). Push this up or down to get a more or less visible effect.", 3, true);

				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
					Studio.Studio.Instance.sceneInfo.ssaoIntensity = _ssao.Intensity;

				_ssao.Radius = IblUi.SliderGUI(_ssao.Radius, 0.01f, 1.25f, 0.125f, "Radius", "The maximum radius of a gap (in world units) that will introduce ambient occlusion.", 3, true);
				_ssao.Distance = IblUi.SliderGUI(_ssao.Distance, 0.0f, 10f, 1f, "Distance", "Represents the distance between an occluded sample and its occluder.", 3, true);
				_ssao.Bias = IblUi.SliderGUI(_ssao.Bias, 0.0f, 1f, 0.1f, "Bias", "The Bias value is added to the occlusion cone. If you’re getting artifacts you may want to push this up while playing with the Distance parameter.", 3, true);
				_ssao.LumContribution = IblUi.SliderGUI(_ssao.LumContribution, 0.0f, 1f, 0.5f, "Lighting Contribution", "Defines how much ambient occlusion should be added in bright areas. By pushing this up, bright areas will have less ambient occlusion which generally leads to more pleasing results.", 3, true);
				IblUi.ColorPickerGUI(_ssao.OcclusionColor, Color.black, "Occlusion Color", c =>
				 {
					 _ssao.OcclusionColor = c;
					 if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
						 Studio.Studio.Instance.sceneInfo.ssaoColor.SetDiffuseRGBA(c);
				 });

				IblUi.Label(new GUIContent("Blur Mode", "None: no blur will be applied to the ambient occlusion pass. Gaussian: an optimized 9 - tap filter. Bilateral: a bilateral box filter capable of detecting borders. High Quality Bilateral: a smooth bilateral gaussian filter capable of detecting borders."), UIUtils.labelStyle);
				_ssao.Blur = (SSAOPro.BlurMode)IblUi.SelectionGrid((int)_ssao.Blur, _possibleSSAOBlurModeNames, 2, UIUtils.buttonStyleStrechWidth);
				if (_ssao.Blur != SSAOPro.BlurMode.None)
				{
					_ssao.BlurDownsampling = IblUi.ToggleGUI(_ssao.BlurDownsampling, new GUIContent("Blur Downsampling", "If enabled, the blur pass will be applied to the downsampled render before it gets resized to fit the screen. Else, it will be applied after the resize, which increases quality but is a bit slower."));
					_ssao.BlurPasses = Mathf.RoundToInt(IblUi.SliderGUI(_ssao.BlurPasses, 1f, 4f, 1, "Blur Passes", "Applies more blur to give a smoother effect at the cost of performance.", 0, true));
					if (_ssao.Blur == SSAOPro.BlurMode.HighQualityBilateral)
						_ssao.BlurBilateralThreshold = IblUi.SliderGUI(_ssao.BlurBilateralThreshold, 0.05f, 1f, 0.1f, "Depth Threshold", "Tweak this to adjust the blur \"sharpness\".", 3, true);
				}
				_ssao.CutoffDistance = IblUi.SliderGUI(_ssao.CutoffDistance, 0.1f, 400f, 150f, "Max Distance", "Used to stop applying ambient occlusion for distant objects (very useful when using fog). ", 1);
				_ssao.CutoffFalloff = IblUi.SliderGUI(_ssao.CutoffFalloff, 0.1f, 100f, 50f, "Falloff", "Used to ease out the cutoff, i.e. set it to 0 and the SSAO will stop abruptly at Max Distance; set it to 50 and the SSAO will smoothly disappear starting at (Max Distance) - (Falloff)", 1);

				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("http://www.thomashourdel.com/ssaopro/doc/usage.html");
			}
		}

		private void SunShaftsModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled,
											   new GUIContent(index + " Sun Shafts"),
											   GUIStrings.disableVsEnable,
											   IblUi.TitleStyle,
											   () => self.MoveUp(effectData.effect),
											   () => self.MoveDown(effectData.effect));

			//if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			//	Studio.Studio.Instance.sceneInfo.enableSunShafts = effectData.enabled;

			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			{
				bool shouldUpdateStudioUI = Studio.Studio.Instance.sceneInfo.enableSunShafts != effectData.enabled;
				Studio.Studio.Instance.sceneInfo.enableSunShafts = effectData.enabled;

				if (shouldUpdateStudioUI)
				{
					new Traverse(Studio.Studio.Instance).Field("m_SystemButtonCtrl").Field("sunShaftsInfo").Method("UpdateInfo").GetValue();
				}
			}

			if (effectData.enabled)
			{
				//IblUi.Label("Sun Shafts", UIUtils.titlestyle2);
				IblUi.Space(UIUtils.space);
				_sunShafts.useDepthTexture = IblUi.ToggleGUI(_sunShafts.useDepthTexture, new GUIContent("Use Depth Buffer", "Use the camera's depth buffer."));
				IblUi.Label(new GUIContent("Resolution", "The resolution at which the shafts are generated. Lower resolutions are faster to calculate and create softer results."), UIUtils.labelStyle);
				_sunShafts.resolution = (SunShafts.SunShaftsResolution)IblUi.SelectionGrid((int)_sunShafts.resolution, _possibleSunShaftsResolutionNames, 3, UIUtils.buttonStyleStrechWidth);
				IblUi.Label("Screen Blend Mode", UIUtils.labelStyle);
				_sunShafts.screenBlendMode = (SunShafts.ShaftsScreenBlendMode)IblUi.SelectionGrid((int)_sunShafts.screenBlendMode, _possibleShaftsScreenBlendModeNames, 2, UIUtils.buttonStyleStrechWidth);
				IblUi.ColorPickerGUI(_sunShafts.sunThreshold, new Color(0.87f, 0.74f, 0.65f), "Threshold Color", "The sun threshold color.", c =>
				 {
					 _sunShafts.sunThreshold = c;
				 });
				IblUi.ColorPickerGUI(_sunShafts.sunColor, Color.white, "Shafts Color", "Shafts color.", c =>
				 {
					 _sunShafts.sunColor = c;
				 });
				_sunShafts.maxRadius = IblUi.SliderGUI(_sunShafts.maxRadius, 0f, 1f, 0.75f, "Max Radius", "The degree to which the shafts' brightness diminishes with distance from the Sun object.", 3);
				_sunShafts.sunShaftBlurRadius = IblUi.SliderGUI(_sunShafts.sunShaftBlurRadius, 0.01f, 20f, 2.5f, "Blur Radius", "The radius over which pixel colours are combined during blurring.", 3);
				_sunShafts.radialBlurIterations = Mathf.RoundToInt(IblUi.SliderGUI(_sunShafts.radialBlurIterations, 0f, 8f, 2f, "Radial Blur Iterations", "The number of repetitions of the blur operation. More iterations will give smoother blurring but each has a cost in processing time.", 0));
				_sunShafts.sunShaftIntensity = IblUi.SliderGUI(_sunShafts.sunShaftIntensity, 0f, 20f, 1.15f, "Intensity", "The brightness of the generated shafts.", 3);
				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-SunShafts.html");
			}
		}

		private void DepthOfFieldModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled,
											   new GUIContent(index + " Depth Of Field"),
											   GUIStrings.disableVsEnable,
											   IblUi.TitleStyle,
											   () => self.MoveUp(effectData.effect),
											   () => self.MoveDown(effectData.effect));

			//if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			//	Studio.Studio.Instance.sceneInfo.enableDepth = effectData.enabled;

			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			{
				bool shouldUpdateStudioUI = Studio.Studio.Instance.sceneInfo.enableDepth != effectData.enabled;
				Studio.Studio.Instance.sceneInfo.enableDepth = effectData.enabled;

				if (shouldUpdateStudioUI)
				{
					new Traverse(Studio.Studio.Instance).Field("m_SystemButtonCtrl").Field("dofInfo").Method("UpdateInfo").GetValue();
				}
			}

			//IblUi.Label("Depth Of Field", UIUtils.titlestyle2);
			if (effectData.enabled)
			{
				IblUi.Space(UIUtils.space);

				_depthOfField.visualizeFocus = IblUi.ToggleGUI(_depthOfField.visualizeFocus, new GUIContent("Visualize Focus", "Overlay color indicating camera focus."));

				IblUi.Label(new GUIContent("Focus Type", "Decides which focus point to use."), UIUtils.labelStyle);
				DOFFocusType newFocusType = (DOFFocusType)IblUi.SelectionGrid((int)_focusType, _possibleDOFFocusTypeNames, 3, UIUtils.buttonStyleStrechWidth);

				if (_focusType != newFocusType)
				{
					_focusType = newFocusType;
					switch (_focusType)
					{
						case DOFFocusType.Crosshair:
							_depthOfField.focalTransform = _depthOfFieldOriginalFocusPoint;
							break;
						case DOFFocusType.Object:
							if (_isStudio == false)
							{
								_depthOfField.focalTransform = _depthOfFieldOriginalFocusPoint;
								_focusType = DOFFocusType.Crosshair;
								break;
							}
							ObjectCtrlInfo info = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault().Value;
							if (info != null)
							{
								_depthOfField.focalTransform = info.guideObject.transformTarget;
								_focussedObject = info;
							}
							else
							{
								_depthOfField.focalTransform = _depthOfFieldOriginalFocusPoint;
								_focusType = DOFFocusType.Crosshair;
							}
							break;
						case DOFFocusType.Manual:
							_depthOfField.focalTransform = null;
							break;
					}
				}

				switch (_focusType)
				{
					case DOFFocusType.Object:

						if (_depthOfField.focalTransform == null || _focussedObject == null)
						{
							_focusType = DOFFocusType.Manual;
							break;
						}
						IblUi.BeginHorizontal();
						IblUi.Label("Current Focus: " + _focussedObject.treeNodeObject.textName, UIUtils.labelStyle);
						IblUi.FlexibleSpace();
						if (IblUi.Button("Select", UIUtils.buttonStyleStrechWidth))
						{
							KeyValuePair<TreeNodeObject, ObjectCtrlInfo> kvp = Studio.Studio.Instance.dicInfo.FirstOrDefault(e => e.Value == _focussedObject);
							if (kvp.Key != null)
								Studio.Studio.Instance.treeNodeCtrl.SelectSingle(kvp.Key);
						}
						IblUi.EndHorizontal();
						if (IblUi.Button("Set selected as focus", UIUtils.buttonStyleStrechWidth))
						{
							if (Studio.Studio.Instance.dicInfo.TryGetValue(Studio.Studio.Instance.treeNodeCtrl.selectNode, out ObjectCtrlInfo info))
							{
								_depthOfField.focalTransform = info.guideObject.transformTarget;
								_focussedObject = info;
							}
						}
						break;
					case DOFFocusType.Manual:
						_depthOfField.focalLength = IblUi.SliderGUI(_depthOfField.focalLength, 0.01f, 50f, 10f, "Focal Distance", "The distance to the focal plane from the camera position in world space.", 2);
						break;
				}

				_depthOfField.focalSize = IblUi.SliderGUI(_depthOfField.focalSize, 0f, 2f, 0.05f, "Focal Size", "Increase the total focal area.", 3);
				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
					Studio.Studio.Instance.sceneInfo.depthFocalSize = _depthOfField.focalSize;
				_depthOfField.aperture = IblUi.SliderGUI(_depthOfField.aperture, 0f, 1f, 0.5f, "Aperture", "The camera’s aperture defining the transition between focused and defocused areas. It is good practice to keep this value as high as possible, as otherwise sampling artifacts might occur, especially when the Max Blur Distance is big. Bigger Aperture values will automatically downsample the image to produce a better defocus.", 3);
				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
					Studio.Studio.Instance.sceneInfo.depthAperture = _depthOfField.aperture;
				_depthOfField.maxBlurSize = IblUi.SliderGUI(_depthOfField.maxBlurSize, 0f, 128f, 2f, "Max Blur Distance", "Max distance for filter taps. Affects texture cache and can cause undersampling artifacts if value is too big. A value smaller than 4.0 should produce decent results.", 1);
				_depthOfField.highResolution = IblUi.ToggleGUI(_depthOfField.highResolution, new GUIContent("High Resolution", "Perform defocus operations in full resolution. Affects performance but might help reduce unwanted artifacts and produce more defined bokeh shapes."));
				_depthOfField.nearBlur = IblUi.ToggleGUI(_depthOfField.nearBlur, new GUIContent("Near Blur", "Foreground areas will overlap at a performance cost."));
				if (_depthOfField.nearBlur)
					_depthOfField.foregroundOverlap = IblUi.SliderGUI(_depthOfField.foregroundOverlap, 0.1f, 2f, 1, "Overlap Size", "Increase foreground overlap dilation if needed.", 3);
				IblUi.Label(new GUIContent("Blur Type", "Algorithm used to produce defocused areas. DX11 is effectively a bokeh splatting technique while DiscBlur indicates a more traditional (scatter as gather) based blur."), UIUtils.labelStyle);
				_depthOfField.blurType = (DepthOfField.BlurType)IblUi.SelectionGrid((int)_depthOfField.blurType, _possibleDOFBlurTypeNames, 2, UIUtils.buttonStyleStrechWidth);
				switch (_depthOfField.blurType)
				{
					case DepthOfField.BlurType.DiscBlur:
						IblUi.Label(new GUIContent("Sample Count", "Amount of filter taps. Greatly affects performance."), UIUtils.labelStyle);
						_depthOfField.blurSampleCount = (DepthOfField.BlurSampleCount)IblUi.SelectionGrid((int)_depthOfField.blurSampleCount, _possibleBlurSampleCountNames, 3, UIUtils.buttonStyleStrechWidth);
						break;
					case DepthOfField.BlurType.DX11:
						IblUi.Label("DX11 Bokeh Settings", UIUtils.labelStyle);
						_depthOfField.dx11BokehScale = IblUi.SliderGUI(_depthOfField.dx11BokehScale, 0f, 50f, 1.2f, "Bokeh Scale", "Size of bokeh texture.", 3);
						_depthOfField.dx11BokehIntensity = IblUi.SliderGUI(_depthOfField.dx11BokehIntensity, 0f, 100f, 2.5f, "Bokeh Intensity", "Blend strength of bokeh shapes.", 2);
						_depthOfField.dx11BokehThreshold = IblUi.SliderGUI(_depthOfField.dx11BokehThreshold, 0f, 2f, 0.5f, "Min Luminance", "Only pixels brighter than this value will cast bokeh shapes. Affects performance as it limits overdraw to a more reasonable amount.", 3);
						_depthOfField.dx11SpawnHeuristic = IblUi.SliderGUI(_depthOfField.dx11SpawnHeuristic, 0.01f, 1f, 0.0875f, "Spawn Heuristic", "Bokeh shapes will only be cast if pixel in questions passes a frequency check. A threshold around 0.1 seems like a good tradeoff between performance and quality.", 4);
						break;
				}
				self.fixDofForUpscaledScreenshots = IblUi.ToggleGUI(self.fixDofForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-DepthOfField.html");
			}
		}

		private void SSRModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUI(effectData.enabled, new GUIContent(index + " Screen Space Reflections"), IblUi.TitleStyle);

			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			{
				bool shouldUpdateStudioUI = Studio.Studio.Instance.sceneInfo.enableSSR != effectData.enabled;
				Studio.Studio.Instance.sceneInfo.enableSSR = effectData.enabled;

				if (shouldUpdateStudioUI)
				{
					new Traverse(Studio.Studio.Instance).Field("m_SystemButtonCtrl").Field("ssrInfo").Method("UpdateInfo").GetValue();
				}
			}

			if (effectData.enabled)
			{
				IblUi.Space(UIUtils.space);

				SSRSettings ssrSettings = _ssr.settings;

				IblUi.Label("---IntensitySettings Settings---", UIUtils.labelStyle);
				IntensitySettings intensitySettings = ssrSettings.intensitySettings;
				{
					intensitySettings.reflectionMultiplier = IblUi.SliderGUI(intensitySettings.reflectionMultiplier, 0.0f, 2f, 1f, "Reflection Multiplier", "Nonphysical multiplier for the SSR reflections. 1.0 is physically based.", 3, true);
					intensitySettings.fadeDistance = IblUi.SliderGUI(intensitySettings.fadeDistance, 0.0f, 1000f, 100f, "Fade Distance", "How far away from the maxDistance to begin fading SSR.", 1, true);
					intensitySettings.fresnelFade = IblUi.SliderGUI(intensitySettings.fresnelFade, 0.0f, 1f, 1f, "Fresnel Fade", "Amplify Fresnel fade out. Increase if floor reflections look good close to the surface and bad farther 'under' the floor.", 3, true);
					intensitySettings.fresnelFadePower = IblUi.SliderGUI(intensitySettings.fresnelFadePower, 0.1f, 10f, 1f, "Fresnel Fade Power", "Higher values correspond to a faster Fresnel fade as the reflection changes from the grazing angle.", 3, true);
				}

				IblUi.Label("---Screen Edge Mask Settings---", UIUtils.labelStyle);
				ScreenEdgeMask screenEdgeMask = ssrSettings.screenEdgeMask;
				{
					screenEdgeMask.intensity = IblUi.SliderGUI(screenEdgeMask.intensity, 0.0f, 1f, 0.03f, "Screen Edge Fading", "Higher = fade out SSRR near the edge of the screen so that reflections don't pop under camera motion.", 3, true);
				}

				IblUi.Label("---Reflection Settings---", UIUtils.labelStyle);
				ReflectionSettings reflectionSettings = ssrSettings.reflectionSettings;
				{
					reflectionSettings.maxDistance = IblUi.SliderGUI(reflectionSettings.maxDistance, 0.1f, 300f, 100f, "Max Distance", "Maximum reflection distance in world units.", 1, true); //previous: 0.5f, 1000f, 100f
					reflectionSettings.blendType =
						(SSRReflectionBlendType)IblUi.SelectGUI(
							(int)reflectionSettings.blendType,
							new GUIContent("Additive Reflection", "How the reflections are blended into the render. 'Additive' is not physically correct, but it can hide some artifacts."),
							_possibleSSRReflectionBlendTypeNames
							);

					reflectionSettings.iterationCount = Mathf.RoundToInt(IblUi.SliderGUI(reflectionSettings.iterationCount, 16f, 1024f, 256f, "Max Steps", "Max raytracing length.", 0, true)); // previous: 16f, 2048f, 0f
					reflectionSettings.stepSize = Mathf.RoundToInt(IblUi.SliderGUI(reflectionSettings.stepSize, 1f, 16f, 3f, "Ray Step Size", "Log base 2 of ray tracing coarse step size. Higher traces farther, lower gives better quality silhouettes.", 0, true));
					reflectionSettings.widthModifier = IblUi.SliderGUI(reflectionSettings.widthModifier, 0.01f, 10f, 0.5f, "Width Modifier", "Typical thickness of columns, walls, furniture, and other objects that reflection rays might pass behind.", 3, true);
					
					reflectionSettings.reflectionBlur = IblUi.SliderGUI(reflectionSettings.reflectionBlur, 0.1f, 8.0f, 1f, "Reflection Blur", "Blurriness of reflections. Higher values increase the blur effect.", 3, true);
					reflectionSettings.reflectBackfaces = IblUi.ToggleGUI(reflectionSettings.reflectBackfaces, new GUIContent("Reflect Backfaces", "Enable for a performance gain in scenes where most glossy objects are horizontal, like floors, water, and tables. Leave on for scenes with glossy vertical objects."));

					// special case for `reflectionQuality` since the values are 0 and 2
					int reflectionQualityIndex = reflectionSettings.reflectionQuality == 0 ? 0 : 1;
					reflectionQualityIndex = IblUi.SelectGUI(
							reflectionQualityIndex,
							new GUIContent("Reflection Quality", "Half resolution SSRR is much faster, but less accurate."),
							_possibleSSRResolutionNames
							);
					reflectionSettings.reflectionQuality = reflectionQualityIndex == 0 ? SSRResolution.High : SSRResolution.Low;
					
				}

				IblUi.Label("---Advanced Settings---", UIUtils.labelStyle);
				{
					_ssr.traceBehindObjects = IblUi.ToggleGUI(_ssr.traceBehindObjects, new GUIContent("Trace Behind Objects", "Enable to allow rays to pass behind objects. This can lead to more screen-space reflections, but the reflections are more likely to be wrong."));
					_ssr.treatBackfaceHitAsMiss = IblUi.ToggleGUI(_ssr.treatBackfaceHitAsMiss, new GUIContent("Treat Backface Hit As Miss", "Enable to force more surfaces to use reflection probes if you see streaks on the sides of objects or bad reflections of their backs."));
					_ssr.highlightSuppression = IblUi.ToggleGUI(_ssr.highlightSuppression, new GUIContent("Highlight Suppression", "Enable to limit the effect a few bright pixels can have on rougher surfaces"));
					_ssr.bilateralUpsample = IblUi.ToggleGUI(_ssr.bilateralUpsample, new GUIContent("Bilateral Upsample", "Drastically improves reflection reconstruction quality at the expense of some performance."));
				}
				
				ssrSettings.intensitySettings = intensitySettings;
				ssrSettings.reflectionSettings = reflectionSettings;
				ssrSettings.screenEdgeMask = screenEdgeMask;				
				_ssr.settings = ssrSettings;

				self.fixSsrForUpscaledScreenshots = IblUi.ToggleGUI(self.fixSsrForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
			}
		}

		private void VolumetricLightsModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUI(effectData.enabled, new GUIContent(index + " Volumetric Lights"), IblUi.TitleStyle);
			if (effectData.enabled)
			{
				IblUi.Space(UIUtils.space);

				IblUi.Label(new GUIContent("Resolution", ""), UIUtils.labelStyle);
				_volumetricLights.Resolution = (VolumetricLightRenderer.VolumetricResolution)IblUi.SelectionGrid((int)_volumetricLights.Resolution, _possibleVolumetricLightsResolutionNames, 3, UIUtils.buttonStyleStrechWidth);
				_volumetricLights.VisualizeLightTexture = IblUi.ToggleGUI(_volumetricLights.VisualizeLightTexture, new GUIContent("Visualize Light Texture", ""));

				if (_currentVolumetricLight != null)
				{
					IblUi.Space(UIUtils.space);
					_currentVolumetricLight.enabled = IblUi.ToggleGUI(_currentVolumetricLight.enabled, new GUIContent("Selected Light", "Enables/Disables the entire effect for this light."), IblUi.TitleStyle);
					_currentVolumetricLight.SampleCount = Mathf.RoundToInt(IblUi.SliderGUI(_currentVolumetricLight.SampleCount, 1f, 64f, 12f, "Sample Count", "The number of raymarching samples (trade quality for performance).", 0, true));
					_currentVolumetricLight.ScatteringCoef = IblUi.SliderGUI(_currentVolumetricLight.ScatteringCoef, 0f, 1f, 0.5f, "Scattering Coef", "Scattering coefficient controls the amount of light that is reflected towards the camera. Makes the effect stronger.", 4, true);
					_currentVolumetricLight.ExtinctionCoef = IblUi.SliderGUI(_currentVolumetricLight.ExtinctionCoef, 0f, 0.1f, 0.01f, "Extinction Coef", "Controls the amount of light absorbed or out-scattered with distance. Makes the effect weaker with increasing distance. It also attenuates existing scene color when used with directional lights.", 4, true);
					_currentVolumetricLight.MieG = IblUi.SliderGUI(_currentVolumetricLight.MieG, 0f, 0.999f, 0.1f, "Mie Scattering", "Mie Scattering.", 4, true);
					if (_currentVolumetricLight.Light.type == LightType.Directional)
					{
						_currentVolumetricLight.SkyboxExtinctionCoef = IblUi.SliderGUI(_currentVolumetricLight.SkyboxExtinctionCoef, 0f, 1f, 0.9f, "Skybox Extinction Coef", "Only affects directional light. It controls how much the skybox is affected by Extinction coefficient. This technique ignores small air particles and decrease particle density with altitude. Skybox extinction coefficient can help when the skybox appears too \"foggy\".", 4);
						_currentVolumetricLight.MaxRayLength = IblUi.SliderGUI(_currentVolumetricLight.MaxRayLength, 0f, 500f, 400f, "Max Ray Length", "Max Ray Length.", 2);
					}

					_currentVolumetricLight.HeightFog = IblUi.ToggleGUI(_currentVolumetricLight.HeightFog, new GUIContent("Height Fog", "Exponential height fog"), IblUi.TitleStyle);
					_currentVolumetricLight.HeightScale = IblUi.SliderGUI(_currentVolumetricLight.HeightScale, 0f, 0.5f, 0.1f, "Scale", "Height scale.", 4);
					_currentVolumetricLight.GroundLevel = IblUi.SliderGUI(_currentVolumetricLight.GroundLevel, -100f, 100f, 0f, "Ground Level", "Ground level.", 2);

					_currentVolumetricLight.Noise = IblUi.ToggleGUI(_currentVolumetricLight.Noise, new GUIContent("Noise", "Volumetric noise"), IblUi.TitleStyle);
					_currentVolumetricLight.NoiseScale = IblUi.SliderGUI(_currentVolumetricLight.NoiseScale, -5f, 5f, 0.15f, "Scale", "Volumetric noise scale.", 3);
					_currentVolumetricLight.NoiseIntensity = IblUi.SliderGUI(_currentVolumetricLight.NoiseIntensity, 0f, 12f, 3.44f, "Intensity", "Volumetric noise intensity.", 3);
					_currentVolumetricLight.NoiseIntensityOffset = IblUi.SliderGUI(_currentVolumetricLight.NoiseIntensityOffset, 0f, 1f, 0.2f, "Intensity Offset", "", 4);
					_currentVolumetricLight.NoiseVelocity = new Vector2(
							IblUi.SliderGUI(_currentVolumetricLight.NoiseVelocity.x, -100f, 100f, 1f, "Velocity X", "Noise animation speed (x)", 2),
							IblUi.SliderGUI(_currentVolumetricLight.NoiseVelocity.y, -100f, 100f, 0.3f, "Velocity Y", "Noise animation speed (y)", 2)
					);
				}
				else
				{
					Color c = IblUi.Color;
					IblUi.Color = Color.yellow;
					IblUi.Label(new GUIContent("Select a light to see more settings", ""), UIUtils.labelStyle);
					IblUi.Color = c;
				}
				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://github.com/SlightlyMad/VolumetricLights");
			}
		}

		private void BloomAndFlaresModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled,
											   new GUIContent(index + " Bloom and Flares"),
											   GUIStrings.disableVsEnable,
											   IblUi.TitleStyle,
											   () => self.MoveUp(effectData.effect),
											   () => self.MoveDown(effectData.effect));

			//if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			//	Studio.Studio.Instance.sceneInfo.enableBloom = effectData.enabled;

			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			{
				bool shouldUpdateStudioUI = Studio.Studio.Instance.sceneInfo.enableBloom != effectData.enabled;
				Studio.Studio.Instance.sceneInfo.enableBloom = effectData.enabled;

				if (shouldUpdateStudioUI)
				{
					new Traverse(Studio.Studio.Instance).Field("m_SystemButtonCtrl").Field("bloomInfo").Method("UpdateInfo").GetValue();
				}
			}

			if (effectData.enabled)
			{
				IblUi.Label(new GUIContent("Blend mode", "The method used to add bloom to the color buffer. The softer \"Screen\" mode is better for preserving bright image details but doesn’t work with HDR."), UIUtils.labelStyle);
				_bloomAndFlares.screenBlendMode = (BloomScreenBlendMode)IblUi.SelectionGrid((int)_bloomAndFlares.screenBlendMode, _possibleScreenBlendModeNames, 2, UIUtils.buttonStyleStrechWidth);
				IblUi.Label(new GUIContent("HDR", "Whether bloom is using HDR buffers. This will result in a different look as pixel intensities may leave the [0,1] range."), UIUtils.labelStyle);
				_bloomAndFlares.hdr = (HDRBloomMode)IblUi.SelectionGrid((int)_bloomAndFlares.hdr, _possibleHDRBloomModeNames, 3, UIUtils.buttonStyleStrechWidth);

				_bloomAndFlares.bloomIntensity = IblUi.SliderGUI(_bloomAndFlares.bloomIntensity, 0f, 8f, 1f, "Intensity", "The global light intensity of the added light (affects bloom and lens flares).", 3);
				_bloomAndFlares.bloomThreshold = IblUi.SliderGUI(_bloomAndFlares.bloomThreshold, -0.05f, 4f, 0.5f, "Threshold", "Regions of the image brighter than this threshold receive blooming (and potentially lens flares).", 3);
				_bloomAndFlares.bloomBlurIterations = Mathf.RoundToInt(IblUi.SliderGUI(_bloomAndFlares.bloomBlurIterations, 1f, 4f, 2f, "Blur iterations", "The number of times gaussian blur is applied. More iterations improve smoothness but take extra time to process and hide small frequencies.", 0));
				_bloomAndFlares.sepBlurSpread = IblUi.SliderGUI(_bloomAndFlares.sepBlurSpread, 0.1f, 10f, 1.5f, "Blur spread", "The max radius of the blur. Does not affect performance.", 3);
				_bloomAndFlares.useSrcAlphaAsMask = IblUi.SliderGUI(_bloomAndFlares.useSrcAlphaAsMask, 0f, 1f, 0f, "Use alpha mask", "The degree to which the alpha channel acts as a mask for the bloom effect.", 3);

				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
				{
					Studio.Studio.Instance.sceneInfo.bloomIntensity = _bloomAndFlares.bloomIntensity;
					Studio.Studio.Instance.sceneInfo.bloomBlur = _bloomAndFlares.sepBlurSpread;
				}

				_bloomAndFlares.lensflares = IblUi.ToggleGUI(_bloomAndFlares.lensflares, new GUIContent("Cast lens flares", "Enable or disable automatic lens flare generation."));
				if (_bloomAndFlares.lensflares)
				{
					IblUi.Label(new GUIContent("Lens flare mode", "The type of lens flare. The options are Ghosting, Anamorphic or a mix of the two."), UIUtils.labelStyle);
					_bloomAndFlares.lensflareMode = (LensflareStyle34)IblUi.SelectionGrid((int)_bloomAndFlares.lensflareMode, _possibleLensFlareStyle34Names, 3, UIUtils.buttonStyleStrechWidth);
					_bloomAndFlares.lensflareIntensity = IblUi.SliderGUI(_bloomAndFlares.lensflareIntensity, 0f, 8f, 1f, "Local intensity", "Local intensity used only for lens flares.", 3);
					_bloomAndFlares.lensflareThreshold = IblUi.SliderGUI(_bloomAndFlares.lensflareThreshold, 0f, 1f, 0.3f, "Local threshold", "The accumulative light intensity threshold that defines which image parts are candidates for lens flares.", 3);

					switch (_bloomAndFlares.lensflareMode)
					{
						case LensflareStyle34.Combined:
						case LensflareStyle34.Anamorphic:
							_bloomAndFlares.hollyStretchWidth = IblUi.SliderGUI(_bloomAndFlares.hollyStretchWidth, 0f, 8f, 3.5f, "Stretch witdh", "The width for anamorphic lens flares.", 3);
							_bloomAndFlares.hollywoodFlareBlurIterations = Mathf.RoundToInt(IblUi.SliderGUI(_bloomAndFlares.hollywoodFlareBlurIterations, 1f, 4f, 2f, "Blur iterations", "The number of times blurring is applied to anamorphic lens flares. More iterations improve smoothness but take more processing time.", 0));
							if (_bloomAndFlares.lensflareMode == LensflareStyle34.Combined)
								goto case LensflareStyle34.Ghosting;
							IblUi.ColorPickerGUI(_bloomAndFlares.flareColorA, new Color(0.4f, 0.4f, 0.8f, 0.75f), "Tint color", "Color modulation for the anamorphic flare type.", c =>
							{
								_bloomAndFlares.flareColorA = c;
							});
							break;
						case LensflareStyle34.Ghosting:
							IblUi.ColorPickerGUI(_bloomAndFlares.flareColorA, new Color(0.4f, 0.4f, 0.8f, 0.75f), "1st Color", "Color modulation for all lens flares when Ghosting or Combined is chosen.", c =>
							{
								_bloomAndFlares.flareColorA = c;
							});
							IblUi.ColorPickerGUI(_bloomAndFlares.flareColorB, new Color(0.4f, 0.8f, 0.8f, 0.75f), "2nd Color", "Color modulation for all lens flares when Ghosting or Combined is chosen.", c =>
							{
								_bloomAndFlares.flareColorB = c;
							});
							IblUi.ColorPickerGUI(_bloomAndFlares.flareColorC, new Color(0.8f, 0.4f, 0.8f, 0.75f), "3rd Color", "Color modulation for all lens flares when Ghosting or Combined is chosen.", c =>
							{
								_bloomAndFlares.flareColorC = c;
							});
							IblUi.ColorPickerGUI(_bloomAndFlares.flareColorD, new Color(0.8f, 0.4f, 0f, 0.75f), "4th Color", "Color modulation for all lens flares when Ghosting or Combined is chosen.", c =>
							{
								_bloomAndFlares.flareColorD = c;
							});
							break;
					}
				}
				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-BloomAndFlares.html");
			}
		}

		private void ColorCorrectionCurvesModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Color Correction Curves"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				_colorCorrectionCurves.saturation = IblUi.SliderGUI(_colorCorrectionCurves.saturation, 0f, 5f, 1f, "Saturation", "Saturation level (0 creates a black & white image).", 3);

				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-ColorCorrectionCurves.html");
			}
		}

		private void VignetteAndChromaticAberrationModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled,
											   new GUIContent(index + " Vignette and Chromatic Aberration"),
											   GUIStrings.disableVsEnable,
											   IblUi.TitleStyle,
											   () => self.MoveUp(effectData.effect),
											   () => self.MoveDown(effectData.effect));

			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
			{
				bool shouldUpdateStudioUI = Studio.Studio.Instance.sceneInfo.enableVignette != effectData.enabled;
				Studio.Studio.Instance.sceneInfo.enableVignette = effectData.enabled;

				if (shouldUpdateStudioUI)
				{
					new Traverse(Studio.Studio.Instance).Field("m_SystemButtonCtrl").Field("vignetteInfo").Method("UpdateInfo").GetValue();
				}
			}

			if (effectData.enabled)
			{
				_vignette.intensity = IblUi.SliderGUI(_vignette.intensity, 0f, 1f, 0.036f, "Vignetting", "The degree of darkening applied to the screen edges and corners. Choose 0 to disable this feature and save on performance.", 3);
				_vignette.blur = IblUi.SliderGUI(_vignette.blur, 0f, 1f, 0f, "Blurred Corners", "The amount of blur that is added to the screen corners. Choose 0 to disable this feature and save on performance.", 3);
				_vignette.blurSpread = IblUi.SliderGUI(_vignette.blurSpread, 0f, 1f, 0.75f, "Blurred Distance", "The blur filter sample distance used when blurring corners.", 3);

				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
					Studio.Studio.Instance.sceneInfo.vignetteVignetting = _vignette.intensity;


				IblUi.Label(new GUIContent("Aberration mode", "Advanced tries to model more aberration effects (the constant axial aberration existant on the entire image plane) while Simple only models tangential aberration (limited to corners)."), UIUtils.labelStyle);
				_vignette.mode = (VignetteAndChromaticAberration.AberrationMode)IblUi.SelectionGrid((int)_vignette.mode, _possibleAberrationModeNames, 2, UIUtils.buttonStyleStrechWidth);

				_vignette.chromaticAberration = IblUi.SliderGUI(_vignette.chromaticAberration, 0f, 5f, 0.2f, "Tangential Aberration", "The degree of tangential chromatic aberration: Uniform on the entire image plane.", 3);
				if (_vignette.mode == VignetteAndChromaticAberration.AberrationMode.Advanced)
				{
					_vignette.axialAberration = IblUi.SliderGUI(_vignette.axialAberration, 0f, 5f, 0.5f, "Axial Aberration", "The degree of axial chromatic aberration: Scales with smaller distance to the image plane’s corners.", 3);
					_vignette.luminanceDependency = IblUi.SliderGUI(_vignette.luminanceDependency, 0.001f, 1f, 0.25f, "Contrast Dependency", "The bigger this value, the more contrast is needed for the aberration to trigger. Higher values are more realistic (in this case, an HDR input is recommended).", 3);
					_vignette.blurDistance = IblUi.SliderGUI(_vignette.blurDistance, 0.001f, 5f, 2.5f, "Blur Distance", "Overall aberration intensity (not to confuse with color offset distance), defaults to 1.0.", 3);
				}
				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-VignettingAndChromaticAberration.html");
			}
		}

		private void AntialiasingModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Antialiasing"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				IblUi.Label(new GUIContent("AA Technique", "The algorithm to be used."), UIUtils.labelStyle);
				_antialiasing.mode = (AAMode)IblUi.SelectionGrid((int)_antialiasing.mode, _possibleAAModeNames, 3, UIUtils.buttonStyleStrechWidth);

				switch (_antialiasing.mode)
				{
					case AAMode.FXAA3Console:
						_antialiasing.edgeThresholdMin = IblUi.SliderGUI(_antialiasing.edgeThresholdMin, 0f, 4f, 0.05f, "Edge Min Threshold", "Edge Min Threshold.", 3);
						_antialiasing.edgeThreshold = IblUi.SliderGUI(_antialiasing.edgeThreshold, 0f, 4f, 0.2f, "Edge Threshold", "Edge Threshold.", 3);
						_antialiasing.edgeSharpness = IblUi.SliderGUI(_antialiasing.edgeSharpness, 0f, 16f, 4f, "Edge Sharpness", "Edge Sharpness.", 3);
						break;
					case AAMode.NFAA:
						_antialiasing.offsetScale = IblUi.SliderGUI(_antialiasing.offsetScale, 0f, 4f, 0.2f, "Edge Detect Offset", "Edge Detect Offset.", 3);
						_antialiasing.blurRadius = IblUi.SliderGUI(_antialiasing.blurRadius, 0f, 18f, 18f, "Blur Radius", "Blur Radius.", 3);
						_antialiasing.showGeneratedNormals = IblUi.ToggleGUI(_antialiasing.showGeneratedNormals, new GUIContent("Show Normals", "Show Normals."));
						break;
					case AAMode.DLAA:
						_antialiasing.dlaaSharp = IblUi.ToggleGUI(_antialiasing.dlaaSharp, new GUIContent("Sharp", "DLAA sharpness."));
						break;
				}
			}
		}

		private void NoiseAndGrainModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Noise and Grain"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				_noiseAndGrain.dx11Grain = IblUi.ToggleGUI(_noiseAndGrain.dx11Grain, new GUIContent("DX11 Grain", "Enable high quality noise and grain (DX11/GL3 only)."));
				_noiseAndGrain.monochrome = IblUi.ToggleGUI(_noiseAndGrain.monochrome, new GUIContent("Monochrome", "Use greyscale noise only."));

				_noiseAndGrain.intensityMultiplier = IblUi.SliderGUI(_noiseAndGrain.intensityMultiplier, 0f, 10f, 0.25f, "Intensity multiplier", "Global intensity adjustment.", 3);
				_noiseAndGrain.generalIntensity = IblUi.SliderGUI(_noiseAndGrain.generalIntensity, 0f, 1f, 0.5f, "General", "Add noise equally for all luminance ranges.", 3);
				_noiseAndGrain.blackIntensity = IblUi.SliderGUI(_noiseAndGrain.blackIntensity, 0f, 1f, 1f, "Black Boost", "Add extra low luminance noise.", 3);
				_noiseAndGrain.whiteIntensity = IblUi.SliderGUI(_noiseAndGrain.whiteIntensity, 0f, 1f, 1f, "White Boost", "Add extra high luminance noise.", 3);
				_noiseAndGrain.midGrey = IblUi.SliderGUI(_noiseAndGrain.midGrey, 0f, 1f, 0.2f, "Mid Grey", "Defines ranges for high-level and low-level noise ranges above.", 3);
				if (_noiseAndGrain.monochrome == false)
				{
					IblUi.ColorPickerGUI(new Color(_noiseAndGrain.intensities.x, _noiseAndGrain.intensities.y, _noiseAndGrain.intensities.z), Color.white, "Color Weights", "Additionally tint noise.", (c) =>
					{
						_noiseAndGrain.intensities = new Vector3(c.r, c.g, c.b);
					});
				}

				_noiseAndGrain.filterMode = (FilterMode)IblUi.SelectionGrid((int)_noiseAndGrain.filterMode, _possibleFilterModeNames, 3, UIUtils.buttonStyleStrechWidth);
				_noiseAndGrain.softness = IblUi.SliderGUI(_noiseAndGrain.softness, 0f, 0.99f, 0.25f, "Softness", "Defines noise or grain crispness. Higher values might yield better performance but require temporary a render target.", 3, true);

				if (_noiseAndGrain.dx11Grain == false)
				{

					if (_noiseAndGrain.monochrome)
					{
						_noiseAndGrain.monochromeTiling = IblUi.SliderGUI(_noiseAndGrain.monochromeTiling, 1f, 128f, 64f, "Tiling", "Noise pattern tiling", 1);
					}
					else
					{
						_noiseAndGrain.tiling = new Vector3(
							IblUi.SliderGUI(_noiseAndGrain.tiling.x, 1f, 128f, 64f, "Tiling (Red)", "Noise pattern tiling (can be tweaked for all color channels individually when in non-DX11 texture mode).", 1),
							IblUi.SliderGUI(_noiseAndGrain.tiling.y, 1f, 128f, 64f, "Tiling (Green)", "Noise pattern tiling (can be tweaked for all color channels individually when in non-DX11 texture mode).", 1),
							IblUi.SliderGUI(_noiseAndGrain.tiling.z, 1f, 128f, 64f, "Tiling (Blue)", "Noise pattern tiling (can be tweaked for all color channels individually when in non-DX11 texture mode).", 1)
							);
					}
				}
				self.fixNoiseAndGrainForUpscaledScreenshots = IblUi.ToggleGUI(self.fixNoiseAndGrainForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", "Adjust the effect based on screenshot dimensions."));
				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-NoiseAndGrain.html");
			}
		}

		private void MotionBlurModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Camera Motion Blur"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				_motionBlur.velocityScale = IblUi.SliderGUI(_motionBlur.velocityScale, 0f, 8f, 0.375f, "Velocity Scale", "Higher scale makes image more likely to blur.", 3);
				_motionBlur.maxVelocity = IblUi.SliderGUI(_motionBlur.maxVelocity, 2f, 10f, 8f, "Velocity Max", "Maximum pixel distance blur will be clamped to and tile size for reconstruction filters (see below).", 3);
				_motionBlur.minVelocity = IblUi.SliderGUI(_motionBlur.minVelocity, 0.1f, 10f, 0.1f, "Velocity Min", "Minimum pixel distance at which blur is removed entirely.", 3);

				IblUi.Label(new GUIContent("Technique", "Motion Blur algorithm. Reconstruction filters will generally give best results at the expense of performance and a limited blur radius of 10 pixels unless a DX11/GL3 enabled graphics device is used."), UIUtils.labelStyle);
				_motionBlur.filterType = (CameraMotionBlur.MotionBlurFilter)IblUi.SelectionGrid((int)_motionBlur.filterType, _possibleMotionBlurFilterNames, 2, UIUtils.buttonStyleStrechWidth);

				if (_motionBlur.filterType == CameraMotionBlur.MotionBlurFilter.CameraMotion)
				{
					_motionBlur.rotationScale = IblUi.SliderGUI(_motionBlur.rotationScale, 0f, 8f, 1f, "Camera Rotation", "Scales strength of blurs due to camera rotations.", 3);
					_motionBlur.movementScale = IblUi.SliderGUI(_motionBlur.movementScale, 0f, 8f, 0f, "Camera Movement", "Scales strength of blurs due to camera translations.", 3);
				}
				else
					//_motionBlur.velocityDownsample = Mathf.Max(1, Mathf.RoundToInt(UIUtils.SliderGUI2(_motionBlur.velocityDownsample, 1f, 32f, 1f, "Velocity Downsample", "Lower resolution velocity buffers might help performance but will heavily degrade blur quality. Might still be a valid option for simple scenes.\r\nHigher values = lower sampling quality.", "0")));
					_motionBlur.velocityDownsample = Mathf.RoundToInt(IblUi.SliderGUI(_motionBlur.velocityDownsample, 1f, 32f, 1f, "Velocity Downsample", "Lower resolution velocity buffers might help performance but will heavily degrade blur quality. Might still be a valid option for simple scenes.\r\nHigher values = lower sampling quality.", 0));
				if (_motionBlur.filterType >= CameraMotionBlur.MotionBlurFilter.Reconstruction)
					_motionBlur.jitter = IblUi.SliderGUI(_motionBlur.jitter, 0f, 10f, 0.05f, "Jitter Strength", "Adding noise helps prevent ghosting for the Reconstruction filter.", 3);

				_motionBlur.preview = IblUi.ToggleGUI(_motionBlur.preview, new GUIContent("Preview", "Preview how blur might look like given artificial camera motion values."));
				if (_motionBlur.preview)
				{
					_motionBlur.previewScale = new Vector3(
						IblUi.SliderGUI(_motionBlur.previewScale.x, -16f, 16f, 1f, "X", "Preview scale (X).", 3),
						IblUi.SliderGUI(_motionBlur.previewScale.y, -16f, 16f, 1f, "Y", "Preview scale (Y).", 3),
						IblUi.SliderGUI(_motionBlur.previewScale.z, -16f, 16f, 1f, "Z", "Preview scale (Z).", 3)
						);
				}
				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-CameraMotionBlur.html");
			}
		}

		private void AfterImageModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " After Image"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				_afterImage.framesToMix = Mathf.RoundToInt(IblUi.SliderGUI(_afterImage.framesToMix, 1f, AfterImage.maxFrames, 8f, "Frames To Mix", "Number of frames to mix together.", 0));
				_afterImage.falloffPower = IblUi.SliderGUI(_afterImage.falloffPower, 0f, 2f, 10f / 7f, "Falloff Power", "How quickly older frames are faded out.", 4);
			}
		}

		private void SEGIModule(EffectData effectData, int i)
		{
			effectData.enabled = IblUi.ToggleGUI(effectData.enabled, new GUIContent(i + " SEGI"), IblUi.TitleStyle);

			if (effectData.enabled)
			{
				if (RenderSettings.ambientIntensity > 0)
				{
					Color color = IblUi.Color;
					IblUi.Color = ColorPresets.Tomato; //Color.red;
					IblUi.Label(
						"SEGI only works properly if the ambient intensity is set to 0, check the Lighting\r\n" +
						"tab and make sure it's the case. Having HSStandard is also needed as it\r\n" +
						"ensures the shaders of everything are compatible with SEGI.",
						UIUtils.labelStyle);
					IblUi.Color = color;
				}

				#region determine sun
				string segiSunInfo = "";
				if (_lastSelectedNode != null &&
					Studio.Studio.Instance.dicInfo.TryGetValue((TreeNodeObject)_lastSelectedNode, out ObjectCtrlInfo oci) &&
					oci is OCILight ociLight &&
					self.segi.sun == ociLight.light)
				{
					segiSunInfo += "The selected light is the SEGI sun.";
				}

				IblUi.Label(
					$"The strongest Directional Light will be considered by SEGI to be the sun,\r\n" +
					$"this will affect the final appearance of the effect.\r\n" +
					$"Current SEGI sun intensity: {(self.segi.sun ? self.segi.sun.intensity.ToString() : "N/A (No Directional Lights in scene)")}\r\n" +
					segiSunInfo,
					UIUtils.labelStyle);
				#endregion

				IblUi.Label("Presets", UIUtils.labelStyle);
				
				//_selectedSegiPreset = IblUi.SelectGUI(_selectedSegiPreset, new GUIContent("Presets:", "SEGI preset (overwrites other settings)"), _possiblePresetNames);
				_selectedSegiPreset = IblUi.SelectionGrid(_selectedSegiPreset, _possiblePresetNames, 3, UIUtils.buttonStyleStrechWidth);
				
				IblUi.BeginHorizontal();
				IblUi.FlexibleSpace();
				if (IblUi.Button(new GUIContent("Apply", "Pressing this will apply the selected preset and change your settings, be careful."), UIUtils.buttonStyleStrechWidth, GUILayout.ExpandWidth(false)))
					_segi.ApplyPreset(_selectedSegiPreset);
				IblUi.EndHorizontal();

				_segi.visualizeSunDepthTexture = IblUi.ToggleGUI(_segi.visualizeSunDepthTexture, new GUIContent("Visualize Sun Depth Texture", "Visualize the depth texture used to render proper shadows while injecting sunlight into voxel data."));
				_segi.visualizeGI = IblUi.ToggleGUI(_segi.visualizeGI, new GUIContent("Visualize GI", "Visualize GI result only (no textures)."));
				_segi.visualizeVoxels = IblUi.ToggleGUI(_segi.visualizeVoxels, new GUIContent("Visualize Voxels", "Directly view the voxels in the scene."));
				IblUi.Label(new GUIContent("Visualize GBuffer", "Various Geometry Buffer debug views."), UIUtils.labelStyle);
				
				
				//_segi.visualizeGBuffers = IblUi.SelectGUI(_segi.visualizeGBuffers, new GUIContent("Visualize GBuffer", "Various debug views."), new[] { "None", "GBuffer0", "GBuffer1", "GBuffer2", "Depth", "DepthNormals" }, 3);
				_segi.visualizeGBuffers = IblUi.SelectionGrid(_segi.visualizeGBuffers, new[] { "None", "GBuffer0", "GBuffer1", "GBuffer2", "Depth", "DepthNormals" }, 3, UIUtils.buttonStyleStrechWidth);

				IblUi.Space(UIUtils.space);

				IblUi.Label(new GUIContent("Voxel Resolution", "The resolution of the voxel texture used to calculate GI."), UIUtils.labelStyle);
				int index = 0;
				switch (_segi.voxelResolution)
				{
					case SEGI.VoxelResolution.low:
						index = 0;
						break;
					case SEGI.VoxelResolution.high:
						index = 1;
						break;
				}

				//index = IblUi.SelectGUI(index, new GUIContent("Voxel Resolution", "The resolution of the voxel texture used to calculate GI."), _possibleVoxelResolutionNames, 2);
				index = IblUi.SelectionGrid(index, _possibleVoxelResolutionNames, 2, UIUtils.buttonStyleStrechWidth);
				
				switch (index)
				{
					case 0:
						_segi.voxelResolution = SEGI.VoxelResolution.low;
						break;
					case 1:
						_segi.voxelResolution = SEGI.VoxelResolution.high;
						break;
				}
				_segi.voxelAA = IblUi.ToggleGUI(_segi.voxelAA, new GUIContent("Voxel AA", "Enables anti-aliasing during voxelization for higher precision voxels."));
				_segi.innerOcclusionLayers = Mathf.RoundToInt(IblUi.SliderGUI(_segi.innerOcclusionLayers, 0f, 2f, 1f, "Inner Occlusion Layers", "Enables the writing of additional black occlusion voxel layers on the back face of geometry. Can help with light leaking but may cause artifacts with small objects.", 0, true));
				_segi.gaussianMipFilter = IblUi.ToggleGUI(_segi.gaussianMipFilter, new GUIContent("Gaussian Mip Filter", "Enables gaussian filtering during mipmap generation. This can improve visual smoothness and consistency, particularly with large moving objects."));
				_segi.voxelSpaceSize = IblUi.SliderGUI(_segi.voxelSpaceSize, 1f, 100f, 50f, "Voxel Space Size", "The size of the voxel volume in world units. Everything inside the voxel volume will contribute to GI.", 3);
				_segi.shadowSpaceSize = IblUi.SliderGUI(_segi.shadowSpaceSize, 1f, 100f, 50f, "Shadow Space Size", "The size of the sun shadow texture used to inject sunlight with shadows into the voxels in world units. It is recommended to set this value similar to Voxel Space Size.", 3);
				_segi.updateGI = IblUi.ToggleGUI(_segi.updateGI, new GUIContent("Update GI", "Whether voxelization and multi-bounce rendering should update every frame. When disabled, GI tracing will use cached data from the last time this was enabled."));
				_segi.infiniteBounces = IblUi.ToggleGUI(_segi.infiniteBounces, new GUIContent("Infinite Bounces", "Enables infinite bounces. This is expensive for complex scenes and is still experimental."));
				_segi.hsStandardCustomShadersCompatibility = IblUi.ToggleGUI(_segi.hsStandardCustomShadersCompatibility, new GUIContent("HSStandard Custom Shaders Compatibility", "Enabling this makes sure the custom hair and skin shaders used in HSStandard when using the options \"dedicatedHairShader\" and \"dedicatedSkinShader\" are used by SEGI (incurs a performance cost)."));

				IblUi.Label("VRAM Usage: " + _segi.vramUsage.ToString("F2") + " MB", UIUtils.labelStyle);

				IblUi.Space(UIUtils.space);
				_segi.softSunlight = IblUi.SliderGUI(_segi.softSunlight, 0f, 16f, 0f, "Soft Sunlight", "The amount of soft diffuse sunlight that will be added to the scene. Use this to simulate the effect of clouds/haze scattering soft sunlight onto the scene.", 3, true);
				IblUi.ColorPickerGUI(_segi.skyColor, Color.clear, "Sky Color", "The color of the light scattered onto the scene coming from the sky.", (c) =>
				{
					_segi.skyColor = c;
				});
				_segi.skyIntensity = IblUi.SliderGUI(_segi.skyIntensity, 0f, 8f, 1f, "Sky Intensity", "The brightness of the sky light.", 3, true);
				_segi.sphericalSkylight = IblUi.ToggleGUI(_segi.sphericalSkylight, new GUIContent("Spherical Skylight", "If enabled, light from the sky will come from all directions. If disabled, light from the sky will only come from the top hemisphere."));

				IblUi.Space(UIUtils.space);

				_segi.temporalBlendWeight = IblUi.SliderGUI(_segi.temporalBlendWeight, 0.01f, 1f, 0.1f, "Temporal Blend Weight", "The lower the value, the more previous frames will be blended with the current frame. Lower values result in smoother GI that updates less quickly.", 3, true);
				_segi.useBilateralFiltering = IblUi.ToggleGUI(_segi.useBilateralFiltering, new GUIContent("Bilateral Filtering", "Enables filtering of the GI result to reduce noise."));
				_segi.halfResolution = IblUi.ToggleGUI(_segi.halfResolution, new GUIContent("Half Resolution", "If enabled, GI tracing will be done at half screen resolution. Improves speed of GI tracing."));
				_segi.stochasticSampling = IblUi.ToggleGUI(_segi.stochasticSampling, new GUIContent("Stochastic Sampling", "If enabled, uses random jitter to reduce banding and discontinuities during GI tracing."));

				_segi.cones = Mathf.RoundToInt(IblUi.SliderGUI(_segi.cones, 1f, 128f, 6f, "Cones", "The number of cones that will be traced in different directions for diffuse GI tracing. More cones result in a smoother result at the cost of performance.", 0, true));
				_segi.coneTraceSteps = Mathf.RoundToInt(IblUi.SliderGUI(_segi.coneTraceSteps, 1f, 32f, 14f, "Cone Trace Steps", "The number of tracing steps for each cone. Too few results in skipping thin features. Higher values result in more accuracy at the cost of performance.", 0, true));
				_segi.coneLength = IblUi.SliderGUI(_segi.coneLength, 0.1f, 2f, 1f, "Cone length", "The length of each cone that is traced for diffuse indirect lighting. This is essentially an adjustment for the \"GI radius\", meaning, this will directly influence how distant objects can be to contribute to GI. If you need a higher \"GI radius\", you may want to simply increase Voxel Space Size instead of this parameter.", 3, true);
				_segi.coneWidth = IblUi.SliderGUI(_segi.coneWidth, 0.5f, 6f, 5.5f, "Cone Width", "The width of each cone. Wider cones cause a softer and smoother result but affect accuracy and incrase over-occlusion. Thinner cones result in more accurate tracing with less coherent (more noisy) results and a higher tracing cost.", 3, true);
				_segi.coneTraceBias = IblUi.SliderGUI(_segi.coneTraceBias, 0f, 4f, 1f, "Cone Trace Bias", "The amount of offset above a surface that cone tracing begins. Higher values reduce \"voxel acne\" (similar to \"shadow acne\"). Values that are too high result in light-leaking.", 3, true);
				_segi.occlusionStrength = IblUi.SliderGUI(_segi.occlusionStrength, 0f, 4f, 1f, "Occlusion Strength", "The strength of shadowing solid objects will cause. Affects the strength of all indirect shadows.", 3, true);
				_segi.nearOcclusionStrength = IblUi.SliderGUI(_segi.nearOcclusionStrength, 0f, 4f, 0.5f, "Near Occlusion Strength", "The strength of shadowing nearby solid objects will cause. Only affects the strength of very close blockers.", 3, true);
				_segi.farOcclusionStrength = IblUi.SliderGUI(_segi.farOcclusionStrength, 0.1f, 4f, 1f, "Far Occlusion Strength", "How much light far occluders block. This value gives additional light blocking proportional to the width of the cone at each trace step.", 3, true);
				_segi.farthestOcclusionStrength = IblUi.SliderGUI(_segi.farthestOcclusionStrength, 0.1f, 4f, 1f, "Farthest Occlusion Strength", "How much light the farthest occluders block. This value gives additional light blocking proportional to (cone width)^2 at each trace step.", 3, true);
				_segi.occlusionPower = IblUi.SliderGUI(_segi.occlusionPower, 0.001f, 4f, 1.5f, "Occlusion Power", "The strength of shadowing far solid objects will cause. Only affects the strength of far blockers. Decrease this value if wide cones are causing over-occlusion.", 3, true);
				_segi.nearLightGain = IblUi.SliderGUI(_segi.nearLightGain, 0f, 4f, 1f, "Near Light Gain", "Affects the attenuation of indirect light. Higher values allow for more close-proximity indirect light. Lower values reduce close-proximity indirect light, sometimes resulting in a cleaner result.", 3, true);
				_segi.giGain = IblUi.SliderGUI(_segi.giGain, 0f, 4f, 1f, "GI Gain", "The overall brightness of indirect light. For Near Light Gain values around 1, a value of 1 for this property is recommended for a physically-accurate result.", 3, true);

				IblUi.Space(UIUtils.space);

				_segi.secondaryBounceGain = IblUi.SliderGUI(_segi.secondaryBounceGain, 0f, 4f, 1f, "Secondary Bounce Gain", "Affects the strength of secondary/infinite bounces. Be careful, values above 1 can cause runaway light bouncing and flood areas with extremely bright light!", 3, true);
				_segi.secondaryCones = Mathf.RoundToInt(IblUi.SliderGUI(_segi.secondaryCones, 3f, 16f, 6f, "Secondary Cones", "The number of secondary cones that will be traced for calculating infinte bounces. Increasing this value improves the accuracy of secondary bounces at the cost of performance. Note: the performance cost of this scales with voxelized scene complexity.", 0, true));
				_segi.secondaryOcclusionStrength = IblUi.SliderGUI(_segi.secondaryOcclusionStrength, 0.1f, 4f, 1f, "Secondary Occlusion Strength", "The strength of light blocking during secondary bounce tracing. Be careful, a value too low can cause runaway light bouncing and flood areas with extremely bright light!", 3, true);

				IblUi.Space(UIUtils.space);

				_segi.doReflections = IblUi.ToggleGUI(_segi.doReflections, new GUIContent("Do Reflections", "Enable this for cone-traced reflections."));
				_segi.reflectionSteps = Mathf.RoundToInt(IblUi.SliderGUI(_segi.reflectionSteps, 12f, 128f, 64f, "Reflection Steps", "Number of reflection trace steps.", 0, true));
				_segi.reflectionOcclusionPower = IblUi.SliderGUI(_segi.reflectionOcclusionPower, 0.001f, 4f, 1f, "Reflection Occlusion Power", "Strength of light blocking during reflection tracing.", 3, true);
				_segi.skyReflectionIntensity = IblUi.SliderGUI(_segi.skyReflectionIntensity, 0f, 1f, 1f, "Sky Reflection Intensity", "Intensity of sky reflections.", 3, true);

				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://github.com/sonicether/SEGI/blob/master/User%20Guide.pdf");
			}
		}

		private void AmplifyBloomModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Amplify Bloom"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				{
					IblUi.Label(new GUIContent("Debug", "Debug each bloom/feature stage to screen."), UIUtils.labelStyle);
					_amplifyBloom.DebugToScreen = (DebugToScreenEnum)IblUi.SelectionGrid((int)_amplifyBloom.DebugToScreen, _possibleDebugToScreenEnumNames, 2, UIUtils.buttonStyleStrechWidth);

					IblUi.Label(new GUIContent("Technique", "Method which will be used to upscale results. Realistic is visually more robust but less efficient."), UIUtils.labelStyle);
					_amplifyBloom.UpscaleQuality = (UpscaleQualityEnum)IblUi.SelectionGrid((int)_amplifyBloom.UpscaleQuality, _possibleUpscaleQualityEnumNames, 2, UIUtils.buttonStyleStrechWidth);
					IblUi.Label(new GUIContent("Source Downscale", "Initial render texture scale on which the Main Threshold will be written."), UIUtils.labelStyle);
					_amplifyBloom.MainThresholdSize = (MainThresholdSizeEnum)IblUi.SelectionGrid((int)_amplifyBloom.MainThresholdSize, _possibleMainThresholdSizeEnumNames, 3, UIUtils.buttonStyleStrechWidth);
					IblUi.Label(new GUIContent("Precision", "Switch between HDR and LDR Render Texture formats."), UIUtils.labelStyle);
					_amplifyBloom.CurrentPrecisionMode = (PrecisionModes)IblUi.SelectionGrid((int)_amplifyBloom.CurrentPrecisionMode, _possiblePrecisionModesNames, 2, UIUtils.buttonStyleStrechWidth);

					if (_amplifyBloom.CurrentPrecisionMode == PrecisionModes.Low)
						_amplifyBloom.BloomRange = IblUi.SliderGUI(_amplifyBloom.BloomRange, 1f, 1000f, 500f, "Range", "LDR Tweakable range. Use to match HDR results.", 0);

					_amplifyBloom.OverallIntensity = IblUi.SliderGUI(_amplifyBloom.OverallIntensity, 0f, 8f, 0.8f, "Intensity", "Overall bloom intensity. Affects all the effects bellow.", 3);

					_amplifyBloom.OverallThreshold = IblUi.SliderGUI(_amplifyBloom.OverallThreshold, 0f, 8f, 0.53f, "Threshold", "Luminance threshold to setup what should generate bloom.", 3);

					if (_amplifyBloom.UpscaleQuality == UpscaleQualityEnum.Realistic)
					{
						_amplifyBloom.UpscaleBlurRadius = IblUi.SliderGUI(_amplifyBloom.UpscaleBlurRadius, 1f, 12f, 1f, "Upscale Blur Radius", "Radius used on the tent filter when upscaling to source size", 3);
					}
					//int weightMaxDowsampleCount = Mathf.Max(1, this._amplifyBloom.BloomDownsampleCount);
					{
						/*
						EditorGUI.indentLevel++;
						m_mipSettingsFoldout = CustomFoldout(m_mipSettingsFoldout, m_mipSettingGC);
						if (m_mipSettingsFoldout)
						{
							EditorGUI.indentLevel++;
							this._amplifyBloom.BloomDownsampleCount = UIUtils.SliderGUI2(this._amplifyBloom.BloomDownsampleCount, AmplifyBloomBase.MinDownscales, this._amplifyBloom.SoftMaxdownscales, 0f, m_downscaleAmountGC, 3);
							bool guiState = this._amplifyBloom.BloomDownsampleCount != 0;

							IblUi.Enabled = guiState;

							int featuresSourceId = this._amplifyBloom.FeaturesSourceId + 1;
							featuresSourceId = UIUtils.SliderGUI2(featuresSourceId, 1, this._amplifyBloom.BloomDownsampleCount, 0f, m_featuresSourceIdGC, 3);
							this._amplifyBloom.FeaturesSourceId = featuresSourceId - 1;
							EditorGUI.indentLevel--;
						}
						IblUi.Enabled = true;
						
						m_bloomWeightsFoldout = CustomFoldout(m_bloomWeightsFoldout, m_bloomWeightsFoldoutGC);
						if (m_bloomWeightsFoldout)
						{
							EditorGUI.indentLevel++;

							float blurStepSize = 15;
							float blurRadiusSize = 15;
							float weightSize = 25;

							IblUi.BeginHorizontal();
							IblUi.Space(41);
							GUILayout.LabelField(m_blurStepGC, GUILayout.MinWidth(blurStepSize));
							IblUi.Space(-26);
							GUILayout.LabelField(m_blurRadiusGC, GUILayout.MinWidth(blurRadiusSize));
							IblUi.Space(-27);
							GUILayout.LabelField(m_upscaleWeightGC, GUILayout.MinWidth(weightSize));
							IblUi.EndHorizontal();
							for (int i = 0; i < weightMaxDowsampleCount; i++)
							{
								IblUi.BeginHorizontal();
								GUILayout.LabelField(m_bloomWeightsLabelGCArr[i], GUILayout.Width(65));
								IblUi.Space(-30);

								this._amplifyBloom.GaussianSteps[i] = GUILayout.IntField(string.Empty, this._amplifyBloom.GaussianSteps[i], GUILayout.MinWidth(blurStepSize));
								this._amplifyBloom.GaussianSteps[i] = Mathf.Clamp(this._amplifyBloom.GaussianSteps[i], 0, AmplifyBloomBase.MaxGaussian);

								IblUi.Space(-27);
								this._amplifyBloom.GaussianRadius[i] = GUILayout.FloatField(string.Empty, this._amplifyBloom.GaussianRadius[i], GUILayout.MinWidth(blurRadiusSize));
								this._amplifyBloom.GaussianRadius[i] = Mathf.Max(this._amplifyBloom.GaussianRadius[i], 0f);

								IblUi.Space(-27);
								int id = weightMaxDowsampleCount - 1 - i;
								this._amplifyBloom.UpscaleWeights[id] = GUILayout.FloatField(string.Empty, this._amplifyBloom.UpscaleWeights[id], GUILayout.MinWidth(weightSize));
								this._amplifyBloom.UpscaleWeights[id] = Mathf.Max(this._amplifyBloom.UpscaleWeights[id], 0f);

								IblUi.EndHorizontal();
							}
							EditorGUI.indentLevel--;
						}
						*/
					}

					_amplifyBloom.TemporalFilteringActive = IblUi.ToggleGUI(_amplifyBloom.TemporalFilteringActive, new GUIContent("Temporal Filter", "Settings for temporal filtering configuration."));
					if (_amplifyBloom.TemporalFilteringActive)
					{
						//this._amplifyBloom.TemporalFilteringCurve = GUILayout.CurveField(m_filterCurveGC, this._amplifyBloom.TemporalFilteringCurve, TemporalCurveColor, TemporalCurveRanges);
						_amplifyBloom.TemporalFilteringValue = IblUi.SliderGUI(_amplifyBloom.TemporalFilteringValue, 0.01f, 1f, 0.05f, "Filter Value", "Position on the filter curve.", 3);
					}

					_amplifyBloom.SeparateFeaturesThreshold = IblUi.ToggleGUI(_amplifyBloom.SeparateFeaturesThreshold, new GUIContent("Features Threshold", "Settings for features threshold."));
					if (_amplifyBloom.SeparateFeaturesThreshold)
					{
						_amplifyBloom.FeaturesThreshold = IblUi.SliderGUI(_amplifyBloom.FeaturesThreshold, 0f, 8f, 0.05f, "Threshold", "Threshold value for second threshold layer.", 3);
					}
				}

				_amplifyBloom.ApplyLensDirt = IblUi.ToggleGUI(_amplifyBloom.ApplyLensDirt, new GUIContent("Lens Dirt", "Settings for Lens Dirt composition."), IblUi.TitleStyle);
				if (_amplifyBloom.ApplyLensDirt)
				{
					_amplifyBloom.LensDirtStrength = IblUi.SliderGUI(_amplifyBloom.LensDirtStrength, 0f, 8f, 2f, "Intensity", "Lens Dirt Intensity.", 3);
					//m_lensDirtWeightsFoldout = CustomFoldout(m_lensDirtWeightsFoldout, m_lensWeightsFoldoutGC);
					//if (m_lensDirtWeightsFoldout)
					//{
					//    for (int i = 0; i < this._amplifyBloom.BloomDownsampleCount; i++)
					//    {
					//        int id = this._amplifyBloom.BloomDownsampleCount - 1 - i;
					//        this._amplifyBloom.LensDirtWeights[id] = GUILayout.FloatField(m_lensWeightGCArr[i], this._amplifyBloom.LensDirtWeights[id]);
					//        this._amplifyBloom.LensDirtWeights[id] = Mathf.Max(this._amplifyBloom.LensDirtWeights[id], 0f);
					//    }
					//}
					IblUi.Label(new GUIContent("Dirt Texture", "Mask from which dirt is going to be created."), UIUtils.labelStyle);
					_amplifyBloom.LensDirtTexture = (LensDirtTextureEnum)IblUi.SelectionGrid((int)_amplifyBloom.LensDirtTexture, _possibleLensDirtTextureEnumNames, 2, UIUtils.buttonStyleStrechWidth);
				}

				_amplifyBloom.ApplyLensStardurst = IblUi.ToggleGUI(_amplifyBloom.ApplyLensStardurst, new GUIContent("Lens Starburst", "Settings for Lens Starburts composition."), IblUi.TitleStyle);
				if (_amplifyBloom.ApplyLensStardurst)
				{
					_amplifyBloom.LensStarburstStrength = IblUi.SliderGUI(_amplifyBloom.LensStarburstStrength, 0f, 8f, 2f, "Intensity", "Lens Starburst Intensity.", 3);
					//EditorGUI.indentLevel++;
					//m_lensStarburstWeightsFoldout = CustomFoldout(m_lensStarburstWeightsFoldout, m_lensWeightsFoldoutGC);
					//if (m_lensStarburstWeightsFoldout)
					//{
					//    for (int i = 0; i < this._amplifyBloom.BloomDownsampleCount; i++)
					//    {
					//        int id = this._amplifyBloom.BloomDownsampleCount - 1 - i;
					//        this._amplifyBloom.LensStarburstWeights[id] = UIUtils.SliderGUI2(this._amplifyBloom.LensStarburstWeights[id], 0f, 1f, 0f, m_lensWeightGCArr[i]);
					//        this._amplifyBloom.LensStarburstWeights[id] = Mathf.Max(this._amplifyBloom.LensStarburstWeights[id], 0f);
					//    }
					//}
					IblUi.Label(new GUIContent("Starburst Texture", "Mask from which starburst is going to be created."), UIUtils.labelStyle);
					_amplifyBloom.LensStardurstTex = (LensStarburstTextureEnum)IblUi.SelectionGrid((int)_amplifyBloom.LensStardurstTex, _possibleLensStarburstTextureEnumNames, 2, UIUtils.buttonStyleStrechWidth);
				}

				_amplifyBloom.BokehFilterInstance.ApplyBokeh = IblUi.ToggleGUI(_amplifyBloom.BokehFilterInstance.ApplyBokeh, new GUIContent("Bokeh Filter", "Settings for Bokeh filter generation."), IblUi.TitleStyle);
				if (_amplifyBloom.BokehFilterInstance.ApplyBokeh)
				{
					_amplifyBloom.BokehFilterInstance.ApplyOnBloomSource = IblUi.ToggleGUI(_amplifyBloom.BokehFilterInstance.ApplyOnBloomSource, new GUIContent("Apply on Bloom Source", "Bokeh filtering can either be applied on the bloom source and visually affect it or only affect features (lens flare/glare/dirt/starburst)."));
					IblUi.Label(new GUIContent("Aperture Shape", "Type of bokeh filter which will reshape bloom results."), UIUtils.labelStyle);
					_amplifyBloom.BokehFilterInstance.ApertureShape = (ApertureShape)IblUi.SelectionGrid((int)_amplifyBloom.BokehFilterInstance.ApertureShape, _possibleApertureShapeNames, 3, UIUtils.buttonStyleStrechWidth);

					_amplifyBloom.BokehFilterInstance.OffsetRotation = IblUi.SliderGUI(_amplifyBloom.BokehFilterInstance.OffsetRotation, 0, 360, 0f, "Rotation", "Filter overall rotation.", 0);
					_amplifyBloom.BokehFilterInstance.BokehSampleRadius = IblUi.SliderGUI(_amplifyBloom.BokehFilterInstance.BokehSampleRadius, 0.01f, 1f, 0.5f, "Sample Radius", "Bokeh imaginary camera DOF's radius.", 3);
					_amplifyBloom.BokehFilterInstance.Aperture = IblUi.SliderGUI(_amplifyBloom.BokehFilterInstance.Aperture, 0.01f, 0.05f, 0.05f, "Aperture", "Bokeh imaginary camera DOF's aperture.", 3);
					_amplifyBloom.BokehFilterInstance.FocalLength = IblUi.SliderGUI(_amplifyBloom.BokehFilterInstance.FocalLength, 0.018f, 0.055f, 0.018f, "Focal Length", "Bokeh imaginary camera DOF's focal length.", 3);
					_amplifyBloom.BokehFilterInstance.FocalDistance = IblUi.SliderGUI(_amplifyBloom.BokehFilterInstance.FocalDistance, 0.055f, 3f, 1.34f, "Focal Distance", "Bokeh imaginary camera DOF's focal distance.", 3);
					_amplifyBloom.BokehFilterInstance.MaxCoCDiameter = IblUi.SliderGUI(_amplifyBloom.BokehFilterInstance.MaxCoCDiameter, 0f, 2f, 0.18f, "Max CoC Diameter", "Bokeh imaginary camera DOF's Max Circle of Confusion diameter.", 3);
				}

				_amplifyBloom.LensFlareInstance.ApplyLensFlare = IblUi.ToggleGUI(_amplifyBloom.LensFlareInstance.ApplyLensFlare, new GUIContent("Lens Flare", "Overall settings for Lens Flare (Halo/Ghosts) generation."), IblUi.TitleStyle);
				if (_amplifyBloom.LensFlareInstance.ApplyLensFlare)
				{
					_amplifyBloom.LensFlareInstance.OverallIntensity = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.OverallIntensity, 0f, 8f, 1f, "Intensity", "Overall intensity for both halo and ghosts.", 3);
					_amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount = Mathf.RoundToInt(IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount, 0, 3, 1, "Blur amount", "Amount of blur applied on generated halo and ghosts.", 0));


					IblUi.Gradient(_amplifyBloom.LensFlareInstance.LensFlareGradient, "Radial Tint", "Dynamic tint color applied to halo and ghosts according to its screen position. Left most color on gradient corresponds to screen center.", false, () =>
					{
						_amplifyBloom.LensFlareInstance.TextureFromGradient();
					}, _amplifyBloom.LensFlareInstance.LensFlareGradientTexture);

					{
						IblUi.Label("Ghosts", IblUi.TitleStyle);
						_amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity, 0f, 8f, 0.8f, "Intensity", "Ghosts intensity.", 3);
						_amplifyBloom.LensFlareInstance.LensFlareGhostAmount = Mathf.RoundToInt(IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareGhostAmount, 3, AmplifyBloom.MaxGhosts, 0f, "Count", "Amount of ghosts generated from each bloom area.", 0));
						_amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal, 0.01f, 1.0f, 0.228f, "Dispersal", "Distance between ghost generated from the same bloom area.", 3);
						_amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion, 0, 10, 2, "Chromatic Distortion", "Amount of chromatic distortion applied on each ghost.", 3);
						_amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor, 1, 2, 1, "Power Factor", "Base on ghost fade power function.", 3);
						_amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff, 1, 128, 128, "Power Falloff", "Exponent on ghost fade power function.", 1);
					}

					{
						IblUi.Label("Halo", IblUi.TitleStyle);
						_amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity, 0f, 1f, 0.1f, "Intensity", "Halo intensity.", 3);
						_amplifyBloom.LensFlareInstance.LensFlareHaloWidth = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareHaloWidth, 0, 1, 0.573f, "Width", "Width/Radius of the generated halo.", 3);
						_amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion, 0, 10, 1.51f, "Chromatic Distortion", "Amount of chromatic distortion applied on halo.", 3);
						_amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor, 1, 2, 1f, "Power Factor", "Base on halo fade power function.", 3);
						_amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff = IblUi.SliderGUI(_amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff, 1, 128, 128f, "Power Falloff", "Exponent on halo fade power function.", 1);
					}
				}

				_amplifyBloom.LensGlareInstance.ApplyLensGlare = IblUi.ToggleGUI(_amplifyBloom.LensGlareInstance.ApplyLensGlare, new GUIContent("Lens Glare", "Settings for Anamorphic Lens Glare generation."), IblUi.TitleStyle);
				if (_amplifyBloom.LensGlareInstance.ApplyLensGlare)
				{
					_amplifyBloom.LensGlareInstance.Intensity = IblUi.SliderGUI(_amplifyBloom.LensGlareInstance.Intensity, 0f, 0.5f, 0.17f, "Intensity", "Lens Glare intensity.", 3);
					_amplifyBloom.LensGlareInstance.OverallStreakScale = IblUi.SliderGUI(_amplifyBloom.LensGlareInstance.OverallStreakScale, 0, 2, 1, "Streak Scale", "Overall glare streak length modifier.", 3);
					IblUi.ColorPickerGUI(_amplifyBloom.LensGlareInstance.OverallTint, Color.white, "Overall Tint", "Tint applied uniformly across each type of glare.", (c) => { _amplifyBloom.LensGlareInstance.OverallTint = c; });

					IblUi.Gradient(_amplifyBloom.LensGlareInstance.CromaticColorGradient, "Tint Along Glare", "Tint for spectral types along each ray. Leftmost color on the gradient corresponds to sample near bloom source.", false, () =>
					{
						_amplifyBloom.LensGlareInstance.SetDirty();
					});

					IblUi.Label(new GUIContent("Type", "Type of glare."), UIUtils.labelStyle);
					_amplifyBloom.LensGlareInstance.CurrentGlare = (GlareLibType)IblUi.SelectionGrid((int)_amplifyBloom.LensGlareInstance.CurrentGlare, _possibleGlareLibTypeNames, 2, UIUtils.buttonStyleStrechWidth);
					
					{
						_amplifyBloom.LensGlareInstance.PerPassDisplacement = IblUi.SliderGUI(_amplifyBloom.LensGlareInstance.PerPassDisplacement, 1, 8, 4, "Per Pass Displacement", "Distance between samples when creating each ray.", 3);
						_amplifyBloom.LensGlareInstance.GlareMaxPassCount = Mathf.RoundToInt(IblUi.SliderGUI(_amplifyBloom.LensGlareInstance.GlareMaxPassCount, 1, AmplifyGlare.MaxPasses, 4, "Max Per Ray Passes", "Max amount of passes used to build each ray. More passes means more defined and propagated rays but decreases performance.", 0));
					}
				}
				self.fixAmplifyBloomForUpscaledScreenshots = IblUi.ToggleGUI(self.fixAmplifyBloomForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
				if (IblUi.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Bloom/Manual");
			}
		}

		private void BlurModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Blur"), GUIStrings.disableVsEnable, IblUi.TitleStyle, () => self.MoveUp(effectData.effect), () => self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				_blur.downsample = Mathf.RoundToInt(IblUi.SliderGUI(_blur.downsample, 0, 2, 1, "Downsample", "The number of times the image will be downsampled before the blur is applied. Bigger blurs and faster speeds can be expected the more you downsample.", 0, true));
				_blur.blurSize = IblUi.SliderGUI(_blur.blurSize, 0, 10, 3, "Blur Size", "The spread used when filtering the image. Higher values produce bigger blurs.", 3, true);
				_blur.blurIterations = Mathf.RoundToInt(IblUi.SliderGUI(_blur.blurIterations, 1, 4, 2, "Blur Iterations", "The number of times the filter operations will be repeated.", 0, true));
				IblUi.Label(new GUIContent("Type", "Type of glare."), UIUtils.labelStyle);
				_blur.blurType = (BlurOptimized.BlurType)IblUi.SelectionGrid((int)_blur.blurType, _possibleBlurTypeNames, 2, UIUtils.buttonStyleStrechWidth);
				self.fixBlurForUpscaledScreenshots = IblUi.ToggleGUI(self.fixBlurForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
			}
		}


		private void GenericModule(EffectData effectData, int index)
		{
			effectData.enabled = IblUi.ToggleGUI(effectData.enabled, new GUIContent(index + " " + effectData.effect.GetType().Name), IblUi.TitleStyle);
		}


		private void CharaMakerWarningWindow(int id)
		{
			IblUi.BeginVertical();
			IblUi.Label("Using image based lighting in the chara maker requires deferred renderingpath and HDR, meanwhile you can't use background image. DO you want to continue?", UIUtils.labelStyle3);
			IblUi.FlexibleSpace();
			IblUi.BeginHorizontal();
			IblUi.FlexibleSpace();
			if (IblUi.Button("Just this time", UIUtils.buttonStyleNoStretch))
			{
				Console.WriteLine("HSIBL Info: Changing rendering path to deferred shading.");
				cam.renderingPath = RenderingPath.DeferredShading;
				cam.clearFlags = (CameraClearFlags)Settings.basicSettings.CharaMakerBackgroundType;
			}
			IblUi.FlexibleSpace();
			if (IblUi.Button("Always", UIUtils.buttonStyleNoStretch))
			{
				Settings.basicSettings.CharaMakerReform = true;
				Console.WriteLine("HSIBL Info: Changing rendering path to deferred shading.");
				cam.renderingPath = RenderingPath.DeferredShading;
				cam.clearFlags = (CameraClearFlags)Settings.basicSettings.CharaMakerBackgroundType;
			}
			IblUi.FlexibleSpace();
			if (IblUi.Button("No", UIUtils.buttonStyleNoStretch))
			{
				_showUI = false;
			}
			IblUi.FlexibleSpace();
			IblUi.EndHorizontal();
			IblUi.FlexibleSpace();
			IblUi.EndVertical();

		}

		private void ErrorWindow(int id)
		{

			IblUi.BeginVertical();
			IblUi.Label("Error" + _errorcode + ": Please make sure you have installed HS linear rendering experiment (Version ≥ 3).", UIUtils.labelStyle3);
			IblUi.FlexibleSpace();
			if (IblUi.Button("OK", UIUtils.buttonStyleNoStretch))
			{
				_showUI = false;
			}
			IblUi.FlexibleSpace();
			IblUi.EndVertical();
		}
		#endregion

		#region Private Methods
		private void CameraControlOffOnGUI()
		{
			switch (Event.current.type)
			{
				case EventType.MouseDown:
				case EventType.MouseDrag:
					cameraCtrlOff = true;
					break;
				default:
					return;
			}

		}

		private IEnumerator RotateCharater()
		{
			while (true)
			{
				if (_autoRotate && _cMfemale != null)
				{
					_charaRotate += _autoRotateSpeed;
					if (_charaRotate > 180f)
					{
						_charaRotate -= 360f;
					}
					else if (_charaRotate < -180f)
					{
						_charaRotate += 360f;
					}
					_cMfemale.SetRotation(_rotateValue.x, _rotateValue.y + _charaRotate, _rotateValue.z);
				}
				yield return new WaitForEndOfFrame();
			}
		}

		private void CharaRotateModule()
		{
			if (_cMfemale)
			{
				IblUi.BeginHorizontal();
				IblUi.Label("Rotate Character", IblUi.TitleStyle);
				IblUi.FlexibleSpace();
				_autoRotate = IblUi.ToggleButton(_autoRotate, new GUIContent("Auto Rotate"));
				IblUi.EndHorizontal();
				if (IblUi.Classic == false)
				{
					// Addition: the kit panel draws these as full sliders, with a number box and a reset button
					if (_autoRotate)
						_autoRotateSpeed = IblUi.SliderGUI(_autoRotateSpeed, -5f, 5f, 0.2f, "Auto Rotate Speed", "Degrees added to the rotation every frame while Auto Rotate is on.", 3);
					else
					{
						_charaRotate = IblUi.SliderGUI(_charaRotate, -180f, 180f, 0f, "Rotation", "Turns the character around its vertical axis.", 1);
						_cMfemale.SetRotation(_rotateValue.x, _rotateValue.y + _charaRotate, _rotateValue.z);
					}
				}
				else if (_autoRotate)
				{
					IblUi.Label("Auto Rotate Speed " + _autoRotateSpeed.ToString("N3"), UIUtils.labelStyle);
					_autoRotateSpeed = IblUi.HorizontalSlider(_autoRotateSpeed, -5f, 5f, UIUtils.sliderStyle, UIUtils.thumbStyle);
				}
				else
				{
					_charaRotate = IblUi.HorizontalSlider(_charaRotate, -180f, 180f, UIUtils.sliderStyle, UIUtils.thumbStyle);
					_cMfemale.SetRotation(_rotateValue.x, _rotateValue.y + _charaRotate, _rotateValue.z);
				}
				IblUi.Space(UIUtils.space);
			}
		}

		private void UserCustomModule()
		{
			WindowSizeModule();
			CharaMakerDefaultCubemapModule();
			PresetsModule();
		}

		private void WindowSizeModule()
		{
			IblUi.BeginHorizontal();
			IblUi.Label(GUIStrings.customWindow, IblUi.TitleStyle);
			IblUi.FlexibleSpace();
			if (IblUi.Button(GUIStrings.customWindowRemember, UIUtils.buttonStyleNoStretch, GUILayout.ExpandWidth(false)))
			{
				ModPrefs.SetFloat("HSIBL", "Window.width", UIUtils.windowRect.width);
				ModPrefs.SetFloat("HSIBL", "Window.height", UIUtils.windowRect.height);
				ModPrefs.SetFloat("HSIBL", "Window.x", UIUtils.windowRect.x);
				ModPrefs.SetFloat("HSIBL", "Window.y", UIUtils.windowRect.y);
				savedWindowWidth = UIUtils.windowRect.width;
				savedWindowHeight = UIUtils.windowRect.height;
			}
			IblUi.EndHorizontal();
			IblUi.Space(UIUtils.space);
			float tempWinWidth = IblUi.SliderGUI(
												UIUtils.windowRect.width,
												UIUtils.minWidth,
												UIUtils.Screen.width * 0.5f,
												savedWindowWidth,
												GUIStrings.windowWidth,
												"IBL window width.",
												0);
			if (Mathf.Approximately(tempWinWidth, UIUtils.windowRect.width) == false)
			{
				UIUtils.windowRect.x += (UIUtils.windowRect.width - tempWinWidth) * (UIUtils.windowRect.x) / (UIUtils.Screen.width - UIUtils.windowRect.width);
				UIUtils.windowRect.width = tempWinWidth;
			}
			UIUtils.windowRect.height = IblUi.SliderGUI(
														  UIUtils.windowRect.height,
														  UIUtils.Screen.height * 0.2f,
														  UIUtils.Screen.height * 0.9f,
														  savedWindowHeight,
														  GUIStrings.windowHeight,
														  "IBL window height.",
														  0);
		}

		private void CharaMakerDefaultCubemapModule()
		{
			if (_isStudio == false && IblUi.Button("Set current cubemap as default for the chara maker", UIUtils.buttonStyleStrechWidth))
			{
				if (_selectedCubemap == -1)
					_defaultCharaMakerCubemap = "";
				else
					_defaultCharaMakerCubemap = _cubeMapFileNames[_selectedCubemap];
			}
		}

		private void PresetsModule()
		{
			IblUi.Label("Presets", IblUi.TitleStyle);
			float thumbW = 0f;
			if (IblUi.Classic)
			{
				GUILayout.BeginVertical(GUI.skin.box);
				thumbW = GUI.skin.verticalScrollbarThumb.fixedWidth;
				GUI.skin.verticalScrollbarThumb.fixedWidth = 40;
				_presetsScroll = GUILayout.BeginScrollView(_presetsScroll, false, true, UIUtils.horizontalScrollbar, UIUtils.verticalScrollbar, GUILayout.MinHeight(160), GUILayout.MaxHeight(800));
			}
			if (_presets.Length != 0)
				foreach (string preset in _presets)
				{
					if (IblUi.Button(preset, UIUtils.buttonStyleStrechWidth))
					{
						if (_removePresetMode)
							DeletePreset(preset + ".xml");
						else
							LoadPreset(preset + ".xml");
					}
				}
			else
				IblUi.Label("No preset...", UIUtils.labelStyle);
			if (IblUi.Classic)
			{
				GUILayout.EndScrollView();
				GUI.skin.verticalScrollbarThumb.fixedWidth = thumbW;
				GUILayout.EndVertical();
			}
			IblUi.BeginHorizontal();
			IblUi.Label("Name: ", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
			Color c = IblUi.Color;
			if (_presets.Any(p => p.Equals(_presetName, StringComparison.OrdinalIgnoreCase)))
				IblUi.Color = ColorPresets.Changed;
			_presetName = IblUi.TextField(_presetName, UIUtils.textFieldStyle2);
			IblUi.Color = c;
			IblUi.EndHorizontal();
			IblUi.BeginHorizontal();
			IblUi.Enabled = _presetName.Length != 0;
			if (IblUi.Button("Save current settings", UIUtils.buttonStyleStrechWidth))
			{
				_presetName = _presetName.Trim();
				_presetName = string.Join("_", _presetName.Split(Path.GetInvalidFileNameChars()));
				if (_presetName.Length != 0)
				{
					SavePreset(_presetName + ".xml");
					RefreshPresetList();
					_removePresetMode = false;
				}
			}
			IblUi.Enabled = true;
			if (_removePresetMode)
				IblUi.Color = ColorPresets.Changed;
			IblUi.Enabled = _presets.Length != 0;
			if (IblUi.Button(_removePresetMode ? "Click on preset" : "Delete preset", UIUtils.buttonStyleStrechWidth))
				_removePresetMode = !_removePresetMode;
			IblUi.Enabled = true;
			IblUi.Color = c;
			IblUi.EndHorizontal();
		}

		private void RefreshPresetList()
		{
			if (Directory.Exists(_presetFolder))
			{
				_presets = Directory.GetFiles(_presetFolder, "*.xml");
				for (int i = 0; i < _presets.Length; i++)
					_presets[i] = Path.GetFileNameWithoutExtension(_presets[i]);
			}
		}

		// note: Avoid GC by keeping a persistent WaitForEndOfFrame instead of creating a new one each frame.
		static readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
		private IEnumerator EndOfFrame()
		{
			while (true)
			{
				if ((!_cubemapLoaded || RenderSettings.ambientMode != AmbientMode.Skybox) && prevFrameAmbientIntensity != RenderSettings.ambientIntensity)
				{
					// Manually trigger environemnt update when ambientIntensity changes are detected, to account for the 'none' cubemap selection; only the flag needs to be set here
					// note: this doesn't seem to have an effect when AmbientMode is Custom
					prevFrameAmbientIntensity = RenderSettings.ambientIntensity;
					_environmentUpdateFlag = true;
				}
				if (_environmentUpdateFlag)
				{
					UpdateEnvironment();
					_environmentUpdateFlag = false;
				}
				yield return waitForEndOfFrame;
			}
		}

		private void UpdateEnvironment()
		{
			DynamicGI.UpdateEnvironment();
			if (_probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
				_probeComponent.RenderProbe();
		}

		private int LoadCubemap(int index)
		{
			if (_previousSelectedCubeMap != index)
			{
				switch (index)
				{
					case -1:
						_skybox.Skybox = _originalSkybox;
						_skybox.ApplySkybox();
						//RenderSettings.skybox = this._originalSkybox;
						RenderSettings.ambientMode = _originalAmbientMode;
						RenderSettings.defaultReflectionMode = _originalDefaultReflectionMode;
						_cubemapLoaded = false;
						_environmentUpdateFlag = true;
						break;
					case 0:
						_proceduralSkybox.ApplySkybox();
						_proceduralSkybox.ApplySkyboxParams();
						_cubemapLoaded = true;
						_environmentUpdateFlag = true;
						break;
					default:
						StartCoroutine(LoadCubemapFileAsync(_cubemapFolder.lstFile[index - 1].FileName));
						break;
				}
				_environmentUpdateFlag = true;
			}
			_previousSelectedCubeMap = index;
			return index;
		}

		private int LoadCubemap(string cubemapName)
		{
			int index;
			switch (cubemapName)
			{
				case "":
					index = -1;
					break;
				case "Procedural":
					index = 0;
					break;
				default:
					index = -1;
					for (int i = 0; i < _cubemapFolder.lstFile.Count; i++)
					{
						FolderAssist.FileInfo fileInfo = _cubemapFolder.lstFile[i];
						if (string.Compare(cubemapName, fileInfo.FileName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							index = i + 1;
							break;
						}
					}
					break;
			}
			return LoadCubemap(index);
		}

		private IEnumerator LoadCubemapFileAsync(string filename)
		{
			_isLoading = true;
			AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync($"{Application.dataPath}/../abdata/plastic/cubemaps/{filename}.unity3d");
			yield return assetBundleCreateRequest;
			AssetBundle cubemapbundle = assetBundleCreateRequest.assetBundle;
			AssetBundleRequest bundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetAsync<Material>("skybox");
			yield return bundleRequest;
			_skybox.Skybox = bundleRequest.asset as Material;
			cubemapbundle.Unload(false);
			_skybox.ApplySkybox();
			_skybox.ApplySkyboxParams();
			_environmentUpdateFlag = true;

			_cubemapLoaded = true;
			cubemapbundle = null;
			bundleRequest = null;
			assetBundleCreateRequest = null;
			Resources.UnloadUnusedAssets();
			_isLoading = false;
			yield break;
		}
		#endregion

		#region Saves
		private void OnSceneLoad(string path, XmlNode node)
		{
			StartCoroutine(OnSceneLoad_Routine(node));
		}

		private IEnumerator OnSceneLoad_Routine(XmlNode node)
		{
			yield return null;
			yield return null;
			if (node == null)
				yield break;
			LoadConfig(node);
			TakeSnapshot(); // addition: a new scene's own settings become the compare's "before"
			ResetUndo(); // addition: and the start of a new undo history
		}

		private void OnSceneSave(string path, XmlTextWriter writer)
		{
			SaveConfig(writer);
		}

		private void SavePreset(string name)
		{
			if (Directory.Exists(_presetFolder) == false)
				Directory.CreateDirectory(_presetFolder);
			using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(_presetFolder, name), Encoding.UTF8))
			{
				writer.WriteStartElement("root");
				SaveConfig(writer);
				writer.WriteEndElement();
			}
		}

		private void LoadPreset(string name)
		{
			string path = Path.Combine(_presetFolder, name);
			if (File.Exists(path) == false)
				return;
			XmlDocument doc = new XmlDocument();
			doc.Load(path);
			LoadConfig(doc.FirstChild);
		}

		private void DeletePreset(string name)
		{
			File.Delete(Path.GetFullPath(Path.Combine(_presetFolder, name)));
			string thumbnail = ThumbnailPath(Path.GetFileNameWithoutExtension(name)); // addition: its thumbnail goes with it
			if (File.Exists(thumbnail))
				File.Delete(thumbnail);
			_removePresetMode = false;
			RefreshPresetList();
		}

		private void LoadConfig(XmlNode node)
		{
			List<KeyValuePair<int, ObjectCtrlInfo>> dic = null;
			if (_isStudio)
				dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
			if (_partialLoad == false) // addition: a per-effect reset loads only that effect's elements
			{
				CompareOnExternalLoad(); // addition: a load while the compare snapshot is shown ends the compare
				self.ResetOrder();
				_colorLooks?.Reset(); // addition: scenes / presets saved without Color Looks have none
				_screenOverlays?.Reset(); // addition: same for screen overlays
				_animatedSky?.Reset(); // addition: and the animated sky
				_skyBackdrop?.Reset(); // addition: and the sky backdrop
			}

			bool isStudioInstance = _isStudio && Studio.Studio.Instance != null;

			foreach (XmlNode moduleNode in node.ChildNodes)
			{
				switch (moduleNode.Name)
				{
					case "animatedSky": // addition
						_animatedSky?.Load(moduleNode);
						break;
					case "skyBackdrop": // addition
						_skyBackdrop?.Load(moduleNode);
						break;
					case "screenOverlays": // addition
						_screenOverlays?.Load(moduleNode);
						break;
					case "colorLooks": // addition
						if (_colorLooks != null)
						{
							_colorLooks.Look1 = _colorLooks.IndexOf(moduleNode.Attributes["look1"]?.Value);
							_colorLooks.Look2 = _colorLooks.IndexOf(moduleNode.Attributes["look2"]?.Value);
							if (moduleNode.Attributes["strength1"] != null)
								_colorLooks.Strength1 = XmlConvert.ToSingle(moduleNode.Attributes["strength1"].Value);
							if (moduleNode.Attributes["strength2"] != null)
								_colorLooks.Strength2 = XmlConvert.ToSingle(moduleNode.Attributes["strength2"].Value);
						}
						break;
					case "cubemap":
						_hideSkybox = XmlConvert.ToBoolean(moduleNode.Attributes["hide"].Value);
						int index = XmlConvert.ToInt32(moduleNode.Attributes["index"].Value);
						if (index == 0) //Procedural
							_selectedCubemap = LoadCubemap(index);
						else //Other
							_selectedCubemap = LoadCubemap(moduleNode.Attributes["fileName"].Value);
						break;
					case "skybox":
						_proceduralSkybox.skyboxparams.exposure = XmlConvert.ToSingle(moduleNode.Attributes["proceduralExposure"].Value);
						_proceduralSkybox.skyboxparams.sunsize = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSunsize"].Value);
						// addition: sun position, absent from older saves
						_proceduralSkybox.skyboxparams.sunheight = moduleNode.Attributes["proceduralSunHeight"] != null ? XmlConvert.ToSingle(moduleNode.Attributes["proceduralSunHeight"].Value) : 35f;
						_proceduralSkybox.skyboxparams.sunazimuth = moduleNode.Attributes["proceduralSunAzimuth"] != null ? XmlConvert.ToSingle(moduleNode.Attributes["proceduralSunAzimuth"].Value) : 140f;
						_proceduralSkybox.skyboxparams.atmospherethickness = XmlConvert.ToSingle(moduleNode.Attributes["proceduralAtmospherethickness"].Value);
						_proceduralSkybox.skyboxparams.skytint = new Color(
							XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintR"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintG"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintB"].Value));
						_proceduralSkybox.skyboxparams.groundcolor = new Color(
							XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorR"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorG"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorB"].Value));

						_skybox.skyboxparams.rotation = XmlConvert.ToSingle(moduleNode.Attributes["classicRotation"].Value);
						_skybox.skyboxparams.exposure = XmlConvert.ToSingle(moduleNode.Attributes["classicExposure"].Value);
						_skybox.skyboxparams.tint = new Color(
							XmlConvert.ToSingle(moduleNode.Attributes["classicTintR"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["classicTintG"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["classicTintB"].Value));
						RenderSettings.ambientIntensity = XmlConvert.ToSingle(moduleNode.Attributes["ambientIntensity"].Value);
						break;
					case "reflection":
						_probeComponent.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
						_probeComponent.refreshMode = XmlConvert.ToBoolean(moduleNode.Attributes["refreshOnDemand"].Value) ? ReflectionProbeRefreshMode.ViaScripting : ReflectionProbeRefreshMode.EveryFrame;
						_probeComponent.timeSlicingMode = (ReflectionProbeTimeSlicingMode)XmlConvert.ToInt32(moduleNode.Attributes["timeSlicing"].Value);
						_reflectionProbeResolution = XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
						_probeComponent.resolution = _possibleReflectionProbeResolutions[_reflectionProbeResolution];
						_probeComponent.intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
						if (moduleNode.Attributes["positionX"] != null)
							probeGameObject.transform.position = new Vector3(
								XmlConvert.ToSingle(moduleNode.Attributes["positionX"].Value),
								XmlConvert.ToSingle(moduleNode.Attributes["positionY"].Value),
								XmlConvert.ToSingle(moduleNode.Attributes["positionZ"].Value));
						if (moduleNode.Attributes["shadowDistance"] != null)
							_probeComponent.shadowDistance = XmlConvert.ToSingle(moduleNode.Attributes["shadowDistance"].Value);
						_probeComponent.nearClipPlane = moduleNode.Attributes["nearClipPlane"] != null ? XmlConvert.ToSingle(moduleNode.Attributes["nearClipPlane"].Value) : 1.1f;
						RenderSettings.reflectionBounces = moduleNode.Attributes["bounce"] != null ? XmlConvert.ToInt32(moduleNode.Attributes["bounce"].Value) : 1;
						_probeComponent.boxProjection = moduleNode.Attributes["boxProjection"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["boxProjection"].Value);
						if (moduleNode.Attributes["cullingMask"] != null)
							_probeComponent.cullingMask = XmlConvert.ToInt32(moduleNode.Attributes["cullingMask"].Value);
						else
						{
							_probeComponent.cullingMask = -1;
							_probeComponent.cullingMask &= ~(1 << 24); //Suimono Water
						}
						_probeComponent.cullingMask &= ~(1 << 27); //SEGI GI Blockers
						_probeComponent.cullingMask &= ~(1 << 26); //Suimono FX
						_probeComponent.cullingMask &= ~(1 << 25); //Suimono Depth
						break;
					case "defaultLight":
						bool frontAnchor = XmlConvert.ToBoolean(moduleNode.Attributes["frontAnchoredToCamera"].Value);

						_frontRotate.x = XmlConvert.ToSingle(moduleNode.Attributes["frontRotateX"].Value);
						_frontRotate.y = XmlConvert.ToSingle(moduleNode.Attributes["frontRotateY"].Value);

						if (frontAnchor)
						{
							_lightsObj.transform.parent = cam.transform;
							_frontDirectionalLight.transform.parent = _lightsObj.transform;
							_lightsObj.transform.localRotation = isStudioInstance ? Quaternion.Euler(Studio.Studio.Instance.sceneInfo.cameraLightRot[0], Studio.Studio.Instance.sceneInfo.cameraLightRot[1], 0f) : _lightsObjDefaultRotation;
							_frontDirectionalLight.transform.localRotation = _frontLightDefaultRotation;
						}
						else
						{
							_lightsObj.transform.parent = null;
							_frontDirectionalLight.transform.parent = null;
							_frontDirectionalLight.transform.eulerAngles = _frontRotate;
						}

						_frontDirectionalLight.intensity = XmlConvert.ToSingle(moduleNode.Attributes["frontIntensity"].Value);
						_frontDirectionalLight.color = new Color(
							XmlConvert.ToSingle(moduleNode.Attributes["frontColorR"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["frontColorG"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["frontColorB"].Value));

						bool backAnchor = XmlConvert.ToBoolean(moduleNode.Attributes["backAnchoredToCamera"].Value);
						_backDirectionalLight.transform.parent = backAnchor == false ? null : cam.transform;

						if (backAnchor)
						{
							_backDirectionalLight.transform.parent = cam.transform;
							_backDirectionalLight.transform.localRotation = _backLightDefaultRotation;
						}
						else
						{
							_backDirectionalLight.transform.parent = null;
							_backDirectionalLight.transform.eulerAngles = _backRotate;
						}

						_backRotate.x = XmlConvert.ToSingle(moduleNode.Attributes["backRotateX"].Value);
						_backRotate.y = XmlConvert.ToSingle(moduleNode.Attributes["backRotateY"].Value);

						_backDirectionalLight.intensity = XmlConvert.ToSingle(moduleNode.Attributes["backIntensity"].Value);
						_backDirectionalLight.color = new Color(
							XmlConvert.ToSingle(moduleNode.Attributes["backColorR"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["backColorG"].Value),
							XmlConvert.ToSingle(moduleNode.Attributes["backColorB"].Value));

						break;

					case "smaa":
						if (_smaa != null)
						{
							EffectData effectData = self.effectsDictionary[_smaa];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							SMAA.GlobalSettings settings = _smaa.settings;
							SMAA.PredicationSettings predication = _smaa.predication;
							SMAA.TemporalSettings temporal = _smaa.temporal;
							SMAA.QualitySettings quality = _smaa.quality;

							settings.debugPass = (SMAA.DebugPass)XmlConvert.ToInt32(moduleNode.Attributes["debugPass"].Value);
							settings.quality = (SMAA.QualityPreset)XmlConvert.ToInt32(moduleNode.Attributes["quality"].Value);
							settings.edgeDetectionMethod = (SMAA.EdgeDetectionMethod)XmlConvert.ToInt32(moduleNode.Attributes["edgeDetectionMethod"].Value);
							quality.diagonalDetection = XmlConvert.ToBoolean(moduleNode.Attributes["qualityDiagonalDetection"].Value);
							quality.cornerDetection = XmlConvert.ToBoolean(moduleNode.Attributes["qualityCornerDetection"].Value);
							quality.threshold = XmlConvert.ToSingle(moduleNode.Attributes["qualityThreshold"].Value);
							quality.depthThreshold = XmlConvert.ToSingle(moduleNode.Attributes["qualityDepthThreshold"].Value);
							quality.maxSearchSteps = XmlConvert.ToInt32(moduleNode.Attributes["qualityMaxSearchSteps"].Value);
							quality.maxDiagonalSearchSteps = XmlConvert.ToInt32(moduleNode.Attributes["qualityMaxDiagonalSearchSteps"].Value);
							quality.cornerRounding = XmlConvert.ToInt32(moduleNode.Attributes["qualityCornerRounding"].Value);
							quality.localContrastAdaptationFactor = XmlConvert.ToSingle(moduleNode.Attributes["qualityLocalContrastAdaptationFactor"].Value);
							predication.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["predicationEnabled"].Value);
							predication.threshold = XmlConvert.ToSingle(moduleNode.Attributes["predicationThreshold"].Value);
							predication.scale = XmlConvert.ToSingle(moduleNode.Attributes["predicationScale"].Value);
							predication.strength = XmlConvert.ToSingle(moduleNode.Attributes["predicationStrength"].Value);
							temporal.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["temporalEnabled"].Value);
							temporal.fuzzSize = XmlConvert.ToSingle(moduleNode.Attributes["temporalFuzzSize"].Value);

							ClampFieldValues(quality);
							ClampFieldValues(predication);
							ClampFieldValues(temporal);

							_smaa.quality = quality;
							_smaa.temporal = temporal;
							_smaa.predication = predication;
							_smaa.settings = settings;
						}
						break;

					case "lens":
						if (_lensManager != null)
						{
							cam.fieldOfView = XmlConvert.ToSingle(moduleNode.Attributes["fov"].Value);
							if (_isStudio)
								Studio.Studio.Instance.cameraCtrl.fieldOfView = cam.fieldOfView;

							_lensManager.distortion.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["distortionEnabled"].Value);
							_lensManager.distortion.amount = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmount"].Value);
							_lensManager.distortion.amountX = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmountX"].Value);
							_lensManager.distortion.amountY = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmountY"].Value);
							_lensManager.distortion.scale = XmlConvert.ToSingle(moduleNode.Attributes["distortionScale"].Value);
							ClampFieldValues(_lensManager.distortion);

							_lensManager.chromaticAberration.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["chromaticAberrationEnabled"].Value);
							_lensManager.chromaticAberration.amount = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationAmount"].Value);
							ClampFieldValues(_lensManager.chromaticAberration);

							if (moduleNode.Attributes["chromaticAberrationColorR"] != null)
							{
								//c = Color.green;
								_lensManager.chromaticAberration.color = new Color(
									XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorR"].Value),
									XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorG"].Value),
									XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorB"].Value));
							}

							_lensManager.vignette.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["vignetteEnabled"].Value);
							_lensManager.vignette.intensity = XmlConvert.ToSingle(moduleNode.Attributes["vignetteIntensity"].Value);
							_lensManager.vignette.smoothness = XmlConvert.ToSingle(moduleNode.Attributes["vignetteSmoothness"].Value);
							_lensManager.vignette.roundness = XmlConvert.ToSingle(moduleNode.Attributes["vignetteRoundness"].Value);
							_lensManager.vignette.desaturate = XmlConvert.ToSingle(moduleNode.Attributes["vignetteDesaturate"].Value);
							_lensManager.vignette.blur = XmlConvert.ToSingle(moduleNode.Attributes["vignetteBlur"].Value);
							ClampFieldValues(_lensManager.vignette);

							_lensManager.vignette.color = new Color(
									XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorR"].Value),
									XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorG"].Value),
									XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorB"].Value));
						}
						break;
					case "colorGrading":
						if (_toneMappingManager != null)
						{
							TonemappingColorGrading.ColorGradingSettings colorGrading = _toneMappingManager.colorGrading;
							TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;
							colorGrading.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							settings.temperatureShift = XmlConvert.ToSingle(moduleNode.Attributes["temperatureShift"].Value);
							settings.tint = XmlConvert.ToSingle(moduleNode.Attributes["tint"].Value);
							settings.contrast = XmlConvert.ToSingle(moduleNode.Attributes["contrast"].Value);
							settings.hue = XmlConvert.ToSingle(moduleNode.Attributes["hue"].Value);
							settings.saturation = XmlConvert.ToSingle(moduleNode.Attributes["saturation"].Value);
							settings.value = XmlConvert.ToSingle(moduleNode.Attributes["value"].Value);
							settings.vibrance = XmlConvert.ToSingle(moduleNode.Attributes["vibrance"].Value);
							settings.gain = XmlConvert.ToSingle(moduleNode.Attributes["gain"].Value);
							settings.gamma = XmlConvert.ToSingle(moduleNode.Attributes["gamma"].Value);

							ClampFieldValues(settings);

							colorGrading.basics = settings;
							_toneMappingManager.colorGrading = colorGrading;
						}
						break;
					case "tonemapping":
						if (_toneMappingManager != null)
						{
							_tonemappingEnabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_toneMapper = (TonemappingColorGrading.Tonemapper)XmlConvert.ToInt32(moduleNode.Attributes["tonemapper"].Value);
							_ev = XmlConvert.ToSingle(moduleNode.Attributes["exposure"].Value);

							_toneMappingManager.tonemapping = new TonemappingColorGrading.TonemappingSettings
							{
								tonemapper = _toneMapper,
								exposure = Mathf.Pow(2f, _ev),
								enabled = _tonemappingEnabled,
								neutralBlackIn = _toneMappingManager.tonemapping.neutralBlackIn,
								neutralBlackOut = _toneMappingManager.tonemapping.neutralBlackOut,
								neutralWhiteClip = _toneMappingManager.tonemapping.neutralWhiteClip,
								neutralWhiteIn = _toneMappingManager.tonemapping.neutralWhiteIn,
								neutralWhiteLevel = _toneMappingManager.tonemapping.neutralWhiteLevel,
								neutralWhiteOut = _toneMappingManager.tonemapping.neutralWhiteOut,
								curve = _toneMappingManager.tonemapping.curve
							};

							ClampFieldValues(_toneMappingManager.tonemapping);
						}
						break;
					case "eyeAdaptation":
						if (_toneMappingManager != null)
						{
							_toneMappingManager.eyeAdaptation = new TonemappingColorGrading.EyeAdaptationSettings
							{
								enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value),
								showDebug = false,
								middleGrey = XmlConvert.ToSingle(moduleNode.Attributes["middleGrey"].Value),
								max = XmlConvert.ToSingle(moduleNode.Attributes["max"].Value),
								min = XmlConvert.ToSingle(moduleNode.Attributes["min"].Value),
								speed = XmlConvert.ToSingle(moduleNode.Attributes["speed"].Value)
							};
							ClampFieldValues(_toneMappingManager.eyeAdaptation);

							_eyeEnabled = _toneMappingManager.eyeAdaptation.enabled;
							_eyeMiddleGrey = _toneMappingManager.eyeAdaptation.middleGrey;
							_eyeMax = _toneMappingManager.eyeAdaptation.max;
							_eyeMin = _toneMappingManager.eyeAdaptation.min;
							_eyeSpeed = _toneMappingManager.eyeAdaptation.speed;
						}
						break;

					case "bloom":
						if (_cinematicBloom != null)
						{
							EffectData effectData = self.effectsDictionary[_cinematicBloom];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							_cinematicBloom.settings.intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
							_cinematicBloom.settings.threshold = XmlConvert.ToSingle(moduleNode.Attributes["threshold"].Value);
							_cinematicBloom.settings.softKnee = XmlConvert.ToSingle(moduleNode.Attributes["softKnee"].Value);
							_cinematicBloom.settings.radius = XmlConvert.ToSingle(moduleNode.Attributes["radius"].Value);
							_cinematicBloom.settings.antiFlicker = XmlConvert.ToBoolean(moduleNode.Attributes["antiFlicker"].Value);

							ClampFieldValues(_cinematicBloom.settings);
						}
						break;

					case "ssao":
						if (_ssao != null)
						{
							EffectData effectData = self.effectsDictionary[_ssao];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							_ssao.Samples = (SSAOPro.SampleCount)XmlConvert.ToInt32(moduleNode.Attributes["samples"].Value);
							_ssao.Downsampling = XmlConvert.ToInt32(moduleNode.Attributes["downsampling"].Value);
							_ssao.Radius = XmlConvert.ToSingle(moduleNode.Attributes["radius"].Value);
							_ssao.Intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
							_ssao.Distance = XmlConvert.ToSingle(moduleNode.Attributes["distance"].Value);
							_ssao.Bias = XmlConvert.ToSingle(moduleNode.Attributes["bias"].Value);
							_ssao.LumContribution = XmlConvert.ToSingle(moduleNode.Attributes["lumContribution"].Value);
							_ssao.OcclusionColor = new Color(
								XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorR"].Value),
								XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorG"].Value),
								XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorB"].Value));
							_ssao.CutoffDistance = XmlConvert.ToSingle(moduleNode.Attributes["cutoffDistance"].Value);
							_ssao.CutoffFalloff = XmlConvert.ToSingle(moduleNode.Attributes["cutoffFalloff"].Value);
							_ssao.BlurPasses = XmlConvert.ToInt32(moduleNode.Attributes["blurPasses"].Value);
							_ssao.BlurBilateralThreshold = XmlConvert.ToSingle(moduleNode.Attributes["blurBilateralThreshold"].Value);
							_ssao.UseHighPrecisionDepthMap = XmlConvert.ToBoolean(moduleNode.Attributes["useHighPrecisionDepthMap"].Value);
							_ssao.Blur = (SSAOPro.BlurMode)XmlConvert.ToInt32(moduleNode.Attributes["blur"].Value);
							_ssao.BlurDownsampling = XmlConvert.ToBoolean(moduleNode.Attributes["blurDownsampling"].Value);
							_ssao.DebugAO = XmlConvert.ToBoolean(moduleNode.Attributes["debugAO"].Value);

							ClampFieldValues(_ssao);

							if (isStudioInstance)
							{
								Studio.Studio.Instance.sceneInfo.enableSSAO = effectData.enabled;
								Studio.Studio.Instance.sceneInfo.ssaoIntensity = _ssao.Intensity;
								Studio.Studio.Instance.sceneInfo.ssaoColor.SetDiffuseRGBA(_ssao.OcclusionColor);
							}
						}
						break;
					case "sunShafts":
						if (_sunShafts != null)
						{
							EffectData effectData = self.effectsDictionary[_sunShafts];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							_sunShafts.useDepthTexture = XmlConvert.ToBoolean(moduleNode.Attributes["useDepthTexture"].Value);
							_sunShafts.resolution = (SunShafts.SunShaftsResolution)XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
							_sunShafts.screenBlendMode = (SunShafts.ShaftsScreenBlendMode)XmlConvert.ToInt32(moduleNode.Attributes["screenBlendMode"].Value);
							_sunShafts.sunThreshold = new Color(
								XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdR"].Value),
								XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdG"].Value),
								XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdB"].Value));
							_sunShafts.sunColor = new Color(
								XmlConvert.ToSingle(moduleNode.Attributes["sunColorR"].Value),
								XmlConvert.ToSingle(moduleNode.Attributes["sunColorG"].Value),
								XmlConvert.ToSingle(moduleNode.Attributes["sunColorB"].Value));
							_sunShafts.maxRadius = XmlConvert.ToSingle(moduleNode.Attributes["maxRadius"].Value);
							_sunShafts.sunShaftBlurRadius = XmlConvert.ToSingle(moduleNode.Attributes["sunShaftBlurRadius"].Value);
							_sunShafts.radialBlurIterations = XmlConvert.ToInt32(moduleNode.Attributes["radialBlurIterations"].Value);
							_sunShafts.sunShaftIntensity = XmlConvert.ToSingle(moduleNode.Attributes["sunShaftIntensity"].Value);
							if (isStudioInstance)
								Studio.Studio.Instance.sceneInfo.enableSunShafts = effectData.enabled;
						}

						break;
					case "depthOfField":
						if (_depthOfField != null)
						{
							EffectData effectData = self.effectsDictionary[_depthOfField];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							_depthOfField.visualizeFocus = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeFocus"].Value);

							//this.StartCoroutine(SetFocusType()); //Yeah, sorry about that
							//IEnumerator SetFocusType()
							//{
							//    yield return null;
							//    yield return null;
							//    yield return null;
							if (moduleNode.Attributes["useCameraOriginAsFocus"] != null)
								_focusType = XmlConvert.ToBoolean(moduleNode.Attributes["useCameraOriginAsFocus"].Value) ? DOFFocusType.Crosshair : DOFFocusType.Manual;
							else if (moduleNode.Attributes["focusType"] != null)
								_focusType = (DOFFocusType)XmlConvert.ToInt32(moduleNode.Attributes["focusType"].Value);
							else
								_focusType = DOFFocusType.Manual;

							switch (_focusType)
							{
								case DOFFocusType.Crosshair:
									_depthOfField.focalTransform = _depthOfFieldOriginalFocusPoint;
									break;
								case DOFFocusType.Object:
									if (_isStudio && moduleNode.Attributes["focussedObjectIndex"] != null)
									{
										List<KeyValuePair<int, ObjectCtrlInfo>> list = Studio.Studio.Instance.dicObjectCtrl.OrderBy(e => e.Key).ToList();
										int focussedObjectIndex = XmlConvert.ToInt32(moduleNode.Attributes["focussedObjectIndex"].Value);
										if (focussedObjectIndex < list.Count)
										{
											_focussedObject = list[focussedObjectIndex].Value;
											_depthOfField.focalTransform = _focussedObject.guideObject.transformTarget;
										}
										else
										{
											_focusType = DOFFocusType.Crosshair;
											goto case DOFFocusType.Crosshair;
										}
									}
									else
									{
										_focusType = DOFFocusType.Crosshair;
										goto case DOFFocusType.Crosshair;
									}
									break;
								case DOFFocusType.Manual:
									_depthOfField.focalTransform = null;
									break;
							}
							//}

							_depthOfField.focalLength = XmlConvert.ToSingle(moduleNode.Attributes["focalLength"].Value);
							_depthOfField.focalSize = XmlConvert.ToSingle(moduleNode.Attributes["focalSize"].Value);
							_depthOfField.aperture = XmlConvert.ToSingle(moduleNode.Attributes["aperture"].Value);
							_depthOfField.maxBlurSize = XmlConvert.ToSingle(moduleNode.Attributes["maxBlurSize"].Value);
							_depthOfField.highResolution = XmlConvert.ToBoolean(moduleNode.Attributes["highResolution"].Value);
							_depthOfField.nearBlur = XmlConvert.ToBoolean(moduleNode.Attributes["nearBlur"].Value);
							_depthOfField.foregroundOverlap = XmlConvert.ToSingle(moduleNode.Attributes["foregroundOverlap"].Value);
							_depthOfField.blurType = (DepthOfField.BlurType)XmlConvert.ToInt32(moduleNode.Attributes["blurType"].Value);
							_depthOfField.blurSampleCount = (DepthOfField.BlurSampleCount)XmlConvert.ToInt32(moduleNode.Attributes["blurSampleCount"].Value);
							_depthOfField.dx11BokehScale = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehScale"].Value);
							_depthOfField.dx11BokehIntensity = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehIntensity"].Value);
							_depthOfField.dx11BokehThreshold = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehThreshold"].Value);
							_depthOfField.dx11SpawnHeuristic = XmlConvert.ToSingle(moduleNode.Attributes["dx11SpawnHeuristic"].Value);
							self.fixDofForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
							if (isStudioInstance)
							{
								Studio.Studio.Instance.sceneInfo.enableDepth = effectData.enabled;
								Studio.Studio.Instance.sceneInfo.depthFocalSize = _depthOfField.focalSize;
								Studio.Studio.Instance.sceneInfo.depthAperture = _depthOfField.aperture;
							}
						}
						break;
					case "ssr":
						if (_ssr != null)
						{
							EffectData effectData = self.effectsDictionary[_ssr];

							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							self.fixSsrForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
							_ssr.settings = IBLData.Ssr.GetSettings(moduleNode, _ssr);

							ClampFieldValues(_ssr.settings.reflectionSettings);
							ClampFieldValues(_ssr.settings.intensitySettings);
							ClampFieldValues(_ssr.settings.screenEdgeMask);

							if (isStudioInstance)
							{
								Studio.Studio.Instance.sceneInfo.enableSSR = effectData.enabled;
							}

						}

						break;
					case "bloomAndFlares":
						if (_bloomAndFlares != null)
						{
							EffectData effectData = self.effectsDictionary[_bloomAndFlares];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							_bloomAndFlares.screenBlendMode = (BloomScreenBlendMode)XmlConvert.ToInt32(moduleNode.Attributes["screenBlendMode"].Value);
							_bloomAndFlares.hdr = (HDRBloomMode)XmlConvert.ToSingle(moduleNode.Attributes["hdr"].Value);

							_bloomAndFlares.bloomIntensity = XmlConvert.ToSingle(moduleNode.Attributes["bloomIntensity"].Value);
							_bloomAndFlares.bloomThreshold = XmlConvert.ToSingle(moduleNode.Attributes["bloomThreshold"].Value);
							_bloomAndFlares.bloomBlurIterations = XmlConvert.ToInt32(moduleNode.Attributes["bloomBlurIterations"].Value);
							_bloomAndFlares.sepBlurSpread = XmlConvert.ToSingle(moduleNode.Attributes["sepBlurSpread"].Value);
							_bloomAndFlares.useSrcAlphaAsMask = XmlConvert.ToSingle(moduleNode.Attributes["useSrcAlphaAsMask"].Value);

							_bloomAndFlares.lensflares = XmlConvert.ToBoolean(moduleNode.Attributes["lensflares"].Value);
							_bloomAndFlares.lensflareMode = (LensflareStyle34)XmlConvert.ToSingle(moduleNode.Attributes["lensflareMode"].Value);
							_bloomAndFlares.lensflareIntensity = XmlConvert.ToSingle(moduleNode.Attributes["lensflareIntensity"].Value);
							_bloomAndFlares.lensflareThreshold = XmlConvert.ToSingle(moduleNode.Attributes["lensflareThreshold"].Value);

							_bloomAndFlares.hollyStretchWidth = XmlConvert.ToSingle(moduleNode.Attributes["hollyStretchWidth"].Value);
							_bloomAndFlares.hollywoodFlareBlurIterations = XmlConvert.ToInt32(moduleNode.Attributes["hollywoodFlareBlurIterations"].Value);

							_bloomAndFlares.flareColorA = new Color(
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorAR"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorAG"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorAB"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorAA"].Value)
																		);

							_bloomAndFlares.flareColorB = new Color(
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorBR"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorBG"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorBB"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorBA"].Value)
																		);

							_bloomAndFlares.flareColorC = new Color(
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorCR"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorCG"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorCB"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorCA"].Value)
																		);

							_bloomAndFlares.flareColorD = new Color(
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorDR"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorDG"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorDB"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorDA"].Value)
																		);
							if (isStudioInstance)
							{
								Studio.Studio.Instance.sceneInfo.enableBloom = effectData.enabled;
								Studio.Studio.Instance.sceneInfo.bloomIntensity = _bloomAndFlares.bloomIntensity;
								Studio.Studio.Instance.sceneInfo.bloomBlur = _bloomAndFlares.sepBlurSpread;
							}
						}
						break;
					case "colorCorrectionCurves":
						if (_colorCorrectionCurves != null)
						{
							EffectData effectData = self.effectsDictionary[_colorCorrectionCurves];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_colorCorrectionCurves.saturation = XmlConvert.ToSingle(moduleNode.Attributes["saturation"].Value);
						}
						break;
					case "vignetteAndChromaticAberration":
						if (_vignette != null)
						{
							EffectData effectData = self.effectsDictionary[_vignette];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_vignette.intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
							_vignette.blur = XmlConvert.ToSingle(moduleNode.Attributes["blur"].Value);
							_vignette.blurSpread = XmlConvert.ToSingle(moduleNode.Attributes["blurSpread"].Value);
							_vignette.mode = (VignetteAndChromaticAberration.AberrationMode)XmlConvert.ToInt32(moduleNode.Attributes["mode"].Value);
							_vignette.chromaticAberration = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberration"].Value);
							_vignette.axialAberration = XmlConvert.ToSingle(moduleNode.Attributes["axialAberration"].Value);
							_vignette.luminanceDependency = XmlConvert.ToSingle(moduleNode.Attributes["luminanceDependency"].Value);
							_vignette.blurDistance = XmlConvert.ToSingle(moduleNode.Attributes["blurDistance"].Value);

							if (isStudioInstance)
							{
								Studio.Studio.Instance.sceneInfo.enableVignette = effectData.enabled;
								Studio.Studio.Instance.sceneInfo.vignetteVignetting = _vignette.intensity;
							}
						}
						break;
					case "antialiasing":
						if (_antialiasing != null)
						{
							EffectData effectData = self.effectsDictionary[_antialiasing];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_antialiasing.mode = (AAMode)XmlConvert.ToInt32(moduleNode.Attributes["mode"].Value);
							_antialiasing.edgeThresholdMin = XmlConvert.ToSingle(moduleNode.Attributes["edgeThresholdMin"].Value);
							_antialiasing.edgeThreshold = XmlConvert.ToSingle(moduleNode.Attributes["edgeThreshold"].Value);
							_antialiasing.edgeSharpness = XmlConvert.ToSingle(moduleNode.Attributes["edgeSharpness"].Value);
							_antialiasing.offsetScale = XmlConvert.ToSingle(moduleNode.Attributes["offsetScale"].Value);
							_antialiasing.blurRadius = XmlConvert.ToSingle(moduleNode.Attributes["blurRadius"].Value);
							_antialiasing.showGeneratedNormals = XmlConvert.ToBoolean(moduleNode.Attributes["showGeneratedNormals"].Value);
							_antialiasing.dlaaSharp = XmlConvert.ToBoolean(moduleNode.Attributes["dlaaSharp"].Value);

						}
						break;
					case "noiseAndGrain":
						if (_noiseAndGrain != null)
						{
							EffectData effectData = self.effectsDictionary[_noiseAndGrain];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_noiseAndGrain.dx11Grain = XmlConvert.ToBoolean(moduleNode.Attributes["dx11Grain"].Value);
							_noiseAndGrain.monochrome = XmlConvert.ToBoolean(moduleNode.Attributes["monochrome"].Value);
							_noiseAndGrain.intensityMultiplier = XmlConvert.ToSingle(moduleNode.Attributes["intensityMultiplier"].Value);
							_noiseAndGrain.generalIntensity = XmlConvert.ToSingle(moduleNode.Attributes["generalIntensity"].Value);
							_noiseAndGrain.blackIntensity = XmlConvert.ToSingle(moduleNode.Attributes["blackIntensity"].Value);
							_noiseAndGrain.whiteIntensity = XmlConvert.ToSingle(moduleNode.Attributes["whiteIntensity"].Value);
							_noiseAndGrain.midGrey = XmlConvert.ToSingle(moduleNode.Attributes["midGrey"].Value);
							_noiseAndGrain.intensities = new Vector3(
																		  XmlConvert.ToSingle(moduleNode.Attributes["intensitiesX"].Value),
																		  XmlConvert.ToSingle(moduleNode.Attributes["intensitiesY"].Value),
																		  XmlConvert.ToSingle(moduleNode.Attributes["intensitiesZ"].Value)
																		 );
							_noiseAndGrain.filterMode = (FilterMode)XmlConvert.ToInt32(moduleNode.Attributes["filterMode"].Value);
							_noiseAndGrain.softness = Mathf.Clamp(XmlConvert.ToSingle(moduleNode.Attributes["softness"].Value), 0, 0.99f);
							_noiseAndGrain.monochromeTiling = XmlConvert.ToSingle(moduleNode.Attributes["monochromeTiling"].Value);
							_noiseAndGrain.tiling = new Vector3(
																	 XmlConvert.ToSingle(moduleNode.Attributes["tilingX"].Value),
																	 XmlConvert.ToSingle(moduleNode.Attributes["tilingY"].Value),
																	 XmlConvert.ToSingle(moduleNode.Attributes["tilingZ"].Value)
																	);
							self.fixNoiseAndGrainForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
						}
						break;
					case "motionBlur":
						if (_motionBlur != null)
						{
							EffectData effectData = self.effectsDictionary[_motionBlur];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_motionBlur.velocityScale = XmlConvert.ToSingle(moduleNode.Attributes["velocityScale"].Value);
							_motionBlur.maxVelocity = XmlConvert.ToSingle(moduleNode.Attributes["maxVelocity"].Value);
							_motionBlur.minVelocity = XmlConvert.ToSingle(moduleNode.Attributes["minVelocity"].Value);
							_motionBlur.filterType = (CameraMotionBlur.MotionBlurFilter)XmlConvert.ToInt32(moduleNode.Attributes["filterType"].Value);
							_motionBlur.rotationScale = XmlConvert.ToSingle(moduleNode.Attributes["rotationScale"].Value);
							_motionBlur.movementScale = XmlConvert.ToSingle(moduleNode.Attributes["movementScale"].Value);
							//_motionBlur.velocityDownsample = Mathf.Max(1, XmlConvert.ToInt32(moduleNode.Attributes["velocityDownsample"].Value));
							_motionBlur.velocityDownsample = XmlConvert.ToInt32(moduleNode.Attributes["velocityDownsample"].Value);
							_motionBlur.jitter = XmlConvert.ToSingle(moduleNode.Attributes["jitter"].Value);
							_motionBlur.preview = XmlConvert.ToBoolean(moduleNode.Attributes["preview"].Value);
							_motionBlur.previewScale = new Vector3(
																		XmlConvert.ToSingle(moduleNode.Attributes["previewScaleX"].Value),
																		XmlConvert.ToSingle(moduleNode.Attributes["previewScaleY"].Value),
																		XmlConvert.ToSingle(moduleNode.Attributes["previewScaleZ"].Value)
																	   );
						}
						break;
					case "afterImage":
						if (_afterImage != null)
						{
							EffectData effectData = self.effectsDictionary[_afterImage];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_afterImage.framesToMix = XmlConvert.ToInt32(moduleNode.Attributes["framesToMix"].Value);
							_afterImage.falloffPower = moduleNode.Attributes["falloffPower"] != null ? XmlConvert.ToSingle(moduleNode.Attributes["falloffPower"].Value) : 10f / 7f;
						}
						break;
					case "volumetricLights":
						if (_volumetricLights != null)
						{
							EffectData effectData = self.effectsDictionary[_volumetricLights];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							_volumetricLights.Resolution = (VolumetricLightRenderer.VolumetricResolution)XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
							if (dic != null)
							{
								foreach (XmlNode childNode in moduleNode.ChildNodes)
								{
									int i = XmlConvert.ToInt32(childNode.Attributes["index"].Value);
									if (i >= dic.Count)
										continue;
									ObjectCtrlInfo oci = dic[i].Value;
									if (oci == null || !(oci is OCILight light))
										continue;
									VolumetricLight volumetricLight = light.light.GetComponent<VolumetricLight>();
									if (volumetricLight == null)
										continue;
									volumetricLight.enabled = childNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
									volumetricLight.SampleCount = XmlConvert.ToInt32(childNode.Attributes["sampleCount"].Value);
									volumetricLight.ScatteringCoef = XmlConvert.ToSingle(childNode.Attributes["scatteringCoef"].Value);
									volumetricLight.ExtinctionCoef = XmlConvert.ToSingle(childNode.Attributes["extinctionCoef"].Value);
									volumetricLight.MieG = XmlConvert.ToSingle(childNode.Attributes["mieG"].Value);
									volumetricLight.SkyboxExtinctionCoef = XmlConvert.ToSingle(childNode.Attributes["skyboxExtinctionCoef"].Value);
									volumetricLight.MaxRayLength = XmlConvert.ToSingle(childNode.Attributes["maxRayLength"].Value);
									volumetricLight.HeightFog = XmlConvert.ToBoolean(childNode.Attributes["heightFog"].Value);
									volumetricLight.HeightScale = XmlConvert.ToSingle(childNode.Attributes["heightScale"].Value);
									volumetricLight.GroundLevel = XmlConvert.ToSingle(childNode.Attributes["groundLevel"].Value);
									volumetricLight.Noise = XmlConvert.ToBoolean(childNode.Attributes["noise"].Value);
									volumetricLight.NoiseScale = XmlConvert.ToSingle(childNode.Attributes["noiseScale"].Value);
									volumetricLight.NoiseIntensity = XmlConvert.ToSingle(childNode.Attributes["noiseIntensity"].Value);
									volumetricLight.NoiseIntensityOffset = XmlConvert.ToSingle(childNode.Attributes["noiseIntensityOffset"].Value);
									volumetricLight.NoiseVelocity = new Vector2(
											XmlConvert.ToSingle(childNode.Attributes["noiseVelocityX"].Value),
											XmlConvert.ToSingle(childNode.Attributes["noiseVelocityY"].Value)
									);
									ClampFieldValues(volumetricLight);
								}
							}
						}
						break;
					case "segi":
						if (_segi != null)
						{
							EffectData effectData = self.effectsDictionary[_segi];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_segi.voxelResolution = (SEGI.VoxelResolution)XmlConvert.ToInt32(moduleNode.Attributes["voxelResolution"].Value);
							_segi.voxelAA = XmlConvert.ToBoolean(moduleNode.Attributes["voxelAA"].Value);
							_segi.innerOcclusionLayers = XmlConvert.ToInt32(moduleNode.Attributes["innerOcclusionLayers"].Value);
							_segi.gaussianMipFilter = XmlConvert.ToBoolean(moduleNode.Attributes["gaussianMipFilter"].Value);
							_segi.voxelSpaceSize = XmlConvert.ToSingle(moduleNode.Attributes["voxelSpaceSize"].Value);
							_segi.shadowSpaceSize = XmlConvert.ToSingle(moduleNode.Attributes["shadowSpaceSize"].Value);
							_segi.updateGI = XmlConvert.ToBoolean(moduleNode.Attributes["updateGI"].Value);
							_segi.infiniteBounces = XmlConvert.ToBoolean(moduleNode.Attributes["infiniteBounces"].Value);
							_segi.softSunlight = XmlConvert.ToSingle(moduleNode.Attributes["softSunlight"].Value);
							_segi.hsStandardCustomShadersCompatibility = moduleNode.Attributes["hsStandardCustomShadersCompatibility"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["hsStandardCustomShadersCompatibility"].Value);

							_segi.skyColor.r = XmlConvert.ToSingle(moduleNode.Attributes["skyColorR"].Value);
							_segi.skyColor.g = XmlConvert.ToSingle(moduleNode.Attributes["skyColorG"].Value);
							_segi.skyColor.b = XmlConvert.ToSingle(moduleNode.Attributes["skyColorB"].Value);
							_segi.skyColor.a = XmlConvert.ToSingle(moduleNode.Attributes["skyColorA"].Value);

							_segi.skyIntensity = XmlConvert.ToSingle(moduleNode.Attributes["skyIntensity"].Value);
							_segi.sphericalSkylight = XmlConvert.ToBoolean(moduleNode.Attributes["sphericalSkylight"].Value);

							_segi.temporalBlendWeight = XmlConvert.ToSingle(moduleNode.Attributes["temporalBlendWeight"].Value);
							_segi.useBilateralFiltering = XmlConvert.ToBoolean(moduleNode.Attributes["useBilateralFiltering"].Value);
							_segi.halfResolution = XmlConvert.ToBoolean(moduleNode.Attributes["halfResolution"].Value);
							_segi.stochasticSampling = XmlConvert.ToBoolean(moduleNode.Attributes["stochasticSampling"].Value);

							_segi.cones = XmlConvert.ToInt32(moduleNode.Attributes["cones"].Value);
							_segi.coneTraceSteps = XmlConvert.ToInt32(moduleNode.Attributes["coneTraceSteps"].Value);
							_segi.coneLength = XmlConvert.ToSingle(moduleNode.Attributes["coneLength"].Value);
							_segi.coneWidth = XmlConvert.ToSingle(moduleNode.Attributes["coneWidth"].Value);
							_segi.coneTraceBias = XmlConvert.ToSingle(moduleNode.Attributes["coneTraceBias"].Value);
							_segi.occlusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["occlusionStrength"].Value);
							_segi.nearOcclusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["nearOcclusionStrength"].Value);
							_segi.farOcclusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["farOcclusionStrength"].Value);
							_segi.farthestOcclusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["farthestOcclusionStrength"].Value);
							_segi.occlusionPower = XmlConvert.ToSingle(moduleNode.Attributes["occlusionPower"].Value);
							_segi.nearLightGain = XmlConvert.ToSingle(moduleNode.Attributes["nearLightGain"].Value);
							_segi.giGain = XmlConvert.ToSingle(moduleNode.Attributes["giGain"].Value);

							_segi.secondaryBounceGain = XmlConvert.ToSingle(moduleNode.Attributes["secondaryBounceGain"].Value);
							_segi.secondaryCones = XmlConvert.ToInt32(moduleNode.Attributes["secondaryCones"].Value);
							_segi.secondaryOcclusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["secondaryOcclusionStrength"].Value);

							_segi.doReflections = XmlConvert.ToBoolean(moduleNode.Attributes["doReflections"].Value);
							_segi.reflectionSteps = XmlConvert.ToInt32(moduleNode.Attributes["reflectionSteps"].Value);
							_segi.reflectionOcclusionPower = XmlConvert.ToSingle(moduleNode.Attributes["reflectionOcclusionPower"].Value);
							_segi.skyReflectionIntensity = XmlConvert.ToSingle(moduleNode.Attributes["skyReflectionIntensity"].Value);

							_segi.visualizeSunDepthTexture = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeSunDepthTexture"].Value);
							_segi.visualizeGI = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeGI"].Value);
							_segi.visualizeVoxels = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeVoxels"].Value);

							ClampFieldValues(_segi);
						}
						break;
					case "amplifyBloom":
						if (_amplifyBloom != null)
						{
							EffectData effectData = self.effectsDictionary[_amplifyBloom];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_amplifyBloom.UpscaleQuality = (UpscaleQualityEnum)XmlConvert.ToInt32(moduleNode.Attributes["UpscaleQuality"].Value);
							_amplifyBloom.MainThresholdSize = (MainThresholdSizeEnum)XmlConvert.ToInt32(moduleNode.Attributes["MainThresholdSize"].Value);
							_amplifyBloom.CurrentPrecisionMode = (PrecisionModes)XmlConvert.ToInt32(moduleNode.Attributes["CurrentPrecisionMode"].Value);
							_amplifyBloom.BloomRange = XmlConvert.ToSingle(moduleNode.Attributes["BloomRange"].Value);
							_amplifyBloom.OverallIntensity = XmlConvert.ToSingle(moduleNode.Attributes["OverallIntensity"].Value);
							_amplifyBloom.OverallThreshold = XmlConvert.ToSingle(moduleNode.Attributes["OverallThreshold"].Value);

							_amplifyBloom.UpscaleBlurRadius = moduleNode.Attributes["UpscaleBlurRadius"] != null ? XmlConvert.ToSingle(moduleNode.Attributes["UpscaleBlurRadius"].Value) : 1;

							_amplifyBloom.DebugToScreen = (DebugToScreenEnum)XmlConvert.ToInt32(moduleNode.Attributes["DebugToScreen"].Value);
							_amplifyBloom.TemporalFilteringActive = XmlConvert.ToBoolean(moduleNode.Attributes["TemporalFilteringActive"].Value);
							_amplifyBloom.TemporalFilteringValue = XmlConvert.ToSingle(moduleNode.Attributes["TemporalFilteringValue"].Value);
							_amplifyBloom.SeparateFeaturesThreshold = XmlConvert.ToBoolean(moduleNode.Attributes["SeparateFeaturesThreshold"].Value);
							_amplifyBloom.FeaturesThreshold = XmlConvert.ToSingle(moduleNode.Attributes["FeaturesThreshold"].Value);
							_amplifyBloom.ApplyLensDirt = XmlConvert.ToBoolean(moduleNode.Attributes["ApplyLensDirt"].Value);
							_amplifyBloom.LensDirtStrength = XmlConvert.ToSingle(moduleNode.Attributes["LensDirtStrength"].Value);
							_amplifyBloom.LensDirtTexture = (LensDirtTextureEnum)XmlConvert.ToInt32(moduleNode.Attributes["LensDirtTexture"].Value);
							_amplifyBloom.ApplyLensStardurst = XmlConvert.ToBoolean(moduleNode.Attributes["ApplyLensStardurst"].Value);
							_amplifyBloom.LensStarburstStrength = XmlConvert.ToSingle(moduleNode.Attributes["LensStarburstStrength"].Value);
							_amplifyBloom.LensStardurstTex = (LensStarburstTextureEnum)XmlConvert.ToInt32(moduleNode.Attributes["LensStardurstTex"].Value);
							_amplifyBloom.BokehFilterInstance.ApplyBokeh = XmlConvert.ToBoolean(moduleNode.Attributes["BokehFilterInstance.ApplyBokeh"].Value);
							_amplifyBloom.BokehFilterInstance.ApplyOnBloomSource = XmlConvert.ToBoolean(moduleNode.Attributes["BokehFilterInstance.ApplyOnBloomSource"].Value);
							_amplifyBloom.BokehFilterInstance.ApertureShape = (ApertureShape)XmlConvert.ToInt32(moduleNode.Attributes["BokehFilterInstance.ApertureShape"].Value);
							_amplifyBloom.BokehFilterInstance.OffsetRotation = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.OffsetRotation"].Value);
							_amplifyBloom.BokehFilterInstance.BokehSampleRadius = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.BokehSampleRadius"].Value);
							_amplifyBloom.BokehFilterInstance.Aperture = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.Aperture"].Value);
							_amplifyBloom.BokehFilterInstance.FocalLength = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.FocalLength"].Value);
							_amplifyBloom.BokehFilterInstance.FocalDistance = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.FocalDistance"].Value);
							_amplifyBloom.BokehFilterInstance.MaxCoCDiameter = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.MaxCoCDiameter"].Value);
							_amplifyBloom.LensFlareInstance.ApplyLensFlare = XmlConvert.ToBoolean(moduleNode.Attributes["LensFlareInstance.ApplyLensFlare"].Value);
							_amplifyBloom.LensFlareInstance.OverallIntensity = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.OverallIntensity"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount = XmlConvert.ToInt32(moduleNode.Attributes["LensFlareInstance.LensFlareGaussianBlurAmount"].Value);
							List<GradientColorKey> colorKeys = new List<GradientColorKey>();
							List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();

							foreach (XmlNode key in FindChildNode(moduleNode, "LensFlareInstance.LensFlareGradient"))
							{
								switch (key.Name)
								{
									case "colorKey":
										colorKeys.Add(new GradientColorKey(
																		   new Color(
																					 XmlConvert.ToSingle(key.Attributes["r"].Value),
																					 XmlConvert.ToSingle(key.Attributes["g"].Value),
																					 XmlConvert.ToSingle(key.Attributes["b"].Value)
																					),
																		   XmlConvert.ToSingle(key.Attributes["time"].Value))
																		   );
										break;
									case "alphaKey":
										alphaKeys.Add(new GradientAlphaKey(
																		   XmlConvert.ToSingle(key.Attributes["alpha"].Value),
																		   XmlConvert.ToSingle(key.Attributes["time"].Value))
																		   );
										break;
								}
							}
							_amplifyBloom.LensFlareInstance.LensFlareGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());

							_amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareNormalizedGhostsIntensity"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareGhostAmount = XmlConvert.ToInt32(moduleNode.Attributes["LensFlareInstance.LensFlareGhostAmount"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareGhostsDispersal"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareGhostChrDistortion"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareGhostsPowerFactor"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareGhostsPowerFalloff"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareNormalizedHaloIntensity"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareHaloWidth = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareHaloWidth"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareHaloChrDistortion"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareHaloPowerFactor"].Value);
							_amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareHaloPowerFalloff"].Value);
							_amplifyBloom.LensGlareInstance.ApplyLensGlare = XmlConvert.ToBoolean(moduleNode.Attributes["LensGlareInstance.ApplyLensGlare"].Value);
							_amplifyBloom.LensGlareInstance.Intensity = XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.Intensity"].Value);
							_amplifyBloom.LensGlareInstance.OverallStreakScale = XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallStreakScale"].Value);
							_amplifyBloom.LensGlareInstance.OverallTint = new Color(XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallTintR"].Value),
																						 XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallTintG"].Value),
																						 XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallTintB"].Value),
																						 XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallTintA"].Value));

							colorKeys.Clear();
							alphaKeys.Clear();

							foreach (XmlNode key in FindChildNode(moduleNode, "LensGlareInstance.CromaticColorGradient"))
							{
								switch (key.Name)
								{
									case "colorKey":
										colorKeys.Add(new GradientColorKey(
																		   new Color(
																					 XmlConvert.ToSingle(key.Attributes["r"].Value),
																					 XmlConvert.ToSingle(key.Attributes["g"].Value),
																					 XmlConvert.ToSingle(key.Attributes["b"].Value)
																					),
																		   XmlConvert.ToSingle(key.Attributes["time"].Value))
																		   );
										break;
									case "alphaKey":
										alphaKeys.Add(new GradientAlphaKey(
																		   XmlConvert.ToSingle(key.Attributes["alpha"].Value),
																		   XmlConvert.ToSingle(key.Attributes["time"].Value))
																		   );
										break;
								}
							}
							_amplifyBloom.LensGlareInstance.CromaticColorGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());

							_amplifyBloom.LensGlareInstance.CurrentGlare = (GlareLibType)XmlConvert.ToInt32(moduleNode.Attributes["LensGlareInstance.CurrentGlare"].Value);
							_amplifyBloom.LensGlareInstance.PerPassDisplacement = XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.PerPassDisplacement"].Value);
							_amplifyBloom.LensGlareInstance.GlareMaxPassCount = XmlConvert.ToInt32(moduleNode.Attributes["LensGlareInstance.GlareMaxPassCount"].Value);
							self.fixAmplifyBloomForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
						}
						break;
					case "effectsOrder":
						foreach (XmlNode childNode in moduleNode.ChildNodes)
						{
							EffectData data = self.generalEffects.Find(e => e.simpleName.Equals(childNode.Name, StringComparison.OrdinalIgnoreCase));
							if (data != null)
								self.MoveAtIndex(data.effect, XmlConvert.ToInt32(childNode.Attributes["index"].Value));
						}
						break;
					case "blur":
						if (_blur != null)
						{
							EffectData effectData = self.effectsDictionary[_blur];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							_blur.downsample = XmlConvert.ToInt32(moduleNode.Attributes["downsample"].Value);
							_blur.blurSize = XmlConvert.ToSingle(moduleNode.Attributes["blurSize"].Value);
							_blur.blurIterations = XmlConvert.ToInt32(moduleNode.Attributes["blurIterations"].Value);
							_blur.blurType = (BlurOptimized.BlurType)XmlConvert.ToInt32(moduleNode.Attributes["blurType"].Value);
							ClampFieldValues(_blur);

							self.fixBlurForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
						}
						break;
				}
			}
			if (_selectedCubemap == 0)
			{
				_proceduralSkybox.ApplySkyboxParams();
				_tempProceduralSkyboxParams = _proceduralSkybox.skyboxparams;
				_previousAmbientIntensity = RenderSettings.ambientIntensity;
			}
			else if (_selectedCubemap > 0)
			{
				_skybox.ApplySkyboxParams();
				_tempSkyboxParams = _skybox.skyboxparams;
				_previousAmbientIntensity = RenderSettings.ambientIntensity;
			}

			_environmentUpdateFlag = true;

			if (_segi != null)
			{
				var src = _isStudio
					? (from oci in Studio.Studio.Instance.dicObjectCtrl.Values where oci.kind == 2 && (oci as OCILight).lightType == LightType.Directional select (oci as OCILight).light)
					: FindObjectsOfType<Light>().Where(l => l.type == LightType.Directional);
				_segi.sun = (src == null || !src.Any()) ? null : src.Aggregate((max, x) => x.intensity > max.intensity ? x : max);
			}

			// update studio UI
			if (isStudioInstance)
			{
				new Traverse(Studio.Studio.Instance).Field("m_SystemButtonCtrl").Method("UpdateInfo").GetValue();
			}
		}

		/// <summary>
		/// Ensure that for a given object, any public fields with a <see cref="RangeAttribute"/> will have their values clamped accordingly. Intended for use when loading PostProcessing effect data from XML.
		/// </summary>
		/// <typeparam name="T">The Type of the object whose fields should be clamped.</typeparam>
		/// <param name="obj">The effect whose fields should be clamped.</param>
		/// <remarks>Uses reflection, may be slow.</remarks>
		private static void ClampFieldValues<T>(T obj)
		{
			return; // TBD whether to enforce clamped sliders, based on feedback
			/*foreach (var field in typeof(T).GetFields().Where(f => f.FieldType.Equals(typeof(int)) || f.FieldType.Equals(typeof(float))))
			{
				var rangeAttributes = (RangeAttribute[])field.GetCustomAttributes(typeof(RangeAttribute), false);
				var minAttributes = (MinAttribute[])field.GetCustomAttributes(typeof(MinAttribute), false);
				var minAttributes2 = (UnityStandardAssets.CinematicEffects.MinAttribute[])field.GetCustomAttributes(typeof(UnityStandardAssets.CinematicEffects.MinAttribute), false);

				bool isInt = field.FieldType.Equals(typeof(int));

				foreach (var rangeAttr in rangeAttributes)
				{
					float value = isInt ? (int)field.GetValue(obj) : (float)field.GetValue(obj);
					var newValue = Mathf.Clamp(value, rangeAttr.min, rangeAttr.max);
					//UnityEngine.Debug.Log($"[Range] {field.Name}: {value:0.######} -> {newValue:0.######} ({rangeAttr.min}, {rangeAttr.max})");
					if (value != newValue)
					{
						if (isInt)
						{
							field.SetValue(obj, (int)newValue);
						}
						else
						{
							field.SetValue(obj, newValue);
						}
					}
				}

				foreach (var minAttr in minAttributes)
				{
					float value = isInt ? (int)field.GetValue(obj) : (float)field.GetValue(obj);
					var newValue = minAttr.GetValue(value);
					//UnityEngine.Debug.Log($"[Min] {field.Name}: {value:0.######} -> {newValue:0.######}");
					if (value != newValue)
					{
						if (isInt)
						{
							field.SetValue(obj, (int)newValue);
						}
						else
						{
							field.SetValue(obj, newValue);
						}
					}
				}

				foreach (var minAttr in minAttributes2)
				{
					float value = isInt ? (int)field.GetValue(obj) : (float)field.GetValue(obj);
					var newValue = Mathf.Max(value, minAttr.min);
					//UnityEngine.Debug.Log($"[Min2] {field.Name}: {value:0.######} -> {newValue:0.######} ({minAttr.min})");
					if (value != newValue)
					{
						if (isInt)
						{
							field.SetValue(obj, (int)newValue);
						}
						else
						{
							field.SetValue(obj, newValue);
						}
					}
				}
			}*/
		}

		public static XmlNode FindChildNode(XmlNode self, string name)
		{
			if (self.HasChildNodes == false)
				return null;

			foreach (XmlNode chilNode in self.ChildNodes)
				if (chilNode.Name.Equals(name))
					return chilNode;

			return null;
		}

		private void SaveConfig(XmlWriter writer)
		{
			SortedDictionary<int, ObjectCtrlInfo> dic = null;
			if (_isStudio)
				dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);

			writer.WriteAttributeString("version", Assembly.GetExecutingAssembly().GetName().Version.ToString());

			{
				writer.WriteStartElement("cubemap");
				writer.WriteAttributeString("hide", XmlConvert.ToString(_hideSkybox));
				writer.WriteAttributeString("index", XmlConvert.ToString(_selectedCubemap));
				string fileName = "";
				if (_selectedCubemap > 0)
					fileName = _cubemapFolder.lstFile[_selectedCubemap - 1].FileName;
				writer.WriteAttributeString("fileName", fileName);
				writer.WriteEndElement();
			}

			_screenOverlays?.Save(writer); // addition: screen overlays, ignored by the original HSLRE
			_animatedSky?.Save(writer); // addition: animated sky, ignored by the original HSLRE
			_skyBackdrop?.Save(writer); // addition: sky backdrop, same

			if (_colorLooks != null) // addition: Color Looks, ignored by the original HSLRE
			{
				writer.WriteStartElement("colorLooks");
				writer.WriteAttributeString("look1", _colorLooks.NameOf(_colorLooks.Look1));
				writer.WriteAttributeString("strength1", XmlConvert.ToString(_colorLooks.Strength1));
				writer.WriteAttributeString("look2", _colorLooks.NameOf(_colorLooks.Look2));
				writer.WriteAttributeString("strength2", XmlConvert.ToString(_colorLooks.Strength2));
				writer.WriteEndElement();
			}

			{
				writer.WriteStartElement("skybox");
				writer.WriteAttributeString("proceduralExposure", XmlConvert.ToString(_proceduralSkybox.skyboxparams.exposure));
				writer.WriteAttributeString("proceduralSunsize", XmlConvert.ToString(_proceduralSkybox.skyboxparams.sunsize));
				writer.WriteAttributeString("proceduralSunHeight", XmlConvert.ToString(_proceduralSkybox.skyboxparams.sunheight)); // addition
				writer.WriteAttributeString("proceduralSunAzimuth", XmlConvert.ToString(_proceduralSkybox.skyboxparams.sunazimuth)); // addition
				writer.WriteAttributeString("proceduralAtmospherethickness", XmlConvert.ToString(_proceduralSkybox.skyboxparams.atmospherethickness));
				writer.WriteAttributeString("proceduralSkytintR", XmlConvert.ToString(_proceduralSkybox.skyboxparams.skytint.r));
				writer.WriteAttributeString("proceduralSkytintG", XmlConvert.ToString(_proceduralSkybox.skyboxparams.skytint.g));
				writer.WriteAttributeString("proceduralSkytintB", XmlConvert.ToString(_proceduralSkybox.skyboxparams.skytint.b));
				writer.WriteAttributeString("proceduralGroundcolorR", XmlConvert.ToString(_proceduralSkybox.skyboxparams.groundcolor.r));
				writer.WriteAttributeString("proceduralGroundcolorG", XmlConvert.ToString(_proceduralSkybox.skyboxparams.groundcolor.g));
				writer.WriteAttributeString("proceduralGroundcolorB", XmlConvert.ToString(_proceduralSkybox.skyboxparams.groundcolor.b));

				writer.WriteAttributeString("classicRotation", XmlConvert.ToString(_skybox.skyboxparams.rotation));
				writer.WriteAttributeString("classicExposure", XmlConvert.ToString(_skybox.skyboxparams.exposure));
				writer.WriteAttributeString("classicTintR", XmlConvert.ToString(_skybox.skyboxparams.tint.r));
				writer.WriteAttributeString("classicTintG", XmlConvert.ToString(_skybox.skyboxparams.tint.g));
				writer.WriteAttributeString("classicTintB", XmlConvert.ToString(_skybox.skyboxparams.tint.b));

				writer.WriteAttributeString("ambientIntensity", XmlConvert.ToString(RenderSettings.ambientIntensity));
				writer.WriteEndElement();
			}

			{
				writer.WriteStartElement("reflection");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(_probeComponent.enabled));
				writer.WriteAttributeString("refreshOnDemand", XmlConvert.ToString(_probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting));
				writer.WriteAttributeString("timeSlicing", XmlConvert.ToString((int)_probeComponent.timeSlicingMode));
				writer.WriteAttributeString("resolution", XmlConvert.ToString(_reflectionProbeResolution));
				writer.WriteAttributeString("intensity", XmlConvert.ToString(_probeComponent.intensity));
				writer.WriteAttributeString("shadowDistance", XmlConvert.ToString(_probeComponent.shadowDistance));
				writer.WriteAttributeString("nearClipPlane", XmlConvert.ToString(_probeComponent.nearClipPlane));
				writer.WriteAttributeString("positionX", XmlConvert.ToString(probeGameObject.transform.position.x));
				writer.WriteAttributeString("positionY", XmlConvert.ToString(probeGameObject.transform.position.y));
				writer.WriteAttributeString("positionZ", XmlConvert.ToString(probeGameObject.transform.position.z));
				writer.WriteAttributeString("bounce", XmlConvert.ToString(RenderSettings.reflectionBounces));
				writer.WriteAttributeString("boxProjection", XmlConvert.ToString(_probeComponent.boxProjection));
				writer.WriteAttributeString("cullingMask", XmlConvert.ToString(_probeComponent.cullingMask));
				writer.WriteEndElement();
			}

			{
				writer.WriteStartElement("defaultLight");

				writer.WriteAttributeString("frontAnchoredToCamera", XmlConvert.ToString(_frontDirectionalLight.transform.parent != null));
				writer.WriteAttributeString("frontRotateX", XmlConvert.ToString(_frontRotate.x));
				writer.WriteAttributeString("frontRotateY", XmlConvert.ToString(_frontRotate.y));
				writer.WriteAttributeString("frontIntensity", XmlConvert.ToString(_frontDirectionalLight.intensity));
				writer.WriteAttributeString("frontColorR", XmlConvert.ToString(_frontDirectionalLight.color.r));
				writer.WriteAttributeString("frontColorG", XmlConvert.ToString(_frontDirectionalLight.color.g));
				writer.WriteAttributeString("frontColorB", XmlConvert.ToString(_frontDirectionalLight.color.b));

				writer.WriteAttributeString("backAnchoredToCamera", XmlConvert.ToString(_backDirectionalLight.transform.parent != null));
				writer.WriteAttributeString("backRotateX", XmlConvert.ToString(_backRotate.x));
				writer.WriteAttributeString("backRotateY", XmlConvert.ToString(_backRotate.y));
				writer.WriteAttributeString("backIntensity", XmlConvert.ToString(_backDirectionalLight.intensity));
				writer.WriteAttributeString("backColorR", XmlConvert.ToString(_backDirectionalLight.color.r));
				writer.WriteAttributeString("backColorG", XmlConvert.ToString(_backDirectionalLight.color.g));
				writer.WriteAttributeString("backColorB", XmlConvert.ToString(_backDirectionalLight.color.b));

				writer.WriteEndElement();
			}

			if (_smaa != null)
			{
				EffectData effectData = self.effectsDictionary[_smaa];

				SMAA.GlobalSettings settings = _smaa.settings;
				SMAA.PredicationSettings predication = _smaa.predication;
				SMAA.TemporalSettings temporal = _smaa.temporal;
				SMAA.QualitySettings quality = _smaa.quality;

				writer.WriteStartElement("smaa");

				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("debugPass", XmlConvert.ToString((int)settings.debugPass));
				writer.WriteAttributeString("quality", XmlConvert.ToString((int)settings.quality));
				writer.WriteAttributeString("edgeDetectionMethod", XmlConvert.ToString((int)settings.edgeDetectionMethod));
				writer.WriteAttributeString("qualityDiagonalDetection", XmlConvert.ToString(quality.diagonalDetection));
				writer.WriteAttributeString("qualityCornerDetection", XmlConvert.ToString(quality.cornerDetection));
				writer.WriteAttributeString("qualityThreshold", XmlConvert.ToString(quality.threshold));
				writer.WriteAttributeString("qualityDepthThreshold", XmlConvert.ToString(quality.depthThreshold));
				writer.WriteAttributeString("qualityMaxSearchSteps", XmlConvert.ToString(quality.maxSearchSteps));
				writer.WriteAttributeString("qualityMaxDiagonalSearchSteps", XmlConvert.ToString(quality.maxDiagonalSearchSteps));
				writer.WriteAttributeString("qualityCornerRounding", XmlConvert.ToString(quality.cornerRounding));
				writer.WriteAttributeString("qualityLocalContrastAdaptationFactor", XmlConvert.ToString(quality.localContrastAdaptationFactor));
				writer.WriteAttributeString("predicationEnabled", XmlConvert.ToString(predication.enabled));
				writer.WriteAttributeString("predicationThreshold", XmlConvert.ToString(predication.threshold));
				writer.WriteAttributeString("predicationScale", XmlConvert.ToString(predication.scale));
				writer.WriteAttributeString("predicationStrength", XmlConvert.ToString(predication.strength));
				writer.WriteAttributeString("temporalEnabled", XmlConvert.ToString(temporal.enabled));
				writer.WriteAttributeString("temporalFuzzSize", XmlConvert.ToString(temporal.fuzzSize));

				writer.WriteEndElement();
			}

			if (_lensManager != null)
			{
				writer.WriteStartElement("lens");

				writer.WriteAttributeString("fov", XmlConvert.ToString(cam.fieldOfView));
				writer.WriteAttributeString("distortionEnabled", XmlConvert.ToString(_lensManager.distortion.enabled));
				writer.WriteAttributeString("distortionAmount", XmlConvert.ToString(_lensManager.distortion.amount));
				writer.WriteAttributeString("distortionAmountX", XmlConvert.ToString(_lensManager.distortion.amountX));
				writer.WriteAttributeString("distortionAmountY", XmlConvert.ToString(_lensManager.distortion.amountY));
				writer.WriteAttributeString("distortionScale", XmlConvert.ToString(_lensManager.distortion.scale));

				writer.WriteAttributeString("chromaticAberrationEnabled", XmlConvert.ToString(_lensManager.chromaticAberration.enabled));
				writer.WriteAttributeString("chromaticAberrationAmount", XmlConvert.ToString(_lensManager.chromaticAberration.amount));
				writer.WriteAttributeString("chromaticAberrationColorR", XmlConvert.ToString(_lensManager.chromaticAberration.color.r));
				writer.WriteAttributeString("chromaticAberrationColorG", XmlConvert.ToString(_lensManager.chromaticAberration.color.g));
				writer.WriteAttributeString("chromaticAberrationColorB", XmlConvert.ToString(_lensManager.chromaticAberration.color.b));

				writer.WriteAttributeString("vignetteEnabled", XmlConvert.ToString(_lensManager.vignette.enabled));
				writer.WriteAttributeString("vignetteIntensity", XmlConvert.ToString(_lensManager.vignette.intensity));
				writer.WriteAttributeString("vignetteSmoothness", XmlConvert.ToString(_lensManager.vignette.smoothness));
				writer.WriteAttributeString("vignetteRoundness", XmlConvert.ToString(_lensManager.vignette.roundness));
				writer.WriteAttributeString("vignetteDesaturate", XmlConvert.ToString(_lensManager.vignette.desaturate));
				writer.WriteAttributeString("vignetteBlur", XmlConvert.ToString(_lensManager.vignette.blur));
				writer.WriteAttributeString("vignetteColorR", XmlConvert.ToString(_lensManager.vignette.color.r));
				writer.WriteAttributeString("vignetteColorG", XmlConvert.ToString(_lensManager.vignette.color.g));
				writer.WriteAttributeString("vignetteColorB", XmlConvert.ToString(_lensManager.vignette.color.b));

				writer.WriteEndElement();
			}

			if (_toneMappingManager != null)
			{
				writer.WriteStartElement("tonemapping");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(_toneMappingManager.tonemapping.enabled));
				writer.WriteAttributeString("tonemapper", XmlConvert.ToString((int)_toneMappingManager.tonemapping.tonemapper));
				writer.WriteAttributeString("exposure", XmlConvert.ToString(_ev));
				writer.WriteEndElement();
			}

			if (_toneMappingManager != null)
			{
				TonemappingColorGrading.ColorGradingSettings colorGrading = _toneMappingManager.colorGrading;
				TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;

				writer.WriteStartElement("colorGrading");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(colorGrading.enabled));
				writer.WriteAttributeString("temperatureShift", XmlConvert.ToString(settings.temperatureShift));
				writer.WriteAttributeString("tint", XmlConvert.ToString(settings.tint));
				writer.WriteAttributeString("contrast", XmlConvert.ToString(settings.contrast));
				writer.WriteAttributeString("hue", XmlConvert.ToString(settings.hue));
				writer.WriteAttributeString("saturation", XmlConvert.ToString(settings.saturation));
				writer.WriteAttributeString("value", XmlConvert.ToString(settings.value));
				writer.WriteAttributeString("vibrance", XmlConvert.ToString(settings.vibrance));
				writer.WriteAttributeString("gain", XmlConvert.ToString(settings.gain));
				writer.WriteAttributeString("gamma", XmlConvert.ToString(settings.gamma));
				writer.WriteEndElement();
			}

			if (_toneMappingManager != null)
			{
				writer.WriteStartElement("eyeAdaptation");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(_toneMappingManager.eyeAdaptation.enabled));
				writer.WriteAttributeString("middleGrey", XmlConvert.ToString(_toneMappingManager.eyeAdaptation.middleGrey));
				writer.WriteAttributeString("min", XmlConvert.ToString(_toneMappingManager.eyeAdaptation.min));
				writer.WriteAttributeString("max", XmlConvert.ToString(_toneMappingManager.eyeAdaptation.max));
				writer.WriteAttributeString("speed", XmlConvert.ToString(_toneMappingManager.eyeAdaptation.speed));
				writer.WriteEndElement();
			}

			if (_cinematicBloom != null)
			{
				EffectData effectData = self.effectsDictionary[_cinematicBloom];

				writer.WriteStartElement("bloom");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("intensity", XmlConvert.ToString(_cinematicBloom.settings.intensity));
				writer.WriteAttributeString("threshold", XmlConvert.ToString(_cinematicBloom.settings.threshold));
				writer.WriteAttributeString("softKnee", XmlConvert.ToString(_cinematicBloom.settings.softKnee));
				writer.WriteAttributeString("radius", XmlConvert.ToString(_cinematicBloom.settings.radius));
				writer.WriteAttributeString("antiFlicker", XmlConvert.ToString(_cinematicBloom.settings.antiFlicker));
				writer.WriteEndElement();
			}

			if (_ssao != null)
			{
				EffectData effectData = self.effectsDictionary[_ssao];

				writer.WriteStartElement("ssao");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("samples", XmlConvert.ToString((int)_ssao.Samples));
				writer.WriteAttributeString("downsampling", XmlConvert.ToString(_ssao.Downsampling));
				writer.WriteAttributeString("radius", XmlConvert.ToString(_ssao.Radius));
				writer.WriteAttributeString("intensity", XmlConvert.ToString(_ssao.Intensity));
				writer.WriteAttributeString("distance", XmlConvert.ToString(_ssao.Distance));
				writer.WriteAttributeString("bias", XmlConvert.ToString(_ssao.Bias));
				writer.WriteAttributeString("lumContribution", XmlConvert.ToString(_ssao.LumContribution));
				writer.WriteAttributeString("occlusionColorR", XmlConvert.ToString(_ssao.OcclusionColor.r));
				writer.WriteAttributeString("occlusionColorG", XmlConvert.ToString(_ssao.OcclusionColor.g));
				writer.WriteAttributeString("occlusionColorB", XmlConvert.ToString(_ssao.OcclusionColor.b));
				writer.WriteAttributeString("cutoffDistance", XmlConvert.ToString(_ssao.CutoffDistance));
				writer.WriteAttributeString("cutoffFalloff", XmlConvert.ToString(_ssao.CutoffFalloff));
				writer.WriteAttributeString("blurPasses", XmlConvert.ToString(_ssao.BlurPasses));
				writer.WriteAttributeString("blurBilateralThreshold", XmlConvert.ToString(_ssao.BlurBilateralThreshold));
				writer.WriteAttributeString("useHighPrecisionDepthMap", XmlConvert.ToString(_ssao.UseHighPrecisionDepthMap));
				writer.WriteAttributeString("blur", XmlConvert.ToString((int)_ssao.Blur));
				writer.WriteAttributeString("blurDownsampling", XmlConvert.ToString(_ssao.BlurDownsampling));
				writer.WriteAttributeString("debugAO", XmlConvert.ToString(_ssao.DebugAO));
				writer.WriteEndElement();
			}

			if (_sunShafts != null)
			{
				EffectData effectData = self.effectsDictionary[_sunShafts];

				writer.WriteStartElement("sunShafts");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("useDepthTexture", XmlConvert.ToString(_sunShafts.useDepthTexture));
				writer.WriteAttributeString("resolution", XmlConvert.ToString((int)_sunShafts.resolution));
				writer.WriteAttributeString("screenBlendMode", XmlConvert.ToString((int)_sunShafts.screenBlendMode));
				writer.WriteAttributeString("sunThresholdR", XmlConvert.ToString(_sunShafts.sunThreshold.r));
				writer.WriteAttributeString("sunThresholdG", XmlConvert.ToString(_sunShafts.sunThreshold.g));
				writer.WriteAttributeString("sunThresholdB", XmlConvert.ToString(_sunShafts.sunThreshold.b));
				writer.WriteAttributeString("sunColorR", XmlConvert.ToString(_sunShafts.sunColor.r));
				writer.WriteAttributeString("sunColorG", XmlConvert.ToString(_sunShafts.sunColor.g));
				writer.WriteAttributeString("sunColorB", XmlConvert.ToString(_sunShafts.sunColor.b));
				writer.WriteAttributeString("maxRadius", XmlConvert.ToString(_sunShafts.maxRadius));
				writer.WriteAttributeString("sunShaftBlurRadius", XmlConvert.ToString(_sunShafts.sunShaftBlurRadius));
				writer.WriteAttributeString("radialBlurIterations", XmlConvert.ToString(_sunShafts.radialBlurIterations));
				writer.WriteAttributeString("sunShaftIntensity", XmlConvert.ToString(_sunShafts.sunShaftIntensity));
				writer.WriteEndElement();
			}

			if (_depthOfField != null)
			{
				EffectData effectData = self.effectsDictionary[_depthOfField];

				writer.WriteStartElement("depthOfField");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("visualizeFocus", XmlConvert.ToString(_depthOfField.visualizeFocus));
				writer.WriteAttributeString("focusType", XmlConvert.ToString((int)_focusType));
				if (_focusType == DOFFocusType.Object && _focussedObject != null && _depthOfField.focalTransform != null)
				{
					List<KeyValuePair<int, ObjectCtrlInfo>> list = Studio.Studio.Instance.dicObjectCtrl.OrderBy(e => e.Key).ToList();
					int index = list.FindIndex(e => e.Value == _focussedObject);
					if (index != -1)
						writer.WriteAttributeString("focussedObjectIndex", XmlConvert.ToString(index));
				}
				writer.WriteAttributeString("focalLength", XmlConvert.ToString(_depthOfField.focalLength));
				writer.WriteAttributeString("focalSize", XmlConvert.ToString(_depthOfField.focalSize));
				writer.WriteAttributeString("aperture", XmlConvert.ToString(_depthOfField.aperture));
				writer.WriteAttributeString("maxBlurSize", XmlConvert.ToString(_depthOfField.maxBlurSize));
				writer.WriteAttributeString("highResolution", XmlConvert.ToString(_depthOfField.highResolution));
				writer.WriteAttributeString("nearBlur", XmlConvert.ToString(_depthOfField.nearBlur));
				writer.WriteAttributeString("foregroundOverlap", XmlConvert.ToString(_depthOfField.foregroundOverlap));
				writer.WriteAttributeString("blurType", XmlConvert.ToString((int)_depthOfField.blurType));
				writer.WriteAttributeString("blurSampleCount", XmlConvert.ToString((int)_depthOfField.blurSampleCount));
				writer.WriteAttributeString("dx11BokehScale", XmlConvert.ToString(_depthOfField.dx11BokehScale));
				writer.WriteAttributeString("dx11BokehIntensity", XmlConvert.ToString(_depthOfField.dx11BokehIntensity));
				writer.WriteAttributeString("dx11BokehThreshold", XmlConvert.ToString(_depthOfField.dx11BokehThreshold));
				writer.WriteAttributeString("dx11SpawnHeuristic", XmlConvert.ToString(_depthOfField.dx11SpawnHeuristic));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(self.fixDofForUpscaledScreenshots));
				writer.WriteEndElement();
			}

			if (_ssr != null)
			{
				var ssrXml = IBLData.Ssr.GetSettings_XmlNode(_ssr, self.effectsDictionary[_ssr].enabled, self.fixSsrForUpscaledScreenshots);
				writer.WriteRaw(ssrXml);
				/*goto skipOld;

				EffectData effectData = self.effectsDictionary[_ssr];

				writer.WriteStartElement("ssr");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(self.fixSsrForUpscaledScreenshots));

				// new save data
				writer.WriteAttributeString("ssrSettings", _ssr.settings.GetSaveDataAsString());
				writer.WriteAttributeString("traceBehindObjects", _ssr.traceBehindObjects.ToString());
				writer.WriteAttributeString("treatBackfaceHitAsMiss", _ssr.treatBackfaceHitAsMiss.ToString());
				writer.WriteAttributeString("highlightSuppression", _ssr.highlightSuppression.ToString());
				writer.WriteAttributeString("bilateralUpsample", _ssr.bilateralUpsample.ToString());

				/* //writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(self.fixSsrForUpscaledScreenshots));

				ScreenSpaceReflection.SSRSettings ssrSettings = _ssr.settings;

				ScreenSpaceReflection.BasicSettings basicSettings = ssrSettings.basicSettings;
				{
					writer.WriteAttributeString("reflectionMultiplier", XmlConvert.ToString(basicSettings.reflectionMultiplier));
					writer.WriteAttributeString("maxDistance", XmlConvert.ToString(basicSettings.maxDistance));
					writer.WriteAttributeString("fadeDistance", XmlConvert.ToString(basicSettings.fadeDistance));
					writer.WriteAttributeString("screenEdgeFading", XmlConvert.ToString(basicSettings.screenEdgeFading));
					writer.WriteAttributeString("enableHDR", XmlConvert.ToString(basicSettings.enableHDR));
					writer.WriteAttributeString("additiveReflection", XmlConvert.ToString(basicSettings.additiveReflection));
				}

				ScreenSpaceReflection.ReflectionSettings reflectionSettings = ssrSettings.reflectionSettings;
				{
					writer.WriteAttributeString("maxSteps", XmlConvert.ToString(reflectionSettings.maxSteps));
					writer.WriteAttributeString("rayStepSize", XmlConvert.ToString(reflectionSettings.rayStepSize));
					writer.WriteAttributeString("widthModifier", XmlConvert.ToString(reflectionSettings.widthModifier));
					writer.WriteAttributeString("smoothFallbackThreshold", XmlConvert.ToString(reflectionSettings.smoothFallbackThreshold));
					writer.WriteAttributeString("smoothFallbackDistance", XmlConvert.ToString(reflectionSettings.smoothFallbackDistance));
					writer.WriteAttributeString("fresnelFade", XmlConvert.ToString(reflectionSettings.fresnelFade));
					writer.WriteAttributeString("fresnelFadePower", XmlConvert.ToString(reflectionSettings.fresnelFadePower));
					writer.WriteAttributeString("distanceBlur", XmlConvert.ToString(reflectionSettings.distanceBlur));
				}

				ScreenSpaceReflection.AdvancedSettings advancedSettings = ssrSettings.advancedSettings;
				{
					writer.WriteAttributeString("temporalFilterStrength", XmlConvert.ToString(advancedSettings.temporalFilterStrength));
					writer.WriteAttributeString("useTemporalConfidence", XmlConvert.ToString(advancedSettings.useTemporalConfidence));
					writer.WriteAttributeString("traceBehindObjects", XmlConvert.ToString(advancedSettings.traceBehindObjects));
					writer.WriteAttributeString("highQualitySharpReflections", XmlConvert.ToString(advancedSettings.highQualitySharpReflections));
					writer.WriteAttributeString("traceEverywhere", XmlConvert.ToString(advancedSettings.traceEverywhere));
					writer.WriteAttributeString("treatBackfaceHitAsMiss", XmlConvert.ToString(advancedSettings.treatBackfaceHitAsMiss));
					writer.WriteAttributeString("allowBackwardsRays", XmlConvert.ToString(advancedSettings.allowBackwardsRays));
					writer.WriteAttributeString("improveCorners", XmlConvert.ToString(advancedSettings.improveCorners));
					writer.WriteAttributeString("resolution", XmlConvert.ToString((int)advancedSettings.resolution));
					writer.WriteAttributeString("bilateralUpsample", XmlConvert.ToString(advancedSettings.bilateralUpsample));
					writer.WriteAttributeString("reduceBanding", XmlConvert.ToString(advancedSettings.reduceBanding));
					writer.WriteAttributeString("highlightSuppression", XmlConvert.ToString(advancedSettings.highlightSuppression));
				}* / // old
				writer.WriteEndElement();
				*/
			}
		//skipOld:

			if (_bloomAndFlares != null)
			{
				writer.WriteStartElement("bloomAndFlares");

				EffectData effectData = self.effectsDictionary[_bloomAndFlares];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));

				writer.WriteAttributeString("screenBlendMode", XmlConvert.ToString((int)_bloomAndFlares.screenBlendMode));
				writer.WriteAttributeString("hdr", XmlConvert.ToString((int)_bloomAndFlares.hdr));

				writer.WriteAttributeString("bloomIntensity", XmlConvert.ToString(_bloomAndFlares.bloomIntensity));
				writer.WriteAttributeString("bloomThreshold", XmlConvert.ToString(_bloomAndFlares.bloomThreshold));
				writer.WriteAttributeString("bloomBlurIterations", XmlConvert.ToString(_bloomAndFlares.bloomBlurIterations));
				writer.WriteAttributeString("sepBlurSpread", XmlConvert.ToString(_bloomAndFlares.sepBlurSpread));
				writer.WriteAttributeString("useSrcAlphaAsMask", XmlConvert.ToString(_bloomAndFlares.useSrcAlphaAsMask));

				writer.WriteAttributeString("lensflares", XmlConvert.ToString(_bloomAndFlares.lensflares));
				writer.WriteAttributeString("lensflareMode", XmlConvert.ToString((int)_bloomAndFlares.lensflareMode));
				writer.WriteAttributeString("lensflareIntensity", XmlConvert.ToString(_bloomAndFlares.lensflareIntensity));
				writer.WriteAttributeString("lensflareThreshold", XmlConvert.ToString(_bloomAndFlares.lensflareThreshold));

				writer.WriteAttributeString("hollyStretchWidth", XmlConvert.ToString(_bloomAndFlares.hollyStretchWidth));
				writer.WriteAttributeString("hollywoodFlareBlurIterations", XmlConvert.ToString(_bloomAndFlares.hollywoodFlareBlurIterations));

				writer.WriteAttributeString("flareColorAR", XmlConvert.ToString(_bloomAndFlares.flareColorA.r));
				writer.WriteAttributeString("flareColorAG", XmlConvert.ToString(_bloomAndFlares.flareColorA.g));
				writer.WriteAttributeString("flareColorAB", XmlConvert.ToString(_bloomAndFlares.flareColorA.b));
				writer.WriteAttributeString("flareColorAA", XmlConvert.ToString(_bloomAndFlares.flareColorA.a));

				writer.WriteAttributeString("flareColorBR", XmlConvert.ToString(_bloomAndFlares.flareColorB.r));
				writer.WriteAttributeString("flareColorBG", XmlConvert.ToString(_bloomAndFlares.flareColorB.g));
				writer.WriteAttributeString("flareColorBB", XmlConvert.ToString(_bloomAndFlares.flareColorB.b));
				writer.WriteAttributeString("flareColorBA", XmlConvert.ToString(_bloomAndFlares.flareColorB.a));

				writer.WriteAttributeString("flareColorCR", XmlConvert.ToString(_bloomAndFlares.flareColorC.r));
				writer.WriteAttributeString("flareColorCG", XmlConvert.ToString(_bloomAndFlares.flareColorC.g));
				writer.WriteAttributeString("flareColorCB", XmlConvert.ToString(_bloomAndFlares.flareColorC.b));
				writer.WriteAttributeString("flareColorCA", XmlConvert.ToString(_bloomAndFlares.flareColorC.a));

				writer.WriteAttributeString("flareColorDR", XmlConvert.ToString(_bloomAndFlares.flareColorD.r));
				writer.WriteAttributeString("flareColorDG", XmlConvert.ToString(_bloomAndFlares.flareColorD.g));
				writer.WriteAttributeString("flareColorDB", XmlConvert.ToString(_bloomAndFlares.flareColorD.b));
				writer.WriteAttributeString("flareColorDA", XmlConvert.ToString(_bloomAndFlares.flareColorD.a));

				writer.WriteEndElement();
			}

			if (_colorCorrectionCurves != null)
			{
				writer.WriteStartElement("colorCorrectionCurves");

				EffectData effectData = self.effectsDictionary[_colorCorrectionCurves];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("saturation", XmlConvert.ToString(_colorCorrectionCurves.saturation));

				writer.WriteEndElement();
			}

			if (_vignette != null)
			{
				writer.WriteStartElement("vignetteAndChromaticAberration");

				EffectData effectData = self.effectsDictionary[_vignette];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("intensity", XmlConvert.ToString(_vignette.intensity));
				writer.WriteAttributeString("blur", XmlConvert.ToString(_vignette.blur));
				writer.WriteAttributeString("blurSpread", XmlConvert.ToString(_vignette.blurSpread));
				writer.WriteAttributeString("mode", XmlConvert.ToString((int)_vignette.mode));
				writer.WriteAttributeString("chromaticAberration", XmlConvert.ToString(_vignette.chromaticAberration));
				writer.WriteAttributeString("axialAberration", XmlConvert.ToString(_vignette.axialAberration));
				writer.WriteAttributeString("luminanceDependency", XmlConvert.ToString(_vignette.luminanceDependency));
				writer.WriteAttributeString("blurDistance", XmlConvert.ToString(_vignette.blurDistance));

				writer.WriteEndElement();
			}

			if (_antialiasing != null)
			{
				writer.WriteStartElement("antialiasing");

				EffectData effectData = self.effectsDictionary[_antialiasing];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("mode", XmlConvert.ToString((int)_antialiasing.mode));
				writer.WriteAttributeString("edgeThresholdMin", XmlConvert.ToString(_antialiasing.edgeThresholdMin));
				writer.WriteAttributeString("edgeThreshold", XmlConvert.ToString(_antialiasing.edgeThreshold));
				writer.WriteAttributeString("edgeSharpness", XmlConvert.ToString(_antialiasing.edgeSharpness));
				writer.WriteAttributeString("offsetScale", XmlConvert.ToString(_antialiasing.offsetScale));
				writer.WriteAttributeString("blurRadius", XmlConvert.ToString(_antialiasing.blurRadius));
				writer.WriteAttributeString("showGeneratedNormals", XmlConvert.ToString(_antialiasing.showGeneratedNormals));
				writer.WriteAttributeString("dlaaSharp", XmlConvert.ToString(_antialiasing.dlaaSharp));

				writer.WriteEndElement();
			}

			if (_noiseAndGrain != null)
			{
				writer.WriteStartElement("noiseAndGrain");
				EffectData effectData = self.effectsDictionary[_noiseAndGrain];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("dx11Grain", XmlConvert.ToString(_noiseAndGrain.dx11Grain));
				writer.WriteAttributeString("monochrome", XmlConvert.ToString(_noiseAndGrain.monochrome));
				writer.WriteAttributeString("intensityMultiplier", XmlConvert.ToString(_noiseAndGrain.intensityMultiplier));
				writer.WriteAttributeString("generalIntensity", XmlConvert.ToString(_noiseAndGrain.generalIntensity));
				writer.WriteAttributeString("blackIntensity", XmlConvert.ToString(_noiseAndGrain.blackIntensity));
				writer.WriteAttributeString("whiteIntensity", XmlConvert.ToString(_noiseAndGrain.whiteIntensity));
				writer.WriteAttributeString("midGrey", XmlConvert.ToString(_noiseAndGrain.midGrey));
				writer.WriteAttributeString("intensitiesX", XmlConvert.ToString(_noiseAndGrain.intensities.x));
				writer.WriteAttributeString("intensitiesY", XmlConvert.ToString(_noiseAndGrain.intensities.y));
				writer.WriteAttributeString("intensitiesZ", XmlConvert.ToString(_noiseAndGrain.intensities.z));
				writer.WriteAttributeString("filterMode", XmlConvert.ToString((int)_noiseAndGrain.filterMode));
				writer.WriteAttributeString("softness", XmlConvert.ToString(_noiseAndGrain.softness));
				writer.WriteAttributeString("monochromeTiling", XmlConvert.ToString(_noiseAndGrain.monochromeTiling));
				writer.WriteAttributeString("tilingX", XmlConvert.ToString(_noiseAndGrain.tiling.x));
				writer.WriteAttributeString("tilingY", XmlConvert.ToString(_noiseAndGrain.tiling.y));
				writer.WriteAttributeString("tilingZ", XmlConvert.ToString(_noiseAndGrain.tiling.z));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(self.fixNoiseAndGrainForUpscaledScreenshots));
				writer.WriteEndElement();
			}

			if (_motionBlur != null)
			{
				writer.WriteStartElement("motionBlur");
				EffectData effectData = self.effectsDictionary[_motionBlur];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("velocityScale", XmlConvert.ToString(_motionBlur.velocityScale));
				writer.WriteAttributeString("maxVelocity", XmlConvert.ToString(_motionBlur.maxVelocity));
				writer.WriteAttributeString("minVelocity", XmlConvert.ToString(_motionBlur.minVelocity));
				writer.WriteAttributeString("filterType", XmlConvert.ToString((int)_motionBlur.filterType));
				writer.WriteAttributeString("rotationScale", XmlConvert.ToString(_motionBlur.rotationScale));
				writer.WriteAttributeString("movementScale", XmlConvert.ToString(_motionBlur.movementScale));
				writer.WriteAttributeString("velocityDownsample", XmlConvert.ToString(_motionBlur.velocityDownsample));
				writer.WriteAttributeString("jitter", XmlConvert.ToString(_motionBlur.jitter));
				writer.WriteAttributeString("preview", XmlConvert.ToString(_motionBlur.preview));
				writer.WriteAttributeString("previewScaleX", XmlConvert.ToString(_motionBlur.previewScale.x));
				writer.WriteAttributeString("previewScaleY", XmlConvert.ToString(_motionBlur.previewScale.y));
				writer.WriteAttributeString("previewScaleZ", XmlConvert.ToString(_motionBlur.previewScale.z));
				writer.WriteEndElement();
			}

			if (_afterImage != null)
			{
				writer.WriteStartElement("afterImage");
				EffectData effectData = self.effectsDictionary[_afterImage];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("framesToMix", XmlConvert.ToString(_afterImage.framesToMix));
				writer.WriteAttributeString("falloffPower", XmlConvert.ToString(_afterImage.falloffPower));
				writer.WriteEndElement();
			}

			if (_volumetricLights != null)
			{
				writer.WriteStartElement("volumetricLights");
				EffectData effectData = self.effectsDictionary[_volumetricLights];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));

				writer.WriteAttributeString("resolution", XmlConvert.ToString((int)_volumetricLights.Resolution));
				if (dic != null)
				{
					int i = 0;
					foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
					{
						if (kvp.Value is OCILight light)
						{
							VolumetricLight volumetricLight = light.light.GetComponent<VolumetricLight>();
							writer.WriteStartElement("light");
							writer.WriteAttributeString("index", XmlConvert.ToString(i));

							writer.WriteAttributeString("enabled", XmlConvert.ToString(volumetricLight.enabled));
							writer.WriteAttributeString("sampleCount", XmlConvert.ToString(volumetricLight.SampleCount));
							writer.WriteAttributeString("scatteringCoef", XmlConvert.ToString(volumetricLight.ScatteringCoef));
							writer.WriteAttributeString("extinctionCoef", XmlConvert.ToString(volumetricLight.ExtinctionCoef));
							writer.WriteAttributeString("mieG", XmlConvert.ToString(volumetricLight.MieG));
							writer.WriteAttributeString("skyboxExtinctionCoef", XmlConvert.ToString(volumetricLight.SkyboxExtinctionCoef));
							writer.WriteAttributeString("maxRayLength", XmlConvert.ToString(volumetricLight.MaxRayLength));
							writer.WriteAttributeString("heightFog", XmlConvert.ToString(volumetricLight.HeightFog));
							writer.WriteAttributeString("heightScale", XmlConvert.ToString(volumetricLight.HeightScale));
							writer.WriteAttributeString("groundLevel", XmlConvert.ToString(volumetricLight.GroundLevel));
							writer.WriteAttributeString("noise", XmlConvert.ToString(volumetricLight.Noise));
							writer.WriteAttributeString("noiseScale", XmlConvert.ToString(volumetricLight.NoiseScale));
							writer.WriteAttributeString("noiseIntensity", XmlConvert.ToString(volumetricLight.NoiseIntensity));
							writer.WriteAttributeString("noiseIntensityOffset", XmlConvert.ToString(volumetricLight.NoiseIntensityOffset));
							writer.WriteAttributeString("noiseVelocityX", XmlConvert.ToString(volumetricLight.NoiseVelocity.x));
							writer.WriteAttributeString("noiseVelocityY", XmlConvert.ToString(volumetricLight.NoiseVelocity.y));

							writer.WriteEndElement();
						}
						++i;
					}
				}
				writer.WriteEndElement();
			}

			if (_segi != null)
			{
				writer.WriteStartElement("segi");
				EffectData effectData = self.effectsDictionary[_segi];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("voxelResolution", XmlConvert.ToString((int)_segi.voxelResolution));
				writer.WriteAttributeString("voxelAA", XmlConvert.ToString(_segi.voxelAA));
				writer.WriteAttributeString("innerOcclusionLayers", XmlConvert.ToString(_segi.innerOcclusionLayers));
				writer.WriteAttributeString("gaussianMipFilter", XmlConvert.ToString(_segi.gaussianMipFilter));
				writer.WriteAttributeString("voxelSpaceSize", XmlConvert.ToString(_segi.voxelSpaceSize));
				writer.WriteAttributeString("shadowSpaceSize", XmlConvert.ToString(_segi.shadowSpaceSize));
				writer.WriteAttributeString("updateGI", XmlConvert.ToString(_segi.updateGI));
				writer.WriteAttributeString("infiniteBounces", XmlConvert.ToString(_segi.infiniteBounces));
				writer.WriteAttributeString("softSunlight", XmlConvert.ToString(_segi.softSunlight));
				writer.WriteAttributeString("hsStandardCustomShadersCompatibility", XmlConvert.ToString(_segi.hsStandardCustomShadersCompatibility));

				writer.WriteAttributeString("skyColorR", XmlConvert.ToString(_segi.skyColor.r));
				writer.WriteAttributeString("skyColorG", XmlConvert.ToString(_segi.skyColor.g));
				writer.WriteAttributeString("skyColorB", XmlConvert.ToString(_segi.skyColor.b));
				writer.WriteAttributeString("skyColorA", XmlConvert.ToString(_segi.skyColor.a));

				writer.WriteAttributeString("skyIntensity", XmlConvert.ToString(_segi.skyIntensity));
				writer.WriteAttributeString("sphericalSkylight", XmlConvert.ToString(_segi.sphericalSkylight));

				writer.WriteAttributeString("temporalBlendWeight", XmlConvert.ToString(_segi.temporalBlendWeight));
				writer.WriteAttributeString("useBilateralFiltering", XmlConvert.ToString(_segi.useBilateralFiltering));
				writer.WriteAttributeString("halfResolution", XmlConvert.ToString(_segi.halfResolution));
				writer.WriteAttributeString("stochasticSampling", XmlConvert.ToString(_segi.stochasticSampling));

				writer.WriteAttributeString("cones", XmlConvert.ToString(_segi.cones));
				writer.WriteAttributeString("coneTraceSteps", XmlConvert.ToString(_segi.coneTraceSteps));
				writer.WriteAttributeString("coneLength", XmlConvert.ToString(_segi.coneLength));
				writer.WriteAttributeString("coneWidth", XmlConvert.ToString(_segi.coneWidth));
				writer.WriteAttributeString("coneTraceBias", XmlConvert.ToString(_segi.coneTraceBias));
				writer.WriteAttributeString("occlusionStrength", XmlConvert.ToString(_segi.occlusionStrength));
				writer.WriteAttributeString("nearOcclusionStrength", XmlConvert.ToString(_segi.nearOcclusionStrength));
				writer.WriteAttributeString("farOcclusionStrength", XmlConvert.ToString(_segi.farOcclusionStrength));
				writer.WriteAttributeString("farthestOcclusionStrength", XmlConvert.ToString(_segi.farthestOcclusionStrength));
				writer.WriteAttributeString("occlusionPower", XmlConvert.ToString(_segi.occlusionPower));
				writer.WriteAttributeString("nearLightGain", XmlConvert.ToString(_segi.nearLightGain));
				writer.WriteAttributeString("giGain", XmlConvert.ToString(_segi.giGain));

				writer.WriteAttributeString("secondaryBounceGain", XmlConvert.ToString(_segi.secondaryBounceGain));
				writer.WriteAttributeString("secondaryCones", XmlConvert.ToString(_segi.secondaryCones));
				writer.WriteAttributeString("secondaryOcclusionStrength", XmlConvert.ToString(_segi.secondaryOcclusionStrength));

				writer.WriteAttributeString("doReflections", XmlConvert.ToString(_segi.doReflections));
				writer.WriteAttributeString("reflectionSteps", XmlConvert.ToString(_segi.reflectionSteps));
				writer.WriteAttributeString("reflectionOcclusionPower", XmlConvert.ToString(_segi.reflectionOcclusionPower));
				writer.WriteAttributeString("skyReflectionIntensity", XmlConvert.ToString(_segi.skyReflectionIntensity));

				writer.WriteAttributeString("visualizeSunDepthTexture", XmlConvert.ToString(_segi.visualizeSunDepthTexture));
				writer.WriteAttributeString("visualizeGI", XmlConvert.ToString(_segi.visualizeGI));
				writer.WriteAttributeString("visualizeVoxels", XmlConvert.ToString(_segi.visualizeVoxels));
				writer.WriteEndElement();
			}

			if (_amplifyBloom != null)
			{
				writer.WriteStartElement("amplifyBloom");
				EffectData effectData = self.effectsDictionary[_amplifyBloom];

				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("UpscaleQuality", XmlConvert.ToString((int)_amplifyBloom.UpscaleQuality));
				writer.WriteAttributeString("MainThresholdSize", XmlConvert.ToString((int)_amplifyBloom.MainThresholdSize));
				writer.WriteAttributeString("CurrentPrecisionMode", XmlConvert.ToString((int)_amplifyBloom.CurrentPrecisionMode));
				writer.WriteAttributeString("BloomRange", XmlConvert.ToString(_amplifyBloom.BloomRange));
				writer.WriteAttributeString("OverallIntensity", XmlConvert.ToString(_amplifyBloom.OverallIntensity));
				writer.WriteAttributeString("OverallThreshold", XmlConvert.ToString(_amplifyBloom.OverallThreshold));
				writer.WriteAttributeString("UpscaleBlurRadius", XmlConvert.ToString(_amplifyBloom.UpscaleBlurRadius));
				writer.WriteAttributeString("DebugToScreen", XmlConvert.ToString((int)_amplifyBloom.DebugToScreen));
				writer.WriteAttributeString("TemporalFilteringActive", XmlConvert.ToString(_amplifyBloom.TemporalFilteringActive));
				writer.WriteAttributeString("TemporalFilteringValue", XmlConvert.ToString(_amplifyBloom.TemporalFilteringValue));
				writer.WriteAttributeString("SeparateFeaturesThreshold", XmlConvert.ToString(_amplifyBloom.SeparateFeaturesThreshold));
				writer.WriteAttributeString("FeaturesThreshold", XmlConvert.ToString(_amplifyBloom.FeaturesThreshold));
				writer.WriteAttributeString("ApplyLensDirt", XmlConvert.ToString(_amplifyBloom.ApplyLensDirt));
				writer.WriteAttributeString("LensDirtStrength", XmlConvert.ToString(_amplifyBloom.LensDirtStrength));
				writer.WriteAttributeString("LensDirtTexture", XmlConvert.ToString((int)_amplifyBloom.LensDirtTexture));
				writer.WriteAttributeString("ApplyLensStardurst", XmlConvert.ToString(_amplifyBloom.ApplyLensStardurst));
				writer.WriteAttributeString("LensStarburstStrength", XmlConvert.ToString(_amplifyBloom.LensStarburstStrength));
				writer.WriteAttributeString("LensStardurstTex", XmlConvert.ToString((int)_amplifyBloom.LensStardurstTex));
				writer.WriteAttributeString("BokehFilterInstance.ApplyBokeh", XmlConvert.ToString(_amplifyBloom.BokehFilterInstance.ApplyBokeh));
				writer.WriteAttributeString("BokehFilterInstance.ApplyOnBloomSource", XmlConvert.ToString(_amplifyBloom.BokehFilterInstance.ApplyOnBloomSource));
				writer.WriteAttributeString("BokehFilterInstance.ApertureShape", XmlConvert.ToString((int)_amplifyBloom.BokehFilterInstance.ApertureShape));
				writer.WriteAttributeString("BokehFilterInstance.OffsetRotation", XmlConvert.ToString(_amplifyBloom.BokehFilterInstance.OffsetRotation));
				writer.WriteAttributeString("BokehFilterInstance.BokehSampleRadius", XmlConvert.ToString(_amplifyBloom.BokehFilterInstance.BokehSampleRadius));
				writer.WriteAttributeString("BokehFilterInstance.Aperture", XmlConvert.ToString(_amplifyBloom.BokehFilterInstance.Aperture));
				writer.WriteAttributeString("BokehFilterInstance.FocalLength", XmlConvert.ToString(_amplifyBloom.BokehFilterInstance.FocalLength));
				writer.WriteAttributeString("BokehFilterInstance.FocalDistance", XmlConvert.ToString(_amplifyBloom.BokehFilterInstance.FocalDistance));
				writer.WriteAttributeString("BokehFilterInstance.MaxCoCDiameter", XmlConvert.ToString(_amplifyBloom.BokehFilterInstance.MaxCoCDiameter));
				writer.WriteAttributeString("LensFlareInstance.ApplyLensFlare", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.ApplyLensFlare));
				writer.WriteAttributeString("LensFlareInstance.OverallIntensity", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.OverallIntensity));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGaussianBlurAmount", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount));
				writer.WriteAttributeString("LensFlareInstance.LensFlareNormalizedGhostsIntensity", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostAmount", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareGhostAmount));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostsDispersal", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostChrDistortion", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostsPowerFactor", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostsPowerFalloff", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff));
				writer.WriteAttributeString("LensFlareInstance.LensFlareNormalizedHaloIntensity", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity));
				writer.WriteAttributeString("LensFlareInstance.LensFlareHaloWidth", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareHaloWidth));
				writer.WriteAttributeString("LensFlareInstance.LensFlareHaloChrDistortion", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion));
				writer.WriteAttributeString("LensFlareInstance.LensFlareHaloPowerFactor", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor));
				writer.WriteAttributeString("LensFlareInstance.LensFlareHaloPowerFalloff", XmlConvert.ToString(_amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff));
				writer.WriteAttributeString("LensGlareInstance.ApplyLensGlare", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.ApplyLensGlare));
				writer.WriteAttributeString("LensGlareInstance.Intensity", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.Intensity));
				writer.WriteAttributeString("LensGlareInstance.OverallStreakScale", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.OverallStreakScale));
				writer.WriteAttributeString("LensGlareInstance.OverallTintR", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.OverallTint.r));
				writer.WriteAttributeString("LensGlareInstance.OverallTintG", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.OverallTint.g));
				writer.WriteAttributeString("LensGlareInstance.OverallTintB", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.OverallTint.b));
				writer.WriteAttributeString("LensGlareInstance.OverallTintA", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.OverallTint.a));
				writer.WriteAttributeString("LensGlareInstance.CurrentGlare", XmlConvert.ToString((int)_amplifyBloom.LensGlareInstance.CurrentGlare));
				writer.WriteAttributeString("LensGlareInstance.PerPassDisplacement", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.PerPassDisplacement));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(self.fixAmplifyBloomForUpscaledScreenshots));
				writer.WriteAttributeString("LensGlareInstance.GlareMaxPassCount", XmlConvert.ToString(_amplifyBloom.LensGlareInstance.GlareMaxPassCount));

				writer.WriteStartElement("LensFlareInstance.LensFlareGradient");
				foreach (GradientColorKey key in _amplifyBloom.LensFlareInstance.LensFlareGradient.colorKeys)
				{
					writer.WriteStartElement("colorKey");
					writer.WriteAttributeString("r", XmlConvert.ToString(key.color.r));
					writer.WriteAttributeString("g", XmlConvert.ToString(key.color.g));
					writer.WriteAttributeString("b", XmlConvert.ToString(key.color.b));
					writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
					writer.WriteEndElement();
				}
				foreach (GradientAlphaKey key in _amplifyBloom.LensFlareInstance.LensFlareGradient.alphaKeys)
				{
					writer.WriteStartElement("alphaKey");
					writer.WriteAttributeString("alpha", XmlConvert.ToString(key.alpha));
					writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
					writer.WriteEndElement();
				}
				writer.WriteEndElement();

				writer.WriteStartElement("LensGlareInstance.CromaticColorGradient");
				foreach (GradientColorKey key in _amplifyBloom.LensGlareInstance.CromaticColorGradient.colorKeys)
				{
					writer.WriteStartElement("colorKey");
					writer.WriteAttributeString("r", XmlConvert.ToString(key.color.r));
					writer.WriteAttributeString("g", XmlConvert.ToString(key.color.g));
					writer.WriteAttributeString("b", XmlConvert.ToString(key.color.b));
					writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
					writer.WriteEndElement();
				}
				foreach (GradientAlphaKey key in _amplifyBloom.LensGlareInstance.CromaticColorGradient.alphaKeys)
				{
					writer.WriteStartElement("alphaKey");
					writer.WriteAttributeString("alpha", XmlConvert.ToString(key.alpha));
					writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
					writer.WriteEndElement();
				}
				writer.WriteEndElement();

				writer.WriteEndElement();
			}

			if (_blur != null)
			{
				writer.WriteStartElement("blur");
				EffectData effectData = self.effectsDictionary[_blur];

				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("downsample", XmlConvert.ToString(_blur.downsample));
				writer.WriteAttributeString("blurSize", XmlConvert.ToString(_blur.blurSize));
				writer.WriteAttributeString("blurIterations", XmlConvert.ToString(_blur.blurIterations));
				writer.WriteAttributeString("blurType", XmlConvert.ToString((int)_blur.blurType));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(self.fixBlurForUpscaledScreenshots));
				writer.WriteEndElement();
			}

			writer.WriteStartElement("effectsOrder");
			for (int i = 0; i < self.generalEffects.Count; i++)
			{
				EffectData data = self.generalEffects[i];
				writer.WriteStartElement(data.simpleName);
				writer.WriteAttributeString("index", XmlConvert.ToString(i));
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		#endregion
	}
}

