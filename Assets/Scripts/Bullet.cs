using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviour
{
    private Rigidbody2D _rb;
    private float timer;
    private PhotonView pv;
    // --- ADD THIS NEW LINE ---
    // This is a public "getter" property. It allows other scripts to READ the value
    // of pv.IsMine, but they cannot change pv itself.
    public bool IsMine => pv.IsMine;

    void Start()
    {
        timer = 1;
        pv = this.gameObject.GetComponent<PhotonView>();
        _rb = this.gameObject.GetComponent<Rigidbody2D>();
        if (!pv.IsMine)
        {
            Destroy(_rb);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pv.IsMine)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                PhotonNetwork.Destroy(this.gameObject);
            }
        }

    }
}