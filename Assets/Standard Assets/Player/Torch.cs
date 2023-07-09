using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Torch : MonoBehaviour
{
    [SerializeField]
    private Transform torch;
    [SerializeField]
    private PlayerInputListener playerInputListener;

    private void Start()
    {
        this.playerInputListener.input.Default.Torch.performed += this.OnTorchButtonPressed;
    }

    private void OnTorchButtonPressed(InputAction.CallbackContext context)
    {
        if (this.torch == null)
            return;

        this.torch.gameObject.SetActive(!this.torch.gameObject.activeSelf);
    }
}
