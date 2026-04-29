using UnityEngine;

public class CivilianWalker : MonoBehaviour
{
    public float minSpeed = 1.2f;
    public float maxSpeed = 2.5f;
    public float roadOffsetAmount = 0.25f;

    private float moveSpeed;
    private RoadNode currentNode;
    private RoadNode targetNode;
    private RoadNode previousNode;

    private Vector3 currentOffset;
    private Vector3 targetOffset;

    public void SetStartNode(RoadNode startNode)
    {
        moveSpeed = Random.Range(minSpeed, maxSpeed);

        currentNode = startNode;
        previousNode = null;

        currentOffset = GetRandomOffset();
        transform.position = currentNode.transform.position + currentOffset;

        PickNextNode();
    }

    void Update()
    {
        if (targetNode == null) return;

        Vector3 targetPos = targetNode.transform.position + targetOffset;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        Vector3 direction = targetPos - transform.position;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            previousNode = currentNode;
            currentNode = targetNode;
            currentOffset = targetOffset;
            PickNextNode();
        }
    }

    void PickNextNode()
    {
        if (currentNode == null || currentNode.connectedNodes.Count == 0)
        {
            targetNode = null;
            return;
        }

        if (currentNode.connectedNodes.Count == 1)
        {
            targetNode = currentNode.connectedNodes[0];
        }
        else
        {
            RoadNode pickedNode;

            do
            {
                int randomIndex = Random.Range(0, currentNode.connectedNodes.Count);
                pickedNode = currentNode.connectedNodes[randomIndex];
            }
            while (pickedNode == previousNode && currentNode.connectedNodes.Count > 1);

            targetNode = pickedNode;
        }

        targetOffset = GetRandomOffset();
    }

    Vector3 GetRandomOffset()
    {
        return new Vector3(
            Random.Range(-roadOffsetAmount, roadOffsetAmount),
            0.3f,
            Random.Range(-roadOffsetAmount, roadOffsetAmount)
        );
    }
}