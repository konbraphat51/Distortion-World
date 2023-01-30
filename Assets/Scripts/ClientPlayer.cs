using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientPlayer : MonoBehaviour
{
    [SerializeField] private float walkingSpeed = 1f;
    [SerializeField] private float runningSpeed = 3f;
    
    public enum State
    {
        Idle,
        Moving,
        Jumping
    }

    public State state { set; private get; } = State.Idle;

    private AnimatorController animatorController;

    void Start()
    {
        animatorController
            = new AnimatorController(
                GetComponent<Animator>()
              );
    }

    private void Update()
    {
        //If the state is other than Idle,
        //state must be changed
        //when other functions such as WalkForward() called every frame
        State currentState = state;
        state = State.Idle;

        animatorController.Update(currentState);
    }

    //Called by InputManager
    public void WalkForward()
    {
        //go forward
        Vector3 vec = transform.forward * walkingSpeed;
        Translate(vec);

        //change states
        this.state = State.Moving;
        animatorController.movingState = AnimatorController.MovingState.Walking;
    }

    //Called by InputManager
    public void RunForward()
    {
        //go forward
        Vector3 vec = transform.forward * runningSpeed;
        Translate(vec);

        //change states
        this.state = State.Moving;
        animatorController.movingState = AnimatorController.MovingState.Running;
    }

    /// <summary>
    /// Move position (not rotating)
    /// </summary>
    private void Translate(Vector3 velocity)
    {
        transform.Translate(velocity * Time.deltaTime);
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
                            SetValues(0.5f, false, 0f);
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
