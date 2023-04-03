using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player_Controller
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        protected PlayerInputListener inputListener;

        [Header("Dependencies")]
        [SerializeField]
        protected Transform visuals;
        [SerializeField]
        protected Animator animator;
        [SerializeField]
        protected Transform cam;

        [Header("Parameters")]
        public bool canMove = true;
        public bool canRun = true;
        [SerializeField]
        protected float runningspeed = 10f;
        [SerializeField]
        protected float walkingspeed = 3f;
        [SerializeField]
        protected LayerMask mask;

        protected CharacterController controller;
        protected Vector3 velocity;
        protected float velocityY;
        protected bool hasJumped = false;
        protected bool shift = false;
        
        protected bool IsGrounded
        {
            get;
            set;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            bool isGrounded = Physics.CheckSphere(this.transform.position - Vector3.down * 0.05f, this.controller.radius - 0.05f, this.mask);

            if (this.canRun && isGrounded && !this.hasJumped)
            {
                this.hasJumped = true;
            }
        }

        public void OnToggleRunning(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                this.shift = true;
            }

            if (context.canceled)
            {
                this.shift = false;
            }
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            this.BindEvents();

            this.controller = this.GetComponent<CharacterController>();
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            Vector3 velocity = this.controller.velocity;
            velocity.y = 0;

            this.animator.SetFloat("VelocitySQR", velocity.sqrMagnitude);
            this.animator.SetBool("InAir", !this.IsGrounded);
        }

        protected virtual void FixedUpdate()
        {
            if (!this.controller.enabled)
                return;

            this.IsGrounded = Physics.CheckSphere(this.transform.position - Vector3.down * 0.05f, this.controller.radius - 0.05f, this.mask);

            this.UpdateMovement(this.cam.forward);
            this.UpdateJump();
            this.Accept();
        }

        protected void UpdateMovement(Vector3 forward)
        {
            Vector2 input = Vector2.zero;
            Vector3 v;

            if (this.canMove)
                input = this.inputListener.input.Default.Move.ReadValue<Vector2>();

            if (input.x != 0 || input.y != 0)
            {
                forward.y = 0;
                Vector3 right = new Vector3(forward.z, 0, -forward.x);

                Vector3 direction = (input.y * forward + input.x * right).normalized;

                if (this.canRun && shift)
                    v = direction * this.runningspeed * Time.fixedDeltaTime;
                else
                    v = direction * this.walkingspeed * Time.fixedDeltaTime;


                this.visuals.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
            else
            {
                v = Vector3.zero;
            }

            this.velocity.x = v.x;
            this.velocity.z = v.z;
        }

        protected void UpdateJump()
        {
            if (!this.IsGrounded)
            {
                this.velocityY -= 9.81f * 4 * Time.fixedDeltaTime;
            }
            else
            {
                this.velocityY = -2; //So it pushes him to the ground and stops him from floating in the air.

                if (hasJumped)
                {
                    this.velocityY += 20f;
                    this.IsGrounded = false;
                }
            }

            this.velocity.y = this.velocityY * Time.fixedDeltaTime;
        }

        protected void Accept()
        {
            if (this.velocity != Vector3.zero)
                this.controller.Move(this.velocity);

            this.hasJumped = false;
        }

        protected void BindEvents()
        {
            this.inputListener.input.Default.Jump.performed += c => OnJump(c);
            this.inputListener.input.Default.ToggleRun.started += c => OnToggleRunning(c);
            this.inputListener.input.Default.ToggleRun.performed += c => OnToggleRunning(c);
            this.inputListener.input.Default.ToggleRun.canceled += c => OnToggleRunning(c);
        }
    }
}