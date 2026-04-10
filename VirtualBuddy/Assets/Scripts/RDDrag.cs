using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RDDrag : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.drag = 5f;        // ÏßÐÔ×èÄá£¨×èÖ¹Æ®ÒÆ£©
        rb.angularDrag = 5f; // ½Ç×èÄá£¨×èÖ¹×ªÈ¦£©
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
