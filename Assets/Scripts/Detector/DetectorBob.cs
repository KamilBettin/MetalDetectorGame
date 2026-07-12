using UnityEngine;
using UnityEngine.InputSystem;

public class DetectorBob : MonoBehaviour
{
    public float bobSpeed = 7f;
    public float bobAmount = 0.035f;
    public float swayAmount = 0.025f;

    private Vector3 startLocalPosition;
    private float timer;

    private void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        bool isMoving = GameUIState.CanProcessGameplayInput
            && Keyboard.current != null
            && (Keyboard.current.wKey.isPressed
                || Keyboard.current.aKey.isPressed
                || Keyboard.current.sKey.isPressed
                || Keyboard.current.dKey.isPressed);

        if (isMoving)
        {
            timer += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(timer) * bobAmount;
            float sway = Mathf.Cos(timer * 0.5f) * swayAmount;
            transform.localPosition = startLocalPosition + new Vector3(sway, bob, 0f);
        }
        else
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, startLocalPosition, Time.deltaTime * 8f);
        }
    }
}
