using UnityEngine;

public class TetheredBillboard : MonoBehaviour
{
    public Camera m_Camera;
    private float offsetDistance = 0.6f;

    //Orient the camera after all movement is completed this frame to avoid jittering
    void LateUpdate()
    {
        // need to translate to a location based on the way the camera is facing
        //transform.position = m_Camera.transform.position + m_Camera.transform.rotation * Vector3.forward * offsetDistance;

        // rotate the billboard about the camera's location to position it off-centre in the user field of view
        // FIXME this results in a frenzy of rotation :) And is independent of time delta, just 60 per frame. Not good.
        //transform.Rotate(m_Camera.transform.up, 60);
        //transform.Rotate(m_Camera.transform.right, 60);

        // do I need to track last frame's camera position (and billboard position?) to Slerp the arc for the billboard to move?
        var cameraPos = m_Camera.transform.position;
        var cameraRot = m_Camera.transform.rotation;
        var targetPos = cameraPos + cameraRot * Vector3.forward; // need to scale magnitude, not each component * offsetDistance;

        var currentRadial = transform.position - cameraPos;
        var targetRadial = targetPos - cameraPos;

        // slerp halfway to target position every frame? Use Time.deltaTime to scale it instead of 50% per frame?
        // TODO normalise and scale back to offsetDistance, in case there's some drift?
        transform.position = Vector3.Slerp(currentRadial, targetRadial, 0.5f) + cameraPos;

        // lastly make sure the billboard is looking back at us
        transform.LookAt(transform.position + cameraRot * Vector3.forward, cameraRot * Vector3.up);
    }
}