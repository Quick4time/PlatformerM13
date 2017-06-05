using UnityEngine;
using System.Collections;


public class Controller2D : RaycastController
{
    public CollisionInfo collisions;

    float maxClimbAngle = 75.0f; // Максимальный угол подьема по поверхности
    float maxDescendAngle = 75.0f; // Максимальный угол спуска с поверхности

    public override void Start() // при override в Controller'e он вызывает и метод virtual старт в RaycastController 
    {
        base.Start();
    }

    public void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.velocityOld = velocity;

        if(velocity.y < 0)
        {
            DescendSlope(ref velocity);
        }

        if (velocity.x != 0)
        {
            HorizontalCollisions(ref velocity); // если персонаж движется по оси x то обновляем ref velocity в HorizontalCollision
        }
        if (velocity.y != 0)
        {
            VerticalCollisions(ref velocity); // если персонаж движется по оси y то обновляем ref velocity в VerticalCollision
        }

        transform.Translate(velocity);
    }

    void HorizontalCollisions(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.green);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); // узнаем угол наклона поверхности

                if (i == 0 && slopeAngle <= maxClimbAngle) // проверяем угол поверхности и максимальный угол на который мы можем подняться.
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)// если мы подымаемся на новый угол т.е slopeAngle не slopeAngleOld
                    {
                        distanceToSlopeStart = hit.distance - skinWidth; // дистанция начала подьема будет равняться дистанцией касания луча - луч который находиться в коллайдере
                        velocity.x -= distanceToSlopeStart * directionX; // перемещение на подьем будет -= дистанцию начала подьема * направление по оси X
                    }
                    print(slopeAngle);
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX; // тут наоборот
                }
                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope) // решение проблемы при взбирании на плоскость, когда находился предмет на ней и игрок начинал взбираться на него, даже если его угол был больше maxClimbAngle
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }  
            }
        }
    }

    void VerticalCollisions(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.blue);

            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope) // Решение проблемы когда collision abowe (т.е столкновение с обьектом происходило сверху и мы пытались забраться по плоскости при этом нас bounce'ло).
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
        if (collisions.climbingSlope) // Решение проблемы когда менялся угол подьема происходило замерание кадра.
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void ClimbSlope(ref Vector3 velocity, float slopeAngle) // Вычесляем при каком угле поверхности мы можем забраться на нее
    {
        float moveDistance = Mathf.Abs(velocity.x); // Abs(округляет число после запятой до 5 чисел).
        float climbVelosityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (velocity.y <= climbVelosityY) // Решение проблемы с прыжком при взбирании на поверхность
        {
            velocity.y = climbVelosityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendSlope(ref Vector3 velocity) // Вычесляем при каком угле поверхности мы можем спускаемся с нее
    {
        float directionX = Mathf.Sign(velocity.x);
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;// Будут проблемы взгляни на эту строку.
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelosityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelosityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAngleOld; // slopeAngleOld = это угол при преведущем кадре.
        public Vector3 velocityOld;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }

}

