using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Pogo;
using MelonLoader;
using Inputter;
using System.Reflection;
using static UnityEngine.PlayerLoop.PreUpdate;
using WizardPhysics;
using static Pogo.SlowFaller;
using System.Security.Cryptography;
using WizardUtils;
using static UnityEngine.GraphicsBuffer;
using static PogoTAS.TAS;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Pogo.Levels;
using System.Runtime.ConstrainedExecution;
using MelonLoader.TinyJSON;
using System.Collections;
using Pogo.Levels.Loading;
using Pogo.AssemblyLines;

namespace PogoTAS
{
	enum ExKeyNames
	{
		Padding = KeyName.Balloon,
		Rewind,
		FastForward,
		Record,
		Playback,
		GotoStartOfRecording,
		FakePause,
		ManualShiftDownForDynamicObjects,
		ManualShiftUpForDynamicObjects,
		AnchorView
	}

	public class TAS : MelonMod
	{
		public static TAS Instance = null;
		int currentMoveTick = 0;
		public List<PlayerMoveTickInfo> playerMoveTickInfos = new List<PlayerMoveTickInfo>();
		public int[] levelLoadTick = new int[100];
		float originalFixedTimeStep = 0f;
		bool recording = true;
		bool manualPaused = false;
		bool wasPaused = false;
		bool isInGame = false;

		PlayerController player = null;
		PogoGameManager lastManager = null;
		LevelDescriptor startingLevel = null;
		List<LevelSceneLoader> pogoLevelManagerCurrentLevelSceneLoaders = null;

		public class DebugPoint
		{
			public float time;
			public Vector3 point;
			public DebugPoint(float t, Vector3 p)
			{
				time = t;
				point = p;
			}
		}

		List<DebugPoint> debugPoints = new List<DebugPoint>();


		public class PlayerMoveTickInfo
		{
			public PlayerMoveTickInfo(PlayerController copyPlayer)
			{
				Copy(copyPlayer);
			}

			public void Copy(PlayerController copyPlayer)
			{
				eyeAngle = copyPlayer.EyeAngles;
				pitchFrac = copyPlayer.PitchFrac;

				physicsPosition = copyPlayer.PhysicsPosition;
				velocity = copyPlayer.Velocity;
				physicsRotation = copyPlayer.PhysicsRotation;
				//copyPlayer.CollisionGroup.CollisionOrbs
			}

			public void Apply(PlayerController wishPlayer, bool fullRestore, bool skipEyeAngles = false)
			{
				wishPlayer.PitchFrac = pitchFrac;
				if (!skipEyeAngles)
				{
					Instance.SetEyeAngles(wishPlayer, eyeAngle);
				}

				if (fullRestore)
				{
					wishPlayer.PhysicsPosition = physicsPosition;
					if (!skipEyeAngles)
					{
						wishPlayer.PhysicsRotation = physicsRotation;
					}
					wishPlayer.Disjoint();
					wishPlayer.Velocity = velocity;
				}
			}

			public Vector3 eyeAngle;
			public float pitchFrac;
			public Vector3 physicsPosition;
			public Vector3 velocity;
			public Quaternion physicsRotation;
			//public Vector3 lastPhysicsPosition;
		}


		public override void OnInitializeMelon()
		{
			base.OnInitializeMelon();
		}

		FieldInfo movingPlatformControllerAnimatorField = null;
		FieldInfo assemblyLineEntryControllerAnimatorField = null;
		public override void OnLateInitializeMelon()
		{
			Instance = this;

			base.OnLateInitializeMelon();

			TypeInfo movingPlatformControllerTypeInfo = typeof(MovingPlatformController).GetTypeInfo();
			movingPlatformControllerAnimatorField = movingPlatformControllerTypeInfo.GetDeclaredField("animator");

			TypeInfo assemblyLineEntryControllerTypeInfo = typeof(AssemblyLineEntryController).GetTypeInfo();
			assemblyLineEntryControllerAnimatorField = assemblyLineEntryControllerTypeInfo.GetDeclaredField("animator");

			for (int i = 0; i < levelLoadTick.Length; i++)
			{
				levelLoadTick[i] = 0;
			}
		}

		InputBinder lastBinder = null;
		int lastKeyCount = 0;
		int newKeyCount = 0;
		public void InitInputs()
		{
			if (lastBinder == null && InputManager.self != null && InputManager.self.Binder != null)
			{
				TypeInfo inputBinderTypeInfo = typeof(InputBinder).GetTypeInfo();

				//FieldInfo keycount = inputBinderTypeInfo.GetDeclaredField("keycount"); no worky idk
				//FieldInfo keycount = inputBinderTypeInfo.GetField("keycount",BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.Instance); no worky idk

				Keyboard current = Keyboard.current;
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.Rewind] = new KeyboardButton(current, Key.A);
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.FastForward] = new KeyboardButton(current, Key.D);
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.Record] = new KeyboardButton(current, Key.UpArrow);
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.Playback] = new KeyboardButton(current, Key.DownArrow);
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.GotoStartOfRecording] = new KeyboardButton(current, Key.M);
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.FakePause] = new KeyboardButton(current, Key.W);
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.ManualShiftDownForDynamicObjects] = new KeyboardButton(current, Key.LeftArrow);
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.ManualShiftUpForDynamicObjects] = new KeyboardButton(current, Key.RightArrow);
				InputManager.self.Binder.Keys[(KeyName)ExKeyNames.AnchorView] = new KeyboardButton(current, Key.F);

				lastKeyCount = (Enum.GetNames(typeof(KeyName)).Length - 1);
				newKeyCount = (Enum.GetNames(typeof(KeyName)).Length - 1) + (Enum.GetNames(typeof(ExKeyNames)).Length - 1);
				//keycount.SetValue((Enum.GetNames(typeof(KeyName)).Length - 1) + (Enum.GetNames(typeof(ExKeyNames)).Length - 1), InputManager.self.Binder);

				lastBinder = InputManager.self.Binder;
			}
		}

		public abstract class OverrideUnityEventBase : UnityEngine.Events.UnityEventBase
		{
			public void OverrideRemoveListener(object target, MethodInfo method)
			{
				RemoveListener(target, method);
			}

			protected override MethodInfo FindMethod_Impl(string name, Type targetObjType)
			{
				return UnityEngine.Events.UnityEventBase.GetValidMethodInfo(targetObjType, name, new Type[0]);
			}
			internal abstract object GetDelegate(object target, MethodInfo theFunction);

		}


		Action ActionPhysicsUpdate = null;
		void CallOriginalPhysicsUpdate()
		{
			if (ActionPhysicsUpdate != null)
			{
				ActionPhysicsUpdate();
			}
		}

		Action<float> ApplyForces = null;
		FieldInfo internalEyeAngles = null;
		FieldInfo currentState = null;
		bool playerFunctionsInitialised = false;
		public void InitPlayerFunctions(PlayerController player)
		{
			TypeInfo playerControllerTypeInfo = typeof(PlayerController).GetTypeInfo();

			MethodInfo PhysicsUpdateMethod = playerControllerTypeInfo.GetDeclaredMethod("PhysicsUpdate");
			ActionPhysicsUpdate = (Action)PhysicsUpdateMethod.CreateDelegate(typeof(Action), player);

			MethodInfo ApplyForcesMethod = playerControllerTypeInfo.GetDeclaredMethod("ApplyForces");
			ApplyForces = (Action<float>)ApplyForcesMethod.CreateDelegate(typeof(Action<float>), player);

			internalEyeAngles = playerControllerTypeInfo.GetDeclaredField("internalEyeAngles");
			currentState = playerControllerTypeInfo.GetDeclaredField("currentState");

			lastManager.TimeManager.OnPhysicsUpdate.RemoveAllListeners();
			lastManager.TimeManager.OnPhysicsUpdate.AddListener(this.PrePhysicsUpdate);
			lastManager.TimeManager.OnPhysicsUpdate.AddListener(CallOriginalPhysicsUpdate);
			lastManager.TimeManager.OnPhysicsUpdate.AddListener(this.LatePhysicsUpdate);

			playerFunctionsInitialised = true;
		}


		public void SetEyeAngles(PlayerController wishPlayer, Vector3 newAngles)
		{
			if (wishPlayer == null || internalEyeAngles == null)
				return;

			internalEyeAngles.SetValue(wishPlayer, newAngles);
		}

		void RemoveOldKeys(ref UnityEngine.Object[] objArray, ref Dictionary<int, bool> dict)
		{
			List<int> keysToRemove = new List<int>(dict.Count);
			if (dict.Count > 0)
			{
				foreach (KeyValuePair<int, bool> keyval in dict)
				{
					bool containsKey = false;
					for (int i = 0; i < objArray.Length; i++)
					{
						if (objArray[i] == null)
							continue;
						MonoBehaviour mover = objArray[i] as MonoBehaviour;
						GameObject moverGameObject = mover.gameObject;

						if (moverGameObject.GetHashCode() == keyval.Key)
						{
							containsKey = true;
							break;
						}
					}
					if (!containsKey)
					{
						keysToRemove.Add(keyval.Key);
					}
				}
			}
			if (keysToRemove.Count > 0)
			{
				foreach (int key in keysToRemove)
				{
					dict.Remove(key);
				}
			}

		}

		Dictionary<int, bool> movingPlatformControllersTimeShiftedAlready = new Dictionary<int, bool>();
		void OffsetMovingPlatformControllers(int tickDelta, bool dontOverShift)
		{
			if (allMovingPlatformControllers != null && allMovingPlatformControllers.Length > 0)
			{
				for (int i = 0; i < allMovingPlatformControllers.Length; i++)
				{
					if (allMovingPlatformControllers[i] == null)
						continue;
					MovingPlatformController mover = allMovingPlatformControllers[i] as MovingPlatformController;
					GameObject moverGameObject = mover.gameObject;

					if (dontOverShift)
					{
						if (movingPlatformControllersTimeShiftedAlready.ContainsKey(moverGameObject.GetHashCode()) && movingPlatformControllersTimeShiftedAlready[moverGameObject.GetHashCode()] == true)
						{
							continue;
						}
						movingPlatformControllersTimeShiftedAlready[moverGameObject.GetHashCode()] = true;
						debugPoints.Add(new DebugPoint(5f, moverGameObject.transform.position));
					}

					Animator animator = (Animator)movingPlatformControllerAnimatorField.GetValue(mover);

					if (animator != null)
					{
						if (tickDelta != 0)
						{
							animator.Update(originalFixedTimeStep * (float)tickDelta);
						}
					}

				}

				if (dontOverShift)
				{
					RemoveOldKeys(ref allMovingPlatformControllers, ref movingPlatformControllersTimeShiftedAlready);
				}
			}
			else
			{
				movingPlatformControllersTimeShiftedAlready.Clear();
			}
		}
		Dictionary<int, bool> assemblyLineEntryControllersTimeShiftedAlready = new Dictionary<int, bool>();
		void OffsetAssemblyLineEntryControllers(int tickDelta, bool dontOverShift)
		{
			if (allAssemblyLineEntryControllers != null && allAssemblyLineEntryControllers.Length > 0)
			{
				for (int i = 0; i < allAssemblyLineEntryControllers.Length; i++)
				{
					if (allAssemblyLineEntryControllers[i] == null)
						continue;
					AssemblyLineEntryController mover = allAssemblyLineEntryControllers[i] as AssemblyLineEntryController;
					GameObject moverGameObject = mover.gameObject;

					if (dontOverShift)
					{
						if (assemblyLineEntryControllersTimeShiftedAlready.ContainsKey(moverGameObject.GetHashCode()) && assemblyLineEntryControllersTimeShiftedAlready[moverGameObject.GetHashCode()] == true)
						{
							continue;
						}
						assemblyLineEntryControllersTimeShiftedAlready[moverGameObject.GetHashCode()] = true;
						debugPoints.Add(new DebugPoint(5f, moverGameObject.transform.position));
					}

					Animator animator = (Animator)assemblyLineEntryControllerAnimatorField.GetValue(mover);

					if (animator != null)
					{
						if (tickDelta != 0)
						{
							animator.Update(originalFixedTimeStep * (float)tickDelta);
						}
					}

				}

				if (dontOverShift)
				{
					RemoveOldKeys(ref allAssemblyLineEntryControllers, ref assemblyLineEntryControllersTimeShiftedAlready);
				}
			}
			else
			{
				assemblyLineEntryControllersTimeShiftedAlready.Clear();
			}
		}

		Dictionary<int, bool> rotatersTimeShiftedAlready = new Dictionary<int, bool>();
		void OffsetRotaters(int tickDelta, bool dontOverShift)
		{
			if (allRotaters != null && allRotaters.Length > 0)
			{
				for (int i = 0; i < allRotaters.Length; i++)
				{
					if (allRotaters[i] == null)
						continue;
					Rotater rotater = allRotaters[i] as Rotater;
					GameObject rotaterGameObject = rotater.gameObject;

					if (dontOverShift)
					{
						if (rotatersTimeShiftedAlready.ContainsKey(rotaterGameObject.GetHashCode()) && rotatersTimeShiftedAlready[rotaterGameObject.GetHashCode()] == true)
						{
							continue;
						}
						rotatersTimeShiftedAlready[rotaterGameObject.GetHashCode()] = true;
						debugPoints.Add(new DebugPoint(5f, rotaterGameObject.transform.position));
					}

					if (rotater.Rotating)
					{

						if (tickDelta != 0)
						{
							rotater.transform.localRotation *= Quaternion.AngleAxis(rotater.RotationSpeed * (originalFixedTimeStep * (float)tickDelta), rotater.localAxis);
						}
					}
				}

				if (dontOverShift)
				{
					RemoveOldKeys(ref allRotaters, ref rotatersTimeShiftedAlready);
				}
			}
			else
			{
				rotatersTimeShiftedAlready.Clear();
			}
		}

		void OffsetDynamicObjects(int tickDelta, bool dontOverShift)
		{
			OffsetMovingPlatformControllers(tickDelta, dontOverShift);
			OffsetRotaters(tickDelta, dontOverShift);
			OffsetAssemblyLineEntryControllers(tickDelta, dontOverShift);
		}

		public void RestoreToTick(int tick)
		{
			if (player == null)
				return;

			if (tick < 0)
			{
				tick = 0;
			}

			if (tick >= playerMoveTickInfos.Count)
			{
				tick = playerMoveTickInfos.Count - 1;
			}

			if (tick < 0 || tick >= playerMoveTickInfos.Count)
				return;

			int tickDelta = tick - currentMoveTick;

			OffsetDynamicObjects(tickDelta, false);	

			currentMoveTick = tick;
			playerMoveTickInfos[tick].Apply(player, true);
		}

		float physicsTimeAccume = 0f;
		public void UnInitPlayerFunctions()
		{
			ActionPhysicsUpdate = null;
			ApplyForces = null;

			playerFunctionsInitialised = false;
			physicsTimeAccume = 0f;
		}

		bool didPrePhysicsUpdate = false;
		Vector3 prePos = Vector3.zero;
		public void PrePhysicsUpdate()
		{

			if (player != null && isInGame)
			{
				if (recording)
				{
					if (currentMoveTick >= playerMoveTickInfos.Count)
					{
						currentMoveTick = playerMoveTickInfos.Count;
						playerMoveTickInfos.Add(new PlayerMoveTickInfo(player));
					}
					else
					{
						playerMoveTickInfos[currentMoveTick].Copy(player);
					}
				}
				else
				{
					if (currentMoveTick >= 0)
					{

						PlayerMoveTickInfo tickInfo = playerMoveTickInfos[currentMoveTick];

						if ((tickInfo.velocity - player.Velocity).magnitude > 0.01f || (tickInfo.physicsPosition - player.PhysicsPosition).magnitude > 0.01f )
						{
							tickInfo.Apply(player, true);
							MelonLogger.Msg($"fudged tick: {currentMoveTick}");
						}

						if (currentMoveTick < playerMoveTickInfos.Count)
						{
							tickInfo.Apply(player, false);
						}
						else
						{
							currentMoveTick = playerMoveTickInfos.Count - 1;
							tickInfo.Apply(player, false);
						}
					}
				}
				prePos = player.PhysicsPosition;

				didPrePhysicsUpdate = true;
			}

		}

		Vector3 posDelta = Vector3.zero;
		public void LatePhysicsUpdate()
		{
			if (didPrePhysicsUpdate)
			{
				if (player != null && isInGame)
				{
					currentMoveTick += 1;
					
					if (!recording)
					{
						if (currentMoveTick >= playerMoveTickInfos.Count)
						{
							currentMoveTick = playerMoveTickInfos.Count - 1;
							manualPaused = true;
						}
					}

					posDelta = player.PhysicsPosition - prePos;
				}

				didPrePhysicsUpdate = false;
			}
		}

		public void ManualPlayerPhysicsUpdate()
		{
			if (player != null)
			{
				player.Disjoint();
				ApplyForces(originalFixedTimeStep);

				player.CollisionGroup.RotateTo(player.DesiredModelRotation);
				player.CollisionGroup.Move(player.Velocity * originalFixedTimeStep);
			}
		}

		public static void PrintMonoBehaviours(GameObject gameObject)
		{
			MonoBehaviour[] components = gameObject.GetComponentsInChildren<MonoBehaviour>();

			foreach (MonoBehaviour component in components)
			{
				if (component != null)
				{
					MelonLogger.Msg(component.GetType().FullName);
				}
			}

			components = gameObject.GetComponentsInParent<MonoBehaviour>();

			foreach (MonoBehaviour component in components)
			{
				if (component != null)
				{
					MelonLogger.Msg(component.GetType().FullName);
				}
			}
		}
		public static MonoBehaviour[] FindAllGenericInstances(Type genericTypeDefinition)
		{
			return UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
				FindObjectsSortMode.None)
				.Where(m =>
				{
					if (m == null)
						return false;

					Type type = m.GetType();

					while (type != null)
					{
						if (type.IsGenericType &&
							type.GetGenericTypeDefinition() == genericTypeDefinition)
						{
							return true;
						}

						type = type.BaseType;
					}

					return false;
				})
				.ToArray();
		}
		float secondTimer = 0f;
		int rotationWaypointerCount = 0;
		int rotaterCount = 0;
		int vectorPhysicsTimeWaypointerCount = 0;
		int floatPhysicsTimeWaypointerCount = 0;
		int doublePhysicsTimeWaypointerCount = 0;
		int linearMoverCount = 0;
		int orbSafeMoverCount = 0;
		int positionWaypointerCount = 0;
		int orbSafePositionWaypointerCount = 0;
		int movingPlatformControllerCount = 0;
		int waypointerCount = 0;
		int localPositionWaypointerCount = 0;
		int assemblyLineEntryControllerCount = 0;
		UnityEngine.Object[] allVectorPhysicsTimeWaypointers = null;
		UnityEngine.Object[] allPositionWaypointers = null;
		UnityEngine.Object[] allMovingPlatformControllers = null;
		UnityEngine.Object[] allLocalPositionWaypointer = null;
		UnityEngine.Object[] allRotaters = null;
		UnityEngine.Object[] allAssemblyLineEntryControllers = null;
		UnityEngine.Object[] allOrbSafeMovers = null;
		void FindAllStuff(bool force = false)
		{

			secondTimer += Time.unscaledDeltaTime;

			if (secondTimer >= 1f || force)
			{
				secondTimer = 0f;
				UnityEngine.Object[] allRotaterSpeedWaypointers = GameObject.FindObjectsOfType(typeof(RotaterSpeedWaypointer));
				if (allRotaterSpeedWaypointers != null)
				{
					rotationWaypointerCount = allRotaterSpeedWaypointers.Length;
				}

				allRotaters = GameObject.FindObjectsOfType(typeof(Rotater));
				if (allRotaters != null)
				{
					rotaterCount = allRotaters.Length;
				}

				allVectorPhysicsTimeWaypointers = GameObject.FindObjectsOfType(typeof(PhysicsTimeWaypointer<Vector3>));
				if (allVectorPhysicsTimeWaypointers != null)
				{
					vectorPhysicsTimeWaypointerCount = allVectorPhysicsTimeWaypointers.Length;
				}

				UnityEngine.Object[] allFloatPhysicsTimeWaypointers = FindAllGenericInstances(typeof(PhysicsTimeWaypointer<>));//Resources.FindObjectsOfTypeAll<PhysicsTimeWaypointer<>>();
				if (allFloatPhysicsTimeWaypointers != null)
				{
					floatPhysicsTimeWaypointerCount = allFloatPhysicsTimeWaypointers.Length;
				}

				UnityEngine.Object[] allDoublePhysicsTimeWaypointers = GameObject.FindObjectsOfType(typeof(PhysicsTimeWaypointer<double>));
				if (allDoublePhysicsTimeWaypointers != null)
				{
					doublePhysicsTimeWaypointerCount = allDoublePhysicsTimeWaypointers.Length;
				}

				UnityEngine.Object[] allLinearMovers = GameObject.FindObjectsOfType(typeof(LinearMover));
				if (allLinearMovers != null)
				{
					linearMoverCount = allLinearMovers.Length;
				}

				allOrbSafeMovers = GameObject.FindObjectsOfType(typeof(OrbSafeMover));
				if (allOrbSafeMovers != null)
				{
				/*	if (orbSafeMoverCount == 0 && allOrbSafeMovers.Length > 0)
					{
						for (int i = 0; i < allOrbSafeMovers.Length; i++)
						{
							if (allOrbSafeMovers[i] == null)
								continue;
							OrbSafeMover mover = allOrbSafeMovers[i] as OrbSafeMover;
							GameObject moverGameObject = mover.gameObject;

							MelonLogger.Msg($"name: {moverGameObject.name}");
							PrintMonoBehaviours(moverGameObject);
							MelonLogger.Msg("");
						}
					}*/
					orbSafeMoverCount = allOrbSafeMovers.Length;
				}


				allPositionWaypointers = GameObject.FindObjectsOfType(typeof(PositionWaypointer));
				if (allPositionWaypointers != null)
				{
					positionWaypointerCount = allPositionWaypointers.Length;
				}

				UnityEngine.Object[] allOrbSafePositionWaypointers = GameObject.FindObjectsOfType(typeof(OrbSafePositionWaypointer));
				if (allOrbSafePositionWaypointers != null)
				{
					orbSafePositionWaypointerCount = allOrbSafePositionWaypointers.Length;
				}

				allMovingPlatformControllers = GameObject.FindObjectsOfType(typeof(MovingPlatformController));
				if (allMovingPlatformControllers != null)
				{

				/*	if (movingPlatformControllerCount == 0 && allMovingPlatformControllers.Length > 0)
					{
						for (int i = 0; i < allMovingPlatformControllers.Length; i++)
						{
							if (allMovingPlatformControllers[i] == null)
								continue;
							MovingPlatformController mover = allMovingPlatformControllers[i] as MovingPlatformController;
							GameObject moverGameObject = mover.gameObject;

							MelonLogger.Msg($"name: {moverGameObject.name}");
							PrintMonoBehaviours(moverGameObject);
							MelonLogger.Msg("");
						}
					}*/
					movingPlatformControllerCount = allMovingPlatformControllers.Length;
				}
				UnityEngine.Object[] allWaypointers = FindAllGenericInstances(typeof(Waypointer<>));
				if (allWaypointers != null)
				{
					/*if (waypointerCount == 0 && allWaypointers.Length > 0)
					{
						for (int i = 0; i < allWaypointers.Length; i++)
						{
							if (allWaypointers[i] == null)
								continue;
							MonoBehaviour mover = allWaypointers[i] as MonoBehaviour;
							GameObject moverGameObject = mover.gameObject;

							MelonLogger.Msg($"name: {moverGameObject.name}");
							PrintMonoBehaviours(moverGameObject);
							MelonLogger.Msg("");
						}
					}*/
					waypointerCount = allWaypointers.Length;
				}

				//Waypointer


				allLocalPositionWaypointer = GameObject.FindObjectsOfType(typeof(LocalPositionWaypointer));//Resources.FindObjectsOfTypeAll<PhysicsTimeWaypointer<>>();
				if (allLocalPositionWaypointer != null)
				{
					localPositionWaypointerCount = allLocalPositionWaypointer.Length;
				}
				//LocalPositionWaypointer

				allAssemblyLineEntryControllers = GameObject.FindObjectsOfType(typeof(AssemblyLineEntryController));
				if (allAssemblyLineEntryControllers != null)
				{
					assemblyLineEntryControllerCount = allAssemblyLineEntryControllers.Length;
				}
			}
		}

		bool levelFinishedLoading = false;
		public void LevelFinishedLoading()
		{
			levelFinishedLoading = true;
		}
		int loadedLevelIndex = 0;
		void FinishedLoadingLevelIndexer()
		{
			if (PogoGameManager.PogoInstance != null && PogoGameManager.PogoInstance.LevelManager != null && PogoGameManager.PogoInstance.LevelManager.CurrentLevel != null)
			{
				if (PogoGameManager.PogoInstance.LevelManager.CurrentLevel.BuildIndex != loadedLevelIndex && levelFinishedLoading)
				{
					loadedLevelIndex = PogoGameManager.PogoInstance.LevelManager.CurrentLevel.BuildIndex;
					levelFinishedLoading = false;
					//MelonLogger.Msg($"levelLoaded: {loadedLevelIndex}");
				}
			}
			else
			{
				loadedLevelIndex = 0;
				//lastLoadedLevelIndex = 0;
			}
		}
		float lastLevelLoadTickDelta = 0f;
		int lastLoadedLevelIndex = 0;
		public bool[] levelLoaded = new bool[100];
		int lastPogoLevelManagerCurrentLevelSceneLoadersCount = 0;
		void LevelLoadedTickOffset()
		{

			if (lastLoadedLevelIndex != loadedLevelIndex)
			{
				lastLoadedLevelIndex = loadedLevelIndex;
				MelonCoroutines.Start(LevelLoadedTickOffsetRoutine(loadedLevelIndex, recording));				
			}

			if (lastPogoLevelManagerCurrentLevelSceneLoadersCount != pogoLevelManagerCurrentLevelSceneLoaders.Count)
			{
				lastPogoLevelManagerCurrentLevelSceneLoadersCount = pogoLevelManagerCurrentLevelSceneLoaders.Count;
				for (int i = 0; i < lastPogoLevelManagerCurrentLevelSceneLoadersCount; i++)
				{
					if (pogoLevelManagerCurrentLevelSceneLoaders[i] != null)
					{
						if (pogoLevelManagerCurrentLevelSceneLoaders[i].CurrentLoadState == LevelSceneLoader.LoadStates.Loading)
						{
							pogoLevelManagerCurrentLevelSceneLoaders[i].OnIdle.AddListener(SpecificLevelWasLoadedOrUnloaded(pogoLevelManagerCurrentLevelSceneLoaders[i].Level.BuildIndex, true));
						}
						else if (pogoLevelManagerCurrentLevelSceneLoaders[i].CurrentLoadState == LevelSceneLoader.LoadStates.Unloading)
						{
							pogoLevelManagerCurrentLevelSceneLoaders[i].OnIdle.AddListener(SpecificLevelWasLoadedOrUnloaded(pogoLevelManagerCurrentLevelSceneLoaders[i].Level.BuildIndex, false));
						}
					}
				}
			}	
		}
		UnityAction SpecificLevelWasLoadedOrUnloaded(int levelBuildIndex, bool loadingIn)
		{
			Instance.levelLoaded[levelBuildIndex] = loadingIn;
			return null;
		}
		/*
		public IEnumerator OffsetDynamicObjectsOnLevelLoadRoutine(int tickDelta)
		{
			yield return new WaitForEndOfFrame();
			FindAllStuff(true);
			OffsetDynamicObjects(tickDelta, true);
			MelonLogger.Msg("worked?");
		}*/
		public IEnumerator LevelLoadedTickOffsetRoutine(int levelIndex, bool wasRecording)
		{
			yield return new WaitForEndOfFrame();
			if (!wasRecording)
			{
				lastLevelLoadTickDelta = 0f;
				if (levelLoadTick[levelIndex] != 0)
				{
					int levelLoadTickDelta = currentMoveTick - levelLoadTick[levelIndex];
					//levelLoadTickDelta *= 2f;
					lastLevelLoadTickDelta = levelLoadTickDelta;
					FindAllStuff(true);
					OffsetDynamicObjects(levelLoadTickDelta, true);
				}

				MelonLogger.Msg($"levelLoaded: {levelIndex}, tickDelta: {lastLevelLoadTickDelta}, original: {levelLoadTick[levelIndex]}, current: {currentMoveTick}");		
			}
			else
			{
				if (levelLoaded[levelIndex])
				{
					levelLoadTick[levelIndex] = currentMoveTick;
					levelLoaded[levelIndex] = false; // lol
					FindAllStuff(true);
					MelonLogger.Msg($"levelLoaded: {levelIndex}");
				}
				else
				{
					MelonLogger.Msg($"levelUnLoaded: {levelIndex}");
				}
			}
		}

		int manualShiftedAmmount = 0;
		bool viewWasAnchored = false;
		Quaternion originalViewRotation = Quaternion.identity;
		const int maxPredictionTicks = 500;
		const int minPredictionTicks = 100;
		int currentPredTicks = minPredictionTicks;
		Vector3[] predictedPositions = new Vector3[maxPredictionTicks];
		public override void OnLateUpdate()
		{
			base.OnLateUpdate();

			if (originalFixedTimeStep == 0f)
			{
				originalFixedTimeStep = Time.fixedDeltaTime;
			}

			InitInputs();

			if (lastBinder != null && lastManager == null && PogoGameManager.PogoInstance != null && PogoGameManager.PogoInstance.TimeManager != null)
			{
				//if (!initialisedPrePhysics)
				//{
					//PogoGameManager.PogoInstance.TimeManager.OnPhysicsUpdate.AddListener(this.PrePhysicsUpdate);
				//	initialisedPrePhysics = true;
				//}
				//if (PogoGameManager.PogoInstance.CurrentCheckpoint.Id.CheckpointNumber > 0)
				{
					lastManager = PogoGameManager.PogoInstance;
				//	initialisedPrePhysics = false;
				}
			}
			else if (lastManager != null && lastBinder != null)
			{

				for (int i = lastKeyCount; i < newKeyCount; i++)
				{
					KeyName key = (KeyName)i;
					lastBinder.Keys[key].Reset(false);
				}
	
				for (int i = lastKeyCount; i < newKeyCount; i++)
				{
					lastBinder.Keys[(KeyName)i].Check();
				}

				if (player == null && PogoGameManager.PogoInstance != null && PogoGameManager.PogoInstance.Player != null)
				{
					InitPlayerFunctions(PogoGameManager.PogoInstance.Player);
					PogoGameManager.PogoInstance.OnLevelLoaded.RemoveListener(LevelFinishedLoading);
					PogoGameManager.PogoInstance.OnLevelLoaded.AddListener(LevelFinishedLoading);				
					player = PogoGameManager.PogoInstance.Player;
					startingLevel = null;
				}
				else if (player == null && playerFunctionsInitialised)
				{
					UnInitPlayerFunctions();
				}

				FinishedLoadingLevelIndexer();

				isInGame = loadedLevelIndex != 0;
				if (isInGame)
				{
					
					FindAllStuff();

					if (startingLevel == null)
					{
						startingLevel = PogoGameManager.PogoInstance.LevelManager.CurrentLevel;
					}

					if (pogoLevelManagerCurrentLevelSceneLoaders == null)
					{
						TypeInfo pogoLevelManagerTypeInfo = typeof(PogoLevelManager).GetTypeInfo();
						FieldInfo currentLevelSceneLoadersField = pogoLevelManagerTypeInfo.GetDeclaredField("CurrentLevelSceneLoaders");
						pogoLevelManagerCurrentLevelSceneLoaders = (List<LevelSceneLoader>)currentLevelSceneLoadersField.GetValue(PogoGameManager.PogoInstance.LevelManager);
					}

					if (player != null)
					{
						if (!manualPaused && Time.timeScale == 0f)
						{
							manualPaused = true;
						}

						if (InputManager.CheckKeyDown((KeyName)ExKeyNames.Record))
						{
							recording = true;
							manualPaused = true;
						}
						else if (InputManager.CheckKeyDown((KeyName)ExKeyNames.Playback))
						{
							manualPaused = true;
							recording = false;
						}

						if (!recording)
						{
							//currentState.SetValue(player, PlayerStates.Dead);

							if (InputManager.CheckKeyDown((KeyName)ExKeyNames.GotoStartOfRecording))
							{
								PogoGameManager.PogoInstance.LoadLevel(startingLevel);
								RestoreToTick(0);
							}
						}
	
						if (InputManager.CheckKeyDown((KeyName)ExKeyNames.FakePause))
						{
							manualPaused = !manualPaused;

							if (recording && !manualPaused)
							{
								playerMoveTickInfos.RemoveRange(currentMoveTick + 1, (playerMoveTickInfos.Count) - (currentMoveTick + 1));
							}
						}

						LevelLoadedTickOffset();

					/*	if (InputManager.CheckKeyDown((KeyName)ExKeyNames.ManualShiftDownForDynamicObjects))
						{
							OffsetDynamicObjects(-1, false);
							manualShiftedAmmount -= 1;
						}
						else if (InputManager.CheckKeyDown((KeyName)ExKeyNames.ManualShiftUpForDynamicObjects))
						{
							OffsetDynamicObjects(1, false);
							manualShiftedAmmount += 1;
						}*/

						bool tryingToRewind = InputManager.CheckKey((KeyName)ExKeyNames.Rewind);
						bool tryingToFastForward = InputManager.CheckKey((KeyName)ExKeyNames.FastForward);

						if (tryingToRewind || tryingToFastForward)
						{
							manualPaused = true;
						}

						if (InputManager.CheckKey((KeyName)ExKeyNames.AnchorView))
						{
							if (!viewWasAnchored)
							{
								viewWasAnchored = true;
							}
							player.CameraSwivelPoint.transform.rotation = originalViewRotation;
							currentPredTicks = maxPredictionTicks;
						}
						else
						{
							originalViewRotation = player.CameraSwivelPoint.transform.rotation;
							currentPredTicks = minPredictionTicks;
							if (viewWasAnchored)
							{							
								viewWasAnchored = false;
							}
						}

						if (manualPaused)
						{
							if (!wasPaused)
							{
								RestoreToTick(currentMoveTick);				
								wasPaused = true;
							}
							Time.timeScale = 0f;
							if (originalFixedTimeStep > 0f)
							{
								float wantedSpeed = 1f;
								float scaledFixedDeltaTime = originalFixedTimeStep * (1f / (wantedSpeed - Time.timeScale));
								physicsTimeAccume += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
								while (physicsTimeAccume >= scaledFixedDeltaTime)
								{
									physicsTimeAccume -= scaledFixedDeltaTime;
									if (tryingToRewind)
									{
										RestoreToTick(currentMoveTick - 1);
									}
									else if (tryingToFastForward)
									{
										RestoreToTick(currentMoveTick + 1);
									}
									
									PlayerMoveTickInfo tickInfo = playerMoveTickInfos[currentMoveTick];

									PlayerStates originalState = (PlayerStates)currentState.GetValue(player);
									currentState.SetValue(player, PlayerStates.Dead);
									tickInfo.Apply(player, true, true);

									player.PhysicsRotation = player.DesiredModelRotation;

									for (int si = 0; si < currentPredTicks; si++)
									{
										ManualPlayerPhysicsUpdate();
										predictedPositions[si] = player.PhysicsPosition;
									}

									tickInfo.Apply(player, true, true);

									currentState.SetValue(player, originalState);
								}
							}

						}
						else
						{
							Time.timeScale = 1f;

							if (wasPaused)
							{
								if (!recording)
								{
									RestoreToTick(currentMoveTick);
								}
								wasPaused = false;
							}
						}
					}
				}		
			}

			//LatePhysicsUpdate();
		}

		public Material PaintMaterial = null;
		public void CreatePaintMaterial()
		{
			if (!PaintMaterial)
			{
				Shader.WarmupAllShaders();
				Shader shader = Shader.Find("Hidden/Internal-Colored");//Shader.Find("Unlit/Draw2DWDepth");//Shader.Find("ProBuilder/UnlitVertexColor");//Shader.Find("Hidden/Internal-Colored")
				if (shader)
				{
					PaintMaterial = new Material(shader);
					PaintMaterial.hideFlags = HideFlags.HideAndDontSave;
					PaintMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					PaintMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					PaintMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
					PaintMaterial.SetInt("_ZWrite", 1);
					//PaintMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
				}
			}
		}

		public void DrawStuffs(UnityEngine.Object[] objs, int width, int height, Color32 clr, bool text = false)
		{
			if (objs != null && objs.Length > 0)
			{
				for (int i = 0; i < objs.Length; i++)
				{
					if (objs[i] == null)
						continue;
					MonoBehaviour mover = objs[i] as MonoBehaviour;
					GameObject moverGameObject = mover.gameObject;

					Vector3 w2s = Draw.WorldToScreen(moverGameObject.transform.position);
					if (w2s.z > 0f)
					{
						Draw.Rect(w2s.x - width * .5f, w2s.y - height * .5f, width, height, clr);
						if (text)
						{
							GUI.Label(new Rect(w2s.x, w2s.y, 1000, 100), $"name: {moverGameObject.name}", GUI.skin.label);
						}
					}
				}
			}
		}

		public override void OnGUI()
		{
			base.OnGUI();
			if (PogoGameManager.PogoInstance != null)
			{
				//return;

				/*	if (PogoGameManager.PogoInstance.LevelManager != null && PogoGameManager.PogoInstance.LevelManager.CurrentLevel != null)
					{
						GUI.Label(new Rect(100, 40, 1000, 100), $"lastLevelLoadTickDelta: {lastLevelLoadTickDelta}", GUI.skin.label);
						GUI.Label(new Rect(100, 60, 1000, 100), $"loadedLevelIndex: {loadedLevelIndex}", GUI.skin.label);
						GUI.Label(new Rect(100, 80, 1000, 100), $"CurrentLevel.BuildIndex: {PogoGameManager.PogoInstance.LevelManager.CurrentLevel.BuildIndex}", GUI.skin.label);
					}

					if (PogoGameManager.PogoInstance.CurrentCheckpoint != null)
					{
						GUI.Label(new Rect(100, 100, 1000, 100), $"CurrentCheckpoint.Id: {PogoGameManager.PogoInstance.CurrentCheckpoint.Id}", GUI.skin.label);
					}*/
				string tasMode = recording ? "RecordingMode" : "PlayBackMode";
				GUI.Label(new Rect(100, 80, 1000, 100), $"currentMode: {tasMode}", GUI.skin.label);
				GUI.Label(new Rect(100, 100, 1000, 100), $"manualPaused: {manualPaused}", GUI.skin.label);

				GUI.Label(new Rect(100, 140, 1000, 100), $"currentMoveTick: {currentMoveTick}", GUI.skin.label);
				GUI.Label(new Rect(100, 160, 1000, 100), $"position: {prePos}", GUI.skin.label);
				if (player != null)
				{
					GUI.Label(new Rect(100, 180, 1000, 100), $"velocity: {player.Velocity.magnitude}", GUI.skin.label);
				}
				//GUI.Label(new Rect(100, 200, 1000, 100), $"posDelta: {posDelta}", GUI.skin.label);

				GUI.Label(new Rect(100, 220, 1000, 100), $"controls: ", GUI.skin.label);
				GUI.Label(new Rect(100, 240, 1000, 100), $" UpArrow = RecordingMode", GUI.skin.label);
				GUI.Label(new Rect(100, 260, 1000, 100), $" DownArrow = PlayBackMode", GUI.skin.label);
				GUI.Label(new Rect(100, 280, 1000, 100), $" W = Play/Pause", GUI.skin.label);
				GUI.Label(new Rect(100, 300, 1000, 100), $" A = BackwardsInTime", GUI.skin.label);
				GUI.Label(new Rect(100, 320, 1000, 100), $" D = ForwardsInTime", GUI.skin.label);
				GUI.Label(new Rect(100, 340, 1000, 100), $" M = (When in PlayBackMode) goto Beginning", GUI.skin.label);
				GUI.Label(new Rect(100, 360, 1000, 100), $" F = (When in RecordingMode and Paused) do long Prediction", GUI.skin.label);
				/*	GUI.Label(new Rect(100, 220, 1000, 100), $"rotationWaypointerCount: {rotationWaypointerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 240, 1000, 100), $"rotaterCount: {rotaterCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 260, 1000, 100), $"vectorPhysicsTimeWaypointerCount: {vectorPhysicsTimeWaypointerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 280, 1000, 100), $"floatPhysicsTimeWaypointerCount: {floatPhysicsTimeWaypointerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 300, 1000, 100), $"doublePhysicsTimeWaypointerCount: {doublePhysicsTimeWaypointerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 320, 1000, 100), $"linearMoverCount: {linearMoverCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 340, 1000, 100), $"orbSafeMoverCount: {orbSafeMoverCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 360, 1000, 100), $"positionWaypointerCount: {positionWaypointerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 380, 1000, 100), $"orbSafePositionWaypointerCount: {orbSafePositionWaypointerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 400, 1000, 100), $"movingPlatformControllerCount: {movingPlatformControllerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 420, 1000, 100), $"waypointerCount: {waypointerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 440, 1000, 100), $"localPositionWaypointerCount: {localPositionWaypointerCount}", GUI.skin.label);
					GUI.Label(new Rect(100, 460, 1000, 100), $"manualShiftedAmmount: {manualShiftedAmmount}", GUI.skin.label);*/

				if (Event.current.type != EventType.Repaint)
				{
					return;
				}
				

				CreatePaintMaterial();
				if (PaintMaterial != null)
				{
					PaintMaterial.SetPass(0);

					//DrawStuffs(allOrbSafeMovers, new Color32(255, 255, 255, 255));
				/*	DrawStuffs(allVectorPhysicsTimeWaypointers, 20, 20, new Color32(255, 255, 255, 255));

					DrawStuffs(allPositionWaypointers, 10, 10, new Color32(255, 0, 0, 255));

					DrawStuffs(allMovingPlatformControllers, 30, 30, new Color32(0, 255, 0, 255));

					DrawStuffs(allOrbSafeMovers, 40, 40, new Color32(0, 0, 255, 255));
					DrawStuffs(allLocalPositionWaypointer, 50, 50, new Color32(255, 0, 255, 255));
					DrawStuffs(allRotaters, 60, 60, new Color32(255, 255, 0, 255));*/

					for (int si = 1; si < currentPredTicks; si++)
					{
						Vector3 w2s = Draw.WorldToScreen(predictedPositions[si]);
						Vector3 oldW2s = Draw.WorldToScreen(predictedPositions[si - 1]);
						if (w2s.z > 0f && oldW2s.z > 0f)
						{
							Draw.Line(w2s.x, w2s.y, oldW2s.x, oldW2s.y, new Color32(255, 255, 255, 255));
							//Draw.FilledRect(w2s.x - 20, w2s.y - 20, 40, 40, new Color32(255, 0, 0, 255));
						}
						
					}

				/*	if (debugPoints != null && debugPoints.Count > 0)
					{
						for (int i = 0; i < debugPoints.Count; i++)
						{
							debugPoints[i].time -= Time.unscaledDeltaTime;
							if (debugPoints[i].time < 0f)
							{
								debugPoints.RemoveAt(i);
								i--;
								continue;
							}

							Vector3 w2s = Draw.WorldToScreen(debugPoints[i].point);
							if (w2s.z > 0f)
							{
								Draw.FilledRect(w2s.x - 20, w2s.y - 20, 40, 40, new Color32(255, 0, 0, 255));
							}
						}
					}*/

				}

			}

		}
	}
}
