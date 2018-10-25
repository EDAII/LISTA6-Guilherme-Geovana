using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointBehaviour : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

        setRandomPosition();
    }

    void setRandomPosition()
    {

        Vector3 position = new Vector3();
        do
        {
            position.x = Random.Range(Manager.instance.xRange[0], Manager.instance.xRange[1]);
            position.y = Random.Range(Manager.instance.yRange[0], Manager.instance.yRange[1]);
            position.z = 1;

            this.transform.position = position;
        } while (isColliding());
    }

    bool isColliding() {

        Collider[] collisions = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius);


        return (collisions.Length != 1);
    }
}