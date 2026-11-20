using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public class Dialogue : MonoBehaviour
{
    [Header("Falas (cada elemento aponta para uma chave da String Table)")]
    public LocalizedString[] dialogue;

    public int dialogueIndex;
    public GameObject dialoguePanel;
    public Text dialogueText;
    public bool startDialogue = false;
    public bool readyDialogue = false;

    private struct LineRequest
    {
        public int index;
        public bool hold;

        public LineRequest(int index, bool hold)
        {
            this.index = index;
            this.hold = hold;
        }
    }

    private readonly Queue<LineRequest> pendingLines = new Queue<LineRequest>();
    private Animator dialoguePanelAnimator;
    private Coroutine presentationRoutine;
    private string currentLine;
    private bool holdCurrentLine;
    private bool hideRequested;
    private bool isTyping;

    public bool IsDialogueVisible => dialoguePanel != null && dialoguePanel.activeSelf;
    public bool IsTyping => isTyping;
    public bool IsIdle => presentationRoutine == null && pendingLines.Count == 0 &&
        !IsDialogueVisible && !readyDialogue;

    private void Start()
    {
        if (dialoguePanel == null)
        {
            Debug.LogError("Dialogue: dialoguePanel não está configurado.", this);
            enabled = false;
            return;
        }

        dialoguePanelAnimator = dialoguePanel.GetComponent<Animator>();
        if (dialoguePanelAnimator == null)
            Debug.LogError("Componente Animator não encontrado no dialoguePanel.", this);

        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        // Mantém compatibilidade com componentes que ainda usam os campos públicos.
        if (readyDialogue)
        {
            int index = dialogueIndex;
            readyDialogue = false;
            EnqueueLine(index, false);
        }
    }

    public void ShowTutorialLine(int index, bool hold = true)
    {
        EnqueueLine(index, hold);
    }

    private void EnqueueLine(int index, bool hold)
    {
        if (dialogue == null || index < 0 || index >= dialogue.Length || dialogue[index].IsEmpty)
        {
            Debug.LogError("Dialogue: índice de fala inválido ou sem localização.", this);
            return;
        }

        pendingLines.Enqueue(new LineRequest(index, hold));
        if (presentationRoutine == null)
            presentationRoutine = StartCoroutine(PresentQueuedLines());
    }

    private IEnumerator PresentQueuedLines()
    {
        while (pendingLines.Count > 0)
        {
            LineRequest request = pendingLines.Dequeue();

            if (IsDialogueVisible)
            {
                // Uma fala tutorial fica visível até a ação seguinte. Falas comuns
                // permanecem por três segundos depois da digitação, como antes.
                if (holdCurrentLine)
                {
                    while (holdCurrentLine && pendingLines.Count == 0)
                        yield return null;
                }
                else if (!hideRequested)
                {
                    yield return new WaitForSeconds(3f);
                }

                yield return HideCurrentLine();
            }

            dialogueIndex = request.index;
            currentLine = dialogue[request.index].GetLocalizedString();
            holdCurrentLine = request.hold;
            hideRequested = false;
            startDialogue = true;
            readyDialogue = false;
            dialoguePanel.SetActive(true);

            if (dialogueText == null)
            {
                Debug.LogError("Dialogue: dialogueText não está configurado.", this);
                dialoguePanel.SetActive(false);
                startDialogue = false;
                continue;
            }

            yield return TypeCurrentLine();

            if (request.hold)
            {
                while (holdCurrentLine && pendingLines.Count == 0)
                    yield return null;
            }
            else if (!hideRequested)
            {
                yield return new WaitForSeconds(3f);
            }

            yield return HideCurrentLine();
        }

        presentationRoutine = null;
    }

    private IEnumerator TypeCurrentLine()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in currentLine)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.025f);
        }

        isTyping = false;
    }

    private IEnumerator HideCurrentLine()
    {
        if (dialoguePanelAnimator != null)
            dialoguePanelAnimator.Play("StartFadeOut");

        yield return new WaitForSeconds(0.4f);
        dialoguePanel.SetActive(false);
        startDialogue = false;
        readyDialogue = false;
        holdCurrentLine = false;
        hideRequested = false;
    }

    public void ResetForTutorial()
    {
        StopAllCoroutines();
        presentationRoutine = null;
        pendingLines.Clear();
        dialogueIndex = 0;
        startDialogue = false;
        readyDialogue = false;
        holdCurrentLine = false;
        hideRequested = false;
        isTyping = false;

        if (dialogueText != null)
            dialogueText.text = "";
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public void HideTutorialLine()
    {
        holdCurrentLine = false;
        hideRequested = true;
        readyDialogue = false;

        if (presentationRoutine == null && IsDialogueVisible)
            presentationRoutine = StartCoroutine(HideWithoutQueue());
    }

    private IEnumerator HideWithoutQueue()
    {
        yield return HideCurrentLine();
        presentationRoutine = null;
    }
}
