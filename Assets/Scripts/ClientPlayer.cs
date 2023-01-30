using UnityEngine;

/// <summary>
/// Control the client player
/// including gravity control.
/// This falls to -transform.up
/// Has GroundedChecker beneath
/// </summary>
public class ClientPlayer : MonoBehaviour
{
    [Header("Physical consts")]
    [SerializeField] private float walkingSpeed = 1f;
    [SerializeField] private float runningSpeed = 3f;
    [SerializeField] private float jumpHeight = 10f;
    [Tooltip("Gravitational Acceleration")]
    [SerializeField] private float gravitation = -0.5f;
    [SerializeField] private float minFallingSpeed = -2f;

    [Header("Grounded Checker")]
    [SerializeField] private float groundedCheckerRadius = 1f;
    [SerializeField] private float groundedCheckerOffset = -1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Adjusting Ground")]
    [Tooltip("The max vertical distance of player from the ground")]
    [SerializeField] private float adjusterRadius = 0.1f;

    public enum State
    {
        Idle,
        Moving,
        Jumping
    }

    public State state { set; private get; } = State.Idle;
    
    //the state will be Idle if this is true for the whole frame
    private bool willBeIdle = false;

    //for jumping + gravity
    private float verticalSpeed = 0f;

    private AnimatorController animatorController;

    void Start()
    {
        animatorController = new AnimatorController(
                                GetComponent<Animator>()
                              );
    }

    private void Update()
    {
        //check if grounded and reflect gravity
        Fall();

        //If the state is changed other than Idle,
        //this must be false
        //as long as other functions such as WalkForward() called every frame
        if (willBeIdle)
        {
            state = State.Idle;
        }
        willBeIdle = true;

        //update animation
        animatorController.Update(state);
    }

    /// <summary>
    /// Change State
    /// </summary>
    private void ChangeState(State newState)
    {
        state = newState;

        //stop being Idle
        willBeIdle = false;
    }

    //Called by InputManager
    public void WalkForward()
    {
        //go forward
        Vector3 vec = transform.forward * walkingSpeed;
        Translate(vec);

        //change states
        if ((state == State.Idle) || (state == State.Moving)) {
            ChangeState(State.Moving);
        }
        animatorController.movingState = AnimatorController.MovingState.Walking;
    }

    //Called by InputManager
    public void RunForward()
    {
        //go forward
        Vector3 vec = transform.forward * runningSpeed;
        Translate(vec);

        //change states
        if ((state == State.Idle) || (state == State.Moving))
        {
            ChangeState(State.Moving);
        }
        animatorController.movingState = AnimatorController.MovingState.Running;
    }

    /// <summary>
    /// Move position (not rotating)
    /// </summary>
    private void Translate(Vector3 velocity)
    {
        transform.position += velocity * Time.deltaTime;
    }

    /// <summary>
    /// Check if in air or not
    /// And get gravity
    /// </summary>
    private void Fall()
    {
        bool grounded = CheckGrounded();

        if (!grounded)
        {
            //in air

            //fall down
            Vector3 vec = transform.up * verticalSpeed;
            Translate(vec);

            //accerlate
            verticalSpeed += gravitation * Time.deltaTime;

            //shouldn't exceed min
            if (verticalSpeed < minFallingSpeed)
            {
                verticalSpeed = minFallingSpeed;
            }
        }
    }

    /// <summary>
    /// Check if grounded.
    /// If not grounded, the state will be Jumping.
    /// Also adjust when landed
    /// </summary>
    private bool CheckGrounded()
    {
        bool grounded = Physics.CheckSphere(
                            GetGroundedCheckerPosition(),
                            groundedCheckerRadius,
                            groundLayer
                        );

        //if this is the first frame of grounded
        if(grounded && (state == State.Jumping))
        {
            AdjustGround();
            state = State.Idle;
        }

        if (!grounded)
        {
            ChangeState(State.Jumping);
        }

        return grounded;
    }

    /// <summary>
    /// Adjust the vertical position of the character
    /// so as it right at the ground, not buried or floating
    /// Should be called when grounded == true
    /// </summary>
    private void AdjustGround()
    {
        Vector3 initialFootPosition = GetGroundedCheckerPosition();

        float optimalZ = 0f;

        //start from the edge to the other edge of the GroundChecker
        for(float z = -groundedCheckerRadius; z <= groundedCheckerRadius; z += adjusterRadius)
        {
            Vector3 adjustedPosition
                = initialFootPosition + transform.up * z;

            //Use the adjuster
            bool collided
                = Physics.CheckSphere(
                        adjustedPosition,
                        adjusterRadius,
                        groundLayer
                    );

            if (collided)
            {
                //adjust completed
                optimalZ = z;
                break;
            }
        }

        //Adjust the position
        transform.position += transform.up * optimalZ;
    }

    //for debugging grounded checker
    private void OnDrawGizmosSelected()
    {
        //show grounded checker
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawSphere(GetGroundedCheckerPosition(), groundedCheckerRadius);

        //show adjuster
        Gizmos.color = new Color(0, 0, 1, 1f);
        Gizmos.DrawSphere(GetGroundedCheckerPosition(), adjusterRadius);
    }

    private Vector3 GetGroundedCheckerPosition()
    {
        return transform.position + transform.up.normalized * groundedCheckerOffset;
    }

    /// <summary>
    /// Control the Animator. Highly depend on the structure of Animator.
    /// </summary>
    //for Unity-Chan Animator
    private class AnimatorController
    {
        //Don't change
        public enum MovingState
        {
            Walking,
            Running
        }

        public MovingState movingState = MovingState.Walking;

        private Animator animator;

        //contructor
        //don't change
        public AnimatorController(Animator animator)
        {
            this.animator = animator;
        }

        //you may change
        public void Update(ClientPlayer.State state)
        {
            switch (state)
            {
                case ClientPlayer.State.Idle:
                    SetValues(0f, false, 0f);
                    break;

                case ClientPlayer.State.Moving:
                    switch (movingState)
                    {
                        case MovingState.Walking:
                            SetValues(0.2f, false, 0f);
                            break;
                        case MovingState.Running:
                            SetValues(1f, false, 0f);
                            break;
                    }
                    break;

                case ClientPlayer.State.Jumping:
                    SetValues(0f, true, 0f);
                    break;
            }
        }

        //you may change
        private void SetValues(float speed, bool isJumping, float direction)
        {
            animator.SetFloat("Speed", speed);
            animator.SetBool("Jump", isJumping);
            animator.SetFloat("Direction", direction);
        }
    }
}
