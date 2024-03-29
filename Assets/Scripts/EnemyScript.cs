using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    private Animator anim;
    public Renderer eyesRenderer;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// 受击
    /// </summary>
    public void GetHit()
    {
        anim.SetTrigger("hit");
        StopCoroutine(EyeHitSprite());
        StartCoroutine(EyeHitSprite());
    }

    IEnumerator EyeHitSprite()
    {
        eyesRenderer.material.SetTextureOffset("_BaseColorMap", new Vector2(0, -.33f)); //眼睛变成“X”的形状
        yield return new WaitForSeconds(.8f);
        eyesRenderer.material.SetTextureOffset("_BaseColorMap", new Vector2(.66f, 0)); //眼睛恢复
    }
}
