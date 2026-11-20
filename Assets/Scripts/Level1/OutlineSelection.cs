using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OutlineSelection : MonoBehaviour
{
    private Transform highlight;
    private Transform selection;
    private RaycastHit raycastHit;
    private TutorialFlow tutorialFlow;

    void Update()
    {
        if (tutorialFlow == null)
            tutorialFlow = FindFirstObjectByType<TutorialFlow>();

        // Highlight
        if (highlight != null)
        {
            highlight.gameObject.GetComponent<Outline>().enabled = false;
            highlight = null;
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out raycastHit)) //Make sure you have EventSystem in the hierarchy before using EventSystem
        {
            highlight = raycastHit.transform;
            bool isPiece = highlight.CompareTag(tutorialFlow != null ? "Peca" : "peca");
            if (isPiece && highlight != selection &&
                (tutorialFlow == null || tutorialFlow.CanInteract(highlight.gameObject)))
            {
                if (highlight.gameObject.GetComponent<Outline>() != null)
                {
                    highlight.gameObject.GetComponent<Outline>().enabled = true;
                }
                else
                {
                    Outline outline = highlight.gameObject.AddComponent<Outline>();
                    outline.enabled = true;
                    highlight.gameObject.GetComponent<Outline>().OutlineColor = Color.green;
                    highlight.gameObject.GetComponent<Outline>().OutlineWidth = 7.0f;
                }
            }
            else
            {
                highlight = null;
            }
        }

        // Selection
        if (Input.GetMouseButtonDown(0))
        {
            if (highlight)
            {
                if (selection != null)
                {
                    Outline selectedOutline = selection.gameObject.GetComponent<Outline>();
                    if (selectedOutline != null)
                        selectedOutline.enabled = false;
                }
                selection = raycastHit.transform;
                selection.gameObject.GetComponent<Outline>().enabled = true;
                highlight = null;
            }
            else
            {
                if (selection)
                {
                    Outline selectedOutline = selection.gameObject.GetComponent<Outline>();
                    if (selectedOutline != null)
                        selectedOutline.enabled = false;
                    selection = null;
                }
            }
        }
    }

}
