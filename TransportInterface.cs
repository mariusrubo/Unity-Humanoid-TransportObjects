using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class TransportInterface : MonoBehaviour {

    InteractionObject whichObject;
    GameObject whichGoal;
    int stepInCascade;
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
        if (GUILayout.Button("Pick Up 1")) { intgoal = 1; whichObject = obj1; whichGoal = dropPoint1; stepInCascade = 0; }
        if (GUILayout.Button("Pick Up 2")) { intgoal = 2; whichObject = obj2; whichGoal = dropPoint2; stepInCascade = 0; }
        if (GUILayout.Button("Pick Up 3")) { intgoal = 3; whichObject = obj3; whichGoal = dropPoint3; stepInCascade = 0; }
        GUILayout.EndArea();
    }


    // Update is called once per frame
        void Update () {
        if (intgoal > 0)
        {
            stepInCascade = TransportObject(whichObject, whichGoal, CharacterWalkTo, CharacterPickUp, 0.3f, stepInCascade);
        }
    }



    public int TransportObject(InteractionObject whichObject, GameObject whichGoal, WalkToGoal walktogoal, PickUp pickup, float pickuptime, int cascade)
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
            float pickedup = pickup.PickUpObject(whichObject); // returns value from 0 to 1 indicating when it has picked up the object
            if (pickedup > 0.50) { cascade++; }
        }

        if (cascade == 3) // walk to the goal
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
