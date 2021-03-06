﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using project;

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

        Point[] points = Manager.instance.points;

        for (int i=0; i < points.Length && points[i] != null; i++) {
            if( points[i].position.x == this.transform.position.x ){
                return true;
            }
        }

        return (collisions.Length != 1);
    }
}
