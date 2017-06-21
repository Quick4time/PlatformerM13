using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    [SerializeField]
    float maxjumpHeight = 4.0f; // Максимальная высота прыжка.
    [SerializeField]
    float minJumpHeight = 1.0f; // Минимальная высота прыжка.
    [SerializeField]
    float timeToJumpApex = 0.4f; // Время которое будет затрачено для достижения максимальной высоты.
    [SerializeField]
    float accelerationTimeAirborne = 0.2f; // при увелечении не позволяют управлять персонажем в вохдухе.
    [SerializeField]
    float accelerationTimeGrounded = 0.1f; // Переменные которые при увелечении параметра создают ускорение и некоторый delay торможения.
    float moveSpeed = 6.0f; // скорость передвижения
    float sprintSpeed = 9.0f; // скорость при спринте
    [Range(0.0f, 100.0f)] // Исключительно для инспектора
    [SerializeField]
    private float stamina;// И такой
    public float Stamina { get { return stamina; } set { stamina = Mathf.Clamp(value, 0.0f, 100.0f); } } // Лучше зделать int !!!такой Clamp!!!

    public float wallSlideSpeedMax = 3.0f; // wallSliding
    public float wallSticTime = 0.25f;// wallSliding
    float timeToWallUnstick;// wallSliding

    public Vector2 wallJumpClimb; // wallSliding
    public Vector2 wallJumpOff; // wallSliding
    public Vector2 wallLeap; // wallSliding

    [SerializeField]
    private bool Sliding; // Переменная для ON/OFF Sliding

    private bool wallSliding; // Внутреняя переменная

    [SerializeField]
    private float mass; // масса наоборот чем больше масса тем меньше на нас действует сила притяжения

    float gravity; // гравитация
    float maxjumpVelocity;
    float minJumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing; // smoothing передвижение

    [SerializeField]
    bool FreezeAllMovement; // Подумать над более прямой реализацией.

    Controller2D controller;

    Vector2 directionalInput;
    int wallDirX;

    void Start()
    {
        Stamina = 50.0f;
        controller = GetComponent<Controller2D>();
        FreezeAllMovement = false;

        gravity = -(1.5f * maxjumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxjumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
    }

    void Update()
    {
        if (!FreezeAllMovement)
        {
            CalculateVelocity();
            if (Sliding)
            {
                HandleWallSliding();
            }

            if (velocity.x <= 6 && controller.collisions.below)
            {
                Stamina += 3.0f * Time.deltaTime;
            }
            controller.Move(velocity * Time.deltaTime, directionalInput);
            Sprint(sprintSpeed);
            if (controller.collisions.above || controller.collisions.below) // гравитация действует только когда true above или below
            {
                if(controller.collisions.slidingDownMaxSlope)
                {
                    velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
                }
                else
                {
                    velocity.y = 0;
                }
            }
            //print("Gravity: " + gravity + "  Jump Velocity: " + maxjumpVelocity);
        }
    }
    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown()
    {
        if (Sliding)
        {
            if (wallSliding)// wallSliding
            {
                if (wallDirX == directionalInput.x)
                {
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
                else if (directionalInput.x == 0)
                {
                    velocity.x = -wallDirX * wallJumpOff.x;
                    velocity.y = wallJumpOff.y;
                }
                else
                {
                    velocity.x = -wallDirX * wallLeap.x;
                    velocity.y = wallLeap.y;
                }
            }
        }
        if (controller.collisions.below)
        {
            Stamina -= 5.0f;
            if (controller.collisions.slidingDownMaxSlope) // В этой строке делаем так что бы при скольжении с максимального угла поверхности игрок выпрыгивал вперед
            {
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                {
                    velocity.y = (maxjumpVelocity - mass) * controller.collisions.slopeNormal.y;
                    velocity.x = (maxjumpVelocity - mass) * controller.collisions.slopeNormal.x;
                }
            }
            else
            {
                velocity.y = maxjumpVelocity - mass;
            }
        }
    }
    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

 
    void CalculateVelocity() // Метод реализующий перемещение
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
    }

    void HandleWallSliding()
    {
        int wallDirX = (controller.collisions.left) ? -1 : 1; // wallSliding
        wallSliding = false;// wallSliding
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0) // реадизуем скольжение по стене с определенной скоростью
        {
            wallSliding = true;// wallSliding

            if (velocity.y < -wallSlideSpeedMax)// wallSliding
            {
                velocity.y = -wallSlideSpeedMax;// wallSliding
            }
            if (timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0;
                velocity.x = 0;
                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    timeToWallUnstick = wallSticTime;
                }
            }
            else
            {
                timeToWallUnstick = wallSticTime;
            }
        }
    }

    bool Sprint(float sSpeed)
    {
        if (Input.GetKey(KeyCode.LeftShift) && controller.collisions.below && Stamina >= 0.0f)
        {
            Stamina -= 5.0f * Time.deltaTime;
            moveSpeed = sprintSpeed;
            return true;
        }
        else
        {
            moveSpeed = 6;
            return false;
        }
    }
}


