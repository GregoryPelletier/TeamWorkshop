using UnityEngine;

// Gives a 3D skeleton a cape that hangs from its neck and sways a little.
// Put it on the skeleton itself (the object with the Enemy Fighter). It builds the cape by itself,
// in the editor and in the game, so there is no cape model to import.
// The cape follows the neck bone, so it moves with the skeleton when it walks and attacks.
[ExecuteAlways]
public class SkeletonCape : MonoBehaviour
{
    public const string CapeObjectName = "Cape";

    [Tooltip("The material that colors the cape.")]
    public Material capeMaterial;
    [Tooltip("The bone the cape hangs from.")]
    public string anchorBoneName = "B-neck";
    [Tooltip("How tall the skeleton model is at scale 1 (used to size the cape).")]
    public float modelHeight = 1.9232f;

    [Header("Shape (as a share of the skeleton's height)")]
    public float capeLength = 0.62f;
    public float topWidth = 0.16f;
    public float bottomWidth = 0.3f;
    [Tooltip("How far the bottom of the cape flares out behind the skeleton.")]
    public float flareBehind = 0.14f;
    [Tooltip("How far behind the neck the cape starts.")]
    public float startBehind = 0.04f;
    [Tooltip("How far below the neck the cape starts.")]
    public float startBelow = 0.02f;

    [Header("Sway")]
    public float swayAmount = 0.025f;
    public float swaySpeed = 2.2f;

    const int ColumnCount = 9;
    const int RowCount = 10;

    Transform anchorBone;
    GameObject capeObject;
    Mesh capeMesh;
    Vector3[] capePoints;

    void OnEnable()
    {
        anchorBone = FindBone(transform, anchorBoneName);
        BuildCape();
    }

    void OnDisable()
    {
        if (capeObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(capeObject);
            }
            else
            {
                DestroyImmediate(capeObject);
            }
        }
        capeObject = null;
    }

    // Searches every child, grandchild and so on for a bone with this name.
    Transform FindBone(Transform searchFrom, string boneName)
    {
        Transform foundBone = null;
        foreach (Transform childBone in searchFrom)
        {
            if (foundBone == null)
            {
                if (childBone.name == boneName)
                {
                    foundBone = childBone;
                }
                else
                {
                    foundBone = FindBone(childBone, boneName);
                }
            }
        }
        return foundBone;
    }

    void BuildCape()
    {
        capeObject = new GameObject(CapeObjectName);
        capeObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        capeObject.transform.SetParent(transform, false);
        capeMesh = new Mesh();
        capeMesh.name = "Cape Mesh";
        capeObject.AddComponent<MeshFilter>().sharedMesh = capeMesh;
        MeshRenderer capeRenderer = capeObject.AddComponent<MeshRenderer>();
        capeRenderer.sharedMaterial = capeMaterial;
        capeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // two copies of the cloth: one facing out and one facing in, so it shows from both sides
        int pointCount = ColumnCount * RowCount;
        capePoints = new Vector3[pointCount * 2];
        int[] triangleList = new int[(ColumnCount - 1) * (RowCount - 1) * 12];
        int triangleSpot = 0;
        for (int rowNumber = 0; rowNumber < RowCount - 1; rowNumber++)
        {
            for (int columnNumber = 0; columnNumber < ColumnCount - 1; columnNumber++)
            {
                int topLeft = rowNumber * ColumnCount + columnNumber;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + ColumnCount;
                int bottomRight = bottomLeft + 1;
                // outside face
                triangleList[triangleSpot] = topLeft; triangleList[triangleSpot + 1] = bottomLeft; triangleList[triangleSpot + 2] = topRight;
                triangleList[triangleSpot + 3] = topRight; triangleList[triangleSpot + 4] = bottomLeft; triangleList[triangleSpot + 5] = bottomRight;
                // inside face (same points, turned the other way)
                triangleList[triangleSpot + 6] = topLeft + pointCount; triangleList[triangleSpot + 7] = topRight + pointCount; triangleList[triangleSpot + 8] = bottomLeft + pointCount;
                triangleList[triangleSpot + 9] = topRight + pointCount; triangleList[triangleSpot + 10] = bottomRight + pointCount; triangleList[triangleSpot + 11] = bottomLeft + pointCount;
                triangleSpot = triangleSpot + 12;
            }
        }
        ShapeCape(0f);
        capeMesh.triangles = triangleList;
        capeMesh.RecalculateNormals();
        capeMesh.RecalculateBounds();
    }

    void LateUpdate()
    {
        if (capeObject == null || capeMesh == null)
        {
            return;
        }
        float swayTime = Application.isPlaying ? Time.time : 0f;
        ShapeCape(swayTime);
        capeMesh.RecalculateNormals();
        capeMesh.RecalculateBounds();
    }

    // Works out where every point of the cloth goes.
    // The skeleton faces its own +Z, so "behind" is -Z. Up is +Y and across the shoulders is X.
    void ShapeCape(float swayTime)
    {
        Vector3 skeletonScale = transform.lossyScale;
        float skeletonHeight = modelHeight * Mathf.Abs(skeletonScale.y);

        // place the cape object at the neck, a little behind and below it
        Vector3 neckPoint = anchorBone != null ? anchorBone.position : transform.position + transform.up * skeletonHeight * 0.85f;
        Vector3 capeTop = neckPoint - transform.forward * startBehind * skeletonHeight - transform.up * startBelow * skeletonHeight;
        capeObject.transform.position = capeTop;
        capeObject.transform.rotation = transform.rotation;

        int pointCount = ColumnCount * RowCount;
        for (int rowNumber = 0; rowNumber < RowCount; rowNumber++)
        {
            float downShare = rowNumber / (float)(RowCount - 1);     // 0 at the top, 1 at the bottom
            float halfWidth = Mathf.Lerp(topWidth, bottomWidth, downShare) * skeletonHeight * 0.5f;
            float flare = flareBehind * skeletonHeight * Mathf.Pow(downShare, 1.6f);
            float sway = Mathf.Sin(swayTime * swaySpeed + downShare * 2.5f) * swayAmount * skeletonHeight * downShare;
            for (int columnNumber = 0; columnNumber < ColumnCount; columnNumber++)
            {
                // the cloth wraps around the back like half a tube, from one shoulder to the other,
                // so from the side you see its full depth instead of a thin edge
                float wrapAngle = (columnNumber / (float)(ColumnCount - 1) - 0.5f) * Mathf.PI;
                float sideAmount = Mathf.Sin(wrapAngle) * halfWidth;
                float behindAmount = Mathf.Cos(wrapAngle) * (halfWidth + flare) + flare * 0.4f + sway;
                Vector3 worldOffset = new Vector3(sideAmount, -downShare * capeLength * skeletonHeight, -behindAmount);
                // the cape object is scaled with the skeleton, so undo that scale here
                Vector3 localPoint = new Vector3(worldOffset.x / skeletonScale.x, worldOffset.y / skeletonScale.y, worldOffset.z / skeletonScale.z);
                int pointNumber = rowNumber * ColumnCount + columnNumber;
                capePoints[pointNumber] = localPoint;
                capePoints[pointNumber + pointCount] = localPoint;
            }
        }
        capeMesh.vertices = capePoints;
    }
}
