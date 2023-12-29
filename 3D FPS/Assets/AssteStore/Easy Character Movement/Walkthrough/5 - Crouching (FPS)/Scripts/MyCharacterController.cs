using ECM.Controllers;
using UnityEngine;

namespace ECM.Walkthrough.FPSCrouching
{
                    
    public class MyCharacterController : BaseFirstPersonController
    {
        protected override void Animate()
        {
                     }

        protected override void AnimateView()
        {
             
              
            Vector3 targetPosition = isCrouching
                ? new Vector3(0.0f, crouchingHeight , 0.0f)
                : new Vector3(0.0f, standingHeight - 0.35f, 0.0f);
 
            cameraTransform.localPosition =
                Vector3.MoveTowards(cameraTransform.localPosition, targetPosition, 5.0f * Time.deltaTime);
        }

        protected override void HandleInput()
        {
             
                          
            if (Input.GetKeyDown(KeyCode.P))
                pause = !pause;

             
            moveDirection = new Vector3
            {
                x = Input.GetAxisRaw("Horizontal"),
                y = 0.0f,
                z = Input.GetAxisRaw("Vertical")
            };

            run = Input.GetButton("Fire3");

            jump = Input.GetButton("Jump");

            crouch = Input.GetKey(KeyCode.C);
        }
    }
}