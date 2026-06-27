using UnityEngine;

public class DetectorBattery : MonoBehaviour
{
    public float maxCharge = 100f;
    public float charge = 100f;
    public float scanDrainPerSecond = 5f;
    public float rechargePerSecond = 12f;
    public float emptyRecoveryThreshold = 12f;

    public float Charge01 => maxCharge <= 0f ? 0f : Mathf.Clamp01(charge / maxCharge);
    public bool IsEmpty => charge <= 0.01f;
    public bool CanScan => !IsEmpty && charge > emptyRecoveryThreshold * 0.25f;

    private void Awake()
    {
        charge = Mathf.Clamp(charge, 0f, maxCharge);
    }

    public bool ConsumeForScan(float deltaTime)
    {
        if (!CanScan)
        {
            return false;
        }

        charge = Mathf.Max(0f, charge - scanDrainPerSecond * deltaTime);
        return true;
    }

    public void Recharge(float deltaTime)
    {
        if (charge >= maxCharge)
        {
            return;
        }

        charge = Mathf.Min(maxCharge, charge + rechargePerSecond * deltaTime);
    }
}
