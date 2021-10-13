using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0618

// A super simple example player. Use arrow keys to move
public class NobleExamplePlayer : NetworkBehaviour
{
    public override void OnStartAuthority()
    {
        StartCoroutine(KeepAlive());
    }
    
    // On Android UNet will disconnect if you don't send messages periodically
    // This method and the corresponding Command prevents that from happening
    IEnumerator KeepAlive()
    {
        while (gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(20);

            CmdStayAlive();
        }
    }

    [Command]
    void CmdStayAlive() { }

    void Update()
    {
        if (!isLocalPlayer) return;

        Vector3 dir = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow)) dir = Vector3.up;
        else if (Input.GetKey(KeyCode.DownArrow)) dir = Vector3.down;
        else if (Input.GetKey(KeyCode.LeftArrow)) dir = Vector3.left;
        else if (Input.GetKey(KeyCode.RightArrow)) dir = Vector3.right;

        transform.position += dir * Time.deltaTime * 5;
    }

}