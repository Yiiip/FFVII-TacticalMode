using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using Cinemachine;
using UnityEngine.Rendering;
using DG.Tweening;

[System.Serializable] public class GameEvent : UnityEvent { }
[System.Serializable] public class TacticalModeEvent : UnityEvent<bool> { }

public class TacticalModeScript : MonoBehaviour
{
    #region 消息事件用于表现层
    [HideInInspector]
    public GameEvent OnAttack; //砍敌人
    [HideInInspector]
    public GameEvent OnModificationATB; //更改能量条
    [HideInInspector]
    public TacticalModeEvent OnTacticalTrigger; //进入或退出战术模式
    [HideInInspector]
    public TacticalModeEvent OnTargetSelectTrigger; //进入或退出选择敌人
    #endregion

    private MovementInput movement;
    private Animator anim;
    public WeaponCollision weapon;

    [Header("Time Stats")]
    public float slowMotionTime = .005f;

    [Space]
    [Header("States")]
    public bool tacticalMode; //是否处于战术模式
    public bool isAiming; //是否选择目标中
    public bool usingAbility; //标记正在使用能力，防抖其他的行为逻辑
    public bool dashing; //是否冲刺中，防抖其他的行为逻辑

    [Space]

    [Header("ATB Data")]
    public float atbSlider;
    public float filledAtbValue = 100;
    public int atbCount;

    [Space]

    [Header("Targets in radius")]
    public List<Transform> targets;
    public int targetIndex;
    public Transform aimObject;

    [Space]
    [Header("VFX")]
    public VisualEffect sparkVFX;
    public VisualEffect abilityVFX;
    public VisualEffect abilityHitVFX;
    public VisualEffect healVFX;
    [Space]
    [Header("Ligts")]
    public Light swordLight;
    public Light groundLight;
    [Header("Ligh Colors")]
    public Color sparkColor;
    public Color healColor;
    public Color abilityColot;
    [Space]
    [Header("Cameras")]
    public GameObject gameCam;
    public CinemachineVirtualCamera targetCam;

    [Space]
    public Volume slowMotionVolume;

    public float VFXDir = 5;

    private CinemachineImpulseSource camImpulseSource;

    private void Start()
    {
        weapon.onHit.AddListener(OnWeaponHitTarget);
        movement = GetComponent<MovementInput>();
        anim = GetComponent<Animator>();
        camImpulseSource = Camera.main.GetComponent<CinemachineImpulseSource>();
    }

    void Update()
    {
        //自动记录最近的敌人，目的是在任意位置进入战术模式后可提供首选目标
        if (targets.Count > 0 && !tacticalMode && !usingAbility)
        {
            targetIndex = NearestTargetToCenter();
            aimObject.LookAt(targets[targetIndex]);
        }

        //鼠标左键：砍敌人
        if (Input.GetMouseButtonDown(0) && !tacticalMode && !usingAbility)
        {
            OnAttack.Invoke();
            if(!dashing)
            {
                //MoveTowardsTarget(targets[targetIndex]);
                anim.SetTrigger("slash");
            }
        }

        //鼠标右键：战术
        if (Input.GetMouseButtonDown(1) && !usingAbility)
        {
            if (atbCount > 0 && !tacticalMode)
            {
                SetTacticalMode(true);
            }
        }

        //ESC键：取消单步行动
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAction();
        }
    }

    /// <summary>
    /// 转身进攻目标对象（UI按钮调用）
    /// </summary>
    public void SpinAttack()
    {
        ModifyATB(-100);

        StartCoroutine(AbilityCooldown());

        SetTacticalMode(false);

        MoveTowardsTarget(targets[targetIndex]); //移动到目标处

        //Animation
        anim.SetTrigger("ability");

        //Polish
        PlayVFX(abilityVFX, false);
        LightColor(groundLight, abilityColot, .3f);
    }

    /// <summary>
    /// 治疗（UI按钮调用）
    /// </summary>
    public void Heal()
    {
        ModifyATB(-100);

        StartCoroutine(AbilityCooldown());

        SetTacticalMode(false);

        //Animation
        anim.SetTrigger("heal");

        //Polish
        PlayVFX(healVFX, false);
        LightColor(groundLight, healColor, .5f);
    }

    /// <summary>
    /// 移动到目标处
    /// </summary>
    /// <param name="target"></param>
    public void MoveTowardsTarget(Transform target)
    {
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > 1 && distance < 10)
        {
            StartCoroutine(DashCooldown());
            transform.DOMove(TargetOffset(), .5f);
            transform.DOLookAt(targets[targetIndex].position, .2f);
        }
    }

    /// <summary>
    /// 标记防抖
    /// </summary>
    /// <returns></returns>
    IEnumerator AbilityCooldown()
    {
        usingAbility = true;
        yield return new WaitForSeconds(1f);
        usingAbility = false;
    }

    IEnumerator DashCooldown()
    {
        dashing = true;
        yield return new WaitForSeconds(1);
        dashing = false;
    }

    public Vector3 TargetOffset()
    {
        Vector3 targetPosition = targets[targetIndex].position;
        return Vector3.MoveTowards(targetPosition, transform.position, 1.2f);
    }

    /// <summary>
    /// 武器砍到敌人时触发的碰撞事件
    /// </summary>
    /// <param name="target"></param>
    public void OnWeaponHitTarget(Transform target)
    {
        OnModificationATB.Invoke();

        PlayVFX(sparkVFX, true);
        if (usingAbility)
            PlayVFX(abilityHitVFX, true, 4,4, .3f);

        ModifyATB(25);

        LightColor(swordLight, sparkColor, .1f);

        if (target.GetComponent<EnemyScript>() != null)
        {
            target.GetComponent<EnemyScript>().GetHit();
        }
    }

    /// <summary>
    /// 更改能量条
    /// </summary>
    /// <param name="delta">正负值</param>
    public void ModifyATB(float delta)
    {
        OnModificationATB.Invoke();

        atbSlider += delta;
        float maxAtbValue = filledAtbValue * 2;
        atbSlider = Mathf.Clamp(atbSlider, 0, maxAtbValue);

        if (delta > 0) //增加数值
        {
            if (atbSlider >= filledAtbValue && atbCount == 0)
                atbCount = 1;
            if (atbSlider >= maxAtbValue && atbCount == 1)
                atbCount = 2;
        }
        else //减少数值
        {
            if (atbSlider <= filledAtbValue)
                atbCount = 0;
            if (atbSlider >= filledAtbValue && atbCount == 0)
                atbCount = 1;
        }

        OnModificationATB.Invoke();
    }

    /// <summary>
    /// 清除一格能量条
    /// </summary>
    public void ClearATB()
    {
        float value = (atbCount == 1) ? 0 : 1;
        atbSlider = value;
    }

    /// <summary>
    /// 设置进入或退出战术模式
    /// </summary>
    /// <param name="on"></param>
    public void SetTacticalMode(bool on)
    {
        //停止移动逻辑
        movement.desiredRotationSpeed = on ? 0 : .3f;
        movement.active = !on;

        tacticalMode = on;

        if (!on)
        {
            SetAimCamera(false);
        }

        camImpulseSource.m_ImpulseDefinition.m_AmplitudeGain = on ? 0 : 2;

        //进入慢动作
        Time.timeScale = on ? slowMotionTime : 1f;

        //Polish
        DOVirtual.Float(on ? 0 : 1, on ? 1 : 0, .3f, SlowmotionPostProcessing).SetUpdate(true);

        OnTacticalTrigger.Invoke(on);
    }

    /// <summary>
    /// 通过UI选择敌人时触发
    /// </summary>
    /// <param name="index"></param>
    public void SelectTarget(int index)
    {
        targetIndex = index;
        aimObject.DOLookAt(targets[targetIndex].position, .3f).SetUpdate(true);
    }

    /// <summary>
    /// 是否使用选择敌人的相机（UI按钮调用true参数）
    /// </summary>
    /// <param name="on"></param>
    public void SetAimCamera(bool on)
    {
        if (targets.Count == 0) return;

        OnTargetSelectTrigger.Invoke(on); //通知UI层

        targetCam.LookAt = on ? aimObject : null;
        targetCam.Follow = on ? aimObject : null;
        targetCam.gameObject.SetActive(on); //使用选择敌人的专用相机
        isAiming = on; //isAiming Unused
    }

    // Unused
    IEnumerator RecenterCamera()
    {
        gameCam.GetComponent<CinemachineFreeLook>().m_RecenterToTargetHeading.m_enabled = true;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        gameCam.GetComponent<CinemachineFreeLook>().m_RecenterToTargetHeading.m_enabled = false;
    }

    /// <summary>
    /// 播放特效
    /// </summary>
    /// <param name="visualEffect"></param>
    /// <param name="shakeCamera"></param>
    /// <param name="shakeAmplitude"></param>
    /// <param name="shakeFrequency"></param>
    /// <param name="shakeSustain"></param>
    public void PlayVFX(VisualEffect visualEffect, bool shakeCamera, float shakeAmplitude = 2, float shakeFrequency = 2, float shakeSustain = .2f)
    {
        if (visualEffect == abilityHitVFX)
            LightColor(groundLight, abilityColot, .2f);

        if(visualEffect == sparkVFX)
            visualEffect.SetFloat("PosX", VFXDir);
        visualEffect.SendEvent("OnPlay");

        camImpulseSource.m_ImpulseDefinition.m_AmplitudeGain = shakeAmplitude;
        camImpulseSource.m_ImpulseDefinition.m_FrequencyGain = shakeFrequency;
        camImpulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_SustainTime = shakeSustain;

        if (shakeCamera)
            camImpulseSource.GenerateImpulse();
    }

    /// <summary>
    /// slash砍动画事件01
    /// </summary>
    public void DirRight()
    {
        VFXDir = -5;
    }
    /// <summary>
    /// slash砍动画事件02
    /// </summary>
    public void DirLeft()
    {
        VFXDir = 5;
    }

    /// <summary>
    /// 取消单步行动
    /// </summary>
    public void CancelAction()
    {
        if (!targetCam.gameObject.activeSelf && tacticalMode)
            SetTacticalMode(false);

        if (targetCam.gameObject.activeSelf)
            SetAimCamera(false);
    }

    /// <summary>
    /// 找到最近的攻击目标
    /// </summary>
    /// <returns>索引</returns>
    int NearestTargetToCenter()
    {
        float[] distances = new float[targets.Count];

        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        for (int i = 0; i < targets.Count; i++)
        {
            distances[i] = Vector2.Distance(Camera.main.WorldToScreenPoint(targets[i].position), screenCenter);
        }

        float minDistance = float.MaxValue;
        int index = 0;
        for (int i = 0; i < distances.Length; i++)
        {
            if (distances[i] <= minDistance)
            {
                minDistance = distances[i];
                index = i;
            }
        }
        return index;
    }

    /// <summary>
    /// 触发灯光特效并自动还原
    /// </summary>
    /// <param name="light"></param>
    /// <param name="color"></param>
    /// <param name="duration"></param>
    public void LightColor(Light light, Color color, float duration)
    {
        light.DOColor(color, duration).OnComplete(() => light.DOColor(Color.black, duration));   
    }
    /// <summary>
    /// 处理慢动作权重
    /// </summary>
    /// <param name="weight"></param>
    public void SlowmotionPostProcessing(float weight)
    {
        slowMotionVolume.weight = weight;
    }

    /// <summary>
    /// 通过碰撞球识别附近敌人并添加到targets列表中
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            targets.Add(other.transform);
        }
    }
    /// <summary>
    /// 通过碰撞球识别远离的敌人并从targets列表中移除
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (targets.Contains(other.transform))
                targets.Remove(other.transform);
        }
    }
}
