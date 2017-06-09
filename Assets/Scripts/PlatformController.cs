using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastController
{
    public LayerMask passengerMask;

    public Vector3[] localWaypoints; // точки по которым будет осуществляться передвижение платформы
    Vector3[] gloabalWaypoints; // точки в глобальном пространстве по которым будет осуществляться передвижение платформы

    public float speed;
    public bool cyclic;
    public float waitTime; // время ожидания в точки движения
    [Range(0,3)] // Clamp работает только в инспекторе
    public float easeAmount; // переменная плваности движения.

    int fromWaypontIndex;
    float percentBetweenWaypoint; // 0-1;
    float nextMoveTime;

    List<PassengerMovement> passengerMovement;
    Dictionary<Transform, Controller2D> passengerDictonary = new Dictionary<Transform, Controller2D>();

    public override void Start()
    {
        base.Start();

        gloabalWaypoints = new Vector3[localWaypoints.Length]; // Конвертация localWaypoints в globalWaypoints т.е что бы точки передвижения не перемещались всесте с платформой а прикреплялись к global'у
        for(int i =0; i < localWaypoints.Length; i++)// Вот это тоже ))
        {
            gloabalWaypoints[i] = localWaypoints[i] + transform.position;// И это тоже))
        }
    }

    private void Update()
    {
        UpdateRaycastOrigins();

        Vector3 velocity = CalculatePlatformMovement();

        CalculatePassengerMovement(velocity);

        MovePassengers(true);
        transform.Translate(velocity);
        MovePassengers(false);
    }

    float Ease (float x) // плавное предвижение плтформы (см [Unity] Creating a 2D Platformer (E08. platform waypoint system) 15:30) спомощью графиков y = Xa/Xa +(1-X)a(a - это степень).
    {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    Vector3 CalculatePlatformMovement() // Расчет цикличного движения платформы 
    {
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }

        fromWaypontIndex %= gloabalWaypoints.Length;
        int toWaypointIndex = (fromWaypontIndex + 1) % gloabalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(gloabalWaypoints[fromWaypontIndex], gloabalWaypoints[toWaypointIndex]);
        percentBetweenWaypoint += Time.deltaTime * speed / distanceBetweenWaypoints;
        percentBetweenWaypoint = Mathf.Clamp01(percentBetweenWaypoint); // Clamp01 это рамки в диапозоне между 0 и 1.
        float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoint);

        Vector3 newPos = Vector3.Lerp(gloabalWaypoints[fromWaypontIndex], gloabalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

        if(percentBetweenWaypoint >= 1)
        {
            percentBetweenWaypoint = 0;
            fromWaypontIndex++;

            if (!cyclic)
            {
                if (fromWaypontIndex >= gloabalWaypoints.Length - 1)
                {
                    fromWaypontIndex = 0;
                    System.Array.Reverse(gloabalWaypoints);
                }
            }
            nextMoveTime = Time.time + waitTime;
        }

        return newPos - transform.position;
    }

    void MovePassengers (bool beforeMovePlatform) // Этот метод делает так что при столкновении с другим препятствие, игрок не перемещаеться вместе с платформой
    {
        foreach(PassengerMovement passenger in passengerMovement)
        {
            if (!passengerDictonary.ContainsKey(passenger.transform))
            {
                passengerDictonary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());// Оптимизация с помощью словаря
            }
            if (passenger.moveBeforePlatform == beforeMovePlatform)
            {
                passengerDictonary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);// Вызывется только один раз //passenger.transform.GetComponent<Controller2D>().Move(passenger.velocity, passenger.standingOnPlatform); // плохая оптимизация постоянно вызывать GetComponent
            }
        }
    }

    void CalculatePassengerMovement(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengerMovement = new List<PassengerMovement>();

        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        // Движение платформы по вертикали
        // Vertically moving platform
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), directionY == 1, true)); // Движение игрока на платформе взависимости от движения платформы и положения игрока
                        //hit.transform.Translate(new Vector3(pushX, pushY)); // Преремещаем игрока вместе с пдатформой
                    }
                }
            }
        }
        // Движение платформы по горизонтали
        // Horizontally moving platform
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight; // конструкция if else : ?
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));// Движение игрока на платформе взависимости от движения платформы и положения игрока
                        //hit.transform.Translate(new Vector3(pushX, pushY));
                    }
                }
            }
        }

        // Passenger on top of a horizontally or downward moving platform
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
        {
            float rayLength = skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));// Движение игрока на платформе взависимости от движения платформы и положения игрока
                        //hit.transform.Translate(new Vector3(pushX, pushY));
                    }
                }
            }
        }
    }

    struct PassengerMovement // Тип struct представляет собой тип значения, который обычно используется для инкапсуляции небольших групп связанных переменных, например координат прямоугольника или характеристик элемента в инвентаризации.
    {
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform;

        public PassengerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform) // Конструктор == // Движение игрока на платформе взависимости от движения платформы и положения игрока
        {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }

    private void OnDrawGizmos()
    {
        if (localWaypoints != null)
        {
            Gizmos.color = Color.red;
            float size = 0.3f;

            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 globalWaypointPos = (Application.isPlaying)? gloabalWaypoints[i] : localWaypoints[i] + transform.position; // Если приложение запущено используем globalwaypoints[i] в гизмос
                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }
}

