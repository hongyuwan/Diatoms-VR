using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Rendering;

/// <summary>
/// NOTE: THIS IS A TEMPORARY MOCKUP CLASS. This is not to be built off of and should be redone once design is finalized.
/// </summary>
public class Bubble : MonoBehaviour
{
	public Transform ObjectContainer; // Add this
	public GameObject ContainedObject { get; private set; } // Add this

	public void Initialize(float speed, MuseumObjectSO museumObject, bool start_at_full_size = false)
	{
		Speed = speed + Random.Range(-0.2f,0.2f);
		ContainedObject = Instantiate(museumObject.ObjectPrefab);

		ContainedObject.transform.SetParent(ObjectContainer, false);
		ContainedObject.transform.localPosition = Vector3.zero;
		ContainedObject.transform.localRotation = Quaternion.identity;
		
		// --- New Scaling Logic ---
		var renderers = ContainedObject.GetComponentsInChildren<Renderer>();
		if (renderers.Length > 0)
		{
			Bounds bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
			foreach (Renderer renderer in renderers)
			{
				bounds.Encapsulate(renderer.bounds);
			}

			float maxEdge = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

			if (maxEdge > 0)
			{
				float scaleFactor = museumObject.TargetSizeInBubble / maxEdge;
				ContainedObject.transform.localScale = Vector3.one * scaleFactor;
			}
			else
			{
				ContainedObject.transform.localScale = Vector3.one;
			}
		}
		else
		{
			ContainedObject.transform.localScale = Vector3.one;
		}
		// --- End of Scaling Logic ---
		
		Id = museumObject.Id;
		IdText.text = Id.ToString();
		
		_initialPosition = transform.localPosition;
		_finalPosition = _initialPosition;
		_initialScale = transform.localScale;
		_startTime = Time.time;
		_bubblePlayedSpawnAnimation = false;

		var sphereCollider = GetComponent<SphereCollider>();
		if(sphereCollider != null)
			_bubbleRadius = sphereCollider.radius * transform.localScale.x;

		if(start_at_full_size) return;
		transform.localPosition = Vector3.zero;
		transform.localScale = Vector3.zero;

		// To ensure the 3D model inside the bubble renders correctly on top of other transparent objects (like the background),
		// we adjust its material's render queue. A higher value means it's rendered later (more on top).
		// The default for transparent geometry is 3000. The seal is at 3001, so we use 3002.
		foreach (var r in ContainedObject.GetComponentsInChildren<Renderer>())
		{
			if (r.material != null)
			{
				r.material.renderQueue = 3002;
			}
		}
	}
	
	private enum State
	{
		FLOAT,
		SELECTED,
		NOT_SELECTED
	}

	private State _state = State.FLOAT;
	
	public Action<Bubble> OnSelectionComplete;
	public Action<Bubble> OnBubbleShouldBeDestroyed;

	public int Id; // Corresponds to the Id of the object pictured in the bubble
	// public SpriteScaler MuseumObjectSpriteRenderer; // Remove or comment out this line
	public TMP_Text IdText;
	public ParticleSystem BubblePop;
	public SpriteRenderer BubbleSpriteRenderer;
	
	public float Speed = 1.0f; // Controls the vertical floating speed
	public float NoiseScale = 4f; // Controls the scale of the Perlin noise
	public float NoiseIntensity = 1f; // Controls the strength of noise movement
	public float CollisionForceWithOtherBubbles = 2f;

	private Vector3 _initialPosition;
	private Vector3 _initialScale;
	private Vector3 _finalPosition;
	private float _randomOffset;
	private float _startTime;
	private bool _bubblePlayedSpawnAnimation = false;
	private float _bubbleRadius;
	
	void Update()
	{
		if (_state != State.FLOAT) return;
		if (!_bubblePlayedSpawnAnimation)
			SpawnBubbleAnimation();
		MoveBubble();
		if (Camera.main.WorldToScreenPoint(transform.position).y > Screen.height + 250)
		{
			OnBubbleShouldBeDestroyed?.Invoke(this);
		}
	}

	private float _timeInSpawnAnimation = 0;

	private void SpawnBubbleAnimation()
	{
		_timeInSpawnAnimation += Time.deltaTime /2f;
		transform.localScale = Vector3.Lerp(Vector3.zero, _initialScale, _timeInSpawnAnimation*2);
		// if (_timeInSpawnAnimation < 0.5f) transform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.left, _timeInSpawnAnimation*2);
		// else 
			transform.localPosition = Vector3.Lerp(Vector3.left*1.2f, _finalPosition, (_timeInSpawnAnimation * 1.5f - 0.5f));
		_initialPosition = transform.localPosition;
		
		if (_timeInSpawnAnimation >= 1) _bubblePlayedSpawnAnimation = true;
	}

	private void MoveBubble()
	{
		// Calculate the new Y position with upward movement
		float newY = _initialPosition.y + (Speed * (Time.time - _startTime));

		// Apply Perlin noise to X and Z for subtle horizontal drift
		float noiseX = Mathf.PerlinNoise((Time.time - _startTime) * NoiseScale + _randomOffset, 0) - 0.5f;
		
		// Update bubble position
		transform.localPosition = new Vector3(
			_initialPosition.x + noiseX * NoiseIntensity,
			newY, 0
		);
	}
	
	/// <summary>
	/// This method is for the XR Interaction Toolkit. It should be called by the 'On Select Entered' event
	/// from an XR Interactable component (e.g., XR Grab Interactable) on this GameObject.
	/// The old OnMouseDown method is removed as it's not compatible with the XR input system.
	/// </summary>
	public void HandleSelectEntered()
	{
		if (_state == State.FLOAT)
		{
			// The original script had an OnBubbleClicked event here, but the controller
			// now handles the selection directly via HandleBubbleSelectedVR.
			// So, we can leave this empty or add a log for debugging.
			Debug.Log($"HandleSelectEntered called on {gameObject.name}", this);
		}
	}

	public async void OnSelect(Vector3 destination)
	{
		_state = State.SELECTED;
		transform.localScale = _initialScale;
		
		// Wait one frame before disabling colliders, allowing XRI to finish its state management.
		await Task.Yield();
		
		// Disable bubble's own interaction to free the raycast for the contained object
		var sphere = GetComponent<SphereCollider>();
		if (sphere) sphere.enabled = false;
		var simple = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
		if (simple) simple.enabled = false;
		
		//Disable regular update/switch states
		

		float time = 0;
		while (time < 3)
		{
			time += Time.deltaTime;
			if ((transform.position - destination).magnitude < 0.01f) break;
			if (this == null) return;
			transform.position = Vector3.Lerp(transform.position, destination, time/3);
			await Task.Yield();
			if (_state == State.NOT_SELECTED) //If a new bubble was selected, return
				return;
		}
		//Play pop animation or something when reaching center
			//Omitted in mockup
		//Trigger event for center reached
		OnSelectionComplete.Invoke(this);
	}

	public async void OnOtherSelected()
	{
		_state = State.NOT_SELECTED;
		
		// Wait one frame before disabling colliders
		await Task.Yield();
		
		var sphere = GetComponent<SphereCollider>();
		if (sphere) sphere.enabled = false;
		var simple = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
		if (simple) simple.enabled = false;
		
		//Get the direction to the center, and move in the opposite of that direction
		Vector3 directionToCenter = (Camera.main.ScreenToWorldPoint(new Vector3(Screen.width/2f,Screen.height/2f,0)) - transform.position);
		directionToCenter.z = 0;
		directionToCenter = directionToCenter.normalized;
		
		float time = 0;
		while (time < 3)
		{
			time += Time.deltaTime;
			if (this == null) return;
			transform.position -= directionToCenter * 4f *Time.deltaTime;
			await Task.Yield();
			if (_state == State.SELECTED) //if it got selected while moving away, don't destroy it
				return;
		}
		//Destroy when off screen
		OnBubbleShouldBeDestroyed?.Invoke(this);
	}

	private void OnCollisionStay(Collision other)
	{
		if (_state != State.FLOAT) return;
		//Move away from other bubble
		Vector3 direction = new Vector2(transform.position.x, transform.position.y) - new Vector2(other.contacts[0].point.x, other.contacts[0].point.y);
		direction.y *= 0.2f; // favour horizontal movement
		direction.Normalize();
		float distance = Vector3.Distance(new Vector2(transform.position.x, transform.position.y), other.contacts[0].point);
		_initialPosition += direction * CollisionForceWithOtherBubbles * Time.deltaTime * (_bubbleRadius - distance);
	}

	public void Pop()
	{
		GetComponentInChildren<SpriteMask>().enabled = false;
		// MuseumObjectSpriteRenderer.SpriteRenderer.maskInteraction = SpriteMaskInteraction.None; // Remove or comment out
		if(ContainedObject != null) ContainedObject.SetActive(false); // Add this
		BubbleSpriteRenderer.enabled = false;
		BubblePop.Play();
		IdText.enabled = false;
	}
}
