using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BubbleControllerFloating : MonoBehaviour
{
    private XRInteractionManager _interactionManager;
    private void Awake()
    {
        _interactionManager = FindObjectOfType<XRInteractionManager>();
        if (_interactionManager == null)
        {
            Debug.LogError("XRInteractionManager not found in the scene!");
        }
    }


    [SerializeField] private Bubble _bubblePrefab;
    [SerializeField] private float _speed;
    [SerializeField] private float _spawnRate;
    [SerializeField] private float _spawnRange;
    [SerializeField] private int _burstAmount;
    [SerializeField] private int _burstDelay;

    [SerializeField] public SealAnimationRunner _sealAnimationRunner;
    [SerializeField] private SpriteFader _otherImagesDisplay;

    [SerializeField] private Transform _bubbleDestinationOnSelect;
    
    [SerializeField] private ContentProvider _contentProvider;
    [SerializeField] private ObjectInteraction _objectInteraction;
    private List<int> _museumObjectIdsCurrentlyInBubbles = new();

    private int _burstCount;

    private List<Bubble> _bubbles = new ();
    private float _timer = 0;

    public void UpdateBubbles()
    { 
        _timer += Time.deltaTime;
        if (_burstCount > _burstAmount)
        {
            if (_timer > _burstDelay)
            {
                _timer = 0;
                _burstCount = 0;
            }
            return;
        }
        if (_timer > 1/_spawnRate)
        {
            _timer -= 1/_spawnRate;
            SpawnBubble();
            _burstCount++;
        }
    }

    public void SelectBubble(int bubble_id)
    {
        Debug.Log("SelectBubble method called for ID: " + bubble_id);
        Bubble selectedBubble = _bubbles.Find(bubble => bubble.Id == bubble_id);
        if (selectedBubble == null)
        {
            SpawnSpecificBubble(bubble_id);
            selectedBubble = _bubbles.Find(bubble => bubble.Id == bubble_id);
        }
        
        List<Bubble> bubblesCopy = new List<Bubble>(_bubbles);
        foreach (Bubble bubble in bubblesCopy)
        {
            if (bubble == selectedBubble)
            {
                bubble.OnSelectionComplete += TransitionToShowcase;
                bubble.OnSelect(_bubbleDestinationOnSelect.position);
            }
            else bubble.OnOtherSelected();
        }
    }

    public void EnterBubbleProducingState()
    {
        _otherImagesDisplay.StopFadingSprites();
        if (_objectInteraction != null) _objectInteraction.ResetAndDisable();
        List<Bubble> tempBubbles = new(_bubbles);
        foreach (Bubble bubble in tempBubbles)
        {
            RemoveBubble(bubble);
        }
        
        _timer = 0;
        _burstCount = 0;
    }

    private void TransitionToShowcase(Bubble bubble)
    {
        bubble.OnSelectionComplete -= TransitionToShowcase;
        _objectInteraction.SetTarget(bubble.ContainedObject.transform);
        _otherImagesDisplay.StartFadingSprites(_contentProvider.MuseumObjectSOs[bubble.Id].OtherImages);
    }

    private void RemoveBubble(Bubble bubble)
    {
        bubble.OnBubbleShouldBeDestroyed -= RemoveBubble;
        _museumObjectIdsCurrentlyInBubbles.Remove(bubble.Id);
        _bubbles.Remove(bubble);
        Destroy(bubble.gameObject);
    }
    
    private void HandleBubbleSelectedVR(SelectEnterEventArgs args)
    {
        Debug.Log("<color=cyan>VR Select event triggered on:</color> " + args.interactableObject.transform.name);
        Bubble bubble = args.interactableObject.transform.GetComponent<Bubble>();
        if (bubble != null)
        {
            SelectBubble(bubble.Id);
        }
    }

    private void EnsureInteractableAndWireEvents(Bubble bubble)
    {
        if (bubble == null) return;
        // Ensure a Collider exists for interaction
        var sphere = bubble.GetComponent<SphereCollider>();
        if (sphere != null) sphere.enabled = true;

        // Ensure an XRSimpleInteractable exists and wire up selection events
        var interactable = bubble.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = bubble.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }
        interactable.interactionManager = _interactionManager;
        // Avoid duplicate bindings
        interactable.selectEntered.RemoveListener(HandleBubbleSelectedVR);
        interactable.selectEntered.AddListener(HandleBubbleSelectedVR);
        interactable.enabled = true;
    }

    private void SpawnBubble()
    {
        MuseumObjectSO museumObjectSO = GetMuseumObjectNotAlreadyInABubble();
        if(museumObjectSO == null) return;
        
        Bubble bubble = Instantiate(_bubblePrefab, transform);
        _museumObjectIdsCurrentlyInBubbles.Add(museumObjectSO.Id);
        _bubbles.Add(bubble);
        
        bubble.transform.position = new Vector3(Random.Range(-_spawnRange,_spawnRange), transform.position.y, 0);
        bubble.Initialize(_speed, museumObjectSO);
        bubble.OnBubbleShouldBeDestroyed += RemoveBubble;
        
        EnsureInteractableAndWireEvents(bubble);
    }
    
    private void SpawnSpecificBubble(int id)
    {
        MuseumObjectSO museumObjectSO = _contentProvider.MuseumObjectSOs[id];
        if(museumObjectSO == null) return;
        
        Bubble bubble = Instantiate(_bubblePrefab, transform);
        _museumObjectIdsCurrentlyInBubbles.Add(museumObjectSO.Id);
        _bubbles.Add(bubble);
        
        bubble.transform.localPosition = new Vector3(Random.Range(-_spawnRange,_spawnRange), transform.position.y, 0);
        bubble.Initialize(_speed, museumObjectSO, true);
        bubble.OnBubbleShouldBeDestroyed += RemoveBubble;

        EnsureInteractableAndWireEvents(bubble);
    }

    private static int _museumObjectIdIterator;
    private MuseumObjectSO GetMuseumObjectNotAlreadyInABubble()
    {
        // If all possible objects are already in bubbles, don't spawn a new one.
        if (_museumObjectIdsCurrentlyInBubbles.Count >= _contentProvider.MuseumObjectSOs.Count)
        {
            return null;
        }

        if (_contentProvider.MaximumId < 0) return null;

        // Loop indefinitely until a valid, non-bubbled object is found.
        // This is safer than the previous complex for-loop.
        while (true)
        {
            // If the iterator goes past the end of the list, wrap it back to the beginning.
            if (_museumObjectIdIterator > _contentProvider.MaximumId)
            {
                _museumObjectIdIterator = 0;
            }

            // Check if the object with the current ID is NOT already in a bubble.
            if (!_museumObjectIdsCurrentlyInBubbles.Contains(_museumObjectIdIterator))
            {
                // Found a valid one, return it and move the iterator for the next call.
                return _contentProvider.MuseumObjectSOs[_museumObjectIdIterator++];
            }
            
            // If the current ID is already taken, just check the next one.
            _museumObjectIdIterator++;
        }
    }
}
