using UnityEngine;
using System.Collections;

namespace project { 
    public class Point {

        public GameObject gameObject;
        public Vector3 position;

        public Point (GameObject gameObject) {

            this.gameObject = gameObject;
            this.position = gameObject.transform.position;
            
        }

        public void updatePosition() {

            position = gameObject.transform.position;

        }

    }
}