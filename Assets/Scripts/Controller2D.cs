using UnityEngine;
using System.Collections;


public class Controller2D : RaycastController
{
    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;

    [SerializeField]
    float maxSlopeAngle = 60f; // Максимальный угол подьема по поверхности

    public override void Start() // при override в Controller'e он вызывает и метод virtual старт в RaycastController 
    {
        base.Start();
        collisions.faceDir = 1;// wallSliding
    }

    public void Move(Vector2 deltaAmount, bool standingOnPlatform)
    {
        Move(deltaAmount, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector2 deltaAmount, Vector2 input, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.deltaAmountOld = deltaAmount;
        playerInput = input;

        if (deltaAmount.y < 0)
        {
            DescendSlope(ref deltaAmount);
        }

        if (deltaAmount.x != 0) // wallSliding
        {
            collisions.faceDir = (int)Mathf.Sign(deltaAmount.x);// wallSliding
        }

        //if (deltaAmount.x != 0) // из за wallSliding
        //{
        HorizontalCollisions(ref deltaAmount); // если персонаж движется по оси x то обновляем ref deltaAmount в HorizontalCollision
        //}
        if (deltaAmount.y != 0)
        {
            VerticalCollisions(ref deltaAmount); // если персонаж движется по оси y то обновляем ref deltaAmount в VerticalCollision
        }

        transform.Translate(deltaAmount);

        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    void HorizontalCollisions(ref Vector2 deltaAmount)
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(deltaAmount.x) + skinWidth;

        if (Mathf.Abs(deltaAmount.x) < skinWidth)// wallSliding
        {
            rayLength = 2 * skinWidth;// wallSliding
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.green);

            if (hit)
            {
                if (hit.distance == 0)
                {
                    continue;
                }

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); // узнаем угол наклона поверхности

                if (i == 0 && slopeAngle <= maxSlopeAngle) // проверяем угол поверхности и максимальный угол на который мы можем подняться.
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        deltaAmount = collisions.deltaAmountOld;
                    }
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)// если мы подымаемся на новый угол т.е slopeAngle не slopeAngleOld
                    {
                        distanceToSlopeStart = hit.distance - skinWidth; // дистанция начала подьема будет равняться дистанцией касания луча - луч который находиться в коллайдере
                        deltaAmount.x -= distanceToSlopeStart * directionX; // перемещение на подьем будет -= дистанцию начала подьема * направление по оси X
                    }
                    print(slopeAngle);
                    ClimbSlope(ref deltaAmount, slopeAngle, hit.normal);
                    deltaAmount.x += distanceToSlopeStart * directionX; // тут наоборот
                }
                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    deltaAmount.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope) // решение проблемы при взбирании на плоскость, когда находился предмет на ней и игрок начинал взбираться на него, даже если его угол был больше maxClimbAngle
                    {
                        deltaAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(deltaAmount.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    void VerticalCollisions(ref Vector2 deltaAmount)
    {
        float directionY = Mathf.Sign(deltaAmount.y);
        float rayLength = Mathf.Abs(deltaAmount.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + deltaAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.blue);

            if (hit)
            {
                if (hit.collider.CompareTag("Through") || hit.distance == 0) // функция реализующая подьем на платформу через нее
                {
                    if (directionY == 1)
                    {
                        continue;
                    }
                    if (collisions.fallingThroughPlatform)
                    {
                        continue;
                    }
                    if (playerInput.y == -1) // Реализум падение при нажатии Vertical - 1 // Создать Input для игрока
                    {
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", 1f); // Заменить на Courutine
                        continue;
                    }
                }
                deltaAmount.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope) // Решение проблемы когда collision abowe (т.е столкновение с обьектом происходило сверху и мы пытались забраться по плоскости при этом нас bounce'ло).
                {
                    deltaAmount.x = deltaAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(deltaAmount.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
        if (collisions.climbingSlope) // Решение проблемы когда менялся угол подьема происходило замерание кадра.
        {
            float directionX = Mathf.Sign(deltaAmount.x);
            rayLength = Mathf.Abs(deltaAmount.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * deltaAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    deltaAmount.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    void ClimbSlope(ref Vector2 deltaAmount, float slopeAngle, Vector2 slopeNormal) // Вычесляем при каком угле поверхности мы можем забраться на нее
    {
        float moveDistance = Mathf.Abs(deltaAmount.x); // Abs(округляет число после запятой до 5 чисел).
        float climbVelosityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (deltaAmount.y <= climbVelosityY) // Решение проблемы с прыжком при взбирании на поверхность
        {
            deltaAmount.y = climbVelosityY;
            deltaAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(deltaAmount.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = slopeNormal;
        }
    }

    void DescendSlope(ref Vector2 deltaAmount) // Вычесляем при каком угле поверхности мы можем спускаемся с нее
    {
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(deltaAmount.y) + skinWidth, collisionMask); // эти две строки служат для испускания луча который будет определять в какйю сторону наклонена поверхность. 
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(deltaAmount.y) + skinWidth, collisionMask); //
        if (maxSlopeHitLeft ^ maxSlopeHitRight) // Бинарные операторы ^ являются предопределенными для целых типов и bool. Для целых типов оператор ^ выполняет побитовую операцию исключающего OR его операндов. Для операндов bool оператор ^ выполняет операцию логического исключающего OR операндов, то есть результатом будет являться значение true только в том случае, если ровно один из его операндов имеет значение true.
        // Исправляем ошибку когда при малом угле поверхности наш обьект потряхивало из-за неправильной коллизии 
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref deltaAmount); // если меняем местами строки работает некорректно.
            SlideDownMaxSlope(maxSlopeHitRight, ref deltaAmount);// в связи с чем можно сделать вывод что применяеться к строке в какой есть булин который true в операнде.
        }

        if (!collisions.slidingDownMaxSlope)
        {
            float directionX = Mathf.Sign(deltaAmount.x);
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;// Будут проблемы взгляни на эту строку.
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (Mathf.Sign(hit.normal.x) == directionX)
                    {
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(deltaAmount.x))
                        {
                            float moveDistance = Mathf.Abs(deltaAmount.x);
                            float descendVelosityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            deltaAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(deltaAmount.x);
                            deltaAmount.y -= descendVelosityY;

                            collisions.slopeAngle = slopeAngle;
                            collisions.descendingSlope = true;
                            collisions.below = true;
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 deltaAmount) // Метод для скольжения с наклонной поверхности
    {
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle > maxSlopeAngle)
            {
                deltaAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(deltaAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad); // строка кода для спуска с угла по оси x если -x или x используем Mathf.Sign(hit.normal.x)

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }
    }

    void ResetFallingThroughPlatform() // Сбрасываем падение с платформы
    {
        collisions.fallingThroughPlatform = false;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public bool slidingDownMaxSlope;
        public float slopeAngle, slopeAngleOld; // slopeAngleOld = это угол при преведущем кадре.
        public Vector2 slopeNormal;
        public Vector2 deltaAmountOld;
        public int faceDir;
        public bool fallingThroughPlatform;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;
            slidingDownMaxSlope = false;
            slopeNormal = Vector2.zero;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }

}

