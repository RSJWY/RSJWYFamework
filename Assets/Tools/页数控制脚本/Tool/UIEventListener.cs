using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIEventListener : EventTrigger
{
    // 声明委托
    public delegate void VoidDelegate();
    public delegate void BoolDelegate(bool isValue);
    public delegate void FloatDelegate(float fValue);
    public delegate void IntDelegate(int iIndex);
    public delegate void StringDelegate(string strValue);
    public delegate void Vector2Delegate(Vector2 vector2Value);

    // 声明事件
    public VoidDelegate onSubmit;
    public VoidDelegate onClick;
    public Vector2Delegate onDrag;
    public BoolDelegate onHover;
    public BoolDelegate onToggleChanged;
    public FloatDelegate onSliderChanged;
    public FloatDelegate onScrollbarChanged;
    public IntDelegate onDrapDownChanged;
    public StringDelegate onInputFieldChanged;
    public BoolDelegate onPress;

    // 重写基类方法
    public override void OnSubmit(BaseEventData eventData)
    {
        if (onSubmit != null)
            onSubmit();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (onHover != null)
            onHover(true);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (onClick != null)
            onClick();
        if (onToggleChanged != null)
            onToggleChanged(gameObject.GetComponent<Toggle>().isOn);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (onPress != null)
        {
            onPress(true);
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (onPress != null)
        {
            onPress(false);
        }
        if (onDrag != null)
        {
            onDrag(eventData.delta);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (onHover != null)
            onHover(false);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (onSliderChanged != null)
            onSliderChanged(gameObject.GetComponent<Slider>().value);
        if (onScrollbarChanged != null)
            onScrollbarChanged(gameObject.GetComponent<Scrollbar>().value);
    }

    public override void OnSelect(BaseEventData eventData)
    {
        if (onDrapDownChanged != null)
            onDrapDownChanged(gameObject.GetComponent<Dropdown>().value);
    }

    public override void OnUpdateSelected(BaseEventData eventData)
    {
        if (onInputFieldChanged != null)
            onInputFieldChanged(gameObject.GetComponent<InputField>().text);
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        if (onInputFieldChanged != null)
            onInputFieldChanged(gameObject.GetComponent<InputField>().text);
    }

    // 获取UIEventListener组件
    public static UIEventListener Get(GameObject go)
    {
        UIEventListener listener = go.GetComponent<UIEventListener>();
        if (listener == null) listener = go.AddComponent<UIEventListener>();
        return listener;
    }
}
