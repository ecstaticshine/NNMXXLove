using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuffType { Attack, Speed, Hp}

[System.Serializable]
public class Buff
{
    public BuffType type;
    public float value;    // 증가량 (예: 1.2f면 20% 증가)
    public int duration;   // 남은 턴 수
}
