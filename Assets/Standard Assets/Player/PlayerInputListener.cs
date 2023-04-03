using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputListener : MonoBehaviour
{
    public PlayerInput input;

    private void Awake()
    {
        this.input = new PlayerInput();
    }

    private void OnEnable()
    {
        this.input.Enable();
    }

    private void OnDisable()
    {
        this.input.Disable();
    }
}
