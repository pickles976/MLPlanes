using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class missile_training : MonoBehaviour
{
    public GameObject parent;

    private void OnDestroy()
    {
        parent.GetComponent<BombingAgent>().MissileHit(transform.position);
    }
}
