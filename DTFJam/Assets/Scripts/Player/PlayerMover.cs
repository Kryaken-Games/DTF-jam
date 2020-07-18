﻿using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Invector.vCharacterController;

public class PlayerMover : MonoBehaviour {
	public Action<bool> onChangeControls;   // 0 - keyboard, 1 - gamepad

	[Header("Values")] [Space]
	[NonSerialized] public float dashForceMultiplier = 1.0f;
	[SerializeField] float dashForce = 10.0f;


	[Header("Mouse pointer")][Space]
	[SerializeField] /*[GameObjectLayer]*/ int mouseRaycastLayer;
	RaycastHit[] mouseRaycastHits = new RaycastHit[5];

	[Header("Attack values")]
	[Space]
	[SerializeField] DealDamageOnTriggerEnter swordAttackBox;
	bool isCurrentlyAttack = false;

	[Header("Refs")][Space]
	[SerializeField] Transform mouseRaycastTransform;
	[SerializeField] Camera mainCamera;
	[SerializeField] Health health;

	[Header("This Refs")][Space]
	[SerializeField] vThirdPersonController cc;
	[SerializeField] Animator anim;
	[SerializeField] Rigidbody rb;

	bool isUseGamepadControl;
	bool isGamepadLookInput;
	Vector3 moveInput;
	Vector3 mousePos;
	Vector3 lookInput;
	bool isAttackMelee;
	bool isDash;
	bool isCurrentlyDashing = false;

	float defaultRunSpeed;

	void Awake() {
		mouseRaycastLayer = 1 << mouseRaycastLayer;

		defaultRunSpeed = cc.strafeSpeed.runningSpeed;
	}

	void Start() {
		cc.Init();
		cc.rotateTarget = mouseRaycastTransform;
	}

	void FixedUpdate() {
		if (!isCurrentlyDashing) {
			cc.UpdateMotor();               // updates the ThirdPersonMotor methods
			cc.ControlLocomotionType();     // handle the controller locomotion type and movespeed
		}

		cc.ControlRotationType();       // handle the controller rotation type
	}

	void Update() {
		ProcessInput();

		if (!isCurrentlyDashing) {
			cc.UpdateAnimator();            // updates the Animator Parameters
		}
	}

	void OnAnimatorMove() {
		cc.ControlAnimatorRootMotion(); // handle root motion animations 
	}

	public void SetRageBuff(float multiplier) {
		anim.speed = multiplier;
		cc.strafeSpeed.runningSpeed = defaultRunSpeed * multiplier;
	}

	void ProcessInput() {
		cc.input.x = moveInput.x;
		cc.input.z = moveInput.y;

		cc.UpdateMoveDirection(mainCamera.transform);
		if (isUseGamepadControl) {
			mouseRaycastTransform.position = transform.position + new Vector3(lookInput.x, 0, lookInput.y);
		}
		else {
			Ray ray = mainCamera.ScreenPointToRay(mousePos);
			if (Physics.Raycast(ray, out RaycastHit hit, 100, mouseRaycastLayer)) {
				mouseRaycastTransform.position = hit.point;
			}
		}
	}

	#region Animators callback
	public void EnableAttackCollider() {
		swordAttackBox.AttackStart();
	}

	public void DisableAttackCollider() {
		swordAttackBox.AttackEnd();
	}

	public void AttackStart() {
		isCurrentlyAttack = true;
		swordAttackBox.CheckHit();
	}

	public void AttackEnd() {
		isCurrentlyAttack = false;
	}

	public void DashStart() {
		cc.input.x = 0;
		cc.input.z = 0;
		cc.UpdateMoveDirection(mainCamera.transform);
		cc.UpdateMotor();               // updates the ThirdPersonMotor methods
		cc.ControlLocomotionType();     // handle the controller locomotion type and movespeed
		cc.ControlRotationType();       // handle the controller rotation type

		Vector3 dashDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;

		var right = mainCamera.transform.right;
		right.y = 0;
		var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
		dashDirection = (dashDirection.x * right) + (dashDirection.z * forward);

		rb.velocity = dashDirection.normalized * dashForce;

		health.isCanTakeDamage = false;
		isCurrentlyDashing = true;
	}

	public void DashEnd() {
		health.isCanTakeDamage = true;
		rb.velocity = Vector3.zero;
		isCurrentlyDashing = false;

	}
	#endregion

	#region Input handling
	public void OnMove(InputAction.CallbackContext context) {
		CheckIsUseGamepad(context.control.device);

		if (!GameManager.Instance.isPlaying)
			return;

		moveInput = context.ReadValue<Vector2>();

		if (isUseGamepadControl && !isGamepadLookInput)
			lookInput = moveInput;
	}

	public void OnMousePos(InputAction.CallbackContext context) {
		CheckIsUseGamepad(context.control.device);
		isGamepadLookInput = false;

		if (!GameManager.Instance.isPlaying)
			return;

		mousePos = context.ReadValue<Vector2>();
	}

	public void OnLook(InputAction.CallbackContext context) {
		CheckIsUseGamepad(context.control.device);
		isGamepadLookInput = true;

		if (!GameManager.Instance.isPlaying)
			return;

		lookInput = context.ReadValue<Vector2>();
	}

	public void OnAttackMelee(InputAction.CallbackContext context) {
		CheckIsUseGamepad(context.control.device);

		if (!GameManager.Instance.isPlaying || isCurrentlyAttack)
			return;

		isAttackMelee = context.ReadValueAsButton();

		if (!isCurrentlyAttack && isAttackMelee && context.phase == InputActionPhase.Started) {
			anim.SetTrigger("IsAttack");
		}
	}

	public void OnDash(InputAction.CallbackContext context) {
		CheckIsUseGamepad(context.control.device);

		if (!GameManager.Instance.isPlaying || isCurrentlyDashing || isCurrentlyAttack || moveInput.sqrMagnitude <= 0.01f)
			return;

		isDash = context.ReadValueAsButton();

		if (!isCurrentlyDashing && isDash && context.phase == InputActionPhase.Started && GameManager.Instance.player.TryDash()) {
			anim.SetTrigger("IsDash");
		}
	}

	void CheckIsUseGamepad(InputDevice device) {
		bool isGamepadInput = device is Gamepad;
		if (isGamepadInput != isUseGamepadControl) {
			isUseGamepadControl = isGamepadInput;
			onChangeControls?.Invoke(isUseGamepadControl);
		}
	}
	#endregion
}
