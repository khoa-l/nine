using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Player : MonoBehaviour
{
    /*
     * Variables controlling jump. Height in meters? and time in 1/10th of seconds?
     */
    [SerializeField] private float JumpHeight = 3;
    [SerializeField] private float TimeToJumpApex = 1;
    [SerializeField] private float MoveSpeed = 4;

    /*
     * Acceleration value for damping movement between current and target
     * We want the airborne time to be faster than the grounded acceleration damping
     * for more airborne control
     */
    [SerializeField] private float AccelerationTimeAirborne = 0.05f;
    [SerializeField] private float AccelerationTimeGrounded = 0.1f;
    // [SerializeField] private float DecelerationTimeAirborne = 0.2f;
    // [SerializeField] private float DecelerationTimeGrounded = 0.1f;
    //
    
    
    private Vector3 _velocity;
    private Vector3 _oldVelocity;
    
    // Empty variable used in the Mathf.SmoothDamp of horizontal velocity
    private float _velocityXSmoothing;
 
    private float _gravity => -(2 * JumpHeight) / Mathf.Pow(TimeToJumpApex, 2);
    private float _jumpVelocity => 2 * JumpHeight / TimeToJumpApex;

    private float _jumpTimer;
    private float _maxHeightReached = Mathf.NegativeInfinity;
    private float _startHeight = Mathf.NegativeInfinity;
    
    private bool _reachedApex = true;
    private bool _isGrounded;

    private PlayerController _controller;
    
    // Start is called before the first frame update
    void Start()
    {
        _controller = GetComponent<PlayerController>();

    }
    
    private void Jump()
    {
        _jumpTimer = 0;
        _maxHeightReached = Mathf.NegativeInfinity;
        _velocity.y = _jumpVelocity;
        _startHeight = transform.position.y;
        _reachedApex = false;
    }


    // Update is called once per frame
    void Update()
    {

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (_isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
        
        if (!_isGrounded && !_reachedApex)
        {
            _jumpTimer += Time.deltaTime;
        }
        
        if (!_reachedApex && _maxHeightReached > transform.position.y)
        {
            float delta = _maxHeightReached - _startHeight;
            float error = JumpHeight - delta;
            Debug.Log($"jump result: start:{_startHeight:F4}, end:{_maxHeightReached:F4}, delta:{delta:F4}, error:{error:F4}, time:{_jumpTimer:F4}, gravity:{_gravity:F4}, jumpForce:{_jumpVelocity:F4}");
            _reachedApex = true;
        }
        _maxHeightReached = Mathf.Max(transform.position.y, _maxHeightReached);

        _oldVelocity = _velocity;
        _velocity.y += _gravity * Time.deltaTime;
        Vector3 deltaPosition = (_oldVelocity + _velocity) * 0.5f * Time.deltaTime;
        _controller.Move(deltaPosition);
        
        
        float targetVelocityX = input.x * MoveSpeed;

        // if (targetVelocityX == 0)
        // {
        //     _velocity.x = Mathf.SmoothDamp(_velocity.x, targetVelocityX, ref _velocityXSmoothing,
        //         _controller.Collisions.below ? DecelerationTimeGrounded : DecelerationTimeAirborne);
        // }
        
        
        Debug.Log($"$Target Velocity X: {targetVelocityX}");
        _velocity.x = Mathf.SmoothDamp(_velocity.x, targetVelocityX, ref _velocityXSmoothing,
            _controller.Collisions.below ? AccelerationTimeGrounded : AccelerationTimeAirborne);
        // _velocity.y += _gravity * Time.deltaTime;
        // _controller.Move(_velocity * Time.deltaTime);

        _isGrounded = _controller.Collisions.below;
        if (_isGrounded || _controller.Collisions.above)
        {
            _velocity.y = 0;
        }
    }
}
