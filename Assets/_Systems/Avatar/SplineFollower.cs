using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SplineFollower : MonoBehaviour
{
    public SplineContainer splineContainer;
    public float speed = 1.0f;

    // 连续旋转（360°慢速）设置
    [Header("Spin (continuous 360°)")]
    [SerializeField] public bool useContinuousSpin = true;
    [SerializeField] public float spinSpeedDegPerSec = 15f; // 旋转速度，越小越慢
    // 为 true 时绕前进方向旋转；false 时做上下（绕右轴）旋转
    [SerializeField] public bool spinAroundForward = true;

    // 正弦翻转（往返）设置：当 useContinuousSpin=false 时生效
    [Header("Flip (sinusoidal, fallback)")]
    [SerializeField] public float flipAmplitudeDeg = 25f;
    [SerializeField] public float flipFrequencyHz = 0.25f;
    [SerializeField] public bool flipAroundForward = false;

    private float t = 0.0f;
    private float _spinAngle; // 累积旋转角

    void Update()
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return;

        // Splines 2.x CalculateLength 需要传入样条的 localToWorld 矩阵
        float4x4 l2w = (float4x4)splineContainer.transform.localToWorldMatrix;
        float splineLength = SplineUtility.CalculateLength(splineContainer.Spline, l2w);
        if (splineLength <= 1e-6f)
            return;

        float distance = speed * Time.deltaTime;
        t += distance / splineLength;
        if (t > 1.0f)
            t -= 1.0f;

        float3 pos = SplineUtility.EvaluatePosition(splineContainer.Spline, t);
        float3 tan = SplineUtility.EvaluateTangent(splineContainer.Spline, t);

        Vector3 targetPosition = new Vector3(pos.x, pos.y, pos.z);
        Vector3 forward = new Vector3(tan.x, tan.y, tan.z).normalized;
        if (forward.sqrMagnitude < 1e-8f) return;

        // 构建基础朝向
        Vector3 worldUp = Vector3.up;
        Vector3 right = Vector3.Cross(worldUp, forward).normalized;
        if (right.sqrMagnitude < 1e-8f) right = Vector3.right;
        Vector3 up = Vector3.Cross(forward, right);
        Quaternion baseRot = Quaternion.LookRotation(forward, up);

        // 计算额外旋转：优先使用连续旋转
        Quaternion extra;
        if (useContinuousSpin)
        {
            _spinAngle += spinSpeedDegPerSec * Time.deltaTime;
            if (_spinAngle >= 360f) _spinAngle -= 360f;
            Vector3 axis = spinAroundForward ? forward : right;
            extra = Quaternion.AngleAxis(_spinAngle, axis);
        }
        else
        {
            float angle = Mathf.Sin(Time.time * Mathf.PI * 2f * flipFrequencyHz) * flipAmplitudeDeg;
            Vector3 axis = flipAroundForward ? forward : right;
            extra = Quaternion.AngleAxis(angle, axis);
        }

        transform.position = targetPosition;
        transform.rotation = baseRot * extra;
    }
}

