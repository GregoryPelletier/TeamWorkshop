using UnityEngine;
using UnityEngine.Tilemaps;

// Put on the tilemap that holds the level's ground, trunks and branches (ForestMap).
// When the level starts, the branch tiles become one-way platforms:
// you can jump up through a branch from below and land on top of it.
// The branches still look the same, only how they collide changes.
[RequireComponent(typeof(Tilemap))]
public class OneWayBranches : MonoBehaviour
{
    [Tooltip("The branch tiles that should be one-way.")]
    public TileBase[] branchTiles;

    void Awake()
    {
        Tilemap levelMap = GetComponent<Tilemap>();
        GameObject branchObject = new GameObject("One Way Branches");
        branchObject.layer = gameObject.layer;
        branchObject.transform.SetParent(transform.parent, false);
        branchObject.transform.localPosition = transform.localPosition;
        branchObject.transform.localRotation = transform.localRotation;
        branchObject.transform.localScale = transform.localScale;
        Tilemap branchMap = branchObject.AddComponent<Tilemap>();
        branchMap.tileAnchor = levelMap.tileAnchor;

        int branchCount = 0;
        foreach (Vector3Int cellPosition in levelMap.cellBounds.allPositionsWithin)
        {
            TileBase cellTile = levelMap.GetTile(cellPosition);
            if (cellTile == null || !IsBranch(cellTile))
            {
                continue;
            }
            // the copy on the new map does the colliding, the original only shows the picture
            branchMap.SetTile(cellPosition, cellTile);
            levelMap.SetColliderType(cellPosition, Tile.ColliderType.None);
            branchCount++;
        }
        // rebuild the level's collision now that the branches no longer collide on it
        TilemapCollider2D levelCollider = GetComponent<TilemapCollider2D>();
        if (levelCollider != null)
        {
            levelCollider.ProcessTilemapChanges();
        }
        CompositeCollider2D levelComposite = GetComponent<CompositeCollider2D>();
        if (levelComposite != null)
        {
            levelComposite.GenerateGeometry();
        }
        if (branchCount == 0)
        {
            Destroy(branchObject);
            return;
        }
        TilemapCollider2D branchCollider = branchObject.AddComponent<TilemapCollider2D>();
        branchCollider.usedByEffector = true;
        PlatformEffector2D oneWay = branchObject.AddComponent<PlatformEffector2D>();
        oneWay.useOneWay = true;
        oneWay.surfaceArc = 160f;
    }

    bool IsBranch(TileBase cellTile)
    {
        foreach (TileBase branchTile in branchTiles)
        {
            if (branchTile == cellTile)
            {
                return true;
            }
        }
        return false;
    }
}
