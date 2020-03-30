using System;
using UnityEngine;
using MLAgents;
using MLAgents.Policies;
using MLAgents.SideChannels;

public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player
    public enum Team
    {
        Blue = 0,
        Purple = 1
    }

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    int m_PlayerIndex;
    public SoccerFieldArea area;
    float m_BallTouch;
    public Position position;

    float m_Power;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    Vector3 m_Transform;

    public override void Initialize()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            m_Transform = new Vector3(transform.position.x - 4f, .5f, transform.position.z);
        }
        else
        {
            team = Team.Purple;
            m_Transform = new Vector3(transform.position.x + 4f, .5f, transform.position.z);
        }
        if (position == Position.Goalie)
        {
            m_Power = 1000f;
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_Power = 3000f;
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else 
        {
            m_Power = 2000f;
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        var playerState = new PlayerState
        {
            agentRb = agentRb,
            startingPos = transform.position,
            agentScript = this,
        };
        area.playerStates.Add(playerState);
        m_PlayerIndex = area.playerStates.IndexOf(playerState);
        playerState.playerIndex = m_PlayerIndex;
    }

    public void MoveAgent(float[] act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = (int)act[0];
        var rightAxis = (int)act[1];
        var rotateAxis = (int)act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    public override void OnActionReceived(float[] vectorAction)
    {

        // Generic gets nothing
        if (position == Position.Striker)
        {
            // Existential penalty for Strikers.
            AddReward(-1f / 3000f);
        }
        else if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(1f / 3000f);
        }
        MoveAgent(vectorAction);
    }

    public override float[] Heuristic()
    {
        var action = new float[3];
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            action[0] = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            action[0] = 2f;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            action[2] = 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            action[2] = 2f;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            action[1] = 1f;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            action[1] = 2f;
        }
        return action;
    }
    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = m_Power * m_KickPower;
        if (c.gameObject.CompareTag("ball"))
        {
            // Generic gets curriculum
            if (position == Position.Generic)
            {
                AddReward(.2f * m_BallTouch);
            }
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = SideChannelUtils.GetSideChannel<FloatPropertiesChannel>().GetPropertyWithDefault("ball_touch", 0);
        if (team == Team.Purple)
        {
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }
        transform.position = m_Transform;
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
        SetResetParameters();
    }

    public void SetResetParameters()
    {
        area.ResetBall();
    }
}
