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

        //public Transform pivot; // The pivot point of the hand targets - I don't use this
        public Transform holdPoint; // The point where the object will lerp to when picked up
        public Transform holdPointOriginal; // von mir: speichert, wo holdPoint liegen soll, wenn es nicht gerade zum dropPoint bewegt wird
        private float pickUpTime = 0.3f; // Maximum lerp speed of the object. Decrease this value to give the object more weight
        float slerpValue = 0f; // I added this to move holdPoint towards destination
        bool dropping = false; // I added this to communicate when object is being dropped to destination

        private float holdWeight, holdWeightVel;
        private Vector3 pickUpPosition;
        private Quaternion pickUpRotation;

        //////////////////////////////////////////////////////////////////////////////// functions I added to the original "PickUp2Handed.cs"
        ////////////////////////////////////////////////////////////////////////////////
        public void SetCurrentIO(InteractionObject interactionobject) { CurrentIO = interactionobject; } // simply set these two values from outside
        public void SetPickUpTime(float time) { pickUpTime = time; }

        public float PickUpObject(InteractionObject whichIO) 
        {
            if (!holding)
            {
                interactionSystem.StartInteraction(FullBodyBipedEffector.LeftHand, whichIO, false);
                interactionSystem.StartInteraction(FullBodyBipedEffector.RightHand, whichIO, false);
            }
            return holdWeight; // lerps from 0 to 1
        }

        public float DropObject(Transform droppoint) // lerp object towards drop point
        {
            holdPoint.position = Vector3.Slerp(holdPointOriginal.position, droppoint.position, slerpValue); // move object towards destination
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
