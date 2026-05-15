using UnityEngine;

public class ExplosionController : MonoBehaviour
{
    private ParticleSystem[] childParticles;

    void Awake()
    {
        // 부모 기준으로 모든 자식 파티클 가져오기
        childParticles = GetComponentsInChildren<ParticleSystem>();
    }

    public void PlayAll()
    {
        foreach (var ps in childParticles)
        {
            ps.Play();
        }
    }

    public void StopAll()
    {
        foreach (var ps in childParticles)
        {
            ps.Stop();
        }
    }

    public void ClearAll()
    {
        foreach (var ps in childParticles)
        {
            ps.Clear();
        }
    }
}