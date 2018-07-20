﻿//Put this script on your blue cube.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class VisionCrawlerAgent : Agent
{
    /// <summary>
    /// Detects when the block touches the goal.
    /// </summary>
	[HideInInspector]
    public GoalDetect goalDetect;

    public bool useVectorObs;

    public MotorCrawlerAgent motorAgent;

    public Transform ground;

    public bool respawnTargetWhenTouched;

    public float targetSpawnRadius;

    RayPerception rayPer;

    public Transform obstacleWall;
    
    [Header("Target To Walk Towards")] [Space(10)]
    public Transform target;

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer groundRenderer;

    public override void InitializeAgent()
    {
        base.InitializeAgent();
        rayPer = GetComponent<RayPerception>();
    }

    public override void CollectObservations()
    {
        if (useVectorObs)
        {
            var rayDistance = 20f;
            float[] rayAngles = { 20f, 45f, 90f, 135f, 160f, 110f, 70f};
            float[] rayAnglesUp = { 30f, 60f, 90f, 120f, 150f};
            var detectableObjects = new[] { "target", "wall" };
            AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 1.5f, 0f));
            AddVectorObs(rayPer.Perceive(rayDistance, rayAnglesUp, detectableObjects, 1.5f, 3f));
            AddVectorObs(rayPer.Perceive(rayDistance, rayAnglesUp, detectableObjects, 1.5f, -3f));
        }
    }
    
    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {
        AddReward(2f);
        motorAgent.AddReward(2f);
        if (respawnTargetWhenTouched)
        {
            GetRandomTargetPos();
        }
        else
        {
            Done();
            motorAgent.Done();
        }
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
	public void SetGoalDirection(float[] act)
    {

        Vector3 goalDirection = Vector3.zero;

        int action = Mathf.FloorToInt(act[0]);

        // Goalies and Strikers have slightly different action spaces.
        switch (action)
        {
            case 0:
                goalDirection = transform.forward * 10f;
                break;
            case 1:
                goalDirection = transform.right * -10f;
                break;
            case 2:
                goalDirection = transform.right * 10f;
                break;
        }

        if (Application.isEditor)
        {
            Debug.DrawRay(transform.position,
                goalDirection, Color.green, 0.01f, true);
        }
        
        motorAgent.dirToTarget = goalDirection;
    }
    
    /// <summary>
    /// Moves target to a random position within specified radius.
    /// </summary>
    public void GetRandomTargetPos()
    {
        Vector3 newTargetPos = new Vector3(15f, 4f, Random.Range(-17f, 17f));
        target.GetComponent<Rigidbody>().velocity = Vector3.zero;
        target.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        target.position = newTargetPos + ground.position;
        target.rotation = Quaternion.identity;
        obstacleWall.transform.position = new Vector3(0f, 2f, Random.Range(-17f, 17f)) + ground.position;
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
	public override void AgentAction(float[] vectorAction, string textAction)
    {
        // Move the agent using the action.
        SetGoalDirection(vectorAction);

        // Penalty given each step to encourage agent to finish task quickly.
        AddReward(-1f / agentParameters.maxStep);
        
        foreach (var bodyPart in motorAgent.jdController.bodyPartsDict.Values)
        {
            if (bodyPart.targetContact && !IsDone() && bodyPart.targetContact.touchingTarget)
            {
                TouchedTarget();
            }
        }

    }

    /// <summary>
    /// In the editor, if "Reset On Done" is checked then AgentReset() will be 
    /// called automatically anytime we mark done = true in an agent script.
    /// </summary>
	public override void AgentReset()
    {
        GetRandomTargetPos();
        motorAgent.AgentReset();
    }
}
