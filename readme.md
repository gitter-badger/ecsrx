# AlphaECS

[![Join the chat at https://gitter.im/AlphaECS/Lobby](https://badges.gitter.im/AlphaECS/Lobby.svg)](https://gitter.im/AlphaECS/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Join the chat at https://gitter.im/grofit/ecsrx](https://badges.gitter.im/grofit/ecsrx.svg)](https://gitter.im/grofit/ecsrx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

AlphaECS is yet another Entity Component System framework for Unity that uses [UniRx](https://github.com/neuecc/UniRx) for fully reactive systems and includes support for dependency injection (we use [Zenject](https://github.com/modesttree/Zenject)). It's a fork of [EcsRx](https://github.com/grofit/ecsrx) and heavily inspired by [uFrame](https://github.com/uFrame/uFrame.github.io).

## Introduction
What follows is my own personal take on ECS based design. I try to keep it light and to the point, but I highly recommend taking a look around Google for more thorough explanations, as designing your code this way can take some getting used to.

Developing with Unity often centers around MonoBehaviours, which are a very special type of class that you can attach to your game objects. Let's imagine we want to create a player. We'll re-use some code from the [Unity Survival Shooter](https://unity3d.com/learn/tutorials/projects/survival-shooter-tutorial) tutorial:

```
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;
}
```

A simple enough way to get started. Next, we'll add some logic for setting up our current health when the player is created and also for taking damage:

```
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;

    void Awake ()
    {
        // Set the initial health of the player.
        currentHealth = startingHealth;
    }

    public void TakeDamage (int amount)
    {
        // Reduce the current health by the damage amount.
        currentHealth -= amount;
    }    
}
```

So far, so good. We've set up simple data for the player (startingHealth and currentHealth), and some methods to transform that data (setting it up in Awake() and removing it with TakeDamage()).

How about adding some special effects when our player gets hit, like playing a sound and decreasing a health bar? With Unity, this is very easy to do:

```
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;
    public Slider healthSlider;
    public AudioClip deathClip;

    void Awake ()
    {
        // Set the initial health of the player.
        currentHealth = startingHealth;
        playerAudio = GetComponent <AudioSource> ();
    }

    public void TakeDamage (int amount)
    {
        // Reduce the current health by the damage amount.
        currentHealth -= amount;

        // Set the health bar's value to the current health.
        healthSlider.value = currentHealth;

        // Play the hurt sound effect.
        playerAudio.Play ();
    }    
}
```

It's so easy to add effects like these, how about a cool red flash that is triggered when the player gets hit, then slowly fades out? We'll add some references to the flash effect image and color, a boolean to keep track of when the player gets damaged, and a timer to fade out the effect:

```
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;
    public Slider healthSlider;

    public Image damageImage;
    public float flashSpeed = 5f;
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);

    bool damaged;

    void Awake ()
    {
        // Setting up the references.
        playerAudio = GetComponent <AudioSource> ();

        // Set the initial health of the player.
        currentHealth = startingHealth;
    }


    void Update ()
    {
        // If the player has just been damaged...
        if(damaged)
        {
            // ... set the colour of the damageImage to the flash colour.
            damageImage.color = flashColour;
        }
        // Otherwise...
        else
        {
            // ... transition the colour back to clear.
            damageImage.color = Color.Lerp (damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }

        // Reset the damaged flag.
        damaged = false;
    }


    public void TakeDamage (int amount)
    {
        // Set the damaged flag so the screen will flash.
        damaged = true;

        // Reduce the current health by the damage amount.
        currentHealth -= amount;

        // Set the health bar's value to the current health.
        healthSlider.value = currentHealth;

        // Play the hurt sound effect.
        playerAudio.Play ();
    }     
}
```

Try not to get too hung up on the logic above. Just understand that our previously simple PlayerHealth class is now doing a few different things: managing health, playing sound fx, and triggering visual effects. Next, we'll add some extra effects for when the player dies, including playing a death animation and a death sound:

```
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;
    public Slider healthSlider;
    public Image damageImage;
    public AudioClip deathClip;
    public float flashSpeed = 5f;
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);

    Animator anim;
    AudioSource playerAudio;
    bool isDead;
    bool damaged;


    void Awake ()
    {
        // Setting up the references.
        anim = GetComponent <Animator> ();
        playerAudio = GetComponent <AudioSource> ();

        // Set the initial health of the player.
        currentHealth = startingHealth;
    }


    void Update ()
    {
        // If the player has just been damaged...
        if(damaged)
        {
            // ... set the colour of the damageImage to the flash colour.
            damageImage.color = flashColour;
        }
        // Otherwise...
        else
        {
            // ... transition the colour back to clear.
            damageImage.color = Color.Lerp (damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }

        // Reset the damaged flag.
        damaged = false;
    }


    public void TakeDamage (int amount)
    {
        // Set the damaged flag so the screen will flash.
        damaged = true;

        // Reduce the current health by the damage amount.
        currentHealth -= amount;

        // Set the health bar's value to the current health.
        healthSlider.value = currentHealth;

        // Play the hurt sound effect.
        playerAudio.Play ();

        // If the player has lost all it's health and the death flag hasn't been set yet...
        if(currentHealth <= 0 && !isDead)
        {
            // ... it should die.
            Death ();
        }
    }


    void Death ()
    {
        // Set the death flag so this function won't be called again.
        isDead = true;

        // Turn off any remaining shooting effects.
        playerShooting.DisableEffects ();

        // Tell the animator that the player is dead.
        anim.SetTrigger ("Die");

        // Set the audiosource to play the death clip and play it (this will stop the hurt sound from playing).
        playerAudio.clip = deathClip;
        playerAudio.Play ();
    }       
}
```

So far, this isn't *too* bad. We're at about 100 lines of code for our PlayerHealth class, and it's mostly doing things related to player health.

We'll take a break from our PlayerHealth and spend some time getting our player moving and shooting by creating a PlayerMovement class and a PlayerShooting class and slowly adding bits of logic similar to our PlayerHealth class. I won't go into the details of these classes, just trust we've now got 3 classes for dealing with the majority of our player logic. They're all nicely encapsulated from each other and contain their own data and methods for transforming that data.

But there's one little issue. When our player dies they can still shoot and move. Our neatly separate classes need to be tied together somehow. We need to disable moving and shooting when the player's health is less than or equal to 0. Where should this logic go? Should the movement and shooting logic be checking if the player is alive? Or should this health class be in charge of disabling movement and shooting? In the tutorial, PlayerHealth ended up being responsible:

```
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int startingHealth = 100;                            // The amount of health the player starts the game with.
    public int currentHealth;                                   // The current health the player has.
    public Slider healthSlider;                                 // Reference to the UI's health bar.
    public Image damageImage;                                   // Reference to an image to flash on the screen on being hurt.
    public AudioClip deathClip;                                 // The audio clip to play when the player dies.
    public float flashSpeed = 5f;                               // The speed the damageImage will fade at.
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);     // The colour the damageImage is set to, to flash.


    Animator anim;                                              // Reference to the Animator component.
    AudioSource playerAudio;                                    // Reference to the AudioSource component.
    PlayerMovement playerMovement;                              // Reference to the player's movement.
    PlayerShooting playerShooting;                              // Reference to the PlayerShooting script.
    bool isDead;                                                // Whether the player is dead.
    bool damaged;                                               // True when the player gets damaged.


    void Awake ()
    {
        // Setting up the references.
        anim = GetComponent <Animator> ();
        playerAudio = GetComponent <AudioSource> ();
        playerMovement = GetComponent <PlayerMovement> ();
        playerShooting = GetComponentInChildren <PlayerShooting> ();

        // Set the initial health of the player.
        currentHealth = startingHealth;
    }


    void Update ()
    {
        // If the player has just been damaged...
        if(damaged)
        {
            // ... set the colour of the damageImage to the flash colour.
            damageImage.color = flashColour;
        }
        // Otherwise...
        else
        {
            // ... transition the colour back to clear.
            damageImage.color = Color.Lerp (damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }

        // Reset the damaged flag.
        damaged = false;
    }


    public void TakeDamage (int amount)
    {
        // Set the damaged flag so the screen will flash.
        damaged = true;

        // Reduce the current health by the damage amount.
        currentHealth -= amount;

        // Set the health bar's value to the current health.
        healthSlider.value = currentHealth;

        // Play the hurt sound effect.
        playerAudio.Play ();

        // If the player has lost all it's health and the death flag hasn't been set yet...
        if(currentHealth <= 0 && !isDead)
        {
            // ... it should die.
            Death ();
        }
    }


    void Death ()
    {
        // Set the death flag so this function won't be called again.
        isDead = true;

        // Turn off any remaining shooting effects.
        playerShooting.DisableEffects ();

        // Tell the animator that the player is dead.
        anim.SetTrigger ("Die");

        // Set the audiosource to play the death clip and play it (this will stop the hurt sound from playing).
        playerAudio.clip = deathClip;
        playerAudio.Play ();

        // Turn off the movement and shooting scripts.
        playerMovement.enabled = false;
        playerShooting.enabled = false;
    }       
}
```

We added some references to PlayerMovement and PlayerShooting and when the Death() method gets called, we disable those scripts. However, the fact that we were even asking the question of "where should this logic go?" is telling: as your project grows larger, you'll be asking yourself this question more frequently, and the flow of logic in your game will become increasingly more complex.

We put this troubling thought to the back of our mind for the time being. Now that we've got a nice working player, we should probably give him some things to kill. And also create some things that can kill him, depleting his health and triggering all those cool effects we just spent so much time coding. We need *ENEMIES*. Here's a simplified version of the EnemyAttack class:

```
using UnityEngine;
using System.Collections;


public class EnemyAttack : MonoBehaviour
{
    public int attackDamage = 10;

    GameObject player;
    PlayerHealth playerHealth;
    EnemyHealth enemyHealth;

    void Awake ()
    {
        // Setting up the references.
        player = GameObject.FindGameObjectWithTag ("Player");
        playerHealth = player.GetComponent <PlayerHealth> ();
        enemyHealth = GetComponent<EnemyHealth>();
    }


    void OnTriggerEnter (Collider other)
    {
        // If the entering collider is the player...
        if(other.gameObject == player)
        {
          Attack();
        }
    }

    void Attack ()
    {
        // If the player has health to lose...
        if(playerHealth.currentHealth > 0)
        {
            // ... damage the player.
            playerHealth.TakeDamage (attackDamage);
        }
    }
}
```

This is manageable. The PlayerHealth class and EnemyAttack class are both fairly small. But there are already some issues with this code that will make things difficult should you try to scale this style of programming to a larger game.

In the Awake() method of EnemyAttack we're calling FindGameObjectWithTag("Player"). This will work just fine but will cause you major problems if you later decide to rename the "Player" tag to something else. A much better (but still ill-advised) way to approach this would be to search for a type instead of a string:

```
void Awake ()
{
    // Setting up the references.
    playerHealth = (PlayerHealth)FindObjectOfType (typeof(PlayerHealth));
    player = playerHealth.gameObject;
}
```

If you later decide to rename `PlayerHealth` to `Player`, the compiler can now at least help your sort things.

However, we're still left with at least one major issue here: dependencies. Remember our PlayerHealth, PlayerMovement, and PlayerShooting classes, where we tried encapsulating things away nicely and neatly into little methods and variables to modify, but ended up having to tie them together? We've done the same thing here with our `EnemyAttack` class.

For a game that won't grow any larger than the scope of the tutorial, this will work. But start adding any kind of complexity to your project and you'll quickly find yourself drowning in this stuff. You'll be checking boolean values trying to track what's alive and what's dead or injured or healing. And calling different methods and passing along different sets of data depending on what "state" you've found. And because you encapsulated everything so nicely you'll have halfway hidden all your dependencies and will be up at 5:00 am tracking down bugs through an endless beautiful chain of method calls.

So you go to Google and then things will get worse because everyone will have a different opinion or framework or style for dealing with this mess. MVVM! MVC! Procedural-functional ABCDEFG! I know because I've been there.

## A Way Out
In the Unity Shooter tutorial, we have a PlayerHealth class and an EnemyHealth class. They both do a lot of similar things. This is great, because when we find similar bits of code in our programs, that's usually a sign that we can simplify things.

The response of many a programmer in this situation, especially coming from an object-oriented background, might then be to create a base class where you keep all your shared logic, then branch out into PlayerHealth and EnemyHealth sub-classes and override the parts that are different. This feels good at first, but will lead to almost as many headaches later on.

Instead, we can stop thinking in terms of nice neat objects and inheritance chains, and start thinking in terms of breaking our game down into little pieces of data. Our new Health class can just be:

```
public class Health
{
	public int StartingHealth;
	public int CurrentHealth;
  public bool IsDead;
}
```

That's it. No logic, no methods, no inheritance, nothing. Instead, think about COMPOSING our player out of these core pieces of data. What other pieces of data might our player have? In addition to health, we want our player to be able to shoot:

```
public class Shooter
{
  public int Damage;
  public float ShotsPerSecond;
  public float Range;
  public BoolReactiveProperty IsShooting;
}
```

We also want our player to be able to move around, so we need some input data:

```
public class Input
{
  public float HorizontalInput; // left and right arrow keys
  public float VerticalInput; // up and down arrow keys
}
```

So our player has Health, Shooter, and Input, similar to our previous PlayerHealth, PlayerMovement, and PlayerShooting classes from before. Only now we've separated our data into small containers that don't do anything. So we'll need to create a few different systems that watch this data and react accordingly. This might seem overly complicated at first. In the Unity tutorial we had 3 fairly simple classes for health, movement, and shooting, and now we're splitting them off into 3 Health, Shooter, and Input data containers and also 3 different systems that will contain our methods. It's not entirely intuitive at first, but just stay with me a bit. We'll start with our HealthSystem:


```
public class HealthSystem
{
    private Health[] HealthComponents;

    void Start ()
    {
        // Setting up the references.
        HealthComponents = FindObjectsOfType(typeof(HealthComponent)) as HealthComponent[];

        // Set the initial health of each health component.
        foreach(HealthComponent healthComponent in HealthComponents)
        {
          healthComponent.CurrentHealth = healthComponent.StartingHealth;
        }
    }

    public void TakeDamage (HealthComponent healthComponent, int amount)
    {
        // Reduce the current health by the damage amount.
        healthComponent.CurrentHealth -= amount;

        // If the player has lost all it's health and the death flag hasn't been set yet...
        if(healthComponent.CurrentHealth <= 0 && !healthComponent.IsDead)
        {
            healthComponent.IsDead = true;
        }
        else if(healthComponent.CurrentHealth > 0 && healthComponent.IsDead)
        {
          healthComponent.IsDead = false;
        }
    }   
}
```

So our HealthSystem only deals with the core health data now, no special effects or other stuff. This might seem like making things overly complicated at first, as often when this kind of thing is done we haven't actually made the logic of our game simpler, we just end up hiding the complexity behind layers of encapsulation. But there are reasons for doing it here and it will pay off hugely in the long run.

One such reason is that your enemies and your players now share a whole bunch of logic. Anything in your game that you want to have health you can add a `Health` component to and it will "just work". It's hard to overstate the value of this. It's a very magical feeling to be able to start adding and removing components to game objects and seeing their behavior change in realtime. Want to add health to your NPCs? Just add a `Health` component. How about creating enemies that can shoot? Just add a `Shooting` component. When you separate your data from your system logic you can very easily start adding these kinds of things to your game, removing systems


```
using UnityEngine;

public class MovementSystem
{
    public float speed = 6f;            // The speed that the player will move at.

    Vector3 movement;                   // The vector to store the direction of the player's movement.
    Animator anim;                      // Reference to the animator component.
    Rigidbody playerRigidbody;          // Reference to the player's rigidbody.
    int floorMask;                      // A layer mask so that a ray can be cast just at gameobjects on the floor layer.
    float camRayLength = 100f;          // The length of the ray from the camera into the scene.

    void Awake ()
    {
        // Create a layer mask for the floor layer.
        floorMask = LayerMask.GetMask ("Floor");

        // Set up references.
        anim = GetComponent <Animator> ();
        playerRigidbody = GetComponent <Rigidbody> ();
    }


    void FixedUpdate ()
    {
        // Store the input axes.
        float h = Input.GetAxisRaw ("Horizontal");
        float v = Input.GetAxisRaw ("Vertical");

        // Move the player around the scene.
        Move (h, v);

        // Turn the player to face the mouse cursor.
        Turning ();

        // Animate the player.
        Animating (h, v);
    }

    void Move (float h, float v)
    {
        // Set the movement vector based on the axis input.
        movement.Set (h, 0f, v);

        // Normalise the movement vector and make it proportional to the speed per second.
        movement = movement.normalized * speed * Time.deltaTime;

        // Move the player to it's current position plus the movement.
        playerRigidbody.MovePosition (transform.position + movement);
    }

    void Turning ()
    {
        // Create a ray from the mouse cursor on screen in the direction of the camera.
        Ray camRay = Camera.main.ScreenPointToRay (Input.mousePosition);

        // Create a RaycastHit variable to store information about what was hit by the ray.
        RaycastHit floorHit;

        // Perform the raycast and if it hits something on the floor layer...
        if(Physics.Raycast (camRay, out floorHit, camRayLength, floorMask))
        {
            // Create a vector from the player to the point on the floor the raycast from the mouse hit.
            Vector3 playerToMouse = floorHit.point - transform.position;

            // Ensure the vector is entirely along the floor plane.
            playerToMouse.y = 0f;

            // Create a quaternion (rotation) based on looking down the vector from the player to the mouse.
            Quaternion newRotation = Quaternion.LookRotation (playerToMouse);

            // Set the player's rotation to this new rotation.
            playerRigidbody.MoveRotation (newRotation);
        }
    }

    void Animating (float h, float v)
    {
        // Create a boolean that is true if either of the input axes is non-zero.
        bool walking = h != 0f || v != 0f;

        // Tell the animator whether or not the player is walking.
        anim.SetBool ("IsWalking", walking);
    }
}
```


We create a simple container of data that our players AND our enemies will use. One way I like to think about it is that the game is basically a giant database, which you then filter at a higher level to create your special effects, control movement, spawn enemies, etc.

## Dependencies

- UniRx (required)
- Zenject (optional)

The **Core** framework only depends upon UniRx. The **Unity** helper classes and MonoBehaviours that bootstrap your scenes use Zenject, but feel free to create your own unity bridge to consume the core framework if you do not want the dependency.

## Quick Start

To feel comfortable with AlphaECS you'll want to be comfortable with a few different ideas:
- Entity Component System (ECS) patterns. If you're unfamiliar with these I suggest taking a quick look [here](http://www.gamedev.net/page/resources/_/technical/game-programming/understanding-component-entity-systems-r3013).
- Reactive programming. Here's a great article to get started: [The introduction to Reactive Programming you've been missing](https://gist.github.com/staltz/868e7e9bc2a7b8c1f754)
- Dependency injection. Again, we use [Zenject](https://github.com/modesttree/Zenject) and think their intro guide is pretty great.

In your Unity project:
- Install AlphaECS, UniRx, and Zenject
- Create a ProjectContext prefab and put it in a Resources folder. Add `ProjectContext`, `AlphaECSInstaller`, and `ProjectInstaller` components to the prefab and then add setup the installer references:

![image](https://cloud.githubusercontent.com/assets/6376639/20701079/fb9243da-b64b-11e6-99ab-0c9b869305a8.png)

 - Add a `SceneContext` and `SceneInstaller` to the root of your scene and setup the installer references:

 ![image](https://cloud.githubusercontent.com/assets/6376639/20701169/773484e4-b64c-11e6-9e36-fc218bc45cc0.png)


This setup accomplishes a few things. First, when you hit play, AlphaECSInstaller will setup the core systems of the framework for you automatically. Then, project installer will setup the **game specific** systems you've added as prefabs to the Resources/Kernel folder (think InputSystem, SaveSystem, MultiplayerSystem, SceneTransitionSystem, etc) as single instances and marks them as DontDestroyOnLoad. More on how to set these up in the paragraph below. Finally, the scene installer will look for any **scene specific** systems that exist in the scene  (think EnemySystem, PowerUpSystem, ShootingSystem, CameraSystem, etc) and bind them as single instances. Of course, if you're comfortable with code you can skip all of this and implement your own bootstrapping method.

There is one optional system included with the framework that allows you to take full advantage of the Unity Editor to compose your entities. It is the EntityBehaviourSystem. To add this to your project, create a new folder under `Resources` called `Kernel`, then create a new prefab there and attach the `EntityBehaviourSystem` component included as one of the **Unity** helper classes.

 ## Example Project
 - [Survival Shooter](https://github.com/tbriley/AlphaECS.SurvivalShooter)


## HEADS UP

This was not designed with performance in mind. However, it should be performant enough for most scenarios, and given its reactive nature and decoupled design you can easily replace implementations at will. Lots of people love performance metrics, but I have none and have put performance secondary to functionality.
