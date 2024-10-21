using UnityEngine;
using UnityEngine.InputSystem;
namespace CharacterSystem
{
	public class InputHandler : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool crouch;
		public bool dash;
		[Header("Movement Settings")]
		public bool analogMovement;
		public bool interact;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;
		public void OnMove(InputValue value)
		{

            move = value.Get<Vector2>();
        }

		public void OnLook(InputValue value)
		{
			if (cursorInputForLook)
			{
				look = value.Get<Vector2>();
			}
		}

		public void OnJump(InputValue value)
		{
			jump = value.isPressed;
		}

		public void OnSprint(InputValue value)
		{
			sprint = value.isPressed;
		}

		public void OnCrouch(InputValue value)
		{
			crouch = value.isPressed;
		}

		public void OnDash(InputValue value)
		{
			dash = value.isPressed;
		}
		public void OnInteract(InputValue value)
		{
			interact = true;
		}
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}

	}
}