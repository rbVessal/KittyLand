using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//directive to enforce that our parent Game Object has a Character Controller
[RequireComponent(typeof(CharacterController))]

public class Player : MonoBehaviour
{
	//The Character Controller on my parent GameObject
	CharacterController characterController;

	// The linear gravity factor. Made available in the Editor.
	public float gravity = 100.0f;
	
	// mass of vehicle
	public float mass = 1.0f;

	// The initial orientation.
	private Quaternion initialOrientation;

	// The cummulative rotation about the y-Axis.
	private float cummulativeRotation;

	// The rotation factor, this will control the speed we rotate at.
	public float rotationSensitvity = 500.0f;

	//variables used to align the vehicle with the terrain surface 
	public float lookAheadDist = 2.0f; // How far ahead the scout is place
	private Vector3 hitNormal; // Normal to the terrain under the vehicle
	private float halfHeight; // half the height of the vehicle
	private Vector3 lookAtPt; // used to align the vehicle; marked by scout
	private Vector3 rayOrigin; // point from which ray is cast to locate scout
	private RaycastHit rayInfo; // struct to hold information returned by raycast
	private int layerMask = 1 << 8; //mask for a layer containg the terrain

	//movement variables - exposed in inspector panel
	public float maxSpeed = 50.0f; //maximum speed of vehicle
	public float maxForce = 15.0f; // maximimum force allowed
	public float friction = 0.997f; // multiplier decreases speed
	//boast to maxspeed when in rainbow dash mode
	private float maxSpeedBoost = 130.0f;
	private float newMaxSpeed;
	
	//movement variables - updated by this component
	private float speed = 0.0f;  //current speed of vehicle
	private Vector3 steeringForce; // force that accelerates the vehicle
	private Vector3 velocity; //change in position per second

    private Vector3 startingPosition;
    public GameObject behindPoint;
    private float radius;
    private const float BEHIND_POINT_OFFSET = 20.0f;
    private int numberOfKitties;
    private Vector3 flockDirection;
	private float radiusOfBehindObject;
	
	//Flag for when the player can run around with a rainbow trail
	private bool canRainbowDash;

    //Create an array of followers that will hold its current followers
    private List<LostKitten> followers;
	//Current lost kitty the player is chasing
	private LostKitten chasingLostKitty;
	
	private bool isGameStarted;
	
	private const int MAX_NUMBER_OF_KITTY_FOLLOWERS = 4;
	
	private GameObject rainbowTrail;
	
	private float originalMaxSpeed;

    //Getters and setters for properties
    public Vector3 StartingPosition
    {
        get { return startingPosition; }
    }

    public Vector3 SteeringForce
    {
        get { return steeringForce; }
        set { steeringForce = value; }
    }

    public float Speed
    {
        get { return speed; }
        set { speed = value; }
    }

    public List<LostKitten> Followers
    {
        get { return followers; }
        set { followers = value; }
    }

    public GameObject BehindPoint
    {
        get { return behindPoint; }
    }

    public float Radius
    {
        get { return radius; }
    }
	
	public float RadiusOfBehindObject
	{
		get { return radiusOfBehindObject;}
	}

    public Vector3 FlockDirection
    {
        get { return flockDirection; }
    }
	
	public LostKitten ChasingLostKitten
	{
		get{return chasingLostKitty;}
		set {chasingLostKitty = value;}
	}
	
	// Use this for initialization
	void Start ()
	{
		//Use GetComponent to save a reference to the Character Controller. This 
		//generic method is avalable from the parent Game Object. The class in the  
		// angle brackets <  > is the type of the component we need a reference to.
		
		characterController = gameObject.GetComponent<CharacterController> ();
		
		//save the quaternion representing our initial orientation from the transform
		initialOrientation = transform.rotation;
		
		//set the cummulativeRotation to zero.
		cummulativeRotation = 0.0f;
		
		//half the height of vehicle bounding box
		halfHeight = renderer.bounds.extents.y;

        //intialize the starting position
        startingPosition = transform.position;

        //initialize the followers list
        followers = new List<LostKitten>();

        radius = gameObject.GetComponent<SphereCollider>().radius;
		radiusOfBehindObject = behindPoint.GetComponent<SphereCollider>().radius + BEHIND_POINT_OFFSET;
        numberOfKitties = 1;
		
		isGameStarted = false;
		canRainbowDash = false;
		
		//Obtain the rainbow trail and store it in the property for later use
		rainbowTrail =  GameObject.FindGameObjectWithTag("Rainbow Trail");
		rainbowTrail.renderer.enabled = false;
		
		originalMaxSpeed = maxSpeed;
		newMaxSpeed = maxSpeed + maxSpeedBoost;
	}

	// Update is called once per frame
	void Update ()
	{
		// We will get our orientation before we move: rotate before translation	
		// We are using the left or right movement of the Mouse to steer our vehicle. 
		SteerWithMouse ();
		CalcForces ();
		//calculate steering - forces that change velocity
		ClampForces ();
		//forces must not exceed maxForce
		CalcVelocity ();
		//orient vehicle transform toward velocity
		if (velocity != Vector3.zero) 
        {
			transform.forward = velocity;
			MoveAndAlign ();
      		if(followers.Count > 0)
			{
				//Calculate forces needed for the flock
            	Alignment();
			}
			if(canRainbowDash)
			{
				runAroundWithARainbowTrail();
				
			}
		}

       
	}
	

	//-----------------------------------steer with mouse------------------------------------		
	// In mouse steering, we keep track of the cumulative rotation on the y-axis which we can combine
	// with our initial orientation to get our current heading. We are keeping our transform level so that
	// right and left turning remains predictable even if our vehicle banks and climbs.	
	void SteerWithMouse ()
	{
		//Get the left/right Input from the Mouse and use time along with a scaling factor 
		// to add a controlled amount to our cummulative rotation about the y-Axis.
		cummulativeRotation += Input.GetAxis ("Mouse X") * Time.deltaTime * rotationSensitvity;
		
		//Create a Quaternion representing our current cummulative rotation around the y-axis. 
		Quaternion currentRotation = Quaternion.Euler (0.0f, cummulativeRotation, 0.0f);
		
		//Use the quaternion to update the transform of our vehicle of the vehicles Game Object based on initial orientation 
		//and the currently applied rotation from the original orientation. 
		transform.rotation = initialOrientation * currentRotation;
	}

	//----------------------------Accelerate with Arrow or WASD keys------------------------------------		
	// If the user is pressing the up-arrow or W key we will return a force to accelerate the vehicle
	// along its z-axis which is to say in the foward direction.
	private Vector3 KeyboardAcceleration ()
	{
		//Move 'forward' based on player input
		Vector3 force;
		Vector3 dv = Vector3.zero;
		//dv is desired velocity
		dv.z = Input.GetAxis ("Vertical");
		//forward is positive z 
		//Take the moveDirection from the vehicle's local space to world space 
		//using the transform of the Game Object this script is attached to.
		dv = transform.TransformDirection (dv);
		dv *= maxSpeed;
		force = dv - transform.forward * speed;
		return force;
	}
	
	// Calculate the forces that alter velocity
	private void CalcForces ()
	{
		steeringForce = Vector3.zero;
		steeringForce += KeyboardAcceleration ();
	}

	// if steering forces exceed maxForce they are set to maxForce
	private void ClampForces ()
	{
		if (steeringForce.magnitude > maxForce) 
        {
			steeringForce.Normalize ();
			steeringForce *= maxForce;
		}
	}
	
	// acceleration and velocity are calculated
	void CalcVelocity ()
	{
		Vector3 moveDirection = transform.forward;
		// move in forward direction
		speed *= friction;
		// speed is reduced to simulate friction
		velocity = moveDirection * speed;
		// movedirection is scaled to get velocity
		Vector3 acceleration = steeringForce / mass;
		// acceleration is force/mass
		velocity += acceleration * Time.deltaTime;
		// add acceleration to velocity
		speed = velocity.magnitude;
		// speed is altered by acceleration		
		if (speed > maxSpeed) 
        {
			// clamp speed & velocity to maxspeed
			speed = maxSpeed;
			velocity = moveDirection * speed;
		}
	}

	//-----------------------------------MoveAndAlign------------------------------------		
	// Alignment permits our vehicle to tilt to climb hills and bank to follow the camber of the path.
	// It is done after we move and the transform is restored to its level state by the mouse steering
	// code at the beginning of the function as we prepare to orient and move again. 
	void MoveAndAlign ()
	{
		rayOrigin = transform.position + transform.forward * lookAheadDist;
		rayOrigin.y += 100;
		// A ray is cast from a position lookAheadDist ahead of the vehicle on its current path 
		// and high above the terrain. If the ray misses the terrain, we are likely to fall off, so
		// no move will take place.
        //Out doesn't return an object but it just sets it
		if (Physics.Raycast (rayOrigin, Vector3.down, out rayInfo, Mathf.Infinity, layerMask)) 
        {
			//Apply net movement to character controller which keeps us from penetrating colliders.
			// Velocity is scaled by deltaTime to give the correct movement for the time elapsed
			// since the last update. Gravity keeps us grounded.
			characterController.Move (velocity * Time.deltaTime + Vector3.down * gravity);
			
			// Use lookat function to align vehicle with terrain
			lookAtPt = rayInfo.point;
			lookAtPt.y += halfHeight;
			transform.LookAt (lookAtPt, hitNormal);
		}
	}

	// The hitNormal will give us a normal to the terrain under our vehicle
	// which we can use to align the vehicle with the terrain. It will be
	// called repeatedly when the collider on the character controller
	// of our vehicle contacts the collider on the terrain
	void OnControllerColliderHit (ControllerColliderHit hit)
	{	
		hitNormal = hit.normal;
	}

    //Collision detection between player and lost kitten
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Lost Kitty")
        {
            LostKitten lostKitten = collider.gameObject.GetComponent<LostKitten>();
            if (!lostKitten.CapturedByPlayer)
            {
				Debug.Log ("captured kitty");
                lostKitten.CapturedByPlayer = true;
                followers.Add(lostKitten);
				int numberOfFollowers = followers.Count;
				//Assign each lost kitten an index for easy access later on
				lostKitten.CatNumber = followers.Count-1;
				lostKitten.IsSearchingForWayPoint = false;
				//Assign the newly captured lost kitten a leader to follow
				//which would be the previous captured lost kitten or the player 
				//if it is the first captured lost kitten
				if(numberOfFollowers > 1)
				{
					lostKitten.CatFollowerLeader = followers[lostKitten.CatNumber-1];
				}
                numberOfKitties++;
            }
        }
		else if(collider.gameObject.tag == "Mom Cat")
		{
			if(!isGameStarted)
			{
				//Turn on the visibility of the lost kittens
				GameObject[]lostKittens = GameObject.FindGameObjectsWithTag("Lost Kitty");
				foreach(GameObject lostKitten in lostKittens)
				{
					lostKitten.gameObject.GetComponent<LostKitten>().Visibility(true);
				}
				
				isGameStarted = true;
			}
			else if(followers.Count == MAX_NUMBER_OF_KITTY_FOLLOWERS)
			{
				//Turn on the seek mom mode for the lost kittens
				foreach(LostKitten lostKitten in followers)
				{
					lostKitten.ReturnedToMomCat = true;
				}
				//Empty out the followers from the player
				followers.RemoveRange(0, MAX_NUMBER_OF_KITTY_FOLLOWERS);
				//Make the rainbow trail visible
				canRainbowDash = true;
				rainbowTrail.renderer.enabled = true;
				
				//Change the mom cat dialogue's
				collider.gameObject.GetComponent<MomCat>().ChangeDialogueText();
			}
		}
    }

    //Calculate the direction the flock should face
    public Vector3 Alignment()
    {
		//Make the followers have the same direction as player
        flockDirection = transform.forward * maxSpeed;
        flockDirection.Normalize();
        flockDirection *= maxSpeed;
		flockDirection.y = 0;
        return flockDirection;
    }
	
	//Make the player's speed the max speed and increase the max speed
	private void runAroundWithARainbowTrail()
	{
		//If player holds down the spacebar then make the speed the new max speed
		if(Input.GetKey(KeyCode.Space))
		{
			speed = newMaxSpeed;
			maxSpeed = speed;
		}
		else
		{
			maxSpeed = originalMaxSpeed;
		}
		generateRainbowTrail();
	}
	
	//Change the width of the rainbow trail depending on the speed of the player
	private void generateRainbowTrail()
	{
		//Scale it by the speed of the player
		rainbowTrail.transform.localScale = new Vector3(rainbowTrail.transform.localScale.x, speed, rainbowTrail.transform.localScale.z);
		//Move the position of the rainbow trail to be behind the player
		rainbowTrail.transform.localPosition = new Vector3(rainbowTrail.transform.localPosition.x, 
			rainbowTrail.transform.localPosition.y,  -speed/2);
		
	}
}

