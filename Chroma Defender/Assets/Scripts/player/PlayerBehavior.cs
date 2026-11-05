using UnityEngine;

public class PlayerBehavior : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;

    private Rigidbody2D rb;
    private IsGroundedChecker IsGroundedChecker;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        IsGroundedChecker = GetComponent<IsGroundedChecker>();
    }

    private void Start()
    {
        GameManager.Instance.InputManager.OnJump += handleJump;
    }

    private void Update()
    {
        float moveDirection = GameManager.Instance.InputManager.Movement;
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    private void handleJump()
    {
        if(IsGroundedChecker.IsGrounded() == false)
        {
            return;
        }

        rb.linearVelocity += Vector2.up * jumpForce;
    }
}