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
    public Transform wander_target;
    public float cone_angle;
    public int path_index;
    public Vector2 targetOffset = Vector2.zero;
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
            if (a == this) continue;
            Vector2 dp = a.transform.position - transform.position;
            Vector2 dv = a.RB.velocity - RB.velocity;
            float t = -1 * Vector2.Dot(dp, dv) / Mathf.Pow(dv.magnitude, 2);
            Vector2 pc = (Vector2)transform.position + RB.velocity * t;
            Vector2 pt = (Vector2)a.transform.position + a.RB.velocity * t;
            if (Vector2.Distance(pc, pt) < 2 * transform.localScale.x) {
                Debug.Log(string.Format("{0} avoiding {1}", this.name, a.name));
                RB.velocity = RB.velocity - pc;
                RB.velocity = RB.velocity.normalized * move_speed;
                return;
            }
        }
    }

    void ConeCheck() {
        Vector3 target_dir = target.position - transform.position;
        float check_angle = Vector3.Angle(target_dir, transform.forward);

        //Check if the angle is small enough i.e. in the cone of view
        if (check_angle < cone_angle) {

        }
    }

    float ApproxDistanceBetween(Agent a, Agent b) {
        float angle = Vector2.Angle(a.RB.velocity, b.RB.velocity)*Mathf.Rad2Deg;
        return angle - Vector2.Distance(a.transform.position, b.transform.position);
    }

}
