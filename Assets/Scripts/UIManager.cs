using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{

    private TacticalModeScript gameScript;
    public Image test;
    public CanvasGroup tacticalCanvas;
    public CanvasGroup attackCanvas;

    public Transform commandsGroup;
    public Transform targetGroup;

    public CanvasGroup aimCanvas;
    public bool aimAtTarget;

    public Slider atbSlider;
    public Image atbCompleteLeft;
    public Image atbCompleteRight;

    void Start()
    {
        gameScript = FindObjectOfType<TacticalModeScript>();
        gameScript.OnAttack.AddListener(AttackAction); //事件：砍敌人
        gameScript.OnModificationATB.AddListener(UpdateSlider); //事件：更改能量条
        gameScript.OnTacticalTrigger.AddListener(ShowTacticalMenu); //事件：进入或退出战术模式
        gameScript.OnTargetSelectTrigger.AddListener(ShowTargetOptions); //事件：进入或退出选择敌人
    }

    private void Update()
    {
        if (aimAtTarget)
        {
            aimCanvas.transform.position = Camera.main.WorldToScreenPoint(gameScript.targets[gameScript.targetIndex].position + Vector3.up);
        }
    }

    public void AttackAction()
    {
    }

    /// <summary>
    /// 更新能量条
    /// </summary>
    public void UpdateSlider()
    {
        atbSlider.DOComplete();
        atbSlider.DOValue(gameScript.atbSlider,.15f);

        atbCompleteLeft.DOFade(gameScript.atbSlider >= 100 ? 1 : 0, .2f);
        atbCompleteRight.DOFade(gameScript.atbSlider >= 200 ? 1 : 0, .2f);
    }

    /// <summary>
    /// 显示或隐藏战术菜单
    /// </summary>
    /// <param name="on"></param>
    public void ShowTacticalMenu(bool on)
    {
        tacticalCanvas.DOFade(on ? 1 : 0, .15f).SetUpdate(true);
        tacticalCanvas.interactable = on;
        attackCanvas.DOFade(on ? 0 : 1, .15f).SetUpdate(true);
        attackCanvas.interactable = !on;

        EventSystem.current.SetSelectedGameObject(null);

        if(on == true)
        {
            EventSystem.current.SetSelectedGameObject(tacticalCanvas.transform.GetChild(0).GetChild(0).gameObject);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(attackCanvas.transform.GetChild(0).gameObject);
            commandsGroup.gameObject.SetActive(!on);
            //targetGroup.gameObject.SetActive(on);
        }
    }

    /// <summary>
    /// 显示目标菜单
    /// </summary>
    /// <param name="on"></param>
    public void ShowTargetOptions(bool on)
    {
        EventSystem.current.SetSelectedGameObject(null);

        aimAtTarget = on;
        aimCanvas.alpha = on ? 1 : 0;

        commandsGroup.gameObject.SetActive(!on);
        targetGroup.GetComponent<CanvasGroup>().DOFade(on ? 1 : 0, .1f).SetUpdate(true);
        targetGroup.GetComponent<CanvasGroup>().interactable = on;

        if (on)
        {
            for (int i = 0; i < targetGroup.childCount; i++)
            {
                if (i <= gameScript.targets.Count - 1)
                {
                    targetGroup.GetChild(i).GetComponent<CanvasGroup>().alpha = 1;
                    targetGroup.GetChild(i).GetComponent<CanvasGroup>().interactable = true;
                    targetGroup.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = gameScript.targets[i].name;
                }
                else
                {
                    targetGroup.GetChild(i).GetComponent<CanvasGroup>().alpha = 0;
                    targetGroup.GetChild(i).GetComponent<CanvasGroup>().interactable = false;
                }
            }
        }
        EventSystem.current.SetSelectedGameObject(on ? targetGroup.GetChild(0).gameObject : commandsGroup.GetChild(0).gameObject);
    }
}
