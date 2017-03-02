using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class TransportInterface : MonoBehaviour {

    InteractionObject whichObject;
    GameObject whichGoal;
    public int stepInCascade;
    int intgoal = 0;
    float holdWeight;
    
    public Transform Character;
    private WalkToGoal CharacterWalkTo = null;
    private PickUp CharacterPickUp;

    // for picking up and dropping
    public InteractionObject obj1; // The object1 to pick up
    public InteractionObject obj2; 
    public InteractionObject obj3; 
    public GameObject dropPoint1; // Gameobject indicating goal position. Why gameObject and not transform? Because it needs to be set to another physics layer to avoid collisions with obj1
    public GameObject dropPoint2;
    public GameObject dropPoint3;
    public Vector3 HoldPoint1Position; // where to hold this object
    public Vector3 HoldPoint1Rotation; // how to rotate this object while holding
    public Vector3 HoldPoint2Position;
    public Vector3 HoldPoint2Rotation;
    public Vector3 HoldPoint3Position;
    public Vector3 HoldPoint3Rotation;
    Vector3 CurrentHoldPointPosition;
    Vector3 CurrentHoldPointRotation;

    // Use this for initialization
    void Start () {
        CharacterWalkTo = Character.GetComponent<WalkToGoal>();
        CharacterPickUp = Character.GetComponent<PickUp>();

        // set all goal transforms on a different layer, and disable collisions to normal layer
        if (dropPoint1 != null) { dropPoint1.layer = 1; }
        if (dropPoint2 != null) { dropPoint2.layer = 1; }
        if (dropPoint3 != null) { dropPoint3.layer = 1; }
        Physics.IgnoreLayerCollision(0, 1, true);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(120, 10, 100, 200)); // You can change position of Interface here. This is designed so that all my interface scripts can run together. 
        if (GUILayout.Button("Transport Box")) { intgoal = 1; whichObject = obj1; whichGoal = dropPoint1; CurrentHoldPointPosition = HoldPoint1Position; CurrentHoldPointRotation = HoldPoint1Rotation; stepInCascade = 0; }
        if (GUILayout.Button("Transport Guitar")) { intgoal = 2; whichObject = obj2; whichGoal = dropPoint2; CurrentHoldPointPosition = HoldPoint2Position; CurrentHoldPointRotation = HoldPoint2Rotation; stepInCascade = 0; }
        if (GUILayout.Button("Transport statue")) { intgoal = 3; whichObject = obj3; whichGoal = dropPoint3; CurrentHoldPointPosition = HoldPoint3Position; CurrentHoldPointRotation = HoldPoint3Rotation; stepInCascade = 0; }
        GUILayout.EndArea();
    }


    // Update is called once per frame
        void Update () {
        if (intgoal > 0)
        {
            stepInCascade = TransportObject(whichObject, whichGoal, CharacterWalkTo, CharacterPickUp, 0.3f, CurrentHoldPointPosition, CurrentHoldPointRotation, stepInCascade);
        }
    }



    public int TransportObject(InteractionObject whichObject, GameObject whichGoal, WalkToGoal walktogoal, PickUp pickup, float pickuptime, Vector3 holdpointPosition, Vector3 holdPointRotation, int cascade)
    {
        if (cascade == 0) // first communicate some basics to pickup script
        {
            pickup.SetPickUpTime(pickuptime);
            pickup.SetCurrentIO(whichObject);
            cascade++;
        }

        if (cascade == 1) // walk to the object
        {
            // public bool WalkTo(Transform CurrentGoal, bool DrawPath, float maxspeed, float mindist, float anglecut) // to check what the parameters mean
            bool ObjectReached = walktogoal.WalkTo(whichObject.transform, false, 0.4f, 0.2f, 120); // walk towards object to be pickup
            if (ObjectReached) {cascade++; }
        }

        if (cascade == 2) // pick it up
        {
            pickup.SetHoldPoint(holdpointPosition, holdPointRotation); // tell script how to hold this object. This is somehow overwritten if in cascade==0. Here it is called multiple times, but it's computationally light so that's ok
            float pickedup = pickup.PickUpObject(whichObject); // returns value from 0 to 1 indicating when it has picked up the object
            if (pickedup > 0.50) { cascade++; } // start walking towards goal before reaching 1, that is before object is perfectly in place. This gives a more fluent movement.
        }

        if (cascade == 30) // walk to the goal
        {
            bool GoalReached = walktogoal.WalkTo(whichGoal.transform, false, 0.4f, 0.2f, 120);
            if (GoalReached) { cascade++; }
        }

        if (cascade == 4) // drop object
        {
            float dropped = pickup.DropObject(whichGoal.transform);
            if (dropped > 0.99f)
            {
                pickup.LetGo();
                cascade++;
            }
        } 
        
        return cascade;
    }
}
