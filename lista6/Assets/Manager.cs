using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using project;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour {

    public static Manager instance;

    public float[] xRange = { 0, 0 };
    public float[] yRange = { 0, 0 };

    int pointsAumont = 100;
    public Point[] points;

    [SerializeField]
    Camera mainCamera;

    [SerializeField]
    GameObject floor;

    [SerializeField]
    GameObject pointPrefab;

    [SerializeField]
    Material minDistMaterial;

    struct PendingLine {
        public Vector3 start;
        public Vector3 end;
        public Color color;
        public float duration;
    }

    private Queue<PendingLine> toDraw;

    private Thread workerThread;
    private bool workerDone = false;
    private float workerResult = 0f;
    private Point[] minPair;

    void Awake() {

        // create a singleton instance
        if (instance == null) {
            instance = this;
        } else {
            Destroy(this.gameObject);
        }

    }

    private void Start() {
        calculateGenerationRange();

        points = new Point[pointsAumont];
        toDraw = new Queue<PendingLine>();


        for (int i = 0; i < pointsAumont; i++) {

            var gameObj = GameObject.Instantiate(pointPrefab);
            gameObj.name = "Point " + i;

            points[i] = new Point(gameObj);

        }

    }

    private void FixedUpdate() {

        for (int i = 0; i < 1; i++) {

            if (toDraw.Count <= 0) break;

            var current = toDraw.Dequeue();

            drawLine(current.start, current.end, current.color, current.duration);

        }
    }

    private void Update() {


        if (workerDone) {

            workerThread.Join();

            Debug.Log("result = " + workerResult);

            workerThread = null;
            workerDone = false;

            drawLine(minPair[0].position, minPair[1].position, Color.white, 25f);

            minPair[0].gameObject.GetComponent<MeshRenderer>().material = minDistMaterial;
            minPair[1].gameObject.GetComponent<MeshRenderer>().material = minDistMaterial;
        }

        if (Input.GetKeyUp(KeyCode.R)) {

            if (workerThread == null) {

                for (int i = 0; i < pointsAumont; i++) {

                    points[i].updatePosition();

                }

                workerThread = new Thread(worker);
                workerThread.Start();
            }
        }

        if (Input.GetKeyUp(KeyCode.Q)) {

            SceneManager.LoadScene(0);


        }
    }

    void worker() {

        Array.Sort(points, compareX);
        minPair = new Point[2];
        workerResult = FindClosest(0, points.Length - 1, ref minPair[0], ref minPair[1]);

        workerDone = true;
    }

    public static int compareX(Point a, Point b) {

        float aX = a.position.x;
        float bX = b.position.x;

        if (aX < bX) return -1;
        if (aX > bX) return 1;

        return 0;
    }

    float FindClosest(int start, int end, ref Point a, ref Point b) {

        if (end == start) {
            a = points[start];
            b = null;
            return Mathf.Infinity;
        }

        // TODO: use mean of means
        int mid = (start + end) / 2;
        Point midPoint = points[mid];

        PendingLine pl = new PendingLine();
        pl.start = new Vector3(midPoint.position.x + 1, yRange[0], 0);
        pl.end = new Vector3(midPoint.position.x + 1, yRange[1], 0);
        pl.color = Color.red;
        pl.duration = ((end - start) / (float)pointsAumont) * 30f;

        toDraw.Enqueue(pl);

        Point[] pairLeft = new Point[2];
        float distanceLeft = FindClosest(start, mid, ref pairLeft[0], ref pairLeft[1]);

        if (pairLeft[1] != null) {

            PendingLine leftLine = new PendingLine();
            leftLine.start = pairLeft[0].position;
            leftLine.end = pairLeft[1].position;
            leftLine.color = Color.blue;
            leftLine.duration = ((end - start) / (float)pointsAumont) * 30f;

            toDraw.Enqueue(leftLine);
        }

        Point[] pairRight = new Point[2];
        float distanceRight = FindClosest(mid + 1, end, ref pairRight[0], ref pairRight[1]);

        if (pairRight[1] != null) {

            PendingLine rightLine = new PendingLine();
            rightLine.start = pairRight[0].position;
            rightLine.end = pairRight[1].position;
            rightLine.color = Color.blue;
            rightLine.duration = ((end - start) / (float)pointsAumont) * 30f;

            toDraw.Enqueue(rightLine);
        } else {

            PendingLine centerline = new PendingLine();
            centerline.start = pairLeft[0].position;
            centerline.end = pairRight[0].position;
            centerline.color = Color.blue;
            centerline.duration = ((end - start) / (float)pointsAumont) * 30f;

            toDraw.Enqueue(centerline);
        }

        Point[] minPair = (distanceLeft < distanceRight) ? pairLeft : pairRight;
        float min = Mathf.Min(distanceLeft, distanceRight);

        int leftSize = Mathf.Abs(mid - start) + 1;
        int rightSize = Mathf.Abs(end - mid);

        Point[] sorted = new Point[leftSize + rightSize];

        int rightCounter = 0;
        int leftCounter = 0;

        for (int i = 0; i < sorted.Length; i++) {

            Point right = (mid + 1 + rightCounter <= end) ? points[mid + 1 + rightCounter] : null;
            Point left = (start + leftCounter <= mid) ? points[start + leftCounter] : null;

            if (right != null && left != null) {


                if (right != null && right.position.y < left.position.y) {

                    sorted[i] = right;
                    rightCounter++;

                } else {

                    sorted[i] = left;
                    leftCounter++;

                }

            }

            if (left == null) {

                sorted[i] = right;
                rightCounter++;

            } else if (right == null) {

                sorted[i] = left;
                leftCounter++;

            }
        }

        int k = 0;
        for (int i = start; i <= end; i++) {

            points[i] = sorted[k];
            k++;

        }


        int y_bound = 6 - (end - start);

        if (y_bound <= 0) y_bound = 6;

        for (int i = start; i <= end; i++) {

            Point current = points[i];

            if (Mathf.Abs(current.position.x) > Mathf.Abs(midPoint.position.x + min)) {
                // filter out points that are out of the strip defined by:
                // [mean - min; mean + min]

                continue;
            }

            for (int j = i + 1; j < i + y_bound && j < pointsAumont; j++) {

                Point compare = points[j];

                float dist = distance(current.position, compare.position);

                if (dist < min) {
                    min = dist;
                    minPair[0] = current;
                    minPair[1] = compare;
                }

            }

        }

        PendingLine minLine = new PendingLine();
        minLine.start = minPair[0].position;
        minLine.end = minPair[1].position;
        minLine.color = Color.green;
        minLine.duration = ((end - start) / (float)pointsAumont) * 30f;

        a = minPair[0];
        b = minPair[1];

        toDraw.Enqueue(minLine);

        return min;

    }

    float distance(Vector2 a, Vector2 b) {

        float result = Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2);
        result = Mathf.Sqrt(result);

        return result;

    }

    void drawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f) {

        GameObject myLine = new GameObject();

        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();

        LineRenderer lr = myLine.GetComponent<LineRenderer>();

        lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));

        lr.SetColors(color, color);
        lr.SetWidth(0.25f, 0.25f);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        GameObject.Destroy(myLine, duration);
    }

    void calculateGenerationRange() {

        var bounds = floor.GetComponent<Collider>().bounds;

        var maxPoint = bounds.max;
        var minPoint = bounds.min;

        xRange[0] = minPoint.x;
        xRange[1] = maxPoint.x;

        // the 'world' z axis is our y axis
        yRange[0] = minPoint.y;
        yRange[1] = maxPoint.y;
    }

}