﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Leap;
using Leap.Unity.PinchUtility;

public class HandsRaycast : MonoBehaviour {

	public GameObject button1;
	Collider button1Collider;
	public GameObject PanelContainer;
	// an object with an array of all buttons should be included 


	// VIEWPANEL
	float VIEWPANEL_EULER_X_LOWER_THRESHHOLD = 12.0f;
	float VIEWPANEL_EULER_X_UPPER_THRESHHOLD = 100.0f;

	int panelState;
	int PANEL_ON = 0;
	float turnPanelOffTimer = 0.0f;
	int PANEL_OFF = 1;
	float turnPanelOnTimer = 0.0f;

	float PANEL_TIMER_CONSTANT = 0.5f;

	public Camera playerCamera; // aka CenterEyeAnchor

	LineRenderer myLineRenderer;

	public GameObject sceneGod;
	GenerateRandomGraph graphGenerator;

	Node[] nodes;

	int stateR;
	public GameObject rightPinchDetector;
	private LeapPinchDetector rightPinchDetectorScript;
	GameObject draggedObjectR = null;
	float distanceOfDraggedObjectR = 0.0f;
	float originalPinchDistanceR = 0.0f;
	Node highlightedObjectR = null;

	int stateL;
	public GameObject leftPinchDetector;
	private LeapPinchDetector leftPinchDetectorScript;
	GameObject draggedObjectL = null;
	float distanceOfDraggedObjectL = 0.0f;
	float originalPinchDistanceL = 0.0f;
	Node highlightedObjectL = null;

	int STATE_NORMAL = 0;
	int STATE_DRAGGING = 1;

	int RIGHT = 0;
	int LEFT = 1;

	Vector3 nodeContainerStartPosition;
	Vector3 zoomPinchStartPositionL;
	Vector3 zoomPinchStartPositionR;
	float zoomPinchStartDistance;
	float lastZoomPinchDistance;

	void Start () {
		GameObject prefabLineToRender = Resources.Load("Line") as GameObject;
		GameObject lineToRender = Instantiate (prefabLineToRender, new Vector3 (0, 0, 0), Quaternion.identity) as GameObject;
		myLineRenderer = lineToRender.GetComponent<LineRenderer> ();
		myLineRenderer.enabled = false;

		graphGenerator = sceneGod.GetComponent<GenerateRandomGraph> ();
		nodes = graphGenerator.masterNodeList;

		rightPinchDetectorScript = rightPinchDetector.GetComponent<LeapPinchDetector> ();
		leftPinchDetectorScript = leftPinchDetector.GetComponent<LeapPinchDetector> ();

		button1Collider = button1.GetComponent<Collider> ();


	}

	void FixedUpdate () {
		UpdateControlPanel ();

		HandlePinches (leftPinchDetectorScript, LEFT);
		HandlePinches (rightPinchDetectorScript, RIGHT);

		if (stateL == STATE_DRAGGING) { // maybe do this if the user stops moving the node around, don't do it if the node is moving a lot
			graphGenerator.explodeSelectedNode (highlightedObjectL);
		} 

		if (stateR == STATE_DRAGGING) {
			graphGenerator.explodeSelectedNode (highlightedObjectR);
		}

	}

	void UpdateControlPanel () {
		// looking at panel
		if (playerCamera.transform.eulerAngles.x >= VIEWPANEL_EULER_X_LOWER_THRESHHOLD && playerCamera.transform.eulerAngles.x <= VIEWPANEL_EULER_X_UPPER_THRESHHOLD) {
			turnPanelOffTimer = 0.0f;
			turnPanelOnTimer += Time.deltaTime;

			if (turnPanelOnTimer >= PANEL_TIMER_CONSTANT) {
				panelState = PANEL_ON;
			}

		} 
		// not looking at panel
		else { 
			turnPanelOnTimer = 0.0f;
			turnPanelOffTimer += Time.deltaTime;

			if (turnPanelOffTimer >= PANEL_TIMER_CONSTANT) {
				panelState = PANEL_OFF;
			}
		}

		if (panelState == PANEL_ON) {
			PanelContainer.SetActive (true);
		} else if (panelState == PANEL_OFF) {
			PanelContainer.SetActive (false);
		}



	}

	void HandlePinches(LeapPinchDetector detector, int handedness) {
		// GET ACTIVITY -- are you pinching, clicking?
		bool isActive = detector.IsPinching;
		bool activeThisFrame = detector.DidStartPinch;

		// GET POSITION OF EVENT
		Vector3 p = detector.Position;
		// camera to pinch vector
		Vector3 heading = Vector3.Normalize(p - playerCamera.transform.position);

		// camera to object vector
		Vector3 objectVector;
		float biggestDotProduct= 0.0f;
		int selectedNodeIndex = 0;
		float dotProduct;

		int state = -1;
		if( handedness == RIGHT){
			state = stateR;
		}
		else{
			state = stateL;
		}

		if (panelState == PANEL_ON) {

			// do panel actions
			graphGenerator.NodesAreDraggable (false);

			RaycastHit hit = new RaycastHit ();
			ButtonActivate hitObject;
			Vector3 endRayPosition = playerCamera.transform.position + (heading.normalized * 100.0f);

			//myLineRenderer.SetVertexCount (2);
			//myLineRenderer.SetPosition (0, p);
			//myLineRenderer.SetPosition (1, endRayPosition);
			//myLineRenderer.enabled = true;



			if ( button1Collider.Raycast (new Ray(playerCamera.transform.position, heading), out hit, 200.0f)) { // if you hit something

				//Debug.Log("Hit something.");
				if (hit.transform.gameObject.tag == "Clickable") { // if it was a button
					//Debug.Log("Hit Clickable.");

					hit.transform.gameObject.GetComponent<ButtonActivate> ().OnHit ();
					graphGenerator.showNodesOfDegreeGreaterThan (22);
				}
			}

		} else {

			graphGenerator.NodesAreDraggable (true);

			if (state != STATE_DRAGGING && isActive) { // can start a drag
				state = STATE_DRAGGING;

				for (int i = 0; i < nodes.Length; i++) {
					objectVector = Vector3.Normalize (nodes [i].gameObject.transform.position - playerCamera.transform.position);
					dotProduct = Vector3.Dot (heading, objectVector);

					if (dotProduct > biggestDotProduct) {
						biggestDotProduct = dotProduct;
						selectedNodeIndex = i;
					}
				}

				GameObject draggedObject = null;
				float distanceOfDraggedObject = 0.0f;
				float originalPinchDistance = 0.0f;

				if (handedness == RIGHT) {
					nodes [selectedNodeIndex].nodeForce.Selected ();
					originalPinchDistance = originalPinchDistanceR;
				} else {
					nodes [selectedNodeIndex].nodeForce.Selected ();
					originalPinchDistance = originalPinchDistanceL;
				}

				if (handedness == LEFT) {
					highlightedObjectL = nodes [selectedNodeIndex];
					highlightedObjectL.nodeForce.Selected ();
					//Debug.Log ("start highlightedObjectL.nodeForce.myTextMesh.text: " + highlightedObjectL.nodeForce.myTextMesh.text );
				} else {
					highlightedObjectR = nodes [selectedNodeIndex];
					highlightedObjectR.nodeForce.Selected ();
					//Debug.Log ("start highlightedObjectR.nodeForce.myTextMesh.text: " + highlightedObjectR.nodeForce.myTextMesh.text );
				}
			}

			if (state == STATE_DRAGGING) { // already dragging

				if (handedness == LEFT) {
					if (highlightedObjectL != null) {
						highlightedObjectL.nodeForce.timeSelected += Time.deltaTime;
					}
				} else {
					if (highlightedObjectR != null) {
						highlightedObjectR.nodeForce.timeSelected += Time.deltaTime;
					}
				}



			}

			if (!isActive) { // if you let go you're not dragging
				state = STATE_NORMAL;

				if (handedness == LEFT) {
					if (highlightedObjectL != null) {
						//Debug.Log ("letgo highlightedObjectL.nodeForce.myTextMesh.text: " + highlightedObjectL.nodeForce.myTextMesh.text );
						highlightedObjectL.nodeForce.Unselected ();
						graphGenerator.unselectNode ();
						highlightedObjectL.nodeForce.timeSelected = 0.0f;
						highlightedObjectL = null;
					}
				} else {
					if (highlightedObjectR != null) {
						//Debug.Log ("letgo highlightedObjectR.nodeForce.myTextMesh.text: " + highlightedObjectR.nodeForce.myTextMesh.text );
						highlightedObjectR.nodeForce.Unselected ();
						graphGenerator.unselectNode ();
						highlightedObjectR.nodeForce.timeSelected = 0.0f;
						highlightedObjectR = null;
					}
				}
			}

			if (handedness == RIGHT) {
				stateR = state;
			} else {
				stateL = state;
			}
		}
	}






}
