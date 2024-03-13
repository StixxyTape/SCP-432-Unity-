using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class PlayerInputScript : MonoBehaviour
{
    [SerializeField] private GameObject closeVision;
    [SerializeField] private GameObject farVision;

    [SerializeField] Sprite idleBob;
    [SerializeField] Sprite flashlightBob;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;

    private PlayerInput playerInputMap;
    private InputAction basicMove;
    private InputAction flashlight;
    private Rigidbody2D rb;

    private Vector3 mousePos3;
    private Vector2 moveInput;

    public bool flashlightEnabled;

    private void Awake()
    {
        playerInputMap = gameObject.GetComponent<PlayerInput>();
        rb = gameObject.GetComponent<Rigidbody2D>();

        basicMove = playerInputMap.actions["BasicMove"];
        flashlight = playerInputMap.actions["Flashlight"];

        flashlightEnabled = false;
    }
    private void Update()
    {
        mousePos3 = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        PlayerMovement();
        FollowMouse();
        ToggleFlashlight();
    }

    // This method handles player movement
    private void PlayerMovement()
    {
        moveInput = basicMove.ReadValue<Vector2>();
        moveInput.Normalize();
        rb.velocity = moveInput * moveSpeed;
    }

    // This method handles making the player rotate to face the mouse
    private void FollowMouse()
    {
        Vector2 mousePos2  = new Vector2( mousePos3.x - transform.position.x, mousePos3.y - transform.position.y);

        Quaternion targetRotation = Quaternion.LookRotation(transform.forward, mousePos2);
        Quaternion rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed);
        rb.MoveRotation(rotation);
    }

    private void ToggleFlashlight()
    {
        if (flashlight.triggered)
        {
            Collider2D[] objectsInFlashlightRange = Physics2D.OverlapCircleAll(transform.position, 3f, LayerMask.GetMask("Light"));

            if (!flashlightEnabled)
            {
                closeVision.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity *= 4;
                farVision.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity *= 4;
                gameObject.GetComponent<SpriteRenderer>().sprite = flashlightBob;

                flashlightEnabled = true;

                foreach (Collider2D light in objectsInFlashlightRange)
                {
                    UnityEngine.Rendering.Universal.Light2D intensity = light.GetComponent<UnityEngine.Rendering.Universal.Light2D>();

                    intensity.intensity = 1.5f;
                }
            }
            else if (flashlightEnabled)
            {
                closeVision.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity /= 4;
                farVision.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity /= 4;
                gameObject.GetComponent<SpriteRenderer>().sprite = idleBob;

                flashlightEnabled = false;

                foreach (Collider2D light in objectsInFlashlightRange)
                {
                    UnityEngine.Rendering.Universal.Light2D intensity = light.GetComponent<UnityEngine.Rendering.Universal.Light2D>();

                    intensity.intensity = 6f;
                }
            }
        }
    }
}
