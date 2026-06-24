using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UEPhysics = UnityEngine.Physics;


namespace PogoTAS // credits: RotaryHeart.Lib.PhysicsExtension
{

	public enum PreviewCondition
	{
		None, Editor, Game, Both
	}

	public enum CastDrawType
	{
		Minimal, Complete
	}
	[RequireComponent(typeof(Camera))]
	public class GLDebug : MonoBehaviour
	{
		private class LineDrawingData
		{
			private const int Capacity = 50;

			public readonly List<Vector3> startPos = new List<Vector3>(50);

			public readonly List<Vector3> endPos = new List<Vector3>(50);

			public readonly List<Color> colors = new List<Color>(50);

			public readonly List<float> startTime = new List<float>(50);

			public readonly List<float> durations = new List<float>(50);

			public void RemoveAt(int i)
			{
				startPos.RemoveAt(i);
				endPos.RemoveAt(i);
				colors.RemoveAt(i);
				startTime.RemoveAt(i);
				durations.RemoveAt(i);
			}

			public void Add(Vector3 start, Vector3 end, Color color, float time, float duration)
			{
				startPos.Add(start);
				endPos.Add(end);
				colors.Add(color);
				startTime.Add(time);
				durations.Add(duration);
			}
		}

		private static bool M_initialized;

		private static GLDebug M_instance;

		public bool displayLines = true;

		public Shader zOnShader;

		public Shader zOffShader;

		private readonly LineDrawingData m_linesZOn = new LineDrawingData();

		private readonly LineDrawingData m_linesZOff = new LineDrawingData();

		private Material m_matZOn;

		private Material m_matZOff;

		public static GLDebug Instance
		{
			get
			{
				if (!M_initialized )
				{
					int camCount = Camera.allCameras.Length;
					if (camCount <= 1)
					{
						return null;
					}
					Camera main = Camera.allCameras[1];
					if (main == null)
					{
						throw new Exception("Couldn't find any main camera to attach the GLDebug script. System will not work");
					}

					M_instance = main.gameObject.AddComponent<GLDebug>();
					M_initialized = true;
				}

				return M_instance;
			}
		}

		private void Awake()
		{
			if (M_instance == null)
			{
				M_instance = this;
				M_initialized = true;
			}
			else if (M_instance != this)
			{
				UnityEngine.Object.Destroy(this);
				return;
			}

			MaterialSetup();
		}

		private void OnEnable()
		{
			RenderPipelineManager.endCameraRendering += OnCameraRender;
		}

		private void OnDisable()
		{
			RenderPipelineManager.endCameraRendering -= OnCameraRender;
		}

		private void MaterialSetup()
		{

			Shader shader = Shader.Find("Hidden/Internal-Colored");//Shader.Find("Unlit/Draw2DWDepth");//Shader.Find("ProBuilder/UnlitVertexColor");//Shader.Find("Hidden/Internal-Colored")
			if (shader)
			{
				m_matZOn = new Material(shader);
				m_matZOn.hideFlags = HideFlags.HideAndDontSave;
				m_matZOn.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				m_matZOn.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				m_matZOn.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
				m_matZOn.SetInt("_ZWrite", 1);
				m_matZOn.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);

				m_matZOff = new Material(shader);
				m_matZOff.hideFlags = HideFlags.HideAndDontSave;
				m_matZOff.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				m_matZOff.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				m_matZOff.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
				m_matZOff.SetInt("_ZWrite", 1);
				m_matZOff.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.GreaterEqual);
			}

			//	m_matZOn = ((zOnShader == null) ? new Material(Shader.Find("Debug/GLlineZOn")) : new Material(zOnShader));
			//m_matZOff = ((zOffShader == null) ? new Material(Shader.Find("Debug/GLlineZOff")) : new Material(zOffShader));
			m_matZOn.hideFlags = HideFlags.HideAndDontSave;
			m_matZOn.shader.hideFlags = HideFlags.HideAndDontSave;
			m_matZOff.hideFlags = HideFlags.HideAndDontSave;
			m_matZOff.shader.hideFlags = HideFlags.HideAndDontSave;
		}

		private void OnPostRender()
		{
			DrawLines();

		}

		private void OnRenderObject()
		{
			DrawLines();
		}

		private void OnCameraRender(ScriptableRenderContext context, Camera cam)
		{
			DrawLines();
		}

		private void DrawLines()
		{
			if (!displayLines)
			{
				return;
			}

			float time = Time.time;
			int count = m_linesZOn.colors.Count;
			if (count > 0)
			{
				m_matZOn.SetPass(0);
				GL.Begin(1);
				for (int num = count - 1; num >= 0; num -= 2)
				{
					GL.Color(m_linesZOn.colors[num]);
					GL.Vertex(m_linesZOn.startPos[num]);
					GL.Vertex(m_linesZOn.endPos[num]);
					if (time - m_linesZOn.startTime[num] >= m_linesZOn.durations[num])
					{
						m_linesZOn.RemoveAt(num);
					}

					int num2 = num - 1;
					if (num2 >= 0)
					{
						GL.Color(m_linesZOn.colors[num2]);
						GL.Vertex(m_linesZOn.startPos[num2]);
						GL.Vertex(m_linesZOn.endPos[num2]);
						if (time - m_linesZOn.startTime[num2] >= m_linesZOn.durations[num2])
						{
							m_linesZOn.RemoveAt(num2);
						}
					}
				}

				GL.End();
			}

			count = m_linesZOff.colors.Count;
			if (count <= 0)
			{
				return;
			}

			m_matZOff.SetPass(0);
			GL.Begin(1);
			for (int num3 = count - 1; num3 >= 0; num3 -= 2)
			{
				GL.Color(m_linesZOff.colors[num3]);
				GL.Vertex(m_linesZOff.startPos[num3]);
				GL.Vertex(m_linesZOff.endPos[num3]);
				if (time - m_linesZOff.startTime[num3] >= m_linesZOff.durations[num3])
				{
					m_linesZOff.RemoveAt(num3);
				}

				int num4 = num3 - 1;
				if (num4 >= 0)
				{
					GL.Color(m_linesZOff.colors[num4]);
					GL.Vertex(m_linesZOff.startPos[num4]);
					GL.Vertex(m_linesZOff.endPos[num4]);
					if (time - m_linesZOff.startTime[num4] >= m_linesZOff.durations[num4])
					{
						m_linesZOff.RemoveAt(num4);
					}
				}
			}

			GL.End();
		}

		public static void DrawLine(Vector3 start, Vector3 end, Color? color = null, float duration = 0f, bool depthTest = false)
		{
			if (Instance != null && Instance.displayLines)
			{
				(depthTest ? M_instance.m_linesZOn : M_instance.m_linesZOff).Add(start, end, color ?? Color.white, (duration > 0f) ? Time.time : 0f, duration);
			}
		}

		public static void DrawRay(Vector3 start, Vector3 dir, Color? color = null, float duration = 0f, bool depthTest = false)
		{
			DrawLine(start, start + dir, color, duration, depthTest);
		}
	}
	public static partial class DebugExtensions
	{
		public static void DebugSquare(Vector3 origin, Vector3 halfExtents, Color color, Quaternion orientation,
			float drawDuration = 0, PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			Vector3 forward = orientation * Vector3.forward;
			Vector3 up = orientation * Vector3.up;
			Vector3 right = orientation * Vector3.right;

			Vector3 topMinY1 = origin + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 topMaxY1 = origin - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 botMinY1 = origin + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 botMaxY1 = origin - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

			DrawLine(topMinY1, botMinY1, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxY1, botMaxY1, color, drawDuration, preview, drawDepth);
			DrawLine(topMinY1, topMaxY1, color, drawDuration, preview, drawDepth);
			DrawLine(botMinY1, botMaxY1, color, drawDuration, preview, drawDepth);
		}

		public static void DebugBox(Vector3 origin, Vector3 halfExtents, Vector3 direction, float maxDistance, Color color,
			Quaternion orientation, Color endColor, bool drawBase = true, float drawDuration = 0,
			PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			Vector3 end = origin + direction * (float.IsPositiveInfinity(maxDistance) ? 1000 * 1000 : maxDistance);

			Vector3 forward = orientation * Vector3.forward;
			Vector3 up = orientation * Vector3.up;
			Vector3 right = orientation * Vector3.right;

			#region Coords

			#region End coords

			Vector3 topMinX0 = end + (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 topMaxX0 = end - (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 topMinY0 = end + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 topMaxY0 = end - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);

			Vector3 botMinX0 = end + (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 botMaxX0 = end - (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 botMinY0 = end + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 botMaxY0 = end - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

			#endregion

			#region Origin coords

			Vector3 topMinX1 = origin + (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 topMaxX1 = origin - (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 topMinY1 = origin + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 topMaxY1 = origin - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);

			Vector3 botMinX1 = origin + (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 botMaxX1 = origin - (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 botMinY1 = origin + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 botMaxY1 = origin - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

			#endregion

			#endregion

			#region Draw lines

			#region Origin box

			if (drawBase)
			{
				DrawLine(topMinX1, botMinX1, color, drawDuration, preview, drawDepth);
				DrawLine(topMaxX1, botMaxX1, color, drawDuration, preview, drawDepth);
				DrawLine(topMinY1, botMinY1, color, drawDuration, preview, drawDepth);
				DrawLine(topMaxY1, botMaxY1, color, drawDuration, preview, drawDepth);

				DrawLine(topMinX1, topMaxX1, color, drawDuration, preview, drawDepth);
				DrawLine(topMinX1, topMinY1, color, drawDuration, preview, drawDepth);
				DrawLine(topMinY1, topMaxY1, color, drawDuration, preview, drawDepth);
				DrawLine(topMaxY1, topMaxX1, color, drawDuration, preview, drawDepth);

				DrawLine(botMinX1, botMaxX1, color, drawDuration, preview, drawDepth);
				DrawLine(botMinX1, botMinY1, color, drawDuration, preview, drawDepth);
				DrawLine(botMinY1, botMaxY1, color, drawDuration, preview, drawDepth);
				DrawLine(botMaxY1, botMaxX1, color, drawDuration, preview, drawDepth);
			}

			#endregion

			#region Connection between boxes

			DrawLine(topMinX0, topMinX1, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxX0, topMaxX1, color, drawDuration, preview, drawDepth);
			DrawLine(topMinY0, topMinY1, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxY0, topMaxY1, color, drawDuration, preview, drawDepth);

			DrawLine(botMinX0, botMinX1, color, drawDuration, preview, drawDepth);
			DrawLine(botMinX0, botMinX1, color, drawDuration, preview, drawDepth);
			DrawLine(botMinY0, botMinY1, color, drawDuration, preview, drawDepth);
			DrawLine(botMaxY0, botMaxY1, color, drawDuration, preview, drawDepth);

			#endregion

			#region End box

			color = endColor;

			DrawLine(topMinX0, botMinX0, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxX0, botMaxX0, color, drawDuration, preview, drawDepth);
			DrawLine(topMinY0, botMinY0, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxY0, botMaxY0, color, drawDuration, preview, drawDepth);

			DrawLine(topMinX0, topMaxX0, color, drawDuration, preview, drawDepth);
			DrawLine(topMinX0, topMinY0, color, drawDuration, preview, drawDepth);
			DrawLine(topMinY0, topMaxY0, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxY0, topMaxX0, color, drawDuration, preview, drawDepth);

			DrawLine(botMinX0, botMaxX0, color, drawDuration, preview, drawDepth);
			DrawLine(botMinX0, botMinY0, color, drawDuration, preview, drawDepth);
			DrawLine(botMinY0, botMaxY0, color, drawDuration, preview, drawDepth);
			DrawLine(botMaxY0, botMaxX0, color, drawDuration, preview, drawDepth);

			#endregion

			#endregion
		}

		public static void DebugBoxCast(Vector3 origin, Vector3 halfExtents, Vector3 direction, float maxDistance, Color color, Quaternion orientation,
			float drawDuration = 0, CastDrawType drawType = CastDrawType.Minimal, PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			if (drawType == CastDrawType.Minimal)
			{
				DrawLine(origin, origin + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}
			else
			{
				Vector3 forward = orientation * Vector3.forward;
				Vector3 up = orientation * Vector3.up;
				Vector3 right = orientation * Vector3.right;

				Vector3 topMinX1 = origin + (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
				Vector3 topMaxX1 = origin - (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
				Vector3 topMinY1 = origin + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
				Vector3 topMaxY1 = origin - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);

				Vector3 botMinX1 = origin + (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
				Vector3 botMaxX1 = origin - (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
				Vector3 botMinY1 = origin + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
				Vector3 botMaxY1 = origin - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

				DrawLine(topMinX1, topMinX1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(topMaxX1, topMaxX1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(topMinY1, topMinY1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(topMaxY1, topMaxY1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(botMinX1, botMinX1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(botMaxX1, botMaxX1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(botMinY1, botMinY1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(botMaxY1, botMaxY1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}

			DebugBox(origin, halfExtents, Physics.M_castColor, orientation, drawDuration, preview, drawDepth);
			DebugBox(origin + direction * maxDistance, halfExtents, color, orientation, drawDuration, preview, drawDepth);
		}

		public static void DebugBox(Vector3 origin, Vector3 halfExtents, Color color, Quaternion orientation,
			float drawDuration = 0, PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			Vector3 forward = orientation * Vector3.forward;
			Vector3 up = orientation * Vector3.up;
			Vector3 right = orientation * Vector3.right;

			Vector3 topMinX1 = origin + (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 topMaxX1 = origin - (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 topMinY1 = origin + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 topMaxY1 = origin - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);

			Vector3 botMinX1 = origin + (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 botMaxX1 = origin - (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
			Vector3 botMinY1 = origin + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
			Vector3 botMaxY1 = origin - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

			DrawLine(topMinX1, botMinX1, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxX1, botMaxX1, color, drawDuration, preview, drawDepth);
			DrawLine(topMinY1, botMinY1, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxY1, botMaxY1, color, drawDuration, preview, drawDepth);

			DrawLine(topMinX1, topMaxX1, color, drawDuration, preview, drawDepth);
			DrawLine(topMinX1, topMinY1, color, drawDuration, preview, drawDepth);
			DrawLine(topMinY1, topMaxY1, color, drawDuration, preview, drawDepth);
			DrawLine(topMaxY1, topMaxX1, color, drawDuration, preview, drawDepth);

			DrawLine(botMinX1, botMaxX1, color, drawDuration, preview, drawDepth);
			DrawLine(botMinX1, botMinY1, color, drawDuration, preview, drawDepth);
			DrawLine(botMinY1, botMaxY1, color, drawDuration, preview, drawDepth);
			DrawLine(botMaxY1, botMaxX1, color, drawDuration, preview, drawDepth);
		}

		public static void DebugOneSidedCapsuleCast(Vector3 baseSphere, Vector3 endSphere, Vector3 direction, float maxDistance,
			Color color, float radius = 1.0f, float drawDuration = 0, CastDrawType drawType = CastDrawType.Minimal,
			PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			Vector3 midPoint = (baseSphere + endSphere) / 2f;

			DebugOneSidedCapsule(baseSphere, endSphere, Physics.M_castColor, radius, true, drawDuration, preview, drawDepth);

			if (drawType == CastDrawType.Minimal)
			{
				DrawLine(midPoint, midPoint + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}
			else
			{
				Vector3 up = (endSphere - baseSphere).normalized;
				if (up == Vector3.zero)
				{
					up = Vector3.up;
				}
				Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
				Vector3 right = Vector3.Cross(up, forward).normalized;

				DrawLine(baseSphere + right * radius, baseSphere + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(endSphere + right * radius, endSphere + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

				DrawLine(baseSphere - right * radius, baseSphere - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(endSphere - right * radius, endSphere - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

				DrawLine(endSphere + up * radius, endSphere + up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(baseSphere - up * radius, baseSphere - up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}

			DebugOneSidedCapsule(baseSphere + direction * maxDistance, endSphere + direction * maxDistance, color, radius, true, drawDuration, preview,
				drawDepth);
		}

		public static void DebugOneSidedCapsule(Vector3 baseSphere, Vector3 endSphere, Color color, float radius = 1,
			bool colorizeBase = false, float drawDuration = 0,
			PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			Vector3 up = (endSphere - baseSphere).normalized * radius;
			if (up == Vector3.zero)
			{
				up = Vector3.up;
			}
			Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
			Vector3 right = Vector3.Cross(up, forward).normalized * radius;

			//Side lines
			DrawLine(baseSphere + right, endSphere + right, color, drawDuration, preview, drawDepth);
			DrawLine(baseSphere - right, endSphere - right, color, drawDuration, preview, drawDepth);

			//Draw end caps
			for (int i = 1; i < 26; i++)
			{
				//Start endcap
				DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + baseSphere, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + baseSphere,
					colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
				DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + baseSphere, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + baseSphere,
					colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);

				//End endcap
				DrawLine(Vector3.Slerp(right, up, i / 25.0f) + endSphere, Vector3.Slerp(right, up, (i - 1) / 25.0f) + endSphere, color, drawDuration, preview,
					drawDepth);
				DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + endSphere, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + endSphere, color,
					drawDuration, preview, drawDepth);
			}
		}

		public static void DebugCapsuleCast(Vector3 baseSphere, Vector3 endSphere, Vector3 direction, float maxDistance,
			Color color, float radius = 1.0f, float drawDuration = 0, CastDrawType drawType = CastDrawType.Minimal,
			PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			Vector3 midPoint = (baseSphere + endSphere) / 2;

			DebugCapsule(baseSphere, endSphere, Physics.M_castColor, radius, true, drawDuration, preview, drawDepth);

			if (drawType == CastDrawType.Minimal)
			{
				DrawLine(midPoint, midPoint + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}
			else
			{
				Vector3 up = (endSphere - baseSphere).normalized;
				if (up == Vector3.zero)
				{
					up = Vector3.up;
				}
				Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
				Vector3 right = Vector3.Cross(up, forward).normalized;

				DrawLine(baseSphere + right * radius, baseSphere + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(endSphere + right * radius, endSphere + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

				DrawLine(baseSphere - right * radius, baseSphere - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(endSphere - right * radius, endSphere - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

				DrawLine(baseSphere + forward * radius, baseSphere + forward * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(endSphere + forward * radius, endSphere + forward * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

				DrawLine(baseSphere - forward * radius, baseSphere - forward * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(endSphere - forward * radius, endSphere - forward * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

				DrawLine(endSphere + up * radius, endSphere + up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(baseSphere - up * radius, baseSphere - up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}

			DebugCapsule(baseSphere + direction * maxDistance, endSphere + direction * maxDistance, color, radius, true, drawDuration, preview, drawDepth);
		}

		public static void DebugCapsule(Vector3 baseSphere, Vector3 endSphere, Color color, float radius = 1,
			bool colorizeBase = true, float drawDuration = 0,
			PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			Vector3 up = (endSphere - baseSphere).normalized * radius;
			if (up == Vector3.zero)
			{
				up = Vector3.up;
			}
			Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
			Vector3 right = Vector3.Cross(up, forward).normalized * radius;

			//Radial circles
			DebugCircle(baseSphere, up, colorizeBase ? color : Color.red, radius, drawDuration, preview, drawDepth);
			DebugCircle(endSphere, -up, color, radius, drawDuration, preview, drawDepth);

			//Side lines
			DrawLine(baseSphere + right, endSphere + right, color, drawDuration, preview, drawDepth);
			DrawLine(baseSphere - right, endSphere - right, color, drawDuration, preview, drawDepth);

			DrawLine(baseSphere + forward, endSphere + forward, color, drawDuration, preview, drawDepth);
			DrawLine(baseSphere - forward, endSphere - forward, color, drawDuration, preview, drawDepth);

			//Draw end caps
			for (int i = 1; i < 26; i++)
			{
				//End endcap
				DrawLine(Vector3.Slerp(right, up, i / 25.0f) + endSphere, Vector3.Slerp(right, up, (i - 1) / 25.0f) + endSphere, color, drawDuration, preview,
					drawDepth);
				DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + endSphere, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + endSphere, color,
					drawDuration, preview, drawDepth);
				DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + endSphere, Vector3.Slerp(forward, up, (i - 1) / 25.0f) + endSphere, color,
					drawDuration, preview, drawDepth);
				DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + endSphere, Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + endSphere, color,
					drawDuration, preview, drawDepth);

				//Start endcap
				DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + baseSphere, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + baseSphere,
					colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
				DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + baseSphere, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + baseSphere,
					colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
				DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + baseSphere, Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + baseSphere,
					colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
				DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + baseSphere, Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + baseSphere,
					colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
			}
		}

		public static void DebugCircleCast(Vector3 origin, Vector3 direction, float maxDistance, Color color, float radius, float drawDuration,
			CastDrawType drawType, PreviewCondition preview, bool drawDepth)
		{
			DebugCircle(origin, Vector3.forward, Physics.M_castColor, radius, drawDuration, preview, drawDepth);

			if (drawType == CastDrawType.Minimal)
			{
				DrawLine(origin, origin + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}
			else
			{
				Vector3 up = origin.normalized * radius;
				if (up == Vector3.zero)
				{
					up = Vector3.up;
				}
				Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
				Vector3 right = Vector3.Cross(up, forward).normalized * radius;

				DrawLine(origin + right * radius, origin + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(origin - right * radius, origin - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(origin + right * radius, origin + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(origin + up * radius, origin + up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(origin - up * radius, origin - up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}

			DebugCircle(origin + direction * maxDistance, Vector3.forward, color, radius, drawDuration, preview, drawDepth);
		}

		public static void DebugCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f,
			float drawDuration = 0, PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			Vector3 upDir = up.normalized * radius;
			Vector3 forwardDir = Vector3.Slerp(upDir, -upDir, 0.5f);
			Vector3 rightDir = Vector3.Cross(upDir, forwardDir).normalized * radius;

			Matrix4x4 matrix = new Matrix4x4()
			{
				[0] = rightDir.x,
				[1] = rightDir.y,
				[2] = rightDir.z,

				[4] = upDir.x,
				[5] = upDir.y,
				[6] = upDir.z,

				[8] = forwardDir.x,
				[9] = forwardDir.y,
				[10] = forwardDir.z
			};

			Vector3 lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
			Vector3 nextPoint = Vector3.zero;

			color = (color == default(Color)) ? Color.white : color;

			for (var i = 0; i < 91; i++)
			{
				nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
				nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
				nextPoint.y = 0;

				nextPoint = position + matrix.MultiplyPoint3x4(nextPoint);

				DrawLine(lastPoint, nextPoint, color, drawDuration, preview, drawDepth);

				lastPoint = nextPoint;
			}
		}

		public static void DebugPoint(Vector3 position, Color color, float scale = 0.5f, float drawDuration = 0,
			PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			color = (color == default(Color)) ? Color.white : color;

			DrawLine(position + (Vector3.up * (scale * 0.5f)), position - Vector3.up * scale, color, drawDuration, preview, drawDepth);
			DrawLine(position + (Vector3.right * (scale * 0.5f)), position - Vector3.right * scale, color, drawDuration, preview, drawDepth);
			DrawLine(position + (Vector3.forward * (scale * 0.5f)), position - Vector3.forward * scale, color, drawDuration, preview, drawDepth);
		}

		public static void DebugSphereCast(Vector3 origin, Vector3 direction, float maxDistance, Color color, float radius, float drawDuration,
			CastDrawType drawType, PreviewCondition preview, bool drawDepth)
		{
			DebugWireSphere(origin, Physics.M_castColor, radius, drawDuration, preview, drawDepth);

			if (drawType == CastDrawType.Minimal)
			{
				DrawLine(origin, origin + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}
			else
			{
				Vector3 up = origin.normalized * radius;
				if (up == Vector3.zero)
				{
					up = Vector3.up;
				}
				Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
				Vector3 right = Vector3.Cross(up, forward).normalized * radius;

				DrawLine(origin + right * radius, origin + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(origin - right * radius, origin - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(origin + right * radius, origin + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(origin + up * radius, origin + up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
				DrawLine(origin - up * radius, origin - up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
			}

			DebugWireSphere(origin + direction * maxDistance, color, radius, drawDuration, preview, drawDepth);
		}

		public static void DebugWireSphere(Vector3 position, Color color, float radius = 1.0f, float drawDuration = 0,
			PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			float angle = 10.0f;

			Vector3 x = new Vector3(position.x, position.y + radius * Mathf.Sin(0), position.z + radius * Mathf.Cos(0));
			Vector3 y = new Vector3(position.x + radius * Mathf.Cos(0), position.y, position.z + radius * Mathf.Sin(0));
			Vector3 z = new Vector3(position.x + radius * Mathf.Cos(0), position.y + radius * Mathf.Sin(0), position.z);

			for (int i = 1; i < 37; i++)
			{
				Vector3 new_x = new Vector3(position.x, position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad),
					position.z + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad));
				Vector3 new_y = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y,
					position.z + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad));
				Vector3 new_z = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad),
					position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z);

				DrawLine(x, new_x, color, drawDuration, preview, drawDepth);
				DrawLine(y, new_y, color, drawDuration, preview, drawDepth);
				DrawLine(z, new_z, color, drawDuration, preview, drawDepth);

				x = new_x;
				y = new_y;
				z = new_z;
			}
		}

		private static void GetDrawStates(PreviewCondition preview, out bool drawEditor, out bool drawGame)
		{
			drawEditor = false;
			drawGame = false;

			switch (preview)
			{
				case PreviewCondition.Editor:
					drawEditor = true;
					break;

				case PreviewCondition.Game:
					drawGame = true;
					break;

				case PreviewCondition.Both:
					drawEditor = true;
					drawGame = true;
					break;
			}
		}

		public static void DrawLine(Vector3 start, Vector3 end, Color? color = null, float duration = 0f,
			PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
		{
			GetDrawStates(preview, out bool drawEditor, out bool drawGame);

			if (drawEditor)
			{
				Debug.DrawLine(start, end, color ?? Color.white, duration, drawDepth);
			}

			if (drawGame)
			{
				GLDebug.DrawLine(start, end, color ?? Color.white, duration, drawDepth);
			}
		}
	}
	public static partial class Physics
	{
		#region Unity Engine Physics
		//Global variables for use on default values, this is left here so that it can be changed easily
		public static Quaternion M_orientation = default(Quaternion);
		public static float M_maxDistance = Mathf.Infinity;
		public static int M_layerMask = -1;
		public static QueryTriggerInteraction M_queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		internal static Color M_castColor = new Color(1, 0.5f, 0, 1);

		#region BoxCast

		#region Boxcast single
		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return BoxCast(center, halfExtents, direction, out rayInfo, M_orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return BoxCast(center, halfExtents, direction, out rayInfo, orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit rayInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCast(center, halfExtents, direction, out rayInfo, M_orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit rayInfo, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCast(center, halfExtents, direction, out rayInfo, orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit rayInfo, Quaternion orientation, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit rayInfo, Quaternion orientation, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			bool collided = UEPhysics.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				if (collided)
				{
					DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					maxDistance = hitInfo.distance;
				}

				DebugExtensions.DebugBoxCast(center, halfExtents, direction, maxDistance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red),
					orientation, drawDuration, drawType, preview, drawDepth);
			}

			return collided;
		}
		#endregion

		#region Boxcast all
		public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCastAll(center, halfExtents, direction, M_orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCastAll(center, halfExtents, direction, orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit[] hitInfo = UEPhysics.BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = false;
				float maxDistanceRay = 0;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				foreach (RaycastHit hit in hitInfo)
				{
					collided = true;

					if (hit.distance > maxDistanceRay)
						maxDistanceRay = hit.distance;

					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

					DebugExtensions.DebugBox(center + direction * hit.distance, halfExtents, hitColor.Value, orientation, drawDuration, preview, drawDepth);
				}

				DebugExtensions.DebugBoxCast(center, halfExtents, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value,
					orientation, drawDuration, drawType, preview, drawDepth);
			}

			return hitInfo;
		}
		#endregion

		#region Boxcast non alloc
		public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCastNonAlloc(center, halfExtents, direction, results, M_orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			int size = UEPhysics.BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = false;
				float maxDistanceRay = 0;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				for (int i = 0; i < size; i++)
				{
					RaycastHit hit = results[i];
					collided = true;

					if (hit.distance > maxDistanceRay)
						maxDistanceRay = hit.distance;

					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

					DebugExtensions.DebugBox(center + direction * hit.distance, halfExtents, hitColor.Value, orientation, drawDuration, preview, drawDepth);
				}

				DebugExtensions.DebugBoxCast(center, halfExtents, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value,
					orientation, drawDuration, drawType, preview, drawDepth);
			}

			return size;
		}
		#endregion

		#endregion

		#region Capsule Cast

		#region Capsulecast single
		public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return CapsuleCast(point1, point2, radius, direction, out rayInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return CapsuleCast(point1, point2, radius, direction, out rayInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCast(point1, point2, radius, direction, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return CapsuleCast(point1, point2, radius, direction, out rayInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit rayInfo;
			return CapsuleCast(point1, point2, radius, direction, out rayInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			bool collided = UEPhysics.CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				if (collided)
				{
					maxDistance = hitInfo.distance;
					DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
				}

				DebugExtensions.DebugCapsuleCast(point1, point2, direction, maxDistance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red),
					radius, drawDuration, drawType, preview, drawDepth);
			}

			return collided;
		}
		#endregion

		#region Capsulecast all
		public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCastAll(point1, point2, radius, direction, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCastAll(point1, point2, radius, direction, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit[] hitInfo = UEPhysics.CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = false;
				float maxDistanceRay = 0;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				foreach (RaycastHit hit in hitInfo)
				{
					collided = true;

					if (hit.distance > maxDistanceRay)
						maxDistanceRay = hit.distance;

					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DebugCapsule(point1 + direction * hit.distance, point2 + direction * hit.distance, hitColor.Value, radius, true, drawDuration, preview, drawDepth);
				}

				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				DebugExtensions.DebugCapsuleCast(point1, point2, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value,
					radius, drawDuration, drawType, preview, drawDepth);
			}

			return hitInfo;
		}
		#endregion

		#region Capsulecast non alloc
		public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCastNonAlloc(point1, point2, radius, direction, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			int size = UEPhysics.CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = false;
				float maxDistanceRay = 0;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				for (int i = 0; i < size; i++)
				{
					collided = true;

					RaycastHit hit = results[i];

					if (hit.distance > maxDistanceRay)
						maxDistanceRay = hit.distance;

					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DebugCapsule(point1 + direction * hit.distance, point2 + direction * hit.distance, hitColor.Value, radius, true, drawDuration, preview, drawDepth);
				}

				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				DebugExtensions.DebugCapsuleCast(point1, point2, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value,
					radius, drawDuration, drawType, preview, drawDepth);
			}

			return size;
		}
		#endregion

		#endregion

		#region Check Box
		public static bool CheckBox(Vector3 center, Vector3 halfExtents, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return CheckBox(center, halfExtents, M_orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return CheckBox(center, halfExtents, orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return CheckBox(center, halfExtents, orientation, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			bool collided = UEPhysics.CheckBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				DebugExtensions.DebugBox(center, halfExtents, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), orientation, drawDuration, preview, drawDepth);
			}

			return collided;
		}
		#endregion

		#region Check Capsule
		public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return CheckCapsule(start, end, radius, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return CheckCapsule(start, end, radius, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			bool collided = UEPhysics.CheckCapsule(start, end, radius, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				DebugExtensions.DebugCapsule(start, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, false, drawDuration, preview, drawDepth);
			}

			return collided;
		}
		#endregion

		#region Check Sphere
		public static bool CheckSphere(Vector3 position, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return CheckSphere(position, radius, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool CheckSphere(Vector3 position, float radius, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return CheckSphere(position, radius, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool CheckSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			bool collided = UEPhysics.CheckSphere(position, radius, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				DebugExtensions.DebugWireSphere(position, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, preview, drawDepth);
			}

			return collided;
		}
		#endregion

		#region Linecast
		public static bool Linecast(Vector3 start, Vector3 end, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Linecast(start, end, out rayInfo, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Linecast(Vector3 start, Vector3 end, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Linecast(start, end, out rayInfo, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Linecast(Vector3 start, Vector3 end, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Linecast(start, end, out rayInfo, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return Linecast(start, end, out hitInfo, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return Linecast(start, end, out hitInfo, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			bool collided = UEPhysics.Linecast(start, end, out hitInfo, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				if (collided)
				{
					end = hitInfo.point;

					DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
				}

				DebugExtensions.DrawLine(start, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
			}

			return collided;
		}
		#endregion

		#region Overlap Box
		#region OverlapBox alloc
		public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapBox(center, halfExtents, M_orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapBox(center, halfExtents, orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapBox(center, halfExtents, orientation, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			Collider[] colliders = UEPhysics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = colliders.Length > 0;

				DebugExtensions.DebugBox(center, halfExtents, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), orientation, drawDuration, preview, drawDepth);
			}

			return colliders;
		}
		#endregion

		#region OverlapBox non alloc
		public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapBoxNonAlloc(center, halfExtents, results, M_orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapBoxNonAlloc(center, halfExtents, results, orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			int size = UEPhysics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = size > 0;

				DebugExtensions.DebugBox(center, halfExtents, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), orientation, drawDuration, preview, drawDepth);
			}

			return size;
		}
		#endregion
		#endregion

		#region Overlap Capsule
		#region OverlapCapsule alloc
		public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapCapsule(point0, point1, radius, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapCapsule(point0, point1, radius, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			Collider[] colliders = UEPhysics.OverlapCapsule(point0, point1, radius, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = colliders.Length > 0;

				DebugExtensions.DebugCapsule(point0, point1, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, false, drawDuration, preview, drawDepth);
			}

			return colliders;
		}
		#endregion

		#region OverlapCapsule non alloc
		public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapCapsuleNonAlloc(point0, point1, radius, results, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			int size = UEPhysics.OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = size > 0;

				DebugExtensions.DebugCapsule(point0, point1, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, false, drawDuration, preview, drawDepth);
			}

			return size;
		}
		#endregion
		#endregion

		#region Overlap Sphere
		#region OverlapSphere alloc
		public static Collider[] OverlapSphere(Vector3 position, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapSphere(position, radius, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapSphere(position, radius, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			Collider[] colliders = UEPhysics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = colliders.Length > 0;

				DebugExtensions.DebugWireSphere(position, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, preview, drawDepth);
			}

			return colliders;
		}
		#endregion

		#region OverlapSphere non alloc
		public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapSphereNonAlloc(position, radius, results, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return OverlapSphereNonAlloc(position, radius, results, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			int size = UEPhysics.OverlapSphereNonAlloc(position, radius, results, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = size > 0;

				DebugExtensions.DebugWireSphere(position, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, preview, drawDepth);
			}

			return size;
		}
		#endregion
		#endregion

		#region Raycast

		#region Raycast single
		#region Vector3
		public static bool Raycast(Vector3 origin, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Raycast(origin, direction, out rayInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Raycast(origin, direction, out rayInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Raycast(origin, direction, out rayInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Raycast(origin, direction, out rayInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return Raycast(origin, direction, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return Raycast(origin, direction, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return Raycast(origin, direction, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			bool collided = UEPhysics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				Vector3 end = origin + direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				if (collided)
				{
					end = hitInfo.point;

					DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
				}

				DebugExtensions.DrawLine(origin, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
			}

			return collided;
		}
		#endregion

		#region Ray
		public static bool Raycast(Ray ray, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Raycast(ray, out rayInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Ray ray, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Raycast(ray, out rayInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Ray ray, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return Raycast(ray, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Ray ray, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Raycast(ray, out rayInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return Raycast(ray, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit rayInfo;
			return Raycast(ray, out rayInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return Raycast(ray, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			bool collided = UEPhysics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				Vector3 end = ray.origin + ray.direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				if (collided)
				{
					end = hitInfo.point;

					DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
				}

				DebugExtensions.DrawLine(ray.origin, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
			}

			return collided;
		}

		#endregion
		#endregion

		#region Raycast all
		#region Vector3
		public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastAll(origin, direction, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastAll(origin, direction, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastAll(origin, direction, maxDistance, (int)layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit[] raycastInfo = UEPhysics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				Vector3 end = origin + direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
				Vector3 previewOrigin = origin;
				Vector3 sectionOrigin = origin;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				foreach (RaycastHit hit in raycastInfo)
				{
					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

					if ((origin - hit.point).sqrMagnitude > (origin - previewOrigin).sqrMagnitude)
						previewOrigin = hit.point;

					sectionOrigin = hit.point;
				}

				DebugExtensions.DrawLine(previewOrigin, end, noHitColor.Value, drawDuration, preview, drawDepth);
			}

			return raycastInfo;
		}
		#endregion

		#region Ray
		public static RaycastHit[] RaycastAll(Ray ray, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastAll(ray, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastAll(ray, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastAll(ray, maxDistance, (int)layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			RaycastHit[] raycastInfo = UEPhysics.RaycastAll(ray, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				Vector3 end = ray.origin + ray.direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
				Vector3 previewOrigin = ray.origin;
				Vector3 sectionOrigin = ray.origin;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				foreach (RaycastHit hit in raycastInfo)
				{
					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

					if ((ray.origin - hit.point).sqrMagnitude > (ray.origin - previewOrigin).sqrMagnitude)
					{
						previewOrigin = hit.point;
					}

					sectionOrigin = hit.point;
				}

				DebugExtensions.DrawLine(previewOrigin, end, noHitColor.Value, drawDuration, preview, drawDepth);
			}

			return raycastInfo;
		}
		#endregion
		#endregion

		#region Raycast non alloc
		#region Vector3
		public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastNonAlloc(origin, direction, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastNonAlloc(origin, direction, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			int size = UEPhysics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				Vector3 end = origin + direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
				Vector3 previewOrigin = origin;
				Vector3 sectionOrigin = origin;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				for (int i = 0; i < size; i++)
				{
					RaycastHit hit = results[i];
					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

					if ((origin - hit.point).sqrMagnitude > (origin - previewOrigin).sqrMagnitude)
					{
						previewOrigin = hit.point;
					}

					sectionOrigin = hit.point;
				}

				DebugExtensions.DrawLine(previewOrigin, end, noHitColor.Value, drawDuration, preview, drawDepth);
			}

			return size;
		}
		#endregion

		#region Ray
		public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastNonAlloc(ray, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastNonAlloc(ray, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			return RaycastNonAlloc(ray, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
		}

		public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
		{
			int size = UEPhysics.RaycastNonAlloc(ray, results, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				Vector3 end = ray.origin + ray.direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
				Vector3 previewOrigin = ray.origin;
				Vector3 sectionOrigin = ray.origin;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				for (int i = 0; i < size; i++)
				{
					RaycastHit hit = results[i];
					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

					if ((ray.origin - hit.point).sqrMagnitude > (ray.origin - previewOrigin).sqrMagnitude)
					{
						previewOrigin = hit.point;
					}

					sectionOrigin = hit.point;
				}

				DebugExtensions.DrawLine(previewOrigin, end, noHitColor.Value, drawDuration, preview, drawDepth);
			}

			return size;
		}
		#endregion
		#endregion

		#endregion

		#region Sphere Cast
		#region Spherecast single
		#region Vector3
		public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit hitInfo;
			return SphereCast(origin, radius, direction, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit hitInfo;
			return SphereCast(origin, radius, direction, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit hitInfo;
			return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit hitInfo;
			return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCast(origin, radius, direction, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCast(origin, radius, direction, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			bool collided = UEPhysics.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				if (collided)
				{
					maxDistance = hitInfo.distance;
					DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
				}

				DebugExtensions.DebugSphereCast(origin, direction, maxDistance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, drawType, preview, drawDepth);
			}

			return collided;
		}
		#endregion

		#region Ray
		public static bool SphereCast(Ray ray, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit hitInfo;
			return SphereCast(ray, radius, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Ray ray, float radius, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit hitInfo;
			return SphereCast(ray, radius, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCast(ray, radius, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Ray ray, float radius, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit hitInfo;
			return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCast(ray, radius, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Ray ray, float radius, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit hitInfo;
			return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			bool collided = UEPhysics.SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				if (collided)
				{
					maxDistance = hitInfo.distance;
					DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
				}

				DebugExtensions.DebugSphereCast(ray.origin, ray.direction, maxDistance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, drawType, preview, drawDepth);
			}

			return collided;
		}
		#endregion
		#endregion

		#region Spherecast all
		#region Vector3
		public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastAll(origin, radius, direction, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastAll(origin, radius, direction, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastAll(origin, radius, direction, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit[] hitInfo = UEPhysics.SphereCastAll(origin, radius, direction, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = false;
				float maxDistanceRay = 0;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				foreach (RaycastHit hit in hitInfo)
				{
					collided = true;

					if (hit.distance > maxDistanceRay)
						maxDistanceRay = hit.distance;

					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DebugWireSphere(origin + direction * hit.distance, hitColor.Value, radius, drawDuration, preview, drawDepth);
				}

				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				DebugExtensions.DebugSphereCast(origin, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value, radius, drawDuration, drawType, preview, drawDepth);
			}

			return hitInfo;
		}
		#endregion

		#region Ray
		public static RaycastHit[] SphereCastAll(Ray ray, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastAll(ray, radius, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastAll(ray, radius, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastAll(ray, radius, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			RaycastHit[] hitInfo = UEPhysics.SphereCastAll(ray, radius, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = false;
				float maxDistanceRay = 0;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				foreach (RaycastHit hit in hitInfo)
				{
					collided = true;

					if (hit.distance > maxDistanceRay)
						maxDistanceRay = hit.distance;

					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DebugWireSphere(ray.origin + ray.direction * hit.distance, hitColor.Value, radius, drawDuration, preview, drawDepth);
				}

				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				DebugExtensions.DebugSphereCast(ray.origin, ray.direction, maxDistance, collided ? hitColor.Value : noHitColor.Value, radius, drawDuration, drawType, preview, drawDepth);
			}

			return hitInfo;
		}
		#endregion
		#endregion

		#region Spherecast non alloc
		#region Vector3
		public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastNonAlloc(origin, radius, direction, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			int size = UEPhysics.SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = false;
				float maxDistanceRay = 0;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				for (int i = 0; i < size; i++)
				{
					RaycastHit hit = results[i];
					collided = true;

					if (hit.distance > maxDistanceRay)
						maxDistanceRay = hit.distance;

					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DebugWireSphere(origin + direction * hit.distance, hitColor.Value, radius, drawDuration, preview, drawDepth);
				}

				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				DebugExtensions.DebugSphereCast(origin, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value, radius, drawDuration, drawType, preview, drawDepth);
			}

			return size;
		}
		#endregion

		#region Ray
		public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastNonAlloc(ray, radius, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastNonAlloc(ray, radius, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			return SphereCastNonAlloc(ray, radius, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
		}

		public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
		{
			int size = UEPhysics.SphereCastNonAlloc(ray, radius, results, maxDistance, layerMask, queryTriggerInteraction);

			if (preview != PreviewCondition.None)
			{
				bool collided = false;
				float maxDistanceRay = 0;

				if (!hitColor.HasValue)
				{
					hitColor = Color.green;
				}
				if (!noHitColor.HasValue)
				{
					noHitColor = Color.red;
				}
				//hitColor ??= Color.green;
				//noHitColor ??= Color.red;

				for (int i = 0; i < size; i++)
				{
					RaycastHit hit = results[i];
					collided = true;

					if (hit.distance > maxDistanceRay)
						maxDistanceRay = hit.distance;

					DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
					DebugExtensions.DebugWireSphere(ray.origin + ray.direction * hit.distance, hitColor.Value, radius, drawDuration, preview, drawDepth);
				}

				maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

				DebugExtensions.DebugSphereCast(ray.origin, ray.direction, maxDistance, collided ? hitColor.Value : noHitColor.Value, radius, drawDuration, drawType, preview, drawDepth);
			}

			return size;
		}
		#endregion
		#endregion
		#endregion

		#endregion
	}
}
