using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // 建立一个通用的单例模式
    public static T instance { get; private set; }

    protected virtual void Awake()
    {
        instance = this as T;
    }
}
