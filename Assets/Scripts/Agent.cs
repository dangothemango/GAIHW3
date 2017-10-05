using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour {

    public enum State {
        wait,
        wander,
        pursue,
        evade,
        path
    }
    
    public enum CollisionType {
        none,
        collisionPredict,
        coneCheck
    }

    public CollisionType collisionType;
    public State curState = State.wait;
    public Transform target;
    public Transform[] path;
    public float rotation_speed;
    public float move_speed;
    public float slow_down_dist;
    public float cone_angle;
    public float cone_distance;
    public float avoidanceForce;
    public int num_whiskers;

    Transform wander_target;
    int path_index = 0;
    Vector2 targetOffset = Vector2.zero;
    Vector3 dir = Vector3.zero;

    Rigidbody2D RB;

    // Use this for initialization
    void Start() {
        RB = GetComponent<Rigidbody2D>();

    }

    // Update is called once per frame
    void Update() {
        switch (curState) {
            case State.wait:
                break;
            case State.wander:
                Wander();
                break;
            case State.pursue:
                Pursue();
                break;
            case State.evade:
                Evade();
                break;
            case State.path:
                FollowPath();
                break;
            default:
                Debug.LogError(string.Format("{0} is not a valid state", curState));
                SetState(State.wait);
                break;
        }
        switch (collisionType) {
            case CollisionType.collisionPredict:
                PredictCollision();
                break;
            case CollisionType.coneCheck:
                ConeCheck();
                break;
        }
    }

    public void SetState(State s) {
        Debug.Log(s);
        curState = s;
    }

    public void SetTarget(Transform t) {
        target = t;
    }

    void Wander() {
        if (dir == Vector3.zero || Random.Range(0, 1.0f) > .99f) {
            newDir();
        }
        Debug.DrawLine(transform.position, transform.position + dir, Color.black);
        RB.velocity = dir.normalized * move_speed / 2;
        RotateTowards(transform.position + dir);
    }

    public void newDir() {
        dir = (Vector2)wander_target.position + Random.insideUnitCircle - (Vector2)transform.position;
    }

    void Pursue() {
        float distance = Vector2.Distance(target.position, transform.position);


        if (distance > .5f) {
            RotateTowards(target.position);
            RB.velocity = (target.position - transform.position).normalized * move_speed * Mathf.Min(distance / slow_down_dist, 1);
        }
        else {
            RB.velocity = Vector3.zero;
        }
    }

    void Evade() {
        Vector3 v = transform.position - target.position;
        RotateTowards(transform.position + v);
        RB.velocity = v.normalized * Time.deltaTime * move_speed;
    }

    void FollowPath() {
        float minDist = float.MaxValue;
        int minI = 0; ;
        for (int i = 0; i < path.Length; i++ ) {
            if (minDist > Vector2.Distance(path[i].position, transform.position)) {
                minDist = Vector2.Distance(path[i].position, transform.position);
                minI = i;
            }
        }
        if (minI < path.Length - 1) {
            //Check if within range of path point to move to next point
            float distance = Vector2.Distance(path[minI + 1].position+(Vector3)targetOffset, transform.position);

            distance = Vector2.Distance(target.position, transform.position);

            RB.velocity = (path[minI + 1].position - transform.position+ (Vector3)targetOffset).normalized * move_speed * Mathf.Min(distance / slow_down_dist, 1);
            RotateTowards(path[minI + 1].position+(Vector3)targetOffset);
        } else {
            Debug.Log(path[minI].name);
            SetTarget(path[path.Length - 1]);
            SetState(State.pursue);
        }
    }

    void RotateTowards(Vector3 position) {
        Vector3 offset = (position - transform.position).normalized;
        transform.right = Vector3.MoveTowards(transform.right, offset, Time.deltaTime * rotation_speed);
    }

    void PredictCollision() {
        foreach (Agent a in GameManager.INSTANCE.Agents) {
            if (a.name[0] == this.name[0]) continue;
            Vector2 dp = a.transform.position - transform.position;
            Vector2 dv = a.RB.velocity - RB.velocity;
            float t = -1 * Vector2.Dot(dp, dv) / Mathf.Pow(dv.magnitude, 2);
            if (t > 2f) return;
            Vector2 pc = (Vector2)transform.position + RB.velocity * t;
            Vector2 pt = (Vector2)a.transform.position + a.RB.velocity * t;
            if (Vector2.Distance(pc, pt) < 2 * transform.localScale.x) {
                Debug.Log(string.Format("{0} avoiding {1}", this.name, a.name));
                RB.AddForce((RB.velocity - pc).normalized * avoidanceForce);
                return;
            }
        }
    }

    void ConeCheck() {
        
        Quaternion start_angle = Quaternion.AngleAxis(-1*cone_angle / 2, transform.forward);
        Quaternion step_angle = Quaternion.AngleAxis(cone_angle / num_whiskers, transform.forward);
        Vector2 direction = start_angle * transform.right;

        for (int i = 0; i < num_whiskers; ++i) {
            Debug.DrawRay(transform.position, direction.normalized*cone_distance, Color.white, 0);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction,cone_distance);
            if (hit.collider !=null) {
                Debug.LogWarning("Hit");
                var hit_agent = hit.collider.GetComponent<Agent>();
                if (hit_agent.name[0] != this.name[0]) {
                    Debug.Log(string.Format("{0} avoiding {1}", this.name, hit_agent.name));
                    RB.AddForce(((Vector2)hit_agent.transform.position-RB.velocity).normalized * avoidanceForce);
                    return;
                } else {
                    Debug.LogError("Raycast hit a non red agent");
                }
            }

            direction = step_angle * direction;
        }
    }

    float ApproxDistanceBetween(Agent a, Agent b) {
        float angle = Vector2.Angle(a.RB.velocity, b.RB.velocity)*Mathf.Rad2Deg;
        return angle - Vector2.Distance(a.transform.position, b.transform.position);
    }

}
