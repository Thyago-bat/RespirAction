using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Guides the player through the first valid diaphragm piece.</summary>
public class TutorialFlow : MonoBehaviour
{
    private enum Step { SelectSternal, DragSternal, PlaceSternal, RotateSternal, ValidateSternal, Completing, Complete }

    private const int SelectionDialogueIndex = 5;
    private const int DragDialogueIndex = 6;
    private const int RotationDialogueIndex = 7;
    private const int ValidationDialogueIndex = 8;
    private readonly Dictionary<string, GameObject> pieces = new Dictionary<string, GameObject>();
    private readonly List<Move3D> allPieceMoves = new List<Move3D>();
    private readonly Dictionary<Move3D, Collider> pieceColliders = new Dictionary<Move3D, Collider>();
    private readonly Dictionary<Move3D, bool> originalColliderStates = new Dictionary<Move3D, bool>();
    private LevelManager levelManager;
    private Dialogue dialogue;
    private GameObject sternal;
    private Step step;
    private bool pressedLeft;
    private bool pressedRight;
    private bool pointerStartedOnSternal;
    private bool draggedSternal;
    private bool feedbackShowing;
    private Vector3 pointerStart;
    private int dialogueRevision;
    private Coroutine feedbackRoutine;

    private void Start()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
        dialogue = FindFirstObjectByType<Dialogue>();

        // Find every draggable piece directly; this does not depend on tag or enumeration order.
        foreach (Move3D move in FindObjectsByType<Move3D>(FindObjectsSortMode.None))
        {
            if (move == null)
                continue;

            allPieceMoves.Add(move);
            if (!pieces.ContainsKey(move.gameObject.name))
                pieces.Add(move.gameObject.name, move.gameObject);
        }

        if (levelManager == null || dialogue == null || !pieces.TryGetValue("Esternal", out sternal))
        {
            Debug.LogError("TutorialFlow: faltam LevelManager, Dialogue ou a peça Esternal.", this);
            enabled = false;
            return;
        }

        foreach (Move3D move in allPieceMoves)
        {
            bool isSternal = move.gameObject == sternal;
            move.enabled = isSternal;

            Collider pieceCollider = move.GetComponent<Collider>();
            if (pieceCollider != null)
            {
                pieceColliders[move] = pieceCollider;
                originalColliderStates[move] = pieceCollider.enabled;
                if (!isSternal)
                    pieceCollider.enabled = false;
            }
        }

        if (TimeManager.instance != null)
            TimeManager.instance.SetRunning(false);

        StartCoroutine(BeginAfterSceneStart());
    }

    private IEnumerator BeginAfterSceneStart()
    {
        yield return null;
        step = Step.SelectSternal;
        pressedLeft = false;
        pressedRight = false;
        pointerStartedOnSternal = false;
        draggedSternal = false;
        dialogue.ResetForTutorial();
        ShowStepLine(SelectionDialogueIndex);
    }

    private void Update()
    {
        if (step == Step.Complete || step == Step.Completing)
            return;

        TrackSelectionAndDrag();

        if (step == Step.RotateSternal)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) pressedLeft = true;
            if (Input.GetKeyDown(KeyCode.RightArrow)) pressedRight = true;
            // Basta demonstrar a rotação em um dos sentidos; o jogador não precisa
            // pressionar as duas setas para avançar para a validação.
            if (pressedLeft || pressedRight)
            {
                step = Step.ValidateSternal;
                ShowStepLine(ValidationDialogueIndex);
            }
        }

        if (step == Step.ValidateSternal && IsSternalGreen())
            CompleteTutorial();
    }

    private void TrackSelectionAndDrag()
    {
        if (feedbackShowing)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            pointerStartedOnSternal = false;
            draggedSternal = false;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Move3D move = hit.collider.GetComponentInParent<Move3D>();
                if (move != null && move.gameObject == sternal && move.enabled)
                {
                    pointerStartedOnSternal = true;
                    pointerStart = Input.mousePosition;
                    if (step == Step.SelectSternal)
                    {
                        step = Step.DragSternal;
                        ShowStepLine(DragDialogueIndex);
                    }
                }
            }
        }

        if (pointerStartedOnSternal && Input.GetMouseButton(0) &&
            Vector3.Distance(pointerStart, Input.mousePosition) > 4f)
            draggedSternal = true;

        if (Input.GetMouseButtonUp(0))
        {
            if (pointerStartedOnSternal && draggedSternal && step == Step.DragSternal)
            {
                // Aguarda a peça encaixar e exibe o retorno correspondente antes
                // de orientar a rotação.
                step = Step.PlaceSternal;
            }

            pointerStartedOnSternal = false;
        }
    }

    private bool IsSternalGreen()
    {
        OutlineManager outline = sternal.GetComponent<OutlineManager>();
        Move3D move = sternal.GetComponent<Move3D>();
        Outline visualOutline = sternal.GetComponent<Outline>();
        return outline != null && outline.correctSnap != null && move != null &&
            move.currentSnap == outline.correctSnap && visualOutline != null &&
            visualOutline.enabled && visualOutline.OutlineColor == Color.green &&
            levelManager.IsPieceCorrect(sternal, outline.correctSnap, 0.1f, 25f) == 2;
    }

    private void CompleteTutorial()
    {
        step = Step.Completing;
        StartCoroutine(ShowCompletionSequence());
    }

    private IEnumerator ShowCompletionSequence()
    {
        dialogue.ShowTutorialLine(0, false);
        yield return new WaitUntil(() => dialogue.IsIdle);

        dialogue.ShowTutorialLine(9, false);
        yield return new WaitUntil(() => dialogue.IsIdle);

        dialogue.ShowTutorialLine(10, false);
        yield return new WaitUntil(() => dialogue.IsIdle);

        foreach (Move3D move in allPieceMoves)
        {
            move.enabled = true;

            if (pieceColliders.TryGetValue(move, out Collider pieceCollider) &&
                originalColliderStates.TryGetValue(move, out bool wasEnabled))
                pieceCollider.enabled = wasEnabled;
        }

        if (TimeManager.instance != null)
            TimeManager.instance.SetRunning(true);

        if (levelManager.audioManager != null)
        {
            AudioManagerScene audio = levelManager.audioManager.GetComponent<AudioManagerScene>();
            if (audio != null)
                audio.PlaySound(2);
        }

        step = Step.Complete;
    }

    public void ShowPlacementFeedback(int dialogueIndex)
    {
        if (step == Step.Completing || step == Step.Complete)
        {
            dialogue.ShowTutorialLine(dialogueIndex, false);
            return;
        }

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackShowing = true;
        int revision = ++dialogueRevision;
        feedbackRoutine = StartCoroutine(ShowPlacementFeedbackAndResume(dialogueIndex, revision));
    }

    private IEnumerator ShowPlacementFeedbackAndResume(int dialogueIndex, int revision)
    {
        dialogue.ShowTutorialLine(dialogueIndex, false);
        yield return new WaitUntil(() => dialogue.IsIdle || revision != dialogueRevision);

        if (revision == dialogueRevision && step != Step.Complete && step != Step.Completing)
        {
            feedbackShowing = false;
            if (dialogueIndex == 11)
            {
                step = Step.DragSternal;
            }
            else if (dialogueIndex == 12)
            {
                step = Step.RotateSternal;
                ShowStepLine(RotationDialogueIndex);
            }
            else
            {
                ShowStepLine(GetInstructionIndex());
            }
        }

        feedbackRoutine = null;
    }

    private void ShowStepLine(int dialogueIndex)
    {
        dialogueRevision++;
        feedbackShowing = false;
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }
        dialogue.ShowTutorialLine(dialogueIndex);
    }

    private int GetInstructionIndex()
    {
        switch (step)
        {
            case Step.SelectSternal: return SelectionDialogueIndex;
            case Step.DragSternal:
            case Step.PlaceSternal: return DragDialogueIndex;
            case Step.RotateSternal: return RotationDialogueIndex;
            default: return ValidationDialogueIndex;
        }
    }

    public bool CanRotate(GameObject piece)
    {
        Move3D move = piece != null ? piece.GetComponent<Move3D>() : null;
        if (move == null && piece != null)
            move = piece.GetComponentInParent<Move3D>();

        if (move == null || !move.enabled)
            return false;

        if (step == Step.Complete)
            return true;

        return step == Step.RotateSternal || step == Step.ValidateSternal
            ? move.gameObject == sternal
            : false;
    }

    public bool CanInteract(GameObject piece)
    {
        Move3D move = piece != null ? piece.GetComponent<Move3D>() : null;
        if (move == null && piece != null)
            move = piece.GetComponentInParent<Move3D>();

        if (move == null || !move.enabled)
            return false;

        if (feedbackShowing)
            return false;

        return step == Step.Complete || move.gameObject == sternal;
    }
}
