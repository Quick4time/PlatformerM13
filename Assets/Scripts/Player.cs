using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    [SerializeField]
    float jumpHeight = 4.0f; // Максимальная высота прыжка.
    [SerializeField]
    float timeToJumpApex = 0.4f; // Время которое будет затрачено для достижения максимальной высоты.
    [SerializeField]
    float accelerationTimeAirborne = 0.2f; // при увелечении не позволяют управлять персонажем в вохдухе.
    [SerializeField]
    float accelerationTimeGrounded = 0.1f; // Переменные которые при увелечении параметра создают ускорение и некоторый delay торможения.
    float moveSpeed = 6.0f; // скорость передвижения
    float sprintSpeed = 9.0f; // скорость при спринте

    private float stamina;
    public float Stamina { get { return stamina; } set { stamina = Mathf.Clamp(value, 0.0f, 100.0f); } } // Лучше зделать int

    float gravity; // гравитация
    float jumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing; // smoothing передвижение

    [SerializeField]
    bool FreezeAllMovement; // Подумать над более прямой реализацией.

    Controller2D controller;

    void Start()
    {
        Stamina = 50.0f;
        controller = GetComponent<Controller2D>();
        FreezeAllMovement = false;

        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        print("Gravity: " + gravity + "  Jump Velocity: " + jumpVelocity);
    }

    void Update()
    {
        //Debug.Log(stamina.ToString());
        if (controller.collisions.above || controller.collisions.below) // гравитация действует только когда true above или below
        {
            velocity.y = 0;
        }
        
        if (velocity.x <= 6 && controller.collisions.below)
        {
            Stamina += 3.0f * Time.deltaTime;
        }
        
        if (!FreezeAllMovement)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below && Stamina >= 5.0f)
            {
                Stamina -= 5.0f;
                velocity.y = jumpVelocity;
            }

            float targetVelocityX = input.x * moveSpeed;
            velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            Sprint(sprintSpeed);
        }
        //Debug.Log(velocity.x.ToString());
        //Debug.Log(moveSpeed.ToString());
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


