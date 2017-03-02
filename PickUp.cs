using UnityEngine;
using System.Collections;
using RootMotion;
using RootMotion.FinalIK;

// adapted from PickUp2Handed.cs from Final IK, which is however a "public abstract class"

//namespace RootMotion.Demos
//{
    /// <summary>
    /// Picking up an arbitrary object with both hands.
    /// </summary>
    public class PickUp : MonoBehaviour
    {
        public InteractionSystem interactionSystem; // The InteractionSystem of the character
        private InteractionObject CurrentIO;

        //public GameObject dropPoint1; // Gameobject indicating goal position. Why gameObject and not transform? Because it needs to be set to another physics layer to avoid collisions with obj1
        //public GameObject dropPoint2;
        //public GameObject dropPoint3;
        //private GameObject CurrentdropPoint;


        //public Transform pivot; // The pivot point of the hand targets - I don't use this
        public Transform holdPoint; // The point where the object will lerp to when picked up
        public Transform holdPointOriginal; // von mir: speichert, wo holdPoint liegen soll, wenn es nicht gerade zum dropPoint bewegt wird
        private float pickUpTime = 0.3f; // Maximum lerp speed of the object. Decrease this value to give the object more weight
        float slerpValue = 0f; // von mir. Brauche ich, um Box beim Abstellen an Ziel heran zu slerpen
        bool dropping = false; // von mir. Bestimmt, wann Box im Drop-Modus ist

        private float holdWeight, holdWeightVel;
        private Vector3 pickUpPosition;
        private Quaternion pickUpRotation;
        Vector3 ObjectWhenUpright; // note the rotation of the object when it was still upright

        //////////////////////////////////////////////////////////////////////////////// functions I added to the original "PickUp2Handed.cs"
        ////////////////////////////////////////////////////////////////////////////////
        public void SetCurrentIO(InteractionObject interactionobject) { CurrentIO = interactionobject; } // simply set these two values from outside
        public void SetPickUpTime(float time) { pickUpTime = time; }
        public void SetHoldPoint(Vector3 holdPointPosition, Vector3 holdPointRotation)
        {
        holdPoint.localPosition = holdPointPosition;
        holdPointOriginal.localPosition = holdPointPosition;
        holdPoint.localEulerAngles = holdPointRotation;
        holdPointOriginal.localEulerAngles = holdPointRotation;
    }

        public float PickUpObject(InteractionObject whichIO) 
        {
            if (!holding)
            {
                interactionSystem.StartInteraction(FullBodyBipedEffector.LeftHand, whichIO, false);
                interactionSystem.StartInteraction(FullBodyBipedEffector.RightHand, whichIO, false);
            }
            ObjectWhenUpright = whichIO.transform.localEulerAngles;
            return holdWeight; // lerps from 0 to 1
        }

        public float DropObject(Transform droppoint) // lerp object towards drop point
        {
            holdPoint.position = Vector3.Slerp(holdPointOriginal.position, droppoint.position, slerpValue); // position is slerped towards goal
                                                                                                            //holdPoint.position = Vector3.Slerp(holdPointOriginal.position, droppoint.position, slerpValue); // Box wird an Ziel herangeslerpt
            holdPoint.rotation = Quaternion.Slerp(holdPointOriginal.rotation, Quaternion.Euler(new Vector3(0, holdPointOriginal.rotation.y, 0)), slerpValue); // Make sure object is upright when put down
        holdPoint.rotation = Quaternion.Slerp(holdPointOriginal.rotation, Quaternion.Euler(new Vector3(ObjectWhenUpright.x, holdPointOriginal.localRotation.y, ObjectWhenUpright.z)), slerpValue); // put object upright, bot y rotation does not need to be like at the beginning
        
            slerpValue += .04f;
            return slerpValue;
        }

        public void LetGo()
        {
            interactionSystem.ResumeAll(); // the actual letting go of the object
            holdPoint.position = holdPointOriginal.position;
            slerpValue = 0;
        }
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        void Start()
        {
            // Listen to interaction events
            interactionSystem.OnInteractionStart += OnStart;
            interactionSystem.OnInteractionPause += OnPause;
            interactionSystem.OnInteractionResume += OnDrop;
        }


        // Called by the InteractionSystem when an interaction is paused (on trigger)
        private void OnPause(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
        {
            if (effectorType != FullBodyBipedEffector.LeftHand) return;
            if (interactionObject != CurrentIO) return;

            // Make the object inherit the character's movement
            CurrentIO.transform.parent = interactionSystem.transform;

            // Make the object kinematic
            var r = CurrentIO.GetComponent<Rigidbody>();
            if (r != null) r.isKinematic = true;

            // Set object pick up position and rotation to current
            pickUpPosition = CurrentIO.transform.position;
            pickUpRotation = CurrentIO.transform.rotation;
            holdWeight = 0f;
            holdWeightVel = 0f;
        }


        // Called by the InteractionSystem when an interaction starts
        private void OnStart(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
        {
            if (effectorType != FullBodyBipedEffector.LeftHand) return;
            if (interactionObject != CurrentIO) return;

            // Rotate the pivot of the hand targets
            //RotatePivot(); // I'm leaving rotating of the pivot out for now

            // Rotate the hold point so it matches the current rotation of the object
            holdPoint.rotation = CurrentIO.transform.rotation;
        }

        // Called by the InteractionSystem when an interaction is resumed from being paused
        private void OnDrop(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
        {
            if (effectorType != FullBodyBipedEffector.LeftHand) return;
            if (interactionObject != CurrentIO) return;

            //holdPoint.position = CurrentdropPoint.position; // von mir

            // Make the object independent of the character
            CurrentIO.transform.parent = null;

            // Turn on physics for the object
            if (CurrentIO.GetComponent<Rigidbody>() != null) CurrentIO.GetComponent<Rigidbody>().isKinematic = false;
            holdWeight = 0f; // von mir: sonst steht holdweight beim zweiten Objekt noch bei 1, und character dreht sich gleich um, bevor er es gegriffe hat
    }


        void LateUpdate()
        {
            if (holding)
            {
                // Smoothing in the hold weight
                holdWeight = Mathf.SmoothDamp(holdWeight, 1f, ref holdWeightVel, pickUpTime);

                // Interpolation
                CurrentIO.transform.position = Vector3.Lerp(pickUpPosition, holdPoint.position, holdWeight);
                CurrentIO.transform.rotation = Quaternion.Lerp(pickUpRotation, holdPoint.rotation, holdWeight);
            }
        }

        // Are we currently holding the object?
        private bool holding
        {
            get
            {
                return interactionSystem.IsPaused(FullBodyBipedEffector.LeftHand);
            }
        }

        // Clean up delegates
        void OnDestroy()
        {
            if (interactionSystem == null) return;

            interactionSystem.OnInteractionStart -= OnStart;
            interactionSystem.OnInteractionPause -= OnPause;
            interactionSystem.OnInteractionResume -= OnDrop;
        }
    }
//}
