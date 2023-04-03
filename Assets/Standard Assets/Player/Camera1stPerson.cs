using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player_Controller
{
    public class Camera1stPerson : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        protected PlayerInputListener inputListener;

        [Header("Dependencies")]
        [SerializeField]
        protected Transform cam;
        [SerializeField]
        protected Transform head;

        [Header("Parameters")]
        [SerializeField]
        protected float mouseSensitivity = 600f;

        private float xRotation = 0f;

        protected void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        protected void Update()
        {
            this.cam.position = this.head.position;

            Vector2 input = this.inputListener.input.Default.MouseDelta.ReadValue<Vector2>() * this.mouseSensitivity;

            xRotation -= input.y;
            xRotation = Mathf.Clamp(xRotation, -89f, 89f);
            this.cam.transform.localRotation = Quaternion.Euler(xRotation, this.cam.localRotation.eulerAngles.y + input.x, 0);
        }
    }
}