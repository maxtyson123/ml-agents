using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class TennisAgent : Agent
{
    [Header("Specific to Tennis")]
    public GameObject ball;
    public bool invertX;
    public int score;
    public GameObject myArea;
    public float scale;

    [HideInInspector]
    public Rigidbody OpponentRb;

    Text m_TextComponent;
    Rigidbody m_AgentRb;
    Rigidbody m_BallRb;
    HitWall m_BallScript;
    TennisArea m_Area;
    float m_InvertMult;
    EnvironmentParameters m_ResetParams;
    Vector3 m_Down = new Vector3(0f, -100f, 0f);
    Vector3 zAxis = new Vector3(0f, 0f, 1f);
    const float k_Angle = 90f;
    const float k_MaxAngle = 145f;
    const float k_MinAngle = 35f;

    [HideInInspector]
    public float timePenalty = 0;
    float m_Existential;

    // Looks for the scoreboard based on the name of the gameObjects.
    // Do not modify the names of the Score GameObjects
    const string k_CanvasName = "Canvas";
    const string k_ScoreBoardAName = "ScoreA";
    const string k_ScoreBoardBName = "ScoreB";

    public override void Initialize()
    {
        m_Existential = 1f / (2f * MaxStep);
        m_AgentRb = GetComponent<Rigidbody>();
        m_BallRb = ball.GetComponent<Rigidbody>();
        m_BallScript = ball.GetComponent<HitWall>();
        m_Area = myArea.GetComponent<TennisArea>();
        var canvas = GameObject.Find(k_CanvasName);
        GameObject scoreBoard;
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        if (invertX)
        {
            scoreBoard = canvas.transform.Find(k_ScoreBoardBName).gameObject;
        }
        else
        {
            scoreBoard = canvas.transform.Find(k_ScoreBoardAName).gameObject;
        }
        m_TextComponent = scoreBoard.GetComponent<Text>();
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //sensor.AddObservation(m_InvertMult * (transform.position.x - myArea.transform.position.x) / -25f);
        //sensor.AddObservation((transform.position.y - myArea.transform.position.y) / -7f);
        //sensor.AddObservation(m_InvertMult * m_AgentRb.velocity.x / 20f);
        //sensor.AddObservation(m_AgentRb.velocity.y / 20f);

        //sensor.AddObservation(m_InvertMult * (ball.transform.position.x - myArea.transform.position.x) / 25f);
        //sensor.AddObservation((ball.transform.position.y - myArea.transform.position.y) / 20f);
        //sensor.AddObservation(m_InvertMult * m_BallRb.velocity.x / 40f);
        //sensor.AddObservation(m_BallRb.velocity.y / 60f);

        //sensor.AddObservation(m_InvertMult * (opponent.transform.position.x - myArea.transform.position.x) / -25f);
        //sensor.AddObservation((opponent.transform.position.y - myArea.transform.position.y) / -7f);
        //sensor.AddObservation(m_InvertMult * OpponentRb.velocity.x / 20f);
        //sensor.AddObservation(OpponentRb.velocity.y / 20f);

        sensor.AddObservation(m_InvertMult * (transform.position.x - myArea.transform.position.x));
        sensor.AddObservation(transform.position.y - myArea.transform.position.y);
        sensor.AddObservation(m_InvertMult * m_AgentRb.velocity.x);
        sensor.AddObservation(m_AgentRb.velocity.y);

        sensor.AddObservation(m_InvertMult * (ball.transform.position.x - myArea.transform.position.x));
        sensor.AddObservation(ball.transform.position.y - myArea.transform.position.y);
        sensor.AddObservation(m_InvertMult * m_BallRb.velocity.x);
        sensor.AddObservation(m_BallRb.velocity.y);

        sensor.AddObservation(m_InvertMult * (OpponentRb.position.x - myArea.transform.position.x));
        sensor.AddObservation(OpponentRb.position.y - myArea.transform.position.y);
        sensor.AddObservation(m_InvertMult * OpponentRb.velocity.x);
        sensor.AddObservation(OpponentRb.velocity.y);

        sensor.AddObservation(m_InvertMult * gameObject.transform.rotation.z);
        //sensor.AddObservation((m_InvertMult * (gameObject.transform.rotation.eulerAngles.z - (1f - m_InvertMult) * 180f) - 35f) / 125f);

        sensor.AddObservation(System.Convert.ToInt32(m_BallScript.lastFloorHit == HitWall.FloorHit.FloorHitUnset));
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        var moveX = Mathf.Clamp(vectorAction[0], -1f, 1f) * m_InvertMult;
        var moveY = Mathf.Clamp(vectorAction[1], -1f, 1f);
        var rotate = Mathf.Clamp(vectorAction[2], -1f, 1f) * m_InvertMult;
        
        var upward = 0.0f;
        if (moveY > 0.0 && transform.position.y - transform.parent.transform.position.y < -1.5f)
        {
            upward = moveY;
        }

        m_AgentRb.AddForce(new Vector3(moveX * 2.5f, upward * 10f, 0f), ForceMode.VelocityChange);

        // calculate angle between m_InvertMult * 55 and m_InvertMult * 125
        var angle = 35f * rotate + m_InvertMult * k_Angle;
        // maps agents rotation into m_InvertMult * 55 and m_InvertMult * 125
        var rotateZ = angle - (gameObject.transform.rotation.eulerAngles.z - (1f - m_InvertMult) * 180f);
        Quaternion deltaRotation = Quaternion.Euler(zAxis * rotateZ);
        m_AgentRb.MoveRotation(m_AgentRb.rotation * deltaRotation);

        if (invertX && transform.position.x - transform.parent.transform.position.x < -m_InvertMult ||
            !invertX && transform.position.x - transform.parent.transform.position.x > -m_InvertMult)
        {
            transform.position = new Vector3(-m_InvertMult + transform.parent.transform.position.x,
                transform.position.y,
                transform.position.z);
        }
        var rgV = m_AgentRb.velocity;
        m_AgentRb.velocity = new Vector3(Mathf.Clamp(rgV.x, -10, 10), Mathf.Min(rgV.y, 10f), rgV.z);

    //    timePenalty -= m_Existential;
        m_TextComponent.text = score.ToString();
    }

    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = Input.GetAxis("Horizontal");    // Racket Movement
        actionsOut[1] = Input.GetKey(KeyCode.Space) ? 1f : 0f;   // Racket Jumping
        actionsOut[2] = Input.GetAxis("Vertical");   // Racket Rotation
    }

    //void OnCollisionEnter(Collision c)
    //{
    //    if (c.gameObject.CompareTag("ball"))
    //    {
    //        AddReward(.01f);
    //    }
    //}

    void FixedUpdate()
    {   
        m_AgentRb.AddForce(m_Down);
    }   

    public override void OnEpisodeBegin()
    {

        timePenalty = 0;
        m_InvertMult = invertX ? -1f : 1f;
        var agentOutX = Random.Range(14f, 16f);
        var agentOutY = Random.Range(-1.5f, 0f);
        transform.position = new Vector3(-m_InvertMult * agentOutX, agentOutY, -1.8f) + transform.parent.transform.position;
        m_AgentRb.velocity = new Vector3(0f, 0f, 0f);
        SetResetParameters();
        if (m_InvertMult == 1f)
        {
            m_Area.MatchReset();
        }

    }

    public void SetRacket()
    {
        gameObject.transform.eulerAngles = new Vector3(
            gameObject.transform.eulerAngles.x,
            gameObject.transform.eulerAngles.y,
            m_InvertMult * k_Angle
        );
    }

    public void SetBall()
    {
        scale = .5f;//m_ResetParams.GetWithDefault("scale", .5f);
        ball.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void SetResetParameters()
    {
        SetRacket();
        SetBall();
    }
}
