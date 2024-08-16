using System;
using System.Collections.Generic;
using UnityEngine;

// Allows us to select and command Units (GameObjects on the Unit collisionLayer that implement IMoveCommandable) to move to specific locations!

public class MouseUnitCommand : TargetingSystem
{
    public enum CommandMode
    {
        Move,
        Attack
    }

    Camera mainCamera;

    [SerializeField]
    Canvas screenspaceCanvas;

    [SerializeField]
    List<RadarMarkerUI> targetMarkerPool = new List<RadarMarkerUI>();

    [SerializeField]
    GameObject targetMarkerPrefab;

    [SerializeField]
    LayerMask selectMask;

    [SerializeField]
    LayerMask moveCommandMask;

    [SerializeField]
    WorldspaceMarker cursor;

    [SerializeField]
    ParticleSystem moveCommandResponseAnim;

    [SerializeField]
    AudioSource commandAudioSource;

    [SerializeField]
    PlayerUIConfig playerUIConfig;

    //clicking and dragging over to select in a box
    bool isBoxSelecting = false;

    CommandMode currentCommandMode = CommandMode.Move;

    //Holding shift for selecting multiple individual targets or queueing movement
    bool isModifierHeld = false;

    private Vector3 selectionStart;
    private Vector3 selectionEnd;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    void EnterMoveCommandState()
    {
        if (currentCommandMode != CommandMode.Move)
        {
            currentCommandMode = CommandMode.Move;
            cursor.GetComponent<Renderer>().material.SetColor("_Color", new Color(0.0f, 1.0f, 0.0f, 0.1f));
        }
    }

    void EnterAttackCommandState()
    {
        if (currentCommandMode != CommandMode.Attack)
        {
            currentCommandMode = CommandMode.Attack;
            cursor.GetComponent<Renderer>().material.SetColor("_Color", new Color(1.0f, 0.0f, 0.0f, 0.1f));
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();

        UpdateRadarInfo();
    }

    void HandleInput()
    {
        isModifierHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        Vector2 mousePos = Input.mousePosition;
        Ray pointerRay = mainCamera.ScreenPointToRay(mousePos);
        RaycastHit hitInfo;

        string actionName = "";
        Unit hoveringOver = null;
        bool validMoveSpot = false;
        //cursor.gameObject.SetActive(false);


        RaycastHit groundHit;
        validMoveSpot = Physics.Raycast(pointerRay.origin, pointerRay.direction, out groundHit, mainCamera.farClipPlane, moveCommandMask);

        //Check under cursor for target
        if (Physics.SphereCast(pointerRay.origin, 4, pointerRay.direction, out hitInfo, mainCamera.farClipPlane, selectMask))
        {
            hoveringOver = hitInfo.collider.GetComponent<Unit>();
            if (hoveringOver == null)
            {
                hoveringOver = hitInfo.collider.GetComponentInParent<Unit>();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            EnterAttackCommandState();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            EnterMoveCommandState();
        }

        /////////////////////////////////
        // Unit Command
        /////////////////////////////////
        if (GetSelectedTargets().Count > 0)
        {

            if (Input.GetMouseButtonDown(1))
            {
                bool success = false;
                if (validMoveSpot && hoveringOver == null) // Move command (not hovering over a target)
                {
                    //Calculate average position for formation movement
                    Vector3 avgPosition = Vector3.zero;
                    foreach (Unit selected in currentTargets)
                    {
                        avgPosition += selected.transform.position;
                    }
                    avgPosition /= currentTargets.Count;


                    //Send command to one or more units, possibly to maintain whatever formation they were in already
                    foreach (Unit selected in currentTargets)
                    {
                        Vector3 target = groundHit.point;
                        Vector3 relativePos = selected.transform.position - avgPosition;
                        Vector3 formationPosition = target;

                        if (relativePos.sqrMagnitude > 0.01f)
                        {
                            formationPosition += (relativePos.normalized) * (selected.GetComponent<Collider>().bounds.size.x);
                        }
                        success |= IssueMoveOrder(selected, formationPosition);
                    }
                }
                else if (hoveringOver != null) // RMB -- Attack command (hovering over a target)
                {
                    foreach (Unit selected in currentTargets)
                    {
                        IAttackCommandable attackControl = selected.GetComponent<IAttackCommandable>();

                        if (attackControl != null)
                        {
                            success |= attackControl.Attack(hoveringOver);
                        }
                    }
                }

                if (success)
                {
                    commandAudioSource.PlayOneShot(playerUIConfig.acknowledgeCommandSound);
                }

                EnterMoveCommandState();
            }
        }

        /////////////////////////////////
        // Unit Selection
        /////////////////////////////////


        if (Input.GetMouseButtonDown(0)) // LMB -- select or deselect if not hovering anything
        {
            selectionStart = groundHit.point;
            selectionEnd = selectionStart;
            isBoxSelecting = true;
        }

        Vector3 selectSize = Vector3.zero;
        Vector3 selectVec = selectionEnd - selectionStart;

        if (isBoxSelecting)
        {
            actionName = "Select ";
            if (hoveringOver != null)
            {
                actionName += hoveringOver.name;
            }
            cursor.marker.icon.sprite = null;
            cursor.marker.text.text = actionName;

            selectionEnd = groundHit.point;

            selectSize = new Vector3(Mathf.Abs(selectVec.x), Mathf.Abs(selectVec.y), Mathf.Abs(selectVec.z));
            Vector3 midpoint = selectionStart + selectVec * 0.5f;
            cursor.transform.position = midpoint + Vector3.up * -0.5f;
            cursor.transform.localScale = (selectSize) + new Vector3(0, 2, 0);

        }
        else
        {
            cursor.transform.localScale = new Vector3(6f, 6f, 6f);

            // Check under cursor for valid move command options 

            cursor.transform.position = groundHit.point + Vector3.up * 0.5f;
            cursor.marker.icon.sprite = playerUIConfig.moveCommandSprite;
            cursor.marker.icon.color = Color.white;

            switch (currentCommandMode)
            {
                case CommandMode.Move:

                    if (GetSelectedTargets().Count > 0)
                    {
                        if (hoveringOver != null)
                        {
                            actionName = hoveringOver.name + "\nLMB: Select\nRMB: Attack";
                            cursor.marker.icon.sprite = playerUIConfig.canSelectSprite;
                        }
                        else
                        {
                            actionName = "RMB: Move";
                        }
                    }
                    else
                    {
                        if (hoveringOver != null)
                        {
                            actionName = hoveringOver.name + "\n";
                            cursor.marker.icon.sprite = playerUIConfig.canSelectSprite;
                        }

                        actionName += "LMB: Select";
                    }

                    break;
                case CommandMode.Attack:
                    if (hoveringOver != null)
                    {
                        actionName = hoveringOver.name + "\nLMB: Select\nRMB: Attack";
                        cursor.marker.icon.sprite = playerUIConfig.canSelectSprite;
                    }
                    else
                    {
                        actionName = "RMB: Attack-Move";
                        cursor.marker.icon.sprite = playerUIConfig.canSelectSprite;
                        cursor.marker.icon.color = Color.red;
                    }
                    break;
            }
        }

        cursor.marker.text.text = actionName;

        if (Input.GetMouseButtonUp(0))
        {
            // selectionEnd = hitInfo.point;
            bool actionSuccess = false;

            if (selectSize.sqrMagnitude > 2 && isBoxSelecting)
            {

                DeselectTargets();
                Collider[] colliders = Physics.OverlapBox(selectionStart + selectVec * 0.5f, selectSize * 0.5f);
                foreach (Collider collider in colliders)
                {
                    Unit unit = collider.GetComponent<Unit>();
                    if (unit != null)
                    {
                        AddSelection(unit);
                    }
                }
                actionSuccess = true;
            }
            else
            {
                //Individual Select
                if (isModifierHeld)
                {
                    //Add to existing selection
                    actionSuccess = AddSelection(hoveringOver);
                }
                else
                {
                    //Change selection
                    actionSuccess = SelectTarget(hoveringOver);
                }
            }

            EnterMoveCommandState();

            if (actionSuccess)
            {
                commandAudioSource.PlayOneShot(playerUIConfig.selectTargetSound);
            }
            isBoxSelecting = false;
        }
        ///////////////
    }

    private bool IssueMoveOrder(Unit selected, Vector3 position)
    {
        bool success = false;
        ParticleSystem.MainModule main = moveCommandResponseAnim.main;

        if (currentCommandMode == CommandMode.Attack)
        {
            //Attempt to issue Attack-Move command
            IAttackMoveCommandable attackMoveControl = selected.GetComponent<IAttackMoveCommandable>();

            if (attackMoveControl != null)
            {
                main.startColor = Color.red;
                success |= attackMoveControl.AttackMove(position);
            }
        } // If cannot issue Attack-Move, attempt to issue regular Move

        if (!success)
        {
            IMoveCommandable locomotionControl = selected.GetComponent<IMoveCommandable>();
            if (locomotionControl != null)
            {
                main.startColor = Color.green;
                success |= locomotionControl.MoveTo(position, isModifierHeld);
            }
        }

        if (success)
        {
            moveCommandResponseAnim.transform.position = position;
            moveCommandResponseAnim.Play();
        }

        return success;
    }

    /////////////////////////////////

    /// <summary>
    /// Display HUD information
    /// </summary>
    void UpdateRadarInfo()
    {
        int targetID = 0;
        foreach (Unit unit in targetAcquirer.GetCopyOfContactsList())
        {
            if (targetID >= targetAcquirer.Contacts.Count) break;
            while (targetID >= targetMarkerPool.Count)
            {
                targetMarkerPool.Add(Instantiate(targetMarkerPrefab, screenspaceCanvas.transform).GetComponent<RadarMarkerUI>());
            }

            RadarMarkerUI marker = targetMarkerPool[targetID];

            if (unit != null)
            {
                Vector3 contactPos = unit.transform.position;
                Bounds bounds = unit.GetComponent<Collider>().bounds;
                Vector3 screenPos;

                // float range = Vector3.Distance(unit.transform.position, targetAcquirer.transform.position);


                if (currentTargets.Contains(unit))
                {
                    screenPos = mainCamera.WorldToScreenPoint(contactPos);
                    marker.TargetingState = ContactTargetingState.Selected;
                    Vector3 max = mainCamera.WorldToScreenPoint(bounds.max);
                    float estimateX = Mathf.Abs(max.x - screenPos.x) * 2;
                    float estimateY = Mathf.Abs(max.y - screenPos.y) * 2;
                    float size = Mathf.Max(estimateX, estimateY);
                    float minSize = 16;
                    size = Mathf.Max(size, minSize);

                    //float size = (estimatedSize * 2) + minSize;
                    marker.icon.rectTransform.sizeDelta = new Vector2(size, size);
                    Vector2 iconPos = marker.icon.rectTransform.position;

                    if (currentTargets.Count < 2)
                    {
                        marker.text.text = unit.name + "\nHP:" + unit.GetHealth();
                    }
                    else
                    {
                        marker.text.text = "\nHP:" + unit.GetHealth();
                    }
                }
                else
                {
                    contactPos.y = bounds.max.y;
                    screenPos = mainCamera.WorldToScreenPoint(contactPos);
                    marker.TargetingState = ContactTargetingState.Contact;
                    marker.icon.rectTransform.sizeDelta = new Vector2(8, 8);
                    marker.text.text = "";
                }

                marker.text.rectTransform.anchoredPosition = new Vector2(0, marker.icon.rectTransform.rect.height / 2);

                if (screenPos.z < 0)
                {
                    marker.transform.localScale = Vector3.zero;
                }
                else
                {
                    IFF_Tag iffResponse = unit.IFF_GetResponse(Team.Blue);

                    screenPos.z = 0;
                    marker.transform.position = screenPos;
                    marker.transform.localScale = Vector3.one;

                    marker.UpdateColor(iffResponse);
                }
                targetID++;
            }
            else
            {
                targetAcquirer.DeregisterContact(unit);
            }
        }

        //Remaining unused target Markers go bye bye
        while (targetID < targetMarkerPool.Count)
        {
            targetMarkerPool[targetID].transform.localScale = Vector3.zero;
            targetID++;
        }
    }
}
