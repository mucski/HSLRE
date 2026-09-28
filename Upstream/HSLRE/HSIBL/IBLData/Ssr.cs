using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using UnityStandardAssets.CinematicEffects;
using static UnityStandardAssets.CinematicEffects.ScreenSpaceReflection2;

namespace HSIBL.IBLData
{
	[XmlRoot(ElementName = "ssr")]
	public class Ssr
	{
		#region Properties
		[XmlAttribute(AttributeName = "enabled")]
		public bool Enabled { get; set; }
		[XmlAttribute(AttributeName = "fixUpscaledScreenshots")]
		public bool FixUpscaledScreenshots { get; set; }
		[XmlAttribute(AttributeName = "reflectionMultiplier")]
		public float ReflectionMultiplier { get; set; }
		[XmlAttribute(AttributeName = "maxDistance")]
		public float MaxDistance { get; set; }
		[XmlAttribute(AttributeName = "fadeDistance")]
		public float FadeDistance { get; set; }
		[XmlAttribute(AttributeName = "screenEdgeFading")]
		public float ScreenEdgeFading { get; set; }
		[XmlAttribute(AttributeName = "enableHDR")]
		public bool EnableHDR { get; set; }
		[XmlAttribute(AttributeName = "additiveReflection")]
		public bool AdditiveReflection { get; set; }
		[XmlAttribute(AttributeName = "maxSteps")]
		public int MaxSteps { get; set; }
		[XmlAttribute(AttributeName = "rayStepSize")]
		public int RayStepSize { get; set; }
		[XmlAttribute(AttributeName = "widthModifier")]
		public float WidthModifier { get; set; }
		[XmlAttribute(AttributeName = "smoothFallbackThreshold")]
		public float SmoothFallbackThreshold { get; set; }
		[XmlAttribute(AttributeName = "smoothFallbackDistance")]
		public float SmoothFallbackDistance { get; set; }
		[XmlAttribute(AttributeName = "fresnelFade")]
		public float FresnelFade { get; set; }
		[XmlAttribute(AttributeName = "fresnelFadePower")]
		public float FresnelFadePower { get; set; }
		[XmlAttribute(AttributeName = "distanceBlur")]
		public float DistanceBlur { get; set; }
		[XmlAttribute(AttributeName = "temporalFilterStrength")]
		public float TemporalFilterStrength { get; set; }
		[XmlAttribute(AttributeName = "useTemporalConfidence")]
		public bool UseTemporalConfidence { get; set; }
		[XmlAttribute(AttributeName = "traceBehindObjects")]
		public bool TraceBehindObjects { get; set; }
		[XmlAttribute(AttributeName = "highQualitySharpReflections")]
		public bool HighQualitySharpReflections { get; set; }
		[XmlAttribute(AttributeName = "traceEverywhere")]
		public bool TraceEverywhere { get; set; }
		[XmlAttribute(AttributeName = "treatBackfaceHitAsMiss")]
		public bool TreatBackfaceHitAsMiss { get; set; }
		[XmlAttribute(AttributeName = "allowBackwardsRays")]
		public bool AllowBackwardsRays { get; set; }
		[XmlAttribute(AttributeName = "improveCorners")]
		public bool ImproveCorners { get; set; }
		[XmlAttribute(AttributeName = "resolution")]
		public int Resolution { get; set; }
		[XmlAttribute(AttributeName = "bilateralUpsample")]
		public bool BilateralUpsample { get; set; }
		[XmlAttribute(AttributeName = "reduceBanding")]
		public bool ReduceBanding { get; set; }
		[XmlAttribute(AttributeName = "highlightSuppression")]
		public bool HighlightSuppression { get; set; }

		// new settings with no previous equivalent
		[XmlAttribute(AttributeName = "reflectionBlur")]
		public float ReflectionBlur { get; set; }
		#endregion

		public static SSRSettings GetSettings(XmlNode node, ScreenSpaceReflection2 _ssr)
		{
			Ssr data = null;

			using (XmlNodeReader reader = new XmlNodeReader(node))
			{
				data = (Ssr)new XmlSerializer(typeof(Ssr)).Deserialize(reader);
			}

			// print data
			/*using (var sww = new StringWriter())
			{
				using (XmlWriter writer = XmlWriter.Create(sww))
				{
					//Create our own namespaces for the output, add empty value to ignore xsi/xsd
					XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
					ns.Add("", "");

					new XmlSerializer(typeof(Ssr)).Serialize(writer, data, ns);

					XmlDocument doc1 = new XmlDocument();
					doc1.LoadXml(sww.ToString());
					Console.WriteLine("2---" + doc1.DocumentElement.OuterXml);

					UnityEngine.Debug.Log("[IBL] Loaded SSR data:\r\n" + sww.ToString());
				}
			}*/


			// Separately load data for variables that were unexposed in the official version of the package
			_ssr.traceBehindObjects = data.TraceBehindObjects;
			_ssr.treatBackfaceHitAsMiss = data.TreatBackfaceHitAsMiss;
			_ssr.highlightSuppression = data.HighlightSuppression;
			_ssr.bilateralUpsample = data.BilateralUpsample;
			
			// create SSR settings from data
			SSRSettings ssrSettings = new SSRSettings
			{
				reflectionSettings = new ReflectionSettings
				{
					blendType = data.AdditiveReflection ? SSRReflectionBlendType.Additive : SSRReflectionBlendType.PhysicallyBased,
					reflectionQuality = (SSRResolution)data.Resolution, // special case, must be 0 or 2
					maxDistance = data.MaxDistance,
					iterationCount = data.MaxSteps,
					stepSize = data.RayStepSize,
					widthModifier = data.WidthModifier,
					reflectionBlur = data.ReflectionBlur == 0 ? 1 : data.ReflectionBlur, // old SSR component doesn't have ReflectionBlur (value = 0), case in which: set to a default value of "1"
					reflectBackfaces = data.AllowBackwardsRays,
				},

				intensitySettings = new IntensitySettings
				{
					reflectionMultiplier = data.ReflectionMultiplier, // 1.0f,
					fadeDistance = data.FadeDistance, // 100.0f,

					fresnelFade = data.FresnelFade, // 1.0f,
					fresnelFadePower = data.FresnelFadePower, // 1.0f,
				},

				screenEdgeMask = new ScreenEdgeMask
				{
					intensity = data.ScreenEdgeFading // 0.03f
				}
			};
			return ssrSettings;
		}

		public static string GetSettings_XmlNode(ScreenSpaceReflection2 _ssr, bool enabled, bool fixUpscaledScreenshots)
		{
			var reflectionSettings = _ssr.settings.reflectionSettings;
			var intensitySettings = _ssr.settings.intensitySettings;
			var screenEdgeMask = _ssr.settings.screenEdgeMask;

			// create IBLData.Ssr object with MB data, use default values for out-of-support properties
			Ssr data = new Ssr()
			{
				Enabled = enabled,
				FixUpscaledScreenshots = fixUpscaledScreenshots,
				ReflectionMultiplier = intensitySettings.reflectionMultiplier,
				MaxDistance = reflectionSettings.maxDistance,
				FadeDistance = intensitySettings.fadeDistance,
				ScreenEdgeFading = screenEdgeMask.intensity,
				EnableHDR = _ssr.camera_.hdr,		// compatibility
				AdditiveReflection = reflectionSettings.blendType == SSRReflectionBlendType.Additive,
				MaxSteps = reflectionSettings.iterationCount,
				RayStepSize = reflectionSettings.stepSize,
				WidthModifier = reflectionSettings.widthModifier,
				SmoothFallbackThreshold = 0.2f,		// compatibility
				SmoothFallbackDistance = 0.042f,	// compatibility | main game default: 0.05f
				FresnelFade = intensitySettings.fresnelFade,
				FresnelFadePower = intensitySettings.fresnelFadePower,
				DistanceBlur = 1f,					// compatibility
				TemporalFilterStrength = 0.05f,		// compatibility | main game default: 0.7f
				UseTemporalConfidence = true,		// compatibility
				TraceBehindObjects = _ssr.traceBehindObjects,
				HighQualitySharpReflections = true,	// compatibility
				TraceEverywhere = false,			// compatibility | false main game default: true
				TreatBackfaceHitAsMiss = _ssr.treatBackfaceHitAsMiss,
				AllowBackwardsRays = reflectionSettings.reflectBackfaces,
				ImproveCorners = true,
				Resolution = (int)reflectionSettings.reflectionQuality,
				BilateralUpsample = _ssr.bilateralUpsample,
				ReduceBanding = true,				// compatibility
				HighlightSuppression = _ssr.highlightSuppression,

				// new settings
				ReflectionBlur = reflectionSettings.reflectionBlur
			};



			// convert IBLData.Ssr object to xml string
			using (var sww = new StringWriter())
			{
				using (XmlWriter writer = XmlWriter.Create(sww))
				{
					//Create our own namespaces for the output, add empty value to ignore xsi/xsd
					XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
					ns.Add("", "");

					new XmlSerializer(typeof(Ssr)).Serialize(writer, data, ns);

					XmlDocument doc1 = new XmlDocument();
					doc1.LoadXml(sww.ToString());
					return doc1.DocumentElement.OuterXml;
				}
			}
		}
	}
}