using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class ThirdPersonController : MonoBehaviour
	{
		[SerializeField] private Transform playerCameraRoot;
		[SerializeField] private Transform cameraLookAtPoint;
		[SerializeField] private GameObject playerFollowCamera;
		[SerializeField] private float cameraDistance = 10f;

		float cameraRotationAngleX;
		private float lookAroundRatio;
		private Vector3 lookAroundPosition;

		[SerializeField] Transform wheelBL;
		[SerializeField] Transform wheelBR;
		[SerializeField] Transform wheelFL;
		[SerializeField] Transform wheelFR;
		
		[SerializeField] private Light BrakeLightLeft;
		[SerializeField] private Light BrakeLightRight;

		[Tooltip("How fast the car can move forward")]
		public float MaximumForwardSpeed = 50.0f;
		[Tooltip("How fast the car can move in reverse")]
		public float MaximumReverseSpeed = 20.0f;
		[Tooltip("How fast the car moves left and right")]
		public float SteeringPower = 5.0f;
		[Tooltip("How fast the car accelerates")]
		public float Acceleration = 1.0f;
		[Tooltip("How fast the car slows down without hitting the pedal (lower is faster)")]
		public float SlowDownFactor = 0.95f;
		[Tooltip("How fast the car brakes (lower is faster)")]
		public float BrakeFactor = 0.5f;

		Quaternion currentCarOrientation;

		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 2.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 5.335f;
		[Tooltip("How fast the character turns to face movement direction")]
		[Range(0.0f, 0.3f)]
		public float RotationSmoothTime = 0.12f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.50f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.28f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 70.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -30.0f;
		[Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
		public float CameraAngleOverride = 0.0f;
		[Tooltip("For locking the camera position on all axis")]
		public bool LockCameraPosition = false;

		// player
		private float _speed;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

        public float Speed { get => _speed; set => _speed = value; }

        private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
		}

		private void Update()
		{
			JumpAndGravity();
			GroundedCheck();
			Move();
		}
		private void FixedUpdate()
		{
			UpdateCarSpeed();
		}

		private void Move()
		{
			transform.rotation = currentCarOrientation;
			// if _input.move.x > 0 then move right
			// if _input.move.x < 0 then move left
			transform.Rotate(Vector3.up * SteeringPower * _speed * _input.move.x * Time.deltaTime, Space.Self);
			currentCarOrientation = transform.rotation;
			Vector3 targetDirection = transform.forward;

			// move the player
			_controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

			RotateWithTerrain();
		}

		private void UpdateCarSpeed()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			int accelerationFactor = _input.sprint ? 2 : 1;

			// accelerate
			if (_input.move.y > 0)
			{
				_speed += Acceleration * accelerationFactor;
				if (_speed > MaximumForwardSpeed)
				{
					_speed = MaximumForwardSpeed;
				}
			}

			// brake / reverse
			if (_input.move.y < 0)
			{
				BrakeLightLeft.enabled = true;
				BrakeLightRight.enabled = true;
				if (_speed > 0.01)
				{
					_speed *= BrakeFactor;
				}
				else
				{
					_speed -= Acceleration;
					if (_speed < -MaximumReverseSpeed)
					{
						_speed = -MaximumReverseSpeed;
					}
				}
			}
			else
			{
				BrakeLightLeft.enabled = false;
				BrakeLightRight.enabled = false;
			}

			// slow down
			if (_input.move.y == 0)
			{
				_speed *= SlowDownFactor;
			}
		}
		private void UpdateLookAroundPosition()
		{
			cameraRotationAngleX += 0.005f * _input.look.x * Time.deltaTime;
			lookAroundPosition = new Vector3((float)(cameraLookAtPoint.position.x + cameraDistance * Math.Sin(cameraRotationAngleX)), 1000, (float)(cameraLookAtPoint.position.z + cameraDistance * Math.Cos(cameraRotationAngleX)));
			lookAroundPosition = new Vector3(lookAroundPosition.x, Terrain.activeTerrain.SampleHeight(lookAroundPosition) + 1.1f, lookAroundPosition.z);
		}

		private void LateUpdate()
		{
			if (_input.look.x != 0)
			{
				lookAroundRatio += 0.5f * Time.deltaTime;
				if (lookAroundRatio > 1)
				{
					lookAroundRatio = 1;
				}
			}
			else if (_input.move.y > 0)
			{
				lookAroundRatio -= 0.5f * Time.deltaTime;
				if (lookAroundRatio < 0)
				{
					lookAroundRatio = 0;
				}
			}

			UpdateLookAroundPosition();

			playerFollowCamera.transform.position = Vector3.Lerp(playerCameraRoot.position, lookAroundPosition, lookAroundRatio);
			playerFollowCamera.transform.LookAt(cameraLookAtPoint);

			Debug.Log(lookAroundRatio);
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void RotateWithTerrain()
		{
			// Get the height of the terrain at all 4 wheels
			float YWheelBL = Terrain.activeTerrain.SampleHeight(wheelBL.position);
			float YWheelBR = Terrain.activeTerrain.SampleHeight(wheelBR.position);
			float YWheelFL = Terrain.activeTerrain.SampleHeight(wheelFL.position);
			float YWheelFR = Terrain.activeTerrain.SampleHeight(wheelFR.position);

			// the wheels rest on the highest terrain.
			float leftRightHeightDifference = Math.Max(YWheelBR, YWheelFR) - Math.Max(YWheelBL, YWheelFL);
			double leftRightAngle = Math.Atan2(leftRightHeightDifference, 1.2452069f);

			float frontBackHeightDifference = Math.Max(YWheelBL, YWheelBR) - Math.Max(YWheelFL, YWheelFR);
			double frontBackAngle = Math.Atan2(frontBackHeightDifference, 2.175764f);

			transform.Rotate((float)(frontBackAngle * 180 / Math.PI), 0f, (float)(leftRightAngle * 180 / Math.PI), Space.Self);
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;
			
			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}