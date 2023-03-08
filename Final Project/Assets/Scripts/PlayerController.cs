using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

// Require the BoxCollider2D on the player
// Prevent BoxCollider2D from being removed
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    // _variableName = private
    // VariableName = public / serialized
    // variableName = local
    
    [SerializeField] private LayerMask CollisionMask;
    
    // Ray cast from a distance inside the player ie skinWidth
    // This is done so when we are flush with a surface we can still cast rays
    private const float skinWidth = .015f;

    // Maximum angle of slope that the player can climbe
    [SerializeField] private float MaxClimbAngle = 80;

    // Determines the number of rays cast in horizontal directions
    [SerializeField] private float HorizontalRayCount = 4;
    
    // Determines the number of rays cast in vertical directions
    [SerializeField] private float VerticalRayCount = 4;

    private float _horizontalRaySpacing;
    private float _verticalRaySpacing;
    
    private BoxCollider2D _collider;
    private RaycastOrigins _raycastOrigins;

    public CollisionInfo Collisions;

    void Start()
    {
        _collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing();

    }
    
    public void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        Collisions.Reset();
        
        // Only check horizontal collisions when moving
        if (velocity.x != 0)
        {
            HorizontalCollisions(ref velocity);
        }
    
        // Only check vertical collisions when jumping or falling
        if (velocity.y != 0)
        {
            VerticalCollisions(ref velocity);
        }
        
        // Apply movement velocities
        transform.Translate(velocity); 
    }
    
    // Checking horizontal collisions
    void HorizontalCollisions(ref Vector3 velocity)
    {
        // -1 is moving left, 1 is moving right
        float directionX = Mathf.Sign(velocity.x);
        
        // Accounting for skinWidth in the length of a ray
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        // Casting and checking every horizontal ray
        for (int i = 0; i < HorizontalRayCount; i++)
        {
            // Change the origin of the ray depending on direction of movement, ternary operator
            Vector2 rayOrigin = (directionX == -1) ? _raycastOrigins.bottomLeft : _raycastOrigins.bottomRight;
            
            // Account for the number of the ray / which ray it is
            rayOrigin += Vector2.up * (_horizontalRaySpacing * i);
            /*
            3 ◄──┬─────┐
            2 ◄├┼│     │
            1 ◄├┼│     │
            0 ◄──┴─────┘
            Rays count from bottom to top
            */

            // Send the raycast out
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, CollisionMask);
            
            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);
            
            // Check the hit for the specific raycast, boolean
            if (hit)
            {
                // Get the angle of the hit if there's an upcoming slope
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                
                // i == 0 means the very bottom ray ie what the player is currently standing on
                // Check for climbing a slope
                if (i == 0 && slopeAngle <= MaxClimbAngle)
                {
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != Collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX;
                }
                
                // Only check these specific horizontal velocities when not climbing slope
                if (!Collisions.climbingSlope || slopeAngle > MaxClimbAngle)
                {
                    
                    velocity.x = Mathf.Min(Mathf.Abs(velocity.x), (hit.distance - skinWidth)) * directionX;
                    rayLength = Mathf.Min(Mathf.Abs(velocity.x) + skinWidth, hit.distance);

                    if (Collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan(Collisions.slopeAngle * Mathf.Deg2Rad * Mathf.Abs(velocity.x));
                    }

                    Collisions.left = directionX == -1;
                    Collisions.right = directionX == 1;
                }
            }
        }
    }

    // Checking veritcal collisions
    void VerticalCollisions(ref Vector3 velocity)
    {
        // -1 is moving down, 1 is moving up
        float directionY = Mathf.Sign(velocity.y);
        
        // Accounting for skinWidth in length of ray
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        // Casting and checking every vertical ray
        for (int i = 0; i < VerticalRayCount; i++)
        {
            // Ray cast starting either on the bottomLeft corner if moving down or topLeft corner if moving up.
            Vector2 rayOrigin = (directionY == -1) ? _raycastOrigins.bottomLeft : _raycastOrigins.topLeft;
            
            // Account for number of ray / which ray it is
            rayOrigin += Vector2.right * (_verticalRaySpacing * i + velocity.x);
            /*
            0 1 2 3
            ▲ ▲ ▲ ▲ - Rays count from left to right
            │ │ │ │
            ├─┴─┴─┤
            │     │
            │     │
            └─────┘
             */

            // Cast the specific ray
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, CollisionMask);
            
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);
            
            // Check hit for the raycast, boolean
            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (Collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(Collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                Collisions.below = directionY == -1;
                Collisions.above = directionY == 1;
            }

            if (Collisions.climbingSlope)
            {
                float directionX = Mathf.Sign(velocity.x);
                rayLength = Mathf.Abs(velocity.x + skinWidth);
                Vector2 rayOrigin2 = (directionX == -1) ? _raycastOrigins.bottomLeft : _raycastOrigins.bottomRight;
                RaycastHit2D hit2 = Physics2D.Raycast(rayOrigin2, Vector2.right * directionX, rayLength, CollisionMask);

                if (hit2)
                {
                    float slopeAngle = Vector2.Angle(hit2.normal, Vector2.up);
                    if (slopeAngle != Collisions.slopeAngle)
                    {
                        velocity.x = (hit2.distance - skinWidth) * directionX;
                        Collisions.slopeAngle = slopeAngle;
                    }
                }
            }
        }
    }
    
    void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (velocity.y <= climbVelocityY)
        {
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            Collisions.below = true;
            Collisions.climbingSlope = true;
            Collisions.slopeAngle = slopeAngle;
        }
            
    }
    
    void UpdateRaycastOrigins()
    {
        Bounds bounds = _collider.bounds;
        bounds.Expand( (skinWidth * -2));

        _raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        _raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        _raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        _raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void CalculateRaySpacing()
    {
        Bounds bounds = _collider.bounds;
        bounds.Expand( (skinWidth * -2));

        HorizontalRayCount = Mathf.Clamp(HorizontalRayCount, 2, int.MaxValue);
        VerticalRayCount = Mathf.Clamp(VerticalRayCount, 2, int.MaxValue);

        _horizontalRaySpacing = bounds.size.y / (HorizontalRayCount - 1);
        _verticalRaySpacing = bounds.size.x / (VerticalRayCount - 1);
    }

    // Struct for different starting points of raycast origins
    struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    // Public struct encapsulation for movement & collision information
    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;

        public float slopeAngle, slopeAngleOld;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}

