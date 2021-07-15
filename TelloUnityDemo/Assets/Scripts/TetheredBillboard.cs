using UnityEngine;

public class TetheredBillboard : MonoBehaviour
{
    public Camera m_Camera;
    public float RightOffsetRadians = 0.2f;
    public float UpOffsetRadians = 0.1f;
    public float OffsetDistance = 1f; // TODO currently ignored, might need to bring it closer if the panel will be used for input buttons

    //Orient the camera after all movement is completed this frame to avoid jittering
    void LateUpdate()
    {
        var cameraPos = m_Camera.transform.position;
        var cameraRot = m_Camera.transform.rotation;

        // offset the tethered billboard from the centre of the camera's view by the specified amounts
        var targetRot = Vector3.RotateTowards(cameraRot * Vector3.forward, cameraRot * Vector3.right, RightOffsetRadians, 0f); // rotation right (radians)
        targetRot = Vector3.RotateTowards(targetRot, cameraRot * Vector3.up, UpOffsetRadians, 0); // rotation up (radians)

        // final position for the billboard is at the end of the vector: camera position + targetRot(scaled magnitude)
        // TODO normalise and scale to desired OffsetDistance
        var targetPos = cameraPos + targetRot;

        // calculate radial vectors for current and target billboard position
        var currentRadial = transform.position - cameraPos;
        var targetRadial = targetPos - cameraPos;

        // slerp halfway to target position every frame? Use Time.deltaTime to scale it instead of 50% per frame?
        transform.position = Vector3.Slerp(currentRadial, targetRadial, 0.5f) + cameraPos;

        // lastly make sure the billboard is looking back at us
        transform.LookAt(transform.position + cameraRot * Vector3.forward, cameraRot * Vector3.up);
    }
}