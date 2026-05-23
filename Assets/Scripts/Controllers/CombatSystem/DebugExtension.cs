using UnityEngine;

public static class DebugExtension
{
    public static void DrawWireSphere(Vector3 position, Color color, float radius)
    {
        float angle = 10f;

        Vector3 lastPoint = position + new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)) * radius;

        for (int i = 1; i <= 36; i++)
        {
            float rad = Mathf.Deg2Rad * angle * i;
            Vector3 nextPoint = position + new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * radius;

            Debug.DrawLine(lastPoint, nextPoint, color);
            lastPoint = nextPoint;
        }
    }
}